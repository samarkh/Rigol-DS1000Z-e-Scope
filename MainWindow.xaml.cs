using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;

namespace Rigol_DS1000Z_E_Control
{
    public partial class MainWindow : Window
    {
      #region Private Fields
       
        private RigolDS1000ZE oscilloscope;
        private Ch1Controller ch1Controller;
        private Ch2Controller ch2Controller;
        private bool isConnected = false;
        // TODO: Add Ch2Controller when implementing Channel 2 functionality
       
        #endregion

      #region Constructor and Initialization
        public MainWindow()
        {
            InitializeComponent();

            // Initialize the oscilloscope object
            oscilloscope = new RigolDS1000ZE();
            oscilloscope.LogEvent += Oscilloscope_LogEvent;

            // Initialize Channel 1 controller
            InitializeChannel1Controller();

            // Initialize Channel 2 controller
            InitializeChannel2Controller();

            Log("Application started. Ready to connect to Rigol DS1000Z-E.");
        }

        /// <summary>
        /// Initialize the Channel 1 controller and wire up UI controls
        /// </summary>
        private void InitializeChannel1Controller()
        {
            ch1Controller = new Ch1Controller(oscilloscope);
            ch1Controller.LogEvent += (sender, message) => Log(message);
            ch1Controller.SettingsChanged += (sender, e) => Log("Channel 1 settings changed");

            // Wire up UI controls to the controller
            ch1Controller.EnableCheckBox = Channel1EnableCheckBox;
            ch1Controller.ProbeRatioComboBox = ProbeRatioComboBox;
            ch1Controller.VerticalScaleComboBox = VerticalScaleComboBox;
            ch1Controller.UnitsComboBox = UnitsComboBox;
            ch1Controller.CurrentSettingsTextBlock = CurrentSettingsText;
            ch1Controller.VerticalOffsetSlider = VerticalOffsetSlider;
            ch1Controller.SliderValueText = SliderValueText;

            // Initialize the controller
            ch1Controller.InitializeControls();
        }


        /// <summary>
        /// Initialize the Channel 2 controller and wire up UI controls
        /// </summary>
        private void InitializeChannel2Controller()
        {
            ch2Controller = new Ch2Controller(oscilloscope);
            ch2Controller.LogEvent += (sender, message) => Log(message);
            ch2Controller.SettingsChanged += (sender, e) => Log("Channel 2 settings changed");

            // Wire up UI controls to the controller
            ch2Controller.EnableCheckBox = Channel2EnableCheckBox;
            ch2Controller.ProbeRatioComboBox = ProbeRatioComboBox2;
            ch2Controller.VerticalScaleComboBox = VerticalScaleComboBox2;
            ch2Controller.UnitsComboBox = UnitsComboBox2;
            ch2Controller.CurrentSettingsTextBlock = CurrentSettingsText2;
            ch2Controller.VerticalOffsetSlider = VerticalOffsetSlider2;
            ch2Controller.SliderValueText = SliderValueText2;

            // Initialize the controller
            ch2Controller.InitializeControls();
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

                    // Query initial channel settings through the controller
                    ch1Controller.QueryAndUpdateSettings();
                    ch2Controller.QueryAndUpdateSettings();

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
                Channel1Controls.IsEnabled = true;
                Channel2Controls.IsEnabled = true;
            }
            else
            {
                StatusText.Text = "Status: Disconnected";
                StatusText.Foreground = Brushes.Red;
                ConnectButton.Content = "Connect";
                Channel1Controls.IsEnabled = false;
                Channel2Controls.IsEnabled = false;
            }
        }
        #endregion

      #region Channel 1 Event Handlers (Delegated to Controller)

        /// <summary>
        /// All Channel 1 event handlers forward to the Ch1Controller
        /// These methods are kept for XAML binding compatibility
        /// </summary>
        private void Channel1Enable_Changed(object sender, RoutedEventArgs e)
        {
            // Ch1Controller handles this through its own event handlers
        }

