using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DS1000Z_E_USB_Control.SerialProtocol
{
    public partial class SerialProtocolPanel : UserControl
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
        public SerialProtocolPanel()
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

                int decoderNum = GetDecoderNumber();
                string command = $":DECoder{decoderNum}:MODE {protocol}";
                LogCommand(command);
                SendSCPICommand(command);
            }
        }

        private void SwitchProtocolControls(string protocol)
        {
            // Hide all protocol controls
            UARTControls.Visibility = Visibility.Collapsed;
            I2CControls.Visibility = Visibility.Collapsed;
            SPIControls.Visibility = Visibility.Collapsed;
            ParallelControls.Visibility = Visibility.Collapsed;

            // Show selected protocol controls
            switch (protocol)
            {
                case "UART":
                    UARTControls.Visibility = Visibility.Visible;
                    break;
                case "IIC":
                    I2CControls.Visibility = Visibility.Visible;
                    break;
                case "SPI":
                    SPIControls.Visibility = Visibility.Visible;
                    break;
                case "PARallel":
                    ParallelControls.Visibility = Visibility.Visible;
                    break;
            }
        }
        #endregion

        #region Decoder Configuration
        private void ToggleDecoder_Click(object sender, RoutedEventArgs e)
        {
            DecoderEnabled = !DecoderEnabled;
            int decoderNum = GetDecoderNumber();
            string command = $":DECoder{decoderNum}:DISPlay {(DecoderEnabled ? "ON" : "OFF")}";
            LogCommand(command);
            SendSCPICommand(command);
        }

        private void SetPosition_Click(object sender, RoutedEventArgs e)
        {
            int decoderNum = GetDecoderNumber();
            string position = VerticalPositionText.Text;
            string command = $":DECoder{decoderNum}:POSition {position}";
            LogCommand(command);
            SendSCPICommand(command);
        }

        private void SetCh1Threshold_Click(object sender, RoutedEventArgs e)
        {
            int decoderNum = GetDecoderNumber();
            string threshold = Ch1ThresholdText.Text;
            string command = $":DECoder{decoderNum}:THREshold:CHANnel1 {threshold}";
            LogCommand(command);
            SendSCPICommand(command);
        }

        private void SetCh2Threshold_Click(object sender, RoutedEventArgs e)
        {
            int decoderNum = GetDecoderNumber();
            string threshold = Ch2ThresholdText.Text;
            string command = $":DECoder{decoderNum}:THREshold:CHANnel2 {threshold}";
            LogCommand(command);
            SendSCPICommand(command);
        }

        private void SetAutoThreshold_Click(object sender, RoutedEventArgs e)
        {
            int decoderNum = GetDecoderNumber();
            string command = $":DECoder{decoderNum}:THREshold:AUTO";
            LogCommand(command);
            SendSCPICommand(command);
        }

        private void UpdateDecoderStatusIndicator()
        {
            DecoderStatusLight.Fill = DecoderEnabled ? Brushes.LimeGreen : Brushes.Red;
        }
        #endregion

        #region Protocol Configuration
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
                $":DECoder{decoderNum}:MODE UART",
                $":DECoder{decoderNum}:UART:TX {tx}",
                $":DECoder{decoderNum}:UART:RX {rx}",
                $":DECoder{decoderNum}:UART:BAUD {baud}",
                $":DECoder{decoderNum}:UART:WIDTh {width}",
                $":DECoder{decoderNum}:UART:STOP {stop}",
                $":DECoder{decoderNum}:UART:PARity {parity}",
                $":DECoder{decoderNum}:UART:POLarity {polarity}",
                $":DECoder{decoderNum}:UART:ENDian {endian}",
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
            string address = GetComboBoxTag(I2CAddressCombo);
            string format = GetComboBoxTag(DisplayFormatCombo);

            var commands = new[]
            {
                $":DECoder{decoderNum}:MODE IIC",
                $":DECoder{decoderNum}:IIC:CLK {clk}",
                $":DECoder{decoderNum}:IIC:DATA {data}",
                $":DECoder{decoderNum}:IIC:ADDRess {address}",
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

        private void SetTableRow_Click(object sender, RoutedEventArgs e)
        {
            int decoderNum = GetDecoderNumber();
            string row = TableRowText.Text;
            string command = $":ETABle{decoderNum}:ROW {row}";
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

        private void ExportTableData_Click(object sender, RoutedEventArgs e)
        {
            int decoderNum = GetDecoderNumber();
            string command = $":ETABle{decoderNum}:DATA?";
            LogCommand(command);
            LogCommand("// This command returns the event table data in TMC format");
            SendSCPICommand(command);
        }

        private void UpdateTableStatusIndicator()
        {
            TableStatusLight.Fill = TableEnabled ? Brushes.LimeGreen : Brushes.Red;
        }
        #endregion

        #region Command Logging
        private void LogCommand(string command)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {command}";

            if (CommandLog.Text == "Ready for decoder commands...")
            {
                CommandLog.Text = logEntry;
            }
            else
            {
                CommandLog.Text += Environment.NewLine + logEntry;
            }

            // Auto-scroll to bottom
            CommandLog.ScrollToEnd();
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            CommandLog.Text = "Ready for decoder commands...";
        }
        #endregion

        #region SCPI Communication
        private void SendSCPICommand(string command)
        {
            // Raise event to notify parent that a command needs to be sent
            SCPICommandGenerated?.Invoke(this, command);
        }
        #endregion

        #region Helper Methods
        private int GetDecoderNumber()
        {
            if (DecoderNumberCombo.SelectedItem is ComboBoxItem selectedItem)
            {
                return int.Parse(selectedItem.Tag.ToString());
            }
            return 1; // Default to decoder 1
        }

        private string GetComboBoxTag(ComboBox comboBox)
        {
            if (comboBox?.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Tag?.ToString() ?? "";
            }
            return "";
        }

        /// <summary>
        /// Public method to enable/disable the decoder programmatically
        /// </summary>
        /// <param name="enabled">True to enable, false to disable</param>
        public void SetDecoderEnabled(bool enabled)
        {
            DecoderEnabled = enabled;
        }

        /// <summary>
        /// Public method to enable/disable the event table programmatically
        /// </summary>
        /// <param name="enabled">True to enable, false to disable</param>
        public void SetEventTableEnabled(bool enabled)
        {
            TableEnabled = enabled;
        }

        /// <summary>
        /// Public method to set the decoder protocol type programmatically
        /// </summary>
        /// <param name="protocol">Protocol type: UART, IIC, SPI, or PARallel</param>
        public void SetProtocolType(string protocol)
        {
            foreach (ComboBoxItem item in ProtocolTypeCombo.Items)
            {
                if (item.Tag.ToString() == protocol)
                {
                    ProtocolTypeCombo.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// Public method to get the current decoder configuration as a summary string
        /// </summary>
        /// <returns>Configuration summary</returns>
        public string GetConfigurationSummary()
        {
            int decoderNum = GetDecoderNumber();
            string protocol = GetComboBoxTag(ProtocolTypeCombo);
            string format = GetComboBoxTag(DisplayFormatCombo);

            return $"Decoder {decoderNum}: {protocol} Protocol, {format} Format, " +
                   $"Enabled: {DecoderEnabled}, Table: {TableEnabled}";
        }

        /// <summary>
        /// Public method to reset all settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            // Reset basic settings
            DecoderNumberCombo.SelectedIndex = 0;
            ProtocolTypeCombo.SelectedIndex = 0;
            DisplayFormatCombo.SelectedIndex = 0;
            VerticalPositionText.Text = "350";

            // Reset thresholds
            Ch1ThresholdText.Text = "1.4";
            Ch2ThresholdText.Text = "1.4";

            // Reset UART settings
            UARTTxCombo.SelectedIndex = 0;
            UARTRxCombo.SelectedIndex = 1;
            UARTBaudCombo.SelectedIndex = 4; // 115200
            UARTWidthCombo.SelectedIndex = 3; // 8 bits
            UARTStopCombo.SelectedIndex = 0;
            UARTParityCombo.SelectedIndex = 0;
            UARTPolarityCombo.SelectedIndex = 0;
            UARTEndianCombo.SelectedIndex = 0;

            // Reset I2C settings
            I2CClkCombo.SelectedIndex = 0;
            I2CDataCombo.SelectedIndex = 1;
            I2CAddressCombo.SelectedIndex = 0;

            // Reset SPI settings
            SPIClkCombo.SelectedIndex = 0;
            SPIMisoCombo.SelectedIndex = 1;
            SPIMosiCombo.SelectedIndex = 0;
            SPICsCombo.SelectedIndex = 0;
            SPIWidthText.Text = "8";
            SPIPolarityCombo.SelectedIndex = 0;
            SPIEdgeCombo.SelectedIndex = 0;
            SPIEndianCombo.SelectedIndex = 0;

            // Reset Parallel settings
            ParallelClkCombo.SelectedIndex = 0;
            ParallelEdgeCombo.SelectedIndex = 0;
            ParallelWidthText.Text = "8";

            // Reset Event Table settings
            TableFormatCombo.SelectedIndex = 0;
            TableViewCombo.SelectedIndex = 0;
            TableColumnCombo.SelectedIndex = 0;
            TableSortCombo.SelectedIndex = 0;
            TableRowText.Text = "1";

            // Reset states
            DecoderEnabled = false;
            TableEnabled = false;
            PanelCollapsed = false;

            LogCommand("// Settings reset to defaults");
        }
        #endregion
    }
}