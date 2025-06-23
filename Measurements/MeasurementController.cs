using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text;
using Rigol_DS1000Z_E_Control;
using DS1000Z_E_USB_Control.Measurements;

namespace DS1000Z_E_USB_Control.Measurements
{
    /// <summary>
    /// Complete controller for handling measurement operations and SCPI communication
    /// Implements Rigol DS1000Z-E measurement commands with full functionality
    /// </summary>
    public class MeasurementController
    {
        #region Fields

        private readonly RigolDS1000ZE oscilloscope;
        private MeasurementSettings settings;
        private readonly Dictionary<string, object> currentMeasurementValues;
        private readonly Dictionary<string, MeasurementStatistics> measurementStatistics;

        #endregion

        #region Events

        /// <summary>
        /// Event raised when measurement values are updated
        /// </summary>
        public event EventHandler<MeasurementValueEventArgs> MeasurementValueUpdated;

        /// <summary>
        /// Event raised when measurement statistics are updated
        /// </summary>
        public event EventHandler<MeasurementStatisticsEventArgs> MeasurementStatisticsUpdated;

        /// <summary>
        /// Event raised for logging purposes
        /// </summary>
        public event EventHandler<string> LogEvent;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize measurement controller with oscilloscope connection
        /// </summary>
        /// <param name="oscilloscope">The oscilloscope instance</param>
        public MeasurementController(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));
            this.settings = new MeasurementSettings();
            this.currentMeasurementValues = new Dictionary<string, object>();
            this.measurementStatistics = new Dictionary<string, MeasurementStatistics>();

            Log("MeasurementController initialized");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Current measurement settings
        /// </summary>
        public MeasurementSettings Settings
        {
            get => settings;
            set => settings = value ?? new MeasurementSettings();
        }

        /// <summary>
        /// Current measurement statistics (for UI access)
        /// </summary>
        public Dictionary<string, MeasurementStatistics> Statistics
        {
            get => measurementStatistics;
        }

        /// <summary>
        /// Current measurement values (for UI access)
        /// </summary>
        public Dictionary<string, object> CurrentValues
        {
            get => currentMeasurementValues;
        }

        #endregion

        #region Core Measurement Methods