        private void ProbeRatio_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Ch1Controller handles this through its own event handlers
        }

        private void VerticalScale_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Ch1Controller handles this through its own event handlers
        }

        private void Units_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Ch1Controller handles this through its own event handlers
        }

        private void VerticalOffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Forward to Ch1Controller - all logic is handled there
            ch1Controller?.HandleVerticalOffsetChanged(e.NewValue);
        }
        #endregion

      #region Channel 2 Event Handlers (Now Fully Implemented)
        /// <summary>
        /// Channel 2 event handlers - now forwarding to Ch2Controller
        /// </summary>
        private void Channel2Enable_Changed(object sender, RoutedEventArgs e)
        {
            // Ch2Controller handles this through its own event handlers
        }

        private void ProbeRatio2_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Ch2Controller handles this through its own event handlers
        }

        private void VerticalScale2_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Ch2Controller handles this through its own event handlers
        }

        private void Units2_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Ch2Controller handles this through its own event handlers
        }

        private void VerticalOffsetSlider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Forward to Ch2Controller - all logic is handled there
            ch2Controller?.HandleVerticalOffsetChanged(e.NewValue);
        }
        #endregion

      #region Menu Commands and Presets (Extended for Channel 1)
        /// <summary>
        /// Apply a preset configuration to Channel 1
        /// </summary>
        private void ApplyChannel1Preset(Ch1Settings preset)
        {
            if (isConnected)
            {
                ch1Controller.SetSettings(preset);
                Log($"Applied Channel 1 preset: {preset}");

            }
        }

        /// <summary>
        /// Example: Apply general purpose preset
        /// </summary>
        private void ApplyGeneralPurposePreset()
        {
            ApplyChannel1Preset(Ch1Settings.Presets.GeneralPurpose);
        }

        /// <summary>
        /// Example: Apply small signal preset
        /// </summary>
        private void ApplySmallSignalPreset()
        {
            ApplyChannel1Preset(Ch1Settings.Presets.SmallSignal);
        }

        /// <summary>
        /// Get current Channel 1 settings for debugging
        /// </summary>
        private void LogCurrentChannel1Settings()
        {
            if (isConnected)
            {
                var settings = ch1Controller.GetSettings();
                Log($"Current Channel 1 settings: {settings}");
            }
        }
        #endregion

      #region Menu Commands and Presets (Extended for Channel 2)
        /// <summary>
        /// Apply a preset configuration to Channel 2
        /// </summary>
        private void ApplyChannel2Preset(Ch2Settings preset)
        {
            if (isConnected)
            {
                ch2Controller.SetSettings(preset);
                Log($"Applied Channel 2 preset: {preset}");
            }
        }

        /// <summary>
        /// Example: Apply dual channel preset for differential measurements
        /// </summary>
        private void ApplyDualChannelPreset()
        {
            if (isConnected)
            {
                // Set both channels to same scale for comparison
                var ch1Preset = Ch1Settings.Presets.GeneralPurpose;
                var ch2Preset = Ch2Settings.Presets.GeneralPurpose;

                ch1Controller.SetSettings(ch1Preset);
                ch2Controller.SetSettings(ch2Preset);

                Log("Applied dual channel preset for differential measurements");
            }
        }

        /// <summary>
        /// Get current Channel 2 settings for debugging
        /// </summary>
        private void LogCurrentChannel2Settings()
        {
            if (isConnected)
            {
                var settings = ch2Controller.GetSettings();
                Log($"Current Channel 2 settings: {settings}");
            }
        }

        /// <summary>
        /// Log settings for both channels
        /// </summary>
        private void LogAllChannelSettings()
        {
            if (isConnected)
            {
                LogCurrentChannel1Settings();
                LogCurrentChannel2Settings();
            }
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
                // UI not ready yet, could store messages in a buffer or just return
                return;
            }

            try
            {
                Dispatcher.Invoke(() =>
                {
                    // Double-check in case UI was disposed between the outer check and this invoke
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
                // If logging fails, we don't want to crash the application
                System.Diagnostics.Debug.WriteLine($"Logging failed: {ex.Message}");
            }
        }
        #endregion

      #region Cleanup
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Clean up resources
            ch1Controller?.Dispose();
            ch2Controller?.Dispose(); // Add this line

            // Ensure proper cleanup
            if (isConnected)
            {
                oscilloscope.Disconnect();
            }
        }
        #endregion
    }
}