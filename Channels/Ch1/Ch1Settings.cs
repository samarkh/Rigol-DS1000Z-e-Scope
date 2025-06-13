using System;
using System.Collections.Generic;

namespace DS1000Z_E_USB_Control.Channels.Ch1
{
    /// <summary>
    /// Model class for Channel 1 settings and configuration
    /// </summary>
    public class Ch1Settings
    {
        #region Properties

        /// <summary>
        /// Whether Channel 1 is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Probe ratio (attenuation factor)
        /// </summary>
        public double ProbeRatio { get; set; } = 10.0;

        /// <summary>
        /// Vertical scale in volts per division
        /// </summary>
        public double VerticalScale { get; set; } = 1.0;

        /// <summary>
        /// Vertical offset in volts
        /// </summary>
        public double VerticalOffset { get; set; } = 0.0;

        /// <summary>
        /// Display units (VOLTage, WATT, AMPere, UNKNown)
        /// </summary>
        public string Units { get; set; } = "VOLTage";

        /// <summary>
        /// Coupling mode (DC, AC, GND)
        /// </summary>
        public string Coupling { get; set; } = "DC";

        /// <summary>
        /// Bandwidth limit (OFF, 20M)
        /// </summary>
        public string BandwidthLimit { get; set; } = "OFF";

        /// <summary>
        /// Whether waveform invert is enabled
        /// </summary>
        public bool InvertEnabled { get; set; } = false;

        /// <summary>
        /// Whether fine adjustment (vernier) is enabled
        /// </summary>
        public bool VernierEnabled { get; set; } = false;

        #endregion

        #region Calculated Properties

        /// <summary>
        /// Calculate vertical range (8 divisions × scale)
        /// </summary>
        public double VerticalRange => VerticalScale * 8.0;

        /// <summary>
        /// Get display string for current scale
        /// </summary>
        public string VerticalScaleDisplay
        {
            get
            {
                if (VerticalScale < 1.0)
                {
                    return $"{VerticalScale * 1000:0} mV/div";
                }
                return $"{VerticalScale:0.###} V/div";
            }
        }

        /// <summary>
        /// Get display string for current probe ratio
        /// </summary>
        public string ProbeRatioDisplay => $"{ProbeRatio:0.##}×";

        #endregion

        #region Static Configuration Data

        /// <summary>
        /// Get available probe ratio options
        /// </summary>
        public static List<(double value, string display)> GetProbeRatioOptions()
        {
            return new List<(double, string)>
            {
                (0.01, "0.01×"),
                (0.02, "0.02×"),
                (0.05, "0.05×"),
                (0.1, "0.1×"),
                (0.2, "0.2×"),
                (0.5, "0.5×"),
                (1, "1×"),
                (2, "2×"),
                (5, "5×"),
                (10, "10×"),
                (20, "20×"),
                (50, "50×"),
                (100, "100×"),
                (200, "200×"),
                (500, "500×"),
                (1000, "1000×")
            };
        }

        /// <summary>
        /// Get available vertical scale options based on probe ratio
        /// </summary>
        public static List<(double value, string display)> GetScaleOptionsForProbeRatio(double probeRatio)
        {
            if (Math.Abs(probeRatio - 1.0) < 0.001)
            {
                // 1X probe: 1mV to 10V
                return new List<(double, string)>
                {
                    (0.001, "1 mV/div"),
                    (0.002, "2 mV/div"),
                    (0.005, "5 mV/div"),
                    (0.01, "10 mV/div"),
                    (0.02, "20 mV/div"),
                    (0.05, "50 mV/div"),
                    (0.1, "100 mV/div"),
                    (0.2, "200 mV/div"),
                    (0.5, "500 mV/div"),
                    (1.0, "1 V/div"),
                    (2.0, "2 V/div"),
                    (5.0, "5 V/div"),
                    (10.0, "10 V/div")
                };
            }
            else
            {
                // 10X probe (and others): 10mV to 100V
                return new List<(double, string)>
                {
                    (0.01, "10 mV/div"),
                    (0.02, "20 mV/div"),
                    (0.05, "50 mV/div"),
                    (0.1, "100 mV/div"),
                    (0.2, "200 mV/div"),
                    (0.5, "500 mV/div"),
                    (1.0, "1 V/div"),
                    (2.0, "2 V/div"),
                    (5.0, "5 V/div"),
                    (10.0, "10 V/div"),
                    (20.0, "20 V/div"),
                    (50.0, "50 V/div"),
                    (100.0, "100 V/div")
                };
            }
        }

        /// <summary>
        /// Get available display units options
        /// </summary>
        public static List<(string value, string display)> GetUnitsOptions()
        {
            return new List<(string, string)>
            {
                ("VOLTage", "Voltage"),
                ("WATT", "Watt"),
                ("AMPere", "Ampere"),
                ("UNKNown", "Unknown")
            };
        }

        /// <summary>
        /// Get available coupling options
        /// </summary>
        public static List<(string value, string display)> GetCouplingOptions()
        {
            return new List<(string, string)>
            {
                ("DC", "DC"),
                ("AC", "AC"),
                ("GND", "GND")
            };
        }

