using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using System.IO;
using System.Text;

namespace DS1000Z_E_USB_Control.Measurements
{
    /// <summary>
    /// Interaction logic for MeasurementPanel.xaml
    /// </summary>
    public partial class MeasurementPanel : UserControl
    {
        #region Fields

        private MeasurementController controller;
        private bool isInitialized = false;
        private bool panelCollapsed = false;
        private DispatcherTimer autoUpdateTimer;
        private readonly Dictionary<string, CheckBox> measurementCheckBoxes;
        private readonly Dictionary<string, Border> valueDisplays;
        private readonly Dictionary<string, Border> statisticsDisplays;

        #endregion

        #region Events

        /// <summary>
        /// Event raised for logging purposes
        /// </summary>
        public event EventHandler<string> LogEvent;

        #endregion

        #region Constructor

        public MeasurementPanel()
        {
            InitializeComponent();
            measurementCheckBoxes = new Dictionary<string, CheckBox>();
            valueDisplays = new Dictionary<string, Border>();
            statisticsDisplays = new Dictionary<string, Border>();
            InitializeAutoUpdateTimer();
            CreateMeasurementSelectionUI();
            isInitialized = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Set the measurement controller
        /// </summary>
        public MeasurementController Controller
        {
            get => controller;
            set
            {
                if (controller != null)
                {
                    controller.MeasurementValueUpdated -= Controller_MeasurementValueUpdated;
                    controller.MeasurementStatisticsUpdated -= Controller_MeasurementStatisticsUpdated;
                    controller.LogEvent -= Controller_LogEvent;
                }

                controller = value;

                if (controller != null)
                {
                    controller.MeasurementValueUpdated += Controller_MeasurementValueUpdated;
                    controller.MeasurementStatisticsUpdated += Controller_MeasurementStatisticsUpdated;
                    controller.LogEvent += Controller_LogEvent;
                    UpdateUIFromSettings();
                }
            }
        }

        /// <summary>
        /// Panel collapsed state
        /// </summary>
        public bool PanelCollapsed
        {
            get => panelCollapsed;
            set
            {
                panelCollapsed = value;
                UpdatePanelVisibility();
            }
        }

        #endregion

        #region Panel Visibility Management

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePanel();
        }

        private void TogglePanel()
        {
            PanelCollapsed = !PanelCollapsed;
        }

        private void UpdatePanelVisibility()
        {
            if (PanelCollapsed)
            {
                MainContent.Visibility = Visibility.Collapsed;
                ToggleIcon.Text = "🔼";
                ToggleText.Text = "Expand";
                StatusIndicator.Text = "Collapsed";
            }
            else
            {
                MainContent.Visibility = Visibility.Visible;
                ToggleIcon.Text = "🔽";
                ToggleText.Text = "Collapse";
                StatusIndicator.Text = "Ready";
            }
        }

        #endregion

        #region UI Creation

        /// <summary>
        /// Create the measurement selection UI dynamically
        /// </summary>
        private void CreateMeasurementSelectionUI()
        {
            var categories = MeasurementSettings.GetParametersByCategory();

            foreach (var category in categories)
            {
                // Create category expander
                var expander = new Expander
                {
                    Header = category.Key,
                    IsExpanded = true,
                    Margin = new Thickness(0, 2, 0, 2),
                    FontWeight = FontWeights.Bold
                };

                var categoryPanel = new StackPanel();

                foreach (var parameter in category.Value)
                {
                    var checkBox = new CheckBox
                    {
                        Content = parameter.DisplayName,
                        Tag = parameter.Key,
                        Margin = new Thickness(10, 2, 2, 2),
                        FontWeight = FontWeights.Normal,
                        ToolTip = $"{parameter.Description}\nUnit: {parameter.Unit}"
                    };

                    checkBox.Checked += MeasurementCheckBox_Checked;
                    checkBox.Unchecked += MeasurementCheckBox_Unchecked;

                    measurementCheckBoxes[parameter.Key] = checkBox;
                    categoryPanel.Children.Add(checkBox);
                }

                expander.Content = categoryPanel;
                MeasurementSelectionPanel.Children.Add(expander);
            }
        }

        /// <summary>
        /// Initialize auto-update timer
        /// </summary>
        private void InitializeAutoUpdateTimer()
        {
            autoUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            autoUpdateTimer.Tick += AutoUpdateTimer_Tick;
        }

