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
using HMI_PanelSaw.Service;

namespace HMI_PanelSaw.Views
{

    public partial class ParametersView : UserControl
    {
        private AdsService _adsService;

        public ParametersView(AdsService adsService)
        {
            InitializeComponent();
            _adsService = adsService;
            InitializeParameterHandles();
            LoadParametersFromPLC();
        }
        private void InitializeParameterHandles()
        {
            try
            {
                _adsService.AddVariable("GVL_Parameters.stMainBlade.rBladeDiameter");
                _adsService.AddVariable("GVL_Parameters.stMainBlade.rBladeThickness");

                _adsService.AddVariable("GVL_Parameters.stScoringBlade.rBladeDiameter");
                _adsService.AddVariable("GVL_Parameters.stScoringBlade.rBladeThickness");

                _adsService.AddVariable("GVL_Parameters.stPanelData.rPanelLength");
                _adsService.AddVariable("GVL_Parameters.stPanelData.rPanelWidth");
                _adsService.AddVariable("GVL_Parameters.stPanelData.rPanelThickness");
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Error initializing parameters {ex.Message}","Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadParametersFromPLC()
        {
            try
            {
                txtPanelLength.Text = _adsService.Read<float>("GVL_Parameters.stPanelData.rPanelLength").ToString("F2");
                txtPanelWidth.Text = _adsService.Read<float>("GVL_Parameters.stPanelData.rPanelWidth").ToString("F2");
                txtPanelThickness.Text = _adsService.Read<float>("GVL_Parameters.stPanelData.rPanelThickness").ToString("F2");

                txtMainBladeDiameter.Text = _adsService.Read<float>("GVL_Parameters.stMainBlade.rBladeDiameter").ToString("F2");
                txtMainBladeThickness.Text = _adsService.Read<float>("GVL_Parameters.stMainBlade.rBladeThickness").ToString("F2");

                txtScoringBladeDiameter.Text = _adsService.Read<float>("GVL_Parameters.stScoringBlade.rBladeDiameter").ToString("F2");
                txtScoringBladeThickness.Text = _adsService.Read<float>("GVL_Parameters.stScoringBlade.rBladeThickness").ToString("F2");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading parameters from PLC {ex.Message}", "Loading error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SaveParametersToPLC()
        {
            try
            {
                if (!_adsService.IsConnected) return;
                _adsService.Write("GVL_Parameters.stPanelData.rPanelLength", float.Parse(txtPanelLength.Text));
                _adsService.Write("GVL_Parameters.stPanelData.rPanelWidth", float.Parse(txtPanelWidth.Text));
                _adsService.Write("GVL_Parameters.stPanelData.rPanelThickness", float.Parse(txtPanelThickness.Text));

                _adsService.Write("GVL_Parameters.stMainBlade.rBladeDiameter", float.Parse(txtMainBladeDiameter.Text));
                _adsService.Write("GVL_Parameters.stMainBlade.rBladeThickness", float.Parse(txtMainBladeThickness.Text));

                _adsService.Write("GVL_Parameters.stScoringBlade.rBladeDiameter", float.Parse(txtScoringBladeDiameter.Text));
                _adsService.Write("GVL_Parameters.stScoringBlade.rBladeThickness", float.Parse(txtScoringBladeThickness.Text));

                MessageBox.Show("Parameters saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving parameters to PLC {ex.Message}", "Saving error",MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLoadParameters_Click(object sender, RoutedEventArgs e)
        {
            LoadParametersFromPLC();
        }

        private void BtnSaveParameters_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Save parameters to PLC?","Confirm",MessageBoxButton.YesNo,MessageBoxImage.Question);
            if(result == MessageBoxResult.Yes)
            {
                SaveParametersToPLC();
            }
        }
    }
}
