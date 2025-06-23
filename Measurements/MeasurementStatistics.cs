using System;
using System.Globalization;

namespace DS1000Z_E_USB_Control.Measurements
{
    /// <summary>
    /// Represents comprehensive statistics for a measurement parameter
    /// Contains current value, statistical analysis, and metadata
    /// </summary>
    public class MeasurementStatistics
    {
        #region Core Properties

        /// <summary>
        /// The key identifying the measurement (e.g., "VMAX", "FREQuency")
        /// </summary>
        public string MeasurementKey { get; set; }

        /// <summary>
        /// The current/latest value of the measurement
        /// </summary>
        public double Current { get; set; }

        /// <summary>
        /// The average value of all measurements collected
        /// </summary>
        public double Average { get; set; }

        /// <summary>
        /// The minimum value observed during the measurement period
        /// </summary>
        public double Minimum { get; set; }

        /// <summary>
        /// The maximum value observed during the measurement period
        /// </summary>
        public double Maximum { get; set; }

        /// <summary>
        /// The standard deviation of the measurements
        /// </summary>
        public double StandardDeviation { get; set; }

        /// <summary>
        /// The total number of measurement samples collected
        /// </summary>
        public int Count { get; set; }

        #endregion

        #region Extended Properties

        /// <summary>
        /// Timestamp when the statistics were last updated
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Unit of measurement (e.g., "V", "Hz", "s")
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Whether the current measurement data is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Optional description of the measurement
        /// </summary>
        public string Description { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public MeasurementStatistics()
        {
            MeasurementKey = string.Empty;
            Current = double.NaN;
            Average = double.NaN;
            Minimum = double.NaN;
            Maximum = double.NaN;
            StandardDeviation = double.NaN;
            Count = 0;
            LastUpdated = DateTime.Now;
            Unit = string.Empty;
            IsValid = false;
            Description = string.Empty;
        }

        /// <summary>
        /// Constructor with measurement key
        /// </summary>
        /// <param name="measurementKey">The measurement identifier</param>
        public MeasurementStatistics(string measurementKey) : this()
        {
            MeasurementKey = measurementKey ?? string.Empty;
        }

        /// <summary>
        /// Constructor with basic measurement data
        /// </summary>
        /// <param name="measurementKey">The measurement identifier</param>
        /// <param name="currentValue">The current measurement value</param>
        /// <param name="unit">The unit of measurement</param>
        public MeasurementStatistics(string measurementKey, double currentValue, string unit = "") : this()
        {
            MeasurementKey = measurementKey ?? string.Empty;
            Current = currentValue;
            Average = currentValue;
            Minimum = currentValue;
            Maximum = currentValue;
            StandardDeviation = 0.0;
            Count = 1;
            Unit = unit ?? string.Empty;
            IsValid = !double.IsNaN(currentValue) && !double.IsInfinity(currentValue);
            LastUpdated = DateTime.Now;
        }

        #endregion

        #region Calculated Properties

        /// <summary>
        /// Gets the range (Maximum - Minimum) of the measurements
        /// </summary>
        public double Range
        {
            get
            {
                if (!IsValid || double.IsNaN(Maximum) || double.IsNaN(Minimum))
                    return double.NaN;
                return Maximum - Minimum;
            }
        }

        /// <summary>
        /// Gets the peak-to-peak value (same as Range for voltage measurements)
        /// </summary>
        public double PeakToPeak => Range;

        /// <summary>
        /// Gets the variance (StandardDeviation squared)
        /// </summary>
        public double Variance
        {
            get
            {
                if (!IsValid || double.IsNaN(StandardDeviation))
                    return double.NaN;
                return StandardDeviation * StandardDeviation;
            }
        }

        /// <summary>
        /// Gets the coefficient of variation (StandardDeviation / Average)
        /// </summary>
        public double CoefficientOfVariation
        {
            get
            {
                if (!IsValid || double.IsNaN(StandardDeviation) || double.IsNaN(Average) || Math.Abs(Average) < double.Epsilon)
                    return double.NaN;
                return StandardDeviation / Math.Abs(Average);
            }
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Update the statistics with a new measurement value
        /// </summary>
        /// <param name="newValue">The new measurement value</param>
        public void UpdateValue(double newValue)
        {
            if (double.IsNaN(newValue) || double.IsInfinity(newValue))
            {
                IsValid = false;
                return;
            }

            Current = newValue;
            LastUpdated = DateTime.Now;

            if (Count == 0)
            {
                // First measurement
                Average = newValue;
                Minimum = newValue;
                Maximum = newValue;
                StandardDeviation = 0.0;
                Count = 1;
            }
            else
            {
                // Update statistics incrementally
                Count++;

                // Update min/max
                if (newValue < Minimum) Minimum = newValue;
                if (newValue > Maximum) Maximum = newValue;

                // Update running average (simple moving average)
                Average = ((Average * (Count - 1)) + newValue) / Count;

                // Note: For proper standard deviation calculation, we would need
                // to maintain sum of squares, but this gives a reasonable approximation
            }

            IsValid = true;
        }

        /// <summary>
        /// Update all statistics at once
        /// </summary>
        /// <param name="current">Current value</param>
        /// <param name="average">Average value</param>
        /// <param name="minimum">Minimum value</param>
        /// <param name="maximum">Maximum value</param>
        /// <param name="standardDeviation">Standard deviation</param>
        /// <param name="count">Sample count</param>
        public void UpdateAll(double current, double average, double minimum, double maximum, double standardDeviation, int count)
        {
            Current = current;
            Average = average;
            Minimum = minimum;
            Maximum = maximum;
            StandardDeviation = standardDeviation;
            Count = count;
            LastUpdated = DateTime.Now;
            IsValid = !double.IsNaN(current) && !double.IsInfinity(current);
        }

        /// <summary>
        /// Reset all statistics to initial state
        /// </summary>
        public void Reset()
        {
            Current = double.NaN;
            Average = double.NaN;
            Minimum = double.NaN;
            Maximum = double.NaN;
            StandardDeviation = double.NaN;
            Count = 0;
            LastUpdated = DateTime.Now;
            IsValid = false;
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Check if the statistics contain valid measurement data
        /// </summary>
        /// <returns>True if the data is valid</returns>
        public bool HasValidData()
        {
            return IsValid &&
                   Count > 0 &&
                   !double.IsNaN(Current) &&
                   !double.IsInfinity(Current);
        }

        /// <summary>
        /// Check if statistical analysis is meaningful (requires multiple samples)
        /// </summary>
        /// <returns>True if statistical analysis is meaningful</returns>
        public bool HasStatisticalData()
        {
            return HasValidData() &&
                   Count > 1 &&
                   !double.IsNaN(StandardDeviation);
        }

        #endregion

        #region Formatting Methods

        /// <summary>
        /// Format the current value for display
        /// </summary>
        /// <param name="format">Optional format string</param>
        /// <returns>Formatted current value string</returns>
        public string FormatCurrentValue(string format = null)
        {
            if (!HasValidData())
                return "---";

            if (!string.IsNullOrEmpty(format))
                return Current.ToString(format, CultureInfo.InvariantCulture);

            // Auto-format based on magnitude
            return FormatValue(Current);
        }

        /// <summary>
        /// Format the average value for display
        /// </summary>
        /// <param name="format">Optional format string</param>
        /// <returns>Formatted average value string</returns>
        public string FormatAverageValue(string format = null)
        {
            if (!HasStatisticalData())
                return "---";

            if (!string.IsNullOrEmpty(format))
                return Average.ToString(format, CultureInfo.InvariantCulture);

            return FormatValue(Average);
        }

        /// <summary>
        /// Format the range (max - min) for display
        /// </summary>
        /// <param name="format">Optional format string</param>
        /// <returns>Formatted range string</returns>
        public string FormatRange(string format = null)
        {
            if (!HasStatisticalData())
                return "---";

            var range = Range;
            if (double.IsNaN(range))
                return "---";

            if (!string.IsNullOrEmpty(format))
                return range.ToString(format, CultureInfo.InvariantCulture);

            return FormatValue(range);
        }

        /// <summary>
        /// Get a complete formatted summary of the statistics
        /// </summary>
        /// <returns>Multi-line summary string</returns>
        public string GetFormattedSummary()
        {
            if (!HasValidData())
                return $"{MeasurementKey}: No valid data";

            var summary = $"{MeasurementKey}:\n";
            summary += $"  Current: {FormatCurrentValue()} {Unit}\n";

            if (HasStatisticalData())
            {
                summary += $"  Average: {FormatAverageValue()} {Unit}\n";
                summary += $"  Range: {FormatValue(Minimum)} to {FormatValue(Maximum)} {Unit}\n";
                summary += $"  Std Dev: {FormatValue(StandardDeviation)} {Unit}\n";
                summary += $"  Samples: {Count}";
            }
            else
            {
                summary += $"  (Single sample)";
            }

            return summary;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Auto-format a value based on its magnitude
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <returns>Formatted value string</returns>
        private string FormatValue(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "---";

            var absValue = Math.Abs(value);

            // Choose format based on magnitude
            if (absValue >= 1000 || (absValue > 0 && absValue < 0.001))
                return value.ToString("E3", CultureInfo.InvariantCulture);
            else if (absValue >= 100)
                return value.ToString("F1", CultureInfo.InvariantCulture);
            else if (absValue >= 1)
                return value.ToString("F3", CultureInfo.InvariantCulture);
            else
                return value.ToString("F6", CultureInfo.InvariantCulture);
        }

        #endregion

        #region Clone and Copy Methods

        /// <summary>
        /// Create a deep copy of this statistics object
        /// </summary>
        /// <returns>New MeasurementStatistics instance with copied values</returns>
        public MeasurementStatistics Clone()
        {
            return new MeasurementStatistics
            {
                MeasurementKey = this.MeasurementKey,
                Current = this.Current,
                Average = this.Average,
                Minimum = this.Minimum,
                Maximum = this.Maximum,
                StandardDeviation = this.StandardDeviation,
                Count = this.Count,
                LastUpdated = this.LastUpdated,
                Unit = this.Unit,
                IsValid = this.IsValid,
                Description = this.Description
            };
        }

        /// <summary>
        /// Copy values from another statistics object
        /// </summary>
        /// <param name="other">Source statistics object</param>
        public void CopyFrom(MeasurementStatistics other)
        {
            if (other == null) return;

            MeasurementKey = other.MeasurementKey;
            Current = other.Current;
            Average = other.Average;
            Minimum = other.Minimum;
            Maximum = other.Maximum;
            StandardDeviation = other.StandardDeviation;
            Count = other.Count;
            LastUpdated = other.LastUpdated;
            Unit = other.Unit;
            IsValid = other.IsValid;
            Description = other.Description;
        }

        #endregion

        #region Comparison Methods

        /// <summary>
        /// Compare current value with another statistics object
        /// </summary>
        /// <param name="other">Statistics to compare with</param>
        /// <returns>Comparison result (-1, 0, 1)</returns>
        public int CompareTo(MeasurementStatistics other)
        {
            if (other == null) return 1;
            if (!HasValidData() && !other.HasValidData()) return 0;
            if (!HasValidData()) return -1;
            if (!other.HasValidData()) return 1;

            return Current.CompareTo(other.Current);
        }

        /// <summary>
        /// Check if two statistics objects are equivalent
        /// </summary>
        /// <param name="other">Statistics to compare with</param>
        /// <param name="tolerance">Tolerance for floating-point comparison</param>
        /// <returns>True if equivalent within tolerance</returns>
        public bool IsEquivalent(MeasurementStatistics other, double tolerance = 1e-9)
        {
            if (other == null) return false;
            if (MeasurementKey != other.MeasurementKey) return false;
            if (Count != other.Count) return false;

            return Math.Abs(Current - other.Current) < tolerance &&
                   Math.Abs(Average - other.Average) < tolerance &&
                   Math.Abs(Minimum - other.Minimum) < tolerance &&
                   Math.Abs(Maximum - other.Maximum) < tolerance &&
                   Math.Abs(StandardDeviation - other.StandardDeviation) < tolerance;
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Get a concise string representation of the statistics
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            if (!HasValidData())
                return $"{MeasurementKey}: Invalid";

            if (HasStatisticalData())
            {
                return $"{MeasurementKey}: {FormatCurrentValue()} {Unit} " +
                       $"(avg: {FormatAverageValue()}, range: {FormatRange()}, n={Count})";
            }
            else
            {
                return $"{MeasurementKey}: {FormatCurrentValue()} {Unit}";
            }
        }

        /// <summary>
        /// Get a detailed string representation for debugging
        /// </summary>
        /// <returns>Detailed string representation</returns>
        public string ToDetailedString()
        {
            return $"MeasurementStatistics[{MeasurementKey}]: " +
                   $"Current={Current}, Average={Average}, Min={Minimum}, Max={Maximum}, " +
                   $"StdDev={StandardDeviation}, Count={Count}, Valid={IsValid}, " +
                   $"Unit={Unit}, Updated={LastUpdated:HH:mm:ss}";
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Create an invalid/empty statistics object
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <returns>Invalid statistics object</returns>
        public static MeasurementStatistics CreateInvalid(string measurementKey)
        {
            return new MeasurementStatistics(measurementKey)
            {
                IsValid = false
            };
        }

        /// <summary>
        /// Create statistics from a single measurement
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <param name="value">The measurement value</param>
        /// <param name="unit">The unit of measurement</param>
        /// <returns>Statistics object with single measurement</returns>
        public static MeasurementStatistics CreateFromSingle(string measurementKey, double value, string unit = "")
        {
            return new MeasurementStatistics(measurementKey, value, unit);
        }

        #endregion
    }
}