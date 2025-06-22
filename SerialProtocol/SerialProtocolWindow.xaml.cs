using Microsoft.Win32;
using System;
using System.Data;
using System.IO;
using System.Text.Json; // Using System.Text.Json consistently
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DS1000Z_E_USB_Control.SerialProtocol
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
            if (ProtocolPanel != null)
            {
                ProtocolPanel.SCPICommandGenerated += OnProtocolPanelSCPICommand;
            }

            // Set initial status
            UpdateStatus("Serial Protocol Analysis window opened");

            // Optional: Set window icon if you have one
            try
            {
                // Replace with your actual icon path
                this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/protocol_icon.ico"));
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
                if (ProtocolPanel != null)
                {
                    ProtocolPanel.ResetToDefaults();
                    UpdateStatus("Configuration reset to defaults");
                }
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
                    // Get current settings from the panel
                    var settings = GetCurrentSettings();

                    // Use System.Text.Json consistently
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    string json = JsonSerializer.Serialize(settings, options);
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
                    var settings = JsonSerializer.Deserialize<DecoderSettings>(json);

                    // Apply loaded settings to the panel
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

        #region Helper Methods

        private void UpdateStatus(string message)
        {
            if (StatusText != null)
            {
                StatusText.Text = message;
            }

            // Update timestamp
            if (TimestampText != null)
            {
                TimestampText.Text = $"Last Updated: {DateTime.Now:HH:mm:ss}";
            }
        }

        private DecoderSettings GetCurrentSettings()
        {
            // Extract current settings from the protocol panel
            if (ProtocolPanel != null)
            {
                return new DecoderSettings
                {
                    ProtocolType = ProtocolPanel.GetCurrentProtocolType(),
                    Enabled = ProtocolPanel.DecoderEnabled,
                    EventTable = new EventTableSettings
                    {
                        Enabled = ProtocolPanel.TableEnabled
                    }
                    // Add other settings as needed
                };
            }

            return new DecoderSettings(); // Default settings
        }

        private void ApplySettingsToPanel(DecoderSettings settings)
        {
            // Apply loaded settings to the protocol panel
            if (ProtocolPanel != null && settings != null)
            {
                try
                {
                    ProtocolPanel.SetProtocolType(settings.ProtocolType ?? "UART");
                    ProtocolPanel.SetDecoderEnabled(settings.Enabled);

                    if (settings.EventTable != null)
                    {
                        ProtocolPanel.SetEventTableEnabled(settings.EventTable.Enabled);
                    }

                    UpdateStatus("Settings applied to panel");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error applying settings: {ex.Message}");
                }
            }
        }

        public void UpdateConnectionStatus(bool connected)
        {
            if (ConnectionStatus != null)
            {
                ConnectionStatus.Text = connected ? "Connected" : "Disconnected";
                ConnectionStatus.Foreground = connected ?
                    System.Windows.Media.Brushes.Green :
                    System.Windows.Media.Brushes.Red;
            }
        }

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
            if (ProtocolPanel != null)
            {
                return ProtocolPanel.GetConfigurationSummary();
            }
            return "No configuration available";
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Class to hold decoder configuration settings
    /// </summary>
    public class DecoderSettings
    {
        public string ProtocolType { get; set; } = "UART";
        public bool Enabled { get; set; } = false;
        public EventTableSettings EventTable { get; set; } = new EventTableSettings();
        public ThresholdSettings Thresholds { get; set; } = new ThresholdSettings();
        public string Position { get; set; } = "0";
    }

    /// <summary>
    /// Event table configuration
    /// </summary>
    public class EventTableSettings
    {
        public bool Enabled { get; set; } = false;
        public string Format { get; set; } = "HEX";
    }

    /// <summary>
    /// Threshold settings for different channels
    /// </summary>
    public class ThresholdSettings
    {
        public double Channel1 { get; set; } = 1.5;
        public double Channel2 { get; set; } = 1.5;
        public bool AutoThreshold { get; set; } = true;
    }

    #endregion
}