using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using HMI_PanelSaw.Service;
namespace HMI_PanelSaw.Views
{
    public partial class SettingsView : UserControl
    {
        private AuthService _authService;
        public SettingsView(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            LoadUserParameters();
        }
        private void LoadUserParameters()
        {
            txtUserId.Text = $"Id: {_authService.CurrentUser.Id}";
            txtUserUsername.Text = $"Username: {_authService.CurrentUser.Username}";
            txtUserRole.Text = $"Role : {_authService.CurrentUser.Role}";
            txtUserCreatedAt.Text = $"Created At: {_authService.CurrentUser.CreatedAt}";
            txtUserLastLogin.Text = $"Last Login At: {_authService.CurrentUser.LastLoginAt}";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UserManagementView userManagementView = new UserManagementView(_authService);
            Window userManagementWindow = new Window
            {
                Title = "User Management",
                Content = userManagementView,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Width = 1000,
                Height = 600,
                ResizeMode = ResizeMode.CanResize
            };
            userManagementWindow.ShowDialog();
        }
    }
}
