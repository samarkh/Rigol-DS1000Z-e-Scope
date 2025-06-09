using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DS1000Z_E_USB_Control.Channels.Ch1;

namespace Rigol_DS1000Z_E_Control
{
    public partial class MainWindow : Window
    {
        private RigolDS1000ZE oscilloscope;
        private Ch1Controller ch1Controller;
        private bool isConnected = false;
        //private Ch1Controller ch1Controller = new Ch1Controller();



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
        //private void InitializeChannel1Controller()
        //{
        //    ch1Controller = new Ch1Controller(oscilloscope);
        //    ch1Controller.LogEvent += (sender, message) => Log(message);
        //    ch1Controller.SettingsChanged += (sender, e) => Log("Channel 1 settings changed");

        //    // Wire up UI controls to the controller
        //    ch1Controller.EnableCheckBox = Channel1EnableCheckBox;
        //    ch1Controller.ProbeRatioComboBox = ProbeRatioComboBox;
        //    ch1Controller.VerticalScaleComboBox = VerticalScaleComboBox;
        //    ch1Controller.VerticalOffsetTextBox = VerticalOffsetTextBox;
        //    ch1Controller.UnitsComboBox = UnitsComboBox;
        //    ch1Controller.CurrentSettingsTextBlock = CurrentSettingsText;

        //    // Initialize the controller
        //    ch1Controller.InitializeControls();
        //}

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
            }
            else
            {
                StatusText.Text = "Status: Disconnected";
                StatusText.Foreground = Brushes.Red;
                ConnectButton.Content = "Connect";
                Channel1Controls.IsEnabled = false;
            }
        }

        #region Channel 1 Event Handlers - Delegated to Controller

        private void Channel1Enable_Changed(object sender, RoutedEventArgs e)
        {
            // The Ch1Controller handles this through its event handlers
            // This method is kept for XAML compatibility but does nothing
        }

        private void ProbeRatio_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // The Ch1Controller handles this through its event handlers
            // This method is kept for XAML compatibility but does nothing
        }

        //private void VerticalScale_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        //{
        //    // The Ch1Controller handles this through its event handlers
        //    // This method is kept for XAML compatibility but does nothing
        //}


        private void VerticalOffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ch1Controller.HandleVerticalOffsetChanged(e.NewValue);
        }


        private void Units_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // The Ch1Controller handles this through its event handlers
            // This method is kept for XAML compatibility but does nothing
        }

        private void VerticalOffset_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SetVerticalOffset_Click(sender, e);
            }
        }

        private void SetVerticalOffset_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            if (double.TryParse(VerticalOffsetTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
            {
                ch1Controller.SetVerticalOffset(offset);
            }
            else
            {
                MessageBox.Show("Please enter a valid numeric value for offset.", "Invalid Input",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Menu Commands (Future Extension Points)

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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Clean up resources
            ch1Controller?.Dispose();

            if (isConnected)
            {
                oscilloscope.Disconnect();
            }
        }
    }
}