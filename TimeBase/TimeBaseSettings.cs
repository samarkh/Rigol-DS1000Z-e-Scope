using System;
using System.Collections.Generic;

namespace DS1000Z_E_USB_Control.TimeBase
{
    /// <summary>
    /// Model class for TimeBase settings and configuration
    /// Implements SCPI commands for Rigol DS1000Z-E timebase control
    /// </summary>
    public class TimeBaseSettings
    {
        #region Properties

        /// <summary>
        /// Timebase mode (MAIN, DELayed)
        /// </summary>
        public string Mode { get; set; } = "MAIN";

        /// <summary>
        /// Main horizontal scale in seconds per division (:TIMebase[:MAIN]:SCALe)
        /// </summary>
        public double MainScale { get; set; } = 1e-3; // 1ms/div default

        /// <summary>
        /// Main horizontal offset in seconds (:TIMebase[:MAIN]:OFFSet)
        /// </summary>
        public double MainOffset { get; set; } = 0.0;

        /// <summary>
        /// Whether delayed timebase is enabled (:TIMebase:DELay:ENABle)
        /// </summary>
        public bool DelayEnabled { get; set; } = false;

        /// <summary>
        /// Delayed timebase scale in seconds per division (:TIMebase:DELay:SCALe)
        /// </summary>
        public double DelayScale { get; set; } = 1e-6; // 1µs/div default

        /// <summary>
        /// Delayed timebase offset in seconds (:TIMebase:DELay:OFFSet)
        /// </summary>
        public double DelayOffset { get; set; } = 0.0;

        #endregion

        #region Calculated Properties

        /// <summary>
        /// Get display string for main scale
        /// </summary>
        public string MainScaleDisplay
        {
            get
            {
                if (MainScale >= 1.0)
                    return $"{MainScale:F3} s/div";
                else if (MainScale >= 1e-3)
                    return $"{MainScale * 1000:F1} ms/div";
                else if (MainScale >= 1e-6)
                    return $"{MainScale * 1000000:F1} μs/div";
                else
                    return $"{MainScale * 1000000000:F1} ns/div";
            }
        }

        /// <summary>
        /// Get display string for delay scale
        /// </summary>
        public string DelayScaleDisplay
        {
            get
            {
                if (DelayScale >= 1.0)
                    return $"{DelayScale:F3} s/div";
                else if (DelayScale >= 1e-3)
                    return $"{DelayScale * 1000:F1} ms/div";
                else if (DelayScale >= 1e-6)
                    return $"{DelayScale * 1000000:F1} μs/div";
                else
                    return $"{DelayScale * 1000000000:F1} ns/div";
            }
        }

        /// <summary>
        /// Total time window in seconds (12 divisions)
        /// </summary>
        public double TimeWindow => MainScale * 12.0;

        /// <summary>
        /// Get display string for main offset
        /// </summary>
        public string MainOffsetDisplay
        {
            get
            {
                if (MainOffset == 0) return "0 s";

                var absOffset = Math.Abs(MainOffset);
                if (absOffset >= 1.0)
                    return $"{MainOffset:F3} s";
                else if (absOffset >= 1e-3)
                    return $"{MainOffset * 1000:F3} ms";
                else if (absOffset >= 1e-6)
                    return $"{MainOffset * 1000000:F3} μs";
                else
                    return $"{MainOffset * 1000000000:F3} ns";
            }
        }

        /// <summary>
        /// Get display string for delay offset
        /// </summary>
        public string DelayOffsetDisplay
        {
            get
            {
                if (DelayOffset == 0) return "0 s";

                var absOffset = Math.Abs(DelayOffset);
                if (absOffset >= 1.0)
                    return $"{DelayOffset:F3} s";
                else if (absOffset >= 1e-3)
                    return $"{DelayOffset * 1000:F3} ms";
                else if (absOffset >= 1e-6)
                    return $"{DelayOffset * 1000000:F3} μs";
                else
                    return $"{DelayOffset * 1000000000:F3} ns";
            }
        }

        #endregion

        #region Static Configuration Data

        /// <summary>
        /// Get available timebase mode options
        /// </summary>
        public static List<(string value, string display)> GetModeOptions()
        {
            return new List<(string, string)>
            {
                ("MAIN", "Main"),
                ("DELayed", "Delayed")
            };
        }

