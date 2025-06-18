using System;
using System.Linq;

namespace OscilloscopeControl.Capture
{
    /// <summary>
    /// Specifies which channel(s) to capture waveform data from
    /// </summary>
    public enum WaveformChannel
    {
        /// <summary>Capture from Channel 1 only</summary>
        Channel1,

        /// <summary>Capture from Channel 2 only</summary>
        Channel2,

        /// <summary>Capture from both Channel 1 and Channel 2</summary>
        Both
    }

    /// <summary>
    /// Specifies the waveform data format from the oscilloscope
    /// Based on DS1000Z-E :WAVeform:FORMat command options
    /// </summary>
    public enum WaveformFormat
    {
        /// <summary>8-bit byte format (default, fastest transfer)</summary>
        BYTE,

        /// <summary>16-bit word format (higher resolution)</summary>
        WORD,

        /// <summary>ASCII text format (human readable, slower transfer)</summary>
        ASCii
    }

    /// <summary>
    /// Specifies the waveform capture mode
    /// Based on DS1000Z-E :WAVeform:MODE command options
    /// </summary>
    public enum WaveformMode
    {
        /// <summary>Normal display mode - screen data only</summary>
        NORMal,

        /// <summary>Raw mode - access to full memory depth</summary>
        RAW,

        /// <summary>Maximum mode - maximum available data points</summary>
        MAXimum
    }

    /// <summary>
    /// Contains all data and metadata for a captured waveform from the oscilloscope
    /// </summary>
    public class WaveformData
    {
        #region Basic Information

