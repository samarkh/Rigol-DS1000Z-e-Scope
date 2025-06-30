using System;
using System.Linq;

namespace DS1000Z_E_USB_Control.Measurements
{
    #region MeasurementValueEventArgs

    /// <summary>
    /// Event arguments for measurement value updates
    /// Provides information about a single measurement value change
    /// </summary>
    public class MeasurementValueEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// The measurement key/type (e.g., "VMAX", "FREQ", "PERiod")
        /// </summary>
        public string MeasurementKey { get; set; }

        /// <summary>
        /// The measurement value
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Optional unit of measurement (e.g., "V", "Hz", "s")
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Timestamp when the measurement was taken
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Optional source channel for the measurement
        /// </summary>
        public string SourceChannel { get; set; }

        /// <summary>
        /// Whether the measurement value is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Optional error message if the measurement failed
        /// </summary>
        public string ErrorMessage { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public MeasurementValueEventArgs()
        {
            MeasurementKey = string.Empty;
            Value = double.NaN;
            Unit = string.Empty;
            Timestamp = DateTime.Now;
            SourceChannel = string.Empty;
            IsValid = false;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Constructor with measurement key and value
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <param name="value">The measurement value</param>
        public MeasurementValueEventArgs(string measurementKey, double value)
        {
            MeasurementKey = measurementKey ?? string.Empty;
            Value = value;
            Unit = string.Empty;
            Timestamp = DateTime.Now;
            SourceChannel = string.Empty;
            IsValid = !double.IsNaN(value) && !double.IsInfinity(value);
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Constructor with measurement key, value, and unit
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <param name="value">The measurement value</param>
        /// <param name="unit">Optional unit</param>
        public MeasurementValueEventArgs(string measurementKey, double value, string unit)
        {
            MeasurementKey = measurementKey ?? string.Empty;
            Value = value;
            Unit = unit ?? string.Empty;
            Timestamp = DateTime.Now;
            SourceChannel = string.Empty;
            IsValid = !double.IsNaN(value) && !double.IsInfinity(value);
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Constructor with full parameter set
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <param name="value">The measurement value</param>
        /// <param name="unit">Unit of measurement</param>
        /// <param name="sourceChannel">Source channel</param>
        /// <param name="isValid">Whether the measurement is valid</param>
        /// <param name="errorMessage">Optional error message</param>
        public MeasurementValueEventArgs(string measurementKey, double value, string unit,
                                       string sourceChannel, bool isValid, string errorMessage = null)
        {
            MeasurementKey = measurementKey ?? string.Empty;
            Value = value;
            Unit = unit ?? string.Empty;
            Timestamp = DateTime.Now;
            SourceChannel = sourceChannel ?? string.Empty;
            IsValid = isValid;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Format the measurement value for display
        /// </summary>
        /// <returns>Formatted value string</returns>
        public string GetFormattedValue()
        {
            if (!IsValid || double.IsNaN(Value) || double.IsInfinity(Value))
                return "---";

            // Auto-format based on magnitude
            var absValue = Math.Abs(Value);

            if (absValue >= 1000 || (absValue > 0 && absValue < 0.001))
                return $"{Value:E3} {Unit}".Trim();
            else if (absValue >= 100)
                return $"{Value:F1} {Unit}".Trim();
            else if (absValue >= 1)
                return $"{Value:F3} {Unit}".Trim();
            else
                return $"{Value:F6} {Unit}".Trim();
        }

        /// <summary>
        /// Get a description of the measurement type
        /// </summary>
        /// <returns>Human-readable measurement description</returns>
        public string GetMeasurementDescription()
        {
            return MeasurementKey switch
            {
                "VMAX" => "Maximum Voltage",
                "VMIN" => "Minimum Voltage",
                "VPP" => "Peak-to-Peak Voltage",
                "VAVG" => "Average Voltage",
                "VRMS" => "RMS Voltage",
                "FREQuency" => "Frequency",
                "PERiod" => "Period",
                "RTIMe" => "Rise Time",
                "FTIMe" => "Fall Time",
                "PWIDth" => "Positive Width",
                "NWIDth" => "Negative Width",
                "PDUTy" => "Positive Duty Cycle",
                "NDUTy" => "Negative Duty Cycle",
                _ => MeasurementKey
            };
        }

        /// <summary>
        /// Check if this measurement represents a voltage measurement
        /// </summary>
        /// <returns>True if voltage measurement</returns>
        public bool IsVoltageMeasurement()
        {
            return MeasurementKey.ToUpper().StartsWith("V");
        }

        /// <summary>
        /// Check if this measurement represents a time measurement
        /// </summary>
        /// <returns>True if time measurement</returns>
        public bool IsTimeMeasurement()
        {
            return MeasurementKey.ToUpper().Contains("TIME") ||
                   MeasurementKey.ToUpper().Contains("PER") ||
                   MeasurementKey.ToUpper().Contains("WIDTH");

        }

        #endregion

        #region String Representation

        /// <summary>
        /// Get string representation of the measurement value event
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            if (IsValid)
            {
                return $"{GetMeasurementDescription()}: {GetFormattedValue()}";
            }
            else
            {
                return $"{GetMeasurementDescription()}: Invalid" +
                       (string.IsNullOrEmpty(ErrorMessage) ? "" : $" ({ErrorMessage})");
            }
        }

        /// <summary>
        /// Get detailed string representation for debugging
        /// </summary>
        /// <returns>Detailed string representation</returns>
        public string ToDetailedString()
        {
            return $"MeasurementValueEventArgs[{MeasurementKey}]: " +
                   $"Value={Value}, Unit={Unit}, Source={SourceChannel}, " +
                   $"Valid={IsValid}, Time={Timestamp:HH:mm:ss.fff}";
        }

        #endregion
    }

    #endregion

    #region MeasurementStatisticsEventArgs

    /// <summary>
    /// Event arguments for measurement statistics updates
    /// Provides information about statistical analysis of measurements
    /// </summary>
    public class MeasurementStatisticsEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// The measurement key/type (e.g., "VMAX", "FREQ", "PERiod")
        /// </summary>
        public string MeasurementKey { get; set; }

        /// <summary>
        /// The measurement statistics object containing all statistical data
        /// </summary>
        public MeasurementStatistics Statistics { get; set; }

        /// <summary>
        /// Timestamp when the statistics were updated
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The type of statistics update that occurred
        /// </summary>
        public StatisticsUpdateType UpdateType { get; set; }

        /// <summary>
        /// Optional source channel for the statistics
        /// </summary>
        public string SourceChannel { get; set; }

        /// <summary>
        /// Whether the statistics update was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Optional error message if the update failed
        /// </summary>
        public string ErrorMessage { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public MeasurementStatisticsEventArgs()
        {
            MeasurementKey = string.Empty;
            Statistics = null;
            Timestamp = DateTime.Now;
            UpdateType = StatisticsUpdateType.Unknown;
            SourceChannel = string.Empty;
            IsSuccessful = false;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Constructor with measurement key and statistics
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <param name="statistics">The measurement statistics</param>
        public MeasurementStatisticsEventArgs(string measurementKey, MeasurementStatistics statistics)
        {
            MeasurementKey = measurementKey ?? string.Empty;
            Statistics = statistics;
            Timestamp = DateTime.Now;
            UpdateType = StatisticsUpdateType.Updated;
            SourceChannel = string.Empty;
            IsSuccessful = statistics != null && statistics.HasValidData();
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Constructor with full parameter set
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <param name="statistics">The measurement statistics</param>
        /// <param name="updateType">Type of update</param>
        /// <param name="sourceChannel">Source channel</param>
        /// <param name="isSuccessful">Whether update was successful</param>
        /// <param name="errorMessage">Optional error message</param>
        public MeasurementStatisticsEventArgs(string measurementKey, MeasurementStatistics statistics,
                                            StatisticsUpdateType updateType, string sourceChannel = null,
                                            bool? isSuccessful = null, string errorMessage = null)
        {
            MeasurementKey = measurementKey ?? string.Empty;
            Statistics = statistics;
            Timestamp = DateTime.Now;
            UpdateType = updateType;
            SourceChannel = sourceChannel ?? string.Empty;
            IsSuccessful = isSuccessful ?? (statistics != null && statistics.HasValidData());
            ErrorMessage = errorMessage ?? string.Empty;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get a summary of the statistics
        /// </summary>
        /// <returns>Statistics summary string</returns>
        public string GetStatisticsSummary()
        {
            if (Statistics == null || !Statistics.HasValidData())
                return "No valid statistics available";

            if (Statistics.HasStatisticalData())
            {
                return $"Current: {Statistics.FormatCurrentValue()}, " +
                       $"Avg: {Statistics.FormatAverageValue()}, " +
                       $"Range: {Statistics.FormatRange()}, " +
                       $"Count: {Statistics.Count}";
            }
            else
            {
                return $"Current: {Statistics.FormatCurrentValue()} (single sample)";
            }
        }

        /// <summary>
        /// Check if the statistics show significant change
        /// </summary>
        /// <param name="threshold">Change threshold (default 5%)</param>
        /// <returns>True if significant change detected</returns>
        public bool HasSignificantChange(double threshold = 0.05)
        {
            if (Statistics == null || !Statistics.HasStatisticalData())
                return false;

            var range = Statistics.Range;
            var average = Statistics.Average;

            if (double.IsNaN(range) || double.IsNaN(average) || Math.Abs(average) < double.Epsilon)
                return false;

            return Math.Abs(range / average) > threshold;
        }

        /// <summary>
        /// Get the measurement description
        /// </summary>
        /// <returns>Human-readable measurement description</returns>
        public string GetMeasurementDescription()
        {
            return MeasurementKey switch
            {
                "VMAX" => "Maximum Voltage Statistics",
                "VMIN" => "Minimum Voltage Statistics",
                "VPP" => "Peak-to-Peak Voltage Statistics",
                "VAVG" => "Average Voltage Statistics",
                "VRMS" => "RMS Voltage Statistics",
                "FREQuency" => "Frequency Statistics",
                "PERiod" => "Period Statistics",
                "RTIMe" => "Rise Time Statistics",
                "FTIMe" => "Fall Time Statistics",
                "PWIDth" => "Positive Width Statistics",
                "NWIDth" => "Negative Width Statistics",
                "PDUTy" => "Positive Duty Cycle Statistics",
                "NDUTy" => "Negative Duty Cycle Statistics",
                _ => $"{MeasurementKey} Statistics"
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Get string representation of the statistics event
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            if (IsSuccessful)
            {
                return $"{GetMeasurementDescription()}: {GetStatisticsSummary()}";
            }
            else
            {
                return $"{GetMeasurementDescription()}: Update failed" +
                       (string.IsNullOrEmpty(ErrorMessage) ? "" : $" ({ErrorMessage})");
            }
        }

        /// <summary>
        /// Get detailed string representation for debugging
        /// </summary>
        /// <returns>Detailed string representation</returns>
        public string ToDetailedString()
        {
            return $"MeasurementStatisticsEventArgs[{MeasurementKey}]: " +
                   $"UpdateType={UpdateType}, Source={SourceChannel}, " +
                   $"Successful={IsSuccessful}, Time={Timestamp:HH:mm:ss.fff}, " +
                   $"Stats={Statistics?.ToDetailedString() ?? "null"}";
        }

        #endregion
    }

    #endregion

    #region Supporting Enumerations

    /// <summary>
    /// Types of statistics updates
    /// </summary>
    public enum StatisticsUpdateType
    {
        /// <summary>
        /// Unknown or unspecified update type
        /// </summary>
        Unknown,

        /// <summary>
        /// Statistics were updated with new data
        /// </summary>
        Updated,

        /// <summary>
        /// Statistics were reset/cleared
        /// </summary>
        Reset,

        /// <summary>
        /// Statistics calculation completed
        /// </summary>
        Completed,

        /// <summary>
        /// Statistics update failed
        /// </summary>
        Failed,

        /// <summary>
        /// Statistics were initialized for the first time
        /// </summary>
        Initialized,

        /// <summary>
        /// Statistics mode was changed
        /// </summary>
        ModeChanged
    }

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Static factory methods for creating event args
    /// </summary>
    public static class MeasurementEventArgsFactory
    {
        #region MeasurementValueEventArgs Factory Methods

        /// <summary>
        /// Create a successful measurement value event
        /// </summary>
        /// <param name="measurementKey">Measurement key</param>
        /// <param name="value">Measurement value</param>
        /// <param name="unit">Unit of measurement</param>
        /// <param name="sourceChannel">Source channel</param>
        /// <returns>MeasurementValueEventArgs instance</returns>
        public static MeasurementValueEventArgs CreateValueUpdated(string measurementKey, double value,
                                                                  string unit = null, string sourceChannel = null)
        {
            return new MeasurementValueEventArgs(measurementKey, value, unit, sourceChannel,
                                               !double.IsNaN(value) && !double.IsInfinity(value));
        }

        /// <summary>
        /// Create a failed measurement value event
        /// </summary>
        /// <param name="measurementKey">Measurement key</param>
        /// <param name="errorMessage">Error message</param>
        /// <param name="sourceChannel">Source channel</param>
        /// <returns>MeasurementValueEventArgs instance</returns>
        public static MeasurementValueEventArgs CreateValueFailed(string measurementKey, string errorMessage,
                                                                 string sourceChannel = null)
        {
            return new MeasurementValueEventArgs(measurementKey, double.NaN, null, sourceChannel, false, errorMessage);
        }

        #endregion

        #region MeasurementStatisticsEventArgs Factory Methods

        /// <summary>
        /// Create a successful statistics update event
        /// </summary>
        /// <param name="measurementKey">Measurement key</param>
        /// <param name="statistics">Statistics object</param>
        /// <param name="updateType">Type of update</param>
        /// <param name="sourceChannel">Source channel</param>
        /// <returns>MeasurementStatisticsEventArgs instance</returns>
        public static MeasurementStatisticsEventArgs CreateStatisticsUpdated(string measurementKey,
                                                                            MeasurementStatistics statistics,
                                                                            StatisticsUpdateType updateType = StatisticsUpdateType.Updated,
                                                                            string sourceChannel = null)
        {
            return new MeasurementStatisticsEventArgs(measurementKey, statistics, updateType, sourceChannel);
        }

        /// <summary>
        /// Create a failed statistics update event
        /// </summary>
        /// <param name="measurementKey">Measurement key</param>
        /// <param name="errorMessage">Error message</param>
        /// <param name="sourceChannel">Source channel</param>
        /// <returns>MeasurementStatisticsEventArgs instance</returns>
        public static MeasurementStatisticsEventArgs CreateStatisticsFailed(string measurementKey, string errorMessage,
                                                                           string sourceChannel = null)
        {
            return new MeasurementStatisticsEventArgs(measurementKey, null, StatisticsUpdateType.Failed,
                                                     sourceChannel, false, errorMessage);
        }

        /// <summary>
        /// Create a statistics reset event
        /// </summary>
        /// <param name="measurementKey">Measurement key</param>
        /// <param name="sourceChannel">Source channel</param>
        /// <returns>MeasurementStatisticsEventArgs instance</returns>
        public static MeasurementStatisticsEventArgs CreateStatisticsReset(string measurementKey,
                                                                          string sourceChannel = null)
        {
            return new MeasurementStatisticsEventArgs(measurementKey, null, StatisticsUpdateType.Reset,
                                                     sourceChannel, true);
        }

        #endregion
    }

    #endregion
}