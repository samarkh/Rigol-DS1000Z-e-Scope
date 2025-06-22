using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DS1000Z_E_USB_Control.SerialProtocol
{
    /// <summary>
    /// SerialProtocolWINDOW - The container window that hosts the SerialProtocolPANEL
    /// Handles window-level functionality like save/load, menus, and status
    /// </summary>
    public partial class SerialProtocolWINDOW : Window
    {
        #region Events
        // Event to forward SCPI commands to main application
        public event EventHandler<string> SCPICommandGenerated;
        #endregion

        #region Constructor
        public SerialProtocolWINDOW()
        {
            InitializeComponent();
            InitializeWindow();
        }
        #endregion

        #region Initialization
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
                // Replace with your actual icon path if available
                this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/protocol_icon.ico"));
            }
            catch
            {
                // Icon not found, continue without it
                // You can remove this try-catch if you don't have an icon
            }
        }
        #endregion

        #region Event Handlers

        /// <summary>
        /// Forwards SCPI commands from the panel to the main application
        /// </summary>
        private void OnProtocolPanelSCPICommand(object sender, string command)
        {
            // Forward the command to the main application
            SCPICommandGenerated?.Invoke(this, command);

            // Update status to show last command sent
            string commandName = command.Split(' ')[0];
            UpdateStatus($"Command sent: {commandName}");
        }

        /// <summary>
        /// Reset all decoder settings to defaults
        /// </summary>
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all decoder settings to defaults?\n\n" +
                "This will:\n" +
                "• Reset all protocol configurations\n" +
                "• Clear the command log\n" +
                "• Return to UART mode with default settings",
                "Reset Configuration",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                if (ProtocolPanel != null)
                {
                    ProtocolPanel.ResetToDefaults();
                    UpdateStatus("Configuration reset to defaults");
                }
            }
        }

        /// <summary>
        /// Save current decoder configuration to file
        /// </summary>
        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Title = "Save Decoder Configuration",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                AddExtension = true,
                FileName = $"SerialProtocol_Config_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // Get current settings from the protocol panel
                    var settings = GetCurrentSettings();

                    // Serialize to JSON with pretty formatting
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    string jsonString = JsonSerializer.Serialize(settings, options);
                    File.WriteAllText(saveDialog.FileName, jsonString);

                    MessageBox.Show($"Configuration saved successfully to:\n{saveDialog.FileName}",
                                  "Save Complete",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);

                    UpdateStatus($"Configuration saved to {Path.GetFileName(saveDialog.FileName)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving configuration:\n{ex.Message}",
                                  "Save Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                    UpdateStatus("Error saving configuration");
                }
            }
        }

        /// <summary>
        /// Load decoder configuration from file
        /// </summary>
        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Title = "Load Decoder Configuration",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                CheckFileExists = true
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    string jsonString = File.ReadAllText(openDialog.FileName);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    };

                    var settings = JsonSerializer.Deserialize<DecoderSettings>(jsonString, options);

                    // Apply settings to the protocol panel
                    ApplySettingsToPanel(settings);

                    MessageBox.Show($"Configuration loaded successfully from:\n{openDialog.FileName}",
                                  "Load Complete",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);

                    UpdateStatus($"Configuration loaded from {Path.GetFileName(openDialog.FileName)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading configuration:\n{ex.Message}",
                                  "Load Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                    UpdateStatus("Error loading configuration");
                }
            }
        }

        /// <summary>
        /// Show help information about protocol analysis
        /// </summary>
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            string helpText = @"Serial Protocol Analysis Help

DECODER CONFIGURATION:
• Select decoder number (1 or 2)
• Choose protocol type: UART, I²C, SPI, or Parallel
• Configure protocol-specific settings
• Set threshold levels for digital signals

UART CONFIGURATION:
• TX/RX Channels: Assign oscilloscope channels
• Baud Rate: 1200 to 115200 bps
• Data Width: 5-9 bits
• Stop Bits: 1 or 2 bits
• Parity: None, Even, or Odd

I²C CONFIGURATION:
• Clock/Data Channels: Assign SCL and SDA
• Address Type: 7-bit, 8-bit, or 10-bit

SPI CONFIGURATION:
• Clock, MISO, MOSI, CS channel assignments
• Clock polarity and edge settings
• Data width and endianness

