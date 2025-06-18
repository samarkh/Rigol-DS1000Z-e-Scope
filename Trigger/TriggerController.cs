using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;
using DS1000Z_E_USB_Control.Controls;
using Rigol_DS1000Z_E_Control;
using System;
using System.Globalization;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Trigger
{
    /// <summary>
    /// Controller for trigger operations with multimedia arrow controls
    /// Manages trigger settings and communication with Rigol DS1000Z-E oscilloscope
    /// </summary>
    public class TriggerController
    {
        #region Fields

        private readonly RigolDS1000ZE oscilloscope;
        private readonly TriggerSettings settings;
        private bool isUpdating = false;

        #endregion

        #region Events

        public event EventHandler<string> LogEvent;
        public event EventHandler SettingsChanged;

        #endregion

        #region UI Control Properties

        // Core combo box controls
        public ComboBox TriggerModeComboBox { get; set; }
        public ComboBox TriggerSweepComboBox { get; set; }
        public ComboBox EdgeSourceComboBox { get; set; }
        public ComboBox EdgeSlopeComboBox { get; set; }
        public ComboBox TriggerCouplingComboBox { get; set; }

        // Text controls
        public TextBox HoldoffTextBox { get; set; } // this needs clearing up
        public TextBlock CurrentTriggerSettingsText { get; set; }

        // Multimedia controls (EmojiArrows instead of slider)
        public EmojiArrows TriggerLevelArrows { get; set; }
        public TextBlock LevelValueText { get; set; }

        // Button controls
        public Button ForceTriggerButton { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize trigger controller with oscilloscope connection
        /// </summary>
        public TriggerController(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));
            this.settings = new TriggerSettings();
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Initialize UI controls with current settings
        /// UPDATED: Add immediate dynamic step sizing
        /// </summary>
        public void InitializeControls()
        {
            DisableEventHandlers();
            isUpdating = true;

            try
            {
                InitializeComboBoxes();
                InitializeTriggerLevelArrowControl();
                InitializeOtherControls();
                UpdateCurrentSettingsDisplay();

                Log("Trigger controls initialized");
                Log("⏳ Waiting for channel settings to set dynamic trigger steps...");
            }
            catch (Exception ex)
            {
                Log($"Error initializing trigger UI: {ex.Message}");
            }
            finally
            {
                EnableEventHandlers();
                isUpdating = false;
            }
        }

        /// <summary>
        /// Handle trigger level changes from arrow control
        /// </summary>
        public void HandleTriggerLevelChanged(double level)
        {
            if (!oscilloscope.IsConnected || isUpdating) return;

            string command = $":TRIGger:EDGe:LEVel {level.ToString(CultureInfo.InvariantCulture)}";

            if (oscilloscope.SendCommand(command))
            {
                settings.EdgeLevel = level;
                Log($"Trigger level set to {level:F1}V");
                UpdateCurrentSettingsDisplay();
                UpdateLevelValueDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger level");
                UpdateArrowControlFromSettings();
            }
        }

        /// <summary>
        /// Set trigger mode
        /// </summary>
        public bool SetMode(string mode)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:MODE {mode}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Mode = mode;
                Log($"Trigger mode set to {mode}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger mode");
            }

            return success;
        }

        /// <summary>
        /// Set trigger sweep mode
        /// </summary>
        public bool SetSweep(string sweep)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:SWEep {sweep}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Sweep = sweep;
                Log($"Trigger sweep set to {sweep}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger sweep");
            }

            return success;
        }

        /// <summary>
        /// Set trigger edge source
        /// </summary>
        public bool SetEdgeSource(string source)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:EDGe:SOURce {source}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.EdgeSource = source;
                Log($"Trigger edge source set to {source}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger edge source");
            }

            return success;
        }

        /// <summary>
        /// Set trigger edge slope
        /// </summary>
        public bool SetEdgeSlope(string slope)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:EDGe:SLOPe {slope}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.EdgeSlope = slope;
                Log($"Trigger edge slope set to {slope}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger edge slope");
            }

            return success;
        }

        /// <summary>
        /// Set trigger coupling
        /// </summary>
        public bool SetCoupling(string coupling)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:COUPling {coupling}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Coupling = coupling;
                Log($"Trigger coupling set to {coupling}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger coupling");
            }

            return success;
        }

        /// <summary>
        /// Set trigger holdoff
        /// </summary>
        public bool SetHoldoff(double holdoff)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:HOLDoff {holdoff.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Holdoff = holdoff;
                Log($"Trigger holdoff set to {holdoff:E3}s");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger holdoff");
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

        #endregion

        #region Initialization Methods

        /// <summary>
        /// Initialize combo box controls
        /// </summary>
        private void InitializeComboBoxes()
        {
            // Initialize Mode ComboBox
            if (TriggerModeComboBox != null)
            {
                SelectComboBoxItem(TriggerModeComboBox, settings.Mode);
            }

            // Initialize Sweep ComboBox
            if (TriggerSweepComboBox != null)
            {
                SelectComboBoxItem(TriggerSweepComboBox, settings.Sweep);
            }

            // Initialize Edge Source ComboBox
            if (EdgeSourceComboBox != null)
            {
                SelectComboBoxItem(EdgeSourceComboBox, settings.EdgeSource);
            }

            // Initialize Edge Slope ComboBox
            if (EdgeSlopeComboBox != null)
            {
                SelectComboBoxItem(EdgeSlopeComboBox, settings.EdgeSlope);
            }

            // Initialize Coupling ComboBox
            if (TriggerCouplingComboBox != null)
            {
                SelectComboBoxItem(TriggerCouplingComboBox, settings.Coupling);
            }
        }

        /// <summary>
        /// Initialize the trigger level arrow control
        /// UPDATED: Remove fixed GraticuleSize - will be set dynamically
        /// </summary>
        private void InitializeTriggerLevelArrowControl()
        {
            if (TriggerLevelArrows == null) return;

            // REMOVED: Fixed GraticuleSize setting
            // TriggerLevelArrows.GraticuleSize = 0.1; // ❌ DELETE THIS LINE

            // Set initial value
            TriggerLevelArrows.CurrentValue = settings.EdgeLevel;

            // Set reasonable default range (will be updated dynamically)
            TriggerLevelArrows.UpdateRange(-5.0, 5.0);

            Log("Trigger level arrow control initialized (dynamic steps will be set later)");
        }

        /// <summary>
        /// Initialize other controls
        /// </summary>
        private void InitializeOtherControls()
        {
            // Initialize holdoff text box
            if (HoldoffTextBox != null)
            {
                // Convert to nanoseconds and display as a regular number
                double holdoffInNanoseconds = settings.Holdoff * 1000000000;
                HoldoffTextBox.Text = holdoffInNanoseconds.ToString("F2");
            }
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// Update current settings display
        /// </summary>
        private void UpdateCurrentSettingsDisplay()
        {
            if (CurrentTriggerSettingsText == null) return;

            CurrentTriggerSettingsText.Text =
                $"Trigger: {settings.Mode}, {settings.EdgeSource}, " +
                $"{settings.EdgeSlope}, Level: {settings.EdgeLevel:F1}V, {settings.Sweep}";
        }

        /// <summary>
        /// Update arrow control from settings
        /// </summary>
        private void UpdateArrowControlFromSettings()
        {
            if (TriggerLevelArrows != null && !isUpdating)
            {
                TriggerLevelArrows.SetValue(settings.EdgeLevel);
                UpdateLevelValueDisplay();
            }
        }

        /// <summary>
        /// Update level value display
        /// </summary>
        private void UpdateLevelValueDisplay()
        {
            if (LevelValueText == null || TriggerLevelArrows == null) return;

            double value = TriggerLevelArrows.CurrentValue;
            LevelValueText.Text = $"{value:F1}"; // Simple format: "0.0"
        }

        /// <summary>
        /// Update trigger level range
        /// </summary>
        public void UpdateTriggerLevelRange(double minLevel, double maxLevel)
        {
            if (TriggerLevelArrows == null) return;

            // Update arrow control range
            TriggerLevelArrows.UpdateRange(minLevel, maxLevel);

            // Clamp current value to new range if needed
            double currentValue = TriggerLevelArrows.CurrentValue;
            if (currentValue < minLevel || currentValue > maxLevel)
            {
                double clampedValue = Math.Max(minLevel, Math.Min(maxLevel, currentValue));
                HandleTriggerLevelChanged(clampedValue);
            }

            Log($"Trigger level range updated: {minLevel:F1}V to {maxLevel:F1}V");
        }

        #endregion

        #region Settings Management

        /// <summary>
        /// Get current trigger settings
        /// </summary>
        public TriggerSettings GetSettings()
        {
            return settings.Clone();
        }

        /// <summary>
        /// Refresh settings from oscilloscope
        /// </summary>
        public void RefreshSettings()
        {
            if (!oscilloscope.IsConnected) return;

            try
            {
                // Query current settings from oscilloscope
                var modeResponse = oscilloscope.SendQuery(":TRIGger:MODE?");
                if (!string.IsNullOrEmpty(modeResponse))
                {
                    settings.Mode = modeResponse.Trim();
                }

                var sweepResponse = oscilloscope.SendQuery(":TRIGger:SWEep?");
                if (!string.IsNullOrEmpty(sweepResponse))
                {
                    settings.Sweep = sweepResponse.Trim();
                }

                var sourceResponse = oscilloscope.SendQuery(":TRIGger:EDGe:SOURce?");
                if (!string.IsNullOrEmpty(sourceResponse))
                {
                    settings.EdgeSource = sourceResponse.Trim();
                }

                var slopeResponse = oscilloscope.SendQuery(":TRIGger:EDGe:SLOPe?");
                if (!string.IsNullOrEmpty(slopeResponse))
                {
                    settings.EdgeSlope = slopeResponse.Trim();
                }

                var levelResponse = oscilloscope.SendQuery(":TRIGger:EDGe:LEVel?");
                if (double.TryParse(levelResponse?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double levelValue))
                {
                    settings.EdgeLevel = levelValue;
                }

                var couplingResponse = oscilloscope.SendQuery(":TRIGger:COUPling?");
                if (!string.IsNullOrEmpty(couplingResponse))
                {
                    settings.Coupling = couplingResponse.Trim();
                }

                var holdoffResponse = oscilloscope.SendQuery(":TRIGger:HOLDoff?");
                if (double.TryParse(holdoffResponse?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double holdoffValue))
                {
                    settings.Holdoff = holdoffValue;
                }

                // Update UI with refreshed settings
                UpdateUIFromSettings();

                Log("Trigger settings refreshed from oscilloscope");
            }
            catch (Exception ex)
            {
                Log($"Error refreshing trigger settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Update UI from current settings
        /// </summary>
        private void UpdateUIFromSettings()
        {
            if (isUpdating) return;

            DisableEventHandlers();
            isUpdating = true;

            try
            {
                InitializeComboBoxes();
                UpdateArrowControlFromSettings();
                InitializeOtherControls();
                UpdateCurrentSettingsDisplay();
            }
            catch (Exception ex)
            {
                Log($"Error updating trigger UI: {ex.Message}");
            }
            finally
            {
                EnableEventHandlers();
                isUpdating = false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Select combo box item by tag value
        /// </summary>
        private void SelectComboBoxItem(ComboBox comboBox, string tagValue)
        {
            if (comboBox == null || string.IsNullOrEmpty(tagValue)) return;

            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Tag?.ToString().Equals(tagValue, StringComparison.OrdinalIgnoreCase) == true)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        /// <summary>
        /// Enable event handlers
        /// </summary>
        private void EnableEventHandlers()
        {
            // Event handlers will be wired up by the control panel
        }

        /// <summary>
        /// Disable event handlers
        /// </summary>
        private void DisableEventHandlers()
        {
            // Event handlers management handled by control panel
        }

        /// <summary>
        /// Log a message
        /// </summary>
        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }

        #endregion


        // Add these methods to your TriggerController.cs

        #region Dynamic Step Sizing

        /// <summary>
        /// Update trigger level arrows step size based on trigger source channel's vertical scale
        /// UPDATED: Added debugging and force update
        /// </summary>
        public void UpdateTriggerLevelStepSize(Ch1Settings ch1Settings, Ch2Settings ch2Settings)
        {
            if (TriggerLevelArrows == null)
            {
                Log("❌ TriggerLevelArrows is null - cannot update step size");
                return;
            }

            double verticalScale = GetTriggerSourceVerticalScale(ch1Settings, ch2Settings);

            // Log current and new values for debugging
            Log($"🔍 Current GraticuleSize: {TriggerLevelArrows.GraticuleSize}");
            Log($"🔍 New GraticuleSize: {verticalScale}");

            // Major step = 1 × vertical scale (1 division)
            // Minor step = 0.1 × vertical scale (0.1 division) - handled automatically by EmojiArrows
            TriggerLevelArrows.GraticuleSize = verticalScale;

            // Force display update
            TriggerLevelArrows.SetValue(TriggerLevelArrows.CurrentValue);

            Log($"✅ Updated trigger level steps: Major={FormatVoltage(verticalScale)}, Minor={FormatVoltage(verticalScale * 0.1)}");
            Log($"🔍 Confirmed GraticuleSize after update: {TriggerLevelArrows.GraticuleSize}");
        }

        /// <summary>
        /// Get the vertical scale of the current trigger source channel
        /// </summary>
        private double GetTriggerSourceVerticalScale(Ch1Settings ch1Settings, Ch2Settings ch2Settings)
        {
            // Parse trigger source to determine which channel
            string source = settings.EdgeSource?.ToUpper() ?? "CHAN1";

            if (source.Contains("CHAN1") || source.Contains("CHANNEL1"))
            {
                double scale = ch1Settings?.VerticalScale ?? 1.0;
                Log($"🔍 Trigger source: Channel 1, Scale: {FormatVoltage(scale)}/div");
                return scale;
            }
            else if (source.Contains("CHAN2") || source.Contains("CHANNEL2"))
            {
                double scale = ch2Settings?.VerticalScale ?? 1.0;
                Log($"🔍 Trigger source: Channel 2, Scale: {FormatVoltage(scale)}/div");
                return scale;
            }
            else
            {
                // External or unknown source - use reasonable default
                Log($"🔍 Trigger source: External/Unknown ({source}), using default 1V/div");
                return 1.0;
            }
        }

        /// <summary>
        /// Update trigger level range and step size based on current channel settings
        /// </summary>
        public void UpdateTriggerLevelControl(Ch1Settings ch1Settings, Ch2Settings ch2Settings)
        {
            if (TriggerLevelArrows == null) return;

            try
            {
                // Update step size based on trigger source
                UpdateTriggerLevelStepSize(ch1Settings, ch2Settings);

                // Update range based on trigger source
                var (minLevel, maxLevel) = settings.GetTriggerLevelRange(ch1Settings, ch2Settings);
                TriggerLevelArrows.UpdateRange(minLevel, maxLevel);

                // Ensure current level is within new range
                if (settings.EdgeLevel < minLevel || settings.EdgeLevel > maxLevel)
                {
                    double clampedLevel = Math.Max(minLevel, Math.Min(maxLevel, settings.EdgeLevel));
                    TriggerLevelArrows.SetValue(clampedLevel);
                    settings.EdgeLevel = clampedLevel;
                    Log($"⚠️ Trigger level clamped to {FormatVoltage(clampedLevel)} (range: {FormatVoltage(minLevel)} to {FormatVoltage(maxLevel)})");
                }
                else
                {
                    TriggerLevelArrows.SetValue(settings.EdgeLevel);
                }

                UpdateLevelValueDisplay();
                Log($"✅ Trigger level control updated for {settings.EdgeSource}");
            }
            catch (Exception ex)
            {
                Log($"❌ Error updating trigger level control: {ex.Message}");
            }
        }

        /// <summary>
        /// Format voltage value with appropriate units
        /// </summary>
        private string FormatVoltage(double voltage)
        {
            if (Math.Abs(voltage) >= 1000)
                return $"{voltage / 1000:F2}kV";
            else if (Math.Abs(voltage) >= 1.0)
                return $"{voltage:F3}V";
            else if (Math.Abs(voltage) >= 0.001)
                return $"{voltage * 1000:F1}mV";
            else
                return $"{voltage * 1000000:F1}μV";
        }

        #endregion


        #region 4. NEW: Add public method for immediate testing

        /// <summary>
        /// PUBLIC: Force update trigger step size for testing
        /// Call this manually to test dynamic step sizing
        /// </summary>
        public void ForceUpdateStepSize(double testVerticalScale)
        {
            if (TriggerLevelArrows == null)
            {
                Log("❌ Cannot force update - TriggerLevelArrows is null");
                return;
            }

            Log($"🧪 FORCE TEST: Setting step size to {testVerticalScale}V");
            TriggerLevelArrows.GraticuleSize = testVerticalScale;
            Log($"🧪 FORCE TEST: GraticuleSize is now {TriggerLevelArrows.GraticuleSize}");
        }

        #endregion

    }
}