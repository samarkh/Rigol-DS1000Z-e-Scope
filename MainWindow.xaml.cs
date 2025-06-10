using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;
using DS1000Z_E_USB_Control;
using Microsoft.Win32;

namespace Rigol_DS1000Z_E_Control
{
    /// <summary>
    /// Enhanced MainWindow with comprehensive settings management
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Fields
        private RigolDS1000ZE oscilloscope;
        private OscilloscopeSettingsManager settingsManager;
        private bool isConnected = false;
        #endregion

        #region Constructor and Initialization
        public MainWindow()
        {
            InitializeComponent();

            // Initialize the oscilloscope object
            oscilloscope = new RigolDS1000ZE();
            oscilloscope.LogEvent += Oscilloscope_LogEvent;

            // Initialize the settings manager
            settingsManager = new OscilloscopeSettingsManager(oscilloscope);
            settingsManager.LogEvent += (sender, message) => Log(message);

            // Initialize both channel panels
            InitializeChannelPanels();

            Log("Application started. Ready to connect to Rigol DS1000Z-E.");
            UpdateDeviceInfo();
        }

        /// <summary>
        /// Initialize both channel control panels
        /// </summary>
        private void InitializeChannelPanels()
        {
            // Initialize Channel 1 panel
            if (Channel1Panel != null)
            {
                Channel1Panel.LogEvent += (sender, message) => Log(message);
                Channel1Panel.Initialize(oscilloscope);
            }

            // Initialize Channel 2 panel
            if (Channel2Panel != null)
            {
                Channel2Panel.LogEvent += (sender, message) => Log(message);
                Channel2Panel.Initialize(oscilloscope);
            }
        }
        #endregion

        #region Connection Management
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                // Try to connect
                Log("Attempting to connect...");

                if (oscilloscope.Connect())
                {
                    isConnected = true;
                    UpdateUI(true);

                    // Query the device ID to confirm connection
                    string id = oscilloscope.SendQuery("*IDN?");
                    if (!string.IsNullOrEmpty(id))
                    {
                        Log($"Device ID: {id}");
                        UpdateDeviceInfo();
                    }

                    // Automatically get current settings after connection
                    Log("Connection successful! Reading current oscilloscope settings...");
                    GetCurrentSettings();
                }
                else
                {
                    MessageBox.Show("Failed to connect. Please check:\n" +
                                  "1. USB cable is connected\n" +
                                  "2. Oscilloscope is powered on\n" +
                                  "3. USB drivers are installed\n" +
                                  "4. VISA runtime is installed",
                                  "Connection Failed",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
            }
            else
            {
                // Try to disconnect
                Log("Disconnecting...");

                if (oscilloscope.Disconnect())
                {
                    isConnected = false;
                    UpdateUI(false);
                    UpdateDeviceInfo();
                }
            }
        }

        private void UpdateUI(bool connected)
        {
            if (connected)
            {
                StatusText.Text = "Status: Connected";
                StatusText.Foreground = Brushes.Green;
                ConnectButton.Content = "Disconnect";
                ConnectButton.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                ConnectButton.BorderBrush = Brushes.Red;

                // Enable control buttons
                GetSettingsButton.IsEnabled = true;
                ExportSettingsButton.IsEnabled = true;
                PresetButton.IsEnabled = true;

                // Enable both channel panels
                Channel1Panel?.SetEnabled(true);
                Channel2Panel?.SetEnabled(true);
            }
            else
            {
                StatusText.Text = "Status: Disconnected";
                StatusText.Foreground = Brushes.Red;
                ConnectButton.Content = "Connect";
                ConnectButton.Background = new SolidColorBrush(Color.FromRgb(232, 245, 232));
                ConnectButton.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));

                // Disable control buttons
                GetSettingsButton.IsEnabled = false;
                ExportSettingsButton.IsEnabled = false;
                PresetButton.IsEnabled = false;

