using System;
using System.Collections.Generic;

namespace DS1000Z_E_USB_Control.TimeBase
{
    /// <summary>
    /// Model class for TimeBase settings and configuration
    /// </summary>
    public class TimeBaseSettings
    {
        #region Properties

        /// <summary>
        /// Timebase mode (MAIN, XY, ROLL)
        /// </summary>
        public string Mode { get; set; } = "MAIN";

        /// <summary>
        /// Main horizontal scale in seconds per division
        /// </summary>
        public double MainScale { get; set; } = 1e-3; // 1ms/div default

        /// <summary>
        /// Main horizontal offset in seconds
        /// </summary>
        public double MainOffset { get; set; } = 0.0;

        /// <summary>
        /// Whether delayed timebase is enabled
        /// </summary>
        public bool DelayEnabled { get; set; } = false;

        /// <summary>
        /// Delayed timebase scale in seconds per division
        /// </summary>
        public double DelayScale { get; set; } = 1e-6; // 1µs/div default

        /// <summary>
        /// Delayed timebase offset in seconds
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
                    return $"{MainScale * 1000000:F1} µs/div";
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
                    return $"{DelayScale * 1000000:F1} µs/div";
                else
                    return $"{DelayScale * 1000000000:F1} ns/div";
            }
        }

        /// <summary>
        /// Total time window in seconds (12 divisions)
        /// </summary>
        public double TimeWindow => MainScale * 12.0;

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
                ("XY", "XY"),
                ("ROLL", "Roll")
            };
        }

        /// <summary>
        /// Get available horizontal scale options
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
                (1e-6, "1 µs/div"),
                (2e-6, "2 µs/div"),
                (5e-6, "5 µs/div"),
                (10e-6, "10 µs/div"),
                (20e-6, "20 µs/div"),
                (50e-6, "50 µs/div"),
                (100e-6, "100 µs/div"),
                (200e-6, "200 µs/div"),
                (500e-6, "500 µs/div"),
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
        /// Get offset range for current scale
        /// </summary>
        public (double min, double max) GetOffsetRange()
        {
            // Offset range is typically ±50 time divisions
            double maxOffset = MainScale * 50.0;
            return (-maxOffset, maxOffset);
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
            return $"TimeBase: Mode={Mode}, Scale={MainScaleDisplay}, Offset={MainOffset:E3}s, " +
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
                DelayEnabled = false
            };

            /// <summary>
            /// Settings for high frequency measurements
            /// </summary>
            public static TimeBaseSettings HighFrequency => new TimeBaseSettings
            {
                Mode = "MAIN",
                MainScale = 10e-9, // 10ns/div
                MainOffset = 0.0,
                DelayEnabled = false
            };

            /// <summary>
            /// Settings for low frequency measurements
            /// </summary>
            public static TimeBaseSettings LowFrequency => new TimeBaseSettings
            {
                Mode = "MAIN",
                MainScale = 10e-3, // 10ms/div
                MainOffset = 0.0,
                DelayEnabled = false
            };

            /// <summary>
            /// Settings for power measurements
            /// </summary>
            public static TimeBaseSettings PowerMeasurement => new TimeBaseSettings
            {
                Mode = "MAIN",
                MainScale = 20e-3, // 20ms/div for 50/60Hz line frequency
                MainOffset = 0.0,
                DelayEnabled = false
            };

            /// <summary>
            /// Settings for digital measurements
            /// </summary>
            public static TimeBaseSettings Digital => new TimeBaseSettings
            {
                Mode = "MAIN",
                MainScale = 100e-9, // 100ns/div
                MainOffset = 0.0,
                DelayEnabled = false
            };
        }

        #endregion
    }
}