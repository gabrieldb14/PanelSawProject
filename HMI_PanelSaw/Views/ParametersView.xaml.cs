using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const float MIN_PANEL_DIMENSION = 10f;     
        private const float MAX_PANEL_DIMENSION = 5000f;    
        private const float MIN_BLADE_DIAMETER = 100f;      
        private const float MAX_BLADE_DIAMETER = 600f;      
        private const float MIN_BLADE_THICKNESS = 1f;       
        private const float MAX_BLADE_THICKNESS = 10f;      

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
        private bool TryParseAndValidate(string input, string parameterName, float min, float max, out float result)
        {
            result = 0f;

            if (!float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                MessageBox.Show($"Invalid value for {parameterName}.\nPlease enter a valid number.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (result < min || result > max)
            {
                MessageBox.Show($"{parameterName} must be between {min:F2} mm and {max:F2} mm.\nEntered value: {result:F2} mm",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void SaveParametersToPLC()
        {
            try
            {
                if (!_adsService.IsConnected)
                {
                    MessageBox.Show("Not connected to PLC. Please check the connection.",
                        "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!TryParseAndValidate(txtPanelLength.Text, "Panel Length", MIN_PANEL_DIMENSION, MAX_PANEL_DIMENSION, out float panelLength))
                    return;

                if (!TryParseAndValidate(txtPanelWidth.Text, "Panel Width", MIN_PANEL_DIMENSION, MAX_PANEL_DIMENSION, out float panelWidth))
                    return;

                if (!TryParseAndValidate(txtPanelThickness.Text, "Panel Thickness", MIN_BLADE_THICKNESS, 100f, out float panelThickness))
                    return;
                if (!TryParseAndValidate(txtMainBladeDiameter.Text, "Main Blade Diameter", MIN_BLADE_DIAMETER, MAX_BLADE_DIAMETER, out float mainBladeDiameter))
                    return;

                if (!TryParseAndValidate(txtMainBladeThickness.Text, "Main Blade Thickness", MIN_BLADE_THICKNESS, MAX_BLADE_THICKNESS, out float mainBladeThickness))
                    return;
                if (!TryParseAndValidate(txtScoringBladeDiameter.Text, "Scoring Blade Diameter", MIN_BLADE_DIAMETER, MAX_BLADE_DIAMETER, out float scoringBladeDiameter))
                    return;

                if (!TryParseAndValidate(txtScoringBladeThickness.Text, "Scoring Blade Thickness", MIN_BLADE_THICKNESS, MAX_BLADE_THICKNESS, out float scoringBladeThickness))
                    return;

                _adsService.Write("GVL_Parameters.stPanelData.rPanelLength", panelLength);
                _adsService.Write("GVL_Parameters.stPanelData.rPanelWidth", panelWidth);
                _adsService.Write("GVL_Parameters.stPanelData.rPanelThickness", panelThickness);

                _adsService.Write("GVL_Parameters.stMainBlade.rBladeDiameter", mainBladeDiameter);
                _adsService.Write("GVL_Parameters.stMainBlade.rBladeThickness", mainBladeThickness);

                _adsService.Write("GVL_Parameters.stScoringBlade.rBladeDiameter", scoringBladeDiameter);
                _adsService.Write("GVL_Parameters.stScoringBlade.rBladeThickness", scoringBladeThickness);

                MessageBox.Show("Parameters saved successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving parameters to PLC: {ex.Message}\n\nPlease check the connection and try again.",
                    "Saving Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
