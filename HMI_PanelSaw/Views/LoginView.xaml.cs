using HMI_PanelSaw.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HMI_PanelSaw.Views
{

    public partial class LoginView : Window
    {
        private readonly AuthService _authService;
        public LoginView()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (_authService.Login(txtUsername.Text, txtPassword.Password))
            {
                MainWindow mainWindow = new MainWindow(_authService);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                lblError.Text = "Wrong username or password.";
                txtPassword.Clear();
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnLogin_Click(null, null);
            /*
            switch (e.Key)
            {
                case Key.Enter:
                    BtnLogin_Click(null, null);
            }
            */
        }
    }
}
