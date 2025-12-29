using HMI_PanelSaw.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HMI_PanelSaw.Models;
using System.IO;
using Microsoft.Win32;

namespace HMI_PanelSaw.Views
{
    public partial class HomeView : UserControl
    {
        private AdsService _adsService;
        private DispatcherTimer _timerCommunication;
        private ObservableCollection<CutData> _cutList;
        private const int MAX_CUTS = 20;

        public HomeView(AdsService adsService)
        {

            InitializeComponent();
            _adsService = adsService;
            _cutList = new ObservableCollection<CutData>();
            dgCutList.ItemsSource = _cutList;
            InitializeParameterHandles();
            LoadCutListFromPLC();
            _timerCommunication = new DispatcherTimer();
            _timerCommunication.Interval = TimeSpan.FromMilliseconds(100);
            _timerCommunication.Tick += LoadParametersFromPLC;
            _timerCommunication.Start();

            this.Loaded += HomeView_Loaded;
            this.Unloaded += HomeView_Unloaded;
        }

        private void HomeView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_timerCommunication != null && !_timerCommunication.IsEnabled)
            {
                _timerCommunication.Start();
            }
        }
        private void HomeView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_timerCommunication != null && _timerCommunication.IsEnabled)
            {
                _timerCommunication.Stop();
            }
        }

        private void InitializeParameterHandles()
        {
            try
            {
                _adsService.AddVariable("GVL_HMI.nTotalCut");
                _adsService.AddVariable("GVL_HMI.bSafetyFencesClosed");
                _adsService.AddVariable("GVL_HMI.bPusherClampActive");
                _adsService.AddVariable("GVL_HMI.rPusherFencePosition");
                _adsService.AddVariable("GVL_HMI.bMainSawActive");
                _adsService.AddVariable("GVL_HMI.bScoringSawActive");
                _adsService.AddVariable("GVL_HMI.bAirTablesActive");
                _adsService.AddVariable("GVL_HMI.nCurrentCutIndex");
                _adsService.AddVariable("GVL_Parameters.stPanelData.rPanelLength");
                _adsService.AddVariable("GVL_Parameters.stPanelData.rPanelWidth");
                _adsService.AddVariable("GVL_Parameters.stPanelData.rPanelThickness");
                _adsService.AddVariable("PRG_MACHINE.fbPusherClamp.eState");
                _adsService.AddVariable("PRG_MACHINE.fbPressureBeam.eState");
                for (int i = 1; i <= MAX_CUTS; i++)
                {
                    _adsService.AddVariable($"GVL_HMI.arCutList[{i}]");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing parameters {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadParametersFromPLC(object sender, EventArgs e)
        {
            if (!this.IsLoaded || _adsService == null || !_adsService.IsConnected) return;
            try
            {
                txtPusherFencePosition.Text = _adsService.Read<float>("GVL_HMI.rPusherFencePosition").ToString("F2");
                PusherClampState clampState = (PusherClampState)_adsService.Read<short>("PRG_MACHINE.fbPusherClamp.eState");
                txtPusherClampStatus.Text = clampState.ToString();
                PressureBeamState pressureBeamState = (PressureBeamState)_adsService.Read<short>("PRG_MACHINE.fbPressureBeam.eState");
                txtPressureBeamStatus.Text = pressureBeamState.ToString();
                txtSafetyFence.Text = _adsService.Read<bool>("GVL_HMI.bSafetyFencesClosed") ? "Closed" : "Open";
                txtAirTable.Text = _adsService.Read<bool>("GVL_HMI.bAirTablesActive") ? "Active" : "Inactive";


                txtTotalCut.Text = _adsService.Read<short>("GVL_HMI.nTotalCut").ToString();
                txtCurrentCutIndex.Text = _adsService.Read<short>("GVL_HMI.nCurrentCutIndex").ToString();

                txtPanelLength.Text = _adsService.Read<float>("GVL_Parameters.stPanelData.rPanelLength").ToString("F2");
                txtPanelWidth.Text = _adsService.Read<float>("GVL_Parameters.stPanelData.rPanelWidth").ToString("F2");
                txtPanelThickness.Text = _adsService.Read<float>("GVL_Parameters.stPanelData.rPanelThickness").ToString("F2");
                
                txtMainSawStatus.Text = _adsService.Read<bool>("GVL_HMI.bMainSawActive") ? "On" : "Off";
                txtScoringSawStatus.Text = _adsService.Read<bool>("GVL_HMI.bScoringSawActive") ? "On" : "Off";
                

            }
            catch (Exception ex)
            {
                if (this.IsLoaded)
                {
                    MessageBox.Show($"Error loading parameters: {ex.Message}",
                        "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void LoadCutListFromPLC()
        {
            try
            {
                _cutList.Clear();
                int totalCuts = _adsService.Read<short>("GVL_HMI.nTotalCut");

                for (int i = 1; i <= totalCuts && i <= MAX_CUTS; i++)
                {
                    float cutLength = _adsService.Read<float>($"GVL_HMI.arCutList[{i}]");
                    if (cutLength > 0)
                    {
                        _cutList.Add(new CutData
                        {
                            CutNumber = i,
                            CutLength = cutLength,
                            Repetitions = 1
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading cut list from PLC: {ex.Message}", "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SaveCutListToPLC()
        {
            try
            {
                var expandedCuts = new List<float>();
                foreach (var cut in _cutList)
                {
                    for (int i = 0; i < cut.Repetitions; i++)
                    {
                        expandedCuts.Add((float)cut.CutLength);
                        if (expandedCuts.Count >= MAX_CUTS)
                            break;
                    }
                    if (expandedCuts.Count >= MAX_CUTS)
                        break;
                }
                _adsService.Write("GVL_HMI.nTotalCut", (short)expandedCuts.Count);

                for (int i = 0; i < MAX_CUTS; i++)
                {
                    float value = (i < expandedCuts.Count) ? expandedCuts[i] : 0f;
                    _adsService.Write($"GVL_HMI.arCutList[{i + 1}]", value);
                }

                MessageBox.Show($"Saved {expandedCuts.Count} cuts to PLC successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving cut list to PLC: {ex.Message}", "Saving Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    Title = "Select Cut List File",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    LoadCutsFromCSV(openFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}", "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadCutsFromCSV(string filePath)
        {
            try
            {
                _cutList.Clear();
                int lineNumber = 0;
                int cutsLoaded = 0;
                int totalExpandedCuts = 0;

                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    bool isFirstLine = true;

                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNumber++;

                        if (isFirstLine)
                        {
                            isFirstLine = false;
                            if (line.ToLower().Contains("length") || line.ToLower().Contains("cut") || line.ToLower().Contains("repetition"))
                            {
                                continue;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        string[] values = line.Split(new[] { ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                        if (values.Length >= 1)
                        {
                            double cutLength;
                            int repetitions = 1;

                            if (double.TryParse(values[0].Trim(), out cutLength))
                            {
                                if (values.Length >= 2)
                                {
                                    int.TryParse(values[1].Trim(), out repetitions);
                                    if (repetitions < 1) repetitions = 1;
                                }
                                int newTotal = totalExpandedCuts + repetitions;

                                if (newTotal > MAX_CUTS)
                                {
                                    int remainingSlots = MAX_CUTS - totalExpandedCuts;
                                    if (remainingSlots > 0)
                                    {
                                        _cutList.Add(new CutData
                                        {
                                            CutNumber = ++cutsLoaded,
                                            CutLength = cutLength,
                                            Repetitions = remainingSlots
                                        });
                                        totalExpandedCuts += remainingSlots;
                                    }

                                    MessageBox.Show($"Maximum of {MAX_CUTS} total cuts reached. Remaining cuts in file were not imported.", 
                                        "Import Limit Reached", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    break;
                                }

                                _cutList.Add(new CutData
                                {
                                    CutNumber = ++cutsLoaded,
                                    CutLength = cutLength,
                                    Repetitions = repetitions
                                });

                                totalExpandedCuts += repetitions;
                            }
                        }
                    }

                    if (cutsLoaded == 0)
                    {
                        MessageBox.Show("No valid cuts found in the file.", "Import Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show($"Successfully loaded {cutsLoaded} cut entries ({totalExpandedCuts} total cuts) from file.", 
                            "Import Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading CSV file: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnRemoveCut_Click(object sender, RoutedEventArgs e)
        {
            if (dgCutList.SelectedItem is CutData selectedCut)
            {
                _cutList.Remove(selectedCut);
                UpdateCutNumbers();
            }
            else
            {
                MessageBox.Show("Please select a cut to remove.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void BtnClearAllCuts_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Clear all cuts?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _cutList.Clear();
            }
        }
        private void BtnSaveCuts_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Save cut list to PLC?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                SaveCutListToPLC();
            }
        }
        private void UpdateCutNumbers()
        {
            for (int i = 0; i < _cutList.Count; i++)
            {
                _cutList[i].CutNumber = i + 1;
            }
        }
    }
}
