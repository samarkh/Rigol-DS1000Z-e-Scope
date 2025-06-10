using System;
using System.Collections.Generic;
using System.Globalization;
using Rigol_DS1000Z_E_Control;

namespace DS1000Z_E_USB_Control.TimeBase
{
    /// <summary>
    /// TimeBase controller for managing horizontal timebase settings
    /// This class will be expanded in future versions for full timebase control
    /// </summary>
    public class TimeBaseController
    {
        private readonly RigolDS1000ZE oscilloscope;
        private readonly TimeBaseSettings settings;

        public event EventHandler<string> LogEvent;
        public event EventHandler SettingsChanged;

        public TimeBaseController(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope;
            this.settings = new TimeBaseSettings();
        }

        /// <summary>
        /// Set the horizontal scale (time per division)
        /// </summary>
        public bool SetHorizontalScale(double scale)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TIMebase:SCALe {scale.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.MainScale = scale;
                Log($"TimeBase horizontal scale set to {settings.MainScaleDisplay}");
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set TimeBase horizontal scale");
            }

            return success;
        }

        /// <summary>
        /// Set the horizontal offset
        /// </summary>
        public bool SetHorizontalOffset(double offset)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TIMebase:OFFSet {offset.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.MainOffset = offset;
                Log($"TimeBase horizontal offset set to {offset:E3}s");
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set TimeBase horizontal offset");
            }

            return success;
        }

        /// <summary>
        /// Set the timebase mode
        /// </summary>
        public bool SetMode(string mode)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TIMebase:MODE {mode}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Mode = mode;
                Log($"TimeBase mode set to {mode}");
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set TimeBase mode");
            }

            return success;
        }

        /// <summary>
        /// Query and update all timebase settings from oscilloscope
        /// </summary>
        public bool QueryAndUpdateSettings()
        {
            if (!oscilloscope.IsConnected) return false;

            try
            {
                // Read timebase mode
                string mode = oscilloscope.SendQuery(":TIMebase:MODE?");
                if (!string.IsNullOrEmpty(mode))
                {
                    settings.Mode = mode.Trim();
                }

                // Read main horizontal scale
                string mainScale = oscilloscope.SendQuery(":TIMebase:SCALe?");
                if (!string.IsNullOrEmpty(mainScale) &&
                    double.TryParse(mainScale, NumberStyles.Float, CultureInfo.InvariantCulture, out double scale))
                {
                    settings.MainScale = scale;
                }

                // Read main horizontal offset
                string mainOffset = oscilloscope.SendQuery(":TIMebase:OFFSet?");
                if (!string.IsNullOrEmpty(mainOffset) &&
                    double.TryParse(mainOffset, NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
                {
                    settings.MainOffset = offset;
                }

                Log($"TimeBase settings updated: {settings}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error querying TimeBase settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get current timebase settings
        /// </summary>
        public TimeBaseSettings GetSettings()
        {
            return settings.Clone();
        }

        /// <summary>
        /// Set timebase settings
        /// </summary>
        public void SetSettings(TimeBaseSettings newSettings)
        {
            if (newSettings == null) return;

            SetMode(newSettings.Mode);
            SetHorizontalScale(newSettings.MainScale);
            SetHorizontalOffset(newSettings.MainOffset);
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}