                // Disable both channel panels
                Channel1Panel?.SetEnabled(false);
                Channel2Panel?.SetEnabled(false);
            }
        }
        #endregion

        #region Settings Management
        /// <summary>
        /// Get Current Settings button handler
        /// </summary>
        private void GetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            GetCurrentSettings();
        }

        /// <summary>
        /// Read all current settings from the oscilloscope and update UI
        /// </summary>
        private void GetCurrentSettings()
        {
            if (!isConnected)
            {
                Log("Cannot get settings - oscilloscope not connected");
                return;
            }

            Log("Reading all current oscilloscope settings...");

            try
            {
                // Read all settings using the settings manager
                bool success = settingsManager.ReadAllCurrentSettings();

                if (success)
                {
                    // Update UI with the new settings
                    UpdateChannelUIFromSettings();
                    UpdateDeviceInfo();
                    UpdateLastUpdateTime();

                    Log("✅ Successfully updated UI with current oscilloscope settings");
                }
                else
                {
                    Log("⚠️ Some settings could not be read - check oscilloscope connection");
                    MessageBox.Show("Some settings could not be read from the oscilloscope.\n" +
                                  "Check the connection and try again.",
                                  "Settings Read Warning",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error reading oscilloscope settings: {ex.Message}");
                MessageBox.Show($"Error reading oscilloscope settings:\n{ex.Message}",
                              "Settings Read Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Update channel UI controls with settings from oscilloscope
        /// </summary>
        private void UpdateChannelUIFromSettings()
        {
            try
            {
                // Update Channel 1 UI
                if (Channel1Panel != null && settingsManager.Channel1Settings != null)
                {
                    Channel1Panel.SetSettings(settingsManager.Channel1Settings);
                    Log($"Updated Channel 1 UI: {settingsManager.Channel1Settings}");
                }

                // Update Channel 2 UI
                if (Channel2Panel != null && settingsManager.Channel2Settings != null)
                {
                    Channel2Panel.SetSettings(settingsManager.Channel2Settings);
                    Log($"Updated Channel 2 UI: {settingsManager.Channel2Settings}");
                }

                // Log timebase and trigger info (for future UI implementation)
                if (settingsManager.TimeBaseSettings != null)
                {
                    Log($"TimeBase Settings: {settingsManager.TimeBaseSettings}");
                }

                if (settingsManager.TriggerSettings != null)
                {
                    Log($"Trigger Settings: {settingsManager.TriggerSettings}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error updating UI from settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Export Settings button handler
        /// </summary>
        private void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.",
                              "Not Connected",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
                return;
            }

            try
            {
                // First, get the latest settings
                settingsManager.ReadAllCurrentSettings();

                // Create export string
                string exportData = settingsManager.ExportSettingsToString();

                // Show save file dialog
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = $"DS1000ZE_Settings_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveDialog.FileName, exportData);
                    Log($"Settings exported to: {saveDialog.FileName}");

                    MessageBox.Show($"Settings successfully exported to:\n{saveDialog.FileName}",
                                  "Export Successful",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log($"Error exporting settings: {ex.Message}");
                MessageBox.Show($"Error exporting settings:\n{ex.Message}",
                              "Export Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Preset Menu button handler
        /// </summary>
        private void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.",
                              "Not Connected",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
                return;
            }

            // Create a simple preset selection dialog
            var result = MessageBox.Show("Apply General Purpose preset to both channels?\n\n" +
                                       "This will set:\n" +
                                       "• Both channels enabled\n" +
                                       "• 10× probe ratio\n" +
                                       "• 500mV/div scale\n" +
                                       "• DC coupling\n" +
                                       "• Zero offset",
                                       "Apply Preset",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ApplyGeneralPurposePresets();
            }
        }

        /// <summary>
        /// Update device information display
        /// </summary>
        private void UpdateDeviceInfo()
        {
            if (isConnected)
            {
                DeviceInfoText.Text = $"Device: {settingsManager.GetDeviceID()}";
                AcquisitionInfoText.Text = $"Acquisition: {settingsManager.GetAcquisitionInfo()}";
            }
            else
            {
                DeviceInfoText.Text = "Device: Not Connected";
                AcquisitionInfoText.Text = "Acquisition: Unknown";
            }
        }

        /// <summary>
        /// Update the last update timestamp
        /// </summary>
        private void UpdateLastUpdateTime()
        {
            LastUpdateText.Text = $"Last Settings Update: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }

        /// <summary>
        /// Clear Log button handler
        /// </summary>
        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
            Log("Log cleared");
        }
        #endregion

        #region Channel Management Methods
        /// <summary>
        /// Apply preset to Channel 1
        /// </summary>
        public void ApplyChannel1Preset(Ch1Settings preset)
        {
            if (isConnected)
            {
                Channel1Panel?.ApplyPreset(preset);
            }
        }

        /// <summary>
        /// Apply preset to Channel 2
        /// </summary>
        public void ApplyChannel2Preset(Ch2Settings preset)
        {
            if (isConnected)
            {
                Channel2Panel?.ApplyPreset(preset);
            }
        }

        /// <summary>
        /// Apply dual channel preset for differential measurements
        /// </summary>
        public void ApplyDualChannelPreset()
        {
            if (isConnected)
            {
                var ch1Preset = Ch1Settings.Presets.GeneralPurpose;
                var ch2Preset = Ch2Settings.Presets.GeneralPurpose;

                Channel1Panel?.ApplyPreset(ch1Preset);
                Channel2Panel?.ApplyPreset(ch2Preset);

                Log("Applied dual channel preset for differential measurements");
            }
        }

        /// <summary>
        /// Apply small signal preset to both channels
        /// </summary>
        public void ApplySmallSignalDualPreset()
        {
            if (isConnected)
            {
                var ch1Preset = Ch1Settings.Presets.SmallSignal;
                var ch2Preset = Ch2Settings.Presets.SmallSignal;

                Channel1Panel?.ApplyPreset(ch1Preset);
                Channel2Panel?.ApplyPreset(ch2Preset);

                Log("Applied small signal preset to both channels");
            }
        }

        /// <summary>
        /// Get current settings from both channels
        /// </summary>
        public void LogAllChannelSettings()
        {
            if (isConnected)
            {
                var ch1Settings = Channel1Panel?.GetSettings();
                var ch2Settings = Channel2Panel?.GetSettings();

                if (ch1Settings != null)
                    Log($"Channel 1: {ch1Settings}");

                if (ch2Settings != null)
                    Log($"Channel 2: {ch2Settings}");
            }
        }

        /// <summary>
        /// Synchronize both channels to the same scale settings
        /// </summary>
        public void SynchronizeChannels()
        {
            if (isConnected)
            {
                var ch1Settings = Channel1Panel?.GetSettings();
                if (ch1Settings != null)
                {
                    var ch2Settings = new Ch2Settings
                    {
                        IsEnabled = true,
                        ProbeRatio = ch1Settings.ProbeRatio,
                        VerticalScale = ch1Settings.VerticalScale,
                        VerticalOffset = 0, // Keep different offset for visual separation
                        Units = ch1Settings.Units,
                        Coupling = ch1Settings.Coupling,
                        BandwidthLimit = ch1Settings.BandwidthLimit
                    };

                    Channel2Panel?.SetSettings(ch2Settings);
                    Log("Synchronized Channel 2 settings to match Channel 1");
                }
            }
        }
        #endregion

        #region Preset Menu Methods
        /// <summary>
        /// Apply general purpose presets to both channels
        /// </summary>
        public void ApplyGeneralPurposePresets()
        {
            ApplyChannel1Preset(Ch1Settings.Presets.GeneralPurpose);
            ApplyChannel2Preset(Ch2Settings.Presets.GeneralPurpose);

            // Wait a moment for settings to apply, then refresh
            System.Threading.Tasks.Task.Delay(500).ContinueWith(t =>
            {
                Dispatcher.Invoke(() => GetCurrentSettings());
            });
        }

        public void ApplyPowerMeasurementPresets()
        {
            ApplyChannel1Preset(Ch1Settings.Presets.PowerMeasurement);
            ApplyChannel2Preset(Ch2Settings.Presets.PowerMeasurement);
        }

        public void ApplyHighFrequencyPresets()
        {
            ApplyChannel1Preset(Ch1Settings.Presets.HighFrequency);
            ApplyChannel2Preset(Ch2Settings.Presets.HighFrequency);
        }
        #endregion

        #region Logging and Events
        private void Oscilloscope_LogEvent(object sender, string message)
        {
            Log(message);
        }

        private void Log(string message)
        {
            // Add null check to prevent NullReferenceException during initialization
            if (LogTextBox == null)
            {
                return;
            }

            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (LogTextBox != null)
                    {
                        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                        LogTextBox.AppendText($"[{timestamp}] {message}\n");
                        LogTextBox.ScrollToEnd();

                        // Limit log size to prevent memory issues
                        if (LogTextBox.Text.Length > 50000)
                        {
                            string[] lines = LogTextBox.Text.Split('\n');
                            if (lines.Length > 100)
                            {
                                LogTextBox.Text = string.Join("\n", lines, lines.Length - 100, 100);
                                LogTextBox.AppendText("\n[Log truncated to preserve memory]\n");
                                LogTextBox.ScrollToEnd();
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logging failed: {ex.Message}");
            }
        }
        #endregion

        #region Cleanup
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Clean up channel panel resources
            Channel1Panel?.Cleanup();
            Channel2Panel?.Cleanup();

            // Ensure proper cleanup
            if (isConnected)
            {
                oscilloscope.Disconnect();
            }
        }
        #endregion
    }
}