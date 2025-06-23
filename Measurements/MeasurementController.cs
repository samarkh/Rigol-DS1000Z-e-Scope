using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Rigol_DS1000Z_E_Control;

namespace DS1000Z_E_USB_Control.Measurements
{
    /// <summary>
    /// Controller for handling measurement operations and SCPI communication
    /// Implements Rigol DS1000Z-E measurement commands
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

        public MeasurementController(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));
            this.settings = new MeasurementSettings();
            this.currentMeasurementValues = new Dictionary<string, object>();
            this.measurementStatistics = new Dictionary<string, MeasurementStatistics>();
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
        /// Current measurement values
        /// </summary>
        public Dictionary<string, object> CurrentValues => new Dictionary<string, object>(currentMeasurementValues);

        /// <summary>
        /// Current measurement statistics
        /// </summary>
        public Dictionary<string, MeasurementStatistics> Statistics =>
            new Dictionary<string, MeasurementStatistics>(measurementStatistics);

        #endregion

        #region Basic Measurement Control

        /// <summary>
        /// Set automatic measurement display (:MEASure:ADISplay)
        /// </summary>
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
        /// Set automatic measurement source (:MEASure:AMSource)
        /// </summary>
        public bool SetAutoMeasureSource(string source)
        {
            try
            {
                string command = $":MEASure:AMSource {source}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.AutoMeasureSource = source;
                    Log($"Auto measure source set to: {source}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error setting auto measure source: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply measurement settings to the oscilloscope
        /// </summary>
        public bool ApplySettings(MeasurementSettings newSettings = null)
        {
            try
            {
                if (newSettings != null)
                    Settings = newSettings;

                // Apply auto display setting
                if (!SetAutoDisplay(Settings.AutoDisplayEnabled))
                    return false;

                // Apply auto measure source
                if (!SetAutoMeasureSource(Settings.AutoMeasureSource))
                    return false;

                // Apply threshold settings
                if (!SetThresholdLevels(Settings.ThresholdMax, Settings.ThresholdMid, Settings.ThresholdMin))
                    return false;

                // Apply pulse and delay setup
                if (!SetPulseSetupB(Settings.PulseSetupB))
                    return false;

                if (!SetDelaySetupA(Settings.DelaySetupA))
                    return false;

                if (!SetDelaySetupB(Settings.DelaySetupB))
                    return false;

                // Apply statistics settings
                if (!SetStatisticsMode(Settings.StatisticMode))
                    return false;

                if (!SetStatisticsDisplay(Settings.StatisticDisplayEnabled))
                    return false;

                // Enable measurements
                foreach (var measurement in Settings.EnabledMeasurements)
                {
                    EnableMeasurement(measurement);
                }

                Log("Settings applied successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error applying settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reset all measurements and clear statistics
        /// </summary>
        public bool ResetAllMeasurements()
        {
            try
            {
                // Clear all measurements using SCPI command
                if (oscilloscope.SendCommand(":MEASure:CLEar ALL"))
                {
                    currentMeasurementValues.Clear();
                    measurementStatistics.Clear();
                    Settings.EnabledMeasurements.Clear();

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

        /// <summary>
        /// Update all enabled measurements
        /// </summary>
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

        #endregion

        #region Individual Measurement Operations

        /// <summary>
        /// Enable a specific measurement
        /// </summary>
        public bool EnableMeasurement(string measurementKey)
        {
            try
            {
                string command = $":MEASure:ITEM {measurementKey},{Settings.AutoMeasureSource}";
                if (oscilloscope.SendCommand(command))
                {
                    if (!Settings.EnabledMeasurements.Contains(measurementKey))
                    {
                        Settings.EnabledMeasurements.Add(measurementKey);
                    }
                    Log($"Measurement enabled: {measurementKey}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error enabling measurement {measurementKey}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disable a specific measurement
        /// </summary>
        public bool DisableMeasurement(string measurementKey)
        {
            try
            {
                string command = $":MEASure:ITEM {measurementKey},OFF";
                if (oscilloscope.SendCommand(command))
                {
                    Settings.EnabledMeasurements.Remove(measurementKey);
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

        /// <summary>
        /// Update a single measurement
        /// </summary>
        private bool UpdateSingleMeasurement(string measurementKey)
        {
            try
            {
                // Query the specific measurement from oscilloscope
                string query = $":MEASure:ITEM? {measurementKey}";
                string response = oscilloscope.QueryCommand(query);

                if (!string.IsNullOrEmpty(response) &&
                    double.TryParse(response.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    currentMeasurementValues[measurementKey] = value;
                    UpdateMeasurementStatistics(measurementKey, value);

                    // Raise event
                    MeasurementValueUpdated?.Invoke(this, new MeasurementValueEventArgs(measurementKey, value));

                    return true;
                }

                Log($"Invalid response for {measurementKey}: {response}");
                return false;
            }
            catch (Exception ex)
            {
                Log($"Error updating measurement {measurementKey}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Statistics Control

        /// <summary>
        /// Set statistics mode (:MEASure:STATistic:MODE)
        /// </summary>
        public bool SetStatisticsMode(string mode)
        {
            try
            {
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
        /// Set statistics display (:MEASure:STATistic:DISPlay)
        /// </summary>
        public bool SetStatisticsDisplay(bool enabled)
        {
            try
            {
                string command = $":MEASure:STATistic:DISPlay {(enabled ? "ON" : "OFF")}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.StatisticDisplayEnabled = enabled;
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
        /// Reset statistics
        /// </summary>
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

        /// <summary>
        /// Update statistics for all measurements
        /// </summary>
        public bool UpdateStatistics()
        {
            try
            {
                // Query statistics from oscilloscope for each enabled measurement
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
        /// Update statistics for a measurement from the device
        /// </summary>
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
                        Count = 1 // Device doesn't provide count, use 1 as placeholder
                    };

                    measurementStatistics[measurementKey] = stats;

                    // Raise statistics updated event
                    MeasurementStatisticsUpdated?.Invoke(this,
                        new MeasurementStatisticsEventArgs(measurementKey, stats));
                }
            }
            catch (Exception ex)
            {
                Log($"Error updating statistics for {measurementKey}: {ex.Message}");
            }
        }

        /// <summary>
        /// Query a specific statistic from the device
        /// </summary>
        private double? QueryMeasurementStatistic(string measurementKey, string statisticType)
        {
            try
            {
                string query = $":MEASure:STATistic:ITEM? {statisticType},{measurementKey}";
                string response = oscilloscope.QueryCommand(query);

                if (!string.IsNullOrEmpty(response) &&
                    double.TryParse(response.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                Log($"Error querying {statisticType} for {measurementKey}: {ex.Message}");
            }

            return null;
        }

        #endregion

        #region Threshold and Setup Controls

        /// <summary>
        /// Set threshold levels (:MEASure:SETup:MAX/MID/MIN)
        /// </summary>
        public bool SetThresholdLevels(double max, double mid, double min)
        {
            try
            {
                bool success = true;
                success &= SetThresholdMax(max);
                success &= SetThresholdMid(mid);
                success &= SetThresholdMin(min);

                if (success)
                {
                    Log($"Threshold levels set - Max: {max}%, Mid: {mid}%, Min: {min}%");
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
        /// Set maximum threshold (:MEASure:SETup:MAX)
        /// </summary>
        public bool SetThresholdMax(double value)
        {
            try
            {
                string command = $":MEASure:SETup:MAX {value}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.ThresholdMax = value;
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
        /// Set middle threshold (:MEASure:SETup:MID)
        /// </summary>
        public bool SetThresholdMid(double value)
        {
            try
            {
                string command = $":MEASure:SETup:MID {value}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.ThresholdMid = value;
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
        /// Set minimum threshold (:MEASure:SETup:MIN)
        /// </summary>
        public bool SetThresholdMin(double value)
        {
            try
            {
                string command = $":MEASure:SETup:MIN {value}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.ThresholdMin = value;
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
        /// Set delay setup parameter A (:MEASure:SETup:DSA)
        /// </summary>
        public bool SetDelaySetupA(double value)
        {
            try
            {
                string command = $":MEASure:SETup:DSA {value}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.DelaySetupA = value;
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
        /// Set delay setup parameter B (:MEASure:SETup:DSB)
        /// </summary>
        public bool SetDelaySetupB(double value)
        {
            try
            {
                string command = $":MEASure:SETup:DSB {value}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.DelaySetupB = value;
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

        #endregion

        #region Helper Methods

        /// <summary>
        /// Update statistics for a measurement (local calculation)
        /// </summary>
        private void UpdateMeasurementStatistics(string measurementKey, double value)
        {
            if (!measurementStatistics.ContainsKey(measurementKey))
            {
                measurementStatistics[measurementKey] = new MeasurementStatistics
                {
                    MeasurementKey = measurementKey,
                    Current = value,
                    Minimum = value,
                    Maximum = value,
                    Average = value,
                    Count = 1,
                    StandardDeviation = 0
                };
            }
            else
            {
                var stats = measurementStatistics[measurementKey];
                stats.Count++;
                stats.Current = value;

                // Update min/max
                stats.Minimum = Math.Min(stats.Minimum, value);
                stats.Maximum = Math.Max(stats.Maximum, value);

                // Update running average
                stats.Average = ((stats.Average * (stats.Count - 1)) + value) / stats.Count;

                // Simple standard deviation calculation
                // Note: For proper statistics, you'd want to store all values or use a more sophisticated algorithm
                double variance = Math.Pow(value - stats.Average, 2);
                stats.StandardDeviation = Math.Sqrt(variance);
            }

            // Raise statistics updated event
            MeasurementStatisticsUpdated?.Invoke(this,
                new MeasurementStatisticsEventArgs(measurementKey, measurementStatistics[measurementKey]));
        }

        /// <summary>
        /// Get measurement value by key
        /// </summary>
        public object GetMeasurementValue(string measurementKey)
        {
            return currentMeasurementValues.TryGetValue(measurementKey, out object value) ? value : null;
        }

        /// <summary>
        /// Get measurement statistics by key
        /// </summary>
        public MeasurementStatistics GetMeasurementStatistics(string measurementKey)
        {
            return measurementStatistics.TryGetValue(measurementKey, out MeasurementStatistics stats) ? stats : null;
        }

        /// <summary>
        /// Log a message
        /// </summary>
        private void Log(string message)
        {
            LogEvent?.Invoke(this, $"[Measurements] {message}");
        }

        #endregion
    }

    #region Event Args Classes

    /// <summary>
    /// Event arguments for measurement value updates
    /// </summary>
    public class MeasurementValueEventArgs : EventArgs
    {
        public string MeasurementKey { get; }
        public double Value { get; }

        public MeasurementValueEventArgs(string measurementKey, double value)
        {
            MeasurementKey = measurementKey;
            Value = value;
        }
    }

    /// <summary>
    /// Event arguments for measurement statistics updates
    /// </summary>
    public class MeasurementStatisticsEventArgs : EventArgs
    {
        public string MeasurementKey { get; }
        public MeasurementStatistics Statistics { get; }

        public MeasurementStatisticsEventArgs(string measurementKey, MeasurementStatistics statistics)
        {
            MeasurementKey = measurementKey;
            Statistics = statistics;
        }
    }

    #endregion

    #region Statistics Class

    /// <summary>
    /// Represents measurement statistics
    /// </summary>
    public class MeasurementStatistics
    {
        public string MeasurementKey { get; set; }
        public double Current { get; set; }
        public double Average { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double StandardDeviation { get; set; }
        public int Count { get; set; }

        // Add properties for backward compatibility with existing code
        public double Min => Minimum;
        public double Max => Maximum;

        public override string ToString()
        {
            return $"Avg: {Average:F3}, Min: {Minimum:F3}, Max: {Maximum:F3}, StdDev: {StandardDeviation:F3} ({Count} samples)";
        }
    }

    #endregion
}