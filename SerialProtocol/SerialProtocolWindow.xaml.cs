using Microsoft.Win32;
using System.Text.Json;  // ✅ Built-in .NET library
using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows;
using System.Xml;

namespace DS1000Z_E.SerialProtocol
{
    public partial class SerialProtocolWindow : Window
    {
        // Event to forward SCPI commands to main application
        public event EventHandler<string> SCPICommandGenerated;

        public SerialProtocolWindow()
        {
            InitializeComponent();
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            // Subscribe to the protocol panel's SCPI command events
            ProtocolPanel.SCPICommandGenerated += OnProtocolPanelSCPICommand;

            // Set initial status
            UpdateStatus("Serial Protocol Analysis window opened");

            // Optional: Set window icon if you have one
            try
            {
                // Replace with your actual icon path
                // this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/protocol_icon.ico"));
            }
            catch
            {
                // Icon not found, continue without it
            }
        }

        #region Event Handlers

        private void OnProtocolPanelSCPICommand(object sender, string command)
        {
            // Forward the command to the main application
            SCPICommandGenerated?.Invoke(this, command);

            // Update status
            UpdateStatus($"Command sent: {command.Split(' ')[0]}");
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all decoder settings to defaults?",
                "Reset Configuration",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ProtocolPanel.ResetToDefaults();
                UpdateStatus("Configuration reset to defaults");
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Title = "Save Decoder Configuration",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = "DecoderConfig.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var settings = new DecoderSettings(); // You might want to get current settings from the panel
                    string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                    File.WriteAllText(saveDialog.FileName, json);

                    UpdateStatus($"Configuration saved to {Path.GetFileName(saveDialog.FileName)}");
                    MessageBox.Show("Configuration saved successfully!", "Save Complete",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving configuration: {ex.Message}", "Save Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus("Error saving configuration");
                }
            }
        }

        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Title = "Load Decoder Configuration",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(openDialog.FileName);
                    var settings = JsonConvert.DeserializeObject<DecoderSettings>(json);

                    // Apply loaded settings to the panel
                    // You might need to add a method to apply settings to the panel
                    ApplySettingsToPanel(settings);

                    UpdateStatus($"Configuration loaded from {Path.GetFileName(openDialog.FileName)}");
                    MessageBox.Show("Configuration loaded successfully!", "Load Complete",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading configuration: {ex.Message}", "Load Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus("Error loading configuration");
                }
            }
        }

        #endregion

        #region Helper Methods

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;

            // Update timestamp
            TimestampText.Text = $"Last Updated: {DateTime.Now:HH:mm:ss}";
        }

        private void ApplySettingsToPanel(DecoderSettings settings)
        {
            // This method would apply the loaded settings to the protocol panel
            // You might need to add public methods to SerialProtocolPanel for this

            try
            {
                ProtocolPanel.SetProtocolType(settings.ProtocolType.ToString());
                ProtocolPanel.SetDecoderEnabled(settings.Enabled);
                ProtocolPanel.SetEventTableEnabled(settings.EventTable.Enabled);

                // Apply other settings as needed
                UpdateStatus("Settings applied to panel");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error applying settings: {ex.Message}");
            }
        }

        public void UpdateConnectionStatus(bool connected)
        {
            ConnectionStatus.Text = connected ? "Connected" : "Disconnected";
            ConnectionStatus.Foreground = connected ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
        }

        #endregion

        #region Window Events

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            if (ProtocolPanel != null)
            {
                ProtocolPanel.SCPICommandGenerated -= OnProtocolPanelSCPICommand;
            }

            base.OnClosed(e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Serial Protocol Analysis ready");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Method to be called from main application when connection status changes
        /// </summary>
        public void SetConnectionStatus(bool connected, string deviceInfo = "")
        {
            UpdateConnectionStatus(connected);

            if (connected && !string.IsNullOrEmpty(deviceInfo))
            {
                UpdateStatus($"Connected to {deviceInfo}");
            }
            else if (!connected)
            {
                UpdateStatus("Disconnected from oscilloscope");
            }
        }

        /// <summary>
        /// Method to get current configuration summary
        /// </summary>
        public string GetConfigurationSummary()
        {
            return ProtocolPanel.GetConfigurationSummary();
        }

        #endregion
    }
}