        /// <summary>
        /// Get available horizontal scale options based on DS1000Z-E specifications
        /// </summary>
        public static List<(double value, string display)> GetHorizontalScaleOptions()
        {
            return new List<(double, string)>
            {
                // Nanoseconds
                (1e-9, "1 ns/div"),
                (2e-9, "2 ns/div"),
                (5e-9, "5 ns/div"),
                (10e-9, "10 ns/div"),
                (20e-9, "20 ns/div"),
                (50e-9, "50 ns/div"),
                (100e-9, "100 ns/div"),
                (200e-9, "200 ns/div"),
                (500e-9, "500 ns/div"),
                // Microseconds
                (1e-6, "1 μs/div"),
                (2e-6, "2 μs/div"),
                (5e-6, "5 μs/div"),
                (10e-6, "10 μs/div"),
                (20e-6, "20 μs/div"),
                (50e-6, "50 μs/div"),
                (100e-6, "100 μs/div"),
                (200e-6, "200 μs/div"),
                (500e-6, "500 μs/div"),
                // Milliseconds
                (1e-3, "1 ms/div"),
                (2e-3, "2 ms/div"),
                (5e-3, "5 ms/div"),
                (10e-3, "10 ms/div"),
                (20e-3, "20 ms/div"),
                (50e-3, "50 ms/div"),
                (100e-3, "100 ms/div"),
                (200e-3, "200 ms/div"),
                (500e-3, "500 ms/div"),
                // Seconds
                (1.0, "1 s/div"),
                (2.0, "2 s/div"),
                (5.0, "5 s/div"),
                (10.0, "10 s/div"),
                (20.0, "20 s/div"),
                (50.0, "50 s/div"),
                (100.0, "100 s/div")
            };
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate if the horizontal scale is supported
        /// </summary>
        public static bool IsValidHorizontalScale(double scale)
        {
            var validScales = GetHorizontalScaleOptions();
            foreach (var (value, _) in validScales)
            {
                if (Math.Abs(value - scale) < value * 0.01) // 1% tolerance
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get offset range for current main scale (±6 times scale according to DS1000Z-E manual)
        /// </summary>
        public (double min, double max) GetOffsetRange()
        {
            // According to DS1000Z-E manual, offset range is typically ±6 times the scale
            double maxOffset = MainScale * 6.0;
            return (-maxOffset, maxOffset);
        }

        /// <summary>
        /// Validate offset against current scale constraints
        /// </summary>
        public bool IsValidOffset(double offset)
        {
            var (min, max) = GetOffsetRange();
            return offset >= min && offset <= max;
        }

        /// <summary>
        /// Clamp offset to valid range
        /// </summary>
        public double ClampOffset(double offset)
        {
            var (min, max) = GetOffsetRange();
            return Math.Max(min, Math.Min(max, offset));
        }

        #endregion

        #region Object Methods

        /// <summary>
        /// Create a deep copy of the settings
        /// </summary>
        public TimeBaseSettings Clone()
        {
            return new TimeBaseSettings
            {
                Mode = this.Mode,
                MainScale = this.MainScale,
                MainOffset = this.MainOffset,
                DelayEnabled = this.DelayEnabled,
                DelayScale = this.DelayScale,
                DelayOffset = this.DelayOffset
            };
        }

        /// <summary>
        /// Check if settings are equal
        /// </summary>
        public bool Equals(TimeBaseSettings other)
        {
            if (other == null) return false;

            return string.Equals(Mode, other.Mode, StringComparison.OrdinalIgnoreCase) &&
                   Math.Abs(MainScale - other.MainScale) < MainScale * 0.01 &&
                   Math.Abs(MainOffset - other.MainOffset) < 1e-9 &&
                   DelayEnabled == other.DelayEnabled &&
                   Math.Abs(DelayScale - other.DelayScale) < DelayScale * 0.01 &&
                   Math.Abs(DelayOffset - other.DelayOffset) < 1e-9;
        }

        /// <summary>
        /// Get a string representation of the settings
        /// </summary>
        public override string ToString()
        {
            return $"TimeBase: Mode={Mode}, Scale={MainScaleDisplay}, Offset={MainOffsetDisplay}, " +
                   $"Window={TimeWindow:E3}s, DelayEnabled={DelayEnabled}";
        }

        /// <summary>
        /// Create default settings for common scenarios
        /// </summary>
        public static class Presets
        {
            /// <summary>
            /// Settings for general purpose measurements
            /// </summary>
            public static TimeBaseSettings GeneralPurpose => new TimeBaseSettings
            {
                Mode = "MAIN",
                MainScale = 1e-3,  // 1ms/div
                MainOffset = 0.0,
                DelayEnabled = false,
                DelayScale = 1e-6,  // 1μs/div for when enabled
                DelayOffset = 0.0
            };

            /// <summary>
            /// Settings for high frequency measurements
            /// </summary>
            public static TimeBaseSettings HighFrequency => new TimeBaseSettings
            {
                Mode = "MAIN",
                MainScale = 10e-9, // 10ns/div
                MainOffset = 0.0,
                DelayEnabled = false,
                DelayScale = 1e-9,  // 1ns/div
                DelayOffset = 0.0
            };

            /// <summary>
            /// Settings for low frequency measurements
            /// </summary>
            public static TimeBaseSettings LowFrequency => new TimeBaseSettings
            {
                Mode = "MAIN",
                MainScale = 10e-3, // 10ms/div
                MainOffset = 0.0,
                DelayEnabled = false,
                DelayScale = 1e-3,  // 1ms/div
                DelayOffset = 0.0
            };

            /// <summary>
            /// Settings for power measurements (50/60Hz line frequency)
            /// </summary>
            public static TimeBaseSettings PowerMeasurement => new TimeBaseSettings
            {
                Mode = "MAIN",
                MainScale = 20e-3, // 20ms/div for 50/60Hz line frequency
                MainOffset = 0.0,
                DelayEnabled = false,
                DelayScale = 5e-3,  // 5ms/div
                DelayOffset = 0.0
            };

            /// <summary>
            /// Settings for digital measurements
            /// </summary>
            public static TimeBaseSettings Digital => new TimeBaseSettings
            {
                Mode = "MAIN",
                MainScale = 100e-9, // 100ns/div
                MainOffset = 0.0,
                DelayEnabled = false,
                DelayScale = 10e-9,  // 10ns/div
                DelayOffset = 0.0
            };

            /// <summary>
            /// Settings with delayed timebase enabled for detailed analysis
            /// </summary>
            public static TimeBaseSettings DelayedAnalysis => new TimeBaseSettings
            {
                Mode = "DELayed",
                MainScale = 1e-3,   // 1ms/div main
                MainOffset = 0.0,
                DelayEnabled = true,
                DelayScale = 100e-6, // 100μs/div delayed
                DelayOffset = 0.0
            };

            /// <summary>
            /// Settings for single shot capture
            /// </summary>
            public static TimeBaseSettings SingleShot => new TimeBaseSettings
            {
                Mode = "MAIN",
                MainScale = 500e-6, // 500μs/div
                MainOffset = -2e-3,  // Slight negative offset to capture pre-trigger
                DelayEnabled = false,
                DelayScale = 50e-6,  // 50μs/div
                DelayOffset = 0.0
            };

            /// <summary>
            /// Settings for serial communication analysis
            /// </summary>
            public static TimeBaseSettings SerialComm => new TimeBaseSettings
            {
                Mode = "MAIN",
                MainScale = 100e-6, // 100μs/div for typical baud rates
                MainOffset = 0.0,
                DelayEnabled = true,
                DelayScale = 10e-6,  // 10μs/div for bit-level analysis
                DelayOffset = 0.0
            };

            /// <summary>
            /// Settings for audio analysis
            /// </summary>
            public static TimeBaseSettings Audio => new TimeBaseSettings
            {
                Mode = "MAIN",
                MainScale = 100e-6, // 100μs/div for audio frequencies
                MainOffset = 0.0,
                DelayEnabled = false,
                DelayScale = 10e-6,  // 10μs/div
                DelayOffset = 0.0
            };

            /// <summary>
            /// Settings for EMI/EMC testing
            /// </summary>
            public static TimeBaseSettings EMI => new TimeBaseSettings
            {
                Mode = "MAIN",
                MainScale = 10e-9,  // 10ns/div for high frequency EMI
                MainOffset = 0.0,
                DelayEnabled = false,
                DelayScale = 1e-9,   // 1ns/div
                DelayOffset = 0.0
            };
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Calculate the current sample rate estimation based on scale
        /// Note: Actual sample rate depends on memory depth and other factors
        /// </summary>
        public double EstimatedSampleRate
        {
            get
            {
                // This is a simplified estimation
                // Actual DS1000Z-E sample rate calculation is more complex
                double timeWindow = TimeWindow;

                // Assume maximum sample rate of 2 GSa/s for DS1000Z-E
                double maxSampleRate = 2e9;

                // For very fast timebases, use maximum sample rate
                if (timeWindow < 12e-6) // Less than 12μs total window
                {
                    return maxSampleRate;
                }

                // For slower timebases, sample rate decreases
                return Math.Min(maxSampleRate, 24e6 / timeWindow); // Approximate formula
            }
        }

        /// <summary>
        /// Get the nearest valid scale value
        /// </summary>
        public static double GetNearestValidScale(double requestedScale)
        {
            var validScales = GetHorizontalScaleOptions();

            double nearestScale = validScales[0].value;
            double minDifference = Math.Abs(requestedScale - nearestScale);

            foreach (var (value, _) in validScales)
            {
                double difference = Math.Abs(requestedScale - value);
                if (difference < minDifference)
                {
                    minDifference = difference;
                    nearestScale = value;
                }
            }

            return nearestScale;
        }

        /// <summary>
        /// Calculate acquisition points for current settings
        /// </summary>
        public long CalculateAcquisitionPoints(long memoryDepth = 12000000)
        {
            // This is simplified - actual calculation depends on oscilloscope mode
            return Math.Min(memoryDepth, (long)(TimeWindow * EstimatedSampleRate));
        }

        /// <summary>
        /// Check if current settings are suitable for measuring a specific frequency
        /// </summary>
        public bool IsSuitableForFrequency(double frequency)
        {
            if (frequency <= 0) return false;

            // Need at least 2-3 periods visible for proper measurement
            double period = 1.0 / frequency;
            double requiredWindow = period * 3;

            return TimeWindow >= requiredWindow;
        }

        /// <summary>
        /// Suggest optimal scale for measuring a specific frequency
        /// </summary>
        public static double SuggestScaleForFrequency(double frequency)
        {
            if (frequency <= 0) return 1e-3; // Default 1ms/div

            double period = 1.0 / frequency;
            double idealWindow = period * 10; // Show ~10 periods
            double idealScale = idealWindow / 12.0; // 12 divisions

            return GetNearestValidScale(idealScale);
        }

        #endregion
    }
}