using System;
using System.Windows;
using System.Windows.Media;
using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;

namespace Rigol_DS1000Z_E_Control
{
    /// <summary>
    /// Simplified MainWindow using Channel UserControls
    /// Much cleaner and more maintainable architecture
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Fields
        private RigolDS1000ZE oscilloscope;
        private bool isConnected = false;
        #endregion

        #region Constructor and Initialization
        public MainWindow()
        {
            InitializeComponent();

            // Initialize the oscilloscope object
            oscilloscope = new RigolDS1000ZE();
            oscilloscope.LogEvent += Oscilloscope_LogEvent;

            // Initialize both channel panels
            InitializeChannelPanels();

            Log("Application started. Ready to connect to Rigol DS1000Z-E.");
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
                    }

                    // Query initial channel settings for both channels
                    Channel1Panel?.QueryAndUpdateSettings();
                    Channel2Panel?.QueryAndUpdateSettings();

                    Log("Both channels initialized and settings synchronized");
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

                // Enable both channel panels
                Channel1Panel?.SetEnabled(true);
                Channel2Panel?.SetEnabled(true);
            }
            else
            {
                StatusText.Text = "Status: Disconnected";
                StatusText.Foreground = Brushes.Red;
                ConnectButton.Content = "Connect";

                // Disable both channel panels
                Channel1Panel?.SetEnabled(false);
                Channel2Panel?.SetEnabled(false);
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

        #region Preset Menu Methods (for future menu implementation)
        /// <summary>
        /// These methods can be connected to menu items or toolbar buttons
        /// </summary>
        public void ApplyGeneralPurposePresets()
        {
            ApplyChannel1Preset(Ch1Settings.Presets.GeneralPurpose);
            ApplyChannel2Preset(Ch2Settings.Presets.GeneralPurpose);
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
                        string timestamp = DateTime.Now.ToString("HH:mm:ss");
                        LogTextBox.AppendText($"[{timestamp}] {message}\n");
                        LogTextBox.ScrollToEnd();
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