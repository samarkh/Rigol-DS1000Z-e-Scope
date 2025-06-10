using System;
using System.Text;
using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;
using DS1000Z_E_USB_Control.TimeBase;
using DS1000Z_E_USB_Control.Trigger;
using Rigol_DS1000Z_E_Control;

namespace DS1000Z_E_USB_Control
{
    /// <summary>
    /// Comprehensive settings manager for all oscilloscope subsystems
    /// </summary>
    public class OscilloscopeSettingsManager
    {
        private readonly RigolDS1000ZE oscilloscope;

        // Controllers for each subsystem
        private readonly Ch1Controller ch1Controller;
        private readonly Ch2Controller ch2Controller;
        private readonly TimeBaseController timeBaseController;
        private readonly TriggerController triggerController;

        public event EventHandler<string> LogEvent;

        // Public properties to access current settings
        public Ch1Settings Channel1Settings => ch1Controller?.GetSettings();
        public Ch2Settings Channel2Settings => ch2Controller?.GetSettings();
        public TimeBaseSettings TimeBaseSettings => timeBaseController?.GetSettings();
        public TriggerSettings TriggerSettings => triggerController?.GetSettings();

        public OscilloscopeSettingsManager(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope;

            // Initialize controllers
            ch1Controller = new Ch1Controller(oscilloscope);
            ch2Controller = new Ch2Controller(oscilloscope);
            timeBaseController = new TimeBaseController(oscilloscope);
            triggerController = new TriggerController(oscilloscope);

            // Wire up logging events
            ch1Controller.LogEvent += (sender, message) => LogEvent?.Invoke(this, $"CH1: {message}");
            ch2Controller.LogEvent += (sender, message) => LogEvent?.Invoke(this, $"CH2: {message}");
            timeBaseController.LogEvent += (sender, message) => LogEvent?.Invoke(this, $"TIME: {message}");
            triggerController.LogEvent += (sender, message) => LogEvent?.Invoke(this, $"TRIG: {message}");
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

            bool allSuccessful = true;

            try
            {
                Log("Reading all oscilloscope settings...");

                // Read Channel 1 settings
                Log("Reading Channel 1 settings...");
                if (!ch1Controller.QueryAndUpdateSettings())
                {
                    Log("⚠️ Failed to read some Channel 1 settings");
                    allSuccessful = false;
                }

                // Read Channel 2 settings
                Log("Reading Channel 2 settings...");
                if (!ch2Controller.QueryAndUpdateSettings())
                {
                    Log("⚠️ Failed to read some Channel 2 settings");
                    allSuccessful = false;
                }

                // Read TimeBase settings
                Log("Reading TimeBase settings...");
                if (!timeBaseController.QueryAndUpdateSettings())
                {
                    Log("⚠️ Failed to read some TimeBase settings");
                    allSuccessful = false;
                }

                // Read Trigger settings
                Log("Reading Trigger settings...");
                if (!triggerController.QueryAndUpdateSettings())
                {
                    Log("⚠️ Failed to read some Trigger settings");
                    allSuccessful = false;
                }

                if (allSuccessful)
                {
                    Log("✅ Successfully read all oscilloscope settings");
                }
                else
                {
                    Log("⚠️ Some settings could not be read completely");
                }

                return allSuccessful;
            }
            catch (Exception ex)
            {
                Log($"❌ Error reading oscilloscope settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Export all current settings to a formatted string
        /// </summary>
        public string ExportSettingsToString()
        {
            var export = new StringBuilder();

            export.AppendLine("=== Rigol DS1000Z-E Oscilloscope Settings Export ===");
            export.AppendLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            export.AppendLine($"Device ID: {GetDeviceID()}");
            export.AppendLine($"Acquisition Info: {GetAcquisitionInfo()}");
            export.AppendLine();

            // Channel 1 Settings
            export.AppendLine("=== CHANNEL 1 SETTINGS ===");
            if (Channel1Settings != null)
            {
                export.AppendLine($"Enabled: {Channel1Settings.IsEnabled}");
                export.AppendLine($"Probe Ratio: {Channel1Settings.ProbeRatio}×");
                export.AppendLine($"Vertical Scale: {Channel1Settings.VerticalScale} V/div");
                export.AppendLine($"Vertical Offset: {Channel1Settings.VerticalOffset} V");
                export.AppendLine($"Vertical Range: {Channel1Settings.VerticalRange} V");
                export.AppendLine($"Input Coupling: {Channel1Settings.Coupling}");
                export.AppendLine($"Bandwidth Limit: {Channel1Settings.BandwidthLimit}");
                export.AppendLine($"Units: {Channel1Settings.Units}");
                export.AppendLine($"Invert Enabled: {Channel1Settings.InvertEnabled}");
                export.AppendLine($"Vernier Enabled: {Channel1Settings.VernierEnabled}");
            }
            else
            {
                export.AppendLine("Channel 1 settings not available");
            }
            export.AppendLine();

            // Channel 2 Settings
            export.AppendLine("=== CHANNEL 2 SETTINGS ===");
            if (Channel2Settings != null)
            {
                export.AppendLine($"Enabled: {Channel2Settings.IsEnabled}");
                export.AppendLine($"Probe Ratio: {Channel2Settings.ProbeRatio}×");
                export.AppendLine($"Vertical Scale: {Channel2Settings.VerticalScale} V/div");
                export.AppendLine($"Vertical Offset: {Channel2Settings.VerticalOffset} V");
                export.AppendLine($"Vertical Range: {Channel2Settings.VerticalRange} V");
                export.AppendLine($"Input Coupling: {Channel2Settings.Coupling}");
                export.AppendLine($"Bandwidth Limit: {Channel2Settings.BandwidthLimit}");
                export.AppendLine($"Units: {Channel2Settings.Units}");
                export.AppendLine($"Invert Enabled: {Channel2Settings.InvertEnabled}");
                export.AppendLine($"Vernier Enabled: {Channel2Settings.VernierEnabled}");
            }
            else
            {
                export.AppendLine("Channel 2 settings not available");
            }
            export.AppendLine();

            // TimeBase Settings
            export.AppendLine("=== TIMEBASE SETTINGS ===");
            if (TimeBaseSettings != null)
            {
                export.AppendLine($"Mode: {TimeBaseSettings.Mode}");
                export.AppendLine($"Main Scale: {TimeBaseSettings.MainScale} s/div ({TimeBaseSettings.MainScaleDisplay})");
                export.AppendLine($"Main Offset: {TimeBaseSettings.MainOffset} s");
                export.AppendLine($"Time Window: {TimeBaseSettings.TimeWindow} s");
                export.AppendLine($"Delay Enabled: {TimeBaseSettings.DelayEnabled}");
                export.AppendLine($"Delay Scale: {TimeBaseSettings.DelayScale} s/div ({TimeBaseSettings.DelayScaleDisplay})");
                export.AppendLine($"Delay Offset: {TimeBaseSettings.DelayOffset} s");
            }
            else
            {
                export.AppendLine("TimeBase settings not available");
            }
            export.AppendLine();

            // Trigger Settings
            export.AppendLine("=== TRIGGER SETTINGS ===");
            if (TriggerSettings != null)
            {
                export.AppendLine($"Mode: {TriggerSettings.Mode}");
                export.AppendLine($"Coupling: {TriggerSettings.Coupling}");
                export.AppendLine($"Sweep: {TriggerSettings.Sweep}");
                export.AppendLine($"Status: {TriggerSettings.Status}");
                export.AppendLine($"Position: {TriggerSettings.Position}%");
                export.AppendLine($"Holdoff: {TriggerSettings.Holdoff} s ({TriggerSettings.HoldoffDisplay})");
                export.AppendLine($"Noise Reject: {TriggerSettings.NoiseReject}");

                // Edge trigger specific settings
                if (TriggerSettings.Mode.ToUpper() == "EDGE")
                {
                    export.AppendLine("--- Edge Trigger Settings ---");
                    export.AppendLine($"Source: {TriggerSettings.EdgeSource}");
                    export.AppendLine($"Slope: {TriggerSettings.EdgeSlope}");
                    export.AppendLine($"Level: {TriggerSettings.EdgeLevel} V ({TriggerSettings.EdgeLevelDisplay})");
                }
            }
            else
            {
                export.AppendLine("Trigger settings not available");
            }
            export.AppendLine();

            // Add footer
            export.AppendLine("=== END OF SETTINGS EXPORT ===");
            export.AppendLine($"Generated by DS1000Z-E USB Control Application");
            export.AppendLine($"Total Settings Exported: {(Channel1Settings != null ? 1 : 0) + (Channel2Settings != null ? 1 : 0) + (TimeBaseSettings != null ? 1 : 0) + (TriggerSettings != null ? 1 : 0)} subsystems");

            return export.ToString();
        }

        /// <summary>
        /// Get device identification string
        /// </summary>
        public string GetDeviceID()
        {
            if (!oscilloscope.IsConnected)
                return "Not Connected";

            try
            {
                string id = oscilloscope.SendQuery("*IDN?");
                return string.IsNullOrEmpty(id) ? "Unknown Device" : id.Trim();
            }
            catch
            {
                return "Query Failed";
            }
        }

        /// <summary>
        /// Get acquisition information
        /// </summary>
        public string GetAcquisitionInfo()
        {
            if (!oscilloscope.IsConnected)
                return "Unknown";

            try
            {
                // Query sample rate
                string sampleRate = oscilloscope.SendQuery(":ACQuire:SRATe?");
                string acqType = oscilloscope.SendQuery(":ACQuire:TYPE?");
                string memDepth = oscilloscope.SendQuery(":ACQuire:MDEPth?");

                if (!string.IsNullOrEmpty(sampleRate) && !string.IsNullOrEmpty(acqType))
                {
                    double sr = double.Parse(sampleRate);
                    string srFormatted = FormatSampleRate(sr);
                    return $"Type: {acqType.Trim()}, Rate: {srFormatted}, Depth: {memDepth?.Trim() ?? "Unknown"}";
                }
                return "Query Incomplete";
            }
            catch
            {
                return "Query Failed";
            }
        }

        /// <summary>
        /// Apply settings to all subsystems
        /// </summary>
        public bool ApplyAllSettings(Ch1Settings ch1Settings, Ch2Settings ch2Settings,
                                   TimeBaseSettings timeBaseSettings, TriggerSettings triggerSettings)
        {
            if (!oscilloscope.IsConnected)
            {
                Log("Cannot apply settings - oscilloscope not connected");
                return false;
            }

            bool allSuccessful = true;

            try
            {
                Log("Applying all oscilloscope settings...");

                // Apply Channel 1 settings
                if (ch1Settings != null)
                {
                    Log("Applying Channel 1 settings...");
                    ch1Controller.SetSettings(ch1Settings);
                }

                // Apply Channel 2 settings
                if (ch2Settings != null)
                {
                    Log("Applying Channel 2 settings...");
                    ch2Controller.SetSettings(ch2Settings);
                }

                // Apply TimeBase settings
                if (timeBaseSettings != null)
                {
                    Log("Applying TimeBase settings...");
                    timeBaseController.SetSettings(timeBaseSettings);
                }

                // Apply Trigger settings
                if (triggerSettings != null)
                {
                    Log("Applying Trigger settings...");
                    triggerController.SetSettings(triggerSettings);
                }

                Log("✅ All settings applied successfully");
                return allSuccessful;
            }
            catch (Exception ex)
            {
                Log($"❌ Error applying oscilloscope settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get individual controller references for UI binding
        /// </summary>
        public Ch1Controller GetCh1Controller() => ch1Controller;
        public Ch2Controller GetCh2Controller() => ch2Controller;
        public TimeBaseController GetTimeBaseController() => timeBaseController;
        public TriggerController GetTriggerController() => triggerController;

        #region Helper Methods

        private string FormatSampleRate(double sampleRate)
        {
            if (sampleRate >= 1e9)
                return $"{sampleRate / 1e9:F2} GSa/s";
            else if (sampleRate >= 1e6)
                return $"{sampleRate / 1e6:F1} MSa/s";
            else if (sampleRate >= 1e3)
                return $"{sampleRate / 1e3:F1} kSa/s";
            else
                return $"{sampleRate:F0} Sa/s";
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }

        #endregion

        #region Preset Management

        /// <summary>
        /// Apply general purpose preset to all subsystems
        /// </summary>
        public void ApplyGeneralPurposePreset()
        {
            ApplyAllSettings(
                Ch1Settings.Presets.GeneralPurpose,
                Ch2Settings.Presets.GeneralPurpose,
                TimeBaseSettings.Presets.GeneralPurpose,
                TriggerSettings.Presets.GeneralPurpose
            );
            Log("Applied General Purpose preset to all subsystems");
        }

        /// <summary>
        /// Apply small signal preset to all subsystems
        /// </summary>
        public void ApplySmallSignalPreset()
        {
            ApplyAllSettings(
                Ch1Settings.Presets.SmallSignal,
                Ch2Settings.Presets.SmallSignal,
                TimeBaseSettings.Presets.LowFrequency,
                TriggerSettings.Presets.NoisySignal
            );
            Log("Applied Small Signal preset to all subsystems");
        }

        /// <summary>
        /// Apply power measurement preset to all subsystems
        /// </summary>
        public void ApplyPowerMeasurementPreset()
        {
            ApplyAllSettings(
                Ch1Settings.Presets.PowerMeasurement,
                Ch2Settings.Presets.PowerMeasurement,
                TimeBaseSettings.Presets.PowerMeasurement,
                TriggerSettings.Presets.PowerMeasurement
            );
            Log("Applied Power Measurement preset to all subsystems");
        }

        /// <summary>
        /// Apply high frequency preset to all subsystems
        /// </summary>
        public void ApplyHighFrequencyPreset()
        {
            ApplyAllSettings(
                Ch1Settings.Presets.HighFrequency,
                Ch2Settings.Presets.HighFrequency,
                TimeBaseSettings.Presets.HighFrequency,
                TriggerSettings.Presets.GeneralPurpose
            );
            Log("Applied High Frequency preset to all subsystems");
        }

        /// <summary>
        /// Apply digital measurement preset to all subsystems
        /// </summary>
        public void ApplyDigitalPreset()
        {
            ApplyAllSettings(
                Ch1Settings.Presets.GeneralPurpose,
                Ch2Settings.Presets.GeneralPurpose,
                TimeBaseSettings.Presets.Digital,
                TriggerSettings.Presets.Digital
            );
            Log("Applied Digital Measurement preset to all subsystems");
        }

        #endregion
    }
}