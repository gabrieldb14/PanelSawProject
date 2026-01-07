using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HMI_PanelSaw.Models;

namespace HMI_PanelSaw.Service
{
    public class AuthService : IDisposable
    {
        private readonly UserRepository _userRepository;
        private const int MAX_LOGIN_ATTEMPTS = 5;
        private const int LOCKOUT_DURATION_MINUTES = 5;

        public User CurrentUser { get; private set; }
        public event EventHandler<string> PasswordChangeRequired;

        public AuthService()
        {
            _userRepository = new UserRepository();
        }
        //Login method
        public bool Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            //Check if account is Locked up
            if (_userRepository.IsAccountLocked(username))
            {
                throw new InvalidOperationException(
                    $"Account is temporarily locked due to multiple failed login attempts. " +
                    $"Please try again in {LOCKOUT_DURATION_MINUTES} minutes.");
            }

            var user = _userRepository.GetByUsername(username);
            if (user == null)
            {
                _userRepository.IncrementFailedLoginAttempts(username, MAX_LOGIN_ATTEMPTS, LOCKOUT_DURATION_MINUTES);
                return false;
            }

            bool passwordValid = VerifyPassword(password, user.PasswordHash);
            if (!passwordValid) //If password is wrong, increments FailedLoginAttempts in database
            {
                _userRepository.IncrementFailedLoginAttempts(username, MAX_LOGIN_ATTEMPTS, LOCKOUT_DURATION_MINUTES);
                return false;
            }

            if (PasswordHashingService.IsLegacyHash(user.PasswordHash) ||
                PasswordHashingService.NeedsRehash(user.PasswordHash))
            {
                string newHashedPassword = PasswordHashingService.HashPassword(password).ToStorageString();
                _userRepository.UpdatePassword(username, newHashedPassword);
            }

            CurrentUser = user;
            _userRepository.UpdateLastLogin(username);

            //Check if user has to change password to login
            if (user.ForcePasswordChange)
            {
                PasswordChangeRequired?.Invoke(this, "You must change your password before continuing.");
            }

            return true;
        }
        public void Logout()
        {
            CurrentUser = null;
        }
        public bool CanExecuteCycle() => CurrentUser != null;
        public bool CanViewHistory() => CurrentUser?.Role >= UserRole.Supervisor;
        public bool CanEditParameters() => CurrentUser?.Role >= UserRole.Supervisor;
        public bool CanAccessAdmin() => CurrentUser?.Role >= UserRole.Administrator;

        //Change password method
        public bool ChangePassword(string username, string oldPassword, string newPassword, out string errorMessage)
        {
            errorMessage = null;

            var validationResult = PasswordHashingService.ValidatePassword(newPassword);
            if (!validationResult.IsValid)
            {
                errorMessage = validationResult.ErrorMessage;
                return false;
            }

            var user = _userRepository.GetByUsername(username);
            if (user == null)
            {
                errorMessage = "User not found.";
                return false;
            }

            if (!VerifyPassword(oldPassword, user.PasswordHash))
            {
                errorMessage = "Current password is incorrect.";
                return false;
            }

            if (VerifyPassword(newPassword, user.PasswordHash))
            {
                errorMessage = "New password must be different from current password.";
                return false;
            }

            string newPasswordHash = PasswordHashingService.HashPassword(newPassword).ToStorageString();
            _userRepository.UpdatePassword(username, newPasswordHash);

            return true;
        }
        public bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            return ChangePassword(username, oldPassword, newPassword, out _);
        }

        // Add new user method
        public bool AddUser(string username, string password, UserRole role, out string errorMessage)
        {
            errorMessage = null;

            if (CurrentUser?.Role != UserRole.Administrator)
            {
                errorMessage = "Only administrators can add users.";
                return false;
            }

            var validationResult = PasswordHashingService.ValidatePassword(password);
            if (!validationResult.IsValid) // Check if password is in the right format and size
            {
                errorMessage = validationResult.ErrorMessage;
                return false;
            }

            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            {
                errorMessage = "Username must be at least 3 characters long.";
                return false;
            }

            try
            {
                var newUser = new User 
                {
                    Username = username,
                    PasswordHash = PasswordHashingService.HashPassword(password).ToStorageString(),
                    Role = role,
                    CreatedAt = DateTime.Now,
                    LastPasswordChange = DateTime.Now,
                    ForcePasswordChange = false // New users don't need to change password immediately
                };

                _userRepository.AddUser(newUser);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to add user: {ex.Message}";
                return false;
            }
        }
        
        //Admin force reset password method
        public bool AdminResetPassword(string username, string newPassword, out string errorMessage)
        {
            errorMessage = null;

            if(CurrentUser?.Role != UserRole.Administrator) //Double check for user(admin) role
            {
                errorMessage = "Only administrators can reset user passwords";
                return false;
            }
           
            var validationResult = PasswordHashingService.ValidatePassword(newPassword);
            if (!validationResult.IsValid)
            {
                errorMessage = validationResult.ErrorMessage;
                return false;
            }

            var user = _userRepository.GetByUsername(username);
            if (user == null)
            {
                errorMessage = "User not found.";
                return false;
            }
            try
            {
                string newPasswordHash = PasswordHashingService.HashPassword(newPassword).ToStorageString();

                _userRepository.UpdatePassword(username, newPasswordHash);
                
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to reset password: {ex.Message}";
                return false;
            }
        }
        public bool AdminResetPassword(string username, string newPassword)
        {
            return AdminResetPassword(username, newPassword, out _);
        }

        public void AddUser(string username, string password, UserRole role)
        {
            if (!AddUser(username, password, role, out string errorMessage))
            {
                throw new InvalidOperationException(errorMessage);
            }
        }
        public void ForcePasswordChange(string username)
        {
            if (CurrentUser?.Role != UserRole.Administrator)
                throw new UnauthorizedAccessException("Only administrators can force password changes.");

            _userRepository.SetForcePasswordChange(username, true);
        }
        private bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            // Check if this is a legacy SHA256 hash (no salt)
            if (PasswordHashingService.IsLegacyHash(storedHash))
            {
                return VerifyLegacyPassword(password, storedHash);
            }

            // Use new secure verification
            return PasswordHashingService.VerifyPassword(password, storedHash);
        }
        private bool VerifyLegacyPassword(string password, string legacyHash)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var computedHash = Convert.ToBase64String(hashedBytes);
                return computedHash == legacyHash;
            }
        }
        public void Dispose()
        {
            _userRepository?.Dispose();
        }
    }
}
