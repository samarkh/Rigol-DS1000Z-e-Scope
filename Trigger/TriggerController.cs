using System;
using System.Globalization;
using System.Windows.Controls;
using Rigol_DS1000Z_E_Control;
using DS1000Z_E_USB_Control.Controls;

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
        public TextBox HoldoffTextBox { get; set; }
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
        /// </summary>
        private void InitializeTriggerLevelArrowControl()
        {
            if (TriggerLevelArrows == null) return;

            // Set up arrow control properties
            TriggerLevelArrows.GraticuleSize = 0.1; // 0.1V per step
            TriggerLevelArrows.CurrentValue = settings.EdgeLevel;

            // Set reasonable default range
            TriggerLevelArrows.UpdateRange(-5.0, 5.0);

            Log("Trigger level arrow control initialized");
        }

        /// <summary>
        /// Initialize other controls
        /// </summary>
        private void InitializeOtherControls()
        {
            // Initialize holdoff text box
            if (HoldoffTextBox != null)
            {
                HoldoffTextBox.Text = settings.Holdoff.ToString("E3");
            }

            // Initialize level value display
            UpdateLevelValueDisplay();
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
    }
}