        /// <summary>
        /// Human-readable channel name (e.g., "Channel 1", "Channel 2")
        /// </summary>
        public string ChannelName { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when this waveform was captured
        /// </summary>
        public DateTime CaptureTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Unique identifier for this waveform capture
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        #endregion

        #region Waveform Data Arrays

        /// <summary>
        /// Time values array in seconds (X-axis data)
        /// </summary>
        public double[] TimeValues { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Voltage values array in volts (Y-axis data)
        /// </summary>
        public double[] VoltageValues { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Original raw data bytes from the oscilloscope
        /// Preserved for potential reprocessing with different parameters
        /// </summary>
        public byte[] RawData { get; set; } = Array.Empty<byte>();

        #endregion

        #region Timing Parameters

        /// <summary>
        /// Sample rate in samples per second (Sa/s)
        /// From :ACQuire:SRATe? command
        /// </summary>
        public double SampleRate { get; set; } = 0.0;

        /// <summary>
        /// Time increment between samples in seconds
        /// From :WAVeform:XINCrement? command
        /// </summary>
        public double TimeIncrement { get; set; } = 0.0;

        /// <summary>
        /// Time origin offset in seconds
        /// From :WAVeform:XORigin? command
        /// </summary>
        public double TimeOrigin { get; set; } = 0.0;

        /// <summary>
        /// Time reference point
        /// From :WAVeform:XREFerence? command
        /// </summary>
        public double TimeReference { get; set; } = 0.0;

        #endregion

        #region Voltage Parameters

        /// <summary>
        /// Voltage increment per data point in volts
        /// From :WAVeform:YINCrement? command
        /// </summary>
        public double VoltageIncrement { get; set; } = 0.0;

        /// <summary>
        /// Voltage origin offset in volts
        /// From :WAVeform:YORigin? command
        /// </summary>
        public double VoltageOrigin { get; set; } = 0.0;

        /// <summary>
        /// Voltage reference point
        /// From :WAVeform:YREFerence? command
        /// </summary>
        public double VoltageReference { get; set; } = 0.0;

        /// <summary>
        /// Channel vertical scale setting in volts/division
        /// From :CHANnel<n>:SCALe? command
        /// </summary>
        public double VerticalScale { get; set; } = 0.0;

        /// <summary>
        /// Channel vertical offset in volts
        /// From :CHANnel<n>:OFFSet? command
        /// </summary>
        public double VerticalOffset { get; set; } = 0.0;

        #endregion

        #region Channel Configuration

        /// <summary>
        /// Channel coupling setting (AC, DC, GND)
        /// From :CHANnel<n>:COUPling? command
        /// </summary>
        public string Coupling { get; set; } = string.Empty;

        /// <summary>
        /// Probe attenuation ratio (1X, 10X, 100X, etc.)
        /// From :CHANnel<n>:PROBe? command
        /// </summary>
        public double ProbeRatio { get; set; } = 1.0;

        /// <summary>
        /// Bandwidth limit setting
        /// From :CHANnel<n>:BWLimit? command
        /// </summary>
        public string BandwidthLimit { get; set; } = string.Empty;

        /// <summary>
        /// Channel measurement units
        /// From :CHANnel<n>:UNITs? command
        /// </summary>
        public string Units { get; set; } = "V";

        #endregion

        #region Acquisition Information

        /// <summary>
        /// Total number of data points captured
        /// </summary>
        public int PointCount => VoltageValues.Length;

        /// <summary>
        /// Memory depth used for this capture
        /// From :ACQuire:MDEPth? command
        /// </summary>
        public string MemoryDepth { get; set; } = string.Empty;

        /// <summary>
        /// Acquisition type (Normal, Average, Peak, High Resolution)
        /// From :ACQuire:TYPE? command
        /// </summary>
        public string AcquisitionType { get; set; } = string.Empty;

        /// <summary>
        /// Number of averages if in average mode
        /// From :ACQuire:AVERages? command
        /// </summary>
        public int AverageCount { get; set; } = 0;

        #endregion

        #region Trigger Information

        /// <summary>
        /// Trigger mode (Edge, Pulse, Video, etc.)
        /// From :TRIGger:MODE? command
        /// </summary>
        public string TriggerMode { get; set; } = string.Empty;

        /// <summary>
        /// Trigger source channel
        /// From :TRIGger:EDGe:SOURce? command
        /// </summary>
        public string TriggerSource { get; set; } = string.Empty;

        /// <summary>
        /// Trigger level in volts
        /// From :TRIGger:EDGe:LEVel? command
        /// </summary>
        public double TriggerLevel { get; set; } = 0.0;

        /// <summary>
        /// Trigger slope (Positive, Negative)
        /// From :TRIGger:EDGe:SLOPe? command
        /// </summary>
        public string TriggerSlope { get; set; } = string.Empty;

        /// <summary>
        /// Trigger coupling (AC, DC, LF Reject, HF Reject)
        /// From :TRIGger:COUPling? command
        /// </summary>
        public string TriggerCoupling { get; set; } = string.Empty;

        #endregion

        #region Timebase Information

        /// <summary>
        /// Horizontal timebase scale in seconds/division
        /// From :TIMebase:SCALe? command
        /// </summary>
        public double TimebaseScale { get; set; } = 0.0;

        /// <summary>
        /// Horizontal timebase offset in seconds
        /// From :TIMebase:OFFSet? command
        /// </summary>
        public double TimebaseOffset { get; set; } = 0.0;

        /// <summary>
        /// Timebase mode (Main, Delayed)
        /// From :TIMebase:MODE? command
        /// </summary>
        public string TimebaseMode { get; set; } = string.Empty;

        #endregion

        #region Capture Settings

        /// <summary>
        /// The capture mode used for this waveform
        /// </summary>
        public WaveformMode CaptureMode { get; set; } = WaveformMode.NORMal;

        /// <summary>
        /// The data format used for this waveform
        /// </summary>
        public WaveformFormat DataFormat { get; set; } = WaveformFormat.BYTE;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Total duration of the captured waveform in seconds
        /// </summary>
        public double Duration => TimeValues.Length > 0 ? TimeValues.Last() - TimeValues.First() : 0.0;

        /// <summary>
        /// Minimum voltage value in the waveform
        /// </summary>
        public double MinVoltage => VoltageValues.Length > 0 ? VoltageValues.Min() : 0.0;

        /// <summary>
        /// Maximum voltage value in the waveform
        /// </summary>
        public double MaxVoltage => VoltageValues.Length > 0 ? VoltageValues.Max() : 0.0;

        /// <summary>
        /// Peak-to-peak voltage of the waveform
        /// </summary>
        public double PeakToPeak => MaxVoltage - MinVoltage;

        /// <summary>
        /// RMS voltage of the waveform
        /// </summary>
        public double RmsVoltage
        {
            get
            {
                if (VoltageValues.Length == 0) return 0.0;

                double sumSquares = VoltageValues.Sum(v => v * v);
                return Math.Sqrt(sumSquares / VoltageValues.Length);
            }
        }

        /// <summary>
        /// Average voltage of the waveform
        /// </summary>
        public double AverageVoltage => VoltageValues.Length > 0 ? VoltageValues.Average() : 0.0;

        /// <summary>
        /// Estimated file size if exported to CSV (in bytes)
        /// </summary>
        public long EstimatedCsvSize
        {
            get
            {
                // Rough estimate: each line has ~30 characters (time,voltage + formatting)
                const int bytesPerLine = 30;
                const int headerBytes = 500; // Metadata header
                return headerBytes + (PointCount * bytesPerLine);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a summary string with key waveform information
        /// </summary>
        public string GetSummary()
        {
            return $"{ChannelName}: {PointCount:N0} points, " +
                   $"{Duration * 1000:F3}ms, " +
                   $"{MinVoltage:F3}V to {MaxVoltage:F3}V, " +
                   $"RMS: {RmsVoltage:F3}V";
        }

        /// <summary>
        /// Creates a detailed information string for display
        /// </summary>
        public string GetDetailedInfo()
        {
            return $"""
                Channel: {ChannelName}
                Captured: {CaptureTime:yyyy-MM-dd HH:mm:ss}
                
                Waveform Data:
                  Points: {PointCount:N0}
                  Duration: {Duration * 1000:F3} ms
                  Sample Rate: {SampleRate:E2} Sa/s
                  
                Voltage Analysis:
                  Range: {MinVoltage:F3}V to {MaxVoltage:F3}V
                  Peak-to-Peak: {PeakToPeak:F3}V
                  RMS: {RmsVoltage:F3}V
                  Average: {AverageVoltage:F3}V
                  
                Channel Settings:
                  Vertical Scale: {VerticalScale:F3}V/div
                  Vertical Offset: {VerticalOffset:F3}V
                  Coupling: {Coupling}
                  Probe Ratio: {ProbeRatio}X
                  
                Trigger:
                  Mode: {TriggerMode}
                  Source: {TriggerSource}
                  Level: {TriggerLevel:F3}V
                  Slope: {TriggerSlope}
                  
                Timebase:
                  Scale: {TimebaseScale:E2}s/div
                  Offset: {TimebaseOffset:E2}s
                  Mode: {TimebaseMode}
                  
                Acquisition:
                  Type: {AcquisitionType}
                  Memory Depth: {MemoryDepth}
                  Capture Mode: {CaptureMode}
                  Data Format: {DataFormat}
                """;
        }

        /// <summary>
        /// Validates that the waveform data is complete and consistent
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ChannelName) &&
                   TimeValues.Length > 0 &&
                   VoltageValues.Length > 0 &&
                   TimeValues.Length == VoltageValues.Length &&
                   RawData.Length > 0 &&
                   SampleRate > 0 &&
                   TimeIncrement > 0;
        }

        /// <summary>
        /// Creates a copy of this waveform data
        /// </summary>
        public WaveformData Clone()
        {
            return new WaveformData
            {
                ChannelName = ChannelName,
                CaptureTime = CaptureTime,
                Id = Guid.NewGuid(), // New ID for the copy
                TimeValues = (double[])TimeValues.Clone(),
                VoltageValues = (double[])VoltageValues.Clone(),
                RawData = (byte[])RawData.Clone(),
                SampleRate = SampleRate,
                TimeIncrement = TimeIncrement,
                TimeOrigin = TimeOrigin,
                TimeReference = TimeReference,
                VoltageIncrement = VoltageIncrement,
                VoltageOrigin = VoltageOrigin,
                VoltageReference = VoltageReference,
                VerticalScale = VerticalScale,
                VerticalOffset = VerticalOffset,
                Coupling = Coupling,
                ProbeRatio = ProbeRatio,
                BandwidthLimit = BandwidthLimit,
                Units = Units,
                MemoryDepth = MemoryDepth,
                AcquisitionType = AcquisitionType,
                AverageCount = AverageCount,
                TriggerMode = TriggerMode,
                TriggerSource = TriggerSource,
                TriggerLevel = TriggerLevel,
                TriggerSlope = TriggerSlope,
                TriggerCoupling = TriggerCoupling,
                TimebaseScale = TimebaseScale,
                TimebaseOffset = TimebaseOffset,
                TimebaseMode = TimebaseMode,
                CaptureMode = CaptureMode,
                DataFormat = DataFormat
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this waveform for debugging
        /// </summary>
        public override string ToString()
        {
            return $"WaveformData: {ChannelName}, {CaptureTime:HH:mm:ss}, {PointCount} points";
        }

        #endregion
    }
}