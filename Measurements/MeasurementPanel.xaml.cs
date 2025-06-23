using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DS1000Z_E_USB_Control.Measurements
{
    /// <summary>
    /// Interaction logic for MeasurementPanel.xaml
    /// Complete implementation with all event handlers and UI management
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

        #region Initialization

        /// <summary>
        /// Initialize the auto-update timer
        /// </summary>
        private void InitializeAutoUpdateTimer()
        {
            autoUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            autoUpdateTimer.Tick += AutoUpdateTimer_Tick;
        }

        /// <summary>
        /// Auto-update timer tick event
        /// </summary>
        private void AutoUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (controller != null)
            {
                UpdateAllMeasurementValues();
                UpdateAllMeasurementStatistics();
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
            if (MainContent != null)
            {
                if (PanelCollapsed)
                {
                    MainContent.Visibility = Visibility.Collapsed;
                    if (ToggleIcon != null) ToggleIcon.Text = "🔼";
                    if (ToggleText != null) ToggleText.Text = "Expand";
                    if (StatusIndicator != null) StatusIndicator.Text = "Collapsed";
                }
                else
                {
                    MainContent.Visibility = Visibility.Visible;
                    if (ToggleIcon != null) ToggleIcon.Text = "🔽";
                    if (ToggleText != null) ToggleText.Text = "Collapse";
                    if (StatusIndicator != null) StatusIndicator.Text = "Ready";
                }
            }
        }

        #endregion

        #region UI Creation

        /// <summary>
        /// Create the measurement selection UI dynamically
        /// </summary>
        private void CreateMeasurementSelectionUI()
        {
            if (MeasurementSelectionPanel == null) return;

            measurementCheckBoxes.Clear();
            MeasurementSelectionPanel.Children.Clear();

            var categories = MeasurementSettings.GetParametersByCategory();

            foreach (var category in categories)
            {
                // Create category expander
                var expander = new Expander
                {
                    Header = category.Key,
                    IsExpanded = true,
                    Margin = new Thickness(2),
                    FontWeight = FontWeights.Bold
                };

                var stackPanel = new StackPanel();

                foreach (var parameter in category.Value)
                {
                    var checkBox = new CheckBox
                    {
                        Content = $"{parameter.DisplayName} ({parameter.Unit})",
                        Tag = parameter.Key,
                        Margin = new Thickness(5, 2),
                        FontWeight = FontWeights.Normal,
                        ToolTip = parameter.Description
                    };

                    checkBox.Checked += MeasurementCheckBox_Checked;
                    checkBox.Unchecked += MeasurementCheckBox_Unchecked;

                    measurementCheckBoxes[parameter.Key] = checkBox;
                    stackPanel.Children.Add(checkBox);
                }

                expander.Content = stackPanel;
                MeasurementSelectionPanel.Children.Add(expander);
            }
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// Update UI from current settings
        /// </summary>
        private void UpdateUIFromSettings()
        {
            if (controller?.Settings == null) return;

            UpdateSourceChannelSelection();
            UpdateMeasurementSelectionFromSettings();
            UpdateThresholdValues();
            UpdateStatisticsSettings();
            UpdateCurrentValuesDisplay();
            UpdateStatisticsDisplay();
        }

        /// <summary>
        /// Update source channel selection
        /// </summary>
        private void UpdateSourceChannelSelection()
        {
            if (SourceChannelCombo?.Items != null)
            {
                foreach (ComboBoxItem item in SourceChannelCombo.Items)
                {
                    if (item.Tag?.ToString() == controller.Settings.AutoMeasureSource)
                    {
                        SourceChannelCombo.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Update measurement checkboxes from settings
        /// </summary>
        private void UpdateMeasurementSelectionFromSettings()
        {
            if (controller?.Settings?.EnabledMeasurements == null) return;

            isInitialized = false;

            foreach (var kvp in measurementCheckBoxes)
            {
                kvp.Value.IsChecked = controller.Settings.EnabledMeasurements.Contains(kvp.Key);
            }

            isInitialized = true;
        }

        /// <summary>
        /// Update threshold values in UI
        /// </summary>
        private void UpdateThresholdValues()
        {
            if (controller?.Settings == null) return;

            if (ThresholdMaxTextBox != null)
                ThresholdMaxTextBox.Text = controller.Settings.ThresholdMax.ToString("F1");

            if (ThresholdMidTextBox != null)
                ThresholdMidTextBox.Text = controller.Settings.ThresholdMid.ToString("F1");

            if (ThresholdMinTextBox != null)
                ThresholdMinTextBox.Text = controller.Settings.ThresholdMin.ToString("F1");

            if (PulseSetupBTextBox != null)
                PulseSetupBTextBox.Text = controller.Settings.PulseSetupB.ToString("F1");

            if (DelaySetupATextBox != null)
                DelaySetupATextBox.Text = controller.Settings.DelaySetupA.ToString("F1");

            if (DelaySetupBTextBox != null)
                DelaySetupBTextBox.Text = controller.Settings.DelaySetupB.ToString("F1");
        }

        /// <summary>
        /// Update statistics settings in UI
        /// </summary>
        private void UpdateStatisticsSettings()
        {
            if (controller?.Settings == null) return;

            if (StatisticsDisplayCheckBox != null)
                StatisticsDisplayCheckBox.IsChecked = controller.Settings.StatisticDisplayEnabled;

            if (StatisticsModeCombo?.Items != null)
            {
                foreach (ComboBoxItem item in StatisticsModeCombo.Items)
                {
                    if (item.Tag?.ToString() == controller.Settings.StatisticMode)
                    {
                        StatisticsModeCombo.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Update no measurements visibility
        /// </summary>
        private void UpdateNoMeasurementsVisibility()
        {
            bool hasMeasurements = controller?.Settings?.EnabledMeasurements?.Any() == true;

            if (NoMeasurementsText != null)
                NoMeasurementsText.Visibility = hasMeasurements ? Visibility.Collapsed : Visibility.Visible;

            if (NoStatisticsText != null)
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
            if (valueDisplays.ContainsKey(measurementKey) || CurrentValuesPanel == null) return;

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
            if (valueDisplays.ContainsKey(measurementKey) && CurrentValuesPanel != null)
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
            if (CurrentValuesPanel == null) return;

            // Clear existing displays
            CurrentValuesPanel.Children.Clear();
            valueDisplays.Clear();

            // Add "No measurements" text back if exists
            if (NoMeasurementsText != null)
                CurrentValuesPanel.Children.Add(NoMeasurementsText);

            // Create displays for enabled measurements
            if (controller?.Settings?.EnabledMeasurements != null)
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
            if (statisticsDisplays.ContainsKey(measurementKey) || StatisticsPanel == null) return;

            var parameters = MeasurementSettings.GetAvailableParameters();
            if (!parameters.ContainsKey(measurementKey)) return;

            var parameter = parameters[measurementKey];

            var expander = new Expander
            {
                Header = parameter.DisplayName,
                Margin = new Thickness(2),
                Background = Brushes.AliceBlue
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            // Min/Max labels and values
            var minLabel = new TextBlock { Text = "Min:", FontWeight = FontWeights.Bold };
            var maxLabel = new TextBlock { Text = "Max:", FontWeight = FontWeights.Bold };
            var avgLabel = new TextBlock { Text = "Avg:", FontWeight = FontWeights.Bold };
            var stdLabel = new TextBlock { Text = "Std:", FontWeight = FontWeights.Bold };

            var minValue = new TextBlock { Text = "---", FontFamily = new FontFamily("Consolas") };
            var maxValue = new TextBlock { Text = "---", FontFamily = new FontFamily("Consolas") };
            var avgValue = new TextBlock { Text = "---", FontFamily = new FontFamily("Consolas") };
            var stdValue = new TextBlock { Text = "---", FontFamily = new FontFamily("Consolas") };

            Grid.SetRow(minLabel, 0); Grid.SetColumn(minLabel, 0);
            Grid.SetRow(minValue, 0); Grid.SetColumn(minValue, 1);
            Grid.SetRow(maxLabel, 1); Grid.SetColumn(maxLabel, 0);
            Grid.SetRow(maxValue, 1); Grid.SetColumn(maxValue, 1);

            grid.Children.Add(minLabel);
            grid.Children.Add(minValue);
            grid.Children.Add(maxLabel);
            grid.Children.Add(maxValue);

            expander.Content = grid;
            expander.Tag = measurementKey;

            var border = new Border
            {
                Child = expander,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(2)
            };

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
            var expander = border.Child as Expander;
            var grid = expander?.Content as Grid;

            if (grid?.Children != null && grid.Children.Count >= 4)
            {
                var parameters = MeasurementSettings.GetAvailableParameters();
                if (parameters.ContainsKey(measurementKey))
                {
                    var parameter = parameters[measurementKey];

                    (grid.Children[1] as TextBlock).Text = FormatMeasurementValue(stats.Minimum, parameter.Unit);
                    (grid.Children[3] as TextBlock).Text = FormatMeasurementValue(stats.Maximum, parameter.Unit);
                }
            }
        }

        /// <summary>
        /// Remove statistics display for a measurement
        /// </summary>
        private void RemoveStatisticsDisplay(string measurementKey)
        {
            if (statisticsDisplays.ContainsKey(measurementKey) && StatisticsPanel != null)
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
            if (StatisticsPanel == null) return;

            // Clear existing displays
            StatisticsPanel.Children.Clear();
            statisticsDisplays.Clear();

            // Add "No statistics" text back if exists
            if (NoStatisticsText != null)
                StatisticsPanel.Children.Add(NoStatisticsText);

            // Create displays for enabled measurements if statistics are enabled
            if (controller?.Settings?.EnabledMeasurements != null &&
                controller.Settings.StatisticDisplayEnabled)
            {
                foreach (var measurementKey in controller.Settings.EnabledMeasurements)
                {
                    CreateStatisticsDisplay(measurementKey);
                }
            }

            UpdateNoMeasurementsVisibility();
        }

        #endregion

        #region Event Handlers - Threshold Setup

        private void ThresholdMax_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && controller != null)
            {
                if (double.TryParse(textBox.Text, out double value) && value >= 0 && value <= 100)
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
            var textBox = sender as TextBox;
            if (textBox != null && controller != null)
            {
                if (double.TryParse(textBox.Text, out double value) && value >= 0 && value <= 100)
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
            var textBox = sender as TextBox;
            if (textBox != null && controller != null)
            {
                if (double.TryParse(textBox.Text, out double value) && value >= 0 && value <= 100)
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
            var textBox = sender as TextBox;
            if (textBox != null && controller != null)
            {
                if (double.TryParse(textBox.Text, out double value) && value >= 0 && value <= 100)
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
            var textBox = sender as TextBox;
            if (textBox != null && controller != null)
            {
                if (double.TryParse(textBox.Text, out double value) && value >= 0 && value <= 100)
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
            var textBox = sender as TextBox;
            if (textBox != null && controller != null)
            {
                if (double.TryParse(textBox.Text, out double value) && value >= 0 && value <= 100)
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

        #region Event Handlers - Settings

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
            UpdateStatisticsDisplay();
        }

        private void StatisticsDisplay_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isInitialized || controller == null) return;
            controller.SetStatisticDisplay(false);
            UpdateStatisticsDisplay();
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

        #region Helper Methods

        /// <summary>
        /// Format measurement value for display
        /// </summary>
        private string FormatMeasurementValue(double value, string unit)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "---";

            // Format based on unit type
            switch (unit.ToLower())
            {
                case "v":
                case "voltage":
                    return $"{value:F3} V";
                case "s":
                case "time":
                    if (Math.Abs(value) >= 1.0)
                        return $"{value:F6} s";
                    else if (Math.Abs(value) >= 1e-3)
                        return $"{value * 1e3:F3} ms";
                    else if (Math.Abs(value) >= 1e-6)
                        return $"{value * 1e6:F3} μs";
                    else
                        return $"{value * 1e9:F3} ns";
                case "hz":
                case "frequency":
                    if (Math.Abs(value) >= 1e6)
                        return $"{value / 1e6:F3} MHz";
                    else if (Math.Abs(value) >= 1e3)
                        return $"{value / 1e3:F3} kHz";
                    else
                        return $"{value:F3} Hz";
                case "percentage":
                case "%":
                    return $"{value:F1}%";
                case "v·s":
                    return $"{value:E3} V·s";
                case "v²":
                    return $"{value:E3} V²";
                default:
                    return $"{value:F3} {unit}";
            }
        }

        /// <summary>
        /// Update all measurement values
        /// </summary>
        private void UpdateAllMeasurementValues()
        {
            if (controller?.Settings?.EnabledMeasurements == null) return;

            foreach (var measurementKey in controller.Settings.EnabledMeasurements)
            {
                var value = controller.QueryMeasurementValue(measurementKey);
                if (value.HasValue)
                {
                    UpdateValueDisplay(measurementKey, value.Value);
                }
            }
        }

        /// <summary>
        /// Update all measurement statistics
        /// </summary>
        private void UpdateAllMeasurementStatistics()
        {
            if (controller?.Settings?.EnabledMeasurements == null ||
                !controller.Settings.StatisticDisplayEnabled) return;

            foreach (var measurementKey in controller.Settings.EnabledMeasurements)
            {
                var stats = controller.QueryMeasurementStatistics(measurementKey);
                if (stats != null)
                {
                    UpdateStatisticsDisplay(measurementKey, stats);
                }
            }
        }

        /// <summary>
        /// Refresh enabled measurements from controller
        /// </summary>
        private void RefreshEnabledMeasurements()
        {
            if (controller == null) return;

            // Update all measurement values for the new source
            UpdateAllMeasurementValues();
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Cleanup()
        {
            autoUpdateTimer?.Stop();

            if (controller != null)
            {
                controller.MeasurementValueUpdated -= Controller_MeasurementValueUpdated;
                controller.MeasurementStatisticsUpdated -= Controller_MeasurementStatisticsUpdated;
                controller.LogEvent -= Controller_LogEvent;
            }
        }

        #endregion
    }
}