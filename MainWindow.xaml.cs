using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DS1000Z_E_USB_Control.Channels.Ch1;

namespace Rigol_DS1000Z_E_Control
{
    public partial class MainWindow : Window
    {
        #region Private Fields
        private RigolDS1000ZE oscilloscope;
        private Ch1Controller ch1Controller;
        private bool isConnected = false;
        // TODO: Add Ch2Controller when implementing Channel 2 functionality
        // private Ch2Controller ch2Controller;
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

        #region Channel 2 Event Handlers (Ready for Ch2Controller)
        /// <summary>
        /// Channel 2 event handlers - ready for Ch2Controller implementation
        /// Currently these are stub methods for XAML binding compatibility
        /// </summary>
        private void Channel2Enable_Changed(object sender, RoutedEventArgs e)
        {
            // TODO: Forward to Ch2Controller when implemented
            // ch2Controller handles this through its own event handlers
            Log("Channel 2 enable changed (Ch2Controller not yet implemented)");
        }

        private void ProbeRatio2_Changed(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Forward to Ch2Controller when implemented
            // ch2Controller handles this through its own event handlers
            Log("Channel 2 probe ratio changed (Ch2Controller not yet implemented)");
        }

        private void VerticalScale2_Changed(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Forward to Ch2Controller when implemented
            // ch2Controller handles this through its own event handlers
            Log("Channel 2 vertical scale changed (Ch2Controller not yet implemented)");
        }

        private void Units2_Changed(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Forward to Ch2Controller when implemented
            // ch2Controller handles this through its own event handlers
            Log("Channel 2 units changed (Ch2Controller not yet implemented)");
        }

        private void VerticalOffsetSlider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // TODO: Forward to Ch2Controller when implemented
            // ch2Controller?.HandleVerticalOffsetChanged(e.NewValue);
            Log($"Channel 2 offset slider changed to {e.NewValue:F3}V (Ch2Controller not yet implemented)");
        }
        #endregion

        #region Menu Commands and Presets (Future Extension Points)
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

        #region Logging and Events
        private void Oscilloscope_LogEvent(object sender, string message)
        {
            Log(message);
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogTextBox.AppendText($"[{timestamp}] {message}\n");
                LogTextBox.ScrollToEnd();
            });
        }
        #endregion

        #region Cleanup
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Clean up resources
            ch1Controller?.Dispose();

            // Ensure proper cleanup
            if (isConnected)
            {
                oscilloscope.Disconnect();
            }
        }
        #endregion
    }
}