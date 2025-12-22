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
            _adsClient.AddVariable("GVL_HMI.rPusherFencePosition");
            _adsClient.AddVariable("GVL_HMI.sStateDescription");
            _adsClient.AddVariable("GVL_HMI.bStartCycle");
            _adsClient.AddVariable("GVL_HMI.bStopCycle");
            _adsClient.AddVariable("GVL_HMI.bEmergencyButton");
            _adsClient.AddVariable("GVL_HMI.bEmergencyResetButton");
            _adsClient.AddVariable("GVL_HMI.nTotalCut");
            _adsClient.AddVariable("GVL_HMI.bSafetyFencesClosed");
            _adsClient.AddVariable("GVL_HMI.bPusherClampActive");
            _adsClient.AddVariable("GVL_HMI.bPusherClampCommand");
            _adsClient.AddVariable("GVL_HMI.bPressureBeamCommand");
            _adsClient.AddVariable("GVL_HMI.bSafetyFencesClosed");
            _adsClient.AddVariable("GVL_HMI.bAirTablesActive");
            _adsClient.AddVariable("GVL_HMI.bMainSawActive");
            _adsClient.AddVariable("GVL_HMI.bScoringSawActive");
            _adsClient.AddVariable("GVL_HMI.nCurrentCutIndex");
            _adsClient.AddVariable("GVL_Parameters.stPanelData.rPanelWidth");
            _adsClient.AddVariable("GVL_Parameters.stPanelData.rPanelLength");
            _adsClient.AddVariable("GVL_Parameters.stPanelData.rPanelThickness");

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
                if (airTable)
                {
                    btnAirTable.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 181, 246));
                }
                else
                {
                    btnAirTable.Background = System.Windows.Media.Brushes.LightGray;
                }
                if (safetyFences)
                {
                    btnSafetyFences.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 181, 246));
                }
                else
                {
                    btnSafetyFences.Background = System.Windows.Media.Brushes.LightGray;
                }
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
            HighlightButton(btnHome);
        }
        private void NavigateToParameters()
        {
            if (_parametersView == null)
                _parametersView = new ParametersView(_adsClient);

            ContentArea.Content = _parametersView;
            HighlightButton(btnParameters);
        }
        
        private void HighlightButton(System.Windows.Controls.Button activeButton)
        {
            btnHome.Background = System.Windows.Media.Brushes.LightGray;
            btnParameters.Background = System.Windows.Media.Brushes.LightGray;

            activeButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 181, 246));
        }
        private async void BtnStartCycle_Click(object sender, RoutedEventArgs e)
        {
            _adsClient.Write("GVL_HMI.bStartCycle", true);
            await Task.Delay(100);
            _adsClient.Write("GVL_HMI.bStartCycle", false);
        }

        private async void BtnStopCycle_Click(object sender, RoutedEventArgs e)
        {
            _adsClient.Write("GVL_HMI.bStopCycle", true);
            await Task.Delay(100);
            _adsClient.Write("GVL_HMI.bStopCycle", false);
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
            _adsClient.Disconnect();
            base.OnClosed(e);
        }


    }
}
