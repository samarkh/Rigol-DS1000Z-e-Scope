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

            // Initialize collections
            measurementCheckBoxes = new Dictionary<string, CheckBox>();
            valueDisplays = new Dictionary<string, Border>();
            statisticsDisplays = new Dictionary<string, Border>();

            // Initialize components
            InitializeAutoUpdateTimer();
            CreateMeasurementSelectionUI();

            isInitialized = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the measurement controller
        /// </summary>
        public MeasurementController Controller
        {
            get => controller;
            set
            {
                if (controller != null)
                {
                    // Unsubscribe from old controller events
                    controller.MeasurementValueUpdated -= Controller_MeasurementValueUpdated;
                    controller.MeasurementStatisticsUpdated -= Controller_MeasurementStatisticsUpdated;
                    controller.LogEvent -= Controller_LogEvent;
                }

                controller = value;

                if (controller != null)
                {
                    // Subscribe to new controller events
                    controller.MeasurementValueUpdated += Controller_MeasurementValueUpdated;
                    controller.MeasurementStatisticsUpdated += Controller_MeasurementStatisticsUpdated;
                    controller.LogEvent += Controller_LogEvent;

                    // Refresh UI with new controller
                    RefreshFromController();
                }
            }
        }

        /// <summary>
        /// Gets whether the panel is collapsed
        /// </summary>
        public bool IsCollapsed => panelCollapsed;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the auto-update timer
        /// </summary>
        private void InitializeAutoUpdateTimer()
        {
            autoUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2000) // Update every 2 seconds
            };
            autoUpdateTimer.Tick += AutoUpdateTimer_Tick;
        }

        /// <summary>
        /// Create the measurement selection UI dynamically
        /// </summary>
        private void CreateMeasurementSelectionUI()
        {
            try
            {
                var categories = MeasurementSettings.GetParametersByCategory();

                // Find the panels in the XAML
                var timeDomainPanel = FindName("TimeDomainPanel") as StackPanel;
                var voltagePanel = FindName("VoltagePanel") as StackPanel;

                if (timeDomainPanel != null && categories.ContainsKey("Time Domain"))
                {
                    PopulateCategory(timeDomainPanel, categories["Time Domain"]);
                }

                if (voltagePanel != null && categories.ContainsKey("Voltage Levels"))
                {
                    PopulateCategory(voltagePanel, categories["Voltage Levels"]);
                }
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error creating measurement UI: {ex.Message}");
            }
        }

        /// <summary>
        /// Populate a category panel with measurement checkboxes
        /// </summary>
        private void PopulateCategory(StackPanel panel, List<MeasurementParameter> parameters)
        {
            panel.Children.Clear();

            foreach (var param in parameters)
            {
                var checkBox = new CheckBox
                {
                    Content = param.DisplayName,
                    Tag = param.Key,
                    Margin = new Thickness(2),
                    VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = $"{param.Description} ({param.Unit})"
                };

                checkBox.Checked += MeasurementCheckBox_Checked;
                checkBox.Unchecked += MeasurementCheckBox_Unchecked;

                panel.Children.Add(checkBox);
                measurementCheckBoxes[param.Key] = checkBox;
            }
        }

        #endregion

        #region Controller Event Handlers

        /// <summary>
        /// Handle controller log events
        /// </summary>
        private void Controller_LogEvent(object sender, string message)
        {
            LogEvent?.Invoke(this, message);
        }

        /// <summary>
        /// Event handler for measurement value updates - FIXED SIGNATURE
        /// </summary>
        private void Controller_MeasurementValueUpdated(object sender, MeasurementValueEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => Controller_MeasurementValueUpdated(sender, e));
                return;
            }

            UpdateValueDisplay(e.MeasurementKey, e.Value);
            UpdateLastUpdateTime();
        }

        /// <summary>
        /// Event handler for measurement statistics updates - FIXED SIGNATURE
        /// </summary>
        private void Controller_MeasurementStatisticsUpdated(object sender, MeasurementStatisticsEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => Controller_MeasurementStatisticsUpdated(sender, e));
                return;
            }

            UpdateStatisticsDisplay(e.MeasurementKey, e.Statistics);
        }

        #endregion

        #region Auto Update Event Handlers

        /// <summary>
        /// Auto-update timer tick event
        /// </summary>
        private void AutoUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (controller?.Settings?.AutoUpdateEnabled == true)
            {
                try
                {
                    controller.UpdateAllMeasurements();
                    if (controller.Settings.StatisticsEnabled)
                    {
                        controller.UpdateStatistics();
                    }
                }
                catch (Exception ex)
                {
                    LogEvent?.Invoke(this, $"Auto-update error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Event handler for auto update checked
        /// </summary>
        private void AutoUpdate_Checked(object sender, RoutedEventArgs e)
        {
            if (controller == null || !isInitialized) return;

            try
            {
                controller.Settings.AutoUpdateEnabled = true;
                autoUpdateTimer.Start();
                LogEvent?.Invoke(this, "Auto update enabled");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error enabling auto update: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for auto update unchecked
        /// </summary>
        private void AutoUpdate_Unchecked(object sender, RoutedEventArgs e)
        {
            if (controller == null || !isInitialized) return;

            try
            {
                controller.Settings.AutoUpdateEnabled = false;
                autoUpdateTimer.Stop();
                LogEvent?.Invoke(this, "Auto update disabled");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error disabling auto update: {ex.Message}");
            }
        }

        #endregion

        #region Auto Display Event Handlers

        /// <summary>
        /// Event handler for auto display checked - MISSING METHOD
        /// </summary>
        private void AutoDisplay_Checked(object sender, RoutedEventArgs e)
        {
            if (controller == null || !isInitialized) return;

            try
            {
                controller.SetAutoDisplay(true);
                LogEvent?.Invoke(this, "Auto display enabled");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error enabling auto display: {ex.Message}", "Auto Display Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event handler for auto display unchecked - MISSING METHOD
        /// </summary>
        private void AutoDisplay_Unchecked(object sender, RoutedEventArgs e)
        {
            if (controller == null || !isInitialized) return;

            try
            {
                controller.SetAutoDisplay(false);
                LogEvent?.Invoke(this, "Auto display disabled");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disabling auto display: {ex.Message}", "Auto Display Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Statistics Event Handlers

        /// <summary>
        /// Event handler for statistics display checked
        /// </summary>
        private void StatisticsDisplay_Checked(object sender, RoutedEventArgs e)
        {
            if (controller == null || !isInitialized) return;

            try
            {
                controller.Settings.StatisticDisplayEnabled = true;
                controller.EnableStatistics(true);
                UpdateStatisticsVisibility(true);
                LogEvent?.Invoke(this, "Statistics display enabled");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error enabling statistics: {ex.Message}", "Statistics Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event handler for statistics display unchecked
        /// </summary>
        private void StatisticsDisplay_Unchecked(object sender, RoutedEventArgs e)
        {
            if (controller == null || !isInitialized) return;

            try
            {
                controller.Settings.StatisticDisplayEnabled = false;
                controller.EnableStatistics(false);
                UpdateStatisticsVisibility(false);
                LogEvent?.Invoke(this, "Statistics display disabled");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disabling statistics: {ex.Message}", "Statistics Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event handler for statistics mode selection changed
        /// </summary>
        private void StatisticsMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (controller == null || !isInitialized) return;

            var comboBox = sender as ComboBox;
            var selectedItem = comboBox?.SelectedItem as ComboBoxItem;
            var mode = selectedItem?.Tag?.ToString();

            if (!string.IsNullOrEmpty(mode))
            {
                controller.SetStatisticsMode(mode);
                LogEvent?.Invoke(this, $"Statistics mode changed to: {mode}");
            }
        }

        /// <summary>
        /// Event handler for reset statistics button
        /// </summary>
        private void ResetStatistics_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            try
            {
                controller.ResetStatistics();
                LogEvent?.Invoke(this, "Measurement statistics reset");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting statistics: {ex.Message}", "Reset Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event handler for export statistics button - MISSING METHOD
        /// </summary>
        private void ExportStatistics_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null)
            {
                MessageBox.Show("No controller available for export.", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"MeasurementStatistics_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    if (controller.ExportMeasurementData(saveDialog.FileName))
                    {
                        LogEvent?.Invoke(this, $"Statistics exported to: {saveDialog.FileName}");
                        MessageBox.Show($"Statistics exported successfully to:\n{saveDialog.FileName}",
                            "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to export statistics.", "Export Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting statistics: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Update Event Handlers

        /// <summary>
        /// Event handler for update values button
        /// </summary>
        private void UpdateValues_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            try
            {
                controller.UpdateAllMeasurements();
                LogEvent?.Invoke(this, "Measurement values updated");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating values: {ex.Message}", "Update Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event handler for update statistics button
        /// </summary>
        private void UpdateStatistics_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            try
            {
                controller.UpdateStatistics();
                LogEvent?.Invoke(this, "Measurement statistics updated");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating statistics: {ex.Message}", "Update Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Source Channel Event Handlers

        /// <summary>
        /// Event handler for source channel selection changed
        /// </summary>
        private void SourceChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (controller == null || !isInitialized) return;

            var comboBox = sender as ComboBox;
            var selectedItem = comboBox?.SelectedItem as ComboBoxItem;
            var channel = selectedItem?.Tag?.ToString();

            if (!string.IsNullOrEmpty(channel))
            {
                controller.SetSourceChannel(channel);
                LogEvent?.Invoke(this, $"Source channel changed to: {channel}");
            }
        }

        #endregion

        #region Threshold Setup Event Handlers

        /// <summary>
        /// Event handler for threshold max lost focus
        /// </summary>
        private void ThresholdMax_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                controller?.SetThresholdMax(value);
                LogEvent?.Invoke(this, $"Threshold max set to: {value:F3}V");
            }
        }

        /// <summary>
        /// Event handler for threshold mid lost focus
        /// </summary>
        private void ThresholdMid_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                controller?.SetThresholdMid(value);
                LogEvent?.Invoke(this, $"Threshold mid set to: {value:F3}V");
            }
        }

        /// <summary>
        /// Event handler for threshold min lost focus
        /// </summary>
        private void ThresholdMin_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                controller?.SetThresholdMin(value);
                LogEvent?.Invoke(this, $"Threshold min set to: {value:F3}V");
            }
        }

        #endregion

        #region Delay and Pulse Setup Event Handlers

        /// <summary>
        /// Event handler for delay setup A lost focus
        /// </summary>
        private void DelaySetupA_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                controller?.SetDelaySetupA(value);
                LogEvent?.Invoke(this, $"Delay setup A set to: {value:F1}%");
            }
        }

        /// <summary>
        /// Event handler for delay setup B lost focus
        /// </summary>
        private void DelaySetupB_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                controller?.SetDelaySetupB(value);
                LogEvent?.Invoke(this, $"Delay setup B set to: {value:F1}%");
            }
        }

        /// <summary>
        /// Event handler for pulse setup B lost focus
        /// </summary>
        private void PulseSetupB_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                controller?.SetPulseSetupB(value);
                LogEvent?.Invoke(this, $"Pulse setup B set to: {value:F1}%");
            }
        }

        #endregion

        #region Preset Event Handlers

        /// <summary>
        /// Event handler for time domain preset button
        /// </summary>
        private void TimeDomainPreset_Click(object sender, RoutedEventArgs e)
        {
            ApplyQuickPreset("TimeDomain");
        }

        /// <summary>
        /// Event handler for voltage preset button
        /// </summary>
        private void VoltagePreset_Click(object sender, RoutedEventArgs e)
        {
            ApplyQuickPreset("Voltage");
        }

        /// <summary>
        /// Event handler for comprehensive preset button
        /// </summary>
        private void ComprehensivePreset_Click(object sender, RoutedEventArgs e)
        {
            ApplyQuickPreset("Comprehensive");
        }

        /// <summary>
        /// Event handler for clear all button
        /// </summary>
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            if (MessageBox.Show("Clear all measurements?", "Clear All Measurements",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    controller.ClearAllMeasurements();
                    UpdateMeasurementCheckboxes();
                    LogEvent?.Invoke(this, "All measurements cleared");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error clearing measurements: {ex.Message}", "Clear Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// Event handler for toggle button - MISSING METHOD
        /// </summary>
        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                panelCollapsed = !panelCollapsed;

                // Toggle panel visibility/collapse state
                if (MainContent != null)
                {
                    MainContent.Visibility = panelCollapsed ? Visibility.Collapsed : Visibility.Visible;
                }

                // Update toggle button text and icon
                if (ToggleIcon != null && ToggleText != null)
                {
                    ToggleIcon.Text = panelCollapsed ? "🔼" : "🔽";
                    ToggleText.Text = panelCollapsed ? "Expand" : "Collapse";
                }

                LogEvent?.Invoke(this, $"Measurement panel {(panelCollapsed ? "collapsed" : "expanded")}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling panel: {ex.Message}", "Toggle Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event handler for measurement checkbox checked
        /// </summary>
        private void MeasurementCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var measurementKey = checkBox?.Tag?.ToString();

            if (!string.IsNullOrEmpty(measurementKey) && controller != null)
            {
                controller.AddMeasurement(measurementKey);
                LogEvent?.Invoke(this, $"Measurement enabled: {measurementKey}");
            }
        }

        /// <summary>
        /// Event handler for measurement checkbox unchecked
        /// </summary>
        private void MeasurementCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var measurementKey = checkBox?.Tag?.ToString();

            if (!string.IsNullOrEmpty(measurementKey) && controller != null)
            {
                controller.Settings.DisableMeasurement(measurementKey);
                LogEvent?.Invoke(this, $"Measurement disabled: {measurementKey}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Apply a quick setup preset
        /// </summary>
        private void ApplyQuickPreset(string presetName)
        {
            if (controller == null) return;

            try
            {
                if (controller.ApplyQuickSetupPreset(presetName))
                {
                    UpdateMeasurementCheckboxes();
                    LogEvent?.Invoke(this, $"Applied {presetName} preset");

                    var presets = controller.GetAvailablePresets();
                    var description = presets.ContainsKey(presetName) ? presets[presetName] : "Preset applied";

                    MessageBox.Show($"Quick Setup Complete!\n\nApplied: {presetName}\n\n{description}",
                                  "Setup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to apply {presetName} preset",
                                  "Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying {presetName} preset: {ex.Message}", "Preset Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Update measurement checkboxes based on enabled measurements
        /// </summary>
        private void UpdateMeasurementCheckboxes()
        {
            if (controller?.Settings == null) return;

            foreach (var kvp in measurementCheckBoxes)
            {
                kvp.Value.IsChecked = controller.Settings.IsMeasurementEnabled(kvp.Key);
            }
        }

        /// <summary>
        /// Update value display for a specific measurement
        /// </summary>
        private void UpdateValueDisplay(string measurementKey, double value)
        {
            // Update the measurement values panel
            var valuesPanel = FindName("MeasurementValuesPanel") as StackPanel;
            if (valuesPanel != null)
            {
                // Find or create display for this measurement
                UpdateOrCreateValueDisplay(valuesPanel, measurementKey, value);
            }
        }

        /// <summary>
        /// Update or create a value display in the panel
        /// </summary>
        private void UpdateOrCreateValueDisplay(StackPanel panel, string measurementKey, double value)
        {
            // Look for existing display
            var existingDisplay = panel.Children.OfType<Border>()
                .FirstOrDefault(b => b.Tag?.ToString() == measurementKey);

            if (existingDisplay != null)
            {
                // Update existing display
                var textBlock = existingDisplay.Child as TextBlock;
                if (textBlock != null)
                {
                    textBlock.Text = $"{measurementKey}: {FormatMeasurementValue(measurementKey, value)}";
                }
            }
            else
            {
                // Create new display
                var border = new Border
                {
                    Tag = measurementKey,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(2),
                    Padding = new Thickness(5),
                    Child = new TextBlock
                    {
                        Text = $"{measurementKey}: {FormatMeasurementValue(measurementKey, value)}",
                        FontFamily = new FontFamily("Consolas")
                    }
                };

                panel.Children.Add(border);
                valueDisplays[measurementKey] = border;
            }
        }

        /// <summary>
        /// Update statistics display for a specific measurement
        /// </summary>
        private void UpdateStatisticsDisplay(string measurementKey, MeasurementStatistics statistics)
        {
            // Implementation would update statistics display
            // This is a placeholder for the statistics display logic
        }

        /// <summary>
        /// Format measurement value for display
        /// </summary>
        private string FormatMeasurementValue(string measurementType, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "---";

            // Format based on measurement type
            return measurementType switch
            {
                string type when type.Contains("FREQuency") => $"{value:F3} Hz",
                string type when type.Contains("PERiod") => $"{value:E3} s",
                string type when type.StartsWith("V") => $"{value:F3} V",
                string type when type.Contains("Time") => $"{value:E3} s",
                string type when type.Contains("Duty") => $"{value:F1} %",
                _ => $"{value:F6}"
            };
        }

        /// <summary>
        /// Update statistics visibility
        /// </summary>
        private void UpdateStatisticsVisibility(bool visible)
        {
            // Implementation would show/hide statistics displays
            foreach (var display in statisticsDisplays.Values)
            {
                display.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Update the last update time display
        /// </summary>
        private void UpdateLastUpdateTime()
        {
            var timeDisplay = FindName("LastUpdateTime") as TextBlock;
            if (timeDisplay != null)
            {
                timeDisplay.Text = $"Last Update: {DateTime.Now:HH:mm:ss}";
            }
        }

        /// <summary>
        /// Refresh UI from controller settings
        /// </summary>
        private void RefreshFromController()
        {
            if (controller?.Settings == null || !isInitialized) return;

            try
            {
                // Update UI controls with controller settings
                if (AutoDisplayCheckBox != null)
                {
                    AutoDisplayCheckBox.IsChecked = controller.Settings.AutoDisplayEnabled;
                }

                if (AutoUpdateCheckBox != null)
                {
                    AutoUpdateCheckBox.IsChecked = controller.Settings.AutoUpdateEnabled;
                }

                if (StatisticsDisplayCheckBox != null)
                {
                    StatisticsDisplayCheckBox.IsChecked = controller.Settings.StatisticDisplayEnabled;
                }

                // Update measurement checkboxes
                UpdateMeasurementCheckboxes();

                // Update timer state
                if (controller.Settings.AutoUpdateEnabled)
                    autoUpdateTimer.Start();
                else
                    autoUpdateTimer.Stop();

                LogEvent?.Invoke(this, "Panel refreshed from controller settings");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error refreshing panel: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the enabled state of the panel
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.IsEnabled = enabled;
        }

        /// <summary>
        /// Export measurement data
        /// </summary>
        public void ExportMeasurementData()
        {
            ExportStatistics_Click(this, new RoutedEventArgs());
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup when the control is unloaded
        /// </summary>
        private void MeasurementPanel_Unloaded(object sender, RoutedEventArgs e)
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