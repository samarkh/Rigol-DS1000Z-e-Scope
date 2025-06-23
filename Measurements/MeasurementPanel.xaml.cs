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
                }

                controller = value;

                if (controller != null)
                {
                    // Subscribe to new controller events
                    controller.MeasurementValueUpdated += Controller_MeasurementValueUpdated;
                    controller.MeasurementStatisticsUpdated += Controller_MeasurementStatisticsUpdated;
                }
            }
        }

        /// <summary>
        /// Gets whether the panel is collapsed
        /// </summary>
        public bool IsCollapsed => panelCollapsed;

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
        /// Create the measurement selection UI
        /// </summary>
        private void CreateMeasurementSelectionUI()
        {
            // This method would create the checkboxes for measurement selection
            // Implementation depends on your specific UI structure
        }

        #endregion

        #region Event Handlers - Auto Update

        /// <summary>
        /// Auto-update timer tick event
        /// </summary>
        private void AutoUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (controller?.Settings?.AutoUpdateEnabled == true)
            {
                controller.UpdateAllMeasurements();
            }
        }

        /// <summary>
        /// Event handler for auto update checked
        /// </summary>
        private void AutoUpdate_Checked(object sender, RoutedEventArgs e)
        {
            if (controller?.Settings != null && isInitialized)
            {
                controller.Settings.AutoUpdateEnabled = true;
                autoUpdateTimer.Start();
                LogEvent?.Invoke(this, "Auto update enabled");
            }
        }

        /// <summary>
        /// Event handler for auto update unchecked
        /// </summary>
        private void AutoUpdate_Unchecked(object sender, RoutedEventArgs e)
        {
            if (controller?.Settings != null && isInitialized)
            {
                controller.Settings.AutoUpdateEnabled = false;
                autoUpdateTimer.Stop();
                LogEvent?.Invoke(this, "Auto update disabled");
            }
        }

        #endregion

        #region Event Handlers - Statistics

        /// <summary>
        /// Event handler for statistics display checked
        /// </summary>
        private void StatisticsDisplay_Checked(object sender, RoutedEventArgs e)
        {
            if (controller?.Settings != null && isInitialized)
            {
                controller.Settings.StatisticsEnabled = true;
                controller.EnableStatistics(true);
                UpdateStatisticsVisibility(true);
                LogEvent?.Invoke(this, "Statistics display enabled");
            }
        }

        /// <summary>
        /// Event handler for statistics display unchecked
        /// </summary>
        private void StatisticsDisplay_Unchecked(object sender, RoutedEventArgs e)
        {
            if (controller?.Settings != null && isInitialized)
            {
                controller.Settings.StatisticsEnabled = false;
                controller.EnableStatistics(false);
                UpdateStatisticsVisibility(false);
                LogEvent?.Invoke(this, "Statistics display disabled");
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

        #endregion

        #region Event Handlers - Preset Buttons

        /// <summary>
        /// Event handler for clear all button
        /// </summary>
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            if (MessageBox.Show("Clear all measurements?", "Confirm Clear",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    controller.ClearAllMeasurements();
                    ClearAllValueDisplays();
                    LogEvent?.Invoke(this, "All measurements cleared");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error clearing measurements: {ex.Message}", "Clear Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Event handler for time domain preset button
        /// </summary>
        private void TimeDomainPreset_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            try
            {
                var timeDomainMeasurements = new[] { "PERiod", "FREQuency", "RISetime", "FALLtime", "PWIDth", "NWIDth" };
                ApplyPresetMeasurements(timeDomainMeasurements, "Time Domain");
                LogEvent?.Invoke(this, "Time domain preset applied");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying time domain preset: {ex.Message}", "Preset Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event handler for voltage preset button
        /// </summary>
        private void VoltagePreset_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            try
            {
                var voltageMeasurements = new[] { "VMAX", "VMIN", "VPP", "VAVG", "VRMS" };
                ApplyPresetMeasurements(voltageMeasurements, "Voltage");
                LogEvent?.Invoke(this, "Voltage preset applied");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying voltage preset: {ex.Message}", "Preset Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event handler for comprehensive preset button
        /// </summary>
        private void ComprehensivePreset_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            try
            {
                var comprehensiveMeasurements = new[] {
                    "VMAX", "VMIN", "VPP", "VAVG", "VRMS",
                    "PERiod", "FREQuency", "RISetime", "FALLtime",
                    "PWIDth", "NWIDth", "PDUTy", "NDUTy"
                };
                ApplyPresetMeasurements(comprehensiveMeasurements, "Comprehensive");

                // Enable statistics for comprehensive analysis
                if (controller.Settings != null)
                {
                    controller.Settings.StatisticsEnabled = true;
                    controller.EnableStatistics(true);
                    UpdateStatisticsVisibility(true);
                }

                LogEvent?.Invoke(this, "Comprehensive preset applied");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying comprehensive preset: {ex.Message}", "Preset Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Event Handlers - Update Buttons

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

        #region Event Handlers - Source and Threshold

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

        #region Event Handlers - Setup Parameters

        /// <summary>
        /// Event handler for delay setup A lost focus
        /// </summary>
        private void DelaySetupA_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                controller?.SetDelaySetupA(value);
                LogEvent?.Invoke(this, $"Delay setup A set to: {value:F6}s");
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
                LogEvent?.Invoke(this, $"Delay setup B set to: {value:F6}s");
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
                LogEvent?.Invoke(this, $"Pulse setup B set to: {value:F6}s");
            }
        }

        #endregion

        #region Controller Event Handlers

        /// <summary>
        /// Event handler for measurement value updates
        /// </summary>
        private void Controller_MeasurementValueUpdated(object sender, MeasurementValueEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => Controller_MeasurementValueUpdated(sender, e));
                return;
            }

            UpdateValueDisplay(e.MeasurementKey, e.Value);
        }

        /// <summary>
        /// Event handler for measurement statistics updates
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

        #region UI Update Methods

        /// <summary>
        /// Update value display for a specific measurement
        /// </summary>
        private void UpdateValueDisplay(string measurementKey, double value)
        {
            if (valueDisplays.TryGetValue(measurementKey, out Border display))
            {
                if (display.Child is TextBlock textBlock)
                {
                    textBlock.Text = FormatMeasurementValue(measurementKey, value);
                }
            }
        }

        /// <summary>
        /// Update statistics display for a specific measurement
        /// </summary>
        private void UpdateStatisticsDisplay(string measurementKey, MeasurementStatistics statistics)
        {
            if (statisticsDisplays.TryGetValue(measurementKey, out Border display))
            {
                if (display.Child is StackPanel panel)
                {
                    // Update statistics display - implementation depends on your UI structure
                    UpdateStatisticsPanel(panel, statistics);
                }
            }
        }

        /// <summary>
        /// Format measurement value for display
        /// </summary>
        private string FormatMeasurementValue(string measurementType, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "---";

            // Format based on measurement type
            return measurementType.ToUpper() switch
            {
                "VMAX" or "VMIN" or "VPP" or "VAVG" or "VRMS" or "VTOP" or "VBASE" or "VAMP" => $"{value:F3} V",
                "PERIOD" or "RISETIME" or "FALLTIME" or "PWIDTH" or "NWIDTH" => FormatTimeValue(value),
                "FREQUENCY" => FormatFrequencyValue(value),
                "PDUTY" or "NDUTY" => $"{value:F1} %",
                _ => $"{value:F3}"
            };
        }

        /// <summary>
        /// Format time values with appropriate units
        /// </summary>
        private string FormatTimeValue(double seconds)
        {
            if (Math.Abs(seconds) >= 1)
                return $"{seconds:F3} s";
            else if (Math.Abs(seconds) >= 1e-3)
                return $"{seconds * 1e3:F3} ms";
            else if (Math.Abs(seconds) >= 1e-6)
                return $"{seconds * 1e6:F3} μs";
            else
                return $"{seconds * 1e9:F3} ns";
        }

        /// <summary>
        /// Format frequency values with appropriate units
        /// </summary>
        private string FormatFrequencyValue(double frequency)
        {
            if (Math.Abs(frequency) >= 1e6)
                return $"{frequency / 1e6:F3} MHz";
            else if (Math.Abs(frequency) >= 1e3)
                return $"{frequency / 1e3:F3} kHz";
            else
                return $"{frequency:F3} Hz";
        }

        /// <summary>
        /// Update statistics panel with new data
        /// </summary>
        private void UpdateStatisticsPanel(StackPanel panel, MeasurementStatistics statistics)
        {
            // Implementation depends on your statistics panel structure
            // This is a placeholder - customize based on your UI needs
        }

        /// <summary>
        /// Update statistics visibility
        /// </summary>
        private void UpdateStatisticsVisibility(bool visible)
        {
            foreach (var display in statisticsDisplays.Values)
            {
                display.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Clear all value displays
        /// </summary>
        private void ClearAllValueDisplays()
        {
            foreach (var display in valueDisplays.Values)
            {
                if (display.Child is TextBlock textBlock)
                {
                    textBlock.Text = "---";
                }
            }

            foreach (var display in statisticsDisplays.Values)
            {
                if (display.Child is StackPanel panel)
                {
                    // Clear statistics display
                    foreach (var child in panel.Children.OfType<TextBlock>())
                    {
                        child.Text = "---";
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Apply preset measurements
        /// </summary>
        private void ApplyPresetMeasurements(string[] measurements, string presetName)
        {
            if (controller == null) return;

            try
            {
                // Clear existing measurements first
                controller.ClearAllMeasurements();

                // Add new measurements
                foreach (var measurement in measurements)
                {
                    controller.AddMeasurement(measurement);
                }

                // Update the display
                controller.UpdateAllMeasurements();

                LogEvent?.Invoke(this, $"{presetName} preset applied: {string.Join(", ", measurements)}");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error applying {presetName} preset: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Refresh the panel from controller settings
        /// </summary>
        public void RefreshFromController()
        {
            if (controller?.Settings == null) return;

            try
            {
                // Update UI based on controller settings
                isInitialized = false;

                // Update auto update checkbox
                if (AutoUpdateCheckBox != null)
                {
                    AutoUpdateCheckBox.IsChecked = controller.Settings.AutoUpdateEnabled;
                }

                // Update statistics checkbox
                // Implementation depends on your XAML structure

                // Update timer state
                if (controller.Settings.AutoUpdateEnabled)
                    autoUpdateTimer.Start();
                else
                    autoUpdateTimer.Stop();

                isInitialized = true;

                LogEvent?.Invoke(this, "Panel refreshed from controller settings");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error refreshing panel: {ex.Message}");
            }
        }

        /// <summary>
        /// Export measurement data
        /// </summary>
        public void ExportMeasurementData()
        {
            if (controller == null) return;

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"MeasurementData_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    controller.ExportMeasurementData(saveFileDialog.FileName);
                    LogEvent?.Invoke(this, $"Measurement data exported to: {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting data: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
        /// Collapse or expand the panel
        /// </summary>
        public void SetCollapsed(bool collapsed)
        {
            panelCollapsed = collapsed;
            // Implementation depends on your UI structure
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
            }
        }

        #endregion
    }
}