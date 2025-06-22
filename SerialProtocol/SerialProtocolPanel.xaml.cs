using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Globalization;

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
            InitializePanel();
        }

        private void InitializePanel()
        {
            LogCommand("// Serial Protocol Analysis & Decoding Panel Ready");
            LogCommand("// Configure your decoder settings and click Apply to generate SCPI commands");
            UpdateDecoderStatusIndicator();
            UpdateTableStatusIndicator();
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
        private void ProtocolType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProtocolTypeCombo.SelectedItem is ComboBoxItem selectedItem)
            {
                string protocol = selectedItem.Tag.ToString();
                SwitchProtocolControls(protocol);
                LogCommand($"// Protocol switched to {protocol}");
            }
        }

        private void SwitchProtocolControls(string protocol)
        {
            // Hide all protocol-specific controls first
            UARTControls.Visibility = Visibility.Collapsed;
            I2CControls.Visibility = Visibility.Collapsed;
            SPIControls.Visibility = Visibility.Collapsed;
            ParallelControls.Visibility = Visibility.Collapsed;

            // Show the selected protocol controls
            switch (protocol.ToUpper())
            {
                case "UART":
                    UARTControls.Visibility = Visibility.Visible;
                    break;
                case "IIC":
                case "I2C":
                    I2CControls.Visibility = Visibility.Visible;
                    break;
                case "SPI":
                    SPIControls.Visibility = Visibility.Visible;
                    break;
                case "PARALLEL":
                    ParallelControls.Visibility = Visibility.Visible;
                    break;
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
            if (int.TryParse(TableRowText.Text, out int row))
            {
                int decoderNum = GetDecoderNumber();
                string command = $":ETABle{decoderNum}:ROW {row}";
                LogCommand(command);
                SendSCPICommand(command);
            }
            else
            {
                LogCommand("// Error: Invalid row number");
            }
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

        #region Command Logging
        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            if (CommandLog != null)
            {
                CommandLog.Clear();
                LogCommand("// Command log cleared");
            }
        }

        private void LogCommand(string command)
        {
            if (CommandLog != null)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                CommandLog.AppendText($"[{timestamp}] {command}\n");
                CommandLog.ScrollToEnd();
            }
        }

        private void SendSCPICommand(string command)
        {
            // Raise the event so the parent window can forward to oscilloscope
            SCPICommandGenerated?.Invoke(this, command);
        }
        #endregion

        #region Helper Methods
        private int GetDecoderNumber()
        {
            if (DecoderNumberCombo?.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Tag?.ToString(), out int number))
            {
                return number;
            }
            return 1; // Default
        }

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
            return GetDecoderNumber();
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
            if (CommandLog != null)
            {
                CommandLog.Clear();
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
        public UARTSettings GetUARTSettings()
        {
            return new UARTSettings
            {
                TxChannel = GetComboBoxTag(UARTTxCombo),
                RxChannel = GetComboBoxTag(UARTRxCombo),
                BaudRate = GetComboBoxTag(UARTBaudCombo),
                DataWidth = GetComboBoxTag(UARTWidthCombo),
                StopBits = GetComboBoxTag(UARTStopCombo),
                Parity = GetComboBoxTag(UARTParityCombo),
                Polarity = GetComboBoxTag(UARTPolarityCombo),
                Endian = GetComboBoxTag(UARTEndianCombo)
            };
        }

        public void ApplyUARTSettings(UARTSettings settings)
        {
            SetComboBoxByTag(UARTTxCombo, settings.TxChannel);
            SetComboBoxByTag(UARTRxCombo, settings.RxChannel);
            SetComboBoxByTag(UARTBaudCombo, settings.BaudRate);
            SetComboBoxByTag(UARTWidthCombo, settings.DataWidth);
            SetComboBoxByTag(UARTStopCombo, settings.StopBits);
            SetComboBoxByTag(UARTParityCombo, settings.Parity);
            SetComboBoxByTag(UARTPolarityCombo, settings.Polarity);
            SetComboBoxByTag(UARTEndianCombo, settings.Endian);
        }

        public I2CSettings GetI2CSettings()
        {
            return new I2CSettings
            {
                ClockChannel = GetComboBoxTag(I2CClkCombo),
                DataChannel = GetComboBoxTag(I2CDataCombo),
                AddressType = GetComboBoxTag(I2CAddressCombo)
            };
        }

        public void ApplyI2CSettings(I2CSettings settings)
        {
            SetComboBoxByTag(I2CClkCombo, settings.ClockChannel);
            SetComboBoxByTag(I2CDataCombo, settings.DataChannel);
            SetComboBoxByTag(I2CAddressCombo, settings.AddressType);
        }

        public SPISettings GetSPISettings()
        {
            return new SPISettings
            {
                ClockChannel = GetComboBoxTag(SPIClkCombo),
                MisoChannel = GetComboBoxTag(SPIMisoCombo),
                MosiChannel = GetComboBoxTag(SPIMosiCombo),
                CsChannel = GetComboBoxTag(SPICsCombo),
                DataWidth = SPIWidthText?.Text ?? "8",
                Polarity = GetComboBoxTag(SPIPolarityCombo),
                Edge = GetComboBoxTag(SPIEdgeCombo),
                Endian = GetComboBoxTag(SPIEndianCombo)
            };
        }

        public void ApplySPISettings(SPISettings settings)
        {
            SetComboBoxByTag(SPIClkCombo, settings.ClockChannel);
            SetComboBoxByTag(SPIMisoCombo, settings.MisoChannel);
            SetComboBoxByTag(SPIMosiCombo, settings.MosiChannel);
            SetComboBoxByTag(SPICsCombo, settings.CsChannel);
            if (SPIWidthText != null) SPIWidthText.Text = settings.DataWidth;
            SetComboBoxByTag(SPIPolarityCombo, settings.Polarity);
            SetComboBoxByTag(SPIEdgeCombo, settings.Edge);
            SetComboBoxByTag(SPIEndianCombo, settings.Endian);
        }

        public ParallelSettings GetParallelSettings()
        {
            return new ParallelSettings
            {
                ClockChannel = GetComboBoxTag(ParallelClkCombo),
                Edge = GetComboBoxTag(ParallelEdgeCombo),
                DataWidth = ParallelWidthText?.Text ?? "8"
            };
        }

        public void ApplyParallelSettings(ParallelSettings settings)
        {
            SetComboBoxByTag(ParallelClkCombo, settings.ClockChannel);
            SetComboBoxByTag(ParallelEdgeCombo, settings.Edge);
            if (ParallelWidthText != null) ParallelWidthText.Text = settings.DataWidth;
        }

        #endregion
    }
}