using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;
using DS1000Z_E_USB_Control.TimeBase;
using DS1000Z_E_USB_Control.Trigger;
using Rigol_DS1000Z_E_Control;
using System;
using System.Globalization;

namespace DS1000Z_E_USB_Control
{
    /// <summary>
    /// Comprehensive settings manager for reading and updating all oscilloscope settings
    /// </summary>
    public class OscilloscopeSettingsManager
    {
        private readonly RigolDS1000ZE oscilloscope;

        public event EventHandler<string> LogEvent;

        #region Current Settings Properties

        public Ch1Settings Channel1Settings { get; private set; } = new Ch1Settings();
        public Ch2Settings Channel2Settings { get; private set; } = new Ch2Settings();
        public TimeBaseSettings TimeBaseSettings { get; private set; } = new TimeBaseSettings();
        public TriggerSettings TriggerSettings { get; private set; } = new TriggerSettings();

        #endregion

        public OscilloscopeSettingsManager(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope;
        }

        /// <summary>
        /// Read all current settings from the oscilloscope
        /// </summary>
        public bool ReadAllCurrentSettings()
        {
            if (!oscilloscope.IsConnected)
            {
                Log("Cannot read settings - oscilloscope not connected");
                return false;
            }

            Log("Reading all current oscilloscope settings...");

            try
            {
                bool success = true;

                // Read channel settings
                success &= ReadChannel1Settings();
                success &= ReadChannel2Settings();

                // Read timebase settings
                success &= ReadTimeBaseSettings();

                // Read trigger settings
                success &= ReadTriggerSettings();

                if (success)
                {
                    Log("Successfully read all oscilloscope settings");
                    LogCurrentSettings();
                }
                else
                {
                    Log("Some settings could not be read - check oscilloscope connection");
                }

                return success;
            }
            catch (Exception ex)
            {
                Log($"Error reading oscilloscope settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read Channel 1 settings from oscilloscope
        /// </summary>
        public bool ReadChannel1Settings()
        {
            try
            {
                Log("Reading Channel 1 settings...");

                // Read enable state
                string enableState = oscilloscope.SendQuery(":CHANnel1:DISPlay?");
                if (!string.IsNullOrEmpty(enableState))
                {
                    Channel1Settings.IsEnabled = enableState.Trim() == "1";
                }

                // Read probe ratio
                string probeRatio = oscilloscope.SendQuery(":CHANnel1:PROBe?");
                if (!string.IsNullOrEmpty(probeRatio) &&
                    double.TryParse(probeRatio, NumberStyles.Float, CultureInfo.InvariantCulture, out double probe))
                {
                    Channel1Settings.ProbeRatio = probe;
                }

                // Read vertical scale
                string verticalScale = oscilloscope.SendQuery(":CHANnel1:SCALe?");
                if (!string.IsNullOrEmpty(verticalScale) &&
                    double.TryParse(verticalScale, NumberStyles.Float, CultureInfo.InvariantCulture, out double scale))
                {
                    Channel1Settings.VerticalScale = scale;
                }

                // Read vertical offset
                string verticalOffset = oscilloscope.SendQuery(":CHANnel1:OFFSet?");
                if (!string.IsNullOrEmpty(verticalOffset) &&
                    double.TryParse(verticalOffset, NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
                {
                    Channel1Settings.VerticalOffset = offset;
                }

                // Read coupling
                string coupling = oscilloscope.SendQuery(":CHANnel1:COUPling?");
                if (!string.IsNullOrEmpty(coupling))
                {
                    Channel1Settings.Coupling = coupling.Trim();
                }

                // Read bandwidth limit
                string bwLimit = oscilloscope.SendQuery(":CHANnel1:BWLimit?");
                if (!string.IsNullOrEmpty(bwLimit))
                {
                    Channel1Settings.BandwidthLimit = bwLimit.Trim() == "20M" ? "20M" : "OFF";
                }

                // Read units
                string units = oscilloscope.SendQuery(":CHANnel1:UNITs?");
                if (!string.IsNullOrEmpty(units))
                {
                    Channel1Settings.Units = units.Trim();
                }

                // Read invert state
                string invert = oscilloscope.SendQuery(":CHANnel1:INVert?");
                if (!string.IsNullOrEmpty(invert))
                {
                    Channel1Settings.InvertEnabled = invert.Trim() == "1";
                }

                // Read vernier state
                string vernier = oscilloscope.SendQuery(":CHANnel1:VERNier?");
                if (!string.IsNullOrEmpty(vernier))
                {
                    Channel1Settings.VernierEnabled = vernier.Trim() == "1";
                }

                Log($"Channel 1: {Channel1Settings}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error reading Channel 1 settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read Channel 2 settings from oscilloscope
        /// </summary>
        public bool ReadChannel2Settings()
        {
            try
            {
                Log("Reading Channel 2 settings...");

                // Read enable state
                string enableState = oscilloscope.SendQuery(":CHANnel2:DISPlay?");
                if (!string.IsNullOrEmpty(enableState))
                {
                    Channel2Settings.IsEnabled = enableState.Trim() == "1";
                }

                // Read probe ratio
                string probeRatio = oscilloscope.SendQuery(":CHANnel2:PROBe?");
                if (!string.IsNullOrEmpty(probeRatio) &&
                    double.TryParse(probeRatio, NumberStyles.Float, CultureInfo.InvariantCulture, out double probe))
                {
                    Channel2Settings.ProbeRatio = probe;
                }

                // Read vertical scale
                string verticalScale = oscilloscope.SendQuery(":CHANnel2:SCALe?");
                if (!string.IsNullOrEmpty(verticalScale) &&
                    double.TryParse(verticalScale, NumberStyles.Float, CultureInfo.InvariantCulture, out double scale))
                {
                    Channel2Settings.VerticalScale = scale;
                }

                // Read vertical offset
                string verticalOffset = oscilloscope.SendQuery(":CHANnel2:OFFSet?");
                if (!string.IsNullOrEmpty(verticalOffset) &&
                    double.TryParse(verticalOffset, NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
                {
                    Channel2Settings.VerticalOffset = offset;
                }

                // Read coupling
                string coupling = oscilloscope.SendQuery(":CHANnel2:COUPling?");
                if (!string.IsNullOrEmpty(coupling))
                {
                    Channel2Settings.Coupling = coupling.Trim();
                }

                // Read bandwidth limit
                string bwLimit = oscilloscope.SendQuery(":CHANnel2:BWLimit?");
                if (!string.IsNullOrEmpty(bwLimit))
                {
                    Channel2Settings.BandwidthLimit = bwLimit.Trim() == "20M" ? "20M" : "OFF";
                }

                // Read units
                string units = oscilloscope.SendQuery(":CHANnel2:UNITs?");
                if (!string.IsNullOrEmpty(units))
                {
                    Channel2Settings.Units = units.Trim();
                }

                // Read invert state
                string invert = oscilloscope.SendQuery(":CHANnel2:INVert?");
                if (!string.IsNullOrEmpty(invert))
                {
                    Channel2Settings.InvertEnabled = invert.Trim() == "1";
                }

                // Read vernier state
                string vernier = oscilloscope.SendQuery(":CHANnel2:VERNier?");
                if (!string.IsNullOrEmpty(vernier))
                {
                    Channel2Settings.VernierEnabled = vernier.Trim() == "1";
                }

                Log($"Channel 2: {Channel2Settings}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error reading Channel 2 settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read TimeBase settings from oscilloscope
        /// </summary>
        public bool ReadTimeBaseSettings()
        {
            try
            {
                Log("Reading TimeBase settings...");

                // Read timebase mode
                string mode = oscilloscope.SendQuery(":TIMebase:MODE?");
                if (!string.IsNullOrEmpty(mode))
                {
                    TimeBaseSettings.Mode = mode.Trim();
                }

                // Read main horizontal scale
                string mainScale = oscilloscope.SendQuery(":TIMebase:SCALe?");
                if (!string.IsNullOrEmpty(mainScale) &&
                    double.TryParse(mainScale, NumberStyles.Float, CultureInfo.InvariantCulture, out double scale))
                {
                    TimeBaseSettings.MainScale = scale;
                }

                // Read main horizontal offset
                string mainOffset = oscilloscope.SendQuery(":TIMebase:OFFSet?");
                if (!string.IsNullOrEmpty(mainOffset) &&
                    double.TryParse(mainOffset, NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
                {
                    TimeBaseSettings.MainOffset = offset;
                }

                // Read delay enable state
                string delayEnable = oscilloscope.SendQuery(":TIMebase:DELay:ENABle?");
                if (!string.IsNullOrEmpty(delayEnable))
                {
                    TimeBaseSettings.DelayEnabled = delayEnable.Trim() == "1";
                }

                // If delay is enabled, read delay settings
                if (TimeBaseSettings.DelayEnabled)
                {
                    // Read delay scale
                    string delayScale = oscilloscope.SendQuery(":TIMebase:DELay:SCALe?");
                    if (!string.IsNullOrEmpty(delayScale) &&
                        double.TryParse(delayScale, NumberStyles.Float, CultureInfo.InvariantCulture, out double dScale))
                    {
                        TimeBaseSettings.DelayScale = dScale;
                    }

                    // Read delay offset
                    string delayOffset = oscilloscope.SendQuery(":TIMebase:DELay:OFFSet?");
                    if (!string.IsNullOrEmpty(delayOffset) &&
                        double.TryParse(delayOffset, NumberStyles.Float, CultureInfo.InvariantCulture, out double dOffset))
                    {
                        TimeBaseSettings.DelayOffset = dOffset;
                    }
                }

                Log($"TimeBase: {TimeBaseSettings}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error reading TimeBase settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read Trigger settings from oscilloscope
        /// </summary>
        public bool ReadTriggerSettings()
        {
            try
            {
                Log("Reading Trigger settings...");

                // Read trigger mode
                string mode = oscilloscope.SendQuery(":TRIGger:MODE?");
                if (!string.IsNullOrEmpty(mode))
                {
                    TriggerSettings.Mode = mode.Trim();
                }

                // Read trigger coupling
                string coupling = oscilloscope.SendQuery(":TRIGger:COUPling?");
                if (!string.IsNullOrEmpty(coupling))
                {
                    TriggerSettings.Coupling = coupling.Trim();
                }

                // Read trigger sweep mode
                string sweep = oscilloscope.SendQuery(":TRIGger:SWEep?");
                if (!string.IsNullOrEmpty(sweep))
                {
                    TriggerSettings.Sweep = sweep.Trim();
                }

                // Read trigger status
                string status = oscilloscope.SendQuery(":TRIGger:STATus?");
                if (!string.IsNullOrEmpty(status))
                {
                    TriggerSettings.Status = status.Trim();
                }

                // Read trigger holdoff
                string holdoff = oscilloscope.SendQuery(":TRIGger:HOLDoff?");
                if (!string.IsNullOrEmpty(holdoff) &&
                    double.TryParse(holdoff, NumberStyles.Float, CultureInfo.InvariantCulture, out double holdoffVal))
                {
                    TriggerSettings.Holdoff = holdoffVal;
                }

                // Read noise reject
                string noisereject = oscilloscope.SendQuery(":TRIGger:NREJect?");
                if (!string.IsNullOrEmpty(noisereject))
                {
                    TriggerSettings.NoiseReject = noisereject.Trim() == "1";
                }

                // For edge trigger mode, read edge-specific settings
                if (TriggerSettings.Mode.ToUpper() == "EDGE")
                {
                    // Read edge trigger source
                    string edgeSource = oscilloscope.SendQuery(":TRIGger:EDGe:SOURce?");
                    if (!string.IsNullOrEmpty(edgeSource))
                    {
                        TriggerSettings.EdgeSource = edgeSource.Trim();
                    }

                    // Read edge trigger slope
                    string edgeSlope = oscilloscope.SendQuery(":TRIGger:EDGe:SLOPe?");
                    if (!string.IsNullOrEmpty(edgeSlope))
                    {
                        TriggerSettings.EdgeSlope = edgeSlope.Trim();
                    }

                    // Read edge trigger level
                    string edgeLevel = oscilloscope.SendQuery(":TRIGger:EDGe:LEVel?");
                    if (!string.IsNullOrEmpty(edgeLevel) &&
                        double.TryParse(edgeLevel, NumberStyles.Float, CultureInfo.InvariantCulture, out double level))
                    {
                        TriggerSettings.EdgeLevel = level;
                    }
                }

                Log($"Trigger: {TriggerSettings}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error reading Trigger settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Log all current settings in a formatted way
        /// </summary>
        public void LogCurrentSettings()
        {
            Log("=== CURRENT OSCILLOSCOPE SETTINGS ===");
            Log($"Device ID: {GetDeviceID()}");
            Log("");
            Log("CHANNELS:");
            Log($"  {Channel1Settings}");
            Log($"  {Channel2Settings}");
            Log("");
            Log("TIMEBASE:");
            Log($"  {TimeBaseSettings}");
            Log("");
            Log("TRIGGER:");
            Log($"  {TriggerSettings}");
            Log("=====================================");
        }

        /// <summary>
        /// Get device identification
        /// </summary>
        public string GetDeviceID()
        {
            try
            {
                return oscilloscope.SendQuery("*IDN?") ?? "Unknown Device";
            }
            catch
            {
                return "Unknown Device";
            }
        }

        /// <summary>
        /// Get acquisition information
        /// </summary>
        public string GetAcquisitionInfo()
        {
            try
            {
                string sampleRate = oscilloscope.SendQuery(":ACQuire:SRATe?");
                string memoryDepth = oscilloscope.SendQuery(":ACQuire:MDEPth?");
                string acqType = oscilloscope.SendQuery(":ACQuire:TYPE?");

                return $"Sample Rate: {sampleRate}, Memory Depth: {memoryDepth}, Type: {acqType}";
            }
            catch (Exception ex)
            {
                Log($"Error reading acquisition info: {ex.Message}");
                return "Acquisition info unavailable";
            }
        }

        /// <summary>
        /// Export all settings to a formatted string
        /// </summary>
        public string ExportSettingsToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Oscilloscope Settings Export - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Device: {GetDeviceID()}");
            sb.AppendLine($"Acquisition: {GetAcquisitionInfo()}");
            sb.AppendLine();

            sb.AppendLine("CHANNEL 1:");
            sb.AppendLine($"  Enabled: {Channel1Settings.IsEnabled}");
            sb.AppendLine($"  Probe Ratio: {Channel1Settings.ProbeRatio}×");
            sb.AppendLine($"  Vertical Scale: {Channel1Settings.VerticalScale} V/div");
            sb.AppendLine($"  Vertical Offset: {Channel1Settings.VerticalOffset} V");
            sb.AppendLine($"  Coupling: {Channel1Settings.Coupling}");
            sb.AppendLine($"  Bandwidth Limit: {Channel1Settings.BandwidthLimit}");
            sb.AppendLine($"  Units: {Channel1Settings.Units}");
            sb.AppendLine($"  Invert: {Channel1Settings.InvertEnabled}");
            sb.AppendLine($"  Vernier: {Channel1Settings.VernierEnabled}");
            sb.AppendLine();

            sb.AppendLine("CHANNEL 2:");
            sb.AppendLine($"  Enabled: {Channel2Settings.IsEnabled}");
            sb.AppendLine($"  Probe Ratio: {Channel2Settings.ProbeRatio}×");
            sb.AppendLine($"  Vertical Scale: {Channel2Settings.VerticalScale} V/div");
            sb.AppendLine($"  Vertical Offset: {Channel2Settings.VerticalOffset} V");
            sb.AppendLine($"  Coupling: {Channel2Settings.Coupling}");
            sb.AppendLine($"  Bandwidth Limit: {Channel2Settings.BandwidthLimit}");
            sb.AppendLine($"  Units: {Channel2Settings.Units}");
            sb.AppendLine($"  Invert: {Channel2Settings.InvertEnabled}");
            sb.AppendLine($"  Vernier: {Channel2Settings.VernierEnabled}");
            sb.AppendLine();

            sb.AppendLine("TIMEBASE:");
            sb.AppendLine($"  Mode: {TimeBaseSettings.Mode}");
            sb.AppendLine($"  Main Scale: {TimeBaseSettings.MainScaleDisplay}");
            sb.AppendLine($"  Main Offset: {TimeBaseSettings.MainOffset:E3} s");
            sb.AppendLine($"  Time Window: {TimeBaseSettings.TimeWindow:E3} s");
            sb.AppendLine($"  Delay Enabled: {TimeBaseSettings.DelayEnabled}");
            if (TimeBaseSettings.DelayEnabled)
            {
                sb.AppendLine($"  Delay Scale: {TimeBaseSettings.DelayScaleDisplay}");
                sb.AppendLine($"  Delay Offset: {TimeBaseSettings.DelayOffset:E3} s");
            }
            sb.AppendLine();

            sb.AppendLine("TRIGGER:");
            sb.AppendLine($"  Mode: {TriggerSettings.Mode}");
            sb.AppendLine($"  Coupling: {TriggerSettings.Coupling}");
            sb.AppendLine($"  Sweep: {TriggerSettings.Sweep}");
            sb.AppendLine($"  Status: {TriggerSettings.Status}");
            sb.AppendLine($"  Holdoff: {TriggerSettings.HoldoffDisplay}");
            sb.AppendLine($"  Noise Reject: {TriggerSettings.NoiseReject}");
            sb.AppendLine($"  Position: {TriggerSettings.Position}%");
            if (TriggerSettings.Mode.ToUpper() == "EDGE")
            {
                sb.AppendLine($"  Edge Source: {TriggerSettings.EdgeSource}");
                sb.AppendLine($"  Edge Slope: {TriggerSettings.EdgeSlope}");
                sb.AppendLine($"  Edge Level: {TriggerSettings.EdgeLevelDisplay}");
            }

            return sb.ToString();
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}