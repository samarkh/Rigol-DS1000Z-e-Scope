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
    /// CORRECTED: Uses existing project classes and structure
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
        /// Current measurement values (read-only copy)
        /// </summary>
        public Dictionary<string, object> CurrentValues => new Dictionary<string, object>(currentMeasurementValues);

        /// <summary>
        /// Current measurement statistics (read-only copy)
        /// </summary>
        public Dictionary<string, MeasurementStatistics> Statistics =>
            new Dictionary<string, MeasurementStatistics>(measurementStatistics);

        #endregion

        #region Basic Measurement Control

        /// <summary>
        /// Set automatic measurement display (:MEASure:ADISplay)
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
                    settings.AutoDisplayEnabled = enabled;
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

        /// <summary>
        /// Add a measurement to the enabled measurements list
        /// ADDED: This method was missing and causing compilation errors
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
                    // Use the existing method from MeasurementSettings
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
        /// ADDED: This method was missing and causing compilation errors
        /// </summary>
        /// <returns>True if successful</returns>
        public bool ClearAllMeasurements()
        {
            try
            {
                string command = ":MEASure:CLEar";
                if (oscilloscope.SendCommand(command))
                {
                    // Use the existing method from MeasurementSettings
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
        /// Set the source channel for measurements
        /// ADDED: This method was missing and causing compilation errors
        /// </summary>
        /// <param name="channel">Channel name (e.g., "CHANnel1", "CHANnel2", "MATH")</param>
        /// <returns>True if successful</returns>
        public bool SetSourceChannel(string channel)
        {
            try
            {
                if (string.IsNullOrEmpty(channel))
                {
                    Log("Cannot set source channel: channel is null or empty");
                    return false;
                }

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
                Log($"Error setting measurement source: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disable a specific measurement
        /// </summary>
        /// <param name="measurementKey">The measurement to disable</param>
        /// <returns>True if successful</returns>
        public bool DisableMeasurement(string measurementKey)
        {
            try
            {
                if (string.IsNullOrEmpty(measurementKey))
                    return false;

                string command = $":MEASure:ITEM {measurementKey},OFF";
                if (oscilloscope.SendCommand(command))
                {
                    // Use the existing method from MeasurementSettings
                    Settings.DisableMeasurement(measurementKey);
                    currentMeasurementValues.Remove(measurementKey);
                    measurementStatistics.Remove(measurementKey);
                    Log($"Measurement disabled: {measurementKey}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error disabling measurement {measurementKey}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Statistics Control

        /// <summary>
        /// Enable or disable statistics display
        /// ADDED: This method was missing and causing compilation errors
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
                    settings.StatisticDisplayEnabled = enabled;
                    settings.StatisticsEnabled = enabled; // Update both properties for compatibility
                    Log($"Statistics {(enabled ? "enabled" : "disabled")}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error setting statistics: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set statistics mode
        /// </summary>
        /// <param name="mode">Statistics mode (e.g., "DIFF", "EXTRemum")</param>
        /// <returns>True if successful</returns>
        public bool SetStatisticsMode(string mode)
        {
            try
            {
                if (string.IsNullOrEmpty(mode))
                    return false;

                string command = $":MEASure:STATistic:MODE {mode}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.StatisticMode = mode;
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
        /// Reset measurement statistics
        /// </summary>
        /// <returns>True if successful</returns>
        public bool ResetStatistics()
        {
            try
            {
                string command = ":MEASure:STATistic:RESet";
                if (oscilloscope.SendCommand(command))
                {
                    // Clear local statistics
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

        #endregion

        #region Data Update and Retrieval

        /// <summary>
        /// Update all enabled measurements
        /// </summary>
        /// <returns>True if all measurements updated successfully</returns>
        public bool UpdateAllMeasurements()
        {
            try
            {
                bool allSuccess = true;
                int successCount = 0;

                foreach (var measurementKey in Settings.EnabledMeasurements.ToList())
                {
                    if (UpdateSingleMeasurement(measurementKey))
                    {
                        successCount++;
                    }
                    else
                    {
                        allSuccess = false;
                    }
                }

                if (successCount > 0)
                {
                    Log($"Updated {successCount} of {Settings.EnabledMeasurements.Count} measurements");
                }

                return allSuccess;
            }
            catch (Exception ex)
            {
                Log($"Error updating measurements: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update a single measurement value
        /// FIXED: Changed QueryCommand to SendQuery
        /// </summary>
        /// <param name="measurementKey">The measurement to update</param>
        /// <returns>True if successful</returns>
        private bool UpdateSingleMeasurement(string measurementKey)
        {
            try
            {
                // Query the specific measurement from oscilloscope
                string query = $":MEASure:ITEM? {measurementKey}";
                string response = oscilloscope.SendQuery(query); // FIXED: was QueryCommand

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
        /// FIXED: Changed QueryCommand to SendQuery
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <param name="statisticType">The statistic type</param>
        /// <returns>The statistic value or null if failed</returns>
        private double? QueryMeasurementStatistic(string measurementKey, string statisticType)
        {
            try
            {
                string query = $":MEASure:STATistic:ITEM? {statisticType},{measurementKey}";
                string response = oscilloscope.SendQuery(query); // FIXED: was QueryCommand

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

        #endregion

        #region Data Export

        /// <summary>
        /// Export measurement data to file
        /// ADDED: This method was missing and causing compilation errors
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
                        measurementStatistics[kvp.Key] : new MeasurementStatistics { MeasurementKey = kvp.Key };

                    sb.AppendLine($"{kvp.Key},{kvp.Value},{stats.Average},{stats.Minimum},{stats.Maximum},{stats.StandardDeviation},{stats.Count}");
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

        #region Threshold Setup Methods

        /// <summary>
        /// Set threshold levels (:MEASure:SETup:MAX/MID/MIN)
        /// </summary>
        public bool SetThresholdLevels(double max, double mid, double min)
        {
            try
            {
                bool success = true;

                success &= oscilloscope.SendCommand($":MEASure:SETup:MAX {max}");
                success &= oscilloscope.SendCommand($":MEASure:SETup:MID {mid}");
                success &= oscilloscope.SendCommand($":MEASure:SETup:MIN {min}");

                if (success)
                {
                    settings.ThresholdMax = max;
                    settings.ThresholdMid = mid;
                    settings.ThresholdMin = min;
                    Log($"Threshold levels set: Max={max}%, Mid={mid}%, Min={min}%");
                }

                return success;
            }
            catch (Exception ex)
            {
                Log($"Error setting threshold levels: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set pulse setup parameter B (:MEASure:SETup:PSB)
        /// </summary>
        public bool SetPulseSetupB(double value)
        {
            try
            {
                string command = $":MEASure:SETup:PSB {value}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.PulseSetupB = value;
                    Log($"Pulse setup B set to: {value}%");
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

        /// <summary>
        /// Set delay setup parameters (:MEASure:SETup:DSA/DSB)
        /// </summary>
        public bool SetDelaySetup(double delayA, double delayB)
        {
            try
            {
                bool success = true;

                success &= oscilloscope.SendCommand($":MEASure:SETup:DSA {delayA}");
                success &= oscilloscope.SendCommand($":MEASure:SETup:DSB {delayB}");

                if (success)
                {
                    settings.DelaySetupA = delayA;
                    settings.DelaySetupB = delayB;
                    Log($"Delay setup set: DSA={delayA}%, DSB={delayB}%");
                }

                return success;
            }
            catch (Exception ex)
            {
                Log($"Error setting delay setup: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get measurement value by key
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <returns>The measurement value or null if not found</returns>
        public double? GetMeasurementValue(string measurementKey)
        {
            if (currentMeasurementValues.TryGetValue(measurementKey, out object value) && value is double doubleValue)
            {
                return doubleValue;
            }
            return null;
        }

        /// <summary>
        /// Get measurement statistics by key
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <returns>The measurement statistics or null if not found</returns>
        public MeasurementStatistics GetMeasurementStatistics(string measurementKey)
        {
            return measurementStatistics.TryGetValue(measurementKey, out MeasurementStatistics stats) ? stats : null;
        }

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