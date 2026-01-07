using HMI_PanelSaw.Service;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace HMI_PanelSaw.Views
{
    public partial class LoginView : Window
    {
        private readonly AuthService _authService;

        public LoginView()
        {
            InitializeComponent();
            _authService = new AuthService();
            _authService.PasswordChangeRequired += OnPasswordChangeRequired;

            Loaded += (s, e) => txtUsername.Focus();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = "";
            try
            {


                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    lblError.Text = "Please enter username.";
                    txtUsername.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    lblError.Text = "Please enter password.";
                    txtPassword.Focus();
                    return;
                }
                if (_authService.Login(txtUsername.Text, txtPassword.Password))
                {
                    MainWindow mainWindow = new MainWindow(_authService);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    lblError.Text = "Invalid username or password";
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch(InvalidOperationException ex) when(ex.Message.Contains("locked"))
            {
                lblError.Text = ex.Message;
                txtPassword.Clear();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Login error: {ex.Message}";
                txtPassword.Clear();
            }
        }
        private void OnPasswordChangeRequired(object sender, string message)
        {
            MessageBox.Show(message, "Password Change Required", MessageBoxButton.OK, MessageBoxImage.Warning);

            var passwordChangeDialog = new PasswordChangeDialog(_authService, _authService.CurrentUser.Username);
            if (passwordChangeDialog.ShowDialog() == true)
            {
                MainWindow mainWindow = new MainWindow(_authService);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                _authService.Logout();
                lblError.Text = "Password change is required to continue";
            }



        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnLogin_Click(null, null);
        }
    }
}
