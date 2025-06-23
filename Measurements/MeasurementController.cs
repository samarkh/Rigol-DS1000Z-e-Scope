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
            }
            catch (Exception ex)
            {
                Log($"Error setting auto display: {ex.Message}");
            }
            return false;
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
            }
            catch (Exception ex)
            {
                Log($"Error setting auto measure source: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Enable a measurement item (:MEASure:ITEM)
        /// </summary>
        public bool EnableMeasurement(string measurementKey, string source = null)
        {
            try
            {
                source = source ?? settings.AutoMeasureSource;
                string command = $":MEASure:ITEM {measurementKey},{source}";

                if (oscilloscope.SendCommand(command))
                {
                    if (!settings.EnabledMeasurements.Contains(measurementKey))
                    {
                        settings.EnabledMeasurements.Add(measurementKey);
                    }
                    Log($"Measurement {measurementKey} enabled for {source}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"Error enabling measurement {measurementKey}: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Disable a measurement item
        /// </summary>
        public bool DisableMeasurement(string measurementKey)
        {
            try
            {
                settings.EnabledMeasurements.Remove(measurementKey);
                Log($"Measurement {measurementKey} disabled");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error disabling measurement {measurementKey}: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Clear all measurements (:MEASure:CLEar)
        /// </summary>
        public bool ClearAllMeasurements()
        {
            try
            {
                if (oscilloscope.SendCommand(":MEASure:CLEar"))
                {
                    settings.EnabledMeasurements.Clear();
                    currentMeasurementValues.Clear();
                    measurementStatistics.Clear();
                    Log("All measurements cleared");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"Error clearing measurements: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Recover measurements (:MEASure:RECover)
        /// </summary>
        public bool RecoverMeasurements()
        {
            try
            {
                if (oscilloscope.SendCommand(":MEASure:RECover"))
                {
                    Log("Measurements recovered");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"Error recovering measurements: {ex.Message}");
            }
            return false;
        }

        #endregion

        #region Measurement Setup and Thresholds

        /// <summary>
        /// Set measurement setup maximum threshold (:MEASure:SETup:MAX)
        /// </summary>
        public bool SetThresholdMax(double percent)
        {
            try
            {
                string command = $":MEASure:SETup:MAX {percent:F1}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.ThresholdMax = percent;
                    Log($"Threshold MAX set to: {percent:F1}%");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting threshold MAX: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Set measurement setup middle threshold (:MEASure:SETup:MID)
        /// FIXED: Complete implementation
        /// </summary>
        public bool SetThresholdMid(double percent)
        {
            try
            {
                string command = $":MEASure:SETup:MID {percent:F1}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.ThresholdMid = percent;
                    Log($"Threshold MID set to: {percent:F1}%");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting threshold MID: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Set measurement setup minimum threshold (:MEASure:SETup:MIN)
        /// </summary>
        public bool SetThresholdMin(double percent)
        {
            try
            {
                string command = $":MEASure:SETup:MIN {percent:F1}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.ThresholdMin = percent;
                    Log($"Threshold MIN set to: {percent:F1}%");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting threshold MIN: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Set pulse setup parameter B (:MEASure:SETup:PSB)
        /// FIXED: Complete error message
        /// </summary>
        public bool SetPulseSetupB(double percent)
        {
            try
            {
                string command = $":MEASure:SETup:PSB {percent:F1}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.PulseSetupB = percent;
                    Log($"Pulse setup B set to: {percent:F1}%");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting pulse setup B: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Set delay setup parameter A (:MEASure:SETup:DSA)
        /// </summary>
        public bool SetDelaySetupA(double percent)
        {
            try
            {
                string command = $":MEASure:SETup:DSA {percent:F1}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.DelaySetupA = percent;
                    Log($"Delay setup A set to: {percent:F1}%");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting delay setup A: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Set delay setup parameter B (:MEASure:SETup:DSB)
        /// </summary>
        public bool SetDelaySetupB(double percent)
        {
            try
            {
                string command = $":MEASure:SETup:DSB {percent:F1}";
                if (oscilloscope.SendCommand(command))
                {
                    settings.DelaySetupB = percent;
                    Log($"Delay setup B set to: {percent:F1}%");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting delay setup B: {ex.Message}");
            }
            return false;
        }

        #endregion

        #region Statistics Control

        /// <summary>
        /// Set statistics display (:MEASure:STATistic:DISPlay)
        /// </summary>
        public bool SetStatisticDisplay(bool enabled)
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
            }
            catch (Exception ex)
            {
                Log($"Error setting statistics display: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Set statistics mode (:MEASure:STATistic:MODE)
        /// </summary>
        public bool SetStatisticMode(string mode)
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
            }
            catch (Exception ex)
            {
                Log($"Error setting statistics mode: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Reset statistics (:MEASure:STATistic:RESet)
        /// </summary>
        public bool ResetStatistics()
        {
            try
            {
                if (oscilloscope.SendCommand(":MEASure:STATistic:RESet"))
                {
                    measurementStatistics.Clear();
                    Log("Statistics reset");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"Error resetting statistics: {ex.Message}");
            }
            return false;
        }

        #endregion

        #region Measurement Value Queries

        /// <summary>
        /// Query a specific measurement value (:MEASure:ITEM?)
        /// </summary>
        public double? QueryMeasurementValue(string measurementKey, string source = null)
        {
            try
            {
                source = source ?? settings.AutoMeasureSource;
                string command = $":MEASure:ITEM? {measurementKey},{source}";
                string response = oscilloscope.SendQuery(command);

                if (!string.IsNullOrEmpty(response) && double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    currentMeasurementValues[measurementKey] = value;
                    MeasurementValueUpdated?.Invoke(this, new MeasurementValueEventArgs(measurementKey, value));
                    return value;
                }
            }
            catch (Exception ex)
            {
                Log($"Error querying measurement {measurementKey}: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Query statistics for a specific measurement (:MEASure:STATistic:ITEM?)
        /// </summary>
        public MeasurementStatistics QueryMeasurementStatistics(string measurementKey, string source = null)
        {
            try
            {
                source = source ?? settings.AutoMeasureSource;
                string command = $":MEASure:STATistic:ITEM? {measurementKey},{source}";
                string response = oscilloscope.SendQuery(command);

                if (!string.IsNullOrEmpty(response))
                {
                    var statistics = ParseStatisticsResponse(measurementKey, response);
                    if (statistics != null)
                    {
                        measurementStatistics[measurementKey] = statistics;
                        MeasurementStatisticsUpdated?.Invoke(this, new MeasurementStatisticsEventArgs(measurementKey, statistics));
                        return statistics;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error querying statistics for {measurementKey}: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Update all enabled measurement values
        /// </summary>
        public void UpdateAllMeasurementValues()
        {
            foreach (var measurementKey in settings.EnabledMeasurements)
            {
                QueryMeasurementValue(measurementKey);
            }
        }

        /// <summary>
        /// Update all enabled measurement statistics
        /// </summary>
        public void UpdateAllMeasurementStatistics()
        {
            if (!settings.StatisticDisplayEnabled) return;

            foreach (var measurementKey in settings.EnabledMeasurements)
            {
                QueryMeasurementStatistics(measurementKey);
            }
        }

        #endregion

        #region Preset Management

        /// <summary>
        /// Apply standard time domain measurements preset
        /// </summary>
        public bool ApplyTimeDomainPreset()
        {
            try
            {
                ClearAllMeasurements();

                var timeMeasurements = new[] { "FREQuency", "PERiod", "RTIMe", "FTIMe", "PDUTy" };
                bool success = true;

                foreach (var measurement in timeMeasurements)
                {
                    success &= EnableMeasurement(measurement);
                }

                if (success)
                {
                    Log("Applied time domain measurements preset");
                }

                return success;
            }
            catch (Exception ex)
            {
                Log($"Error applying time domain preset: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply standard voltage measurements preset
        /// </summary>
        public bool ApplyVoltagePreset()
        {
            try
            {
                ClearAllMeasurements();

                var voltageMeasurements = new[] { "VMAX", "VMIN", "VPP", "VAVG", "VRMS" };
                bool success = true;

                foreach (var measurement in voltageMeasurements)
                {
                    success &= EnableMeasurement(measurement);
                }

                if (success)
                {
                    Log("Applied voltage measurements preset");
                }

                return success;
            }
            catch (Exception ex)
            {
                Log($"Error applying voltage preset: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply comprehensive analysis preset
        /// </summary>
        public bool ApplyComprehensivePreset()
        {
            try
            {
                ClearAllMeasurements();

                var comprehensiveMeasurements = new[]
                {
                    "FREQuency", "PERiod", "VMAX", "VMIN", "VPP", "VAVG", "VRMS",
                    "RTIMe", "FTIMe", "PDUTy", "NDUTy", "VTOP", "VBASe"
                };
                bool success = true;

                foreach (var measurement in comprehensiveMeasurements)
                {
                    success &= EnableMeasurement(measurement);
                }

                if (success)
                {
                    Log("Applied comprehensive measurements preset");
                }

                return success;
            }
            catch (Exception ex)
            {
                Log($"Error applying comprehensive preset: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Parse statistics response from oscilloscope
        /// </summary>
        private MeasurementStatistics ParseStatisticsResponse(string measurementKey, string response)
        {
            try
            {
                // Expected format: "current,average,minimum,maximum,stddev,count"
                var parts = response.Split(',');
                if (parts.Length >= 6)
                {
                    return new MeasurementStatistics
                    {
                        MeasurementKey = measurementKey,
                        Current = double.Parse(parts[0], CultureInfo.InvariantCulture),
                        Average = double.Parse(parts[1], CultureInfo.InvariantCulture),
                        Minimum = double.Parse(parts[2], CultureInfo.InvariantCulture),
                        Maximum = double.Parse(parts[3], CultureInfo.InvariantCulture),
                        StandardDeviation = double.Parse(parts[4], CultureInfo.InvariantCulture),
                        Count = int.Parse(parts[5], CultureInfo.InvariantCulture)
                    };
                }
            }
            catch (Exception ex)
            {
                Log($"Error parsing statistics response: {ex.Message}");
            }
            return null;
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

        public override string ToString()
        {
            return $"Avg: {Average:F3}, Min: {Minimum:F3}, Max: {Maximum:F3}, StdDev: {StandardDeviation:F3} ({Count} samples)";
        }
    }

    #endregion
}