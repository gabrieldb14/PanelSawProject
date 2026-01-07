using HMI_PanelSaw.Models;
using HMI_PanelSaw.Service;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace HMI_PanelSaw.Views
{
    public partial class EditUserDialog : Window
    {
        private readonly AuthService _authService;
        private readonly User _user;

        public EditUserDialog(AuthService authService, User user)
        {
            InitializeComponent();
            _authService = authService;
            _user = user;

            // Populate role combo box
            cmbRole.ItemsSource = Enum.GetValues(typeof(UserRole));

            // Load user data
            txtUsername.Text = user.Username;
            cmbRole.SelectedItem = user.Role;
            chkForcePasswordChange.IsChecked = user.ForcePasswordChange;

            // Username cannot be changed
            txtUsername.IsReadOnly = true;
            txtUsername.Background = System.Windows.Media.Brushes.LightGray;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lblError.Text = "";

                if (cmbRole.SelectedItem == null)
                {
                    lblError.Text = "Please select a role.";
                    cmbRole.Focus();
                    return;
                }

                var newRole = (UserRole)cmbRole.SelectedItem;
                var forcePasswordChange = chkForcePasswordChange.IsChecked == true;
                bool hasChanges = false;

                // Update user properties through UserRepository
                using (var userRepository = new UserRepository())
                {
                    // Update role if changed
                    if (newRole != _user.Role)
                    {
                        userRepository.UpdateUserRole(_user.Username, newRole);
                    }
                    if (!string.IsNullOrEmpty(txtNewPassword.Password)) 
                    {
                        if(_authService.AdminResetPassword(_user.Username, txtNewPassword.Password, out string passwordError)){
                            hasChanges = true;
                            forcePasswordChange = true;
                        }
                        else
                        {
                            lblError.Text = passwordError;
                            return;
                        }
                    }
                    // Update force password change if changed
                    if (forcePasswordChange != _user.ForcePasswordChange)
                    {
                        userRepository.SetForcePasswordChange(_user.Username, forcePasswordChange);
                        hasChanges = true;
                    }
                }
                if (hasChanges)
                {
                    string message = "User updated successfully.";
                    if (!string.IsNullOrWhiteSpace(txtNewPassword.Password))
                    {
                        message += " The user will be required to change their password on next login.";
                    }

                    MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("No changes were made.", "Information",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                MessageBox.Show("User updated successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Error: {ex.Message}";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnSave_Click(null, null);
            else if (e.Key == Key.Escape)
                BtnCancel_Click(null, null);
        }
    }
}