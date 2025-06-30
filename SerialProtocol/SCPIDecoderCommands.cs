using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DS1000Z_E_USB_Control.SerialProtocol
{
    /// <summary>
    /// Static class for generating SCPI commands for the Rigol DS1000z-e decoder functionality
    /// Based on the official Rigol DS1000Z-E Programming Guide
    /// </summary>
    public static class SCPIDecoderCommands
    {
        #region Basic Decoder Commands

        /// <summary>
        /// Enable or disable a decoder
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="enabled">True to enable, false to disable</param>
        /// <returns>SCPI command string</returns>
        public static string SetDecoderEnabled(int decoderNumber, bool enabled)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:DISPlay {(enabled ? "ON" : "OFF")}";
        }

        /// <summary>
        /// Query decoder enable status
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <returns>SCPI query command</returns>
        public static string QueryDecoderEnabled(int decoderNumber)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:DISPlay?";
        }

        /// <summary>
        /// Set decoder protocol type
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="protocolType">Protocol type</param>
        /// <returns>SCPI command string</returns>
        public static string SetProtocolType(int decoderNumber, ProtocolType protocolType)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:MODE {protocolType}";
        }

        /// <summary>
        /// Set decoder display format
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="format">Display format</param>
        /// <returns>SCPI command string</returns>
        public static string SetDisplayFormat(int decoderNumber, DisplayFormat format)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:FORMat {format}";
        }

        /// <summary>
        /// Set decoder vertical position
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="position">Position (50-350)</param>
        /// <returns>SCPI command string</returns>
        public static string SetVerticalPosition(int decoderNumber, int position)
        {
            ValidateDecoderNumber(decoderNumber);
            if (position < 50 || position > 350)
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be between 50 and 350");

            return $":DECoder{decoderNumber}:POSition {position}";
        }

        #endregion

        #region Threshold Commands

        /// <summary>
        /// Set Channel 1 threshold voltage
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="threshold">Threshold voltage</param>
        /// <returns>SCPI command string</returns>
        public static string SetChannel1Threshold(int decoderNumber, double threshold)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:THREshold:CHANnel1 {threshold.ToString(CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// Set Channel 2 threshold voltage
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="threshold">Threshold voltage</param>
        /// <returns>SCPI command string</returns>
        public static string SetChannel2Threshold(int decoderNumber, double threshold)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:THREshold:CHANnel2 {threshold.ToString(CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// Set automatic threshold detection
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <returns>SCPI command string</returns>
        public static string SetAutoThreshold(int decoderNumber)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:THREshold:AUTO";
        }

        /// <summary>
        /// Generate commands to set both channel thresholds
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="settings">Threshold settings</param>
        /// <returns>Array of SCPI commands</returns>
        public static string[] SetThresholds(int decoderNumber, ThresholdSettings settings)
        {
            return new[]
            {
                SetChannel1Threshold(decoderNumber, settings.Channel1Threshold),
                SetChannel2Threshold(decoderNumber, settings.Channel2Threshold)
            };
        }

        #endregion

        #region UART/RS232 Commands

        /// <summary>
        /// Set UART TX channel
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="channel">Channel source</param>
        /// <returns>SCPI command string</returns>
        public static string SetUARTTxChannel(int decoderNumber, ChannelSource channel)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:UART:TX {channel}";
        }

        /// <summary>
        /// Set UART RX channel
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="channel">Channel source</param>
        /// <returns>SCPI command string</returns>
        public static string SetUARTRxChannel(int decoderNumber, ChannelSource channel)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:UART:RX {channel}";
        }

        /// <summary>
        /// Set UART baud rate
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="baudRate">Baud rate</param>
        /// <returns>SCPI command string</returns>
        public static string SetUARTBaudRate(int decoderNumber, int baudRate)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:UART:BAUD {baudRate}";
        }

        /// <summary>
        /// Set UART data width
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="width">Data width (5-9 bits)</param>
        /// <returns>SCPI command string</returns>
        public static string SetUARTDataWidth(int decoderNumber, int width)
        {
            ValidateDecoderNumber(decoderNumber);
            if (width < 5 || width > 9)
                throw new ArgumentOutOfRangeException(nameof(width), "UART data width must be between 5 and 9 bits");

            return $":DECoder{decoderNumber}:UART:WIDTh {width}";
        }

        /// <summary>
        /// Set UART stop bits
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="stopBits">Stop bits (1, 1.5, or 2)</param>
        /// <returns>SCPI command string</returns>
        public static string SetUARTStopBits(int decoderNumber, double stopBits)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:UART:STOP {stopBits.ToString(CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// Set UART parity
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="parity">Parity type</param>
        /// <returns>SCPI command string</returns>
        public static string SetUARTParity(int decoderNumber, ParityType parity)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:UART:PARity {parity}";
        }

        /// <summary>
        /// Set UART polarity
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="polarity">Polarity type</param>
        /// <returns>SCPI command string</returns>
        public static string SetUARTPolarity(int decoderNumber, PolarityType polarity)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:UART:POLarity {polarity}";
        }

        /// <summary>
        /// Set UART endian
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="endian">Endian type</param>
        /// <returns>SCPI command string</returns>
        public static string SetUARTEndian(int decoderNumber, EndianType endian)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:UART:ENDian {endian}";
        }

        /// <summary>
        /// Generate complete UART configuration command sequence
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="settings">UART settings</param>
        /// <param name="displayFormat">Display format</param>
        /// <returns>Array of SCPI commands</returns>
        public static string[] ConfigureUART(int decoderNumber, UARTSettings settings, DisplayFormat displayFormat)
        {
            return new[]
            {
                SetProtocolType(decoderNumber, ProtocolType.UART),
                SetUARTTxChannel(decoderNumber, settings.TxChannel),
                SetUARTRxChannel(decoderNumber, settings.RxChannel),
                SetUARTBaudRate(decoderNumber, settings.BaudRate),
                SetUARTDataWidth(decoderNumber, settings.DataWidth),
                SetUARTStopBits(decoderNumber, settings.StopBits),
                SetUARTParity(decoderNumber, settings.Parity),
                SetUARTPolarity(decoderNumber, settings.Polarity),
                SetUARTEndian(decoderNumber, settings.Endian),
                SetDisplayFormat(decoderNumber, displayFormat)
            };
        }

        #endregion

        #region I2C Commands

        /// <summary>
        /// Set I2C clock channel
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="channel">Channel source</param>
        /// <returns>SCPI command string</returns>
        public static string SetI2CClockChannel(int decoderNumber, ChannelSource channel)
        {
            ValidateDecoderNumber(decoderNumber);
            if (channel == ChannelSource.OFF)
                throw new ArgumentException("I2C clock channel cannot be OFF");

            return $":DECoder{decoderNumber}:IIC:CLK {channel}";
        }

        /// <summary>
        /// Set I2C data channel
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="channel">Channel source</param>
        /// <returns>SCPI command string</returns>
        public static string SetI2CDataChannel(int decoderNumber, ChannelSource channel)
        {
            ValidateDecoderNumber(decoderNumber);
            if (channel == ChannelSource.OFF)
                throw new ArgumentException("I2C data channel cannot be OFF");

            return $":DECoder{decoderNumber}:IIC:DATA {channel}";
        }

        /// <summary>
        /// Set I2C address mode
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="addressMode">Address mode</param>
        /// <returns>SCPI command string</returns>
        public static string SetI2CAddressMode(int decoderNumber, I2CAddressMode addressMode)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:IIC:ADDRess {addressMode}";
        }

        /// <summary>
        /// Generate complete I2C configuration command sequence
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="settings">I2C settings</param>
        /// <param name="displayFormat">Display format</param>
        /// <returns>Array of SCPI commands</returns>
        public static string[] ConfigureI2C(int decoderNumber, I2CSettings settings, DisplayFormat displayFormat)
        {
            return new[]
            {
                SetProtocolType(decoderNumber, ProtocolType.IIC),
                SetI2CClockChannel(decoderNumber, settings.ClockChannel),
                SetI2CDataChannel(decoderNumber, settings.DataChannel),
                SetI2CAddressMode(decoderNumber, settings.AddressMode),
                SetDisplayFormat(decoderNumber, displayFormat)
            };
        }

        #endregion

        #region SPI Commands

        /// <summary>
        /// Set SPI clock channel
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="channel">Channel source</param>
        /// <returns>SCPI command string</returns>
        public static string SetSPIClockChannel(int decoderNumber, ChannelSource channel)
        {
            ValidateDecoderNumber(decoderNumber);
            if (channel == ChannelSource.OFF)
                throw new ArgumentException("SPI clock channel cannot be OFF");

            return $":DECoder{decoderNumber}:SPI:CLK {channel}";
        }

        /// <summary>
        /// Set SPI MISO channel
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="channel">Channel source</param>
        /// <returns>SCPI command string</returns>
        public static string SetSPIMisoChannel(int decoderNumber, ChannelSource channel)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:SPI:MISO {channel}";
        }

        /// <summary>
        /// Set SPI MOSI channel
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="channel">Channel source</param>
        /// <returns>SCPI command string</returns>
        public static string SetSPIMosiChannel(int decoderNumber, ChannelSource channel)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:SPI:MOSI {channel}";
        }

        /// <summary>
        /// Set SPI CS (Chip Select) channel
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="channel">Channel source</param>
        /// <returns>SCPI command string</returns>
        public static string SetSPICSChannel(int decoderNumber, ChannelSource channel)
        {
            ValidateDecoderNumber(decoderNumber);
            if (channel == ChannelSource.OFF)
                throw new ArgumentException("SPI CS channel cannot be OFF");

            return $":DECoder{decoderNumber}:SPI:CS {channel}";
        }

        /// <summary>
        /// Set SPI data width
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="width">Data width (8-32 bits)</param>
        /// <returns>SCPI command string</returns>
        public static string SetSPIDataWidth(int decoderNumber, int width)
        {
            ValidateDecoderNumber(decoderNumber);
            if (width < 8 || width > 32)
                throw new ArgumentOutOfRangeException(nameof(width), "SPI data width must be between 8 and 32 bits");

            return $":DECoder{decoderNumber}:SPI:WIDTh {width}";
        }

        /// <summary>
        /// Set SPI clock polarity
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="polarity">Polarity type</param>
        /// <returns>SCPI command string</returns>
        public static string SetSPIClockPolarity(int decoderNumber, PolarityType polarity)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:SPI:POLarity {polarity}";
        }

        /// <summary>
        /// Set SPI clock edge
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="edge">Edge type</param>
        /// <returns>SCPI command string</returns>
        public static string SetSPIClockEdge(int decoderNumber, PolarityType edge)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:SPI:EDGE {edge}";
        }

        /// <summary>
        /// Set SPI endian
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="endian">Endian type</param>
        /// <returns>SCPI command string</returns>
        public static string SetSPIEndian(int decoderNumber, EndianType endian)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:SPI:ENDian {endian}";
        }

        /// <summary>
        /// Generate complete SPI configuration command sequence
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="settings">SPI settings</param>
        /// <param name="displayFormat">Display format</param>
        /// <returns>Array of SCPI commands</returns>
        public static string[] ConfigureSPI(int decoderNumber, SPISettings settings, DisplayFormat displayFormat)
        {
            return new[]
            {
                SetProtocolType(decoderNumber, ProtocolType.SPI),
                SetSPIClockChannel(decoderNumber, settings.ClockChannel),
                SetSPIMisoChannel(decoderNumber, settings.MisoChannel),
                SetSPIMosiChannel(decoderNumber, settings.MosiChannel),
                SetSPICSChannel(decoderNumber, settings.CsChannel),
                SetSPIDataWidth(decoderNumber, settings.DataWidth),
                SetSPIClockPolarity(decoderNumber, settings.ClockPolarity),
                SetSPIClockEdge(decoderNumber, settings.ClockEdge),
                SetSPIEndian(decoderNumber, settings.Endian),
                SetDisplayFormat(decoderNumber, displayFormat)
            };
        }

        #endregion

        #region Parallel Commands

        /// <summary>
        /// Set parallel clock channel
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="channel">Channel source</param>
        /// <returns>SCPI command string</returns>
        public static string SetParallelClockChannel(int decoderNumber, ChannelSource channel)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:PARallel:CLK {channel}";
        }

        /// <summary>
        /// Set parallel clock edge
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="edge">Edge type</param>
        /// <returns>SCPI command string</returns>
        public static string SetParallelClockEdge(int decoderNumber, EdgeType edge)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":DECoder{decoderNumber}:PARallel:EDGE {edge}";
        }

        /// <summary>
        /// Set parallel data width
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="width">Data width (1-16 bits)</param>
        /// <returns>SCPI command string</returns>
        public static string SetParallelDataWidth(int decoderNumber, int width)
        {
            ValidateDecoderNumber(decoderNumber);
            if (width < 1 || width > 16)
                throw new ArgumentOutOfRangeException(nameof(width), "Parallel data width must be between 1 and 16 bits");

            return $":DECoder{decoderNumber}:PARallel:WIDTh {width}";
        }

        /// <summary>
        /// Generate complete parallel configuration command sequence
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="settings">Parallel settings</param>
        /// <param name="displayFormat">Display format</param>
        /// <returns>Array of SCPI commands</returns>
        public static string[] ConfigureParallel(int decoderNumber, ParallelSettings settings, DisplayFormat displayFormat)
        {
            return new[]
            {
                SetProtocolType(decoderNumber, ProtocolType.PARallel),
                SetParallelClockChannel(decoderNumber, settings.ClockChannel),
                SetParallelClockEdge(decoderNumber, settings.ClockEdge),
                SetParallelDataWidth(decoderNumber, settings.DataWidth),
                SetDisplayFormat(decoderNumber, displayFormat)
            };
        }

        #endregion

        #region Event Table Commands

        /// <summary>
        /// Enable or disable event table display
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="enabled">True to enable, false to disable</param>
        /// <returns>SCPI command string</returns>
        public static string SetEventTableEnabled(int decoderNumber, bool enabled)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":ETABle{decoderNumber}:DISP {(enabled ? "ON" : "OFF")}";
        }

        /// <summary>
        /// Set event table display format
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="format">Display format (HEX, ASCii, or DEC only)</param>
        /// <returns>SCPI command string</returns>
        public static string SetEventTableFormat(int decoderNumber, DisplayFormat format)
        {
            ValidateDecoderNumber(decoderNumber);
            if (format != DisplayFormat.HEX && format != DisplayFormat.ASCii && format != DisplayFormat.DEC)
                throw new ArgumentException("Event table format must be HEX, ASCii, or DEC");

            return $":ETABle{decoderNumber}:FORMat {format}";
        }

        /// <summary>
        /// Set event table view mode
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="viewMode">View mode</param>
        /// <returns>SCPI command string</returns>
        public static string SetEventTableView(int decoderNumber, EventTableView viewMode)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":ETABle{decoderNumber}:VIEW {viewMode}";
        }

        /// <summary>
        /// Set event table column
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="column">Column selection</param>
        /// <returns>SCPI command string</returns>
        public static string SetEventTableColumn(int decoderNumber, EventTableColumn column)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":ETABle{decoderNumber}:COLumn {column}";
        }

        /// <summary>
        /// Set event table sort order
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="sortOrder">Sort order</param>
        /// <returns>SCPI command string</returns>
        public static string SetEventTableSort(int decoderNumber, SortOrder sortOrder)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":ETABle{decoderNumber}:SORT {sortOrder}";
        }

        /// <summary>
        /// Set event table current row
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="row">Row number</param>
        /// <returns>SCPI command string</returns>
        public static string SetEventTableRow(int decoderNumber, int row)
        {
            ValidateDecoderNumber(decoderNumber);
            if (row < 1)
                throw new ArgumentOutOfRangeException(nameof(row), "Row number must be 1 or greater");

            return $":ETABle{decoderNumber}:ROW {row}";
        }

        /// <summary>
        /// Query event table data
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <returns>SCPI query command</returns>
        public static string QueryEventTableData(int decoderNumber)
        {
            ValidateDecoderNumber(decoderNumber);
            return $":ETABle{decoderNumber}:DATA?";
        }

        /// <summary>
        /// Generate complete event table configuration command sequence
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <param name="settings">Event table settings</param>
        /// <returns>Array of SCPI commands</returns>
        public static string[] ConfigureEventTable(int decoderNumber, EventTableSettings settings)
        {
            var commands = new List<string>();

            if (settings.Enabled)
            {
                commands.Add(SetEventTableEnabled(decoderNumber, true));
                commands.Add(SetEventTableFormat(decoderNumber, settings.Format));
                commands.Add(SetEventTableView(decoderNumber, settings.ViewMode));
                commands.Add(SetEventTableColumn(decoderNumber, settings.Column));
                commands.Add(SetEventTableSort(decoderNumber, settings.SortOrder));

                if (settings.CurrentRow > 1)
                {
                    commands.Add(SetEventTableRow(decoderNumber, settings.CurrentRow));
                }
            }
            else
            {
                commands.Add(SetEventTableEnabled(decoderNumber, false));
            }

            return commands.ToArray();
        }

        #endregion

        #region Complete Configuration Methods

        /// <summary>
        /// Generate complete decoder configuration based on settings
        /// </summary>
        /// <param name="settings">Complete decoder settings</param>
        /// <returns>Array of SCPI commands</returns>
        public static string[] ConfigureDecoder(DecoderSettings settings)
        {
            var commands = new List<string>();

            // Basic decoder setup
            commands.Add(SetProtocolType(settings.DecoderNumber, settings.ProtocolType));
            commands.Add(SetDisplayFormat(settings.DecoderNumber, settings.DisplayFormat));
            commands.Add(SetVerticalPosition(settings.DecoderNumber, settings.VerticalPosition));

            // Threshold settings
            commands.AddRange(SetThresholds(settings.DecoderNumber, settings.Thresholds));

            // Protocol-specific configuration
            switch (settings.ProtocolType)
            {
                case ProtocolType.UART:
                    commands.AddRange(ConfigureUART(settings.DecoderNumber, settings.UART, settings.DisplayFormat).Skip(2)); // Skip duplicate commands
                    break;
                case ProtocolType.IIC:
                    commands.AddRange(ConfigureI2C(settings.DecoderNumber, settings.I2C, settings.DisplayFormat).Skip(2));
                    break;
                case ProtocolType.SPI:
                    commands.AddRange(ConfigureSPI(settings.DecoderNumber, settings.SPI, settings.DisplayFormat).Skip(2));
                    break;
                case ProtocolType.PARallel:
                    commands.AddRange(ConfigureParallel(settings.DecoderNumber, settings.Parallel, settings.DisplayFormat).Skip(2));
                    break;
            }

            // Event table configuration
            commands.AddRange(ConfigureEventTable(settings.DecoderNumber, settings.EventTable));

            // Enable decoder if requested
            if (settings.Enabled)
            {
                commands.Add(SetDecoderEnabled(settings.DecoderNumber, true));
            }

            return commands.ToArray();
        }

        /// <summary>
        /// Generate query commands to read current decoder configuration
        /// </summary>
        /// <param name="decoderNumber">Decoder number (1 or 2)</param>
        /// <returns>Array of SCPI query commands</returns>
        public static string[] QueryDecoderConfiguration(int decoderNumber)
        {
            ValidateDecoderNumber(decoderNumber);

            return new[]
            {
                QueryDecoderEnabled(decoderNumber),
                $":DECoder{decoderNumber}:MODE?",
                $":DECoder{decoderNumber}:FORMat?",
                $":DECoder{decoderNumber}:POSition?",
                $":DECoder{decoderNumber}:THREshold:CHANnel1?",
                $":DECoder{decoderNumber}:THREshold:CHANnel2?",
                $":ETABle{decoderNumber}:DISP?"
            };
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Validate decoder number parameter
        /// </summary>
        /// <param name="decoderNumber">Decoder number to validate</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if decoder number is invalid</exception>
        private static void ValidateDecoderNumber(int decoderNumber)
        {
            if (decoderNumber < 1 || decoderNumber > 2)
                throw new ArgumentOutOfRangeException(nameof(decoderNumber), "Decoder number must be 1 or 2");
        }

        /// <summary>
        /// Format multiple commands for batch sending
        /// </summary>
        /// <param name="commands">Array of commands</param>
        /// <param name="delimiter">Command delimiter (default is semicolon)</param>
        /// <returns>Formatted command string</returns>
        public static string FormatBatchCommands(string[] commands, string delimiter = ";")
        {
            return string.Join(delimiter, commands.Where(cmd => !string.IsNullOrWhiteSpace(cmd)));
        }

        /// <summary>
        /// Get recommended baud rates for UART configuration
        /// </summary>
        /// <returns>Array of common baud rates</returns>
        public static int[] GetStandardBaudRates()
        {
            return new[] { 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 };
        }

        /// <summary>
        /// Validate protocol-specific settings
        /// </summary>
        /// <param name="settings">Decoder settings to validate</param>
        /// <returns>Validation error message or empty string if valid</returns>
        public static string ValidateConfiguration(DecoderSettings settings)
        {
            try
            {
                switch (settings.ProtocolType)
                {
                    case ProtocolType.UART:
                        if (settings.UART.TxChannel == ChannelSource.OFF && settings.UART.RxChannel == ChannelSource.OFF)
                            return "UART requires at least one channel (TX or RX) to be enabled";
                        break;

                    case ProtocolType.SPI:
                        if (settings.SPI.MisoChannel == ChannelSource.OFF && settings.SPI.MosiChannel == ChannelSource.OFF)
                            return "SPI requires at least one data channel (MISO or MOSI) to be enabled";
                        break;

                    case ProtocolType.IIC:
                        if (settings.I2C.ClockChannel == settings.I2C.DataChannel)
                            return "I2C clock and data channels cannot be the same";
                        if (settings.I2C.ClockChannel == ChannelSource.OFF || settings.I2C.DataChannel == ChannelSource.OFF)
                            return "I2C requires both clock and data channels to be assigned";
                        break;
                }

                return string.Empty; // No validation errors
            }
            catch (Exception ex)
            {
                return $"Validation error: {ex.Message}";
            }
        }

        #endregion
    }
}