        /// <summary>
        /// Update a specific measurement from the oscilloscope
        /// </summary>
        /// <param name="measurementKey">The measurement type (e.g., "VMAX", "FREQuency", etc.)</param>
        /// <returns>True if successful</returns>
        public bool UpdateMeasurement(string measurementKey)
        {
            try
            {
                string query = $":MEASure:ITEM? {measurementKey}";
                string response = oscilloscope.SendQuery(query);

                if (!string.IsNullOrEmpty(response) &&
                    double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    currentMeasurementValues[measurementKey] = value;

                    // Raise event for UI update
                    MeasurementValueUpdated?.Invoke(this, new MeasurementValueEventArgs(measurementKey, value));

                    return true;
                }
                else
                {
                    Log($"Invalid response for {measurementKey}: {response}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"Error updating measurement {measurementKey}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update all enabled measurements
        /// </summary>
        /// <returns>True if successful</returns>
        public bool UpdateAllMeasurements()
        {
            try
            {
                foreach (var measurementKey in Settings.EnabledMeasurements)
                {
                    UpdateMeasurement(measurementKey);
                }

                Log($"Updated {Settings.EnabledMeasurements.Count} measurements");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error updating all measurements: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Add a measurement to the enabled measurements list
        /// </summary>
        /// <param name="measurementKey">The measurement type (e.g., "VMAX", "FREQuency", etc.)</param>
        /// <returns>True if successful</returns>
        public bool AddMeasurement(string measurementKey)
        {
            try
            {
                if (string.IsNullOrEmpty(measurementKey))
                {
                    Log("Cannot add measurement: measurement key is null or empty");
                    return false;
                }

                // Enable the measurement on the device
                string command = $":MEASure:ITEM {measurementKey},{Settings.AutoMeasureSource}";
                if (oscilloscope.SendCommand(command))
                {
                    Settings.EnableMeasurement(measurementKey);
                    Log($"Measurement added: {measurementKey}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error adding measurement {measurementKey}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clear all measurements
        /// </summary>
        /// <returns>True if successful</returns>
        public bool ClearAllMeasurements()
        {
            try
            {
                string command = ":MEASure:CLEar";
                if (oscilloscope.SendCommand(command))
                {
                    Settings.ClearAllMeasurements();
                    currentMeasurementValues.Clear();
                    measurementStatistics.Clear();
                    Log("All measurements cleared");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error clearing measurements: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reset all measurements
        /// </summary>
        /// <returns>True if successful</returns>
        public bool ResetAllMeasurements()
        {
            try
            {
                string command = ":MEASure:CLEar";
                if (oscilloscope.SendCommand(command))
                {
                    Settings.ClearAllMeasurements();
                    currentMeasurementValues.Clear();
                    measurementStatistics.Clear();
                    Log("All measurements reset");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error resetting measurements: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Statistics Methods

        /// <summary>
        /// Enable or disable statistics display
        /// </summary>
        /// <param name="enabled">True to enable statistics</param>
        /// <returns>True if successful</returns>
        public bool EnableStatistics(bool enabled)
        {
            try
            {
                string command = $":MEASure:STATistic:DISPlay {(enabled ? "ON" : "OFF")}";
                if (oscilloscope.SendCommand(command))
                {
                    Settings.StatisticDisplayEnabled = enabled;
                    Log($"Statistics display {(enabled ? "enabled" : "disabled")}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error setting statistics display: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set statistics mode
        /// </summary>
        /// <param name="mode">Statistics mode (DIFF, EXTR, etc.)</param>
        /// <returns>True if successful</returns>
        public bool SetStatisticsMode(string mode)
        {
            try
            {
                string command = $":MEASure:STATistic:MODE {mode}";
                if (oscilloscope.SendCommand(command))
                {
                    Settings.StatisticMode = mode;
                    Log($"Statistics mode set to: {mode}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error setting statistics mode: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        /// <returns>True if successful</returns>
        public bool ResetStatistics()
        {
            try
            {
                string command = ":MEASure:STATistic:RESet";
                if (oscilloscope.SendCommand(command))
                {
                    measurementStatistics.Clear();
                    Log("Statistics reset");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error resetting statistics: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update statistics for all measurements
        /// </summary>
        /// <returns>True if successful</returns>
        public bool UpdateStatistics()
        {
            try
            {
                foreach (var measurementKey in Settings.EnabledMeasurements)
                {
                    UpdateMeasurementStatisticsFromDevice(measurementKey);
                }

                Log($"Statistics updated for {Settings.EnabledMeasurements.Count} measurements");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error updating statistics: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Configuration Methods

        /// <summary>
        /// Set source channel for measurements
        /// </summary>
        /// <param name="channel">Channel name (CHANnel1, CHANnel2, etc.)</param>
        /// <returns>True if successful</returns>
        public bool SetSourceChannel(string channel)
        {
            try
            {
                string command = $":MEASure:SOURce {channel}";
                if (oscilloscope.SendCommand(command))
                {
                    Settings.AutoMeasureSource = channel;
                    Log($"Measurement source set to: {channel}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error setting source channel: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set auto display mode
        /// </summary>
        /// <param name="enabled">True to enable auto display</param>
        /// <returns>True if successful</returns>
        public bool SetAutoDisplay(bool enabled)
        {
            try
            {
                string command = $":MEASure:ADISplay {(enabled ? "ON" : "OFF")}";
                if (oscilloscope.SendCommand(command))
                {
                    Settings.AutoDisplayEnabled = enabled;
                    Log($"Auto display {(enabled ? "enabled" : "disabled")}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error setting auto display: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Threshold Configuration Methods

        /// <summary>
        /// Set threshold maximum
        /// </summary>
        /// <param name="value">Maximum threshold value</param>
        /// <returns>True if successful</returns>
        public bool SetThresholdMax(double value)
        {
            try
            {
                string command = $":MEASure:SETup:MAX {value:F3}";
                if (oscilloscope.SendCommand(command))
                {
                    Settings.ThresholdMax = value;
                    Log($"Threshold max set to {value:F3}V");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error setting threshold max: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set threshold middle
        /// </summary>
        /// <param name="value">Middle threshold value</param>
        /// <returns>True if successful</returns>
        public bool SetThresholdMid(double value)
        {
            try
            {
                string command = $":MEASure:SETup:MID {value:F3}";
                if (oscilloscope.SendCommand(command))
                {
                    Settings.ThresholdMid = value;
                    Log($"Threshold mid set to {value:F3}V");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error setting threshold mid: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set threshold minimum
        /// </summary>
        /// <param name="value">Minimum threshold value</param>
        /// <returns>True if successful</returns>
        public bool SetThresholdMin(double value)
        {
            try
            {
                string command = $":MEASure:SETup:MIN {value:F3}";
                if (oscilloscope.SendCommand(command))
                {
                    Settings.ThresholdMin = value;
                    Log($"Threshold min set to {value:F3}V");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error setting threshold min: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set all threshold levels at once
        /// </summary>
        /// <param name="max">Maximum threshold</param>
        /// <param name="mid">Middle threshold</param>
        /// <param name="min">Minimum threshold</param>
        /// <returns>True if successful</returns>
        public bool SetThresholdLevels(double max, double mid, double min)
        {
            bool success = true;
            success &= SetThresholdMax(max);
            success &= SetThresholdMid(mid);
            success &= SetThresholdMin(min);
            return success;
        }

        #endregion

        #region Delay and Pulse Setup Methods

        /// <summary>
        /// Set delay setup A
        /// </summary>
        /// <param name="value">Delay setup A value</param>
        /// <returns>True if successful</returns>
        public bool SetDelaySetupA(double value)
        {
            try
            {
                string command = $":MEASure:SETup:DELay:A {value:F6}";
                if (oscilloscope.SendCommand(command))
                {
                    Settings.DelaySetupA = value;
                    Log($"Delay setup A set to {value:F6}s");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error setting delay setup A: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set delay setup B
        /// </summary>
        /// <param name="value">Delay setup B value</param>
        /// <returns>True if successful</returns>
        public bool SetDelaySetupB(double value)
        {
            try
            {
                string command = $":MEASure:SETup:DELay:B {value:F6}";
                if (oscilloscope.SendCommand(command))
                {
                    Settings.DelaySetupB = value;
                    Log($"Delay setup B set to {value:F6}s");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error setting delay setup B: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set delay setup for both A and B
        /// </summary>
        /// <param name="delayA">Delay A value</param>
        /// <param name="delayB">Delay B value</param>
        /// <returns>True if successful</returns>
        public bool SetDelaySetup(double delayA, double delayB)
        {
            bool success = true;
            success &= SetDelaySetupA(delayA);
            success &= SetDelaySetupB(delayB);
            return success;
        }

        /// <summary>
        /// Set pulse setup B
        /// </summary>
        /// <param name="value">Pulse setup B value</param>
        /// <returns>True if successful</returns>
        public bool SetPulseSetupB(double value)
        {
            try
            {
                string command = $":MEASure:SETup:PSA {value:F6}";
                if (oscilloscope.SendCommand(command))
                {
                    Settings.PulseSetupB = value;
                    Log($"Pulse setup B set to {value:F6}s");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error setting pulse setup B: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Settings Application Methods

        /// <summary>
        /// Apply settings to the oscilloscope
        /// </summary>
        /// <param name="settings">Settings to apply</param>
        /// <returns>True if successful</returns>
        public bool ApplySettings(MeasurementSettings settings)
        {
            if (settings == null) return false;

            this.settings = settings;
            return ApplyAllSettings();
        }

        /// <summary>
        /// Apply all current settings to the oscilloscope
        /// </summary>
        /// <returns>True if all settings applied successfully</returns>
        public bool ApplyAllSettings()
        {
            try
            {
                bool success = true;

                success &= SetAutoDisplay(Settings.AutoDisplayEnabled);
                success &= SetSourceChannel(Settings.AutoMeasureSource);
                success &= EnableStatistics(Settings.StatisticDisplayEnabled);
                success &= SetStatisticsMode(Settings.StatisticMode);
                success &= SetThresholdLevels(Settings.ThresholdMax, Settings.ThresholdMid, Settings.ThresholdMin);
                success &= SetPulseSetupB(Settings.PulseSetupB);
                success &= SetDelaySetup(Settings.DelaySetupA, Settings.DelaySetupB);

                if (success)
                {
                    Log("All measurement settings applied successfully");
                }
                else
                {
                    Log("Some measurement settings failed to apply");
                }

                return success;
            }
            catch (Exception ex)
            {
                Log($"Error applying settings: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Export Methods (Replaces MeasurementExportUtility.cs)

        /// <summary>
        /// Export measurement data to file
        /// </summary>
        /// <param name="filePath">The file path to export to</param>
        /// <returns>True if successful</returns>
        public bool ExportMeasurementData(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    Log("Cannot export: file path is null or empty");
                    return false;
                }

                var sb = new StringBuilder();
                sb.AppendLine("Measurement,Current Value,Average,Minimum,Maximum,Std Deviation,Count");

                foreach (var kvp in currentMeasurementValues)
                {
                    var stats = measurementStatistics.ContainsKey(kvp.Key) ?
                               measurementStatistics[kvp.Key] : null;

                    sb.AppendLine($"{kvp.Key}," +
                                 $"{kvp.Value}," +
                                 $"{stats?.Average ?? 0.0}," +
                                 $"{stats?.Minimum ?? 0.0}," +
                                 $"{stats?.Maximum ?? 0.0}," +
                                 $"{stats?.StandardDeviation ?? 0.0}," +
                                 $"{stats?.Count ?? 0}");
                }

                File.WriteAllText(filePath, sb.ToString());
                Log($"Measurement data exported to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error exporting measurement data: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Quick Setup Methods (Replaces QuickSetupDialog functionality)

        /// <summary>
        /// Apply quick setup preset
        /// </summary>
        /// <param name="presetName">Name of the preset</param>
        /// <returns>True if successful</returns>
        public bool ApplyQuickSetupPreset(string presetName)
        {
            try
            {
                string[] measurements = presetName.ToLower() switch
                {
                    "timedomain" => new[] { "PERiod", "FREQuency", "RTIMe", "FTIMe", "PWIDth", "NWIDth", "PDUTy", "NDUTy" },
                    "voltage" => new[] { "VMAX", "VMIN", "VPP", "VAVG", "VRMS" },
                    "comprehensive" => new[] { "VMAX", "VMIN", "VPP", "VAVG", "VRMS", "PERiod", "FREQuency", "RTIMe", "FTIMe", "PWIDth", "NWIDth", "PDUTy", "NDUTy" },
                    _ => new string[0]
                };

                if (measurements.Length == 0)
                {
                    Log($"Unknown preset: {presetName}");
                    return false;
                }

                // Clear existing measurements
                Settings.ClearAllMeasurements();

                // Add preset measurements
                foreach (var measurement in measurements)
                {
                    Settings.EnableMeasurement(measurement);
                    AddMeasurement(measurement);
                }

                // Enable statistics for comprehensive preset
                if (presetName.ToLower() == "comprehensive")
                {
                    Settings.StatisticDisplayEnabled = true;
                    EnableStatistics(true);
                }

                Log($"Applied {presetName} preset with {measurements.Length} measurements");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error applying preset {presetName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get available quick setup presets
        /// </summary>
        /// <returns>Dictionary of preset names and descriptions</returns>
        public Dictionary<string, string> GetAvailablePresets()
        {
            return new Dictionary<string, string>
            {
                { "TimeDomain", "Time Domain Analysis: Frequency, Period, Rise/Fall Time, Duty Cycle" },
                { "Voltage", "Voltage Analysis: Max, Min, Peak-to-Peak, Average, RMS" },
                { "Comprehensive", "Comprehensive Analysis: All common measurements + Statistics" }
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Check if a measurement is enabled
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <returns>True if enabled</returns>
        public bool IsMeasurementEnabled(string measurementKey)
        {
            return Settings.IsMeasurementEnabled(measurementKey);
        }

        /// <summary>
        /// Get all available measurement types using existing project method
        /// </summary>
        /// <returns>Dictionary of available measurement parameters</returns>
        public Dictionary<string, MeasurementParameter> GetAvailableMeasurements()
        {
            return MeasurementSettings.GetAvailableParameters();
        }

        /// <summary>
        /// Update statistics for a specific measurement from the device
        /// </summary>
        /// <param name="measurementKey">The measurement to update statistics for</param>
        private void UpdateMeasurementStatisticsFromDevice(string measurementKey)
        {
            try
            {
                // Query various statistics from the device
                var current = QueryMeasurementStatistic(measurementKey, "CURRent");
                var average = QueryMeasurementStatistic(measurementKey, "AVERages");
                var minimum = QueryMeasurementStatistic(measurementKey, "MINimum");
                var maximum = QueryMeasurementStatistic(measurementKey, "MAXimum");
                var stddev = QueryMeasurementStatistic(measurementKey, "SDEViation");

                if (current.HasValue)
                {
                    var stats = new MeasurementStatistics
                    {
                        MeasurementKey = measurementKey,
                        Current = current.Value,
                        Average = average ?? current.Value,
                        Minimum = minimum ?? current.Value,
                        Maximum = maximum ?? current.Value,
                        StandardDeviation = stddev ?? 0.0,
                        Count = 1 // This would need to be tracked separately
                    };

                    measurementStatistics[measurementKey] = stats;

                    // Raise event for UI update
                    MeasurementStatisticsUpdated?.Invoke(this, new MeasurementStatisticsEventArgs(measurementKey, stats));
                }
            }
            catch (Exception ex)
            {
                Log($"Error updating statistics for {measurementKey}: {ex.Message}");
            }
        }

        /// <summary>
        /// Query a specific measurement statistic from the device
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <param name="statisticType">The statistic type</param>
        /// <returns>The statistic value or null if failed</returns>
        private double? QueryMeasurementStatistic(string measurementKey, string statisticType)
        {
            try
            {
                string query = $":MEASure:STATistic:ITEM? {statisticType},{measurementKey}";
                string response = oscilloscope.SendQuery(query);

                if (double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    return value;
                }
                return null;
            }
            catch (Exception ex)
            {
                Log($"Error querying {statisticType} for {measurementKey}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Log a message with timestamp
        /// </summary>
        /// <param name="message">The message to log</param>
        private void Log(string message)
        {
            LogEvent?.Invoke(this, $"[{DateTime.Now:HH:mm:ss}] [Measurements] {message}");
        }

        #endregion
    }
}