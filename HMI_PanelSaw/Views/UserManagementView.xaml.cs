using HMI_PanelSaw.Service;
using HMI_PanelSaw.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HMI_PanelSaw.Views
{
    public partial class UserManagementView : UserControl
    {
        private readonly AuthService _authService;
        private readonly ObservableCollection<User> _users;
        private readonly ObservableCollection<User> _filteredUsers;

        public UserManagementView(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            _users = new ObservableCollection<User>();
            _filteredUsers = new ObservableCollection<User>();
            dgUsers.ItemsSource = _filteredUsers;

            // Check if current user has admin rights
            if (_authService.CurrentUser?.Role != UserRole.Administrator)
            {
                DisableAdminFunctions();
            }

            LoadUsers();
        }

        private void DisableAdminFunctions()
        {
            btnAddUser.IsEnabled = false;
            btnEditUser.IsEnabled = false;
            btnDeleteUser.IsEnabled = false;
            btnChangePassword.IsEnabled = false;
            btnResetLockout.IsEnabled = false;

            btnAddUser.ToolTip = "Administrator access required";
            btnEditUser.ToolTip = "Administrator access required";
            btnDeleteUser.ToolTip = "Administrator access required";
            btnChangePassword.ToolTip = "Administrator access required";
            btnResetLockout.ToolTip = "Administrator access required";
        }

        private void LoadUsers()
        {
            try
            {
                using (var userRepository = new UserRepository())
                {
                    var users = userRepository.GetAllUsers();
                    _users.Clear();
                    foreach (var user in users)
                    {
                        _users.Add(user);
                    }
                    FilterUsers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterUsers()
        {
            _filteredUsers.Clear();
            var searchText = txtSearch.Text.ToLower();

            var filtered = string.IsNullOrEmpty(searchText)
                ? _users
                : _users.Where(u => u.Username.ToLower().Contains(searchText));

            foreach (var user in filtered)
            {
                _filteredUsers.Add(user);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterUsers();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (_authService.CurrentUser?.Role != UserRole.Administrator)
            {
                MessageBox.Show("Only administrators can add users.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new AddUserDialog(_authService);
            if (dialog.ShowDialog() == true)
            {
                LoadUsers();
            }
        }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (_authService.CurrentUser?.Role != UserRole.Administrator)
            {
                MessageBox.Show("Only administrators can edit users.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedUser = dgUsers.SelectedItem as User;
            if (selectedUser == null)
            {
                MessageBox.Show("Please select a user to edit.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new EditUserDialog(_authService, selectedUser);
            if (dialog.ShowDialog() == true)
            {
                LoadUsers();
            }
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (_authService.CurrentUser?.Role != UserRole.Administrator)
            {
                MessageBox.Show("Only administrators can force password changes.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedUser = dgUsers.SelectedItem as User;
            if (selectedUser == null)
            {
                MessageBox.Show("Please select a user.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _authService.ForcePasswordChange(selectedUser.Username);
                MessageBox.Show($"Password change forced for user '{selectedUser.Username}'. " +
                    "They will be required to change their password on next login.",
                    "Password Change Forced", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error forcing password change: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnResetLockout_Click(object sender, RoutedEventArgs e)
        {
            if (_authService.CurrentUser?.Role != UserRole.Administrator)
            {
                MessageBox.Show("Only administrators can reset lockouts.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedUser = dgUsers.SelectedItem as User;
            if (selectedUser == null)
            {
                MessageBox.Show("Please select a user.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var userRepository = new UserRepository())
                {
                    userRepository.UpdateLastLogin(selectedUser.Username); // This resets failed attempts and lockout
                }

                MessageBox.Show($"Lockout reset for user '{selectedUser.Username}'.",
                    "Lockout Reset", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting lockout: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (_authService.CurrentUser?.Role != UserRole.Administrator)
            {
                MessageBox.Show("Only administrators can delete users.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedUser = dgUsers.SelectedItem as User;
            if (selectedUser == null)
            {
                MessageBox.Show("Please select a user to delete.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Prevent deleting current user
            if (selectedUser.Username == _authService.CurrentUser.Username)
            {
                MessageBox.Show("You cannot delete your own account.", "Operation Not Allowed",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete user '{selectedUser.Username}'?\n\n" +
                "This action cannot be undone.", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var userRepository = new UserRepository())
                    {
                        userRepository.DeleteUser(selectedUser.Username);
                    }

                    MessageBox.Show($"User '{selectedUser.Username}' has been deleted.",
                        "User Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting user: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}