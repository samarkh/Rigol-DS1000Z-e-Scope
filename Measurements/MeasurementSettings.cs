using System;
using System.Collections.Generic;
using System.Linq;

namespace DS1000Z_E_USB_Control.Measurements
{
    /// <summary>
    /// Model class for measurement settings and configuration
    /// Implements SCPI commands for Rigol DS1000Z-E automatic measurements
    /// </summary>
    public class MeasurementSettings
    {
        #region Core Properties

        /// <summary>
        /// Whether automatic measurement display is enabled (:MEASure:ADISplay)
        /// </summary>
        public bool AutoDisplayEnabled { get; set; } = true;

        /// <summary>
        /// Automatic measurement source channel (:MEASure:AMSource)
        /// </summary>
        public string AutoMeasureSource { get; set; } = "CHANnel1";

        /// <summary>
        /// List of enabled measurement items (:MEASure:ITEM)
        /// </summary>
        public List<string> EnabledMeasurements { get; set; } = new List<string>();

        /// <summary>
        /// Whether auto update is enabled for measurements
        /// </summary>
        public bool AutoUpdateEnabled { get; set; } = false;

        /// <summary>
        /// Auto update interval in milliseconds
        /// </summary>
        public int AutoUpdateIntervalMs { get; set; } = 2000;

        #endregion

        #region Statistics Properties

        /// <summary>
        /// Statistical analysis mode (:MEASure:STATistic:MODE)
        /// </summary>
        public string StatisticMode { get; set; } = "DIFF";

        /// <summary>
        /// Whether statistics display is enabled (:MEASure:STATistic:DISPlay)
        /// </summary>
        public bool StatisticDisplayEnabled { get; set; } = false;

        /// <summary>
        /// Whether statistics are enabled for collection
        /// </summary>
        public bool StatisticsEnabled { get; set; } = false;

        #endregion

        #region Threshold Setup Properties

        /// <summary>
        /// Measurement threshold setup - Maximum level (:MEASure:SETup:MAX)
        /// </summary>
        public double ThresholdMax { get; set; } = 90.0; // Percentage

        /// <summary>
        /// Measurement threshold setup - Middle level (:MEASure:SETup:MID)
        /// </summary>
        public double ThresholdMid { get; set; } = 50.0; // Percentage

        /// <summary>
        /// Measurement threshold setup - Minimum level (:MEASure:SETup:MIN)
        /// </summary>
        public double ThresholdMin { get; set; } = 10.0; // Percentage

        #endregion

        #region Delay and Pulse Setup Properties

        /// <summary>
        /// Delay setup parameter A (:MEASure:SETup:DSA)
        /// </summary>
        public double DelaySetupA { get; set; } = 10.0; // Percentage

        /// <summary>
        /// Delay setup parameter B (:MEASure:SETup:DSB)
        /// </summary>
        public double DelaySetupB { get; set; } = 90.0; // Percentage

        /// <summary>
        /// Pulse setup parameter B (:MEASure:SETup:PSB)
        /// </summary>
        public double PulseSetupB { get; set; } = 50.0; // Percentage

        #endregion

        #region Counter Properties

        /// <summary>
        /// Whether counter functionality is enabled
        /// </summary>
        public bool CounterEnabled { get; set; } = false;

        #endregion

        #region Measurement Management Methods

        /// <summary>
        /// Check if a specific measurement is enabled
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <returns>True if enabled</returns>
        public bool IsMeasurementEnabled(string measurementKey)
        {
            return EnabledMeasurements.Contains(measurementKey);
        }

        /// <summary>
        /// Enable a measurement
        /// </summary>
        /// <param name="measurementKey">The measurement key to enable</param>
        public void EnableMeasurement(string measurementKey)
        {
            if (!EnabledMeasurements.Contains(measurementKey))
            {
                EnabledMeasurements.Add(measurementKey);
            }
        }

        /// <summary>
        /// Disable a measurement
        /// </summary>
        /// <param name="measurementKey">The measurement key to disable</param>
        public void DisableMeasurement(string measurementKey)
        {
            EnabledMeasurements.Remove(measurementKey);
        }

        /// <summary>
        /// Clear all enabled measurements
        /// </summary>
        public void ClearAllMeasurements()
        {
            EnabledMeasurements.Clear();
        }

        /// <summary>
        /// Get a string representation of enabled measurements
        /// </summary>
        /// <returns>Comma-separated list of enabled measurement names</returns>
        public string GetEnabledMeasurementsString()
        {
            if (!EnabledMeasurements.Any())
                return "None";

            var allParams = GetAvailableParameters();
            var enabledNames = EnabledMeasurements
                .Where(m => allParams.ContainsKey(m))
                .Select(m => allParams[m].DisplayName);

            return string.Join(", ", enabledNames);
        }

        #endregion

