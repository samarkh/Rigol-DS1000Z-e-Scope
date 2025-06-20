using DS1000Z_E_USB_Control;
using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;
using DS1000Z_E_USB_Control.Storage;
using DS1000Z_E_USB_Control.TimeBase;
using DS1000Z_E_USB_Control.Trigger;
using Microsoft.Win32;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        private SimpleWaveformCapture captureSystem;


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

            // ADD THIS LINE HERE:
            InitializeCaptureSystem();

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

                    // ADD THIS LINE HERE:
                    InitializeCaptureSystem();

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
        // Replace the UpdateDeviceInfo method in your MainWindow.xaml.cs with this:

        /// <summary>
        /// Update device information display (simplified for new UI)
        /// </summary>
        private void UpdateDeviceInfo()
        {
            try
            {
                if (settingsManager != null)
                {
                    string deviceId = settingsManager.GetDeviceID();
                    string acquisitionInfo = settingsManager.GetAcquisitionInfo();

                    // Update the main status text instead of separate device info controls
                    if (!string.IsNullOrEmpty(deviceId))
                    {
                        // Log the device info instead of showing in separate controls
                        Log($"📱 Device: {deviceId}");
                        Log($"📊 Acquisition: {acquisitionInfo}");
                    }
                    else
                    {
                        Log("📱 Device: Not Connected");
                        Log("📊 Acquisition: Unknown");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error updating device info: {ex.Message}");
            }
        }

        /// <summary>
        /// Update last update time display (simplified for new UI)
        /// </summary>
        private void UpdateLastUpdateTime()
        {
            // Log the update time instead of showing in a separate control
            Log($"🕒 Settings updated at: {DateTime.Now:HH:mm:ss}");
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


        // ADD these to your MainWindow.xaml.cs class:
      #region Simple Capture System

       

        /// <summary>
        /// Initialize the simple capture system (call this in your Window_Loaded or connection method)
        /// </summary>
        private void InitializeCaptureSystem()
        {
            try
            {
                captureSystem = new SimpleWaveformCapture(oscilloscope); // your existing oscilloscope instance
                captureSystem.LogEvent += (s, message) => Log(message);
                captureSystem.WaveformCaptured += (s, waveform) => Log($"Captured: {waveform}");

                Log("✅ Simple capture system ready");
            }
            catch (Exception ex)
            {
                Log($"❌ Error initializing capture: {ex.Message}");
            }
        }

        /// <summary>
        /// Capture from Channel 1
        /// </summary>
        private void CaptureChannel1_Click(object sender, RoutedEventArgs e)
        {
            if (captureSystem == null) return;

            var waveform = captureSystem.CaptureWaveform(1);
            if (waveform != null)
            {
                Log($"✅ Captured CH1: {waveform.SampleCount} points");
            }
        }

        /// <summary>
        /// Capture from Channel 2
        /// </summary>
        private void CaptureChannel2_Click(object sender, RoutedEventArgs e)
        {
            if (captureSystem == null) return;

            var waveform = captureSystem.CaptureWaveform(2);
            if (waveform != null)
            {
                Log($"✅ Captured CH2: {waveform.SampleCount} points");
            }
        }

        /// <summary>
        /// Show memory status
        /// </summary>
        private void ShowMemoryStatus_Click(object sender, RoutedEventArgs e)
        {
            if (captureSystem == null) return;

            string status = captureSystem.GetMemoryStatus();
            MessageBox.Show(status, "Memory Status", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ============================================================================
        // Replace your ExportLatestWaveform_Click method in MainWindow.xaml.cs with this:
        // ============================================================================

        /// <summary>
        /// Export latest waveform with format selection (DEBUGGED VERSION)
        /// </summary>
        private void ExportLatestWaveform_Click(object sender, RoutedEventArgs e)
        {
            if (captureSystem == null)
            {
                Log("❌ Capture system not initialized");
                return;
            }

            var waveforms = captureSystem.GetStoredWaveforms();
            if (waveforms.Count == 0)
            {
                MessageBox.Show("No waveforms to export", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // DEBUG: Check if ComboBox exists
            if (ExportFormatComboBox == null)
            {
                Log("❌ Export format ComboBox not found - using CSV default");
                // Fallback to CSV if ComboBox doesn't exist
                ExportWithFormat(waveforms.Last(), ExportFormat.CSV);
                return;
            }

            // Get selected format with debugging
            var selectedItem = ExportFormatComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null)
            {
                Log("❌ No format selected - using CSV default");
                ExportWithFormat(waveforms.Last(), ExportFormat.CSV);
                return;
            }

            // DEBUG: Log selected item details
            string formatTag = selectedItem.Tag?.ToString() ?? "csv";
            string formatContent = selectedItem.Content?.ToString() ?? "CSV";
            Log($"🔍 Selected format: {formatContent} (tag: {formatTag})");

            // Convert tag to enum with detailed logging
            ExportFormat exportFormat;
            switch (formatTag.ToLower())
            {
                case "csv":
                    exportFormat = ExportFormat.CSV;
                    Log("📄 Using CSV format");
                    break;
                case "json":
                    exportFormat = ExportFormat.JSON;
                    Log("📄 Using JSON format");
                    break;
                case "matlab":
                    exportFormat = ExportFormat.MATLAB;
                    Log("📄 Using MATLAB format");
                    break;
                case "binary":
                    exportFormat = ExportFormat.RawBinary;
                    Log("📄 Using Raw Binary format");
                    break;
                case "preamble":
                    exportFormat = ExportFormat.WithPreamble;
                    Log("📄 Using With Preamble format");
                    break;
                default:
                    Log($"⚠️ Unknown format tag '{formatTag}' - defaulting to CSV");
                    exportFormat = ExportFormat.CSV;
                    break;
            }

            // Export with the selected format
            ExportWithFormat(waveforms.Last(), exportFormat);
        }


        /// <summary>
        /// Helper method to handle the actual export with proper file handling
        /// </summary>
        private void ExportWithFormat(CapturedWaveform waveform, ExportFormat format)
        {
            try
            {
                // Get format-specific file info
                string extension = SimpleWaveformCapture.GetFileExtension(format);
                string filter = SimpleWaveformCapture.GetFileFilter(format);
                string formatName = format.ToString();

                Log($"🔧 Setting up export: Format={formatName}, Extension={extension}");

                // Create save dialog with format-specific settings
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Waveform_CH{waveform.ChannelNumber}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}",
                    DefaultExt = extension,
                    Filter = filter,
                    Title = $"Export Waveform as {formatName}"
                };

                Log($"💾 Save dialog: File={dialog.FileName}, Filter={filter}");

                if (dialog.ShowDialog() == true)
                {
                    Log($"📁 User selected file: {dialog.FileName}");
                    Log($"🔄 Starting export in {formatName} format...");

                    // Call the export method with explicit format
                    bool success = captureSystem.ExportWaveform(waveform, dialog.FileName, format);

                    if (success)
                    {
                        Log($"✅ Export successful: {dialog.FileName}");
                        MessageBox.Show($"Waveform exported successfully!\n\nFile: {dialog.FileName}\nFormat: {formatName}\nSize: {new FileInfo(dialog.FileName).Length} bytes",
                                      "Export Complete",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                    }
                    else
                    {
                        Log($"❌ Export failed for format {formatName}");
                        MessageBox.Show($"Export failed for {formatName} format. Check the log for details.",
                                      "Export Error",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Error);
                    }
                }
                else
                {
                    Log("🚫 Export cancelled by user");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Export error: {ex.Message}");
                MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Clear all stored waveforms
        /// </summary>
        private void ClearMemory_Click(object sender, RoutedEventArgs e)
        {
            if (captureSystem == null) return;

            if (MessageBox.Show("Clear all stored waveforms?", "Clear Memory",
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                captureSystem.ClearMemory();
                Log("Memory cleared");
            }
        }

        // Add these event handlers to your MainWindow.xaml.cs

        /// <summary>
        /// Save CH1 waveform directly to USB drive on oscilloscope
        /// </summary>
        private void SaveCH1ToUSB_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "USB Save",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string filename = $"CH1_Waveform_{DateTime.Now:yyyyMMdd_HHmmss}";

            if (captureSystem.SaveWaveformToUSB(1, filename))
            {
                Log($"✅ CH1 waveform saved to oscilloscope USB as {filename}");
                MessageBox.Show($"CH1 waveform saved to USB drive as:\n{filename}.csv",
                              "USB Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to save to USB. Check:\n• USB drive is connected to oscilloscope\n• USB drive has free space\n• Oscilloscope is not busy",
                              "USB Save Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Save CH2 waveform directly to USB drive on oscilloscope
        /// </summary>
        private void SaveCH2ToUSB_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "USB Save",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string filename = $"CH2_Waveform_{DateTime.Now:yyyyMMdd_HHmmss}";

            if (captureSystem.SaveWaveformToUSB(2, filename))
            {
                Log($"✅ CH2 waveform saved to oscilloscope USB as {filename}");
                MessageBox.Show($"CH2 waveform saved to USB drive as:\n{filename}.csv",
                              "USB Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to save to USB. Check:\n• USB drive is connected to oscilloscope\n• USB drive has free space\n• Oscilloscope is not busy",
                              "USB Save Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Save oscilloscope screen image to USB drive
        /// </summary>
        private void SaveScreenToUSB_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "USB Save",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string filename = $"Screen_{DateTime.Now:yyyyMMdd_HHmmss}";

            if (captureSystem.SaveScreenToUSB(filename, "PNG"))
            {
                Log($"✅ Screen image saved to oscilloscope USB as {filename}.png");
                MessageBox.Show($"Screen image saved to USB drive as:\n{filename}.png",
                              "USB Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to save screen to USB. Check:\n• USB drive is connected to oscilloscope\n• USB drive has free space",
                              "USB Save Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Check USB drive status on oscilloscope
        /// </summary>
        private void CheckUSBStatus_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "USB Status",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (captureSystem.CheckUSBStatus())
            {
                Log("✅ USB drive detected on oscilloscope");
            }
            else
            {
                Log("❌ No USB drive detected or empty");
                MessageBox.Show("No USB drive detected on oscilloscope.\n\nMake sure:\n• USB drive is properly connected\n• USB drive is formatted (FAT32 recommended)\n• USB drive is not write-protected",
                              "USB Status", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        #endregion


        // Add these methods to your MainWindow.xaml.cs
      #region Dynamic Trigger Step Integration

        /// <summary>
        /// Update trigger level control when channel settings change
        /// </summary>
        private void UpdateTriggerLevelStepSizes()
        {
            if (!isConnected || TriggerPanel == null) return;

            try
            {
                // Get current channel settings
                var ch1Settings = Channel1Panel?.GetController()?.GetSettings() ?? new Ch1Settings();
                var ch2Settings = Channel2Panel?.GetController()?.GetSettings() ?? new Ch2Settings();

                // Update trigger level control with current channel settings
                TriggerPanel.UpdateTriggerLevelControl(ch1Settings, ch2Settings);

                Log("🎯 Trigger level step sizes updated based on channel settings");
            }
            catch (Exception ex)
            {
                Log($"❌ Error updating trigger level step sizes: {ex.Message}");
            }
        }

        /// <summary>
        /// UPDATED: Initialize trigger panel with dynamic step integration
        /// </summary>
        private void InitializeTriggerPanel()
        {
            if (TriggerPanel == null) return;

            try
            {
                TriggerPanel.Initialize(oscilloscope, settingsManager);
                TriggerPanel.LogEvent += (sender, message) => Log(message);

                // NEW: Subscribe to trigger source changes
                TriggerPanel.TriggerSourceChanged += (sender, e) => UpdateTriggerLevelStepSizes();

                Log("🎯 Trigger panel initialized with dynamic step sizing");
            }
            catch (Exception ex)
            {
                Log($"❌ Error initializing trigger panel: {ex.Message}");
            }
        }

        /// <summary>
        /// UPDATED: Initialize channel panels with trigger step update integration
        /// </summary>
        private void InitializeChannelPanels()
        {
            try
            {
                // Initialize Channel 1
                if (Channel1Panel != null)
                {
                    Channel1Panel.Initialize(oscilloscope);
                    Channel1Panel.LogEvent += (sender, message) => Log(message);

                    // NEW: Update trigger steps when Channel 1 settings change
                    var ch1Controller = Channel1Panel.GetController();
                    if (ch1Controller != null)
                    {
                        ch1Controller.SettingsChanged += (sender, e) => UpdateTriggerLevelStepSizes();
                    }
                }

                // Initialize Channel 2  
                if (Channel2Panel != null)
                {
                    Channel2Panel.Initialize(oscilloscope);
                    Channel2Panel.LogEvent += (sender, message) => Log(message);

                    // NEW: Update trigger steps when Channel 2 settings change
                    var ch2Controller = Channel2Panel.GetController();
                    if (ch2Controller != null)
                    {
                        ch2Controller.SettingsChanged += (sender, e) => UpdateTriggerLevelStepSizes();
                    }
                }

                Log("📺 Channel panels initialized with trigger step integration");
            }
            catch (Exception ex)
            {
                Log($"❌ Error initializing channel panels: {ex.Message}");
            }
        }

        /// <summary>
        /// Read all current settings from the oscilloscope and update UI
        /// FIXED: UpdateUIFromSettings → UpdateAllPanelsFromSettings
        /// </summary>
        private void GetCurrentSettings()
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.",
                              "Get Settings",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
                return;
            }

            try
            {
                Log("📊 Reading current oscilloscope settings...");

                // Read all settings using the settings manager
                bool success = settingsManager.ReadAllCurrentSettings();

                if (success)
                {
                    // FIXED: Use correct method name
                    UpdateAllPanelsFromSettings();  // ✅ CORRECT - not UpdateUIFromSettings()
                    UpdateDeviceInfo();
                    UpdateLastUpdateTime();

                    // NEW: Update trigger level step sizes after reading settings
                    UpdateTriggerLevelStepSizes();

                    Log("✅ Settings updated successfully with dynamic trigger steps");
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
                Log($"❌ Error reading settings: {ex.Message}");
                MessageBox.Show($"Error reading oscilloscope settings:\n{ex.Message}",
                              "Settings Read Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        #endregion
        // Add these DEBUG methods to your MainWindow.xaml.cs

      #region DEBUG: Test Dynamic Trigger Steps

        /// <summary>
        /// DEBUG: Test trigger step sizing manually
        /// </summary>
        private void TestTriggerStepSizing()
        {
            if (!isConnected || TriggerPanel == null)
            {
                Log("❌ Cannot test - not connected or no trigger panel");
                return;
            }

            var triggerController = TriggerPanel.GetController();
            if (triggerController == null)
            {
                Log("❌ Cannot get trigger controller for testing");
                return;
            }

            Log("🧪 === TESTING DYNAMIC TRIGGER STEPS ===");

            // Test 1: Force set to 1V steps
            Log("Test 1: Setting to 1V steps");
            triggerController.ForceUpdateStepSize(1.0);
            System.Threading.Thread.Sleep(100);

            // Test 2: Force set to 100mV steps  
            Log("Test 2: Setting to 100mV steps");
            triggerController.ForceUpdateStepSize(0.1);
            System.Threading.Thread.Sleep(100);

            // Test 3: Force set to 50mV steps
            Log("Test 3: Setting to 50mV steps");
            triggerController.ForceUpdateStepSize(0.05);

            Log("🧪 Manual testing complete - try using trigger arrows now");
        }

        /// <summary>
        /// DEBUG: Get current channel settings and test dynamic update
        /// </summary>
        private void TestChannelBasedStepSizing()
        {
            if (!isConnected)
            {
                Log("❌ Cannot test - not connected");
                return;
            }

            Log("🧪 === TESTING CHANNEL-BASED TRIGGER STEPS ===");

            try
            {
                // Get current channel settings
                var ch1Settings = Channel1Panel?.GetController()?.GetSettings();
                var ch2Settings = Channel2Panel?.GetController()?.GetSettings();

                if (ch1Settings == null || ch2Settings == null)
                {
                    Log("❌ Cannot get channel settings for testing");
                    return;
                }

                Log($"📊 CH1: Scale={ch1Settings.VerticalScale}V/div, Enabled={ch1Settings.IsEnabled}");
                Log($"📊 CH2: Scale={ch2Settings.VerticalScale}V/div, Enabled={ch2Settings.IsEnabled}");

                // Test the dynamic update
                UpdateTriggerLevelStepSizes();

                Log("🧪 Channel-based testing complete");
            }
            catch (Exception ex)
            {
                Log($"❌ Error testing channel-based steps: {ex.Message}");
            }
        }

        #endregion



        // Additional event handlers to add to your MainWindow.xaml.cs
        // These support the enhanced storage functionality in the complete XAML

      #region Enhanced Storage Event Handlers

        /// <summary>
        /// Save both channels to USB simultaneously
        /// </summary>
        private void SaveBothChannelsToUSB_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "USB Save",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var format = GetSelectedUSBWaveformFormat();
                string baseFilename = $"DualChannel_{DateTime.Now:yyyyMMdd_HHmmss}";

                Log($"📊 Starting dual channel save to USB (Format: {format})...");

                // Save both channels with format
                bool ch1Success = usbStorageManager.SaveWaveformToUSB(1, $"{baseFilename}_CH1", format);
                System.Threading.Thread.Sleep(500); // Delay between saves
                bool ch2Success = usbStorageManager.SaveWaveformToUSB(2, $"{baseFilename}_CH2", format);

                if (ch1Success && ch2Success)
                {
                    Log("✅ Both channels saved successfully to USB");
                    MessageBox.Show($"Both channels saved to USB drive:\n{baseFilename}_CH1.{format.ToString().ToLower()}\n{baseFilename}_CH2.{format.ToString().ToLower()}",
                                  "Dual Channel Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Log($"⚠️ Partial save: CH1={ch1Success}, CH2={ch2Success}");
                    MessageBox.Show("Partial save completed. Check log for details.", "Dual Channel Save",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Dual channel save error: {ex.Message}");
                MessageBox.Show($"Error saving both channels: {ex.Message}", "Save Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Enhanced screen save with format options
        /// </summary>
        private void SaveScreenToUSB_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "USB Save",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var format = GetSelectedUSBImageFormat();
                bool colorMode = ColorModeCheckBox?.IsChecked ?? true;
                bool invertColors = InvertColorsCheckBox?.IsChecked ?? false;

                string filename = $"Screen_{DateTime.Now:yyyyMMdd_HHmmss}";

                if (usbStorageManager.SaveScreenImageToUSB(filename, format, colorMode, invertColors))
                {
                    string modeDesc = colorMode ? "color" : "grayscale";
                    string invertDesc = invertColors ? ", inverted" : "";
                    Log($"✅ Screen image saved: {filename}.{format.ToString().ToLower()} ({modeDesc}{invertDesc})");
                    MessageBox.Show($"Screen image saved to USB drive:\n{filename}.{format.ToString().ToLower()}\nMode: {modeDesc}{invertDesc}",
                                  "Screen Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to save screen to USB. Check connection and USB drive.",
                                  "Screen Save Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Screen save error: {ex.Message}");
                MessageBox.Show($"Error saving screen: {ex.Message}", "Save Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Save system setup with selected format
        /// </summary>
        private void SaveSetup_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "Setup Save",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var format = GetSelectedSetupFormat();
                Log($"💾 Saving setup in {format} format...");

                if (setupStorageManager.SaveSetupWithDialog(format))
                {
                    Log("✅ Setup saved successfully");
                }
                else
                {
                    Log("🚫 Setup save cancelled or failed");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Setup save error: {ex.Message}");
                MessageBox.Show($"Error saving setup: {ex.Message}", "Save Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Load system setup from file
        /// </summary>
        private void LoadSetup_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "Setup Load",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Log("📂 Loading setup from file...");

                if (setupStorageManager.LoadSetupWithDialog())
                {
                    Log("✅ Setup loaded successfully");
                    MessageBox.Show("Setup loaded successfully. All settings have been applied to the oscilloscope.",
                                  "Setup Load Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Refresh the UI to reflect loaded settings
                    GetSettingsButton_Click(sender, e);
                }
                else
                {
                    Log("🚫 Setup load cancelled or failed");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Setup load error: {ex.Message}");
                MessageBox.Show($"Error loading setup: {ex.Message}", "Load Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Save setup directly to USB in native format
        /// </summary>
        private void SaveSetupToUSB_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "USB Setup Save",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string filename = $"Setup_{DateTime.Now:yyyyMMdd_HHmmss}";
                Log($"📀 Saving setup to USB: {filename}...");

                if (setupStorageManager.SaveAsNativeFormat(filename))
                {
                    Log($"✅ Setup saved to USB: {filename}.set");
                    MessageBox.Show($"Setup saved to USB drive:\n{filename}.set",
                                  "USB Setup Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to save setup to USB. Check USB connection.",
                                  "USB Setup Save Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ USB setup save error: {ex.Message}");
                MessageBox.Show($"Error saving setup to USB: {ex.Message}", "Save Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Refresh USB file list and status
        /// </summary>
        private void RefreshUSB_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "USB Refresh",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Log("🔄 Refreshing USB status...");

                var status = usbStorageManager.GetUSBStatus();
                USBStatusTextBlock.Text = status.ToString();
                USBFileListBox.ItemsSource = status.Files;

                if (status.IsConnected)
                {
                    Log($"✅ USB refreshed: {status.FileCount} files found");
                }
                else
                {
                    Log($"❌ USB not connected: {status.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ USB refresh error: {ex.Message}");
                USBStatusTextBlock.Text = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Delete selected file from USB
        /// </summary>
        private void DeleteSelectedFile_Click(object sender, RoutedEventArgs e)
        {
            if (USBFileListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a file to delete.", "Delete File",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string selectedFile = USBFileListBox.SelectedItem.ToString();

            var result = MessageBox.Show($"Are you sure you want to delete '{selectedFile}' from the USB drive?\n\nThis action cannot be undone.",
                                       "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Log($"🗑️ Deleting file: {selectedFile}");

                    if (usbStorageManager.DeleteUSBFile(selectedFile))
                    {
                        Log($"✅ File deleted: {selectedFile}");
                        RefreshUSB_Click(sender, e); // Refresh the list
                    }
                    else
                    {
                        MessageBox.Show($"Failed to delete file: {selectedFile}", "Delete Failed",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    Log($"❌ Delete error: {ex.Message}");
                    MessageBox.Show($"Error deleting file: {ex.Message}", "Delete Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// USB file list selection changed
        /// </summary>
        private void USBFileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteSelectedFileButton.IsEnabled = USBFileListBox.SelectedItem != null;
        }

        /// <summary>
        /// Export all waveforms in selected format
        /// </summary>
        private void ExportAllWaveforms_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var waveforms = captureSystem.GetStoredWaveforms();
                if (waveforms.Count == 0)
                {
                    MessageBox.Show("No waveforms to export. Capture some waveforms first.", "Export All",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var exportFormat = GetSelectedExportFormat();
                Log($"📦 Starting batch export of {waveforms.Count} waveforms in {exportFormat} format...");

                string folder = GetExportFolder();
                if (string.IsNullOrEmpty(folder)) return;

                int successCount = 0;
                foreach (var waveform in waveforms)
                {
                    string filename = Path.Combine(folder, $"Waveform_CH{waveform.ChannelNumber}_{waveform.Timestamp:yyyyMMdd_HHmmss}");
                    if (captureSystem.ExportWaveform(waveform, filename, exportFormat))
                    {
                        successCount++;
                    }
                }

                Log($"✅ Batch export completed: {successCount}/{waveforms.Count} files exported");
                MessageBox.Show($"Batch export completed!\n\nExported: {successCount}/{waveforms.Count} files\nLocation: {folder}",
                              "Batch Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log($"❌ Batch export error: {ex.Message}");
                MessageBox.Show($"Batch export error: {ex.Message}", "Export Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Quick backup - save everything
        /// </summary>
        private void QuickBackup_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "Quick Backup",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Log("🎯 Starting quick backup...");
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // Save setup
                if (setupStorageManager.SaveAsNativeFormat($"QuickBackup_Setup_{timestamp}"))
                {
                    Log("✅ Setup backed up");
                }

                // Save current waveforms to USB
                var format = EnhancedUSBStorageManager.USBWaveformFormat.CSV;
                if (usbStorageManager.SaveMultipleWaveformsToUSB(new[] { 1, 2 }, $"QuickBackup_Waveforms_{timestamp}", format))
                {
                    Log("✅ Waveforms backed up");
                }

                // Save screen image
                if (usbStorageManager.SaveScreenImageToUSB($"QuickBackup_Screen_{timestamp}",
                    EnhancedUSBStorageManager.USBImageFormat.PNG))
                {
                    Log("✅ Screen image backed up");
                }

                MessageBox.Show("Quick backup completed! All data saved to USB drive.", "Backup Complete",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log($"❌ Quick backup error: {ex.Message}");
                MessageBox.Show($"Quick backup error: {ex.Message}", "Backup Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Quick restore from USB
        /// </summary>
        private void QuickRestore_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Quick Restore functionality coming soon!\n\nFor now, use 'Load Setup' to restore configurations.",
                          "Quick Restore", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Batch export with options
        /// </summary>
        private void BatchExport_Click(object sender, RoutedEventArgs e)
        {
            ExportAllWaveforms_Click(sender, e);
        }

        /// <summary>
        /// Storage settings dialog
        /// </summary>
        private void StorageSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Storage Settings dialog coming soon!\n\nThis will allow you to configure:\n• Default export formats\n• Auto-backup settings\n• File naming patterns\n• Storage locations",
                          "Storage Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Save log to file
        /// </summary>
        private void SaveLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Save Activity Log",
                    FileName = $"RigolDS1000ZE_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = ".txt"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, LogTextBox.Text, Encoding.UTF8);
                    Log($"💾 Log saved: {Path.GetFileName(dialog.FileName)}");
                    MessageBox.Show($"Log saved successfully:\n{dialog.FileName}", "Log Saved",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving log: {ex.Message}", "Save Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

      #region Helper Methods for Format Selection

        private EnhancedUSBStorageManager.USBWaveformFormat GetSelectedUSBWaveformFormat()
        {
            string tag = ((ComboBoxItem)USBWaveformFormatComboBox?.SelectedItem)?.Tag?.ToString() ?? "CSV";
            return Enum.Parse<EnhancedUSBStorageManager.USBWaveformFormat>(tag);
        }

        private EnhancedUSBStorageManager.USBImageFormat GetSelectedUSBImageFormat()
        {
            string tag = ((ComboBoxItem)USBImageFormatComboBox?.SelectedItem)?.Tag?.ToString() ?? "PNG";
            return Enum.Parse<EnhancedUSBStorageManager.USBImageFormat>(tag);
        }

        private SystemSetupStorageManager.SetupFileFormat GetSelectedSetupFormat()
        {
            string tag = ((ComboBoxItem)SetupFormatComboBox?.SelectedItem)?.Tag?.ToString() ?? "JSON";
            return Enum.Parse<SystemSetupStorageManager.SetupFileFormat>(tag);
        }

        private SimpleWaveformCapture.ExportFormat GetSelectedExportFormat()
        {
            string tag = ((ComboBoxItem)ExportFormatComboBox?.SelectedItem)?.Tag?.ToString() ?? "csv";
            return tag switch
            {
                "csv" => SimpleWaveformCapture.ExportFormat.CSV,
                "json" => SimpleWaveformCapture.ExportFormat.JSON,
                "matlab" => SimpleWaveformCapture.ExportFormat.MATLAB,
                "binary" => SimpleWaveformCapture.ExportFormat.RawBinary,
                "preamble" => SimpleWaveformCapture.ExportFormat.WithPreamble,
                _ => SimpleWaveformCapture.ExportFormat.CSV
            };
        }

        private string GetExportFolder()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select folder for batch export",
                ShowNewFolderButton = true
            };

            return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
        }

        #endregion

      #region Additional Initialization for Enhanced Storage

        // Add this to your existing InitializeCaptureSystem method or create a new initialization method
        private void InitializeEnhancedStorage()
        {
            try
            {
                // Initialize enhanced storage managers
                usbStorageManager = new EnhancedUSBStorageManager(oscilloscope, Log);
                setupStorageManager = new SystemSetupStorageManager(oscilloscope, settingsManager, Log);

                Log("🏪 Enhanced storage system initialized");

                // Set default format selections
                if (USBWaveformFormatComboBox != null) USBWaveformFormatComboBox.SelectedIndex = 0;
                if (USBImageFormatComboBox != null) USBImageFormatComboBox.SelectedIndex = 0;
                if (SetupFormatComboBox != null) SetupFormatComboBox.SelectedIndex = 0;
                if (ExportFormatComboBox != null) ExportFormatComboBox.SelectedIndex = 0;

                // Enable auto-scroll by default
                if (AutoScrollCheckBox != null) AutoScrollCheckBox.IsChecked = true;
            }
            catch (Exception ex)
            {
                Log($"❌ Enhanced storage initialization error: {ex.Message}");
            }
        }

        // Don't forget to add these private fields to your MainWindow class:
        private EnhancedUSBStorageManager usbStorageManager;
        private SystemSetupStorageManager setupStorageManager;

        #endregion


        private void Channel2Panel_Loaded(object sender, RoutedEventArgs e)
        {

        }



    }
}