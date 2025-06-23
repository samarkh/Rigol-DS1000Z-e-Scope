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
        #region Properties

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
        /// Statistical analysis mode (:MEASure:STATistic:MODE)
        /// </summary>
        public string StatisticMode { get; set; } = "DIFF";

        /// <summary>
        /// Whether statistics display is enabled (:MEASure:STATistic:DISPlay)
        /// </summary>
        public bool StatisticDisplayEnabled { get; set; } = false;

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

        /// <summary>
        /// Pulse setup parameter B (:MEASure:SETup:PSB)
        /// </summary>
        public double PulseSetupB { get; set; } = 50.0; // Percentage

        /// <summary>
        /// Delay setup parameter A (:MEASure:SETup:DSA)
        /// </summary>
        public double DelaySetupA { get; set; } = 50.0; // Percentage

        /// <summary>
        /// Delay setup parameter B (:MEASure:SETup:DSB)
        /// </summary>
        public double DelaySetupB { get; set; } = 50.0; // Percentage

        /// <summary>
        /// Counter frequency measurement enabled
        /// </summary>
        public bool CounterEnabled { get; set; } = false;

        /// <summary>
        /// Enable/disable automatic updates
        /// </summary>
        public bool AutoUpdateEnabled { get; set; } = true;

        /// <summary>
        /// Auto update interval in milliseconds
        /// </summary>
        public int AutoUpdateIntervalMs { get; set; } = 1000;

        /// <summary>
        /// Enable/disable statistics collection
        /// </summary>
        public bool StatisticsEnabled { get; set; } = true;

        #endregion

        #region Measurement Parameter Definitions

        /// <summary>
        /// Get all available measurement parameters with their descriptions
        /// Based on Rigol DS1000Z-E 37 automatic measurement parameters
        /// </summary>
        public static Dictionary<string, MeasurementParameter> GetAvailableParameters()
        {
            return new Dictionary<string, MeasurementParameter>
            {
                // Time Domain Measurements
                { "PERiod", new MeasurementParameter("PERiod", "Period", "Time", "Period of the waveform") },
                { "FREQuency", new MeasurementParameter("FREQuency", "Frequency", "Frequency", "Frequency of the waveform") },
                { "RTIMe", new MeasurementParameter("RTIMe", "Rise Time", "Time", "Rise time (10%-90%)") },
                { "FTIMe", new MeasurementParameter("FTIMe", "Fall Time", "Time", "Fall time (90%-10%)") },
                { "PWIDth", new MeasurementParameter("PWIDth", "+Width", "Time", "Positive pulse width") },
                { "NWIDth", new MeasurementParameter("NWIDth", "-Width", "Time", "Negative pulse width") },
                { "PDUTy", new MeasurementParameter("PDUTy", "+Duty", "Percentage", "Positive duty cycle") },
                { "NDUTy", new MeasurementParameter("NDUTy", "-Duty", "Percentage", "Negative duty cycle") },
                { "PPULses", new MeasurementParameter("PPULses", "+Pulses", "Count", "Positive pulse count") },
                { "NPULses", new MeasurementParameter("NPULses", "-Pulses", "Count", "Negative pulse count") },
                { "PEDGes", new MeasurementParameter("PEDGes", "+Edges", "Count", "Positive edge count") },
                { "NEDGes", new MeasurementParameter("NEDGes", "-Edges", "Count", "Negative edge count") },
                { "TVMax", new MeasurementParameter("TVMax", "tVmax", "Time", "Time at voltage maximum") },
                { "TVMin", new MeasurementParameter("TVMin", "tVmin", "Time", "Time at voltage minimum") },
                { "PRAte", new MeasurementParameter("PRAte", "+Rate", "V/s", "Positive slew rate") },
                { "NRAte", new MeasurementParameter("NRAte", "-Rate", "V/s", "Negative slew rate") },
                { "DEL12", new MeasurementParameter("DEL12", "Delay1→2", "Time", "Delay between channels 1 and 2") },
                { "PHA12", new MeasurementParameter("PHA12", "Phase1→2", "Degrees", "Phase between channels 1 and 2") },

                // Voltage Measurements
                { "VMAX", new MeasurementParameter("VMAX", "Vmax", "Voltage", "Maximum voltage") },
                { "VMIN", new MeasurementParameter("VMIN", "Vmin", "Voltage", "Minimum voltage") },
                { "VPP", new MeasurementParameter("VPP", "Vpp", "Voltage", "Peak-to-peak voltage") },
                { "VTOP", new MeasurementParameter("VTOP", "Vtop", "Voltage", "Top voltage (flat top)") },
                { "VBASe", new MeasurementParameter("VBASe", "Vbase", "Voltage", "Base voltage (flat base)") },
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

        #region Source Channel Options

        /// <summary>
        /// Get available source channel options
        /// </summary>
        public static List<(string value, string display)> GetSourceChannelOptions()
        {
            return new List<(string, string)>
            {
                ("CHANnel1", "Channel 1"),
                ("CHANnel2", "Channel 2"),
                ("MATH", "Math")
            };
        }

        #endregion

        #region Statistic Mode Options

        /// <summary>
        /// Get available statistic mode options
        /// </summary>
        public static List<(string value, string display)> GetStatisticModeOptions()
        {
            return new List<(string, string)>
            {
                ("DIFF", "Difference"),
                ("EXTRemum", "Extremum")
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Create a deep copy of settings
        /// </summary>
        public MeasurementSettings Clone()
        {
            return new MeasurementSettings
            {
                AutoDisplayEnabled = this.AutoDisplayEnabled,
                AutoMeasureSource = this.AutoMeasureSource,
                EnabledMeasurements = new List<string>(this.EnabledMeasurements),
                StatisticMode = this.StatisticMode,
                StatisticDisplayEnabled = this.StatisticDisplayEnabled,
                ThresholdMax = this.ThresholdMax,
                ThresholdMid = this.ThresholdMid,
                ThresholdMin = this.ThresholdMin,
                PulseSetupB = this.PulseSetupB,
                DelaySetupA = this.DelaySetupA,
                DelaySetupB = this.DelaySetupB,
                CounterEnabled = this.CounterEnabled
            };
        }

        /// <summary>
        /// Get a string representation of enabled measurements
        /// </summary>
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

        /// <summary>
        /// Check if a specific measurement is enabled
        /// </summary>
        public bool IsMeasurementEnabled(string measurementKey)
        {
            return EnabledMeasurements.Contains(measurementKey);
        }

        /// <summary>
        /// Enable a measurement
        /// </summary>
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
        /// Get display string for settings summary
        /// </summary>
        public override string ToString()
        {
            return $"Measurements: {EnabledMeasurements.Count} enabled, Source: {AutoMeasureSource}, " +
                   $"Statistics: {(StatisticDisplayEnabled ? "Enabled" : "Disabled")}";
        }

        #endregion
    }

    /// <summary>
    /// Represents a single measurement parameter
    /// </summary>
    public class MeasurementParameter
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public string Unit { get; set; }
        public string Description { get; set; }

        public MeasurementParameter(string key, string displayName, string unit, string description)
        {
            Key = key;
            DisplayName = displayName;
            Unit = unit;
            Description = description;
        }

        public override string ToString()
        {
            return $"{DisplayName} ({Unit})";
        }
    }
}