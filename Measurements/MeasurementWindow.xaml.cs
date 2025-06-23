using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace DS1000Z_E_USB_Control.Measurements
{
    /// <summary>
    /// Interaction logic for MeasurementWindow.xaml
    /// </summary>
    public partial class MeasurementWindow : Window
    {
        #region Fields

        private MeasurementController controller;
        private DispatcherTimer statusUpdateTimer;
        private bool isConnected = false;

        #endregion

        #region Constructor

        public MeasurementWindow()
        {
            InitializeComponent();
            InitializeStatusTimer();
            UpdateConnectionStatus(false);
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
                    controller.LogEvent -= Controller_LogEvent;
                    controller.MeasurementValueUpdated -= Controller_MeasurementValueUpdated;
                    controller.MeasurementStatisticsUpdated -= Controller_MeasurementStatisticsUpdated;
                }

                controller = value;
                MeasurementPanel.Controller = controller;

                if (controller != null)
                {
                    controller.LogEvent += Controller_LogEvent;
                    controller.MeasurementValueUpdated += Controller_MeasurementValueUpdated;
                    controller.MeasurementStatisticsUpdated += Controller_MeasurementStatisticsUpdated;
                    UpdateConnectionStatus(true);
                }
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize status update timer
        /// </summary>
        private void InitializeStatusTimer()
        {
            statusUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            statusUpdateTimer.Tick += StatusUpdateTimer_Tick;
            statusUpdateTimer.Start();
        }

        #endregion

        #region Event Handlers - Menu Bar

        private void ResetAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Reset all measurements and settings to defaults?\n\nThis will clear all enabled measurements and reset thresholds.",
                "Reset All Measurements", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    controller?.ResetAllMeasurements();
                    LogMessage("All measurements reset to defaults");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error resetting measurements: {ex.Message}", "Reset Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void QuickSetup_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null)
            {
                MessageBox.Show("Measurement controller not available.", "Quick Setup",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Use the new separate dialog
                var selectedPreset = QuickSetupDialog.ShowDialog(this);

                if (!string.IsNullOrEmpty(selectedPreset))
                {
                    // Get measurements for the selected preset
                    var measurements = QuickSetupDialog.GetPresetMeasurements(selectedPreset);

                    if (measurements.Length > 0)
                    {
                        // Apply the preset measurements
                        ApplyPresetMeasurements(measurements, selectedPreset);

                        LogMessage($"Applied {selectedPreset} preset with {measurements.Length} measurements");

                        // Show confirmation
                        var description = QuickSetupDialog.GetPresetDescription(selectedPreset);
                        MessageBox.Show($"Quick Setup Complete!\n\nPreset: {selectedPreset}\n" +
                                      $"Measurements: {measurements.Length}\n\n{description}",
                                      "Setup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"No measurements defined for preset: {selectedPreset}",
                                      "Setup Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during quick setup: {ex.Message}", "Quick Setup Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                LogMessage($"Quick setup error: {ex.Message}");
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            if (controller?.Settings == null)
            {
                MessageBox.Show("No measurement configuration available to save.", "Save Configuration",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"MeasurementConfig_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var json = JsonSerializer.Serialize(controller.Settings, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    File.WriteAllText(saveDialog.FileName, json);

                    LogMessage($"Configuration saved to: {saveDialog.FileName}");
                    MessageBox.Show($"Configuration saved successfully to:\n{saveDialog.FileName}",
                        "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving configuration:\n{ex.Message}", "Save Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(openDialog.FileName);
                    var settings = JsonSerializer.Deserialize<MeasurementSettings>(json);

                    if (settings != null)
                    {
                        controller.ApplySettings(settings);
                        LogMessage($"Configuration loaded from: {openDialog.FileName}");
                        MessageBox.Show($"Configuration loaded successfully from:\n{openDialog.FileName}",
                            "Load Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading configuration:\n{ex.Message}", "Load Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            if (controller?.Statistics == null || !controller.Statistics.Any())
            {
                MessageBox.Show("No measurement data available to export.", "Export Data",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"MeasurementData_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    ExportStatisticsToFile(saveDialog.FileName);
                    LogMessage($"Data exported to: {saveDialog.FileName}");
                    MessageBox.Show($"Measurement data exported successfully to:\n{saveDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting data:\n{ex.Message}", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LiveChart_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement live chart functionality
            MessageBox.Show("Live chart functionality coming soon!", "Live Chart",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            var helpMessage = @"📊 MEASUREMENT WINDOW HELP

TIME DOMAIN MEASUREMENTS:
• Period/Frequency - Waveform period and frequency
• Rise/Fall Time - Signal transition times (10%-90%)
• Pulse Width - Positive and negative pulse widths
• Duty Cycle - Positive and negative duty cycles
• Edge/Pulse Count - Count of edges and pulses

VOLTAGE MEASUREMENTS:
• Vmax/Vmin - Maximum and minimum voltage levels
• Vpp - Peak-to-peak voltage
• Vtop/Vbase - Flat top and base voltage levels
• Vamp - Amplitude (Vtop - Vbase)
• Vavg/Vrms - Average and RMS voltage
• Reference Levels - Upper, middle, lower reference levels

SIGNAL QUALITY:
• Overshoot/Preshoot - Signal overshoot and preshoot percentages
• Variance - Voltage variance

MEASUREMENT SETUP:
• Threshold Levels: Define measurement reference points (MAX, MID, MIN)
• Quick Setup: Use presets for common measurement scenarios
• Statistics: Track min, max, average, and standard deviation

TIPS:
• Use Quick Setup for fast configuration
• Enable auto-update for continuous monitoring
• Configure thresholds based on your signal characteristics
• Export data for analysis in external tools";

            MessageBox.Show(helpMessage, "Measurement Help",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Event Handlers - Controller Events

        private void Controller_LogEvent(object sender, string message)
        {
            LogMessage(message);
        }

        private void Controller_MeasurementValueUpdated(object sender, (string measurement, object value) data)
        {
            // Handle measurement value updates
            // This could update UI elements or trigger other actions
        }

        private void Controller_MeasurementStatisticsUpdated(object sender, EventArgs e)
        {
            // Handle statistics updates
            // This could refresh statistics displays
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Apply preset measurements to the controller
        /// </summary>
        private void ApplyPresetMeasurements(string[] measurements, string presetName)
        {
            try
            {
                // Clear existing measurements
                controller.Settings.EnabledMeasurements.Clear();

                // Add preset measurements
                foreach (var measurement in measurements)
                {
                    controller.Settings.EnabledMeasurements.Add(measurement);
                }

                // Enable statistics for comprehensive preset
                if (presetName.ToLower() == "comprehensive")
                {
                    controller.Settings.StatisticDisplayEnabled = true;
                    controller.Settings.StatisticMode = "DIFF";
                }

                // Apply settings to the oscilloscope
                controller.ApplySettings(controller.Settings);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to apply preset measurements: {ex.Message}");
            }
        }

        /// <summary>
        /// Export statistics to file
        /// </summary>
        private void ExportStatisticsToFile(string fileName)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Measurement,Current Value,Min,Max,Average,Standard Deviation,Count");

                if (controller?.Statistics != null)
                {
                    foreach (var kvp in controller.Statistics)
                    {
                        var stats = kvp.Value;
                        var currentValue = controller.CurrentValues.ContainsKey(kvp.Key)
                            ? controller.CurrentValues[kvp.Key]?.ToString() ?? "N/A"
                            : "N/A";

                        sb.AppendLine($"{kvp.Key},{currentValue},{stats.Min},{stats.Max},{stats.Average:F3},{stats.StandardDeviation:F3},{stats.Count}");
                    }
                }

                File.WriteAllText(fileName, sb.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to export statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Update connection status display
        /// </summary>
        public void UpdateConnectionStatus(bool connected)
        {
            isConnected = connected;

            if (ConnectionIndicator != null)
            {
                ConnectionIndicator.Foreground = connected ? Brushes.Green : Brushes.Red;
            }

            if (StatusText != null)
            {
                StatusText.Text = connected ? "Connected" : "Disconnected";
            }
        }

        /// <summary>
        /// Log a message to status
        /// </summary>
        private void LogMessage(string message)
        {
            if (StatusText != null)
            {
                StatusText.Text = message;
            }

            if (TimestampText != null)
            {
                TimestampText.Text = $"Last Updated: {DateTime.Now:HH:mm:ss}";
            }
        }

        #endregion

        #region Timer Events

        /// <summary>
        /// Status update timer tick
        /// </summary>
        private void StatusUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (controller?.Settings != null)
            {
                var enabledCount = controller.Settings.EnabledMeasurements?.Count ?? 0;
                if (MeasurementCountText != null)
                {
                    MeasurementCountText.Text = enabledCount == 1 ? "1 active" : $"{enabledCount} active";
                }
            }
            else
            {
                if (MeasurementCountText != null)
                {
                    MeasurementCountText.Text = "0 active";
                }
            }
        }

        #endregion

        #region Window Events

        protected override void OnClosed(EventArgs e)
        {
            statusUpdateTimer?.Stop();

            if (controller != null)
            {
                controller.LogEvent -= Controller_LogEvent;
                controller.MeasurementValueUpdated -= Controller_MeasurementValueUpdated;
                controller.MeasurementStatisticsUpdated -= Controller_MeasurementStatisticsUpdated;
            }

            base.OnClosed(e);
        }

        #endregion
    }
}