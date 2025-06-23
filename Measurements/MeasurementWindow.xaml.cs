using Microsoft.Win32;
using System;
using System.IO;
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
                "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                controller?.ClearAllMeasurements();

                // Reset to default settings
                if (controller?.Settings != null)
                {
                    controller.Settings.ThresholdMax = 90.0;
                    controller.Settings.ThresholdMid = 50.0;
                    controller.Settings.ThresholdMin = 10.0;
                    controller.Settings.PulseSetupB = 50.0;
                    controller.Settings.DelaySetupA = 50.0;
                    controller.Settings.DelaySetupB = 50.0;
                    controller.Settings.AutoDisplayEnabled = true;
                    controller.Settings.StatisticDisplayEnabled = false;
                    controller.Settings.AutoMeasureSource = "CHANnel1";
                    controller.Settings.StatisticMode = "DIFF";

                    // Apply settings to oscilloscope
                    controller.SetAutoDisplay(true);
                    controller.SetStatisticDisplay(false);
                    controller.SetAutoMeasureSource("CHANnel1");
                    controller.SetStatisticMode("DIFF");
                    controller.SetThresholdMax(90.0);
                    controller.SetThresholdMid(50.0);
                    controller.SetThresholdMin(10.0);
                    controller.SetPulseSetupB(50.0);
                    controller.SetDelaySetupA(50.0);
                    controller.SetDelaySetupB(50.0);
                }

                LogMessage("All measurements and settings reset to defaults");
            }
        }

        private void QuickSetup_Click(object sender, RoutedEventArgs e)
        {
            var setupDialog = new QuickSetupDialog();
            if (setupDialog.ShowDialog() == true)
            {
                switch (setupDialog.SelectedPreset)
                {
                    case "TimeDomain":
                        controller?.ApplyTimeDomainPreset();
                        LogMessage("Applied Time Domain preset");
                        break;
                    case "Voltage":
                        controller?.ApplyVoltagePreset();
                        LogMessage("Applied Voltage preset");
                        break;
                    case "Comprehensive":
                        controller?.ApplyComprehensivePreset();
                        LogMessage("Applied Comprehensive preset");
                        break;
                }
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            if (controller?.Settings == null)
            {
                MessageBox.Show("No measurement configuration to save.", "Save Configuration",
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

                    if (settings != null && controller != null)
                    {
                        controller.Settings = settings;

                        // Apply loaded settings to oscilloscope
                        controller.SetAutoDisplay(settings.AutoDisplayEnabled);
                        controller.SetAutoMeasureSource(settings.AutoMeasureSource);
                        controller.SetStatisticDisplay(settings.StatisticDisplayEnabled);
                        controller.SetStatisticMode(settings.StatisticMode);
                        controller.SetThresholdMax(settings.ThresholdMax);
                        controller.SetThresholdMid(settings.ThresholdMid);
                        controller.SetThresholdMin(settings.ThresholdMin);
                        controller.SetPulseSetupB(settings.PulseSetupB);
                        controller.SetDelaySetupA(settings.DelaySetupA);
                        controller.SetDelaySetupB(settings.DelaySetupB);

                        // Re-enable measurements
                        foreach (var measurement in settings.EnabledMeasurements)
                        {
                            controller.EnableMeasurement(measurement);
                        }

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
            if (controller?.CurrentValues == null || controller.CurrentValues.Count == 0)
            {
                MessageBox.Show("No measurement data to export.", "Export Data",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var exportDialog = new ExportDataDialog(controller);
            exportDialog.ShowDialog();
        }

        private void LiveChart_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement live chart window
            MessageBox.Show("Live Chart feature coming soon!", "Live Chart",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            var helpDialog = new MeasurementHelpDialog();
            helpDialog.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Event Handlers - Controller

        private void Controller_LogEvent(object sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogMessage(message);
            });
        }

        private void Controller_MeasurementValueUpdated(object sender, MeasurementValueEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TimestampText.Text = $"Last Updated: {DateTime.Now:HH:mm:ss}";
            });
        }

        private void Controller_MeasurementStatisticsUpdated(object sender, MeasurementStatisticsEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TimestampText.Text = $"Last Updated: {DateTime.Now:HH:mm:ss}";
            });
        }

        #endregion

        #region Status Updates

        /// <summary>
        /// Update connection status
        /// </summary>
        public void UpdateConnectionStatus(bool connected)
        {
            isConnected = connected;

            if (ConnectionIndicator != null)
            {
                ConnectionIndicator.Fill = connected ? Brushes.Green : Brushes.Red;
            }

            if (StatusText != null)
            {
                StatusText.Text = connected ? "Connected" : "Disconnected";
            }
        }

        /// <summary>
        /// Status update timer tick
        /// </summary>
        private void StatusUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (controller?.Settings != null)
            {
                var enabledCount = controller.Settings.EnabledMeasurements?.Count ?? 0;
                MeasurementCountText.Text = enabledCount == 1 ? "1 active" : $"{enabledCount} active";
            }
            else
            {
                MeasurementCountText.Text = "0 active";
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Log a message to status
        /// </summary>
        private void LogMessage(string message)
        {
            StatusText.Text = message;
            TimestampText.Text = $"Last Updated: {DateTime.Now:HH:mm:ss}";
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

    #region Helper Dialog Classes

    /// <summary>
    /// Quick setup dialog for measurement presets
    /// </summary>
    public partial class QuickSetupDialog : Window
    {
        public string SelectedPreset { get; private set; }

        public QuickSetupDialog()
        {
            Title = "Quick Measurement Setup";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterParent;
            ResizeMode = ResizeMode.NoResize;

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            stackPanel.Children.Add(new TextBlock
            {
                Text = "Select a measurement preset:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            });

            var timeDomainButton = new Button
            {
                Content = "Time Domain\nFrequency, Period, Rise/Fall Time, Duty Cycle",
                Height = 50,
                Margin = new Thickness(0, 5),
                Tag = "TimeDomain"
            };
            timeDomainButton.Click += PresetButton_Click;

            var voltageButton = new Button
            {
                Content = "Voltage Analysis\nMax, Min, Peak-to-Peak, Average, RMS",
                Height = 50,
                Margin = new Thickness(0, 5),
                Tag = "Voltage"
            };
            voltageButton.Click += PresetButton_Click;

            var comprehensiveButton = new Button
            {
                Content = "Comprehensive Analysis\nAll common measurements + Statistics",
                Height = 50,
                Margin = new Thickness(0, 5),
                Tag = "Comprehensive"
            };
            comprehensiveButton.Click += PresetButton_Click;

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            cancelButton.Click += (s, e) => { DialogResult = false; };

            stackPanel.Children.Add(timeDomainButton);
            stackPanel.Children.Add(voltageButton);
            stackPanel.Children.Add(comprehensiveButton);
            stackPanel.Children.Add(cancelButton);

            Content = stackPanel;
        }

        private void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            SelectedPreset = button?.Tag?.ToString();
            DialogResult = true;
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

                        sb.AppendLine($"{kvp.Key},{currentValue},{stats.Minimum},{stats.Maximum},{stats.Average},{stats.StandardDeviation},{stats.Count}");
                    }
                }

                File.WriteAllText(fileName, sb.ToString());
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error writing statistics file: {ex.Message}");
            }
        }


    /// <summary>
    /// Export data dialog
    /// </summary>
    public partial class ExportDataDialog : Window
    {
        private readonly MeasurementController controller;

        public ExportDataDialog(MeasurementController controller)
        {
            this.controller = controller;
            Title = "Export Measurement Data";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterParent;

            // Simple implementation - full export dialog would be more complex
            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var exportButton = new Button
            {
                Content = "Export Current Values & Statistics to CSV",
                Height = 40,
                Margin = new Thickness(0, 20)
            };
            exportButton.Click += ExportButton_Click;

            stackPanel.Children.Add(new TextBlock
            {
                Text = "Export measurement data to file",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 20)
            });
            stackPanel.Children.Add(exportButton);

            Content = stackPanel;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            // Trigger the export functionality from the measurement panel
            DialogResult = true;
        }
    }

    /// <summary>
    /// Measurement help dialog
    /// </summary>
    public partial class MeasurementHelpDialog : Window
    {
        public MeasurementHelpDialog()
        {
            Title = "Measurement Help";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterParent;

            var scrollViewer = new ScrollViewer();
            var textBlock = new TextBlock
            {
                Text = GetHelpText(),
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap
            };

            scrollViewer.Content = textBlock;
            Content = scrollViewer;
        }

        private string GetHelpText()
        {
            return @"RIGOL DS1000Z-E AUTOMATIC MEASUREMENTS HELP

OVERVIEW:
The automatic measurement system provides 37 different measurement parameters organized into categories:

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

ADVANCED:
• Area measurements - Waveform area calculations
• Delay/Phase - Between-channel measurements

MEASUREMENT SETUP:
• Threshold Levels: Define measurement reference points (MAX, MID, MIN)
• Pulse Setup: Configure pulse measurement parameters
• Delay Setup: Configure delay measurement parameters

STATISTICS:
• Enable statistics to track min, max, average, and standard deviation
• Reset statistics to clear accumulated data
• Export statistics to CSV for analysis

TIPS:
• Use presets for quick setup of common measurement scenarios
• Enable auto-update for continuous measurement monitoring
• Configure thresholds based on your signal characteristics
• Use statistics for long-term signal analysis";
        }
    }

    #endregion
}