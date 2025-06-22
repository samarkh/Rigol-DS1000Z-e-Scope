using System;
using System.ComponentModel;

namespace DS1000Z_E_USB_Control.SerialProtocol
{
    #region Enums

    /// <summary>
    /// Available protocol types for decoding
    /// </summary>
    public enum ProtocolType
    {
        UART,
        IIC,
        SPI,
        PARallel
    }

    /// <summary>
    /// Display format options for decoded data
    /// </summary>
    public enum DisplayFormat
    {
        HEX,
        ASCii,
        DEC,
        BIN,
        LINE
    }

    /// <summary>
    /// Channel assignment options
    /// </summary>
    public enum ChannelSource
    {
        CHANnel1,
        CHANnel2,
        OFF
    }

    /// <summary>
    /// UART parity options
    /// </summary>
    public enum ParityType
    {
        NONE,
        EVEN,
        ODD
    }

    /// <summary>
    /// Signal polarity options
    /// </summary>
    public enum PolarityType
    {
        POSitive,
        NEGative
    }

    /// <summary>
    /// Data endian options
    /// </summary>
    public enum EndianType
    {
        LSB,
        MSB
    }

    /// <summary>
    /// I2C address mode options
    /// </summary>
    public enum I2CAddressMode
    {
        NORMal,  // 7-bit address
        RW       // 7-bit address + R/W bit
    }

    /// <summary>
    /// Clock edge detection options
    /// </summary>
    public enum EdgeType
    {
        RISE,
        FALL,
        BOTH
    }

    /// <summary>
    /// Event table view modes
    /// </summary>
    public enum EventTableView
    {
        PACKage,
        DETail,
        PAYLoad
    }

    /// <summary>
    /// Event table column options
    /// </summary>
    public enum EventTableColumn
    {
        DATA,
        TX,
        RX,
        MISO,
        MOSI
    }

    /// <summary>
    /// Sort order options
    /// </summary>
    public enum SortOrder
    {
        ASCend,
        DESCend
    }

    #endregion

    #region Settings Classes

    /// <summary>
    /// UART/RS232 specific configuration settings
    /// </summary>
    public class UARTSettings : INotifyPropertyChanged
    {
        private ChannelSource _txChannel = ChannelSource.CHANnel1;
        private ChannelSource _rxChannel = ChannelSource.CHANnel2;
        private int _baudRate = 115200;
        private int _dataWidth = 8;
        private double _stopBits = 1.0;
        private ParityType _parity = ParityType.NONE;
        private PolarityType _polarity = PolarityType.POSitive;
        private EndianType _endian = EndianType.LSB;

        public ChannelSource TxChannel
        {
            get => _txChannel;
            set { _txChannel = value; OnPropertyChanged(nameof(TxChannel)); }
        }

        public ChannelSource RxChannel
        {
            get => _rxChannel;
            set { _rxChannel = value; OnPropertyChanged(nameof(RxChannel)); }
        }

        public int BaudRate
        {
            get => _baudRate;
            set { _baudRate = value; OnPropertyChanged(nameof(BaudRate)); }
        }

        public int DataWidth
        {
            get => _dataWidth;
            set { _dataWidth = Math.Max(5, Math.Min(9, value)); OnPropertyChanged(nameof(DataWidth)); }
        }

        public double StopBits
        {
            get => _stopBits;
            set { _stopBits = value; OnPropertyChanged(nameof(StopBits)); }
        }

        public ParityType Parity
        {
            get => _parity;
            set { _parity = value; OnPropertyChanged(nameof(Parity)); }
        }

        public PolarityType Polarity
        {
            get => _polarity;
            set { _polarity = value; OnPropertyChanged(nameof(Polarity)); }
        }