        #endregion

        #region Event Handlers - Basic Controls

        private void AutoDisplay_Checked(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || controller == null) return;
            controller.SetAutoDisplay(true);
        }

        private void AutoDisplay_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || controller == null) return;
            controller.SetAutoDisplay(false);
        }

        private void SourceChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized || controller == null) return;

            var combo = sender as ComboBox;
            var selectedItem = combo?.SelectedItem as ComboBoxItem;
            var source = selectedItem?.Tag?.ToString();

            if (!string.IsNullOrEmpty(source))
            {
                controller.SetAutoMeasureSource(source);
                RefreshEnabledMeasurements();
            }
        }

        private void StatisticsDisplay_Checked(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || controller == null) return;
            controller.SetStatisticDisplay(true);
        }

        private void StatisticsDisplay_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || controller == null) return;
            controller.SetStatisticDisplay(false);
        }

        private void StatisticsMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized || controller == null) return;

            var combo = sender as ComboBox;
            var selectedItem = combo?.SelectedItem as ComboBoxItem;
            var mode = selectedItem?.Tag?.ToString();

            if (!string.IsNullOrEmpty(mode))
            {
                controller.SetStatisticMode(mode);
            }
        }

        private void AutoUpdate_Checked(object sender, RoutedEventArgs e)
        {
            autoUpdateTimer?.Start();
            LogEvent?.Invoke(this, "Auto-update enabled (2s interval)");
        }

        private void AutoUpdate_Unchecked(object sender, RoutedEventArgs e)
        {
            autoUpdateTimer?.Stop();
            LogEvent?.Invoke(this, "Auto-update disabled");
        }

        #endregion

        #region Event Handlers - Action Buttons

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            if (MessageBox.Show("Clear all measurements?", "Confirm Clear",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                controller.ClearAllMeasurements();
                UpdateMeasurementSelectionFromSettings();
                UpdateCurrentValuesDisplay();
                UpdateStatisticsDisplay();
            }
        }

        private void TimeDomainPreset_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;
            controller.ApplyTimeDomainPreset();
            UpdateMeasurementSelectionFromSettings();
        }

        private void VoltagePreset_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;
            controller.ApplyVoltagePreset();
            UpdateMeasurementSelectionFromSettings();
        }

        private void ComprehensivePreset_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;
            controller.ApplyComprehensivePreset();
            UpdateMeasurementSelectionFromSettings();
        }

        private void UpdateValues_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;
            UpdateAllMeasurementValues();
        }

        private void UpdateStatistics_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;
            UpdateAllMeasurementStatistics();
        }

        private void ResetStatistics_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            if (MessageBox.Show("Reset all measurement statistics?", "Confirm Reset",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                controller.ResetStatistics();
                UpdateStatisticsDisplay();
            }
        }

        private void ExportStatistics_Click(object sender, RoutedEventArgs e)
        {
            ExportStatisticsToFile();
        }

        #endregion

        #region Event Handlers - Threshold Setup

        private void ThresholdMax_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || controller == null) return;

            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                if (value >= 0 && value <= 100)
                {
                    controller.SetThresholdMax(value);
                }
                else
                {
                    textBox.Text = controller.Settings.ThresholdMax.ToString("F1");
                    MessageBox.Show("Threshold MAX must be between 0 and 100%", "Invalid Value",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ThresholdMid_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || controller == null) return;

            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                if (value >= 0 && value <= 100)
                {
                    controller.SetThresholdMid(value);
                }
                else
                {
                    textBox.Text = controller.Settings.ThresholdMid.ToString("F1");
                    MessageBox.Show("Threshold MID must be between 0 and 100%", "Invalid Value",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ThresholdMin_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || controller == null) return;

            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                if (value >= 0 && value <= 100)
                {
                    controller.SetThresholdMin(value);
                }
                else
                {
                    textBox.Text = controller.Settings.ThresholdMin.ToString("F1");
                    MessageBox.Show("Threshold MIN must be between 0 and 100%", "Invalid Value",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void PulseSetupB_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || controller == null) return;

            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                if (value >= 0 && value <= 100)
                {
                    controller.SetPulseSetupB(value);
                }
                else
                {
                    textBox.Text = controller.Settings.PulseSetupB.ToString("F1");
                    MessageBox.Show("Pulse Setup B must be between 0 and 100%", "Invalid Value",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void DelaySetupA_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || controller == null) return;

            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                if (value >= 0 && value <= 100)
                {
                    controller.SetDelaySetupA(value);
                }
                else
                {
                    textBox.Text = controller.Settings.DelaySetupA.ToString("F1");
                    MessageBox.Show("Delay Setup A must be between 0 and 100%", "Invalid Value",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void DelaySetupB_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || controller == null) return;

            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                if (value >= 0 && value <= 100)
                {
                    controller.SetDelaySetupB(value);
                }
                else
                {
                    textBox.Text = controller.Settings.DelaySetupB.ToString("F1");
                    MessageBox.Show("Delay Setup B must be between 0 and 100%", "Invalid Value",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        #endregion

        #region Event Handlers - Measurement Selection

        private void MeasurementCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || controller == null) return;

            var checkBox = sender as CheckBox;
            var measurementKey = checkBox?.Tag?.ToString();

            if (!string.IsNullOrEmpty(measurementKey))
            {
                controller.EnableMeasurement(measurementKey);
                CreateValueDisplay(measurementKey);
                CreateStatisticsDisplay(measurementKey);
                UpdateNoMeasurementsVisibility();
            }
        }

        private void MeasurementCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || controller == null) return;

            var checkBox = sender as CheckBox;
            var measurementKey = checkBox?.Tag?.ToString();

            if (!string.IsNullOrEmpty(measurementKey))
            {
                controller.DisableMeasurement(measurementKey);
                RemoveValueDisplay(measurementKey);
                RemoveStatisticsDisplay(measurementKey);
                UpdateNoMeasurementsVisibility();
            }
        }

        #endregion

        #region Controller Event Handlers

        private void Controller_MeasurementValueUpdated(object sender, MeasurementValueEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateValueDisplay(e.MeasurementKey, e.Value);
            });
        }

        private void Controller_MeasurementStatisticsUpdated(object sender, MeasurementStatisticsEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatisticsDisplay(e.MeasurementKey, e.Statistics);
            });
        }

        private void Controller_LogEvent(object sender, string message)
        {
            LogEvent?.Invoke(this, message);
        }

        #endregion

        #region Auto Update

        private void AutoUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (controller != null && !PanelCollapsed)
            {
                UpdateAllMeasurementValues();

                if (controller.Settings.StatisticDisplayEnabled)
                {
                    UpdateAllMeasurementStatistics();
                }
            }
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// Update UI from controller settings
        /// </summary>
        private void UpdateUIFromSettings()
        {
            if (controller?.Settings == null) return;

            var settings = controller.Settings;

            // Update basic controls
            AutoDisplayCheckBox.IsChecked = settings.AutoDisplayEnabled;
            StatisticsDisplayCheckBox.IsChecked = settings.StatisticDisplayEnabled;

            // Update source channel
            var sourceChannelOptions = MeasurementSettings.GetSourceChannelOptions();
            var sourceIndex = sourceChannelOptions.FindIndex(o => o.value == settings.AutoMeasureSource);
            if (sourceIndex >= 0 && sourceIndex < SourceChannelCombo.Items.Count)
            {
                SourceChannelCombo.SelectedIndex = sourceIndex;
            }

            // Update statistics mode
            var statisticModeOptions = MeasurementSettings.GetStatisticModeOptions();
            var modeIndex = statisticModeOptions.FindIndex(o => o.value == settings.StatisticMode);
            if (modeIndex >= 0 && modeIndex < StatisticsModeCombo.Items.Count)
            {
                StatisticsModeCombo.SelectedIndex = modeIndex;
            }

            // Update threshold values
            ThresholdMaxTextBox.Text = settings.ThresholdMax.ToString("F1");
            ThresholdMidTextBox.Text = settings.ThresholdMid.ToString("F1");
            ThresholdMinTextBox.Text = settings.ThresholdMin.ToString("F1");
            PulseSetupBTextBox.Text = settings.PulseSetupB.ToString("F1");
            DelaySetupATextBox.Text = settings.DelaySetupA.ToString("F1");
            DelaySetupBTextBox.Text = settings.DelaySetupB.ToString("F1");

            // Update measurement selections
            UpdateMeasurementSelectionFromSettings();
        }

        /// <summary>
        /// Update measurement selection checkboxes from settings
        /// </summary>
        private void UpdateMeasurementSelectionFromSettings()
        {
            if (controller?.Settings == null) return;

            foreach (var checkBox in measurementCheckBoxes.Values)
            {
                var measurementKey = checkBox.Tag?.ToString();
                checkBox.IsChecked = controller.Settings.IsMeasurementEnabled(measurementKey);
            }

            UpdateCurrentValuesDisplay();
            UpdateStatisticsDisplay();
        }

        /// <summary>
        /// Refresh enabled measurements with current source
        /// </summary>
        private void RefreshEnabledMeasurements()
        {
            if (controller?.Settings == null) return;

            var enabledMeasurements = controller.Settings.EnabledMeasurements.ToList();
            foreach (var measurementKey in enabledMeasurements)
            {
                controller.EnableMeasurement(measurementKey);
            }
        }

        /// <summary>
        /// Update all measurement values
        /// </summary>
        private void UpdateAllMeasurementValues()
        {
            if (controller == null) return;

            var values = controller.QueryAllMeasurementValues();
            foreach (var kvp in values)
            {
                UpdateValueDisplay(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Update all measurement statistics
        /// </summary>
        private void UpdateAllMeasurementStatistics()
        {
            if (controller == null) return;

            var statistics = controller.QueryAllMeasurementStatistics();
            foreach (var kvp in statistics)
            {
                UpdateStatisticsDisplay(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Update the "no measurements" visibility
        /// </summary>
        private void UpdateNoMeasurementsVisibility()
        {
            bool hasMeasurements = controller?.Settings?.EnabledMeasurements?.Any() == true;
            NoMeasurementsText.Visibility = hasMeasurements ? Visibility.Collapsed : Visibility.Visible;
            NoStatisticsText.Visibility = (hasMeasurements && controller.Settings.StatisticDisplayEnabled) ?
                Visibility.Collapsed : Visibility.Visible;
        }

        #endregion

        #region Value Display Management

        /// <summary>
        /// Create value display for a measurement
        /// </summary>
        private void CreateValueDisplay(string measurementKey)
        {
            if (valueDisplays.ContainsKey(measurementKey)) return;

            var parameters = MeasurementSettings.GetAvailableParameters();
            if (!parameters.ContainsKey(measurementKey)) return;

            var parameter = parameters[measurementKey];

            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(2),
                Padding = new Thickness(5)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var label = new TextBlock
            {
                Text = parameter.DisplayName + ":",
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(label, 0);

            var value = new TextBlock
            {
                Text = "---",
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            Grid.SetColumn(value, 1);

            grid.Children.Add(label);
            grid.Children.Add(value);
            border.Child = grid;
            border.Tag = measurementKey;

            valueDisplays[measurementKey] = border;
            CurrentValuesPanel.Children.Add(border);
        }

        /// <summary>
        /// Update value display for a measurement
        /// </summary>
        private void UpdateValueDisplay(string measurementKey, double value)
        {
            if (!valueDisplays.ContainsKey(measurementKey)) return;

            var border = valueDisplays[measurementKey];
            var grid = border.Child as Grid;
            var valueText = grid?.Children[1] as TextBlock;

            if (valueText != null)
            {
                var parameters = MeasurementSettings.GetAvailableParameters();
                if (parameters.ContainsKey(measurementKey))
                {
                    var parameter = parameters[measurementKey];
                    string formattedValue = FormatMeasurementValue(value, parameter.Unit);
                    valueText.Text = formattedValue;
                }
            }
        }

        /// <summary>
        /// Remove value display for a measurement
        /// </summary>
        private void RemoveValueDisplay(string measurementKey)
        {
            if (valueDisplays.ContainsKey(measurementKey))
            {
                CurrentValuesPanel.Children.Remove(valueDisplays[measurementKey]);
                valueDisplays.Remove(measurementKey);
            }
        }

        /// <summary>
        /// Update current values display
        /// </summary>
        private void UpdateCurrentValuesDisplay()
        {
            // Clear existing displays
            CurrentValuesPanel.Children.Clear();
            valueDisplays.Clear();

            // Add "No measurements" text back
            CurrentValuesPanel.Children.Add(NoMeasurementsText);

            // Create displays for enabled measurements
            if (controller?.Settings != null)
            {
                foreach (var measurementKey in controller.Settings.EnabledMeasurements)
                {
                    CreateValueDisplay(measurementKey);
                }
            }

            UpdateNoMeasurementsVisibility();
        }

        #endregion

        #region Statistics Display Management

        /// <summary>
        /// Create statistics display for a measurement
        /// </summary>
        private void CreateStatisticsDisplay(string measurementKey)
        {
            if (statisticsDisplays.ContainsKey(measurementKey)) return;

            var parameters = MeasurementSettings.GetAvailableParameters();
            if (!parameters.ContainsKey(measurementKey)) return;

            var parameter = parameters[measurementKey];

            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(2),
                Padding = new Thickness(5)
            };

            var stackPanel = new StackPanel();

            var header = new TextBlock
            {
                Text = parameter.DisplayName,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var statsGrid = new Grid();
            for (int i = 0; i < 3; i++)
                statsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            for (int i = 0; i < 2; i++)
                statsGrid.RowDefinitions.Add(new RowDefinition());

            // Current
            var currentLabel = new TextBlock { Text = "Current:", FontWeight = FontWeights.SemiBold };
            var currentValue = new TextBlock { Text = "---", FontFamily = new FontFamily("Consolas") };
            Grid.SetRow(currentLabel, 0); Grid.SetColumn(currentLabel, 0);
            Grid.SetRow(currentValue, 1); Grid.SetColumn(currentValue, 0);

            // Average
            var avgLabel = new TextBlock { Text = "Average:", FontWeight = FontWeights.SemiBold };
            var avgValue = new TextBlock { Text = "---", FontFamily = new FontFamily("Consolas") };
            Grid.SetRow(avgLabel, 0); Grid.SetColumn(avgLabel, 1);
            Grid.SetRow(avgValue, 1); Grid.SetColumn(avgValue, 1);

            // Range
            var rangeLabel = new TextBlock { Text = "Min/Max:", FontWeight = FontWeights.SemiBold };
            var rangeValue = new TextBlock { Text = "---", FontFamily = new FontFamily("Consolas") };
            Grid.SetRow(rangeLabel, 0); Grid.SetColumn(rangeLabel, 2);
            Grid.SetRow(rangeValue, 1); Grid.SetColumn(rangeValue, 2);

            statsGrid.Children.Add(currentLabel);
            statsGrid.Children.Add(currentValue);
            statsGrid.Children.Add(avgLabel);
            statsGrid.Children.Add(avgValue);
            statsGrid.Children.Add(rangeLabel);
            statsGrid.Children.Add(rangeValue);

            stackPanel.Children.Add(header);
            stackPanel.Children.Add(statsGrid);
            border.Child = stackPanel;
            border.Tag = measurementKey;

            statisticsDisplays[measurementKey] = border;
            StatisticsPanel.Children.Add(border);
        }

        /// <summary>
        /// Update statistics display for a measurement
        /// </summary>
        private void UpdateStatisticsDisplay(string measurementKey, MeasurementStatistics stats)
        {
            if (!statisticsDisplays.ContainsKey(measurementKey)) return;

            var border = statisticsDisplays[measurementKey];
            var stackPanel = border.Child as StackPanel;
            var statsGrid = stackPanel?.Children[1] as Grid;

            if (statsGrid != null && stats != null)
            {
                var parameters = MeasurementSettings.GetAvailableParameters();
                if (parameters.ContainsKey(measurementKey))
                {
                    var parameter = parameters[measurementKey];

                    var currentValue = statsGrid.Children[1] as TextBlock;
                    var avgValue = statsGrid.Children[3] as TextBlock;
                    var rangeValue = statsGrid.Children[5] as TextBlock;

                    if (currentValue != null)
                        currentValue.Text = FormatMeasurementValue(stats.Current, parameter.Unit);
                    if (avgValue != null)
                        avgValue.Text = FormatMeasurementValue(stats.Average, parameter.Unit);
                    if (rangeValue != null)
                        rangeValue.Text = $"{FormatMeasurementValue(stats.Minimum, parameter.Unit)} / {FormatMeasurementValue(stats.Maximum, parameter.Unit)}";
                }
            }
        }

        /// <summary>
        /// Remove statistics display for a measurement
        /// </summary>
        private void RemoveStatisticsDisplay(string measurementKey)
        {
            if (statisticsDisplays.ContainsKey(measurementKey))
            {
                StatisticsPanel.Children.Remove(statisticsDisplays[measurementKey]);
                statisticsDisplays.Remove(measurementKey);
            }
        }

        /// <summary>
        /// Update statistics display
        /// </summary>
        private void UpdateStatisticsDisplay()
        {
            // Clear existing displays
            StatisticsPanel.Children.Clear();
            statisticsDisplays.Clear();

            // Add "No statistics" text back
            StatisticsPanel.Children.Add(NoStatisticsText);

            // Create displays for enabled measurements
            if (controller?.Settings != null)
            {
                foreach (var measurementKey in controller.Settings.EnabledMeasurements)
                {
                    CreateStatisticsDisplay(measurementKey);
                }
            }

            UpdateNoMeasurementsVisibility();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Format measurement value with appropriate units and precision
        /// </summary>
        private string FormatMeasurementValue(double value, string unit)
        {
            switch (unit.ToLower())
            {
                case "time":
                    return FormatTimeValue(value);
                case "frequency":
                    return FormatFrequencyValue(value);
                case "voltage":
                    return FormatVoltageValue(value);
                case "percentage":
                    return $"{value:F2}%";
                case "count":
                    return value.ToString("F0");
                case "degrees":
                    return $"{value:F2}°";
                case "v/s":
                    return $"{value:E2} V/s";
                case "v·s":
                    return $"{value:E3} V·s";
                case "v²":
                    return $"{value:E3} V²";
                default:
                    return $"{value:E3} {unit}";
            }
        }

        private string FormatTimeValue(double timeInSeconds)
        {
            double absTime = Math.Abs(timeInSeconds);
            if (absTime >= 1.0)
                return $"{timeInSeconds:F3} s";
            else if (absTime >= 1e-3)
                return $"{timeInSeconds * 1000:F3} ms";
            else if (absTime >= 1e-6)
                return $"{timeInSeconds * 1000000:F3} μs";
            else
                return $"{timeInSeconds * 1000000000:F3} ns";
        }

        private string FormatFrequencyValue(double frequency)
        {
            double absFreq = Math.Abs(frequency);
            if (absFreq >= 1e9)
                return $"{frequency / 1e9:F3} GHz";
            else if (absFreq >= 1e6)
                return $"{frequency / 1e6:F3} MHz";
            else if (absFreq >= 1e3)
                return $"{frequency / 1e3:F3} kHz";
            else
                return $"{frequency:F3} Hz";
        }

        private string FormatVoltageValue(double voltage)
        {
            double absVoltage = Math.Abs(voltage);
            if (absVoltage >= 1.0)
                return $"{voltage:F3} V";
            else if (absVoltage >= 1e-3)
                return $"{voltage * 1000:F3} mV";
            else
                return $"{voltage * 1000000:F3} μV";
        }

        /// <summary>
        /// Export statistics to file
        /// </summary>
        private void ExportStatisticsToFile()
        {
            if (controller?.Statistics == null || !controller.Statistics.Any())
            {
                MessageBox.Show("No statistics data to export.", "Export Statistics",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"MeasurementStatistics_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Measurement Statistics Export");
                    sb.AppendLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"Source: {controller.Settings.AutoMeasureSource}");
                    sb.AppendLine();

                    sb.AppendLine("Measurement,Current,Average,Minimum,Maximum,Std Deviation,Sample Count,Unit");

                    var parameters = MeasurementSettings.GetAvailableParameters();
                    foreach (var kvp in controller.Statistics)
                    {
                        var stats = kvp.Value;
                        var parameter = parameters.ContainsKey(kvp.Key) ? parameters[kvp.Key] : null;
                        var unit = parameter?.Unit ?? "Unknown";

                        sb.AppendLine($"{kvp.Key},{stats.Current:E6},{stats.Average:E6}," +
                                    $"{stats.Minimum:E6},{stats.Maximum:E6}," +
                                    $"{stats.StandardDeviation:E6},{stats.Count},{unit}");
                    }

                    File.WriteAllText(saveDialog.FileName, sb.ToString());
                    MessageBox.Show($"Statistics exported successfully to:\n{saveDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting statistics:\n{ex.Message}", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}