        /// <summary>
        /// Get available bandwidth limit options
        /// </summary>
        public static List<(string value, string display)> GetBandwidthLimitOptions()
        {
            return new List<(string, string)>
            {
                ("OFF", "Off"),
                ("20M", "20 MHz")
            };
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate if the probe ratio is supported
        /// </summary>
        public static bool IsValidProbeRatio(double ratio)
        {
            var validRatios = GetProbeRatioOptions();
            foreach (var (value, _) in validRatios)
            {
                if (Math.Abs(value - ratio) < 0.001)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Validate if the vertical scale is valid for the current probe ratio
        /// </summary>
        public bool IsValidVerticalScale(double scale)
        {
            var validScales = GetScaleOptionsForProbeRatio(ProbeRatio);
            foreach (var (value, _) in validScales)
            {
                if (Math.Abs(value - scale) < 0.0001)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Validate if the units string is supported
        /// </summary>
        public static bool IsValidUnits(string units)
        {
            var validUnits = GetUnitsOptions();
            foreach (var (value, _) in validUnits)
            {
                if (string.Equals(value, units, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Get channel vertical offset range based on vertical scale
        /// Using the same 4.0 factor as trigger level calculation
        /// </summary>
        public (double min, double max) GetVerticalOffsetRange()
        {
            // Use same factor as trigger level range: 4.0 × VerticalScale
            double range = 4.0 * VerticalScale;
            double min = -range;
            double max = +range;

            return (min, max);
        }
        /// <summary>
        /// Get the maximum offset range for current scale and probe ratio
        /// </summary>
        public (double min, double max) GetOffsetRange()
        {
            // Always make offset range exactly 5 times the vertical scale
            double maxOffset = VerticalScale * 5;

            // Optional: Set a reasonable upper limit to prevent extreme ranges
            maxOffset = Math.Min(maxOffset, 1000.0);

            return (-maxOffset, maxOffset);
        }

        #endregion

        #region Object Methods

        /// <summary>
        /// Create a deep copy of the settings
        /// </summary>
        public Ch1Settings Clone()
        {
            return new Ch1Settings
            {
                IsEnabled = this.IsEnabled,
                ProbeRatio = this.ProbeRatio,
                VerticalScale = this.VerticalScale,
                VerticalOffset = this.VerticalOffset,
                Units = this.Units,
                Coupling = this.Coupling,
                BandwidthLimit = this.BandwidthLimit,
                InvertEnabled = this.InvertEnabled,
                VernierEnabled = this.VernierEnabled
            };
        }

        /// <summary>
        /// Check if settings are equal
        /// </summary>
        public bool Equals(Ch1Settings other)
        {
            if (other == null) return false;

            return IsEnabled == other.IsEnabled &&
                   Math.Abs(ProbeRatio - other.ProbeRatio) < 0.001 &&
                   Math.Abs(VerticalScale - other.VerticalScale) < 0.0001 &&
                   Math.Abs(VerticalOffset - other.VerticalOffset) < 0.0001 &&
                   string.Equals(Units, other.Units, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Coupling, other.Coupling, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(BandwidthLimit, other.BandwidthLimit, StringComparison.OrdinalIgnoreCase) &&
                   InvertEnabled == other.InvertEnabled &&
                   VernierEnabled == other.VernierEnabled;
        }

        /// <summary>
        /// Get a string representation of the settings
        /// </summary>
        public override string ToString()
        {
            return $"Ch1: Enabled={IsEnabled}, Probe={ProbeRatio}×, Scale={VerticalScale}V/div, " +
                   $"Offset={VerticalOffset}V, Range={VerticalRange}V, Coupling={Coupling}";
        }

        /// <summary>
        /// Create default settings for common measurement scenarios
        /// </summary>
        public static class Presets
        {
            /// <summary>
            /// Settings for general purpose measurements (±4V range)
            /// </summary>
            public static Ch1Settings GeneralPurpose => new Ch1Settings
            {
                IsEnabled = true,
                ProbeRatio = 10.0,
                VerticalScale = 0.5,
                VerticalOffset = 0.0,
                Coupling = "DC",
                BandwidthLimit = "OFF"
            };

            /// <summary>
            /// Settings for small signal measurements (mV range)
            /// </summary>
            public static Ch1Settings SmallSignal => new Ch1Settings
            {
                IsEnabled = true,
                ProbeRatio = 1.0,
                VerticalScale = 0.01,
                VerticalOffset = 0.0,
                Coupling = "AC",  // AC coupling for small signals to remove DC offset
                BandwidthLimit = "OFF"
            };

            /// <summary>
            /// Settings for power measurements
            /// </summary>
            public static Ch1Settings PowerMeasurement => new Ch1Settings
            {
                IsEnabled = true,
                ProbeRatio = 10.0,
                VerticalScale = 5.0,
                VerticalOffset = -10.0,
                Coupling = "DC",  // DC coupling for power measurements
                BandwidthLimit = "OFF"
            };

            /// <summary>
            /// Settings for high frequency measurements
            /// </summary>
            public static Ch1Settings HighFrequency => new Ch1Settings
            {
                IsEnabled = true,
                ProbeRatio = 10.0,
                VerticalScale = 1.0,
                VerticalOffset = 0.0,
                Coupling = "DC",
                BandwidthLimit = "OFF"  // Keep full bandwidth for high frequency
            };

            /// <summary>
            /// Settings for DC voltage measurements
            /// </summary>
            public static Ch1Settings DCVoltage => new Ch1Settings
            {
                IsEnabled = true,
                ProbeRatio = 10.0,
                VerticalScale = 2.0,
                VerticalOffset = 0.0,
                Coupling = "DC",
                BandwidthLimit = "20M"  // Limit bandwidth to reduce noise
            };

            /// <summary>
            /// Settings for AC signal analysis
            /// </summary>
            public static Ch1Settings ACSignal => new Ch1Settings
            {
                IsEnabled = true,
                ProbeRatio = 10.0,
                VerticalScale = 1.0,
                VerticalOffset = 0.0,
                Coupling = "AC",  // AC coupling to remove DC component
                BandwidthLimit = "OFF"
            };
        }

        #endregion
    }
}