        public EndianType Endian
        {
            get => _endian;
            set { _endian = value; OnPropertyChanged(nameof(Endian)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// I2C specific configuration settings
    /// </summary>
    public class I2CSettings : INotifyPropertyChanged
    {
        private ChannelSource _clockChannel = ChannelSource.CHANnel1;
        private ChannelSource _dataChannel = ChannelSource.CHANnel2;
        private I2CAddressMode _addressMode = I2CAddressMode.NORMal;

        public ChannelSource ClockChannel
        {
            get => _clockChannel;
            set { _clockChannel = value; OnPropertyChanged(nameof(ClockChannel)); }
        }

        public ChannelSource DataChannel
        {
            get => _dataChannel;
            set { _dataChannel = value; OnPropertyChanged(nameof(DataChannel)); }
        }

        public I2CAddressMode AddressMode
        {
            get => _addressMode;
            set { _addressMode = value; OnPropertyChanged(nameof(AddressMode)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// SPI specific configuration settings
    /// </summary>
    public class SPISettings : INotifyPropertyChanged
    {
        private ChannelSource _clockChannel = ChannelSource.CHANnel1;
        private ChannelSource _misoChannel = ChannelSource.CHANnel2;
        private ChannelSource _mosiChannel = ChannelSource.CHANnel1;
        private ChannelSource _csChannel = ChannelSource.CHANnel1;
        private int _dataWidth = 8;
        private PolarityType _clockPolarity = PolarityType.POSitive;
        private PolarityType _clockEdge = PolarityType.POSitive;
        private EndianType _endian = EndianType.LSB;

        public ChannelSource ClockChannel
        {
            get => _clockChannel;
            set { _clockChannel = value; OnPropertyChanged(nameof(ClockChannel)); }
        }

        public ChannelSource MisoChannel
        {
            get => _misoChannel;
            set { _misoChannel = value; OnPropertyChanged(nameof(MisoChannel)); }
        }

        public ChannelSource MosiChannel
        {
            get => _mosiChannel;
            set { _mosiChannel = value; OnPropertyChanged(nameof(MosiChannel)); }
        }

        public ChannelSource CsChannel
        {
            get => _csChannel;
            set { _csChannel = value; OnPropertyChanged(nameof(CsChannel)); }
        }

        public int DataWidth
        {
            get => _dataWidth;
            set { _dataWidth = Math.Max(8, Math.Min(32, value)); OnPropertyChanged(nameof(DataWidth)); }
        }

        public PolarityType ClockPolarity
        {
            get => _clockPolarity;
            set { _clockPolarity = value; OnPropertyChanged(nameof(ClockPolarity)); }
        }

        public PolarityType ClockEdge
        {
            get => _clockEdge;
            set { _clockEdge = value; OnPropertyChanged(nameof(ClockEdge)); }
        }

        public EndianType Endian
        {
            get => _endian;
            set { _endian = value; OnPropertyChanged(nameof(Endian)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Parallel decoding specific configuration settings
    /// </summary>
    public class ParallelSettings : INotifyPropertyChanged
    {
        private ChannelSource _clockChannel = ChannelSource.CHANnel1;
        private EdgeType _clockEdge = EdgeType.RISE;
        private int _dataWidth = 8;

        public ChannelSource ClockChannel
        {
            get => _clockChannel;
            set { _clockChannel = value; OnPropertyChanged(nameof(ClockChannel)); }
        }

        public EdgeType ClockEdge
        {
            get => _clockEdge;
            set { _clockEdge = value; OnPropertyChanged(nameof(ClockEdge)); }
        }

        public int DataWidth
        {
            get => _dataWidth;
            set { _dataWidth = Math.Max(1, Math.Min(16, value)); OnPropertyChanged(nameof(DataWidth)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Event table configuration settings
    /// </summary>
    public class EventTableSettings : INotifyPropertyChanged
    {
        private bool _enabled = false;
        private DisplayFormat _format = DisplayFormat.HEX;
        private EventTableView _viewMode = EventTableView.PACKage;
        private EventTableColumn _column = EventTableColumn.DATA;
        private SortOrder _sortOrder = SortOrder.ASCend;
        private int _currentRow = 1;

        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; OnPropertyChanged(nameof(Enabled)); }
        }

        public DisplayFormat Format
        {
            get => _format;
            set { _format = value; OnPropertyChanged(nameof(Format)); }
        }

        public EventTableView ViewMode
        {
            get => _viewMode;
            set { _viewMode = value; OnPropertyChanged(nameof(ViewMode)); }
        }

        public EventTableColumn Column
        {
            get => _column;
            set { _column = value; OnPropertyChanged(nameof(Column)); }
        }

        public SortOrder SortOrder
        {
            get => _sortOrder;
            set { _sortOrder = value; OnPropertyChanged(nameof(SortOrder)); }
        }

        public int CurrentRow
        {
            get => _currentRow;
            set { _currentRow = Math.Max(1, value); OnPropertyChanged(nameof(CurrentRow)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Threshold settings for decoder channels
    /// </summary>
    public class ThresholdSettings : INotifyPropertyChanged
    {
        private double _channel1Threshold = 1.4;
        private double _channel2Threshold = 1.4;

        public double Channel1Threshold
        {
            get => _channel1Threshold;
            set { _channel1Threshold = value; OnPropertyChanged(nameof(Channel1Threshold)); }
        }

        public double Channel2Threshold
        {
            get => _channel2Threshold;
            set { _channel2Threshold = value; OnPropertyChanged(nameof(Channel2Threshold)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    /// <summary>
    /// Complete decoder configuration settings
    /// </summary>
    public class DecoderSettings : INotifyPropertyChanged
    {
        private int _decoderNumber = 1;
        private bool _enabled = false;
        private ProtocolType _protocolType = ProtocolType.UART;
        private DisplayFormat _displayFormat = DisplayFormat.HEX;
        private int _verticalPosition = 350;

        #region Basic Properties

        public int DecoderNumber
        {
            get => _decoderNumber;
            set { _decoderNumber = Math.Max(1, Math.Min(2, value)); OnPropertyChanged(nameof(DecoderNumber)); }
        }

        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; OnPropertyChanged(nameof(Enabled)); }
        }

        public ProtocolType ProtocolType
        {
            get => _protocolType;
            set { _protocolType = value; OnPropertyChanged(nameof(ProtocolType)); }
        }

        public DisplayFormat DisplayFormat
        {
            get => _displayFormat;
            set { _displayFormat = value; OnPropertyChanged(nameof(DisplayFormat)); }
        }

        public int VerticalPosition
        {
            get => _verticalPosition;
            set { _verticalPosition = Math.Max(50, Math.Min(350, value)); OnPropertyChanged(nameof(VerticalPosition)); }
        }

        #endregion

        #region Protocol-Specific Settings

        public UARTSettings UART { get; set; } = new UARTSettings();
        public I2CSettings I2C { get; set; } = new I2CSettings();
        public SPISettings SPI { get; set; } = new SPISettings();
        public ParallelSettings Parallel { get; set; } = new ParallelSettings();

        #endregion

        #region Additional Settings

        public EventTableSettings EventTable { get; set; } = new EventTableSettings();
        public ThresholdSettings Thresholds { get; set; } = new ThresholdSettings();


        /// <summary>
        /// Configuration version for compatibility tracking
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Timestamp when configuration was created or last modified
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        #endregion

        #region Methods

        /// <summary>
        /// Reset all settings to their default values
        /// </summary>
        public void ResetToDefaults()
        {
            DecoderNumber = 1;
            Enabled = false;
            ProtocolType = ProtocolType.UART;
            DisplayFormat = DisplayFormat.HEX;
            VerticalPosition = 350;

            // Reset protocol-specific settings
            UART = new UARTSettings();
            I2C = new I2CSettings();
            SPI = new SPISettings();
            Parallel = new ParallelSettings();

            // Reset additional settings
            EventTable = new EventTableSettings();
            Thresholds = new ThresholdSettings();

            OnPropertyChanged(null); // Notify all properties changed
        }

        /// <summary>
        /// Create a copy of the current settings
        /// </summary>
        /// <returns>Deep copy of decoder settings</returns>
        public DecoderSettings Clone()
        {
            var clone = new DecoderSettings
            {
                DecoderNumber = this.DecoderNumber,
                Enabled = this.Enabled,
                ProtocolType = this.ProtocolType,
                DisplayFormat = this.DisplayFormat,
                VerticalPosition = this.VerticalPosition
            };

            // Copy UART settings
            clone.UART.TxChannel = this.UART.TxChannel;
            clone.UART.RxChannel = this.UART.RxChannel;
            clone.UART.BaudRate = this.UART.BaudRate;
            clone.UART.DataWidth = this.UART.DataWidth;
            clone.UART.StopBits = this.UART.StopBits;
            clone.UART.Parity = this.UART.Parity;
            clone.UART.Polarity = this.UART.Polarity;
            clone.UART.Endian = this.UART.Endian;

            // Copy I2C settings
            clone.I2C.ClockChannel = this.I2C.ClockChannel;
            clone.I2C.DataChannel = this.I2C.DataChannel;
            clone.I2C.AddressMode = this.I2C.AddressMode;

            // Copy SPI settings
            clone.SPI.ClockChannel = this.SPI.ClockChannel;
            clone.SPI.MisoChannel = this.SPI.MisoChannel;
            clone.SPI.MosiChannel = this.SPI.MosiChannel;
            clone.SPI.CsChannel = this.SPI.CsChannel;
            clone.SPI.DataWidth = this.SPI.DataWidth;
            clone.SPI.ClockPolarity = this.SPI.ClockPolarity;
            clone.SPI.ClockEdge = this.SPI.ClockEdge;
            clone.SPI.Endian = this.SPI.Endian;

            // Copy Parallel settings
            clone.Parallel.ClockChannel = this.Parallel.ClockChannel;
            clone.Parallel.ClockEdge = this.Parallel.ClockEdge;
            clone.Parallel.DataWidth = this.Parallel.DataWidth;

            // Copy Event Table settings
            clone.EventTable.Enabled = this.EventTable.Enabled;
            clone.EventTable.Format = this.EventTable.Format;
            clone.EventTable.ViewMode = this.EventTable.ViewMode;
            clone.EventTable.Column = this.EventTable.Column;
            clone.EventTable.SortOrder = this.EventTable.SortOrder;
            clone.EventTable.CurrentRow = this.EventTable.CurrentRow;

            // Copy Threshold settings
            clone.Thresholds.Channel1Threshold = this.Thresholds.Channel1Threshold;
            clone.Thresholds.Channel2Threshold = this.Thresholds.Channel2Threshold;

            return clone;
        }

        /// <summary>
        /// Get a summary string of the current configuration
        /// </summary>
        /// <returns>Configuration summary</returns>
        public string GetConfigurationSummary()
        {
            return $"Decoder {DecoderNumber}: {ProtocolType} Protocol, {DisplayFormat} Format, " +
                   $"Position: {VerticalPosition}, Enabled: {Enabled}, Table: {EventTable.Enabled}";
        }

        /// <summary>
        /// Validate current settings and return any issues
        /// </summary>
        /// <returns>Validation error message or empty string if valid</returns>
        public string ValidateSettings()
        {
            switch (ProtocolType)
            {
                case ProtocolType.UART:
                    if (UART.TxChannel == ChannelSource.OFF && UART.RxChannel == ChannelSource.OFF)
                        return "UART requires at least one channel (TX or RX) to be enabled";
                    break;

                case ProtocolType.SPI:
                    if (SPI.MisoChannel == ChannelSource.OFF && SPI.MosiChannel == ChannelSource.OFF)
                        return "SPI requires at least one data channel (MISO or MOSI) to be enabled";
                    break;

                case ProtocolType.IIC:
                    if (I2C.ClockChannel == I2C.DataChannel)
                        return "I2C clock and data channels cannot be the same";
                    break;
            }

            return string.Empty; // No validation errors
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}