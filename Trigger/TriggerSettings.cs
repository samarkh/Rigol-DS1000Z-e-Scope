using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;
using System;
using System.Collections.Generic;

namespace DS1000Z_E_USB_Control.Trigger
{
    /// <summary>
    /// Model class for Trigger settings and configuration
    /// </summary>
    public class TriggerSettings
    {
        #region Properties

        /// <summary>
        /// Trigger mode (EDGe, PULSe, SLOPe, VIDeo, PATTern, etc.)
        /// </summary>
        public string Mode { get; set; } = "EDGe";

        /// <summary>
        /// Trigger coupling (AC, DC, LFREject, HFREject)
        /// </summary>
        public string Coupling { get; set; } = "DC";

        /// <summary>
        /// Trigger sweep mode (AUTO, NORMal, SINGle)
        /// </summary>
        public string Sweep { get; set; } = "AUTO";

        /// <summary>
        /// Edge trigger source (CHANnel1, CHANnel2, EXT, ACLine)
        /// </summary>
        public string EdgeSource { get; set; } = "CHANnel1";

        /// <summary>
        /// Edge trigger slope (POSitive, NEGative)
        /// </summary>
        public string EdgeSlope { get; set; } = "POSitive";

        /// <summary>
        /// Edge trigger level in volts
        /// </summary>
        public double EdgeLevel { get; set; } = 0.0;

        /// <summary>
        /// Trigger holdoff time in seconds
        /// </summary>
        public double Holdoff { get; set; } = 16e-9; // 16ns default

        /// <summary>
        /// Noise reject enable
        /// </summary>
        public bool NoiseReject { get; set; } = false;

        /// <summary>
        /// Trigger status (STOP, WAIT, RUN, AUTO, etc.)
        /// </summary>
        public string Status { get; set; } = "AUTO";

        /// <summary>
        /// Trigger position (percentage of screen)
        /// </summary>
        public double Position { get; set; } = 50.0; // 50% default

        #endregion

        #region Calculated Properties

        /// <summary>
        /// Get display string for trigger level
        /// </summary>
        public string EdgeLevelDisplay
        {
            get
            {
                if (Math.Abs(EdgeLevel) >= 1.0)
                    return $"{EdgeLevel:F3} V";
                else if (Math.Abs(EdgeLevel) >= 0.001)
                    return $"{EdgeLevel * 1000:F1} mV";
                else
                    return $"{EdgeLevel * 1000000:F0} µV";
            }
        }

        /// <summary>
        /// Get display string for holdoff time
        /// </summary>
        public string HoldoffDisplay
        {
            get
            {
                if (Holdoff >= 1.0)
                    return $"{Holdoff:F3} s";
                else if (Holdoff >= 1e-3)
                    return $"{Holdoff * 1000:F1} ms";
                else if (Holdoff >= 1e-6)
                    return $"{Holdoff * 1000000:F1} µs";
                else
                    return $"{Holdoff * 1000000000:F1} ns";
            }
        }

        #endregion

        #region Static Configuration Data

        /// <summary>
        /// Get available trigger mode options
        /// </summary>
        public static List<(string value, string display)> GetModeOptions()
        {
            return new List<(string, string)>
            {
                ("EDGe", "Edge"),
                ("PULSe", "Pulse"),
                ("SLOPe", "Slope"),
                ("VIDeo", "Video"),
                ("PATTern", "Pattern"),
                ("DURATion", "Duration"),
                ("TIMeout", "Timeout"),
                ("RUNT", "Runt"),
                ("WINDows", "Windows"),
                ("DELay", "Delay"),
                ("SHOLd", "Setup/Hold"),
                ("NEDGe", "Nth Edge"),
                ("RS232", "RS232"),
                ("IIC", "I2C"),
                ("SPI", "SPI")
            };
        }

        /// <summary>
        /// Get available trigger coupling options
        /// </summary>
        public static List<(string value, string display)> GetCouplingOptions()
        {
            return new List<(string, string)>
            {
                ("DC", "DC"),
                ("AC", "AC"),
                ("LFREject", "LF Reject"),
                ("HFREject", "HF Reject")
            };
        }

        /// <summary>
        /// Get available trigger sweep options
        /// </summary>
        public static List<(string value, string display)> GetSweepOptions()
        {
            return new List<(string, string)>
            {
                ("AUTO", "Auto"),
                ("NORMal", "Normal"),
                ("SINGle", "Single")
            };
        }

        /// <summary>
        /// Get available edge trigger source options
        /// </summary>
        public static List<(string value, string display)> GetEdgeSourceOptions()
        {
            return new List<(string, string)>
            {
                ("CHANnel1", "Channel 1"),
                ("CHANnel2", "Channel 2"),
                ("EXT", "External"),
                ("ACLine", "AC Line")
            };
        }