EVENT TABLE:
• Toggle event table display
• Configure data format and view options
• Export decoded data to CSV files

SCPI COMMANDS:
• All actions generate SCPI commands
• Commands are logged and sent to oscilloscope
• Use Save/Load to preserve configurations

TIPS:
• Adjust threshold levels for reliable decoding
• Use the correct protocol settings for your signals
• Export data for offline analysis";

            MessageBox.Show(helpText,
                          "Protocol Analysis Help",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        /// <summary>
        /// Window loaded event handler
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Serial Protocol Analysis ready");
        }

        #endregion

        #region Window Events

        /// <summary>
        /// Clean up when window is closed
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events to prevent memory leaks
            if (ProtocolPanel != null)
            {
                ProtocolPanel.SCPICommandGenerated -= OnProtocolPanelSCPICommand;
            }

            base.OnClosed(e);
        }

        /// <summary>
        /// Handle window closing - could add confirmation if needed
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Optional: Add confirmation dialog if unsaved changes
            // For now, just close normally
            base.OnClosing(e);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Update the status bar with current information
        /// </summary>
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

        /// <summary>
        /// Get current decoder settings from the protocol panel
        /// </summary>
        private DecoderSettings GetCurrentSettings()
        {
            if (ProtocolPanel != null)
            {
                return new DecoderSettings
                {
                    Version = "1.0",
                    Timestamp = DateTime.Now,
                    ProtocolType = ProtocolPanel.GetCurrentProtocolType(),
                    DecoderNumber = ProtocolPanel.GetDecoderNumber(),
                    Enabled = ProtocolPanel.DecoderEnabled,
                    DisplayFormat = ProtocolPanel.GetDisplayFormat(),
                    VerticalPosition = ProtocolPanel.GetVerticalPosition(),
                    Thresholds = new ThresholdSettings
                    {
                        Channel1 = ProtocolPanel.GetChannel1Threshold(),
                        Channel2 = ProtocolPanel.GetChannel2Threshold()
                    },
                    EventTable = new EventTableSettings
                    {
                        Enabled = ProtocolPanel.TableEnabled,
                        Format = ProtocolPanel.GetTableFormat(),
                        View = ProtocolPanel.GetTableView(),
                        SortOrder = ProtocolPanel.GetTableSortOrder()
                    },
                    // Add protocol-specific settings based on current protocol
                    UARTSettings = ProtocolPanel.GetUARTSettings(),
                    I2CSettings = ProtocolPanel.GetI2CSettings(),
                    SPISettings = ProtocolPanel.GetSPISettings(),
                    ParallelSettings = ProtocolPanel.GetParallelSettings()
                };
            }

            return new DecoderSettings { Version = "1.0", Timestamp = DateTime.Now };
        }

        /// <summary>
        /// Apply loaded settings to the protocol panel
        /// </summary>
        private void ApplySettingsToPanel(DecoderSettings settings)
        {
            if (ProtocolPanel != null && settings != null)
            {
                try
                {
                    // Apply basic settings
                    ProtocolPanel.SetDecoderNumber(settings.DecoderNumber);
                    ProtocolPanel.SetProtocolType(settings.ProtocolType ?? "UART");
                    ProtocolPanel.SetDisplayFormat(settings.DisplayFormat ?? "HEX");
                    ProtocolPanel.SetVerticalPosition(settings.VerticalPosition);

                    // Apply threshold settings
                    if (settings.Thresholds != null)
                    {
                        ProtocolPanel.SetChannel1Threshold(settings.Thresholds.Channel1);
                        ProtocolPanel.SetChannel2Threshold(settings.Thresholds.Channel2);
                    }

                    // Apply event table settings
                    if (settings.EventTable != null)
                    {
                        ProtocolPanel.TableEnabled = settings.EventTable.Enabled;
                        ProtocolPanel.SetTableFormat(settings.EventTable.Format ?? "HEX");
                        ProtocolPanel.SetTableView(settings.EventTable.View ?? "PACKet");
                        ProtocolPanel.SetTableSortOrder(settings.EventTable.SortOrder ?? "ASCend");
                    }

                    // Apply protocol-specific settings
                    if (settings.UARTSettings != null)
                        ProtocolPanel.ApplyUARTSettings(settings.UARTSettings);

                    if (settings.I2CSettings != null)
                        ProtocolPanel.ApplyI2CSettings(settings.I2CSettings);

                    if (settings.SPISettings != null)
                        ProtocolPanel.ApplySPISettings(settings.SPISettings);

                    if (settings.ParallelSettings != null)
                        ProtocolPanel.ApplyParallelSettings(settings.ParallelSettings);

                    // Set decoder enabled state
                    ProtocolPanel.DecoderEnabled = settings.Enabled;

                    UpdateStatus("Configuration applied successfully");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error applying configuration: {ex.Message}");
                    throw; // Re-throw so the calling method can handle it
                }
            }
        }

        #endregion

        #region Public Interface Methods

        /// <summary>
        /// Public method to allow external access to panel functionality
        /// </summary>
        public void ResetConfiguration()
        {
            ProtocolPanel?.ResetToDefaults();
            UpdateStatus("Configuration reset externally");
        }

        /// <summary>
        /// Public method to get current decoder state
        /// </summary>
        public bool IsDecoderEnabled()
        {
            return ProtocolPanel?.DecoderEnabled ?? false;
        }

        /// <summary>
        /// Public method to get current protocol type
        /// </summary>
        public string GetCurrentProtocol()
        {
            return ProtocolPanel?.GetCurrentProtocolType() ?? "UART";
        }

        #endregion
    }

    #region Data Classes for Settings

    /// <summary>
    /// Complete decoder settings data structure for save/load functionality
    /// </summary>
    public class DecoderSettings
    {
        public string Version { get; set; } = "1.0";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string ProtocolType { get; set; } = "UART";
        public int DecoderNumber { get; set; } = 1;
        public bool Enabled { get; set; } = false;
        public string DisplayFormat { get; set; } = "HEX";
        public double VerticalPosition { get; set; } = 0.0;
        public ThresholdSettings Thresholds { get; set; } = new ThresholdSettings();
        public EventTableSettings EventTable { get; set; } = new EventTableSettings();
        public UARTSettings UARTSettings { get; set; } = new UARTSettings();
        public I2CSettings I2CSettings { get; set; } = new I2CSettings();
        public SPISettings SPISettings { get; set; } = new SPISettings();
        public ParallelSettings ParallelSettings { get; set; } = new ParallelSettings();
    }

    public class ThresholdSettings
    {
        public double Channel1 { get; set; } = 1.4;
        public double Channel2 { get; set; } = 1.4;
    }

    public class EventTableSettings
    {
        public bool Enabled { get; set; } = false;
        public string Format { get; set; } = "HEX";
        public string View { get; set; } = "PACKet";
        public string SortOrder { get; set; } = "ASCend";
        public string Column { get; set; } = "TIME";
        public int CurrentRow { get; set; } = 1;
    }

    public class UARTSettings
    {
        public string TxChannel { get; set; } = "CHANnel1";
        public string RxChannel { get; set; } = "CHANnel2";
        public string BaudRate { get; set; } = "9600";
        public string DataWidth { get; set; } = "8";
        public string StopBits { get; set; } = "1";
        public string Parity { get; set; } = "NONE";
        public string Polarity { get; set; } = "POS";
        public string Endian { get; set; } = "LSB";
    }

    public class I2CSettings
    {
        public string ClockChannel { get; set; } = "CHANnel1";
        public string DataChannel { get; set; } = "CHANnel2";
        public string AddressType { get; set; } = "ADDR7";
    }

    public class SPISettings
    {
        public string ClockChannel { get; set; } = "CHANnel1";
        public string MisoChannel { get; set; } = "CHANnel2";
        public string MosiChannel { get; set; } = "CHANnel1";
        public string CsChannel { get; set; } = "CHANnel1";
        public string DataWidth { get; set; } = "8";
        public string Polarity { get; set; } = "POSitive";
        public string Edge { get; set; } = "POSitive";
        public string Endian { get; set; } = "LSB";
    }

    public class ParallelSettings
    {
        public string ClockChannel { get; set; } = "CHANnel1";
        public string Edge { get; set; } = "POSitive";
        public string DataWidth { get; set; } = "8";
    }

    #endregion
}