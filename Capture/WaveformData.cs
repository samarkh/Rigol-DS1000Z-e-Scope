using System;
using System.Collections.Generic;

namespace OscilloscopeControl.Capture
{
    /// <summary>
    /// Enumeration of available waveform channels
    /// </summary>
    public enum WaveformChannel
    {
        Channel1,
        Channel2,
        Both,
        AllChannels = Both  // Alias for compatibility
    }

    /// <summary>
    /// Enumeration of waveform capture modes
    /// </summary>
    public enum WaveformMode
    {
        /// <summary>Normal mode - processed data</summary>
        NORMal,
        /// <summary>Raw mode - unprocessed data</summary>
        RAW,
        /// <summary>Maximum mode - maximum points</summary>
        MAXimum
    }

    /// <summary>
    /// Enumeration of waveform data formats
    /// </summary>
    public enum WaveformFormat
    {
        /// <summary>8-bit byte format</summary>
        BYTE,
        /// <summary>16-bit word format</summary>
        WORD,
        /// <summary>ASCII text format</summary>
        ASCii
    }

    /// <summary>
    /// Class representing captured waveform data
    /// </summary>
    public class WaveformData
    {
        #region Properties

        /// <summary>The channel this waveform was captured from</summary>
        public int ChannelNumber { get; set; }

        /// <summary>The channel name (e.g., "Channel 1", "CH1")</summary>
        public string ChannelName { get; set; } = "CH1";

        /// <summary>Timestamp when the waveform was captured</summary>
        public DateTime CaptureTime { get; set; } = DateTime.Now;

        /// <summary>Raw voltage data points</summary>
        public List<double> VoltageData { get; set; } = new List<double>();

        /// <summary>Time data points</summary>
        public List<double> TimeData { get; set; } = new List<double>();

        /// <summary>Sample rate used for capture</summary>
        public double SampleRate { get; set; }

        /// <summary>Vertical scale setting</summary>
        public double VerticalScale { get; set; }

        /// <summary>Vertical offset setting</summary>
        public double VerticalOffset { get; set; }

        /// <summary>Horizontal scale setting</summary>
        public double HorizontalScale { get; set; }

        /// <summary>Horizontal offset setting</summary>
        public double HorizontalOffset { get; set; }

        /// <summary>Channel coupling setting</summary>
        public string Coupling { get; set; } = "DC";

        /// <summary>Probe ratio setting</summary>
        public double ProbeRatio { get; set; } = 1.0;

        /// <summary>Channel units</summary>
        public string Units { get; set; } = "V";

        /// <summary>Memory depth used for capture</summary>
        public string MemoryDepth { get; set; } = "AUTO";

        /// <summary>Acquisition type</summary>
        public string AcquisitionType { get; set; } = "NORM";

        /// <summary>Average count (if in average mode)</summary>
        public int AverageCount { get; set; } = 0;

        /// <summary>Waveform mode used for capture</summary>
        public WaveformMode Mode { get; set; } = WaveformMode.NORMal;

        /// <summary>Data format used for capture</summary>
        public WaveformFormat Format { get; set; } = WaveformFormat.BYTE;

        /// <summary>Optional description or label</summary>
        public string Description { get; set; } = "";

        #endregion

        #region Methods

        /// <summary>
        /// Get the number of data points in this waveform
        /// </summary>
        public int PointCount => VoltageData?.Count ?? 0;

        /// <summary>
        /// Get the estimated memory size of this waveform in bytes
        /// </summary>
        public long EstimatedMemorySize => (long)(PointCount * 16); // Rough estimate: 8 bytes per double * 2 arrays

        /// <summary>
        /// Create a descriptive string for this waveform
        /// </summary>
        public override string ToString()
        {
            // C# 7.3 compatible string formatting (no raw string literals)
            return string.Format("{0} - {1:N0} points - {2:HH:mm:ss} - {3}",
                ChannelName, PointCount, CaptureTime, Description);
        }

        #endregion
    }

    /// <summary>
    /// Event arguments for memory status changes
    /// </summary>
    public class MemoryStatusEventArgs : EventArgs
    {
        /// <summary>Number of stored waveforms</summary>
        public int WaveformCount { get; set; }

        /// <summary>Total memory usage in bytes</summary>
        public long TotalMemoryUsage { get; set; }

        /// <summary>Maximum allowed waveforms</summary>
        public int MaxWaveforms { get; set; }

        /// <summary>Memory usage as a percentage (0-100)</summary>
        public double MemoryUsagePercent { get; set; }

        /// <summary>Most recent waveform information</summary>
        public string LastWaveformInfo { get; set; } = "";

        // Additional properties for compatibility
        public int TotalWaveforms { get; set; }
        public int Channel1Count { get; set; }
        public int Channel2Count { get; set; }
        public long TotalDataPoints => Channel1Count + Channel2Count;
    }
}