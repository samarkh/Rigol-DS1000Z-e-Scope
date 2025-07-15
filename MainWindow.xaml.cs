using DS1000Z_E_USB_Control;
using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;
using DS1000Z_E_USB_Control.Mathematics;
using DS1000Z_E_USB_Control.Measurements;
using DS1000Z_E_USB_Control.SerialProtocol;
using DS1000Z_E_USB_Control.Storage;
using DS1000Z_E_USB_Control.TimeBase;
using DS1000Z_E_USB_Control.Trigger;
using Microsoft.Win32;
using Rigol_DS1000Z_E_Control;
using System;
using System.IO;
using System.Linq;
using System.Text;
//using System.Windows.Forms; // For FolderBrowserDialog
using System.Text.Json;      // For JSON serialization
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace Rigol_DS1000Z_E_Control
{
    /// <summary>
    /// Enhanced MainWindow with comprehensive settings management and enhanced storage capabilities
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Fields
        private RigolDS1000ZE oscilloscope;
        private OscilloscopeSettingsManager settingsManager;
        private bool isConnected = false;
        private SimpleWaveformCapture captureSystem;

        // Serial protocol window for SCPI command testing
        private SerialProtocolWINDOW _serialProtocolWindow;

        // Measurement controller and window
        private Window _measurementWindow;
        private MeasurementController _measurementController;


        // VISA manager for enhanced communication
        private VisaManager visaManager;


        // Mathematics window for advanced calculations
        private MathematicsWindow _mathematicsWindow;

        // Enhanced storage managers
        private EnhancedUSBStorageManager usbStorageManager;
        private SystemSetupStorageManager setupStorageManager;
        #endregion

        #region Constructor and Initialization
        public MainWindow()
        {
            InitializeComponent();

            // Initialize the oscilloscope object
            oscilloscope = new RigolDS1000ZE();
            oscilloscope.LogEvent += Oscilloscope_LogEvent;

            // Initialize the VISA manager
            InitializeVisaManager();

            // Initialize the settings manager
            settingsManager = new OscilloscopeSettingsManager(oscilloscope);
            settingsManager.LogEvent += (sender, message) => Log(message);

            // Initialize all control panels
            InitializeControlPanels();

            // Initialize capture system
            InitializeCaptureSystem();

            // Initialize enhanced storage system
            InitializeEnhancedStorage();

            Log("🚀 Application started. Ready to connect to Rigol DS1000Z-E.");
            UpdateDeviceInfo();
        }

        /// <summary>
        /// Initialize all control panels (channels, trigger, and timebase)
        /// </summary>
        private void InitializeControlPanels()
        {
            try
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

                // Initialize Trigger panel
                if (TriggerPanel != null)
                {
                    TriggerPanel.LogEvent += (sender, message) => Log($"Trigger: {message}");
                    TriggerPanel.Initialize(oscilloscope, settingsManager);
                }

                // Initialize TimeBase panel
                if (TimeBasePanel != null)
                {
                    TimeBasePanel.LogEvent += (sender, message) => Log($"TimeBase: {message}");
                    TimeBasePanel.Initialize(oscilloscope);
                }

                Log("✅ All control panels initialized");
            }
            catch (Exception ex)
            {
                Log($"❌ Error initializing control panels: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize the simple capture system
        /// </summary>
        private void InitializeCaptureSystem()
        {
            try
            {
                captureSystem = new SimpleWaveformCapture(oscilloscope);
                captureSystem.LogEvent += (s, message) => Log($"Capture: {message}");
                captureSystem.WaveformCaptured += (s, waveform) =>
                    Log($"📈 Captured waveform: CH{waveform.ChannelNumber}, {waveform.SampleCount} points");

                Log("✅ Capture system initialized");
            }
            catch (Exception ex)
            {
                Log($"❌ Error initializing capture system: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize enhanced storage system
        /// </summary>
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


        // ===================================================================

        // ✅ PROBLEM 2: visaManager initialization issue  
        // FIX: Ensure visaManager is properly initialized in the constructor

        // ✅ FIND THIS METHOD in MainWindow.xaml.cs (in the constructor region):
        // Look for: private void InitializeControlPanels()
        // ADD THIS METHOD or UPDATE the existing one:

        /// <summary>
        /// FIXED: Initialize VISA manager properly
        /// </summary>
        //private void InitializeVisaManager()
        //{
        //    try
        //    {
        //        // ✅ FIX: Initialize the VISA manager if it doesn't exist
        //        if (visaManager == null)
        //        {
        //            visaManager = new VisaManager();
        //            Log("🔌 VISA Manager initialized");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log($"❌ Error initializing VISA Manager: {ex.Message}");
        //    }
        //}

        /// <summary>
        /// Initialize VISA manager properly
        /// </summary>
        private void InitializeVisaManager()
        {
            try
            {
                // Initialize the VISA manager if it doesn't exist
                if (visaManager == null)
                {
                    visaManager = new VisaManager();
                    visaManager.LogEvent += (sender, message) => Log(message);
                    Log("🔌 VISA Manager initialized");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error initializing VISA Manager: {ex.Message}");
                // Continue without VISA manager - graceful degradation
                visaManager = null;
            }
        }
        #endregion

        #region Connection Management
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                // Try to connect
                Log("🔌 Attempting to connect...");

                if (oscilloscope.Connect())
                {
                    isConnected = true;
                    UpdateConnectionUI(true);

                    // Query the device ID to confirm connection
                    string id = oscilloscope.SendQuery("*IDN?");
                    if (!string.IsNullOrEmpty(id))
                    {
                        Log($"📱 Device ID: {id}");
                        UpdateDeviceInfo();
                    }

                    // Automatically get current settings after connection
                    Log("🔄 Connection successful! Reading current oscilloscope settings...");
                    GetCurrentSettings();
                    // Then establish the channel-trigger connection
                    EstablishChannelTriggerConnection();

                }
                else
                {
                    MessageBox.Show("Failed to connect. Please check:\n" +
                                  "1. USB cable is connected\n" +
                                  "2. Oscilloscope is powered on\n" +
                                  "3. USB drivers are installed\n" +
                                  "4. Oscilloscope is not connected to other software",
                                  "Connection Failed",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
            }
            else
            {
                // Disconnect
                Log("🔌 Disconnecting...");
                if (oscilloscope.Disconnect())
                {
                    isConnected = false;
                    UpdateConnectionUI(false);
                    Log("✅ Disconnected successfully");
                }
                else
                {
                    Log("❌ Error during disconnect");
                }
            }
        }

        // MODIFICATION: Update your disconnect method to clean up events
        private void DisconnectFromOscilloscope()
        {
            try
            {
                // Clean up channel events before disconnecting
                if (Channel1Panel?.GetController() != null)
                {
                    Channel1Panel.GetController().SettingsChanged -= OnChannelSettingsChanged;
                }

                if (Channel2Panel?.GetController() != null)
                {
                    Channel2Panel.GetController().SettingsChanged -= OnChannelSettingsChanged;
                }

                // ... rest of your existing disconnect code ...

                Log("🔌 Channel-Trigger connection cleaned up");
            }
            catch (Exception ex)
            {
                Log($"❌ Error during disconnect cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Update UI based on connection state
        /// </summary>
        private void UpdateConnectionUI(bool connected)
        {
            isConnected = connected;

            if (connected)
            {
                StatusText.Text = "Status: Connected";
                StatusText.Foreground = Brushes.Green;
                ConnectButton.Content = "🔌 Disconnect";
                ConnectButton.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                ConnectButton.BorderBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                if (ConnectionIndicator != null) ConnectionIndicator.Fill = Brushes.Green;

                // Enable main control buttons
                GetSettingsButton.IsEnabled = true;
                ExportSettingsButton.IsEnabled = true;
                PresetButton.IsEnabled = true;
                TriggerPanel.IsEnabled = true;

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
                if (TimeBasePanel != null) TimeBasePanel.IsEnabled = true;

                // Enable enhanced storage controls
                UpdateEnhancedStorageControlState(true);
            }
            else
            {
                StatusText.Text = "Status: Disconnected";
                StatusText.Foreground = Brushes.Red;
                ConnectButton.Content = "🔌 Connect";
                ConnectButton.Background = new SolidColorBrush(Color.FromRgb(232, 245, 232));
                ConnectButton.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                if (ConnectionIndicator != null) ConnectionIndicator.Fill = Brushes.Red;

                // Disable main control buttons
                GetSettingsButton.IsEnabled = false;
                ExportSettingsButton.IsEnabled = false;
                PresetButton.IsEnabled = false;
                TriggerPanel.IsEnabled = false;

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
                if (TimeBasePanel != null) TimeBasePanel.IsEnabled = false;

                // Disable enhanced storage controls
                UpdateEnhancedStorageControlState(false);
            }
        }

        /// <summary>
        /// Update enhanced storage control state
        /// </summary>
        private void UpdateEnhancedStorageControlState(bool isConnected)
        {
            // Enable/disable enhanced storage controls based on connection state
            if (SaveCH1ToUSBButton != null) SaveCH1ToUSBButton.IsEnabled = isConnected;
            if (SaveCH2ToUSBButton != null) SaveCH2ToUSBButton.IsEnabled = isConnected;
            if (SaveScreenToUSBButton != null) SaveScreenToUSBButton.IsEnabled = isConnected;
            if (SaveBothChannelsToUSBButton != null) SaveBothChannelsToUSBButton.IsEnabled = isConnected;
            if (SaveSetupButton != null) SaveSetupButton.IsEnabled = isConnected;
            if (LoadSetupButton != null) LoadSetupButton.IsEnabled = isConnected;
            if (SaveToUSBSetupButton != null) SaveToUSBSetupButton.IsEnabled = isConnected;
            if (RefreshUSBButton != null) RefreshUSBButton.IsEnabled = isConnected;
            if (CheckUSBStatusButton != null) CheckUSBStatusButton.IsEnabled = isConnected;
            if (QuickBackupButton != null) QuickBackupButton.IsEnabled = isConnected;
            if (QuickRestoreButton != null) QuickRestoreButton.IsEnabled = isConnected;
            if (BatchExportButton != null) BatchExportButton.IsEnabled = isConnected;
        }

        // <summary>
        /// SIMPLE FIX: Keep the existing OpenMathematics_Click method mostly unchanged
        /// Just ensure proper event subscription
        /// </summary>
        private async void OpenMathematics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mathematicsWindow == null || !_mathematicsWindow.IsLoaded)
                {
                    _mathematicsWindow = new MathematicsWindow();

                    // Use the existing connection check - no changes needed here
                    if (isConnected && oscilloscope?.IsConnected == true)
                    {
                        // Simple initialization - no need for complex VisaManager passing
                        bool initialized = await _mathematicsWindow.InitializeAsync(oscilloscope);

                        if (!initialized)
                        {
                            this.OnErrorOccurred("Failed to initialize Mathematics panel");
                        }
                        else
                        {
                            this.OnStatusUpdated("✅ Mathematics window initialized");
                        }
                    }
                    else
                    {
                        this.OnErrorOccurred("⚠️ Mathematics window opened but oscilloscope not connected");
                    }

                    // Subscribe to SCPI command events - THIS IS THE KEY
                    _mathematicsWindow.SCPICommandGenerated += OnMathematicsSCPICommand;

                    // ⭐ ADD THESE LINES RIGHT HERE ⭐
                    // Set the TimeBase controller reference for frequency calculations
                    if (TimeBasePanel?.GetController() != null)
                    {
                        _mathematicsWindow.MathPanel.TimeBaseController = TimeBasePanel.GetController();
                        Log("✅ TimeBase controller reference set for Mathematics tooltips");
                    }
                    else
                    {
                        Log("⚠️ TimeBase controller not available for Mathematics tooltips");
                    }

                    // Position relative to main window
                    _mathematicsWindow.Owner = this;
                    _mathematicsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    _mathematicsWindow.Show();
                    this.OnStatusUpdated("📊 Mathematics window opened");
                }
                else
                {
                    // Bring existing window to front
                    _mathematicsWindow.Activate();
                    _mathematicsWindow.Focus();
                }
            }
            catch (Exception ex)
            {
                this.OnErrorOccurred($"Error opening mathematics window: {ex.Message}");
            }
        }

        #endregion

        // Add this to your MainWindow.xaml.cs class in the connection/initialization section
        #region Channel Settings Change Event Wiring

        /// <summary>
        /// Wire up channel controller events AND trigger source change events
        /// Call this after initializing all panels
        /// </summary>
        private void WireUpChannelTriggerConnection()
        {
            try
            {
                // Subscribe to Channel 1 settings changes
                if (Channel1Panel?.GetController() != null)
                {
                    Channel1Panel.GetController().SettingsChanged -= OnChannelSettingsChanged;
                    Channel1Panel.GetController().SettingsChanged += OnChannelSettingsChanged;
                    Log("✅ Channel 1 settings change events wired to trigger updates");
                }

                // Subscribe to Channel 2 settings changes  
                if (Channel2Panel?.GetController() != null)
                {
                    Channel2Panel.GetController().SettingsChanged -= OnChannelSettingsChanged;
                    Channel2Panel.GetController().SettingsChanged += OnChannelSettingsChanged;
                    Log("✅ Channel 2 settings change events wired to trigger updates");
                }

                // Subscribe to trigger source changes (NEW)
                if (TriggerPanel != null)
                {
                    TriggerPanel.TriggerSourceChanged -= OnTriggerSourceChanged;
                    TriggerPanel.TriggerSourceChanged += OnTriggerSourceChanged;
                    Log("✅ Trigger source change events wired");
                }

                Log("🔗 Enhanced Channel-Trigger connection established");
            }
            catch (Exception ex)
            {
                Log($"❌ Error wiring enhanced channel-trigger connection: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle trigger source changes and update step sizes immediately
        /// </summary>
        private void OnTriggerSourceChanged(object sender, EventArgs e)
        {
            try
            {
                if (!isConnected || TriggerPanel == null) return;

                // Get current channel settings
                var ch1Settings = Channel1Panel?.GetController()?.GetSettings() ?? new Ch1Settings();
                var ch2Settings = Channel2Panel?.GetController()?.GetSettings() ?? new Ch2Settings();

                // Update trigger panel immediately when source changes
                TriggerPanel.OnTriggerSourceChanged(ch1Settings, ch2Settings);

                Log("🎯 Trigger step sizes updated for source change");
            }
            catch (Exception ex)
            {
                Log($"❌ Error handling trigger source change: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle channel settings changes and update trigger step sizes
        /// </summary>
        private void OnChannelSettingsChanged(object sender, EventArgs e)
        {
            UpdateTriggerLevelStepSizes();
        }

        /// <summary>
        /// Establish the enhanced channel-trigger connection
        /// Call this after all panels are initialized and connected
        /// </summary>
        private void EstablishChannelTriggerConnection()
        {
            if (isConnected && TriggerPanel != null &&
                Channel1Panel?.IsInitialized == true &&
                Channel2Panel?.IsInitialized == true)
            {
                // Wire up all the events
                WireUpChannelTriggerConnection();

                // Do an initial update to set correct step sizes
                UpdateTriggerLevelStepSizes();

                Log("🎯 Enhanced Channel-Trigger dynamic step sizing connection restored");
            }
        }

        /// <summary>
        /// Enhanced disconnect cleanup
        /// </summary>
        private void CleanupChannelTriggerConnection()
        {
            try
            {
                // Clean up channel events
                if (Channel1Panel?.GetController() != null)
                {
                    Channel1Panel.GetController().SettingsChanged -= OnChannelSettingsChanged;
                }

                if (Channel2Panel?.GetController() != null)
                {
                    Channel2Panel.GetController().SettingsChanged -= OnChannelSettingsChanged;
                }

                // Clean up trigger source events (NEW)
                if (TriggerPanel != null)
                {
                    TriggerPanel.TriggerSourceChanged -= OnTriggerSourceChanged;
                }

                Log("🔌 Enhanced Channel-Trigger connection cleaned up");
            }
            catch (Exception ex)
            {
                Log($"❌ Error during enhanced connection cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Serial Protocol Button Handlers

        // Add this event handler method
        /// <summary>
        /// Opens Serial Protocol window with proper event handling
        /// </summary>
        private void OpenSerialProtocol_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_serialProtocolWindow == null || !_serialProtocolWindow.IsVisible)
                {
                    _serialProtocolWindow = new SerialProtocolWINDOW();

                    // FIX: Convert event signatures properly
                    _serialProtocolWindow.SCPICommandGenerated += (s, command) =>
                    {
                        var args = new SCPICommandEventArgs(command, "SerialProtocol", "PROTOCOL");
                        OnSCPICommandGenerated(this, args);
                    };

                    // FIX: Clean up reference when window closes
                    _serialProtocolWindow.Closed += (s, args) => _serialProtocolWindow = null;

                    _serialProtocolWindow.Owner = this;
                    _serialProtocolWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    _serialProtocolWindow.Show();
                    OnStatusUpdated("🔧 Serial Protocol window opened");
                }
                else
                {
                    _serialProtocolWindow.Activate();
                    _serialProtocolWindow.Focus();
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error opening serial protocol window: {ex.Message}");
            }
        }

        // In MainWindow.xaml.cs - Use this CORRECT signature:
        private void OnSCPICommandGenerated(object sender, SCPICommandEventArgs e)
        {
            try
            {
                if (e == null || string.IsNullOrEmpty(e.Command)) return;

                // Log the command with source information
                string logMessage = string.IsNullOrEmpty(e.Source)
                    ? $"SCPI: {e.Command}"
                    : $"SCPI ({e.Source}): {e.Command}";

                Log(logMessage);

                // Send command to VISA manager if connected
                if (visaManager?.IsConnected == true)
                {
                    visaManager.SendCommand(e.Command);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error handling SCPI command: {ex.Message}");
            }
        }

        // You probably already have a method like this for SCPI communication
        private void SendSCPICommand(string command)
        {
            // Your existing SCPI communication code here
            // For example:
            // oscilloscope.WriteString(command);
            // or
            // visaSession.FormattedIO.WriteLine(command);

            // Placeholder for your implementation
            Console.WriteLine($"SCPI: {command}");
        }

        // Optional: Method to log messages to your existing logging system
        private void LogMessage(string message)
        {
            // Your existing logging implementation
            // For example:
            // LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        // Optional: Close protocol window when main window closes
        protected override void OnClosed(EventArgs e)
        {
            _serialProtocolWindow?.Close();
            CloseMeasurementWindow(); // Add this line
            base.OnClosed(e);
        }


        /// <summary>
        /// SIMPLE FIX: Use the existing oscilloscope's VisaManager directly
        /// No need for a second VisaManager instance
        /// </summary>
        private void OnMathematicsSCPICommand(object sender, SCPICommandEventArgs e)
        {
            try
            {
                if (e == null || string.IsNullOrEmpty(e.Command)) return;

                // Log the command with extra debug info
                Log($"📊 Math SCPI: {e.Command}");
                Log($"🔍 Debug: isConnected={isConnected}, oscilloscope.IsConnected={oscilloscope?.IsConnected}");

                // SIMPLE FIX: Use the existing oscilloscope connection
                // This should work and appear in VISA trace
                if (isConnected && oscilloscope?.IsConnected == true)
                {
                    // Add timestamp for NI Trace correlation
                    Log($"⏰ Sending at {DateTime.Now:HH:mm:ss.fff}: {e.Command}");

                    // Send through the existing, working oscilloscope object
                    bool success = oscilloscope.SendCommand(e.Command);

                    if (success)
                    {
                        // Parse operation type directly from command
                        string operationType = ParseMathOperationFromCommand(e.Command);
                        Log($"✅ Math command sent successfully: {e.Command}");
                        OnStatusUpdated($"Math {operationType} command sent: {e.Command}");
                    }
                    else
                    {
                        Log($"❌ Math command failed: {e.Command}");
                        OnErrorOccurred($"Failed to send math command: {e.Command}");
                    }
                }
                else
                {
                    Log($"⚠️ Not connected - math command not sent: {e.Command}");
                    OnStatusUpdated($"Math command generated (not connected): {e.Command}");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Exception in math SCPI handler: {ex.Message}");
                OnErrorOccurred($"Error handling Mathematics SCPI command: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse math operation type from SCPI command
        /// </summary>
        private string ParseMathOperationFromCommand(string command)
        {
            if (string.IsNullOrEmpty(command)) return "Unknown";

            string cmd = command.ToUpper();

            // Basic Operations
            if (cmd.Contains("ADD")) return "Add";
            if (cmd.Contains("SUBTRACT")) return "Subtract";
            if (cmd.Contains("MULTIPLY")) return "Multiply";
            if (cmd.Contains("DIVISION")) return "Divide";

            // FFT Analysis
            if (cmd.Contains("FFT")) return "FFT Analysis";

            // Digital Filters
            if (cmd.Contains("FILTER")) return "Filter";

            // Advanced Math
            if (cmd.Contains("OPTION")) return "Advanced Math";

            // Display controls
            if (cmd.Contains("DISPLAY")) return "Display Control";
            if (cmd.Contains("RESET")) return "Reset";

            return "Math Operation";
        }


        // added 02/07/2025
        /// <summary>
        /// Handle SCPI commands from Serial Protocol window - FIXED: Correct signature for EventHandler<string>
        /// This handles line 575 error and supports serial protocol decoding commands
        /// </summary>
        private void OnSerialProtocolSCPICommand(object sender, string command)
        {
            try
            {
                if (string.IsNullOrEmpty(command)) return;

                // Log the command with source information for serial protocol operations
                Log($"🔍 Serial Protocol SCPI: {command}");

                // Send the serial protocol command if connected
                if (isConnected && oscilloscope != null)
                {
                    oscilloscope.SendCommand(command);

                    // Enhanced logging for different protocol operations
                    string protocolType = GetProtocolOperationType(command);
                    OnStatusUpdated($"Serial Protocol {protocolType} command sent: {command}");
                }
                else
                {
                    OnStatusUpdated($"Serial Protocol command generated (not connected): {command}");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error handling Serial Protocol SCPI command: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to identify protocol operation types for enhanced logging
        /// </summary>
        private string GetProtocolOperationType(string command)
        {
            if (string.IsNullOrEmpty(command)) return "Unknown";

            string cmd = command.ToUpper();

            // Decoder Configuration
            if (cmd.Contains(":DECODER") && cmd.Contains(":MODE"))
            {
                if (cmd.Contains("UART")) return "UART Decoder";
                if (cmd.Contains("SPI")) return "SPI Decoder";
                if (cmd.Contains("IIC")) return "I²C Decoder";
                if (cmd.Contains("PARALLEL")) return "Parallel Decoder";
                return "Decoder Mode";
            }

            // Display Control
            if (cmd.Contains(":DECODER") && cmd.Contains(":DISPLAY")) return "Decoder Display";

            // Format Settings
            if (cmd.Contains(":DECODER") && cmd.Contains(":FORMAT"))
            {
                if (cmd.Contains("HEX")) return "Hex Format";
                if (cmd.Contains("ASCII")) return "ASCII Format";
                if (cmd.Contains("DEC")) return "Decimal Format";
                if (cmd.Contains("BIN")) return "Binary Format";
                return "Display Format";
            }

            // Threshold Settings
            if (cmd.Contains(":DECODER") && cmd.Contains(":THRESHOLD")) return "Threshold Setting";

            // Protocol-Specific Settings
            if (cmd.Contains(":UART:"))
            {
                if (cmd.Contains(":BAUD")) return "UART Baud Rate";
                if (cmd.Contains(":PARITY")) return "UART Parity";
                if (cmd.Contains(":STOP")) return "UART Stop Bits";
                if (cmd.Contains(":DATA")) return "UART Data Bits";
                return "UART Setting";
            }

            if (cmd.Contains(":SPI:"))
            {
                if (cmd.Contains(":EDGE")) return "SPI Clock Edge";
                if (cmd.Contains(":WIDTH")) return "SPI Data Width";
                if (cmd.Contains(":ENDIAN")) return "SPI Endian";
                if (cmd.Contains(":TIMEOUT")) return "SPI Timeout";
                return "SPI Setting";
            }

            if (cmd.Contains(":IIC:"))
            {
                if (cmd.Contains(":ADDRESS")) return "I²C Address";
                if (cmd.Contains(":SRATE")) return "I²C Sample Rate";
                return "I²C Setting";
            }

            if (cmd.Contains(":PARALLEL:"))
            {
                if (cmd.Contains(":WIDTH")) return "Parallel Bus Width";
                if (cmd.Contains(":CLOCK")) return "Parallel Clock";
                return "Parallel Setting";
            }

            // Configuration Management
            if (cmd.Contains(":CONFIG:"))
            {
                if (cmd.Contains(":LABEL")) return "Label Display";
                if (cmd.Contains(":LINE")) return "Bus Line Display";
                if (cmd.Contains(":FORMAT")) return "Format Display";
                return "Configuration";
            }

            return "Protocol Operation";
        }


        #endregion


        #region Measurements Window Management

        // ================================
        // CRITICAL FIX #3: MainWindow.xaml.cs OpenMeasurements_Click method
        // ================================
        // FIND this method around line 450 and REPLACE the entire method:

        private void OpenMeasurements_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "Open Measurements",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (_measurementWindow == null || !_measurementWindow.IsVisible)
                {
                    // Initialize measurement controller if not already done
                    if (_measurementController == null)
                    {
                        _measurementController = new MeasurementController(oscilloscope);
                        _measurementController.LogEvent += (s, message) => Log(message);
                    }

                    // Create simple window with measurement panel (NO MeasurementWindow class)
                    _measurementWindow = new Window
                    {
                        Title = "📊 Measurements & Statistics",
                        Width = 900,
                        Height = 700,
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Background = Brushes.White
                    };

                    // Create measurement panel directly
                    var measurementPanel = new MeasurementPanel
                    {
                        Controller = _measurementController,
                        Margin = new Thickness(10)
                    };

                    // Handle panel logging
                    measurementPanel.LogEvent += (s, message) => Log(message);

                    // Set panel as window content
                    _measurementWindow.Content = measurementPanel;

                    // Handle window closed event
                    _measurementWindow.Closed += (s, ev) => _measurementWindow = null;

                    Log("📊 Opening Measurements & Statistics window...");
                }

                // Show and bring to front
                _measurementWindow.Show();
                _measurementWindow.Activate();
                _measurementWindow.Focus();

                Log("📊 Measurements window opened");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Measurements window: {ex.Message}",
                              "Measurements Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                Log($"❌ Error opening Measurements window: {ex.Message}");
            }
        }


        /// <summary>
        /// Open or bring to front the Measurements window
        /// </summary>
        private void OpenMeasurementsWindow()
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "Open Measurements",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (_measurementWindow == null || !_measurementWindow.IsLoaded)
                {
                    // Initialize measurement controller if not already done
                    if (_measurementController == null)
                    {
                        _measurementController = new MeasurementController(oscilloscope);
                        _measurementController.LogEvent += (s, message) => Log(message);
                    }

                    // Create new measurement window
                    _measurementWindow = new Window
                    {
                        Title = "📊 Measurements & Statistics",
                        Width = 900,
                        Height = 700,
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Content = new MeasurementPanel { Controller = _measurementController }
                    };

                    // Update connection status
                    //

                    // Handle window closed event
                    _measurementWindow.Closed += (s, e) => _measurementWindow = null;

                    Log("📊 Opening Measurements & Statistics window...");
                }

                // Show and bring to front
                _measurementWindow.Show();
                _measurementWindow.Activate();
                _measurementWindow.Focus();

                Log("📊 Measurements window opened");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Measurements window: {ex.Message}",
                              "Measurements Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                Log($"❌ Error opening Measurements window: {ex.Message}");
            }
        }

        /// <summary>
        /// Update measurement window connection status
        /// </summary>
        // REPLACE WITH:
        private void UpdateMeasurementWindowConnection(bool connected)
        {
            // Simple approach - no special connection update needed
            // The controller handles connection state internally
        }

        /// <summary>
        /// Close measurement window when main window closes
        /// </summary>
        private void CloseMeasurementWindow()
        {
            _measurementWindow?.Close();
            _measurementWindow = null;
        }

        #endregion


        #region Settings Management
        /// <summary>
        /// Get current settings from the oscilloscope
        /// </summary>
        private void GetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            GetCurrentSettings();
        }

        /// <summary>
        /// Get current settings from oscilloscope
        /// </summary>
        private void GetCurrentSettings()
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "Get Settings",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Log("📊 Reading all oscilloscope settings...");

                if (settingsManager.ReadAllCurrentSettings())
                {
                    Log("✅ Successfully read all oscilloscope settings");
                    UpdateDeviceInfo();
                }
                else
                {
                    Log("⚠️ Some settings could not be read");
                    MessageBox.Show("Some settings could not be read. Check connection and try again.",
                                  "Settings Read Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error reading settings: {ex.Message}");
                MessageBox.Show($"Error reading settings: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Export current settings to file
        /// </summary>
        private void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "Export Settings",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Show save dialog
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Settings Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    Title = "Export Oscilloscope Settings",
                    FileName = $"RigolDS1000ZE_Settings_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string settingsText = settingsManager.ExportSettingsToString();
                    File.WriteAllText(saveDialog.FileName, settingsText, Encoding.UTF8);

                    Log($"✅ Settings exported: {Path.GetFileName(saveDialog.FileName)}");
                    MessageBox.Show($"Settings exported successfully to:\n{saveDialog.FileName}",
                                  "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Export error: {ex.Message}");
                MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Show presets menu
        /// </summary>
        private void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "Presets",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("Apply General Purpose preset to all subsystems?\n\n" +
                                       "This will configure:\n" +
                                       "• Channel 1: 1V/div, DC coupling, 1X probe\n" +
                                       "• Channel 2: 1V/div, DC coupling, 1X probe\n" +
                                       "• Trigger: Edge, Rising, CH1 source\n" +
                                       "• TimeBase: 1ms/div, Main mode",
                                       "Apply Preset", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ApplyGeneralPurposePresets();
            }
        }

        /// <summary>
        /// Apply general purpose presets
        /// </summary>
        private void ApplyGeneralPurposePresets()
        {
            try
            {
                Log("⚙️ Applying general purpose presets...");

                if (settingsManager.ApplyGeneralPurposePreset())
                {
                    Log("✅ General purpose presets applied successfully");

                    // Refresh settings after applying presets
                    System.Threading.Tasks.Task.Delay(1000).ContinueWith(t =>
                    {
                        Dispatcher.Invoke(() => GetCurrentSettings());
                    });
                }
                else
                {
                    Log("❌ Failed to apply presets");
                    MessageBox.Show("Failed to apply presets. Check connection and try again.",
                                  "Preset Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error applying presets: {ex.Message}");
                MessageBox.Show($"Error applying presets: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Update device information display
        /// </summary>
        private void UpdateDeviceInfo()
        {
            try
            {
                if (settingsManager != null)
                {
                    string deviceId = settingsManager.GetDeviceID();
                    string acquisitionInfo = settingsManager.GetAcquisitionInfo();

                    if (!string.IsNullOrEmpty(deviceId))
                    {
                        Log($"📱 Device: {deviceId}");
                        if (!string.IsNullOrEmpty(acquisitionInfo))
                        {
                            Log($"📊 Acquisition: {acquisitionInfo}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error updating device info: {ex.Message}");
            }
        }
        #endregion

        #region Oscilloscope Control Buttons
        /// <summary>
        /// Start oscilloscope acquisition
        /// </summary>
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            try
            {
                if (oscilloscope.SendCommand(":RUN"))
                {
                    Log("▶️ Oscilloscope started");
                }
                else
                {
                    Log("❌ Failed to start oscilloscope");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error starting oscilloscope: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop oscilloscope acquisition
        /// </summary>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            try
            {
                if (oscilloscope.SendCommand(":STOP"))
                {
                    Log("⏹️ Oscilloscope stopped");
                }
                else
                {
                    Log("❌ Failed to stop oscilloscope");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error stopping oscilloscope: {ex.Message}");
            }
        }

        /// <summary>
        /// Set oscilloscope to single trigger mode
        /// </summary>
        private void SingleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            try
            {
                if (oscilloscope.SendCommand(":SINGle"))
                {
                    Log("⏯️ Single trigger mode activated");
                }
                else
                {
                    Log("❌ Failed to set single trigger mode");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error setting single trigger mode: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear oscilloscope display
        /// </summary>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            try
            {
                if (oscilloscope.SendCommand(":CLEar"))
                {
                    Log("🧹 Display cleared");
                }
                else
                {
                    Log("❌ Failed to clear display");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error clearing display: {ex.Message}");
            }
        }

        /// <summary>
        /// Perform auto scale
        /// </summary>
        private void AutoScaleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            try
            {
                Log("📏 Performing auto scale...");
                if (oscilloscope.SendCommand(":AUToscale"))
                {
                    Log("✅ Auto scale completed");

                    // Refresh settings after auto scale
                    System.Threading.Tasks.Task.Delay(2000).ContinueWith(t =>
                    {
                        Dispatcher.Invoke(() => GetCurrentSettings());
                    });
                }
                else
                {
                    Log("❌ Failed to perform auto scale");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error performing auto scale: {ex.Message}");
            }
        }

        /// <summary>
        /// Open trigger control window
        /// </summary>
        private void TriggerControlButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Trigger controls are integrated into the main interface.\n\n" +
                          "Use the Trigger Control Panel on the right side of the main window.",
                          "Trigger Control", MessageBoxButton.OK, MessageBoxImage.Information);
        }



        /// <summary>
        /// Force trigger button click handler
        /// </summary>
        private void ForceTriggerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "Force Trigger",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Log("⚡ Forcing trigger...");

                // Send force trigger command to oscilloscope
                if (oscilloscope != null)
                {
                    oscilloscope.SendCommand(":TRIG:FORC");
                    Log("✅ Force trigger sent");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Force trigger error: {ex.Message}");
                MessageBox.Show($"Error forcing trigger: {ex.Message}", "Force Trigger Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        #endregion

        #region Waveform Capture
        /// <summary>
        /// Capture waveform from Channel 1
        /// </summary>
        private void CaptureChannel1_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "Capture",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Log("📈 Capturing CH1 waveform...");
                var waveform = captureSystem.CaptureWaveform(1);

                if (waveform != null)
                {
                    Log($"✅ CH1 captured: {waveform.SampleCount} points, {waveform.SampleRate / 1e6:F1} MSa/s");
                    MessageBox.Show($"CH1 waveform captured successfully!\n\n" +
                                  $"Sample Count: {waveform.SampleCount:N0}\n" +
                                  $"Sample Rate: {waveform.SampleRate / 1e6:F1} MSa/s\n" +
                                  $"Timestamp: {waveform.Timestamp:HH:mm:ss}",
                                  "Capture Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to capture CH1 waveform. Check connection and settings.",
                                  "Capture Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ CH1 capture error: {ex.Message}");
                MessageBox.Show($"Capture error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Capture waveform from Channel 2
        /// </summary>
        private void CaptureChannel2_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Please connect to the oscilloscope first.", "Capture",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Log("📈 Capturing CH2 waveform...");
                var waveform = captureSystem.CaptureWaveform(2);

                if (waveform != null)
                {
                    Log($"✅ CH2 captured: {waveform.SampleCount} points, {waveform.SampleRate / 1e6:F1} MSa/s");
                    MessageBox.Show($"CH2 waveform captured successfully!\n\n" +
                                  $"Sample Count: {waveform.SampleCount:N0}\n" +
                                  $"Sample Rate: {waveform.SampleRate / 1e6:F1} MSa/s\n" +
                                  $"Timestamp: {waveform.Timestamp:HH:mm:ss}",
                                  "Capture Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to capture CH2 waveform. Check connection and settings.",
                                  "Capture Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ CH2 capture error: {ex.Message}");
                MessageBox.Show($"Capture error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Show memory status
        /// </summary>
        private void ShowMemoryStatus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string status = captureSystem.GetMemoryStatus();
                Log($"💾 Memory Status:\n{status}");
                MessageBox.Show(status, "Memory Status", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log($"❌ Memory status error: {ex.Message}");
                MessageBox.Show($"Error getting memory status: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
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
                Log("🗑️ Memory cleared");
            }
        }

        /// <summary>
        /// Export latest captured waveform
        /// </summary>
        private void ExportLatestWaveform_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var waveforms = captureSystem.GetStoredWaveforms();
                if (waveforms.Count == 0)
                {
                    MessageBox.Show("No waveforms to export. Capture a waveform first.", "Export",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var exportFormat = GetSelectedExportFormat();
                ExportWithFormat(waveforms.Last(), exportFormat);
            }
            catch (Exception ex)
            {
                Log($"❌ Export error: {ex.Message}");
                MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                var dialog = new SaveFileDialog
                {
                    FileName = $"Waveform_CH{waveform.ChannelNumber}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}",
                    DefaultExt = extension,
                    Filter = filter,
                    Title = $"Export Waveform as {formatName}"
                };

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
                                      "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        Log($"❌ Export failed for format {formatName}");
                        MessageBox.Show($"Export failed for {formatName} format. Check the log for details.",
                                      "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        #endregion

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
                bool success = usbStorageManager.SaveMultipleWaveformsToUSB(new[] { 1, 2 }, baseFilename, format);

                if (success)
                {
                    Log("✅ Both channels saved successfully to USB");
                    MessageBox.Show($"Both channels saved to USB drive:\n{baseFilename}_CH1.{format.ToString().ToLower()}\n{baseFilename}_CH2.{format.ToString().ToLower()}",
                                  "Dual Channel Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
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
        /// Save CH1 waveform to USB
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
            var format = GetSelectedUSBWaveformFormat();

            if (usbStorageManager.SaveWaveformToUSB(1, filename, format))
            {
                Log($"✅ CH1 waveform saved to oscilloscope USB as {filename}");
                MessageBox.Show($"CH1 waveform saved to USB drive as:\n{filename}.{format.ToString().ToLower()}",
                              "USB Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to save to USB. Check:\n• USB drive is connected to oscilloscope\n• USB drive has free space\n• Oscilloscope is not busy",
                              "USB Save Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Save CH2 waveform to USB
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
            var format = GetSelectedUSBWaveformFormat();

            if (usbStorageManager.SaveWaveformToUSB(2, filename, format))
            {
                Log($"✅ CH2 waveform saved to oscilloscope USB as {filename}");
                MessageBox.Show($"CH2 waveform saved to USB drive as:\n{filename}.{format.ToString().ToLower()}",
                              "USB Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to save to USB. Check:\n• USB drive is connected to oscilloscope\n• USB drive has free space\n• Oscilloscope is not busy",
                              "USB Save Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    GetCurrentSettings();
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
                if (USBStatusTextBlock != null) USBStatusTextBlock.Text = status.ToString();
                if (USBFileListBox != null) USBFileListBox.ItemsSource = status.Files;

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
                if (USBStatusTextBlock != null) USBStatusTextBlock.Text = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Delete selected file from USB
        /// </summary>
        private void DeleteSelectedFile_Click(object sender, RoutedEventArgs e)
        {
            if (USBFileListBox?.SelectedItem == null)
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
            if (DeleteSelectedFileButton != null)
                DeleteSelectedFileButton.IsEnabled = USBFileListBox?.SelectedItem != null;
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
                var format = USBWaveformFormat.CSV;
                if (usbStorageManager.SaveMultipleWaveformsToUSB(new[] { 1, 2 }, $"QuickBackup_Waveforms_{timestamp}", format))
                {
                    Log("✅ Waveforms backed up");
                }

                // Save screen image
                if (usbStorageManager.SaveScreenImageToUSB($"QuickBackup_Screen_{timestamp}",
                    USBImageFormat.PNG))
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
        /// Check USB status
        /// </summary>
        private void CheckUSBStatus_Click(object sender, RoutedEventArgs e)
        {
            RefreshUSB_Click(sender, e);
        }

        #endregion

        #region Helper Methods for Format Selection

        private USBWaveformFormat GetSelectedUSBWaveformFormat()
        {
            string tag = ((ComboBoxItem)USBWaveformFormatComboBox?.SelectedItem)?.Tag?.ToString() ?? "CSV";
            return (USBWaveformFormat)Enum.Parse(typeof(USBWaveformFormat), tag);  // Fixed: Remove generics
        }


        private USBImageFormat GetSelectedUSBImageFormat()
        {
            string tag = ((ComboBoxItem)USBImageFormatComboBox?.SelectedItem)?.Tag?.ToString() ?? "PNG";
            return (USBImageFormat)Enum.Parse(typeof(USBImageFormat), tag);  // Fixed: Remove generics
        }

        private SetupFileFormat GetSelectedSetupFormat()
        {
            string tag = ((ComboBoxItem)SetupFormatComboBox?.SelectedItem)?.Tag?.ToString() ?? "JSON";
            return (SetupFileFormat)Enum.Parse(typeof(SetupFileFormat), tag);  // Fixed: Remove generics
        }

        private ExportFormat GetSelectedExportFormat()
        {
            string tag = ((ComboBoxItem)ExportFormatComboBox?.SelectedItem)?.Tag?.ToString() ?? "csv";
            return tag switch
            {
                "csv" => ExportFormat.CSV,
                "json" => ExportFormat.JSON,
                "matlab" => ExportFormat.MATLAB,
                "binary" => ExportFormat.RawBinary,
                "preamble" => ExportFormat.WithPreamble,
                _ => ExportFormat.CSV
            };
        }

        // Replace GetExportFolder() method with:
        private string GetExportFolder()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder",
                Filter = "Folders|*.",
                Title = "Select folder for batch export"
            };

            if (dialog.ShowDialog() == true)
            {
                return System.IO.Path.GetDirectoryName(dialog.FileName);
            }
            return null;
        }

        #endregion

        #region Logging and UI
        /// <summary>
        /// Handle log events from the oscilloscope
        /// </summary>
        private void Oscilloscope_LogEvent(object sender, string message)
        {
            Log($"Oscilloscope: {message}");
        }

        /// <summary>
        /// Log a message to the UI and debug output
        /// </summary>
        private void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] {message}";

            // Write to console for debugging
            Console.WriteLine(logMessage);

            // Write to debug output
            System.Diagnostics.Debug.WriteLine(logMessage);

            // Update UI on the main thread
            Dispatcher.Invoke(() =>
            {
                if (LogTextBox != null)
                {
                    LogTextBox.AppendText(logMessage + Environment.NewLine);

                    // Auto-scroll if enabled
                    if (AutoScrollCheckBox?.IsChecked == true && LogScrollViewer != null)
                    {
                        LogScrollViewer.ScrollToEnd();
                    }
                }
            });
        }

        /// <summary>
        /// Clear log button click
        /// </summary>
        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LogTextBox != null)
                {
                    LogTextBox.Clear();
                    Log("📋 Log cleared");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error clearing log: {ex.Message}");
            }
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
                    File.WriteAllText(dialog.FileName, LogTextBox?.Text ?? "", Encoding.UTF8);
                    Log($"💾 Log saved: {Path.GetFileName(dialog.FileName)}");
                    MessageBox.Show($"Log saved successfully:\n{dialog.FileName}", "Log Saved",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error saving log: {ex.Message}");
                MessageBox.Show($"Error saving log: {ex.Message}", "Save Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Channel Panel Events
        /// <summary>
        /// Channel 2 panel loaded event
        /// </summary>
        private void Channel2Panel_Loaded(object sender, RoutedEventArgs e)
        {
            // Channel 2 panel loaded - can add additional initialization here if needed
        }

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
        #endregion


        #region Missing Methods - Add These to Your Existing MainWindow.xaml.cs

        /// <summary>
        /// Handle error events
        /// </summary>
        /// <summary>
        /// Handle error events
        /// </summary>
        private void OnErrorOccurred(string errorMessage)
        {
            try
            {
                if (string.IsNullOrEmpty(errorMessage)) return;

                // Log the error
                Log($"❌ Error: {errorMessage}");

                // Update status with error indication
                Dispatcher.Invoke(() =>
                {
                    // REMOVE THIS BLOCK:
                    /*
                    if (StatusTextBlock != null)
                    {
                        StatusTextBlock.Text = $"Error: {errorMessage}";
                        StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    }
                    else 
                    */
                    if (StatusText != null) // Keep only this part
                    {
                        StatusText.Text = $"Error: {errorMessage}";
                        StatusText.Foreground = new SolidColorBrush(Colors.Red);
                    }
                });
            }
            catch (Exception ex)
            {
                // Fallback logging
                System.Diagnostics.Debug.WriteLine($"Error in OnErrorOccurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle status update events
        /// </summary>
        private void OnStatusUpdated(string statusMessage)
        {
            try
            {
                if (string.IsNullOrEmpty(statusMessage)) return;

                // Log the status
                Log($"📊 Status: {statusMessage}");

                // Update status display
                Dispatcher.Invoke(() =>
                {
                    if (StatusText != null) // Keep only this part
                    {
                        StatusText.Text = statusMessage;
                        StatusText.Foreground = new SolidColorBrush(Colors.Green);
                    }
                });
            }
            catch (Exception ex)
            {
                // Fallback logging
                System.Diagnostics.Debug.WriteLine($"Error in OnStatusUpdated: {ex.Message}");
            }
        }


        // In MainWindow.xaml.cs - ADD this method anywhere in the class:

        /// <summary>
        /// Get the current math operation type from mathematics window
        /// </summary>
        /// <returns>Current math operation type or default</returns>

        private string GetMathOperationType()
        {
            try
            {
                if (_mathematicsWindow?.IsVisible == true && _mathematicsWindow.MathPanel != null)
                {
                    return _mathematicsWindow.MathPanel.GetCurrentMathMode();
                }
                return "BasicOperations";
            }
            catch (Exception ex)
            {
                Log($"❌ Error getting math operation type: {ex.Message}");
                return "BasicOperations";
            }
        }

        #endregion


        #region Event Argument Classes - Add These if Missing

        // Add this to your MainWindow.xaml.cs class

        /// <summary>
        /// Notify mathematics panel when timebase changes
        /// Call this whenever the timebase is changed
        /// </summary>
        private void NotifyMathematicsPanelTimebaseChanged(double newTimebaseSeconds)
        {
            try
            {
                // If mathematics window is open, update its timebase
                if (_mathematicsWindow?.IsVisible == true)
                {
                    _mathematicsWindow.MathPanel.UpdateTimebase(newTimebaseSeconds);
                    Log($"📐 Mathematics panel timebase updated: {newTimebaseSeconds * 1000:F1}ms");
                }
            }
            catch (Exception ex)
            {
                Log($"⚠️ Error updating mathematics panel timebase: {ex.Message}");
            }
        }



        #endregion







    }
}