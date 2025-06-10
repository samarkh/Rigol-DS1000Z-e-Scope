using System;
using System.Globalization;
using Rigol_DS1000Z_E_Control;

namespace DS1000Z_E_USB_Control.Trigger
{
    /// <summary>
    /// Trigger controller for managing trigger settings
    /// This class will be expanded in future versions for full trigger control
    /// </summary>
    public class TriggerController
    {
        private readonly RigolDS1000ZE oscilloscope;
        private readonly TriggerSettings settings;

        public event EventHandler<string> LogEvent;
        public event EventHandler SettingsChanged;

        public TriggerController(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope;
            this.settings = new TriggerSettings();
        }

        /// <summary>
        /// Set the trigger mode
        /// </summary>
        public bool SetTriggerMode(string mode)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:MODE {mode}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Mode = mode;
                Log($"Trigger mode set to {mode}");
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger mode");
            }

            return success;
        }

        /// <summary>
        /// Set the trigger sweep mode
        /// </summary>
        public bool SetTriggerSweep(string sweep)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:SWEep {sweep}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Sweep = sweep;
                Log($"Trigger sweep set to {sweep}");
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger sweep");
            }

            return success;
        }

        /// <summary>
        /// Set the edge trigger source
        /// </summary>
        public bool SetEdgeSource(string source)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:EDGe:SOURce {source}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.EdgeSource = source;
                Log($"Edge trigger source set to {source}");
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set edge trigger source");
            }

            return success;
        }

        /// <summary>
        /// Set the edge trigger slope
        /// </summary>
        public bool SetEdgeSlope(string slope)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:EDGe:SLOPe {slope}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.EdgeSlope = slope;
                Log($"Edge trigger slope set to {slope}");
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set edge trigger slope");
            }

            return success;
        }

        /// <summary>
        /// Set the edge trigger level
        /// </summary>
        public bool SetEdgeLevel(double level)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:EDGe:LEVel {level.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.EdgeLevel = level;
                Log($"Edge trigger level set to {settings.EdgeLevelDisplay}");
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set edge trigger level");
            }

            return success;
        }

        /// <summary>
        /// Set the trigger coupling
        /// </summary>
        public bool SetTriggerCoupling(string coupling)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:COUPling {coupling}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Coupling = coupling;
                Log($"Trigger coupling set to {coupling}");
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger coupling");
            }

            return success;
        }

        /// <summary>
        /// Set the trigger holdoff time
        /// </summary>
        public bool SetHoldoff(double holdoff)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:HOLDoff {holdoff.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Holdoff = holdoff;
                Log($"Trigger holdoff set to {settings.HoldoffDisplay}");
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger holdoff");
            }

            return success;
        }

        /// <summary>
        /// Enable or disable noise reject
        /// </summary>
        public bool SetNoiseReject(bool enable)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:NREJect {(enable ? "ON" : "OFF")}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.NoiseReject = enable;
                Log($"Trigger noise reject {(enable ? "enabled" : "disabled")}");
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger noise reject");
            }

            return success;
        }

        /// <summary>
        /// Force a trigger
        /// </summary>
        public bool ForceTrigger()
        {
            if (!oscilloscope.IsConnected) return false;

            bool success = oscilloscope.SendCommand(":TFORce");

            if (success)
            {
                Log("Trigger forced");
            }
            else
            {
                Log("Failed to force trigger");
            }

            return success;
        }

        /// <summary>
        /// Query and update all trigger settings from oscilloscope
        /// </summary>
        public bool QueryAndUpdateSettings()
        {
            if (!oscilloscope.IsConnected) return false;

            try
            {
                // Read trigger mode
                string mode = oscilloscope.SendQuery(":TRIGger:MODE?");
                if (!string.IsNullOrEmpty(mode))
                {
                    settings.Mode = mode.Trim();
                }

                // Read trigger coupling
                string coupling = oscilloscope.SendQuery(":TRIGger:COUPling?");
                if (!string.IsNullOrEmpty(coupling))
                {
                    settings.Coupling = coupling.Trim();
                }

                // Read trigger sweep mode
                string sweep = oscilloscope.SendQuery(":TRIGger:SWEep?");
                if (!string.IsNullOrEmpty(sweep))
                {
                    settings.Sweep = sweep.Trim();
                }

                // Read trigger status
                string status = oscilloscope.SendQuery(":TRIGger:STATus?");
                if (!string.IsNullOrEmpty(status))
                {
                    settings.Status = status.Trim();
                }

                // For edge trigger mode, read edge-specific settings
                if (settings.Mode.ToUpper() == "EDGE")
                {
                    // Read edge trigger source
                    string edgeSource = oscilloscope.SendQuery(":TRIGger:EDGe:SOURce?");
                    if (!string.IsNullOrEmpty(edgeSource))
                    {
                        settings.EdgeSource = edgeSource.Trim();
                    }

                    // Read edge trigger slope
                    string edgeSlope = oscilloscope.SendQuery(":TRIGger:EDGe:SLOPe?");
                    if (!string.IsNullOrEmpty(edgeSlope))
                    {
                        settings.EdgeSlope = edgeSlope.Trim();
                    }

                    // Read edge trigger level
                    string edgeLevel = oscilloscope.SendQuery(":TRIGger:EDGe:LEVel?");
                    if (!string.IsNullOrEmpty(edgeLevel) &&
                        double.TryParse(edgeLevel, NumberStyles.Float, CultureInfo.InvariantCulture, out double level))
                    {
                        settings.EdgeLevel = level;
                    }
                }

                Log($"Trigger settings updated: {settings}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error querying Trigger settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get current trigger settings
        /// </summary>
        public TriggerSettings GetSettings()
        {
            return settings.Clone();
        }

        /// <summary>
        /// Set trigger settings
        /// </summary>
        public void SetSettings(TriggerSettings newSettings)
        {
            if (newSettings == null) return;

            SetTriggerMode(newSettings.Mode);
            SetTriggerCoupling(newSettings.Coupling);
            SetTriggerSweep(newSettings.Sweep);

            if (newSettings.Mode.ToUpper() == "EDGE")
            {
                SetEdgeSource(newSettings.EdgeSource);
                SetEdgeSlope(newSettings.EdgeSlope);
                SetEdgeLevel(newSettings.EdgeLevel);
            }

            SetHoldoff(newSettings.Holdoff);
            SetNoiseReject(newSettings.NoiseReject);
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}