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
            var result = MessageBox.Show("Reset all decoder settings to defaults?",
                                       "Confirm Reset",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ProtocolPanel?.ResetToDefaults();
                UpdateStatus("All settings reset to defaults");
            }
        }

        /// <summary>
        /// Save decoder configuration to file
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
            string helpMessage = @"Serial Protocol Analysis Tool

This tool helps you decode and analyze serial communication protocols on your Rigol oscilloscope.

Supported Protocols:
• UART (RS232) - Universal Asynchronous Receiver-Transmitter
• I²C (IIC) - Inter-Integrated Circuit
• SPI - Serial Peripheral Interface  
• Parallel - Parallel bus communication

Features:
• Configure decoder settings for each protocol
• Set threshold levels for accurate decoding
• Customize display format (HEX, ASCII, DEC, BIN)
• Event table for detailed packet analysis
• Save/load configurations for different setups

Tips:
• Ensure proper channel connections match your protocol setup
• Adjust threshold levels for reliable signal detection
• Use the event table to analyze captured packets
• Save frequently used configurations for quick setup";

            MessageBox.Show(helpMessage, "Protocol Analysis Help",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Handle window closing - clean up resources
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
                    ProtocolType = ParseEnum<ProtocolType>(ProtocolPanel.GetCurrentProtocolType()),
                    DecoderNumber = ProtocolPanel.GetDecoderNumber(),
                    Enabled = ProtocolPanel.DecoderEnabled,
                    DisplayFormat = ParseEnum<DisplayFormat>(ProtocolPanel.GetDisplayFormat()),
                    VerticalPosition = (int)ProtocolPanel.GetVerticalPosition(),
                    Thresholds = new ThresholdSettings
                    {
                        Channel1Threshold = ProtocolPanel.GetChannel1Threshold(),
                        Channel2Threshold = ProtocolPanel.GetChannel2Threshold()
                    },
                    EventTable = new EventTableSettings
                    {
                        Enabled = ProtocolPanel.TableEnabled,
                        Format = ParseEnum<DisplayFormat>(ProtocolPanel.GetTableFormat()),
                        ViewMode = ParseEnum<EventTableView>(ProtocolPanel.GetTableView()),
                        SortOrder = ParseEnum<SortOrder>(ProtocolPanel.GetTableSortOrder())
                    },
                    // Fixed: Use correct property names (UART, I2C, SPI, Parallel)
                    UART = ProtocolPanel.GetUARTSettings(),
                    I2C = ProtocolPanel.GetI2CSettings(),
                    SPI = ProtocolPanel.GetSPISettings(),
                    Parallel = ProtocolPanel.GetParallelSettings()
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
                    ProtocolPanel.SetProtocolType(settings.ProtocolType.ToString());
                    ProtocolPanel.SetDisplayFormat(settings.DisplayFormat.ToString());
                    ProtocolPanel.SetVerticalPosition(settings.VerticalPosition);

                    // Apply threshold settings
                    if (settings.Thresholds != null)
                    {
                        ProtocolPanel.SetChannel1Threshold(settings.Thresholds.Channel1Threshold);
                        ProtocolPanel.SetChannel2Threshold(settings.Thresholds.Channel2Threshold);
                    }

                    // Apply event table settings
                    if (settings.EventTable != null)
                    {
                        ProtocolPanel.TableEnabled = settings.EventTable.Enabled;
                        ProtocolPanel.SetTableFormat(settings.EventTable.Format.ToString());
                        ProtocolPanel.SetTableView(settings.EventTable.ViewMode.ToString());
                        ProtocolPanel.SetTableSortOrder(settings.EventTable.SortOrder.ToString());
                    }

                    // Apply protocol-specific settings
                    if (settings.UART != null)
                        ProtocolPanel.ApplyUARTSettings(settings.UART);

                    if (settings.I2C != null)
                        ProtocolPanel.ApplyI2CSettings(settings.I2C);

                    // Fixed: Use correct property name (SPI instead of SPISettings)
                    if (settings.SPI != null)
                        ProtocolPanel.ApplySPISettings(settings.SPI);

                    if (settings.Parallel != null)
                        ProtocolPanel.ApplyParallelSettings(settings.Parallel);

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

        /// <summary>
        /// Helper method to parse enum values safely
        /// </summary>
        private T ParseEnum<T>(string value) where T : struct, Enum
        {
            if (string.IsNullOrEmpty(value))
                return default(T);

            if (Enum.TryParse<T>(value, true, out T result))
                return result;

            return default(T);
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
}