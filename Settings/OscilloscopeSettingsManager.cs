using System;
using System.Text;
using System.Globalization;
using Rigol_DS1000Z_E_Control;
using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;
using DS1000Z_E_USB_Control.Trigger;
using DS1000Z_E_USB_Control.TimeBase;
using DS1000Z_E_USB_Control.Measurements;

namespace DS1000Z_E_USB_Control
{
    /// <summary>
    /// Comprehensive settings manager for all oscilloscope subsystems
    /// </summary>
    public class OscilloscopeSettingsManager
    {
        private readonly RigolDS1000ZE oscilloscope;
        private string deviceId = "Unknown";
        private string acquisitionInfo = "Unknown";
                
        /// Measurement settings
        
        public MeasurementSettings MeasurementSettings { get; set; } = new MeasurementSettings();
        
        public event EventHandler<string> LogEvent;

        #region Settings Properties
        public Ch1Settings Channel1Settings { get; private set; }
        public Ch2Settings Channel2Settings { get; private set; }
        public TriggerSettings TriggerSettings { get; set; }
        public TimeBaseSettings TimeBaseSettings { get; private set; }
        #endregion

        public OscilloscopeSettingsManager(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope;

            // Initialize settings objects
            Channel1Settings = new Ch1Settings();
            Channel2Settings = new Ch2Settings();
            TriggerSettings = new TriggerSettings();
            TimeBaseSettings = new TimeBaseSettings();
        }

        #region Main Settings Operations

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

            bool allSuccess = true;