        /// <summary>
        /// Get available edge trigger slope options
        /// </summary>
        public static List<(string value, string display)> GetEdgeSlopeOptions()
        {
            return new List<(string, string)>
            {
                ("POSitive", "Rising Edge"),
                ("NEGative", "Falling Edge")
            };
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate if the trigger mode is supported
        /// </summary>
        public static bool IsValidMode(string mode)
        {
            var validModes = GetModeOptions();
            foreach (var (value, _) in validModes)
            {
                if (string.Equals(value, mode, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get trigger level range based on source channel (original method for backward compatibility)
        /// </summary>
        public (double min, double max) GetTriggerLevelRange()
        {
            // Default range when channel info not available
            return (-4.0, 4.0);
        }


        /// <summary>
        /// Get trigger level range based on source channel with graticule calculation
        /// </summary>
        public (double min, double max) GetTriggerLevelRange(Ch1Settings ch1Settings, Ch2Settings ch2Settings)
        {
            // Determine which channel is the trigger source
            double verticalScale, verticalOffset;

            if (EdgeSource.ToUpper().Contains("CHANNEL1") || EdgeSource.ToUpper().Contains("CHAN1"))
            {
                verticalScale = ch1Settings.VerticalScale;
                verticalOffset = ch1Settings.VerticalOffset;
            }
            else if (EdgeSource.ToUpper().Contains("CHANNEL2") || EdgeSource.ToUpper().Contains("CHAN2"))
            {
                verticalScale = ch2Settings.VerticalScale;
                verticalOffset = ch2Settings.VerticalOffset;
            }
            else
            {
                // External trigger or other sources - use reasonable default
                return (-4.0, 4.0);
            }

            // Rigol formula: (-4 × VerticalScale - VerticalOffset) to (+4 × VerticalScale - VerticalOffset)
            double min = -4.0 * verticalScale - verticalOffset;
            double max = +4.0 * verticalScale - verticalOffset;

            return (min, max);
        }


        /// <summary>
        /// Get holdoff range
        /// </summary>
        public (double min, double max) GetHoldoffRange()
        {
            return (16e-9, 10.0); // 16ns to 10s
        }

        #endregion

        #region Object Methods

        /// <summary>
        /// Create a deep copy of the settings
        /// </summary>
        public TriggerSettings Clone()
        {
            return new TriggerSettings
            {
                Mode = this.Mode,
                Coupling = this.Coupling,
                Sweep = this.Sweep,
                EdgeSource = this.EdgeSource,
                EdgeSlope = this.EdgeSlope,
                EdgeLevel = this.EdgeLevel,
                Holdoff = this.Holdoff,
                NoiseReject = this.NoiseReject,
                Status = this.Status,
                Position = this.Position
            };
        }

        /// <summary>
        /// Check if settings are equal
        /// </summary>
        public bool Equals(TriggerSettings other)
        {
            if (other == null) return false;

            return string.Equals(Mode, other.Mode, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Coupling, other.Coupling, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Sweep, other.Sweep, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(EdgeSource, other.EdgeSource, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(EdgeSlope, other.EdgeSlope, StringComparison.OrdinalIgnoreCase) &&
                   Math.Abs(EdgeLevel - other.EdgeLevel) < 0.001 &&
                   Math.Abs(Holdoff - other.Holdoff) < 1e-9 &&
                   NoiseReject == other.NoiseReject &&
                   string.Equals(Status, other.Status, StringComparison.OrdinalIgnoreCase) &&
                   Math.Abs(Position - other.Position) < 0.1;
        }

        /// <summary>
        /// Get a string representation of the settings
        /// </summary>
        public override string ToString()
        {
            return $"Trigger: Mode={Mode}, Source={EdgeSource}, Slope={EdgeSlope}, " +
                   $"Level={EdgeLevelDisplay}, Sweep={Sweep}, Coupling={Coupling}";
        }

        /// <summary>
        /// Create default settings for common scenarios
        /// </summary>
        public static class Presets
        {
            /// <summary>
            /// Settings for general purpose measurements
            /// </summary>
            public static TriggerSettings GeneralPurpose => new TriggerSettings
            {
                Mode = "EDGe",
                Coupling = "DC",
                Sweep = "AUTO",
                EdgeSource = "CHANnel1",
                EdgeSlope = "POSitive",
                EdgeLevel = 0.0,
                Holdoff = 16e-9,
                NoiseReject = false,
                Position = 50.0
            };

            /// <summary>
            /// Settings for single shot measurements
            /// </summary>
            public static TriggerSettings SingleShot => new TriggerSettings
            {
                Mode = "EDGe",
                Coupling = "DC",
                Sweep = "SINGle",
                EdgeSource = "CHANnel1",
                EdgeSlope = "POSitive",
                EdgeLevel = 0.0,
                Holdoff = 16e-9,
                NoiseReject = false,
                Position = 10.0 // Trigger near left side for single events
            };

            /// <summary>
            /// Settings for noisy signals
            /// </summary>
            public static TriggerSettings NoisySignal => new TriggerSettings
            {
                Mode = "EDGe",
                Coupling = "AC",
                Sweep = "AUTO",
                EdgeSource = "CHANnel1",
                EdgeSlope = "POSitive",
                EdgeLevel = 0.0,
                Holdoff = 100e-9, // Longer holdoff for noisy signals
                NoiseReject = true,
                Position = 50.0
            };

            /// <summary>
            /// Settings for digital signals
            /// </summary>
            public static TriggerSettings Digital => new TriggerSettings
            {
                Mode = "EDGe",
                Coupling = "DC",
                Sweep = "AUTO",
                EdgeSource = "CHANnel1",
                EdgeSlope = "POSitive",
                EdgeLevel = 1.65, // Typical digital threshold
                Holdoff = 16e-9,
                NoiseReject = false,
                Position = 50.0
            };

            /// <summary>
            /// Settings for power measurements
            /// </summary>
            public static TriggerSettings PowerMeasurement => new TriggerSettings
            {
                Mode = "EDGe",
                Coupling = "AC",
                Sweep = "AUTO",
                EdgeSource = "CHANnel1",
                EdgeSlope = "POSitive",
                EdgeLevel = 0.0,
                Holdoff = 16e-3, // Longer holdoff for power frequency
                NoiseReject = true,
                Position = 25.0 // Show more of the waveform after trigger
            };
        }

        #endregion
    }
}