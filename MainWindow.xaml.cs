using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;
using DS1000Z_E_USB_Control.Trigger;
using DS1000Z_E_USB_Control;
using Microsoft.Win32;

namespace Rigol_DS1000Z_E_Control
{
    /// <summary>
    /// Enhanced MainWindow with comprehensive settings management including trigger control
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

            // Initialize all control panels
            InitializeControlPanels();

            Log("Application started. Ready to connect to Rigol DS1000Z-E.");
            UpdateDeviceInfo();
        }

        /// <summary>
        /// Initialize all control panels (channels, trigger, and timebase)
        /// </summary>
        private void InitializeControlPanels()
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

            // Initialize Trigger panel
            if (TriggerPanel != null)
            {
                TriggerPanel.LogEvent += (sender, message) => Log(message);
                TriggerPanel.Initialize(oscilloscope);
            }

            // Initialize TimeBase panel
            if (TimeBasePanel != null)
            {
                TimeBasePanel.LogEvent += (sender, message) => Log(message);
                TimeBasePanel.Initialize(oscilloscope);
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

                // Enable existing control buttons
                GetSettingsButton.IsEnabled = true;
                ExportSettingsButton.IsEnabled = true;
                PresetButton.IsEnabled = true;
                TriggerControlButton.IsEnabled = true;

                // Enable NEW oscilloscope control buttons (if you add them)
                RunButton.IsEnabled = true;
                StopButton.IsEnabled = true;
                SingleButton.IsEnabled = true;
                ClearButton.IsEnabled = true;
                AutoScaleButton.IsEnabled = true;

                // Enable all control panels
                Channel1Panel?.SetEnabled(true);
                Channel2Panel?.SetEnabled(true);
                TriggerPanel?.SetEnabled(true);
                TimeBasePanel.IsEnabled = true;
            }
            else
            {
                StatusText.Text = "Status: Disconnected";
                StatusText.Foreground = Brushes.Red;
                ConnectButton.Content = "Connect";
                ConnectButton.Background = new SolidColorBrush(Color.FromRgb(232, 245, 232));
                ConnectButton.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));

                // Disable existing control buttons
                GetSettingsButton.IsEnabled = false;
                ExportSettingsButton.IsEnabled = false;
                PresetButton.IsEnabled = false;
                TriggerControlButton.IsEnabled = false;

                // Disable NEW oscilloscope control buttons (if you add them)
                RunButton.IsEnabled = false;
                StopButton.IsEnabled = false;
                SingleButton.IsEnabled = false;
                ClearButton.IsEnabled = false;
                AutoScaleButton.IsEnabled = false;

                // Disable all control panels
                Channel1Panel?.SetEnabled(false);
                Channel2Panel?.SetEnabled(false);
                TriggerPanel?.SetEnabled(false);
                TimeBasePanel.IsEnabled = false;
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
                    UpdateAllPanelsFromSettings();
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
        /// Update all control panel UIs with settings from oscilloscope
        /// </summary>
        private void UpdateAllPanelsFromSettings()
        {
            try
            {
                // Update Channel 1 UI
                if (Channel1Panel != null && settingsManager.Channel1Settings != null)
                {
                    Channel1Panel.UpdateFromSettings(settingsManager.Channel1Settings);
                    Log($"✅ Updated Channel 1 UI: {settingsManager.Channel1Settings}");
                }

                // Update Channel 2 UI
                if (Channel2Panel != null && settingsManager.Channel2Settings != null)
                {
                    Channel2Panel.UpdateFromSettings(settingsManager.Channel2Settings);
                    Log($"✅ Updated Channel 2 UI: {settingsManager.Channel2Settings}");
                }

                // Update Trigger UI
                if (TriggerPanel != null && settingsManager.TriggerSettings != null)
                {
                    TriggerPanel.UpdateFromSettings(settingsManager.TriggerSettings);
                    Log($"🎯 Updated Trigger UI: {settingsManager.TriggerSettings}");
                }

                // Log timebase info (for future UI implementation)
                if (settingsManager.TimeBaseSettings != null)
                {
                    Log($"📊 TimeBase Settings: {settingsManager.TimeBaseSettings}");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error updating UI from settings: {ex.Message}");
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

            // Create enhanced preset selection dialog
            var result = MessageBox.Show("Apply General Purpose preset to all subsystems?\n\n" +
                                       "This will set:\n" +
                                       "• Both channels enabled with 10× probe\n" +
                                       "• 500mV/div scale, DC coupling\n" +
                                       "• 1ms/div timebase\n" +
                                       "• Edge trigger on CH1, auto sweep\n" +
                                       "• Zero offsets and levels",
                                       "Apply Comprehensive Preset",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ApplyComprehensivePresets();
            }
        }

        /// <summary>
        /// Trigger Control button handler
        /// </summary>
        private void TriggerControlButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.",
                              "Not Connected",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
                return;
            }

            // Query and update trigger settings specifically
            Log("Querying trigger settings...");
            TriggerPanel?.QueryAndUpdateSettings();
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

      #region Oscilloscope Control Button Handlers

        /// <summary>
        /// Run button - Start oscilloscope acquisition
        /// </summary>
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            if (oscilloscope.SendCommand(":RUN"))
            {
                Log("▶️ Oscilloscope started (RUN mode)");
            }
            else
            {
                Log("❌ Failed to start oscilloscope");
            }
        }

        /// <summary>
        /// Stop button - Stop oscilloscope acquisition
        /// </summary>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            if (oscilloscope.SendCommand(":STOP"))
            {
                Log("⏹️ Oscilloscope stopped");
            }
            else
            {
                Log("❌ Failed to stop oscilloscope");
            }
        }

        /// <summary>
        /// Single trigger button - Set to single trigger mode
        /// </summary>
        private void SingleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            if (oscilloscope.SendCommand(":SINGle"))
            {
                Log("🎯 Single trigger mode activated");
            }
            else
            {
                Log("❌ Failed to set single trigger mode");
            }
        }

        /// <summary>
        /// Auto Scale button - Automatically adjust display
        /// </summary>
        private void AutoScaleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            Log("📏 Running auto scale...");
            if (oscilloscope.SendCommand(":AUToscale"))
            {
                Log("✅ Auto scale completed");
                // Refresh settings after auto scale
                GetCurrentSettings();
            }
            else
            {
                Log("❌ Auto scale failed");
            }
        }

        /// <summary>
        /// Clear button - Clear the display
        /// </summary>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            if (oscilloscope.SendCommand(":CLEar"))
            {
                Log("🗑️ Display cleared");
            }
            else
            {
                Log("❌ Failed to clear display");
            }
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
        /// Apply preset to Trigger
        /// </summary>
        public void ApplyTriggerPreset(TriggerSettings preset)
        {
            if (isConnected)
            {
                TriggerPanel?.ApplyPreset(preset);
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
                var triggerPreset = TriggerSettings.Presets.NoisySignal;

                Channel1Panel?.ApplyPreset(ch1Preset);
                Channel2Panel?.ApplyPreset(ch2Preset);
                TriggerPanel?.ApplyPreset(triggerPreset);

                Log("Applied small signal preset to both channels and trigger");
            }
        }

        /// <summary>
        /// Get current settings from all panels
        /// </summary>
        public void LogAllPanelSettings()
        {
            if (isConnected)
            {
                var ch1Settings = Channel1Panel?.GetSettings();
                var ch2Settings = Channel2Panel?.GetSettings();
                var triggerSettings = TriggerPanel?.GetSettings();

                if (ch1Settings != null)
                    Log($"Channel 1: {ch1Settings}");

                if (ch2Settings != null)
                    Log($"Channel 2: {ch2Settings}");

                if (triggerSettings != null)
                    Log($"Trigger: {triggerSettings}");
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

      #region Enhanced Preset Methods
        /// <summary>
        /// Apply comprehensive presets to all subsystems including trigger
        /// </summary>
        public void ApplyComprehensivePresets()
        {
            if (!isConnected) return;

            try
            {
                // Apply to all subsystems using the settings manager
                settingsManager.ApplyGeneralPurposePreset();

                // Wait a moment for settings to apply, then refresh
                System.Threading.Tasks.Task.Delay(1000).ContinueWith(t =>
                {
                    Dispatcher.Invoke(() => GetCurrentSettings());
                });

                Log("Applied comprehensive presets to all subsystems");
            }
            catch (Exception ex)
            {
                Log($"Error applying comprehensive presets: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply power measurement presets to all subsystems
        /// </summary>
        public void ApplyPowerMeasurementPresets()
        {
            if (!isConnected) return;

            try
            {
                settingsManager.ApplyPowerMeasurementPreset();
                Log("Applied power measurement presets to all subsystems");
            }
            catch (Exception ex)
            {
                Log($"Error applying power measurement presets: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply high frequency presets to all subsystems
        /// </summary>
        public void ApplyHighFrequencyPresets()
        {
            if (!isConnected) return;

            try
            {
                settingsManager.ApplyHighFrequencyPreset();
                Log("Applied high frequency presets to all subsystems");
            }
            catch (Exception ex)
            {
                Log($"Error applying high frequency presets: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply digital measurement presets to all subsystems
        /// </summary>
        public void ApplyDigitalPresets()
        {
            if (!isConnected) return;

            try
            {
                settingsManager.ApplyDigitalPreset();
                Log("Applied digital measurement presets to all subsystems");
            }
            catch (Exception ex)
            {
                Log($"Error applying digital presets: {ex.Message}");
            }
        }
        #endregion

      #region Trigger-Specific Methods
        /// <summary>
        /// Force a trigger event
        /// </summary>
        public void ForceTrigger()
        {
            if (isConnected)
            {
                bool success = oscilloscope.SendCommand(":TFORce");
                if (success)
                {
                    Log("🎯 Trigger forced manually");
                }
                else
                {
                    Log("❌ Failed to force trigger");
                }
            }
        }

        /// <summary>
        /// Set trigger to single shot mode
        /// </summary>
        public void SetSingleShotTrigger()
        {
            if (isConnected)
            {
                var singleShotPreset = TriggerSettings.Presets.SingleShot;
                TriggerPanel?.ApplyPreset(singleShotPreset);
                Log("🎯 Set trigger to single shot mode");
            }
        }

        /// <summary>
        /// Reset trigger to auto mode
        /// </summary>
        public void ResetTriggerToAuto()
        {
            if (isConnected)
            {
                var autoPreset = TriggerSettings.Presets.GeneralPurpose;
                TriggerPanel?.ApplyPreset(autoPreset);
                Log("🎯 Reset trigger to auto mode");
            }
        }
        #endregion

      #region TimeBase Methods



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

            // Clean up all panel resources
            Channel1Panel?.Cleanup();
            Channel2Panel?.Cleanup();
            TriggerPanel?.Cleanup();
            TimeBasePanel?.Cleanup();  // Add this line

            // Ensure proper cleanup
            if (isConnected)
            {
                oscilloscope.Disconnect();
            }
        }
        #endregion
    }
}