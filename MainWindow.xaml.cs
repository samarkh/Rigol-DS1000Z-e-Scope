﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;
using DS1000Z_E_USB_Control.Trigger;
using DS1000Z_E_USB_Control.TimeBase;
using DS1000Z_E_USB_Control;
using Microsoft.Win32;
using System.Text;

namespace Rigol_DS1000Z_E_Control
{
    /// <summary>
    /// Enhanced MainWindow with comprehensive settings management including trigger control
    /// Fixed all compilation errors (CS7036, CS1503, CS1061, CS1501)
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
        /// FIXED: Updated to use correct constructor parameters for TriggerController
        /// </summary>
        private void InitializeControlPanels()
        {
            // Initialize Channel 1 panel
            if (Channel1Panel != null)
            {
                Channel1Panel.LogEvent += (sender, message) => Log($"Ch1: {message}");
                Channel1Panel.Initialize(oscilloscope);
            }

            // Initialize Channel 2 panel
            if (Channel2Panel != null)
            {
                Channel2Panel.LogEvent += (sender, message) => Log($"Ch2: {message}");
                Channel2Panel.Initialize(oscilloscope);
            }

            // Initialize Trigger panel - FIXED: Use enhanced constructor with settingsManager
            if (TriggerPanel != null)
            {
                TriggerPanel.LogEvent += (sender, message) => Log($"Trigger: {message}");
                TriggerPanel.Initialize(oscilloscope, settingsManager); // Pass both parameters to fix CS7036
            }

            // Initialize TimeBase panel
            if (TimeBasePanel != null)
            {
                TimeBasePanel.LogEvent += (sender, message) => Log($"TimeBase: {message}");
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
                                  "4. Oscilloscope USB mode is set to 'Computer'",
                                  "Connection Failed",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    Log("Connection failed");
                }
            }
            else
            {
                // Disconnect
                oscilloscope.Disconnect();
                isConnected = false;
                UpdateUI(false);
                Log("Disconnected from oscilloscope");
            }
        }

        /// <summary>
        /// Update UI based on connection status
        /// </summary>
        private void UpdateUI(bool connected)
        {
            if (connected)
            {
                StatusText.Text = "Status: Connected";
                StatusText.Foreground = Brushes.Green;
                ConnectButton.Content = "Disconnect";
                ConnectButton.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                ConnectButton.BorderBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));

                // Enable control buttons
                GetSettingsButton.IsEnabled = true;
                ExportSettingsButton.IsEnabled = true;
                PresetButton.IsEnabled = true;
                TriggerControlButton.IsEnabled = true;

                // Enable oscilloscope control buttons
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

                // Disable oscilloscope control buttons
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

        /// <summary>
        /// Update device information display
        /// </summary>
        private void UpdateDeviceInfo()
        {
            if (isConnected)
            {
                string deviceInfo = settingsManager.GetDeviceID();
                string acquisitionInfo = settingsManager.GetAcquisitionInfo();

                if (DeviceInfoText != null)
                    DeviceInfoText.Text = $"Device: {deviceInfo}";

                if (AcquisitionInfoText != null)
                    AcquisitionInfoText.Text = $"Acquisition: {acquisitionInfo}";
            }
            else
            {
                if (DeviceInfoText != null)
                    DeviceInfoText.Text = "Device: Not Connected";

                if (AcquisitionInfoText != null)
                    AcquisitionInfoText.Text = "Acquisition: Unknown";
            }
        }

        /// <summary>
        /// Update last update time display
        /// </summary>
        private void UpdateLastUpdateTime()
        {
            if (LastUpdateText != null)
            {
                LastUpdateText.Text = $"Last Settings Update: {DateTime.Now:HH:mm:ss}";
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
        /// FIXED: Line 259 error - proper UpdateFromSettings calls
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
                    // Update UI with the new settings - FIXED: Use proper method calls
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
        /// FIXED: CS1501 error - proper UpdateFromSettings method calls
        /// </summary>
        private void UpdateAllPanelsFromSettings()
        {
            try
            {
                // Update Channel 1 UI - FIXED: Use proper single parameter method
                if (Channel1Panel != null && settingsManager.Channel1Settings != null)
                {
                    var ch1Controller = Channel1Panel.GetController();
                    if (ch1Controller != null)
                    {
                        ch1Controller.UpdateFromSettings(settingsManager.Channel1Settings); // Proper single parameter call
                        Log($"✅ Updated Channel 1 UI: {settingsManager.Channel1Settings}");
                    }
                }

                // Update Channel 2 UI - FIXED: Use proper single parameter method
                if (Channel2Panel != null && settingsManager.Channel2Settings != null)
                {
                    var ch2Controller = Channel2Panel.GetController();
                    if (ch2Controller != null)
                    {
                        ch2Controller.UpdateFromSettings(settingsManager.Channel2Settings); // Proper single parameter call
                        Log($"✅ Updated Channel 2 UI: {settingsManager.Channel2Settings}");
                    }
                }

                // Update Trigger UI - FIXED: Use proper method call
                if (TriggerPanel != null && settingsManager.TriggerSettings != null)
                {
                    TriggerPanel.UpdateFromSettings(settingsManager.TriggerSettings);
                    Log($"🎯 Updated Trigger UI: {settingsManager.TriggerSettings}");
                }

                // Update TimeBase UI if available
                if (TimeBasePanel != null && settingsManager.TimeBaseSettings != null)
                {
                    var timeBaseController = TimeBasePanel.GetController();
                    if (timeBaseController != null)
                    {
                        timeBaseController.UpdateFromSettings(settingsManager.TimeBaseSettings);
                        Log($"📊 Updated TimeBase UI: {settingsManager.TimeBaseSettings}");
                    }
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
                              "Export Settings",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
                return;
            }

            try
            {
                // Get current settings first
                if (!settingsManager.ReadAllCurrentSettings())
                {
                    MessageBox.Show("Failed to read current settings from oscilloscope.",
                                  "Export Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                // Show save dialog
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Settings Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    Title = "Export Oscilloscope Settings",
                    FileName = $"RigolDS1000ZE_Settings_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // FIXED: CS1503 errors - proper string conversion from settings objects
                    StringBuilder settingsText = new StringBuilder();
                    settingsText.AppendLine($"Rigol DS1000Z-E Settings Export");
                    settingsText.AppendLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    settingsText.AppendLine($"Device ID: {settingsManager.GetDeviceID()}");
                    settingsText.AppendLine($"Acquisition Info: {settingsManager.GetAcquisitionInfo()}");
                    settingsText.AppendLine();

                    // Channel 1 Settings - FIXED: Convert to string properly
                    if (settingsManager.Channel1Settings != null)
                    {
                        settingsText.AppendLine("[Channel 1 Settings]");
                        settingsText.AppendLine(settingsManager.Channel1Settings.ToString()); // Line 527 fix
                        settingsText.AppendLine();
                    }

                    // Channel 2 Settings - FIXED: Convert to string properly  
                    if (settingsManager.Channel2Settings != null)
                    {
                        settingsText.AppendLine("[Channel 2 Settings]");
                        settingsText.AppendLine(settingsManager.Channel2Settings.ToString()); // Line 563 fix
                        settingsText.AppendLine();
                    }

                    // Trigger Settings - FIXED: Convert to string properly
                    if (settingsManager.TriggerSettings != null)
                    {
                        settingsText.AppendLine("[Trigger Settings]");
                        settingsText.AppendLine(settingsManager.TriggerSettings.ToString()); // Line 581 fix
                        settingsText.AppendLine();
                    }

                    // TimeBase Settings
                    if (settingsManager.TimeBaseSettings != null)
                    {
                        settingsText.AppendLine("[TimeBase Settings]");
                        settingsText.AppendLine(settingsManager.TimeBaseSettings.ToString());
                        settingsText.AppendLine();
                    }

                    // Write to file
                    File.WriteAllText(saveDialog.FileName, settingsText.ToString());

                    Log($"Settings exported to: {saveDialog.FileName}");
                    MessageBox.Show($"Settings successfully exported to:\n{saveDialog.FileName}",
                                  "Export Complete",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error exporting settings: {ex.Message}");
                MessageBox.Show($"Error exporting settings:\n{ex.Message}",
                              "Export Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Preset button handler
        /// </summary>
        private void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.",
                              "Apply Preset",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
                return;
            }

            ApplyComprehensivePresets();
        }
        #endregion

        #region Oscilloscope Control Methods
        /// <summary>
        /// Run button handler
        /// </summary>
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            if (oscilloscope.SendCommand(":RUN"))
            {
                Log("Oscilloscope started (RUN mode)");
            }
            else
            {
                Log("Failed to start oscilloscope");
            }
        }

        /// <summary>
        /// Stop button handler
        /// </summary>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            if (oscilloscope.SendCommand(":STOP"))
            {
                Log("Oscilloscope stopped");
            }
            else
            {
                Log("Failed to stop oscilloscope");
            }
        }

        /// <summary>
        /// Single button handler
        /// </summary>
        private void SingleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            if (oscilloscope.SendCommand(":SINGle"))
            {
                Log("Single acquisition triggered");
            }
            else
            {
                Log("Failed to trigger single acquisition");
            }
        }

        /// <summary>
        /// Clear button handler
        /// </summary>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            if (oscilloscope.SendCommand(":CLEar"))
            {
                Log("Display cleared");
            }
            else
            {
                Log("Failed to clear display");
            }
        }

        /// <summary>
        /// Auto Scale button handler
        /// </summary>
        private void AutoScaleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            if (oscilloscope.SendCommand(":AUToscale"))
            {
                Log("Auto scale applied");
                // Auto scale changes settings, so update them
                System.Threading.Tasks.Task.Delay(2000).ContinueWith(t =>
                {
                    Dispatcher.Invoke(() => GetCurrentSettings());
                });
            }
            else
            {
                Log("Failed to apply auto scale");
            }
        }
        #endregion

        #region Additional Methods
        /// <summary>
        /// Trigger Control button handler
        /// </summary>
        private void TriggerControlButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.",
                              "Trigger Control",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
                return;
            }

            // Example: Force trigger
            if (oscilloscope.SendCommand(":TFORce"))
            {
                Log("Force trigger executed");
            }
            else
            {
                Log("Failed to execute force trigger");
            }
        }

        /// <summary>
        /// FIXED: Channel synchronization method (lines 624-629 fix)
        /// </summary>
        private void SynchronizeChannels()
        {
            if (!isConnected) return;

            var ch1Controller = Channel1Panel?.GetController();
            if (ch1Controller != null)
            {
                var ch1Settings = ch1Controller.GetSettings(); // FIXED: Get properly typed settings
                if (ch1Settings != null)
                {
                    var ch2Settings = new Ch2Settings
                    {
                        IsEnabled = true,
                        ProbeRatio = ch1Settings.ProbeRatio,        // Line 624 fix - proper property access
                        VerticalScale = ch1Settings.VerticalScale,   // Line 625 fix - proper property access
                        VerticalOffset = 0, // Keep different offset for visual separation
                        Units = ch1Settings.Units,                  // Line 627 fix - proper property access
                        Coupling = ch1Settings.Coupling,            // Line 628 fix - proper property access
                        BandwidthLimit = ch1Settings.BandwidthLimit  // Line 629 fix - proper property access
                    };

                    var ch2Controller = Channel2Panel?.GetController();
                    ch2Controller?.SetSettings(ch2Settings);
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

                // Refresh settings after applying presets
                System.Threading.Tasks.Task.Delay(1000).ContinueWith(t =>
                {
                    Dispatcher.Invoke(() => GetCurrentSettings());
                });
            }
            catch (Exception ex)
            {
                Log($"Error applying power measurement presets: {ex.Message}");
            }
        }
        #endregion

        #region Event Handlers and Logging
        /// <summary>
        /// Handle log events from the oscilloscope
        /// </summary>
        private void Oscilloscope_LogEvent(object sender, string message)
        {
            Log($"Oscilloscope: {message}");
        }

        /// <summary>
        /// Log a message to the console and any connected log handlers
        /// </summary>
        private void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] {message}";

            // Write to console for debugging
            Console.WriteLine(logMessage);

            // Write to debug output
            System.Diagnostics.Debug.WriteLine(logMessage);

            // You can add additional logging here (file, UI log panel, etc.)
        }


        // ============================================================================
        // FIX 1: Add missing Initialize overload to TriggerControlPanel.xaml.cs
        // ============================================================================

        // ============================================================================
        // FIX 2: Add missing ClearLogButton_Click to MainWindow.xaml.cs
        // ============================================================================

        // Add this method to MainWindow class:
        /// <summary>
        /// ADDED: Missing ClearLogButton_Click method - fixes CS1061 error on line 238
        /// </summary>
        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LogTextBox != null)
                {
                    LogTextBox.Clear();
                    Log("Log cleared");
                }
            }
            catch (Exception ex)
            {
                Log($"Error clearing log: {ex.Message}");
            }
        }



        #endregion
    }
}