        #region Clone and Copy Methods

        /// <summary>
        /// Create a deep copy of the current settings
        /// </summary>
        /// <returns>A new MeasurementSettings instance with copied values</returns>
        public MeasurementSettings Clone()
        {
            return new MeasurementSettings
            {
                AutoDisplayEnabled = this.AutoDisplayEnabled,
                AutoMeasureSource = this.AutoMeasureSource,
                EnabledMeasurements = new List<string>(this.EnabledMeasurements),
                AutoUpdateEnabled = this.AutoUpdateEnabled,
                AutoUpdateIntervalMs = this.AutoUpdateIntervalMs,
                StatisticMode = this.StatisticMode,
                StatisticDisplayEnabled = this.StatisticDisplayEnabled,
                StatisticsEnabled = this.StatisticsEnabled,
                ThresholdMax = this.ThresholdMax,
                ThresholdMid = this.ThresholdMid,
                ThresholdMin = this.ThresholdMin,
                PulseSetupB = this.PulseSetupB,
                DelaySetupA = this.DelaySetupA,
                DelaySetupB = this.DelaySetupB,
                CounterEnabled = this.CounterEnabled
            };
        }

        #endregion

        #region Static Available Parameters

        /// <summary>
        /// Get all available measurement parameters with their descriptions
        /// </summary>
        /// <returns>Dictionary of measurement parameters</returns>
        public static Dictionary<string, MeasurementParameter> GetAvailableParameters()
        {
            return new Dictionary<string, MeasurementParameter>
            {
                // Time Domain Measurements
                { "PERiod", new MeasurementParameter("PERiod", "Period", "Time", "Waveform period") },
                { "FREQuency", new MeasurementParameter("FREQuency", "Frequency", "Frequency", "Waveform frequency") },
                { "RTIMe", new MeasurementParameter("RTIMe", "Rise Time", "Time", "10%-90% rise time") },
                { "FTIMe", new MeasurementParameter("FTIMe", "Fall Time", "Time", "90%-10% fall time") },
                { "PWIDth", new MeasurementParameter("PWIDth", "Positive Width", "Time", "Positive pulse width") },
                { "NWIDth", new MeasurementParameter("NWIDth", "Negative Width", "Time", "Negative pulse width") },
                { "PDUTy", new MeasurementParameter("PDUTy", "Positive Duty", "Percentage", "Positive duty cycle") },
                { "NDUTy", new MeasurementParameter("NDUTy", "Negative Duty", "Percentage", "Negative duty cycle") },
                { "TVMax", new MeasurementParameter("TVMax", "Time at Vmax", "Time", "Time when maximum voltage occurs") },
                { "TVMin", new MeasurementParameter("TVMin", "Time at Vmin", "Time", "Time when minimum voltage occurs") },
                { "PRAte", new MeasurementParameter("PRAte", "Positive Rate", "V/s", "Positive slew rate") },
                { "NRAte", new MeasurementParameter("NRAte", "Negative Rate", "V/s", "Negative slew rate") },
                { "DEL12", new MeasurementParameter("DEL12", "Delay 1→2", "Time", "Delay between channels 1 and 2") },
                { "PHA12", new MeasurementParameter("PHA12", "Phase 1→2", "Degrees", "Phase between channels 1 and 2") },
                
                // Pulse and Edge Count Measurements
                { "PPULses", new MeasurementParameter("PPULses", "Positive Pulses", "Count", "Positive pulse count") },
                { "NPULses", new MeasurementParameter("NPULses", "Negative Pulses", "Count", "Negative pulse count") },
                { "PEDGes", new MeasurementParameter("PEDGes", "Positive Edges", "Count", "Positive edge count") },
                { "NEDGes", new MeasurementParameter("NEDGes", "Negative Edges", "Count", "Negative edge count") },
                
                // Voltage Level Measurements
                { "VMAX", new MeasurementParameter("VMAX", "Vmax", "Voltage", "Maximum voltage") },
                { "VMIN", new MeasurementParameter("VMIN", "Vmin", "Voltage", "Minimum voltage") },
                { "VPP", new MeasurementParameter("VPP", "Vpp", "Voltage", "Peak-to-peak voltage") },
                { "VTOP", new MeasurementParameter("VTOP", "Vtop", "Voltage", "Flat top voltage") },
                { "VBASe", new MeasurementParameter("VBASe", "Vbase", "Voltage", "Flat base voltage") },
                { "VAMP", new MeasurementParameter("VAMP", "Vamp", "Voltage", "Amplitude (Vtop - Vbase)") },
                { "VUPPer", new MeasurementParameter("VUPPer", "Vupper", "Voltage", "Upper reference level") },
                { "VMID", new MeasurementParameter("VMID", "Vmid", "Voltage", "Middle reference level") },
                { "VLOWer", new MeasurementParameter("VLOWer", "Vlower", "Voltage", "Lower reference level") },
                { "VAVG", new MeasurementParameter("VAVG", "Vavg", "Voltage", "Average voltage") },
                { "VRMS", new MeasurementParameter("VRMS", "Vrms", "Voltage", "RMS voltage") },
                { "OVERshoot", new MeasurementParameter("OVERshoot", "Overshoot", "Percentage", "Overshoot percentage") },
                { "PREShoot", new MeasurementParameter("PREShoot", "Preshoot", "Percentage", "Preshoot percentage") },
                
                // Area and Advanced Measurements
                { "AREA", new MeasurementParameter("AREA", "Area", "V·s", "Waveform area") },
                { "PARea", new MeasurementParameter("PARea", "Period Area", "V·s", "Area of one period") },
                { "PVRMs", new MeasurementParameter("PVRMs", "Period RMS", "Voltage", "RMS of one period") },
                { "VARiance", new MeasurementParameter("VARiance", "Variance", "V²", "Voltage variance") }
            };
        }

