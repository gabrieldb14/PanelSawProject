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

        public AuthService()
        {
            _userRepository = new UserRepository();
        }
        
        public bool Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            // Check if account is locked
            if (_userRepository.IsAccountLocked(username))
            {
                throw new InvalidOperationException(
                    $"Account is temporarily locked due to multiple failed login attempts. " +
                    $"Please try again in {LOCKOUT_DURATION_MINUTES} minutes.");
            }
            // Get user from database
            var user = _userRepository.GetByUsername(username);
            if (user == null || !VerifyPassword(password, user.PasswordHash)){
                _userRepository.IncrementFailedLoginAttempts(username, MAX_LOGIN_ATTEMPTS, LOCKOUT_DURATION_MINUTES);

                return false;
            }
            CurrentUser = user;
            _userRepository.UpdateLastLogin(username);
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

        public bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            var user = _userRepository.GetByUsername(username);
            if (user == null || !VerifyPassword(oldPassword, user.PasswordHash))
                return false;

            string newPasswordHash = HashPassword(newPassword);
            _userRepository.UpdatePassword(username, newPasswordHash);
            return true;
        }

        public void AddUser(string username, string password, UserRole role)
        {
            if (CurrentUser?.Role != UserRole.Administrator)
                throw new UnauthorizedAccessException("Only administrators can add users.");

            var newUser = new User
            {
                Username = username,
                PasswordHash = HashPassword(password),
                Role = role,
                CreatedAt = DateTime.Now
            };

            _userRepository.AddUser(newUser);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hash;
        }

        public void Dispose()
        {
            _userRepository?.Dispose();
        }
    }
}
