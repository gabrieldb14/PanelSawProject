using HMI_PanelSaw.Service;
using HMI_PanelSaw.Views;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TwinCAT.Ads;

namespace HMI_PanelSaw
{

    public partial class MainWindow : Window
    {
        private AdsService _adsClient = new AdsService();
        private DispatcherTimer _timerCommunication;

        private HomeView _homeView;
        private ParametersView _parametersView;
        private bool _isHomeViewActive;
        private bool _isParametersViewActive;

        private static readonly System.Windows.Media.SolidColorBrush ActiveBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 181, 246));
        private static readonly System.Windows.Media.SolidColorBrush EmergencyBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 38, 38));
        private static readonly System.Windows.Media.SolidColorBrush InactiveBrush = System.Windows.Media.Brushes.LightGray;

        public MainWindow()
        {
            InitComms();
            InitializeComponent();

            _timerCommunication = new DispatcherTimer();
            _timerCommunication.Interval = TimeSpan.FromMilliseconds(100);
            _timerCommunication.Tick += PLCReadCycle;
            _timerCommunication.Start();

            NavigateToHome();
        }
        private void InitComms()
        {
            _adsClient.Connect("192.168.2.108.1.1", 851);

            //Variables for handle creation
            _adsClient.AddVariable("GVL_HMI.sStateDescription");
            _adsClient.AddVariable("GVL_HMI.bStartCycle");
            _adsClient.AddVariable("GVL_HMI.bStopCycle");
            _adsClient.AddVariable("GVL_HMI.bEmergencyButton");
            _adsClient.AddVariable("GVL_HMI.bEmergencyResetButton");
            _adsClient.AddVariable("GVL_HMI.bSafetyFencesClosed");
            _adsClient.AddVariable("GVL_HMI.bAirTablesActive");
            _adsClient.AddVariable("GVL_HMI.nMachineState");

        }
        
        private void PLCReadCycle(object sender, EventArgs e)
        {
            if (!_adsClient.IsConnected) return;
            try
            {
                string stateDescription = _adsClient.Read<string>("GVL_HMI.sStateDescription");
                
                txtStatus.Text = string.Empty;
                txtStatus.InvalidateVisual();
                txtStatus.Text = stateDescription;
                ButtonActive();
            }
            catch (Exception ex)
            {
                txtStatus.Text = string.Empty;
                txtStatus.InvalidateVisual();
                txtStatus.Text = $"ERROR: {ex.Message}";
            }
        }
        private void ButtonActive()
        {
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
                _parametersView = new ParametersView(_adsClient);

            ContentArea.Content = _parametersView;
            _isHomeViewActive = false;
            _isParametersViewActive = true;
        }
        
        private void BtnStartCycle_Click(object sender, RoutedEventArgs e)
        {
            var startCycle = _adsClient.Read<bool>("GVL_HMI.bStartCycle");
            _adsClient.Write("GVL_HMI.bStartCycle",!startCycle);
        }

        private void BtnStopCycle_Click(object sender, RoutedEventArgs e)
        {
            var stopCycle = _adsClient.Read<bool>("GVL_HMI.bStopCycle");
            _adsClient.Write("GVL_HMI.bStopCycle", !stopCycle);
        }
        private async void BtnEmergencyReset_Click(object sender, RoutedEventArgs e)
        {
            _adsClient.Write("GVL_HMI.bEmergencyResetButton", true);
            await Task.Delay(100);
            _adsClient.Write("GVL_HMI.bEmergencyResetButton", false);
        }

        private async void BtnEmergency_Click(object sender, RoutedEventArgs e)
        {
            _adsClient.Write("GVL_HMI.bEmergencyButton", true);
            await Task.Delay(100);
            _adsClient.Write("GVL_HMI.bEmergencyButton", false);
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
            var airTable = _adsClient.Read<bool>("GVL_HMI.bAirTablesActive");
            _adsClient.Write("GVL_HMI.bAirTablesActive", !airTable);
        }
        private void BtnSafetyFences_Click(object sender, RoutedEventArgs e)
        {
            var safetyFences = _adsClient.Read<bool>("GVL_HMI.bSafetyFencesClosed");
            _adsClient.Write("GVL_HMI.bSafetyFencesClosed", !safetyFences);
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
                _timerCommunication?.Stop();
                _adsClient?.Dispose();
            }
            catch(Exception ex)
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
