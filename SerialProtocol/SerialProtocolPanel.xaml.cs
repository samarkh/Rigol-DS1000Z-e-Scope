using Microsoft.Win32;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DS1000Z_E_USB_Control.SerialProtocol
{
    /// <summary>
    /// SerialProtocolPANEL - The main UserControl containing all protocol analysis functionality
    /// Handles UART, I²C, SPI, and Parallel protocol configuration and SCPI command generation
    /// </summary>
    public partial class SerialProtocolPANEL : UserControl
    {
        #region Properties

        private bool _decoderEnabled = false;
        private bool _tableEnabled = false;
        private bool _panelCollapsed = false;

        private bool _isInitialized = false;
        
        public bool DecoderEnabled
        {
            get => _decoderEnabled;
            set
            {
                _decoderEnabled = value;
                UpdateDecoderStatusIndicator();
            }
        }

        public bool TableEnabled
        {
            get => _tableEnabled;
            set
            {
                _tableEnabled = value;
                UpdateTableStatusIndicator();
            }
        }

        public bool PanelCollapsed
        {
            get => _panelCollapsed;
            set
            {
                _panelCollapsed = value;
                UpdatePanelVisibility();
            }
        }

        // Event for sending SCPI commands to the oscilloscope
        public event EventHandler<string> SCPICommandGenerated;
        #endregion

        #region Constructor

        public SerialProtocolPANEL()
        {
            InitializeComponent();
            this.Loaded += SerialProtocolPANEL_Loaded;
        }


        // Update your InitializePanel method
        private void InitializePanel()
        {
            try
            {
                LogCommand("// Serial Protocol Analysis & Decoding Panel Ready");
                LogCommand("// Configure your decoder settings and click Apply to generate SCPI commands");

                UpdateDecoderStatusIndicator();
                UpdateTableStatusIndicator();

                // Set initial protocol visibility - UART should be visible by default
                SwitchProtocolControls("UART");
            }
            catch (Exception ex)
            {
                LogCommand($"// Initialization error: {ex.Message}");
            }
        }



        #endregion

        #region Toggle Functionality
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TogglePanel();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePanel();
        }

        private void TogglePanel()
        {
            PanelCollapsed = !PanelCollapsed;
        }

        private void UpdatePanelVisibility()
        {
            if (PanelCollapsed)
            {
                MainContent.Visibility = Visibility.Collapsed;
                ToggleIcon.Text = "🔼";
                ToggleText.Text = "Expand";
                LogCommand("// Panel collapsed - click header to expand");
            }
            else
            {
                MainContent.Visibility = Visibility.Visible;
                ToggleIcon.Text = "🔽";
                ToggleText.Text = "Collapse";
                LogCommand("// Panel expanded - ready for configuration");
            }
        }
        #endregion


        #region Protocol Management

        // Update your ProtocolType_SelectionChanged method
        private void ProtocolType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Prevent execution during initialization
            if (!_isInitialized || ProtocolTypeCombo == null)
                return;

            try
            {
                if (ProtocolTypeCombo.SelectedItem is ComboBoxItem selectedItem)
                {
                    string protocol = selectedItem.Tag?.ToString() ?? "UART";
                    SwitchProtocolControls(protocol);
                    LogCommand($"// Protocol switched to {protocol}");
                }
            }
            catch (Exception ex)
            {
                LogCommand($"// Error in protocol selection: {ex.Message}");
            }
        }


        private void SwitchProtocolControls(string protocol)
        {
            // Add null checks to prevent exceptions during initialization
            try
            {
                // Hide all protocol-specific controls first (with null checks)
                if (UARTControls != null)
                    UARTControls.Visibility = Visibility.Collapsed;

                if (I2CControls != null)
                    I2CControls.Visibility = Visibility.Collapsed;

                if (SPIControls != null)
                    SPIControls.Visibility = Visibility.Collapsed;

                if (ParallelControls != null)
                    ParallelControls.Visibility = Visibility.Collapsed;

                // Show the selected protocol controls (with null checks)
                switch (protocol?.ToUpper())
                {
                    case "UART":
                    case "RS232":
                        if (UARTControls != null)
                            UARTControls.Visibility = Visibility.Visible;
                        break;

                    case "IIC":
                    case "I2C":
                        if (I2CControls != null)
                            I2CControls.Visibility = Visibility.Visible;
                        break;

                    case "SPI":
                        if (SPIControls != null)
                            SPIControls.Visibility = Visibility.Visible;
                        break;

                    case "PARALLEL":
                    case "PAR":
                        if (ParallelControls != null)
                            ParallelControls.Visibility = Visibility.Visible;
                        break;

                    default:
                        // Default to UART if protocol is unknown
                        if (UARTControls != null)
                            UARTControls.Visibility = Visibility.Visible;
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log the error safely
                LogCommand($"// Error switching protocol controls: {ex.Message}");
            }
        }

        #endregion

        #region Decoder Management
        private void ToggleDecoder_Click(object sender, RoutedEventArgs e)
        {
            DecoderEnabled = !DecoderEnabled;
            int decoderNum = GetDecoderNumber();
            string command = $":DECoder{decoderNum}:DISPlay {(DecoderEnabled ? "ON" : "OFF")}";
            LogCommand(command);
            SendSCPICommand(command);
        }

        private void UpdateDecoderStatusIndicator()
        {
            if (DecoderStatusLight != null)
            {
                DecoderStatusLight.Fill = DecoderEnabled ?
                    new SolidColorBrush(Colors.LimeGreen) :
                    new SolidColorBrush(Colors.Red);
            }
        }
        #endregion

        #region Protocol Configuration Handlers

        private void ConfigureUART_Click(object sender, RoutedEventArgs e)
        {
            int decoderNum = GetDecoderNumber();
            string tx = GetComboBoxTag(UARTTxCombo);
            string rx = GetComboBoxTag(UARTRxCombo);
            string baud = GetComboBoxTag(UARTBaudCombo);
            string width = GetComboBoxTag(UARTWidthCombo);
            string stop = GetComboBoxTag(UARTStopCombo);
            string parity = GetComboBoxTag(UARTParityCombo);
            string polarity = GetComboBoxTag(UARTPolarityCombo);
            string endian = GetComboBoxTag(UARTEndianCombo);
            string format = GetComboBoxTag(DisplayFormatCombo);

            var commands = new[]
            {
                $":DECoder{decoderNum}:MODE RS232",
                $":DECoder{decoderNum}:RS232:TX {tx}",
                $":DECoder{decoderNum}:RS232:RX {rx}",
                $":DECoder{decoderNum}:RS232:BAUD {baud}",
                $":DECoder{decoderNum}:RS232:WIDTh {width}",
                $":DECoder{decoderNum}:RS232:STOP {stop}",
                $":DECoder{decoderNum}:RS232:PARity {parity}",
                $":DECoder{decoderNum}:RS232:POLarity {polarity}",
                $":DECoder{decoderNum}:RS232:ENDian {endian}",
                $":DECoder{decoderNum}:FORMat {format}"
            };

            foreach (string command in commands)
            {
                LogCommand(command);
                SendSCPICommand(command);
            }
        }

        private void ConfigureI2C_Click(object sender, RoutedEventArgs e)
        {
            int decoderNum = GetDecoderNumber();
            string clk = GetComboBoxTag(I2CClkCombo);
            string data = GetComboBoxTag(I2CDataCombo);
            string addr = GetComboBoxTag(I2CAddressCombo);
            string format = GetComboBoxTag(DisplayFormatCombo);

            var commands = new[]
            {
                $":DECoder{decoderNum}:MODE IIC",
                $":DECoder{decoderNum}:IIC:CLK {clk}",
                $":DECoder{decoderNum}:IIC:DATA {data}",
                $":DECoder{decoderNum}:IIC:ADDR {addr}",
                $":DECoder{decoderNum}:FORMat {format}"
            };

            foreach (string command in commands)
            {
                LogCommand(command);
                SendSCPICommand(command);
            }
        }

        private void ConfigureSPI_Click(object sender, RoutedEventArgs e)
        {
            int decoderNum = GetDecoderNumber();
            string clk = GetComboBoxTag(SPIClkCombo);
            string miso = GetComboBoxTag(SPIMisoCombo);
            string mosi = GetComboBoxTag(SPIMosiCombo);
            string cs = GetComboBoxTag(SPICsCombo);
            string width = SPIWidthText.Text;
            string polarity = GetComboBoxTag(SPIPolarityCombo);
            string edge = GetComboBoxTag(SPIEdgeCombo);
            string endian = GetComboBoxTag(SPIEndianCombo);
            string format = GetComboBoxTag(DisplayFormatCombo);

            var commands = new[]
            {
                $":DECoder{decoderNum}:MODE SPI",
                $":DECoder{decoderNum}:SPI:CLK {clk}",
                $":DECoder{decoderNum}:SPI:MISO {miso}",
                $":DECoder{decoderNum}:SPI:MOSI {mosi}",
                $":DECoder{decoderNum}:SPI:CS {cs}",
                $":DECoder{decoderNum}:SPI:WIDTh {width}",
                $":DECoder{decoderNum}:SPI:POLarity {polarity}",
                $":DECoder{decoderNum}:SPI:EDGE {edge}",
                $":DECoder{decoderNum}:SPI:ENDian {endian}",
                $":DECoder{decoderNum}:FORMat {format}"
            };

            foreach (string command in commands)
            {
                LogCommand(command);
                SendSCPICommand(command);
            }
        }

        private void ConfigureParallel_Click(object sender, RoutedEventArgs e)
        {
            int decoderNum = GetDecoderNumber();
            string clk = GetComboBoxTag(ParallelClkCombo);
            string edge = GetComboBoxTag(ParallelEdgeCombo);
            string width = ParallelWidthText.Text;
            string format = GetComboBoxTag(DisplayFormatCombo);

            var commands = new[]
            {
                $":DECoder{decoderNum}:MODE PARallel",
                $":DECoder{decoderNum}:PARallel:CLK {clk}",
                $":DECoder{decoderNum}:PARallel:EDGE {edge}",
                $":DECoder{decoderNum}:PARallel:WIDTh {width}",
                $":DECoder{decoderNum}:FORMat {format}"
            };

            foreach (string command in commands)
            {
                LogCommand(command);
                SendSCPICommand(command);
            }
        }
        #endregion

        #region Event Table Management
        private void ToggleEventTable_Click(object sender, RoutedEventArgs e)
        {
            TableEnabled = !TableEnabled;
            int decoderNum = GetDecoderNumber();
            string command = $":ETABle{decoderNum}:DISP {(TableEnabled ? "ON" : "OFF")}";
            LogCommand(command);
            SendSCPICommand(command);
        }

        private void ApplyTableSettings_Click(object sender, RoutedEventArgs e)
        {
            int decoderNum = GetDecoderNumber();
            string format = GetComboBoxTag(TableFormatCombo);
            string view = GetComboBoxTag(TableViewCombo);
            string column = GetComboBoxTag(TableColumnCombo);
            string sort = GetComboBoxTag(TableSortCombo);

            var commands = new[]
            {
                $":ETABle{decoderNum}:FORMat {format}",
                $":ETABle{decoderNum}:VIEW {view}",
                $":ETABle{decoderNum}:COLumn {column}",
                $":ETABle{decoderNum}:SORT {sort}"
            };

            foreach (string command in commands)
            {
                LogCommand(command);
                SendSCPICommand(command);
            }
        }

        private void SetTableRow_Click(object sender, RoutedEventArgs e)
        {
            //if (int.TryParse(TableRowText.Text, out int row))
            //{
            //    int decoderNum = GetDecoderNumber();
            //    string command = $":ETABle{decoderNum}:ROW {row}";
            //    LogCommand(command);
            //    SendSCPICommand(command);
            //}
            //else
            //{
                LogCommand("// Error: Invalid row number");
            //}
        }

        private void ExportTableData_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Title = "Export Event Table Data",
                Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"EventTable_Decoder{GetDecoderNumber()}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // This would typically query the oscilloscope for table data
                    // For now, we'll create a sample export
                    var sb = new StringBuilder();
                    sb.AppendLine("Time,Data,Type,Status");
                    sb.AppendLine("0.000000,0x55,Start,Valid");
                    sb.AppendLine("0.000100,0x41,Data,Valid");
                    sb.AppendLine("0.000200,0x42,Data,Valid");
                    sb.AppendLine("// Export functionality - implement actual data retrieval");

                    File.WriteAllText(saveDialog.FileName, sb.ToString());
                    LogCommand($"// Event table data exported to {Path.GetFileName(saveDialog.FileName)}");
                }
                catch (Exception ex)
                {
                    LogCommand($"// Export error: {ex.Message}");
                }
            }
        }

        private void UpdateTableStatusIndicator()
        {
            if (TableStatusLight != null)
            {
                TableStatusLight.Fill = TableEnabled ?
                    new SolidColorBrush(Colors.LimeGreen) :
                    new SolidColorBrush(Colors.Red);
            }
        }
        #endregion

        #region Missing Event Handlers (Add these to your existing file)


        // Missing event handlers referenced in XAML:

        private void ToggleTable_Click(object sender, RoutedEventArgs e)
        {
            TableEnabled = !TableEnabled;
            int decoderNum = GetDecoderNumber();
            string command = $":ETABle{decoderNum}:DISP {(TableEnabled ? "ON" : "OFF")}";
            LogCommand(command);
            SendSCPICommand(command);
            UpdateTableStatusIndicator();
        }

        private void ExportTable_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Title = "Export Event Table Data",
                Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"EventTable_Decoder{GetDecoderNumber()}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // Sample export - replace with actual data retrieval from oscilloscope
                    var sb = new StringBuilder();
                    sb.AppendLine("Time,Data,Type,Status");
                    sb.AppendLine("0.000000,0x55,Start,Valid");
                    sb.AppendLine("0.000100,0x41,Data,Valid");
                    sb.AppendLine("0.000200,0x42,Data,Valid");
                    sb.AppendLine("// Export functionality - implement actual data retrieval");

                    File.WriteAllText(saveDialog.FileName, sb.ToString());
                    LogCommand($"// Event table data exported to {Path.GetFileName(saveDialog.FileName)}");
                }
                catch (Exception ex)
                {
                    LogCommand($"// Export error: {ex.Message}");
                }
            }
        }

        private void CopyCommands_Click(object sender, RoutedEventArgs e)
        {
            if (CommandLogTextBox != null)
            {
                try
                {
                    Clipboard.SetText(CommandLogTextBox.Text);
                    LogCommand("// Commands copied to clipboard");
                }
                catch (Exception ex)
                {
                    LogCommand($"// Copy error: {ex.Message}");
                }
            }
        }

        private void SaveCommands_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Title = "Save SCPI Commands",
                Filter = "Text files (*.txt)|*.txt|SCPI files (*.scpi)|*.scpi|All files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"SCPI_Commands_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveDialog.FileName, CommandLogTextBox?.Text ?? "");
                    LogCommand($"// Commands saved to {Path.GetFileName(saveDialog.FileName)}");
                }
                catch (Exception ex)
                {
                    LogCommand($"// Save error: {ex.Message}");
                }
            }
        }

        private void ResetAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Reset all decoder settings to defaults?",
                                        "Reset Confirmation",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ResetToDefaults();
                LogCommand("// All settings reset to defaults");
            }
        }

        private void ApplyAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Apply basic decoder settings
                int decoderNum = GetDecoderNumber();
                string protocol = GetComboBoxTag(ProtocolTypeCombo);
                string format = GetComboBoxTag(DisplayFormatCombo);
                string position = VerticalPositionText?.Text ?? "0";

                var basicCommands = new[]
                {
            $":DECoder{decoderNum}:MODE {protocol}",
            $":DECoder{decoderNum}:FORMat {format}",
            $":DECoder{decoderNum}:POSition {position}",
            $":DECoder{decoderNum}:DISPlay {(DecoderEnabled ? "ON" : "OFF")}"
        };

                foreach (string command in basicCommands)
                {
                    LogCommand(command);
                    SendSCPICommand(command);
                }

                // Apply protocol-specific settings based on current selection
                switch (protocol.ToUpper())
                {
                    case "UART":
                    case "RS232":
                        ConfigureUART_Click(sender, e);
                        break;
                    case "IIC":
                    case "I2C":
                        ConfigureI2C_Click(sender, e);
                        break;
                    case "SPI":
                        ConfigureSPI_Click(sender, e);
                        break;
                    case "PARALLEL":
                        ConfigureParallel_Click(sender, e);
                        break;
                }

                // Apply table settings
                ApplyTableSettings_Click(sender, e);

                LogCommand("// All settings applied successfully");
            }
            catch (Exception ex)
            {
                LogCommand($"// Error applying settings: {ex.Message}");
            }
        }

        private void LoadConfiguration_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Title = "Load Configuration",
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

                    // You'll need to implement the DecoderSettings class and ApplySettings method
                    // var settings = JsonSerializer.Deserialize<DecoderSettings>(jsonString, options);
                    // ApplySettings(settings);

                    LogCommand($"// Configuration loaded from {Path.GetFileName(openDialog.FileName)}");
                }
                catch (Exception ex)
                {
                    LogCommand($"// Load error: {ex.Message}");
                }
            }
        }

        private void SaveConfiguration_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Title = "Save Configuration",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"SerialProtocol_Config_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // You'll need to implement the GetCurrentSettings method
                    // var settings = GetCurrentSettings();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    };

                    // string jsonString = JsonSerializer.Serialize(settings, options);
                    // File.WriteAllText(saveDialog.FileName, jsonString);

                    // For now, save a basic configuration template
                    string configTemplate = "{\n  \"decoderNumber\": 1,\n  \"enabled\": false,\n  \"protocolType\": \"UART\"\n}";
                    File.WriteAllText(saveDialog.FileName, configTemplate);

                    LogCommand($"// Configuration saved to {Path.GetFileName(saveDialog.FileName)}");
                }
                catch (Exception ex)
                {
                    LogCommand($"// Save error: {ex.Message}");
                }
            }
        }

        private void SerialProtocolPANEL_Loaded(object sender, RoutedEventArgs e)
        {
            InitializePanel();
            _isInitialized = true;
        }


        #endregion

        #region Command Logging
        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            if (CommandLogTextBox != null)
            {
                CommandLogTextBox.Clear();
                LogCommand("// Command log cleared");
            }
        }

        private void LogCommand(string command)
        {
            if (CommandLogTextBox != null)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                CommandLogTextBox.AppendText($"[{timestamp}] {command}\n");
                CommandLogTextBox.ScrollToEnd();
            }
        }

        private void SendSCPICommand(string command)
        {
            // Raise the event so the parent window can forward to oscilloscope
            SCPICommandGenerated?.Invoke(this, command);
        }
        #endregion

        #region Helper Methods


        private string GetComboBoxTag(ComboBox comboBox)
        {
            if (comboBox?.SelectedItem is ComboBoxItem item)
            {
                return item.Tag?.ToString() ?? "";
            }
            return "";
        }

        private double GetThresholdValue(TextBox textBox)
        {
            if (textBox != null && double.TryParse(textBox.Text, NumberStyles.Float,
                CultureInfo.InvariantCulture, out double value))
            {
                return value;
            }
            return 1.4; // Default threshold
        }
        #endregion

        #region Public Interface Methods (for SerialProtocolWINDOW)

        public string GetCurrentProtocolType()
        {
            return GetComboBoxTag(ProtocolTypeCombo);
        }

        public void SetProtocolType(string protocol)
        {
            foreach (ComboBoxItem item in ProtocolTypeCombo.Items)
            {
                if (item.Tag.ToString().Equals(protocol, StringComparison.OrdinalIgnoreCase))
                {
                    ProtocolTypeCombo.SelectedItem = item;
                    SwitchProtocolControls(protocol);
                    break;
                }
            }
        }

        public int GetDecoderNumber()
        {
            if (DecoderNumberCombo?.SelectedItem is ComboBoxItem selectedItem &&
                int.TryParse(selectedItem.Tag?.ToString(), out int number))
            {
                return number;
            }
            return 1; // Default decoder number
        }

        public void SetDecoderNumber(int number)
        {
            foreach (ComboBoxItem item in DecoderNumberCombo.Items)
            {
                if (item.Tag.ToString() == number.ToString())
                {
                    DecoderNumberCombo.SelectedItem = item;
                    break;
                }
            }
        }

        public string GetDisplayFormat()
        {
            return GetComboBoxTag(DisplayFormatCombo);
        }

        public void SetDisplayFormat(string format)
        {
            foreach (ComboBoxItem item in DisplayFormatCombo.Items)
            {
                if (item.Tag.ToString().Equals(format, StringComparison.OrdinalIgnoreCase))
                {
                    DisplayFormatCombo.SelectedItem = item;
                    break;
                }
            }
        }

        public double GetVerticalPosition()
        {
            return GetThresholdValue(VerticalPositionText);
        }

        public void SetVerticalPosition(double position)
        {
            if (VerticalPositionText != null)
            {
                VerticalPositionText.Text = position.ToString("F1", CultureInfo.InvariantCulture);
            }
        }

        public double GetChannel1Threshold()
        {
            return GetThresholdValue(Ch1ThresholdText);
        }

        public void SetChannel1Threshold(double threshold)
        {
            if (Ch1ThresholdText != null)
            {
                Ch1ThresholdText.Text = threshold.ToString("F1", CultureInfo.InvariantCulture);
            }
        }

        public double GetChannel2Threshold()
        {
            return GetThresholdValue(Ch2ThresholdText);
        }

        public void SetChannel2Threshold(double threshold)
        {
            if (Ch2ThresholdText != null)
            {
                Ch2ThresholdText.Text = threshold.ToString("F1", CultureInfo.InvariantCulture);
            }
        }

        public string GetTableFormat()
        {
            return GetComboBoxTag(TableFormatCombo);
        }

        public void SetTableFormat(string format)
        {
            foreach (ComboBoxItem item in TableFormatCombo.Items)
            {
                if (item.Tag.ToString().Equals(format, StringComparison.OrdinalIgnoreCase))
                {
                    TableFormatCombo.SelectedItem = item;
                    break;
                }
            }
        }

        public string GetTableView()
        {
            return GetComboBoxTag(TableViewCombo);
        }

        public void SetTableView(string view)
        {
            foreach (ComboBoxItem item in TableViewCombo.Items)
            {
                if (item.Tag.ToString().Equals(view, StringComparison.OrdinalIgnoreCase))
                {
                    TableViewCombo.SelectedItem = item;
                    break;
                }
            }
        }

        public string GetTableSortOrder()
        {
            return GetComboBoxTag(TableSortCombo);
        }

        public void SetTableSortOrder(string sortOrder)
        {
            foreach (ComboBoxItem item in TableSortCombo.Items)
            {
                if (item.Tag.ToString().Equals(sortOrder, StringComparison.OrdinalIgnoreCase))
                {
                    TableSortCombo.SelectedItem = item;
                    break;
                }
            }
        }

        public void ResetToDefaults()
        {
            // Reset to default values
            DecoderEnabled = false;
            TableEnabled = false;
            PanelCollapsed = false;

            // Reset combo boxes to defaults
            if (DecoderNumberCombo.Items.Count > 0)
                DecoderNumberCombo.SelectedIndex = 0;

            if (ProtocolTypeCombo.Items.Count > 0)
            {
                ProtocolTypeCombo.SelectedIndex = 0; // UART
                SwitchProtocolControls("UART");
            }

            if (DisplayFormatCombo.Items.Count > 0)
                DisplayFormatCombo.SelectedIndex = 0; // HEX

            // Reset text fields
            if (VerticalPositionText != null)
                VerticalPositionText.Text = "0";

            if (Ch1ThresholdText != null)
                Ch1ThresholdText.Text = "1.4";

            if (Ch2ThresholdText != null)
                Ch2ThresholdText.Text = "1.4";

            // Reset UART settings to defaults
            ResetUARTToDefaults();
            ResetI2CToDefaults();
            ResetSPIToDefaults();
            ResetParallelToDefaults();

            // Clear command log
            if (CommandLogTextBox != null)
            {
                CommandLogTextBox.Clear();
                LogCommand("// Panel reset to defaults");
            }
        }

        private void ResetUARTToDefaults()
        {
            SetComboBoxByTag(UARTTxCombo, "CHANnel1");
            SetComboBoxByTag(UARTRxCombo, "CHANnel2");
            SetComboBoxByTag(UARTBaudCombo, "9600");
            SetComboBoxByTag(UARTWidthCombo, "8");
            SetComboBoxByTag(UARTStopCombo, "1");
            SetComboBoxByTag(UARTParityCombo, "NONE");
            SetComboBoxByTag(UARTPolarityCombo, "POS");
            SetComboBoxByTag(UARTEndianCombo, "LSB");
        }

        private void ResetI2CToDefaults()
        {
            SetComboBoxByTag(I2CClkCombo, "CHANnel1");
            SetComboBoxByTag(I2CDataCombo, "CHANnel2");
            SetComboBoxByTag(I2CAddressCombo, "ADDR7");
        }

        private void ResetSPIToDefaults()
        {
            SetComboBoxByTag(SPIClkCombo, "CHANnel1");
            SetComboBoxByTag(SPIMisoCombo, "CHANnel2");
            SetComboBoxByTag(SPIMosiCombo, "CHANnel1");
            SetComboBoxByTag(SPICsCombo, "CHANnel1");
            if (SPIWidthText != null) SPIWidthText.Text = "8";
            SetComboBoxByTag(SPIPolarityCombo, "POSitive");
            SetComboBoxByTag(SPIEdgeCombo, "POSitive");
            SetComboBoxByTag(SPIEndianCombo, "LSB");
        }

        private void ResetParallelToDefaults()
        {
            SetComboBoxByTag(ParallelClkCombo, "CHANnel1");
            SetComboBoxByTag(ParallelEdgeCombo, "POSitive");
            if (ParallelWidthText != null) ParallelWidthText.Text = "8";
        }

        private void SetComboBoxByTag(ComboBox comboBox, string tag)
        {
            if (comboBox != null)
            {
                foreach (ComboBoxItem item in comboBox.Items)
                {
                    if (item.Tag?.ToString().Equals(tag, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        comboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        // Methods for getting/setting protocol-specific settings
        // Fix the settings getter methods:

        public UARTSettings GetUARTSettings()
        {
            return new UARTSettings
            {
                TxChannel = ParseEnum<ChannelSource>(GetComboBoxTag(UARTTxCombo)),
                RxChannel = ParseEnum<ChannelSource>(GetComboBoxTag(UARTRxCombo)),
                BaudRate = int.TryParse(GetComboBoxTag(UARTBaudCombo), out int baud) ? baud : 9600,
                DataWidth = int.TryParse(GetComboBoxTag(UARTWidthCombo), out int width) ? width : 8,
                StopBits = int.TryParse(GetComboBoxTag(UARTStopCombo), out int stop) ? stop : 1,
                Parity = ParseEnum<ParityType>(GetComboBoxTag(UARTParityCombo)),
                Polarity = ParseEnum<PolarityType>(GetComboBoxTag(UARTPolarityCombo)),
                Endian = ParseEnum<EndianType>(GetComboBoxTag(UARTEndianCombo))
            };
        }

        public void ApplyUARTSettings(UARTSettings settings)
        {
            SetComboBoxByTag(UARTTxCombo, EnumToString(settings.TxChannel));
            SetComboBoxByTag(UARTRxCombo, EnumToString(settings.RxChannel));
            SetComboBoxByTag(UARTBaudCombo, settings.BaudRate.ToString());
            SetComboBoxByTag(UARTWidthCombo, settings.DataWidth.ToString());
            SetComboBoxByTag(UARTStopCombo, settings.StopBits.ToString());
            SetComboBoxByTag(UARTParityCombo, EnumToString(settings.Parity));
            SetComboBoxByTag(UARTPolarityCombo, EnumToString(settings.Polarity));
            SetComboBoxByTag(UARTEndianCombo, EnumToString(settings.Endian));
        }

        public I2CSettings GetI2CSettings()
        {
            return new I2CSettings
            {
                ClockChannel = ParseEnum<ChannelSource>(GetComboBoxTag(I2CClkCombo)),
                DataChannel = ParseEnum<ChannelSource>(GetComboBoxTag(I2CDataCombo)),
                AddressMode = ParseEnum<I2CAddressMode>(GetComboBoxTag(I2CAddressCombo)) // Fixed: AddressType -> AddressMode
            };
        }

        public void ApplyI2CSettings(I2CSettings settings)
        {
            SetComboBoxByTag(I2CClkCombo, EnumToString(settings.ClockChannel));
            SetComboBoxByTag(I2CDataCombo, EnumToString(settings.DataChannel));
            SetComboBoxByTag(I2CAddressCombo, EnumToString(settings.AddressMode)); // Fixed: AddressType -> AddressMode
        }

        public SPISettings GetSPISettings()
        {
            return new SPISettings
            {
                ClockChannel = ParseEnum<ChannelSource>(GetComboBoxTag(SPIClkCombo)),
                MisoChannel = ParseEnum<ChannelSource>(GetComboBoxTag(SPIMisoCombo)),
                MosiChannel = ParseEnum<ChannelSource>(GetComboBoxTag(SPIMosiCombo)),
                CsChannel = ParseEnum<ChannelSource>(GetComboBoxTag(SPICsCombo)),
                DataWidth = int.TryParse(SPIWidthText?.Text, out int width) ? width : 8,
                ClockPolarity = ParseEnum<PolarityType>(GetComboBoxTag(SPIPolarityCombo)), // Fixed: Polarity -> ClockPolarity
                ClockEdge = ParseEnum<PolarityType>(GetComboBoxTag(SPIEdgeCombo)), // Fixed: Edge -> ClockEdge
                Endian = ParseEnum<EndianType>(GetComboBoxTag(SPIEndianCombo))
            };
        }

        public void ApplySPISettings(SPISettings settings)
        {
            SetComboBoxByTag(SPIClkCombo, EnumToString(settings.ClockChannel));
            SetComboBoxByTag(SPIMisoCombo, EnumToString(settings.MisoChannel));
            SetComboBoxByTag(SPIMosiCombo, EnumToString(settings.MosiChannel));
            SetComboBoxByTag(SPICsCombo, EnumToString(settings.CsChannel));
            if (SPIWidthText != null) SPIWidthText.Text = settings.DataWidth.ToString();
            SetComboBoxByTag(SPIPolarityCombo, EnumToString(settings.ClockPolarity)); // Fixed: Polarity -> ClockPolarity
            SetComboBoxByTag(SPIEdgeCombo, EnumToString(settings.ClockEdge)); // Fixed: Edge -> ClockEdge
            SetComboBoxByTag(SPIEndianCombo, EnumToString(settings.Endian));
        }

        public ParallelSettings GetParallelSettings()
        {
            return new ParallelSettings
            {
                ClockChannel = ParseEnum<ChannelSource>(GetComboBoxTag(ParallelClkCombo)),
                ClockEdge = ParseEnum<EdgeType>(GetComboBoxTag(ParallelEdgeCombo)), // Fixed: Edge -> ClockEdge
                DataWidth = int.TryParse(ParallelWidthText?.Text, out int width) ? width : 8
            };
        }

        public void ApplyParallelSettings(ParallelSettings settings)
        {
            SetComboBoxByTag(ParallelClkCombo, EnumToString(settings.ClockChannel));
            SetComboBoxByTag(ParallelEdgeCombo, EnumToString(settings.ClockEdge)); // Fixed: Edge -> ClockEdge
            if (ParallelWidthText != null) ParallelWidthText.Text = settings.DataWidth.ToString();
        }

        #endregion

        #region Enum Conversion Helpers

        private T ParseEnum<T>(string value) where T : struct, Enum
        {
            if (Enum.TryParse<T>(value, ignoreCase: true, out T result))
                return result;
            return default(T);
        }

        private string EnumToString<T>(T enumValue) where T : struct, Enum
        {
            return enumValue.ToString();
        }

        #endregion

    }
}