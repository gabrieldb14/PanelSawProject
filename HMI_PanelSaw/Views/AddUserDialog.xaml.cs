using HMI_PanelSaw.Models;
using HMI_PanelSaw.Service;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace HMI_PanelSaw.Views
{
    public partial class AddUserDialog : Window
    {
        private readonly AuthService _authService;

        public AddUserDialog(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;

            // Populate role combo box
            cmbRole.ItemsSource = Enum.GetValues(typeof(UserRole));
            cmbRole.SelectedIndex = 0; // Default to Operator

            Loaded += (s, e) => txtUsername.Focus();
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lblError.Text = "";

                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    lblError.Text = "Please enter a username.";
                    txtUsername.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(txtPassword.Password))
                {
                    lblError.Text = "Please enter a password.";
                    txtPassword.Focus();
                    return;
                }

                if (txtPassword.Password != txtConfirmPassword.Password)
                {
                    lblError.Text = "Passwords do not match.";
                    txtConfirmPassword.Clear();
                    txtConfirmPassword.Focus();
                    return;
                }

                if (cmbRole.SelectedItem == null)
                {
                    lblError.Text = "Please select a role.";
                    cmbRole.Focus();
                    return;
                }

                var role = (UserRole)cmbRole.SelectedItem;

                if (_authService.AddUser(txtUsername.Text.Trim(), txtPassword.Password, role, out string errorMessage))
                {
                    MessageBox.Show("User created successfully.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    lblError.Text = errorMessage;
                    txtPassword.Clear();
                    txtConfirmPassword.Clear();
                    txtUsername.Focus();
                }
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
                BtnCreate_Click(null, null);
            else if (e.Key == Key.Escape)
                BtnCancel_Click(null, null);
        }
    }
}