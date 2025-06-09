using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rigol_DS1000Z_E_Control
{
    public partial class MainWindow : Window
    {
        private RigolDS1000ZE oscilloscope;
        private bool isConnected = false;
        private bool isUpdatingUI = false; // Prevent recursive updates

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the oscilloscope object
            oscilloscope = new RigolDS1000ZE();
            oscilloscope.LogEvent += Oscilloscope_LogEvent;

            // Initialize vertical scale options
            UpdateVerticalScaleOptions();

            Log("Application started. Ready to connect to Rigol DS1000Z-E.");
        }

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

                    // Query initial channel settings
                    QueryAndUpdateCurrentSettings();
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

        private void Channel1Enable_Changed(object sender, RoutedEventArgs e)
        {
            if (!isConnected || isUpdatingUI) return;

            bool enabled = Channel1EnableCheckBox.IsChecked ?? false;
            string command = $":CHANnel1:DISPlay {(enabled ? "ON" : "OFF")}";

            if (oscilloscope.SendCommand(command))
            {
                Log($"Channel 1 {(enabled ? "enabled" : "disabled")}");
            }
            else
            {
                Log("Failed to change channel 1 enable state");
                // Revert the checkbox state
                isUpdatingUI = true;
                Channel1EnableCheckBox.IsChecked = !enabled;
                isUpdatingUI = false;
            }
        }

        private void ProbeRatio_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!isConnected || isUpdatingUI) return;

            var selectedItem = ProbeRatioComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string probeRatio = selectedItem.Tag.ToString();
                string command = $":CHANnel1:PROBe {probeRatio}";

                if (oscilloscope.SendCommand(command))
                {
                    Log($"Probe ratio set to {selectedItem.Content}");
                    UpdateVerticalScaleOptions();
                    QueryAndUpdateCurrentSettings();
                }
                else
                {
                    Log("Failed to set probe ratio");
                }
            }
        }
        private void VerticalScale_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!isConnected || isUpdatingUI) return;

            var selectedItem = VerticalScaleComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string scale = selectedItem.Tag.ToString();
                string command = $":CHANnel1:SCALe {scale}";

                if (oscilloscope.SendCommand(command))
                {
                    Log($"Vertical scale set to {selectedItem.Content}");
                    QueryAndUpdateCurrentSettings();
                }
                else
                {
                    Log("Failed to set vertical scale");
                }
            }
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
                string command = $":CHANnel1:OFFSet {offset}";

                if (oscilloscope.SendCommand(command))
                {
                    Log($"Vertical offset set to {offset}V");
                    QueryAndUpdateCurrentSettings();
                }
                else
                {
                    Log("Failed to set vertical offset");
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid numeric value for offset.", "Invalid Input",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Units_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!isConnected || isUpdatingUI) return;

            var selectedItem = UnitsComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string units = selectedItem.Tag.ToString();
                string command = $":CHANnel1:UNITs {units}";

                if (oscilloscope.SendCommand(command))
                {
                    Log($"Display units set to {selectedItem.Content}");
                    QueryAndUpdateCurrentSettings();
                }
                else
                {
                    Log("Failed to set display units");
                }
            }
        }

        private void UpdateVerticalScaleOptions()
        {
            var selectedProbe = ProbeRatioComboBox.SelectedItem as ComboBoxItem;
            if (selectedProbe == null) return;

            double probeRatio = double.Parse(selectedProbe.Tag.ToString(), CultureInfo.InvariantCulture);

            isUpdatingUI = true;
            VerticalScaleComboBox.Items.Clear();

            List<(double value, string display)> scaleOptions;

            if (probeRatio == 1.0)
            {
                // 1X probe: 1mV to 10V
                scaleOptions = new List<(double, string)>
                {
                    (0.001, "1 mV/div"),
                    (0.002, "2 mV/div"),
                    (0.005, "5 mV/div"),
                    (0.01, "10 mV/div"),
                    (0.02, "20 mV/div"),
                    (0.05, "50 mV/div"),
                    (0.1, "100 mV/div"),
                    (0.2, "200 mV/div"),
                    (0.5, "500 mV/div"),
                    (1.0, "1 V/div"),
                    (2.0, "2 V/div"),
                    (5.0, "5 V/div"),
                    (10.0, "10 V/div")
                };
            }
            else
            {
                // 10X probe (and others): 10mV to 100V
                scaleOptions = new List<(double, string)>
                {
                    (0.01, "10 mV/div"),
                    (0.02, "20 mV/div"),
                    (0.05, "50 mV/div"),
                    (0.1, "100 mV/div"),
                    (0.2, "200 mV/div"),
                    (0.5, "500 mV/div"),
                    (1.0, "1 V/div"),
                    (2.0, "2 V/div"),
                    (5.0, "5 V/div"),
                    (10.0, "10 V/div"),
                    (20.0, "20 V/div"),
                    (50.0, "50 V/div"),
                    (100.0, "100 V/div")
                };
            }

            foreach (var option in scaleOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = option.display,
                    Tag = option.value.ToString(CultureInfo.InvariantCulture)
                };
                VerticalScaleComboBox.Items.Add(item);
            }

            // Select 1V/div as default
            foreach (ComboBoxItem item in VerticalScaleComboBox.Items)
            {
                if (item.Tag.ToString() == "1")
                {
                    VerticalScaleComboBox.SelectedItem = item;
                    break;
                }
            }

            isUpdatingUI = false;
        }
        private void QueryAndUpdateCurrentSettings()
        {
            if (!isConnected) return;

            try
            {
                // Query current settings
                string enableState = oscilloscope.SendQuery(":CHANnel1:DISPlay?");
                string probeRatio = oscilloscope.SendQuery(":CHANnel1:PROBe?");
                string verticalScale = oscilloscope.SendQuery(":CHANnel1:SCALe?");
                string verticalOffset = oscilloscope.SendQuery(":CHANnel1:OFFSet?");
                string units = oscilloscope.SendQuery(":CHANnel1:UNITs?");

                isUpdatingUI = true;

                // Initialize variables with default values
                double scale = 1.0;
                double offset = 0.0;

                // Update enable state
                if (!string.IsNullOrEmpty(enableState))
                {
                    Channel1EnableCheckBox.IsChecked = enableState.Trim() == "1";
                }

                // Update probe ratio
                if (!string.IsNullOrEmpty(probeRatio) && double.TryParse(probeRatio, NumberStyles.Float, CultureInfo.InvariantCulture, out double probe))
                {
                    foreach (ComboBoxItem item in ProbeRatioComboBox.Items)
                    {
                        if (double.TryParse(item.Tag.ToString(), out double itemProbe) && Math.Abs(itemProbe - probe) < 0.001)
                        {
                            ProbeRatioComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update vertical scale options based on current probe ratio
                UpdateVerticalScaleOptions();

                // Update vertical scale
                if (!string.IsNullOrEmpty(verticalScale) && double.TryParse(verticalScale, NumberStyles.Float, CultureInfo.InvariantCulture, out scale))
                {
                    foreach (ComboBoxItem item in VerticalScaleComboBox.Items)
                    {
                        if (double.TryParse(item.Tag.ToString(), out double itemScale) && Math.Abs(itemScale - scale) < 0.0001)
                        {
                            VerticalScaleComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update vertical offset
                if (!string.IsNullOrEmpty(verticalOffset) && double.TryParse(verticalOffset, NumberStyles.Float, CultureInfo.InvariantCulture, out offset))
                {
                    VerticalOffsetTextBox.Text = offset.ToString("F3", CultureInfo.InvariantCulture);
                }

                // Update units
                if (!string.IsNullOrEmpty(units))
                {
                    string unitsUpper = units.Trim().ToUpper();
                    foreach (ComboBoxItem item in UnitsComboBox.Items)
                    {
                        if (item.Tag.ToString().ToUpper() == unitsUpper)
                        {
                            UnitsComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update current settings display
                double range = scale * 8; // 8 divisions
                CurrentSettingsText.Text = $"Current: Scale={scale:F3}V/div, Offset={offset:F3}V, Range={range:F1}V";

                isUpdatingUI = false;

                Log("Channel 1 settings updated from oscilloscope");
            }
            catch (Exception ex)
            {
                Log($"Error querying current settings: {ex.Message}");
                isUpdatingUI = false;
            }
        }


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

            // Ensure proper cleanup
            if (isConnected)
            {
                oscilloscope.Disconnect();
            }
        }
    }
}