            try
            {
                Log("Reading all oscilloscope settings...");

                // Read device information
                ReadDeviceInformation();

                // Read Channel 1 settings
                if (!ReadChannel1Settings())
                {
                    Log("⚠️ Failed to read some Channel 1 settings");
                    allSuccess = false;
                }

                // Read Channel 2 settings
                if (!ReadChannel2Settings())
                {
                    Log("⚠️ Failed to read some Channel 2 settings");
                    allSuccess = false;
                }

                // Read Trigger settings
                if (!ReadTriggerSettings())
                {
                    Log("⚠️ Failed to read some Trigger settings");
                    allSuccess = false;
                }

                // Read TimeBase settings
                if (!ReadTimeBaseSettings())
                {
                    Log("⚠️ Failed to read some TimeBase settings");
                    allSuccess = false;
                }

                if (allSuccess)
                {
                    Log("✅ Successfully read all oscilloscope settings");
                }
                else
                {
                    Log("⚠️ Some settings could not be read");
                }

                return allSuccess;
            }
            catch (Exception ex)
            {
                Log($"❌ Error reading oscilloscope settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Export all settings to a formatted string
        /// </summary>
        public string ExportSettingsToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== Rigol DS1000Z-E Settings Export ===");
            sb.AppendLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Device: {deviceId}");
            sb.AppendLine($"Acquisition Info: {acquisitionInfo}");
            sb.AppendLine();

            // Channel 1 Settings
            sb.AppendLine("=== Channel 1 Settings ===");
            sb.AppendLine($"Enabled: {Channel1Settings.IsEnabled}");
            sb.AppendLine($"Probe Ratio: {Channel1Settings.ProbeRatio}:1");
            sb.AppendLine($"Vertical Scale: {Channel1Settings.VerticalScale}V/div ({Channel1Settings.VerticalScaleDisplay})");
            sb.AppendLine($"Vertical Offset: {Channel1Settings.VerticalOffset}V");
            sb.AppendLine($"Coupling: {Channel1Settings.Coupling}");
            sb.AppendLine($"Bandwidth Limit: {Channel1Settings.BandwidthLimit}");
            sb.AppendLine($"Units: {Channel1Settings.Units}");
            sb.AppendLine($"Invert: {Channel1Settings.InvertEnabled}");
            sb.AppendLine($"Vernier: {Channel1Settings.VernierEnabled}");
            sb.AppendLine();

            // Channel 2 Settings
            sb.AppendLine("=== Channel 2 Settings ===");
            sb.AppendLine($"Enabled: {Channel2Settings.IsEnabled}");
            sb.AppendLine($"Probe Ratio: {Channel2Settings.ProbeRatio}:1");
            sb.AppendLine($"Vertical Scale: {Channel2Settings.VerticalScale}V/div ({Channel2Settings.VerticalScaleDisplay})");
            sb.AppendLine($"Vertical Offset: {Channel2Settings.VerticalOffset}V");
            sb.AppendLine($"Coupling: {Channel2Settings.Coupling}");
            sb.AppendLine($"Bandwidth Limit: {Channel2Settings.BandwidthLimit}");
            sb.AppendLine($"Units: {Channel2Settings.Units}");
            sb.AppendLine($"Invert: {Channel2Settings.InvertEnabled}");
            sb.AppendLine($"Vernier: {Channel2Settings.VernierEnabled}");
            sb.AppendLine();

            // Trigger Settings
            sb.AppendLine("=== Trigger Settings ===");
            sb.AppendLine($"Mode: {TriggerSettings.Mode}");
            sb.AppendLine($"Coupling: {TriggerSettings.Coupling}");
            sb.AppendLine($"Sweep: {TriggerSettings.Sweep}");
            sb.AppendLine($"Status: {TriggerSettings.Status}");
            sb.AppendLine($"Edge Source: {TriggerSettings.EdgeSource}");
            sb.AppendLine($"Edge Slope: {TriggerSettings.EdgeSlope}");
            sb.AppendLine($"Edge Level: {TriggerSettings.EdgeLevel}V ({TriggerSettings.EdgeLevelDisplay})");
            sb.AppendLine($"Holdoff: {TriggerSettings.Holdoff}s ({TriggerSettings.HoldoffDisplay})");
            sb.AppendLine($"Noise Reject: {TriggerSettings.NoiseReject}");
            sb.AppendLine($"Position: {TriggerSettings.Position}%");
            sb.AppendLine();

            // TimeBase Settings
            sb.AppendLine("=== TimeBase Settings ===");
            sb.AppendLine($"Mode: {TimeBaseSettings.Mode}");
            sb.AppendLine($"Main Scale: {TimeBaseSettings.MainScale}s/div ({TimeBaseSettings.MainScaleDisplay})");
            sb.AppendLine($"Main Offset: {TimeBaseSettings.MainOffset}s");
            sb.AppendLine($"Time Window: {TimeBaseSettings.TimeWindow}s");
            sb.AppendLine($"Delay Enabled: {TimeBaseSettings.DelayEnabled}");
            sb.AppendLine($"Delay Scale: {TimeBaseSettings.DelayScale}s/div ({TimeBaseSettings.DelayScaleDisplay})");
            sb.AppendLine($"Delay Offset: {TimeBaseSettings.DelayOffset}s");
            sb.AppendLine();

            sb.AppendLine("=== End of Export ===");

            return sb.ToString();
        }

        #endregion

        #region Individual Settings Readers

        /// <summary>
        /// Read Channel 1 settings from oscilloscope
        /// </summary>
        private bool ReadChannel1Settings()
        {
            try
            {
                // Read all Channel 1 parameters
                string enabled = oscilloscope.SendQuery(":CHANnel1:DISPlay?");
                string probe = oscilloscope.SendQuery(":CHANnel1:PROBe?");
                string scale = oscilloscope.SendQuery(":CHANnel1:SCALe?");
                string offset = oscilloscope.SendQuery(":CHANnel1:OFFSet?");
                string coupling = oscilloscope.SendQuery(":CHANnel1:COUPling?");
                string bwLimit = oscilloscope.SendQuery(":CHANnel1:BWLimit?");
                string units = oscilloscope.SendQuery(":CHANnel1:UNITs?");
                string invert = oscilloscope.SendQuery(":CHANnel1:INVert?");
                string vernier = oscilloscope.SendQuery(":CHANnel1:VERNier?");

                // Parse and update settings
                if (!string.IsNullOrEmpty(enabled))
                    Channel1Settings.IsEnabled = enabled.Trim() == "1";

                if (!string.IsNullOrEmpty(probe) && double.TryParse(probe, NumberStyles.Float, CultureInfo.InvariantCulture, out double probeVal))
                    Channel1Settings.ProbeRatio = probeVal;

                if (!string.IsNullOrEmpty(scale) && double.TryParse(scale, NumberStyles.Float, CultureInfo.InvariantCulture, out double scaleVal))
                    Channel1Settings.VerticalScale = scaleVal;

                if (!string.IsNullOrEmpty(offset) && double.TryParse(offset, NumberStyles.Float, CultureInfo.InvariantCulture, out double offsetVal))
                    Channel1Settings.VerticalOffset = offsetVal;

                if (!string.IsNullOrEmpty(coupling))
                    Channel1Settings.Coupling = coupling.Trim();

                if (!string.IsNullOrEmpty(bwLimit))
                    Channel1Settings.BandwidthLimit = bwLimit.Trim();

                if (!string.IsNullOrEmpty(units))
                    Channel1Settings.Units = units.Trim();

                if (!string.IsNullOrEmpty(invert))
                    Channel1Settings.InvertEnabled = invert.Trim() == "1";

                if (!string.IsNullOrEmpty(vernier))
                    Channel1Settings.VernierEnabled = vernier.Trim() == "1";

                Log($"✅ Channel 1 settings read: {Channel1Settings}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"❌ Error reading Channel 1 settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read Channel 2 settings from oscilloscope
        /// </summary>
        private bool ReadChannel2Settings()
        {
            try
            {
                // Read all Channel 2 parameters
                string enabled = oscilloscope.SendQuery(":CHANnel2:DISPlay?");
                string probe = oscilloscope.SendQuery(":CHANnel2:PROBe?");
                string scale = oscilloscope.SendQuery(":CHANnel2:SCALe?");
                string offset = oscilloscope.SendQuery(":CHANnel2:OFFSet?");
                string coupling = oscilloscope.SendQuery(":CHANnel2:COUPling?");
                string bwLimit = oscilloscope.SendQuery(":CHANnel2:BWLimit?");
                string units = oscilloscope.SendQuery(":CHANnel2:UNITs?");
                string invert = oscilloscope.SendQuery(":CHANnel2:INVert?");
                string vernier = oscilloscope.SendQuery(":CHANnel2:VERNier?");

                // Parse and update settings
                if (!string.IsNullOrEmpty(enabled))
                    Channel2Settings.IsEnabled = enabled.Trim() == "1";

                if (!string.IsNullOrEmpty(probe) && double.TryParse(probe, NumberStyles.Float, CultureInfo.InvariantCulture, out double probeVal))
                    Channel2Settings.ProbeRatio = probeVal;

                if (!string.IsNullOrEmpty(scale) && double.TryParse(scale, NumberStyles.Float, CultureInfo.InvariantCulture, out double scaleVal))
                    Channel2Settings.VerticalScale = scaleVal;

                if (!string.IsNullOrEmpty(offset) && double.TryParse(offset, NumberStyles.Float, CultureInfo.InvariantCulture, out double offsetVal))
                    Channel2Settings.VerticalOffset = offsetVal;

                if (!string.IsNullOrEmpty(coupling))
                    Channel2Settings.Coupling = coupling.Trim();

                if (!string.IsNullOrEmpty(bwLimit))
                    Channel2Settings.BandwidthLimit = bwLimit.Trim();

                if (!string.IsNullOrEmpty(units))
                    Channel2Settings.Units = units.Trim();

                if (!string.IsNullOrEmpty(invert))
                    Channel2Settings.InvertEnabled = invert.Trim() == "1";

                if (!string.IsNullOrEmpty(vernier))
                    Channel2Settings.VernierEnabled = vernier.Trim() == "1";

                Log($"✅ Channel 2 settings read: {Channel2Settings}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"❌ Error reading Channel 2 settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read Trigger settings from oscilloscope
        /// </summary>
        private bool ReadTriggerSettings()
        {
            try
            {
                // Read trigger parameters
                string mode = oscilloscope.SendQuery(":TRIGger:MODE?");
                string coupling = oscilloscope.SendQuery(":TRIGger:COUPling?");
                string sweep = oscilloscope.SendQuery(":TRIGger:SWEep?");
                string status = oscilloscope.SendQuery(":TRIGger:STATus?");
                string holdoff = oscilloscope.SendQuery(":TRIGger:HOLDoff?");
                string noiseReject = oscilloscope.SendQuery(":TRIGger:NREJect?");

                // Parse basic settings
                if (!string.IsNullOrEmpty(mode))
                    TriggerSettings.Mode = mode.Trim();

                if (!string.IsNullOrEmpty(coupling))
                    TriggerSettings.Coupling = coupling.Trim();

                if (!string.IsNullOrEmpty(sweep))
                    TriggerSettings.Sweep = sweep.Trim();

                if (!string.IsNullOrEmpty(status))
                    TriggerSettings.Status = status.Trim();

                if (!string.IsNullOrEmpty(holdoff) && double.TryParse(holdoff, NumberStyles.Float, CultureInfo.InvariantCulture, out double holdoffVal))
                    TriggerSettings.Holdoff = holdoffVal;

                if (!string.IsNullOrEmpty(noiseReject))
                    TriggerSettings.NoiseReject = noiseReject.Trim() == "1" || noiseReject.Trim().ToUpper() == "ON";

                // For edge trigger, read edge-specific settings
                if (TriggerSettings.Mode.ToUpper() == "EDGE")
                {
                    string edgeSource = oscilloscope.SendQuery(":TRIGger:EDGe:SOURce?");
                    string edgeSlope = oscilloscope.SendQuery(":TRIGger:EDGe:SLOPe?");
                    string edgeLevel = oscilloscope.SendQuery(":TRIGger:EDGe:LEVel?");

                    if (!string.IsNullOrEmpty(edgeSource))
                        TriggerSettings.EdgeSource = edgeSource.Trim();

                    if (!string.IsNullOrEmpty(edgeSlope))
                        TriggerSettings.EdgeSlope = edgeSlope.Trim();

                    if (!string.IsNullOrEmpty(edgeLevel) && double.TryParse(edgeLevel, NumberStyles.Float, CultureInfo.InvariantCulture, out double levelVal))
                        TriggerSettings.EdgeLevel = levelVal;
                }

                Log($"🎯 Trigger settings read: {TriggerSettings}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"❌ Error reading Trigger settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read TimeBase settings from oscilloscope
        /// </summary>
        private bool ReadTimeBaseSettings()
        {
            try
            {
                // Read timebase parameters
                string mode = oscilloscope.SendQuery(":TIMebase:MODE?");
                string mainScale = oscilloscope.SendQuery(":TIMebase:SCALe?");
                string mainOffset = oscilloscope.SendQuery(":TIMebase:OFFSet?");

                // Parse settings
                if (!string.IsNullOrEmpty(mode))
                    TimeBaseSettings.Mode = mode.Trim();

                if (!string.IsNullOrEmpty(mainScale) && double.TryParse(mainScale, NumberStyles.Float, CultureInfo.InvariantCulture, out double scaleVal))
                    TimeBaseSettings.MainScale = scaleVal;

                if (!string.IsNullOrEmpty(mainOffset) && double.TryParse(mainOffset, NumberStyles.Float, CultureInfo.InvariantCulture, out double offsetVal))
                    TimeBaseSettings.MainOffset = offsetVal;

                Log($"📊 TimeBase settings read: {TimeBaseSettings}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"❌ Error reading TimeBase settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read device information
        /// </summary>
        private void ReadDeviceInformation()
        {
            try
            {
                // Read device ID
                string id = oscilloscope.SendQuery("*IDN?");
                if (!string.IsNullOrEmpty(id))
                {
                    deviceId = id.Trim();
                }

                // Read acquisition information
                string acqType = oscilloscope.SendQuery(":ACQuire:TYPE?");
                string sampleRate = oscilloscope.SendQuery(":ACQuire:SRATe?");
                string memDepth = oscilloscope.SendQuery(":ACQuire:MDEPth?");

                if (!string.IsNullOrEmpty(acqType) && !string.IsNullOrEmpty(sampleRate))
                {
                    acquisitionInfo = $"Type: {acqType.Trim()}, Rate: {sampleRate.Trim()}Sa/s";
                    if (!string.IsNullOrEmpty(memDepth))
                    {
                        acquisitionInfo += $", Depth: {memDepth.Trim()}";
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"⚠️ Error reading device information: {ex.Message}");
            }
        }

        #endregion

        #region Preset Application Methods

        /// <summary>
        /// Apply general purpose preset to all subsystems
        /// </summary>
        public bool ApplyGeneralPurposePreset()
        {
            if (!oscilloscope.IsConnected) return false;

            try
            {
                Log("Applying general purpose preset to all subsystems...");

                bool allSuccess = true;

                // Apply Channel 1 preset
                var ch1Preset = Ch1Settings.Presets.GeneralPurpose;
                if (!ApplyChannel1Settings(ch1Preset))
                    allSuccess = false;

                // Apply Channel 2 preset
                var ch2Preset = Ch2Settings.Presets.GeneralPurpose;
                if (!ApplyChannel2Settings(ch2Preset))
                    allSuccess = false;

                // Apply Trigger preset
                var triggerPreset = TriggerSettings.Presets.GeneralPurpose;
                if (!ApplyTriggerSettings(triggerPreset))
                    allSuccess = false;

                // Apply TimeBase preset
                var timebasePreset = TimeBaseSettings.Presets.GeneralPurpose;
                if (!ApplyTimeBaseSettings(timebasePreset))
                    allSuccess = false;

                if (allSuccess)
                {
                    Log("✅ General purpose preset applied successfully");
                }
                else
                {
                    Log("⚠️ Some preset settings could not be applied");
                }

                return allSuccess;
            }
            catch (Exception ex)
            {
                Log($"❌ Error applying general purpose preset: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply power measurement preset to all subsystems
        /// </summary>
        public bool ApplyPowerMeasurementPreset()
        {
            if (!oscilloscope.IsConnected) return false;

            try
            {
                Log("Applying power measurement preset...");

                bool allSuccess = true;

                var ch1Preset = Ch1Settings.Presets.PowerMeasurement;
                if (!ApplyChannel1Settings(ch1Preset))
                    allSuccess = false;

                var ch2Preset = Ch2Settings.Presets.PowerMeasurement;
                if (!ApplyChannel2Settings(ch2Preset))
                    allSuccess = false;

                var triggerPreset = TriggerSettings.Presets.PowerMeasurement;
                if (!ApplyTriggerSettings(triggerPreset))
                    allSuccess = false;

                var timebasePreset = TimeBaseSettings.Presets.PowerMeasurement;
                if (!ApplyTimeBaseSettings(timebasePreset))
                    allSuccess = false;

                return allSuccess;
            }
            catch (Exception ex)
            {
                Log($"❌ Error applying power measurement preset: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply high frequency preset to all subsystems
        /// </summary>
        public bool ApplyHighFrequencyPreset()
        {
            if (!oscilloscope.IsConnected) return false;

            try
            {
                Log("Applying high frequency preset...");

                bool allSuccess = true;

                var ch1Preset = Ch1Settings.Presets.HighFrequency;
                if (!ApplyChannel1Settings(ch1Preset))
                    allSuccess = false;

                var ch2Preset = Ch2Settings.Presets.HighFrequency;
                if (!ApplyChannel2Settings(ch2Preset))
                    allSuccess = false;

                var triggerPreset = TriggerSettings.Presets.GeneralPurpose;
                if (!ApplyTriggerSettings(triggerPreset))
                    allSuccess = false;

                var timebasePreset = TimeBaseSettings.Presets.HighFrequency;
                if (!ApplyTimeBaseSettings(timebasePreset))
                    allSuccess = false;

                return allSuccess;
            }
            catch (Exception ex)
            {
                Log($"❌ Error applying high frequency preset: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply digital measurement preset to all subsystems
        /// </summary>
        public bool ApplyDigitalPreset()
        {
            if (!oscilloscope.IsConnected) return false;

            try
            {
                Log("Applying digital measurement preset...");

                bool allSuccess = true;

                var ch1Preset = Ch1Settings.Presets.GeneralPurpose;
                if (!ApplyChannel1Settings(ch1Preset))
                    allSuccess = false;

                var ch2Preset = Ch2Settings.Presets.GeneralPurpose;
                if (!ApplyChannel2Settings(ch2Preset))
                    allSuccess = false;

                var triggerPreset = TriggerSettings.Presets.Digital;
                if (!ApplyTriggerSettings(triggerPreset))
                    allSuccess = false;

                var timebasePreset = TimeBaseSettings.Presets.Digital;
                if (!ApplyTimeBaseSettings(timebasePreset))
                    allSuccess = false;

                return allSuccess;
            }
            catch (Exception ex)
            {
                Log($"❌ Error applying digital preset: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Individual Settings Application - CHANGED TO PUBLIC

        /// <summary>
        /// Apply Channel 1 settings to oscilloscope
        /// </summary>
        public bool ApplyChannel1Settings(Ch1Settings settings)  // Changed from private to public
        {
            try
            {
                bool allSuccess = true;

                if (!oscilloscope.SendCommand($":CHANnel1:DISPlay {(settings.IsEnabled ? "ON" : "OFF")}"))
                    allSuccess = false;

                if (!oscilloscope.SendCommand($":CHANnel1:PROBe {settings.ProbeRatio.ToString(CultureInfo.InvariantCulture)}"))
                    allSuccess = false;

                if (!oscilloscope.SendCommand($":CHANnel1:SCALe {settings.VerticalScale.ToString(CultureInfo.InvariantCulture)}"))
                    allSuccess = false;

                if (!oscilloscope.SendCommand($":CHANnel1:OFFSet {settings.VerticalOffset.ToString(CultureInfo.InvariantCulture)}"))
                    allSuccess = false;

                if (!oscilloscope.SendCommand($":CHANnel1:COUPling {settings.Coupling}"))
                    allSuccess = false;

                Channel1Settings = settings.Clone();
                return allSuccess;
            }
            catch (Exception ex)
            {
                Log($"❌ Error applying Channel 1 settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply Channel 2 settings to oscilloscope
        /// </summary>
        public bool ApplyChannel2Settings(Ch2Settings settings)  // Changed from private to public
        {
            try
            {
                bool allSuccess = true;

                if (!oscilloscope.SendCommand($":CHANnel2:DISPlay {(settings.IsEnabled ? "ON" : "OFF")}"))
                    allSuccess = false;

                if (!oscilloscope.SendCommand($":CHANnel2:PROBe {settings.ProbeRatio.ToString(CultureInfo.InvariantCulture)}"))
                    allSuccess = false;

                if (!oscilloscope.SendCommand($":CHANnel2:SCALe {settings.VerticalScale.ToString(CultureInfo.InvariantCulture)}"))
                    allSuccess = false;

                if (!oscilloscope.SendCommand($":CHANnel2:OFFSet {settings.VerticalOffset.ToString(CultureInfo.InvariantCulture)}"))
                    allSuccess = false;

                if (!oscilloscope.SendCommand($":CHANnel2:COUPling {settings.Coupling}"))
                    allSuccess = false;

                Channel2Settings = settings.Clone();
                return allSuccess;
            }
            catch (Exception ex)
            {
                Log($"❌ Error applying Channel 2 settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply Trigger settings to oscilloscope
        /// </summary>
        public bool ApplyTriggerSettings(TriggerSettings settings)  // Changed from private to public
        {
            try
            {
                bool allSuccess = true;

                if (!oscilloscope.SendCommand($":TRIGger:MODE {settings.Mode}"))
                    allSuccess = false;

                if (!oscilloscope.SendCommand($":TRIGger:COUPling {settings.Coupling}"))
                    allSuccess = false;

                if (!oscilloscope.SendCommand($":TRIGger:SWEep {settings.Sweep}"))
                    allSuccess = false;

                if (settings.Mode.ToUpper() == "EDGE")
                {
                    if (!oscilloscope.SendCommand($":TRIGger:EDGe:SOURce {settings.EdgeSource}"))
                        allSuccess = false;

                    if (!oscilloscope.SendCommand($":TRIGger:EDGe:SLOPe {settings.EdgeSlope}"))
                        allSuccess = false;

                    if (!oscilloscope.SendCommand($":TRIGger:EDGe:LEVel {settings.EdgeLevel.ToString(CultureInfo.InvariantCulture)}"))
                        allSuccess = false;
                }

                if (!oscilloscope.SendCommand($":TRIGger:HOLDoff {settings.Holdoff.ToString(CultureInfo.InvariantCulture)}"))
                    allSuccess = false;

                if (!oscilloscope.SendCommand($":TRIGger:NREJect {(settings.NoiseReject ? "ON" : "OFF")}"))
                    allSuccess = false;

                TriggerSettings = settings.Clone();
                return allSuccess;
            }
            catch (Exception ex)
            {
                Log($"❌ Error applying Trigger settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply TimeBase settings to oscilloscope
        /// </summary>
        public bool ApplyTimeBaseSettings(TimeBaseSettings settings)  // Changed from private to public
        {
            try
            {
                bool allSuccess = true;

                if (!oscilloscope.SendCommand($":TIMebase:MODE {settings.Mode}"))
                    allSuccess = false;

                if (!oscilloscope.SendCommand($":TIMebase:SCALe {settings.MainScale.ToString(CultureInfo.InvariantCulture)}"))
                    allSuccess = false;

                if (!oscilloscope.SendCommand($":TIMebase:OFFSet {settings.MainOffset.ToString(CultureInfo.InvariantCulture)}"))
                    allSuccess = false;

                TimeBaseSettings = settings.Clone();
                return allSuccess;
            }
            catch (Exception ex)
            {
                Log($"❌ Error applying TimeBase settings: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Public Information Methods

        /// <summary>
        /// Get device ID string
        /// </summary>
        public string GetDeviceID()
        {
            return deviceId;
        }

        /// <summary>
        /// Get acquisition information string
        /// </summary>
        public string GetAcquisitionInfo()
        {
            return acquisitionInfo;
        }

        #endregion


        #region Measurements

        // Add this method to read measurement settings:

        /// <summary>
        /// Read Measurement settings from oscilloscope
        /// </summary>
        private bool ReadMeasurementSettings()
        {
            try
            {
                // Read measurement display settings
                string autoDisplay = oscilloscope.SendQuery(":MEASure:ADISplay?");
                string autoSource = oscilloscope.SendQuery(":MEASure:AMSource?");
                string statisticDisplay = oscilloscope.SendQuery(":MEASure:STATistic:DISPlay?");
                string statisticMode = oscilloscope.SendQuery(":MEASure:STATistic:MODE?");

                // Read measurement setup parameters
                string thresholdMax = oscilloscope.SendQuery(":MEASure:SETup:MAX?");
                string thresholdMid = oscilloscope.SendQuery(":MEASure:SETup:MID?");
                string thresholdMin = oscilloscope.SendQuery(":MEASure:SETup:MIN?");
                string pulseSetupB = oscilloscope.SendQuery(":MEASure:SETup:PSB?");
                string delaySetupA = oscilloscope.SendQuery(":MEASure:SETup:DSA?");
                string delaySetupB = oscilloscope.SendQuery(":MEASure:SETup:DSB?");

                // Parse and set values
                if (!string.IsNullOrEmpty(autoDisplay))
                    MeasurementSettings.AutoDisplayEnabled = autoDisplay.Trim() == "1";

                if (!string.IsNullOrEmpty(autoSource))
                    MeasurementSettings.AutoMeasureSource = ParseSourceChannelResponse(autoSource.Trim());

                if (!string.IsNullOrEmpty(statisticDisplay))
                    MeasurementSettings.StatisticDisplayEnabled = statisticDisplay.Trim() == "1";

                if (!string.IsNullOrEmpty(statisticMode))
                    MeasurementSettings.StatisticMode = ParseStatisticModeResponse(statisticMode.Trim());

                if (!string.IsNullOrEmpty(thresholdMax) && double.TryParse(thresholdMax, out double maxVal))
                    MeasurementSettings.ThresholdMax = maxVal;

                if (!string.IsNullOrEmpty(thresholdMid) && double.TryParse(thresholdMid, out double midVal))
                    MeasurementSettings.ThresholdMid = midVal;

                if (!string.IsNullOrEmpty(thresholdMin) && double.TryParse(thresholdMin, out double minVal))
                    MeasurementSettings.ThresholdMin = minVal;

                if (!string.IsNullOrEmpty(pulseSetupB) && double.TryParse(pulseSetupB, out double psbVal))
                    MeasurementSettings.PulseSetupB = psbVal;

                if (!string.IsNullOrEmpty(delaySetupA) && double.TryParse(delaySetupA, out double dsaVal))
                    MeasurementSettings.DelaySetupA = dsaVal;

                if (!string.IsNullOrEmpty(delaySetupB) && double.TryParse(delaySetupB, out double dsbVal))
                    MeasurementSettings.DelaySetupB = dsbVal;

                // Note: Enabled measurements list cannot be easily queried from DS1000Z-E
                // They would need to be maintained by the application or queried individually

                Log("✅ Measurement settings read successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log($"❌ Error reading measurement settings: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// Parse source channel response from oscilloscope
        /// </summary>
        private string ParseSourceChannelResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return "CHAN1";

            response = response.ToUpper().Trim();

            // Handle different response formats
            if (response.Contains("CHAN1") || response == "1")
                return "CHAN1";
            else if (response.Contains("CHAN2") || response == "2")
                return "CHAN2";
            else if (response.Contains("CHAN3") || response == "3")
                return "CHAN3";
            else if (response.Contains("CHAN4") || response == "4")
                return "CHAN4";
            else
                return "CHAN1"; // Default fallback
        }

        /// <summary>
        /// Parse statistic mode response from oscilloscope
        /// </summary>
        private string ParseStatisticModeResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return "OFF";

            response = response.ToUpper().Trim();

            // Handle different response formats
            if (response == "DIFF" || response == "DIFFERENCE")
                return "DIFFERENCE";
            else if (response == "EXTR" || response == "EXTREMUM")
                return "EXTREMUM";
            else if (response == "OFF" || response == "0")
                return "OFF";
            else if (response == "ON" || response == "1")
                return "EXTREMUM"; // Default when enabled
            else
                return "OFF"; // Default fallback
        }


        #endregion



        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}