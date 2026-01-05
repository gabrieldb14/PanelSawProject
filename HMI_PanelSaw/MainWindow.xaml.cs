using HMI_PanelSaw.Service;
using HMI_PanelSaw.Views;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TwinCAT.Ads;

namespace HMI_PanelSaw
{

    public partial class MainWindow : Window
    {
        private readonly AuthService _authService;
        private AdsService _adsClient = new AdsService();
        private DispatcherTimer _timerCommunication;

        private HomeView _homeView;
        private ParametersView _parametersView;
        private bool _isHomeViewActive;
        private bool _isParametersViewActive;
        private bool _isInitialized = false;

        private static readonly System.Windows.Media.SolidColorBrush ActiveBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 181, 246));
        private static readonly System.Windows.Media.SolidColorBrush EmergencyBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 38, 38));
        private static readonly System.Windows.Media.SolidColorBrush InactiveBrush = System.Windows.Media.Brushes.LightGray;

        public MainWindow(AuthService authService)
        {
            try
            {
                InitializeComponent();
                _authService = authService;
                InitComms();
                InitializeTimer();
                NavigateToHome();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to Initialize application: {ex.Message}","Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _isInitialized = false;
            }
            /*
            InitComms();
            InitializeComponent();
            _authService = authService;
            int pollingInterval = GetConfigValue("PlcPollingInterval", 100);
            _timerCommunication = new DispatcherTimer();
            _timerCommunication.Interval = TimeSpan.FromMilliseconds(pollingInterval);
            _timerCommunication.Tick += PLCReadCycle;
            _timerCommunication.Start();
            NavigateToHome();
            */

        }
        private void InitComms()
        {
            string adsNetId = ConfigurationManager.AppSettings["AdsNetId"];
            string adsPortStr = ConfigurationManager.AppSettings["AdsPort"];
            if (string.IsNullOrEmpty(adsNetId))
            {
                throw new ConfigurationErrorsException("AdsNetId is not configured in App.config");
            }
            if (string.IsNullOrEmpty(adsPortStr) || !int.TryParse(adsPortStr, out int adsPort))
            {
                throw new ConfigurationErrorsException("AdsPort is not configured or invalid in App.config");
            }
            _adsClient = new AdsService();
            try
            {
                _adsClient.Connect(adsNetId, adsPort);
                InitializePlcVariables();
            }
            catch (Exception ex)
            {
                _adsClient?.Dispose();
                throw new Exception($"Failed to connect to PLC: {ex.Message}", ex);
            }

        }
        private void InitializePlcVariables()
        {
            //Variables for handle creation
            var variables = new[]
            {
                "GVL_HMI.sStateDescription",
                "GVL_HMI.bStartCycle",
                "GVL_HMI.bStopCycle",
                "GVL_HMI.bEmergencyButton",
                "GVL_HMI.bEmergencyResetButton",
                "GVL_HMI.bSafetyFencesClosed",
                "GVL_HMI.bAirTablesActive",
                "GVL_HMI.nMachineState"
            };
            foreach (var variable in variables)
            {
                try
                {
                    _adsClient.AddVariable(variable);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to add variable {variable}: {ex.Message}", ex);
                }
            }
        }
        private int GetConfigValue(string key, int defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrEmpty(value) || !int.TryParse(value, out int result))
            {
                return defaultValue;
            }
            return result;
        }

        private void PLCReadCycle(object sender, EventArgs e)
        {
            {
                if (!_isInitialized || _adsClient?.IsConnected != true) return;

                try
                {
                    string stateDescription = _adsClient.Read<string>("GVL_HMI.sStateDescription");

                    Dispatcher.Invoke(() =>
                    {
                        txtStatus.Text = stateDescription ?? "No Status";
                        ButtonActive();
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtStatus.Text = $"ERROR: {ex.Message}";
                    });
                }
            }
        }
        private void InitializeTimer()
        {
            int pollingInterval = GetConfigValue("PlcPollingInterval", 100);
            _timerCommunication = new DispatcherTimer();
            _timerCommunication.Interval = TimeSpan.FromMilliseconds(pollingInterval);
            _timerCommunication.Tick += PLCReadCycle;
            _timerCommunication.Start();
        }
        private void ButtonActive()
        {
            if (!_isInitialized || _adsClient?.IsConnected != true) return;

            try
            {   
                bool airTable = _adsClient.Read<bool>("GVL_HMI.bAirTablesActive");
                bool safetyFences = _adsClient.Read<bool>("GVL_HMI.bSafetyFencesClosed");
                short machineState = _adsClient.Read<short>("GVL_HMI.nMachineState");

                btnAirTable.Background = airTable ? ActiveBrush : InactiveBrush;
                btnSafetyFences.Background = safetyFences ? ActiveBrush : InactiveBrush;
                btnStartCycle.Background = machineState >= 1 && machineState <= 3 ? ActiveBrush: InactiveBrush;
                btnStopCycle.Background = machineState == 4 ? ActiveBrush: InactiveBrush ;
                btnEmergency.Background = machineState == 999 ? EmergencyBrush : InactiveBrush;
                btnHome.Background =  _isHomeViewActive ? ActiveBrush : InactiveBrush;
                btnParameters.Background =  _isParametersViewActive ? ActiveBrush : InactiveBrush;
            }
            catch (Exception)
            {
            }
        }

        private void NavigateToHome()
        {
            if (_homeView == null)
                _homeView = new HomeView(_adsClient);
            ContentArea.Content = _homeView;
            _isParametersViewActive = false;
            _isHomeViewActive = true;
        }
        private void NavigateToParameters()
        {
            if (_parametersView == null)
                _parametersView = new ParametersView(_adsClient, _authService);

            ContentArea.Content = _parametersView;
            _isHomeViewActive = false;
            _isParametersViewActive = true;
        }
        
        private void BtnStartCycle_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized || _adsClient?.IsConnected != true) return;

            try
            {
                var startCycle = _adsClient.Read<bool>("GVL_HMI.bStartCycle");
                _adsClient.Write("GVL_HMI.bStartCycle", !startCycle);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error controlling start cycle: {ex.Message}", "Control Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnStopCycle_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized || _adsClient?.IsConnected != true) return;

            try
            {
                var stopCycle = _adsClient.Read<bool>("GVL_HMI.bStopCycle");
                _adsClient.Write("GVL_HMI.bStopCycle", !stopCycle);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error controlling stop cycle: {ex.Message}", "Control Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private async void BtnEmergencyReset_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized || _adsClient?.IsConnected != true) return;

            try
            {
                _adsClient.Write("GVL_HMI.bEmergencyResetButton", true);
                await Task.Delay(100);
                _adsClient.Write("GVL_HMI.bEmergencyResetButton", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during emergency reset: {ex.Message}", "Emergency Control Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEmergency_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized || _adsClient?.IsConnected != true) return;

            try
            {
                _adsClient.Write("GVL_HMI.bEmergencyButton", true);
                await Task.Delay(100);
                _adsClient.Write("GVL_HMI.bEmergencyButton", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during emergency stop: {ex.Message}", "Emergency Control Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            NavigateToHome();
        }

        private void BtnParameters_Click(object sender, RoutedEventArgs e)
        {
            NavigateToParameters();
        }
        private void BtnAirTable_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized || _adsClient?.IsConnected != true) return;

            try
            {
                var airTable = _adsClient.Read<bool>("GVL_HMI.bAirTablesActive");
                _adsClient.Write("GVL_HMI.bAirTablesActive", !airTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error controlling air table: {ex.Message}", "Control Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void BtnSafetyFences_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized || _adsClient?.IsConnected != true) return;

            try
            {
                var safetyFences = _adsClient.Read<bool>("GVL_HMI.bSafetyFencesClosed");
                _adsClient.Write("GVL_HMI.bSafetyFencesClosed", !safetyFences);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error controlling safety fences: {ex.Message}", "Control Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key) 
            {
                case Key.F1:
                    NavigateToHome();
                    break;
                case Key.F2:
                    NavigateToParameters();
                    break;
                case Key.F5:
                    BtnStartCycle_Click(null, null);
                    break;
                case Key.F6:
                    BtnStopCycle_Click(null, null);
                    break;
                case Key.F11:
                    BtnEmergencyReset_Click(null, null);
                    break;
                case Key.F12:
                    BtnEmergency_Click(null, null);
                    break;
            }

        }
        private void BtnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void BtnMinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                if (_timerCommunication != null)
                {
                    _timerCommunication.Stop();
                    _timerCommunication = null;
                }
                if (_homeView is IDisposable disposableHome)
                {
                    disposableHome.Dispose();
                }

                if (_parametersView is IDisposable disposableParams)
                {
                    disposableParams.Dispose();
                }
                _adsClient?.Dispose();
                _adsClient = null;
                _authService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
            finally
            {
                base.OnClosed(e);
            }
        }


    }
}
