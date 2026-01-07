using HMI_PanelSaw.Service;
using System;
using System.Windows;
using System.Windows.Input;


namespace HMI_PanelSaw.Views
{
    
    public partial class PasswordChangeDialog : Window
    {
        private readonly AuthService _authService;
        private readonly string _username;

        public PasswordChangeDialog(AuthService authService, string username)
        {
            InitializeComponent();
            _authService = authService;
            _username = username;

            txtUsername.Text = username;
            txtUsername.IsReadOnly = true;

            Loaded += (s, e) => txtCurrentPassword.Focus();
        }

        private void BtnChange_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lblError.Text = "";

                if (string.IsNullOrEmpty(txtCurrentPassword.Password))
                {
                    lblError.Text = "Please enter current password.";
                    txtCurrentPassword.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(txtNewPassword.Password))
                {
                    lblError.Text = "Please enter new password.";
                    txtNewPassword.Focus();
                    return;
                }

                if(txtNewPassword.Password != txtConfirmPassword.Password)
                {
                    lblError.Text = "New passwords do not match.";
                    txtConfirmPassword.Clear();
                    txtConfirmPassword.Focus();
                    return;
                }

                if (_authService.ChangePassword(_username, txtCurrentPassword.Password, txtNewPassword.Password, out string errorMessage))
                {
                    MessageBox.Show("Password changed succesfully","Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    lblError.Text = errorMessage;
                    txtCurrentPassword.Clear();
                    txtNewPassword.Clear();
                    txtConfirmPassword.Clear();
                    txtCurrentPassword.Focus();
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
                BtnChange_Click(null, null);
            else if (e.Key == Key.Escape)
                BtnCancel_Click(null, null);
        }
    }
}