        /// <summary>
        /// Get measurement parameters organized by category
        /// </summary>
        /// <returns>Dictionary of categories with their measurement parameters</returns>
        public static Dictionary<string, List<MeasurementParameter>> GetParametersByCategory()
        {
            var allParams = GetAvailableParameters();
            return new Dictionary<string, List<MeasurementParameter>>
            {
                {
                    "Time Domain",
                    allParams.Where(p => new[] { "PERiod", "FREQuency", "RTIMe", "FTIMe", "PWIDth", "NWIDth",
                                                "PDUTy", "NDUTy", "TVMax", "TVMin", "PRAte", "NRAte",
                                                "DEL12", "PHA12" }.Contains(p.Key))
                            .Select(p => p.Value).ToList()
                },
                {
                    "Pulse & Edge Count",
                    allParams.Where(p => new[] { "PPULses", "NPULses", "PEDGes", "NEDGes" }.Contains(p.Key))
                            .Select(p => p.Value).ToList()
                },
                {
                    "Voltage Levels",
                    allParams.Where(p => new[] { "VMAX", "VMIN", "VPP", "VTOP", "VBASe", "VAMP",
                                                "VUPPer", "VMID", "VLOWer", "VAVG", "VRMS" }.Contains(p.Key))
                            .Select(p => p.Value).ToList()
                },
                {
                    "Signal Quality",
                    allParams.Where(p => new[] { "OVERshoot", "PREShoot", "VARiance" }.Contains(p.Key))
                            .Select(p => p.Value).ToList()
                },
                {
                    "Area & Advanced",
                    allParams.Where(p => new[] { "AREA", "PARea", "PVRMs" }.Contains(p.Key))
                            .Select(p => p.Value).ToList()
                }
            };
        }

        #endregion

        #region Static Options Methods

        /// <summary>
        /// Get available source channel options
        /// </summary>
        /// <returns>List of source channel options</returns>
        public static List<(string value, string display)> GetSourceChannelOptions()
        {
            return new List<(string, string)>
            {
                ("CHANnel1", "Channel 1"),
                ("CHANnel2", "Channel 2"),
                ("MATH", "Math")
            };
        }

        /// <summary>
        /// Get available statistic mode options
        /// </summary>
        /// <returns>List of statistic mode options</returns>
        public static List<(string value, string display)> GetStatisticModeOptions()
        {
            return new List<(string, string)>
            {
                ("DIFF", "Difference"),
                ("EXTRemum", "Extremum")
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Get display string for settings summary
        /// </summary>
        /// <returns>Human-readable settings summary</returns>
        public override string ToString()
        {
            return $"Measurements: {EnabledMeasurements.Count} enabled, Source: {AutoMeasureSource}, " +
                   $"Statistics: {(StatisticDisplayEnabled ? "Enabled" : "Disabled")}";
        }

        #endregion
    }

    /// <summary>
    /// Represents a single measurement parameter with metadata
    /// </summary>
    public class MeasurementParameter
    {
        #region Properties

        /// <summary>
        /// SCPI command key for the measurement
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Human-readable display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Unit of measurement
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Description of what this measurement represents
        /// </summary>
        public string Description { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize a new measurement parameter
        /// </summary>
        /// <param name="key">SCPI command key</param>
        /// <param name="displayName">Human-readable name</param>
        /// <param name="unit">Unit of measurement</param>
        /// <param name="description">Description</param>
        public MeasurementParameter(string key, string displayName, string unit, string description)
        {
            Key = key;
            DisplayName = displayName;
            Unit = unit;
            Description = description;
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Get string representation of the parameter
        /// </summary>
        /// <returns>Display name with unit</returns>
        public override string ToString()
        {
            return $"{DisplayName} ({Unit})";
        }

        #endregion
    }
}