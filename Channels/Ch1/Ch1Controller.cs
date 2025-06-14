using DS1000Z_E_USB_Control.Channels.Ch2;
using Rigol_DS1000Z_E_Control;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Channels.Ch1
{
    /// <summary>
    /// Controller class for Channel 1 operations and UI management
    /// </summary>
    public class Ch1Controller
    {
        private readonly RigolDS1000ZE oscilloscope;
        private readonly Ch1Settings settings;
        private bool isUpdating = false;

        public event EventHandler<string> LogEvent;
        public event EventHandler SettingsChanged;

        // UI Control references
        public CheckBox EnableCheckBox { get; set; }
        public ComboBox ProbeRatioComboBox { get; set; }
        public ComboBox VerticalScaleComboBox { get; set; }
        public ComboBox CouplingComboBox { get; set; }  // Changed from UnitsComboBox
        public TextBlock CurrentSettingsTextBlock { get; set; }
        public Slider VerticalOffsetSlider { get; set; }
        public TextBlock SliderValueText { get; set; }

        public Ch1Controller(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope;
            this.settings = new Ch1Settings();
        }

        /// <summary>
        /// Handle vertical offset changes from slider or other sources
        /// </summary>
        public void HandleVerticalOffsetChanged(double offset)
        {
            if (!oscilloscope.IsConnected) return;

            string command = $":CHANnel1:OFFSet {offset.ToString(CultureInfo.InvariantCulture)}";

            if (oscilloscope.SendCommand(command))
            {
                settings.VerticalOffset = offset;
                Log($"Channel 1 vertical offset set to {offset:F3}V");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 vertical offset");
                UpdateSliderFromSettings();
            }
        }

        /// <summary>
        /// Initialize UI controls and set up event handlers
        /// </summary>
        public void InitializeControls()
        {
            if (ProbeRatioComboBox != null)
            {
                PopulateProbeRatioOptions();
                ProbeRatioComboBox.SelectionChanged += OnProbeRatioChanged;
            }

            if (VerticalScaleComboBox != null)
            {
                UpdateVerticalScaleOptions();
                VerticalScaleComboBox.SelectionChanged += OnVerticalScaleChanged;
            }

            if (EnableCheckBox != null)
            {
                EnableCheckBox.Checked += OnEnableChanged;
                EnableCheckBox.Unchecked += OnEnableChanged;
            }

            if (CouplingComboBox != null)  // Changed from UnitsComboBox
            {
                CouplingComboBox.SelectionChanged += OnCouplingChanged;
            }

            if (VerticalOffsetSlider != null)
            {
                VerticalOffsetSlider.ValueChanged += OnVerticalOffsetSliderChanged;
                UpdateSliderRange();
            }
        }

        /// <summary>
        /// Enable or disable Channel 1
        /// </summary>
        public bool SetEnabled(bool enabled)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel1:DISPlay {(enabled ? "ON" : "OFF")}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.IsEnabled = enabled;
                Log($"Channel 1 {(enabled ? "enabled" : "disabled")}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 enable state");
            }

            return success;
        }

        /// <summary>
        /// Set the probe ratio for Channel 1
        /// </summary>
        public bool SetProbeRatio(double ratio)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel1:PROBe {ratio.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.ProbeRatio = ratio;
                Log($"Channel 1 probe ratio set to {ratio}×");
                UpdateVerticalScaleOptions();
                UpdateSliderRange();
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 probe ratio");
            }

            return success;
        }

        /// <summary>
        /// Set the vertical scale for Channel 1
        /// </summary>
        public bool SetVerticalScale(double scale)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel1:SCALe {scale.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.VerticalScale = scale;
                Log($"Channel 1 vertical scale set to {scale}V/div");
                UpdateSliderRange();
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 vertical scale");
            }

            return success;
        }

        /// <summary>
        /// Set the vertical offset for Channel 1
        /// </summary>
        public bool SetVerticalOffset(double offset)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel1:OFFSet {offset.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.VerticalOffset = offset;
                Log($"Channel 1 vertical offset set to {offset:F3}V");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 vertical offset");
            }

            return success;
        }

        /// <summary>
        /// Set the input coupling for Channel 1
        /// </summary>
        public bool SetCoupling(string coupling)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel1:COUPling {coupling}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Coupling = coupling;
                Log($"Channel 1 input coupling set to {coupling}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 input coupling");
            }

            return success;
        }

        /// <summary>
        /// Refresh settings from the oscilloscope
        /// </summary>
        public void RefreshSettings()
        {
            if (!oscilloscope.IsConnected) return;

            try
            {
                isUpdating = true;

                // Query current channel settings
                string enableQuery = oscilloscope.SendQuery(":CHANnel1:DISPlay?");
                string scaleQuery = oscilloscope.SendQuery(":CHANnel1:SCALe?");
                string offsetQuery = oscilloscope.SendQuery(":CHANnel1:OFFSet?");
                string couplingQuery = oscilloscope.SendQuery(":CHANnel1:COUPling?");
                string probeQuery = oscilloscope.SendQuery(":CHANnel1:PROBe?");

                // Update UI controls if they exist
                if (EnableCheckBox != null && bool.TryParse(enableQuery?.Trim(), out bool enabled))
                    EnableCheckBox.IsChecked = enabled;

                if (VerticalScaleComboBox != null && !string.IsNullOrEmpty(scaleQuery))
                    SelectComboBoxItem(VerticalScaleComboBox, scaleQuery.Trim());

                if (CouplingComboBox != null && !string.IsNullOrEmpty(couplingQuery))
                    SelectComboBoxItem(CouplingComboBox, couplingQuery.Trim());

                if (ProbeRatioComboBox != null && !string.IsNullOrEmpty(probeQuery))
                    SelectComboBoxItem(ProbeRatioComboBox, probeQuery.Trim());

                // Update offset slider if present
                if (VerticalOffsetSlider != null && !string.IsNullOrEmpty(offsetQuery))
                {
                    if (double.TryParse(offsetQuery.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
                    {
                        VerticalOffsetSlider.Value = offset;
                        settings.VerticalOffset = offset;
                    }
                }

                SettingsChanged?.Invoke(this, EventArgs.Empty);
                LogEvent?.Invoke(this, "Channel 1 settings refreshed from oscilloscope");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error refreshing Channel 1 settings: {ex.Message}");
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Apply a preset configuration to Channel 1
        /// </summary>
        public void ApplyPreset(string presetName)
        {
            if (!oscilloscope.IsConnected) return;

            try
            {
                // Define some common presets
                switch (presetName.ToLower())
                {
                    case "default":
                        oscilloscope.SendCommand(":CHANnel1:DISPlay ON");
                        oscilloscope.SendCommand(":CHANnel1:SCALe 1");
                        oscilloscope.SendCommand(":CHANnel1:OFFSet 0");
                        oscilloscope.SendCommand(":CHANnel1:COUPling DC");
                        oscilloscope.SendCommand(":CHANnel1:PROBe 1");
                        break;

                    case "high_voltage":
                        oscilloscope.SendCommand(":CHANnel1:DISPlay ON");
                        oscilloscope.SendCommand(":CHANnel1:SCALe 10");
                        oscilloscope.SendCommand(":CHANnel1:OFFSet 0");
                        oscilloscope.SendCommand(":CHANnel1:COUPling DC");
                        oscilloscope.SendCommand(":CHANnel1:PROBe 10");
                        break;

                    case "low_voltage":
                        oscilloscope.SendCommand(":CHANnel1:DISPlay ON");
                        oscilloscope.SendCommand(":CHANnel1:SCALe 0.1");
                        oscilloscope.SendCommand(":CHANnel1:OFFSet 0");
                        oscilloscope.SendCommand(":CHANnel1:COUPling DC");
                        oscilloscope.SendCommand(":CHANnel1:PROBe 1");
                        break;

                    case "ac_coupled":
                        oscilloscope.SendCommand(":CHANnel1:DISPlay ON");
                        oscilloscope.SendCommand(":CHANnel1:SCALe 1");
                        oscilloscope.SendCommand(":CHANnel1:OFFSet 0");
                        oscilloscope.SendCommand(":CHANnel1:COUPling AC");
                        oscilloscope.SendCommand(":CHANnel1:PROBe 1");
                        break;

                    default:
                        LogEvent?.Invoke(this, $"Unknown preset: {presetName}");
                        return;
                }

                RefreshSettings(); // Update UI after applying preset
                LogEvent?.Invoke(this, $"Applied preset '{presetName}' to Channel 1");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error applying preset '{presetName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Get current channel settings as an object
        /// </summary>
        public object GetCurrentSettings()
        {
            if (!oscilloscope.IsConnected) return null;

            try
            {
                return new
                {
                    Enabled = EnableCheckBox?.IsChecked ?? false,
                    Scale = VerticalScaleComboBox?.SelectedItem?.ToString(),
                    Coupling = CouplingComboBox?.SelectedItem?.ToString(),
                    ProbeRatio = ProbeRatioComboBox?.SelectedItem?.ToString(),
                    Offset = VerticalOffsetSlider?.Value ?? 0.0,
                    Settings = settings
                };
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error getting current settings: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Query all Channel 1 settings from the oscilloscope
        /// </summary>
        public void QueryAndUpdateSettings()
        {
            if (!oscilloscope.IsConnected || isUpdating) return;

            try
            {
                isUpdating = true;
                DisableEventHandlers();

                // Query current settings
                string enableState = oscilloscope.SendQuery(":CHANnel1:DISPlay?");
                string probeRatio = oscilloscope.SendQuery(":CHANnel1:PROBe?");
                string verticalScale = oscilloscope.SendQuery(":CHANnel1:SCALe?");
                string verticalOffset = oscilloscope.SendQuery(":CHANnel1:OFFSet?");
                string coupling = oscilloscope.SendQuery(":CHANnel1:COUPling?");

                // Parse and update settings
                if (bool.TryParse(enableState?.Trim(), out bool enabled))
                    settings.IsEnabled = enabled;

                if (double.TryParse(probeRatio?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double probe))
                    settings.ProbeRatio = probe;

                if (double.TryParse(verticalScale?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double scale))
                    settings.VerticalScale = scale;

                if (double.TryParse(verticalOffset?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
                    settings.VerticalOffset = offset;

                if (!string.IsNullOrEmpty(coupling))
                    settings.Coupling = coupling.Trim();

                // Update UI from settings
                UpdateUIFromSettings();
                Log($"Channel 1 settings updated from oscilloscope: {settings}");
            }
            catch (Exception ex)
            {
                Log($"Error updating Channel 1 UI: {ex.Message}");
            }
            finally
            {
                EnableEventHandlers();
                isUpdating = false;
            }
        }

        #region UI Update Methods

        /// <summary>
        /// Update UI controls from current settings
        /// </summary>
        private void UpdateUIFromSettings()
        {
            if (isUpdating) return;

            try
            {
                isUpdating = true;

                // Update checkbox
                if (EnableCheckBox != null)
                    EnableCheckBox.IsChecked = settings.IsEnabled;

                // Update probe ratio combo
                if (ProbeRatioComboBox != null)
                    SelectComboBoxByValue(ProbeRatioComboBox, settings.ProbeRatio.ToString(CultureInfo.InvariantCulture));

                // Update vertical scale combo
                if (VerticalScaleComboBox != null)
                    SelectComboBoxByValue(VerticalScaleComboBox, settings.VerticalScale.ToString(CultureInfo.InvariantCulture));

                // Update coupling combo
                if (CouplingComboBox != null)
                    SelectComboBoxByValue(CouplingComboBox, settings.Coupling);

                // Update slider
                if (VerticalOffsetSlider != null)
                    VerticalOffsetSlider.Value = settings.VerticalOffset;

                UpdateCurrentSettingsDisplay();
                UpdateSliderValueDisplay();
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Update slider range based on current vertical scale and probe ratio
        /// </summary>
        private void UpdateSliderRange()
        {
            if (VerticalOffsetSlider == null || isUpdating) return;

            isUpdating = true;

            // Calculate maximum offset based on scale and probe ratio
            double maxOffset = settings.VerticalScale * settings.ProbeRatio * 5.0; // ±5 divisions
            double minOffset = -maxOffset;

            VerticalOffsetSlider.Minimum = minOffset;
            VerticalOffsetSlider.Maximum = maxOffset;

            // Set tick frequency based on scale
            double tickFreq;
            if (maxOffset <= 1.0) tickFreq = 0.01;      // For ±1V range
            else if (maxOffset <= 10.0) tickFreq = 0.1; // For ±10V range  
            else if (maxOffset <= 100.0) tickFreq = 1.0; // For ±100V range
            else tickFreq = 10.0;    // For ±1000V range

            VerticalOffsetSlider.TickFrequency = tickFreq;

            // Clamp current value to new range
            if (VerticalOffsetSlider.Value < minOffset)
                VerticalOffsetSlider.Value = minOffset;
            else if (VerticalOffsetSlider.Value > maxOffset)
                VerticalOffsetSlider.Value = maxOffset;

            isUpdating = false;

            Log($"Channel 1 slider range updated: {minOffset:F1}V to {maxOffset:F1}V (ticks: {tickFreq})");
        }

        /// <summary>
        /// Update the slider value display text
        /// </summary>
        private void UpdateSliderValueDisplay()
        {
            if (SliderValueText != null && VerticalOffsetSlider != null)
            {
                SliderValueText.Text = $"{VerticalOffsetSlider.Value:F3} V";
            }
        }

        /// <summary>
        /// Update vertical scale options based on probe ratio
        /// </summary>
        public void UpdateVerticalScaleOptions()
        {
            if (VerticalScaleComboBox == null) return;

            var scaleOptions = Ch1Settings.GetScaleOptionsForProbeRatio(settings.ProbeRatio);

            VerticalScaleComboBox.Items.Clear();
            foreach (var option in scaleOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = option.display,
                    Tag = option.value.ToString(CultureInfo.InvariantCulture)
                };
                VerticalScaleComboBox.Items.Add(item);
            }

            // Select 1V/div as default if nothing is selected
            if (VerticalScaleComboBox.SelectedItem == null)
            {
                foreach (ComboBoxItem item in VerticalScaleComboBox.Items)
                {
                    if (item.Tag.ToString() == "1")
                    {
                        VerticalScaleComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Update the current settings display text
        /// </summary>
        private void UpdateCurrentSettingsDisplay()
        {
            if (CurrentSettingsTextBlock != null)
            {
                double range = settings.VerticalScale * 8; // 8 divisions
                CurrentSettingsTextBlock.Text = $"Current: Scale={settings.VerticalScale:F3}V/div, Offset={settings.VerticalOffset:F3}V, Range={range:F1}V, Coupling={settings.Coupling}";
            }
        }

        /// <summary>
        /// Populate probe ratio options
        /// </summary>
        private void PopulateProbeRatioOptions()
        {
            if (ProbeRatioComboBox == null) return;

            var probeOptions = Ch1Settings.GetProbeRatioOptions();

            ProbeRatioComboBox.Items.Clear();
            foreach (var option in probeOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = option.display,
                    Tag = option.value.ToString(CultureInfo.InvariantCulture)
                };
                ProbeRatioComboBox.Items.Add(item);

                // Select 10× as default
                if (option.value == 10.0)
                {
                    ProbeRatioComboBox.SelectedItem = item;
                }
            }
        }

        #endregion

        #region Event Handlers

        private void OnEnableChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (isUpdating) return;

            bool enabled = EnableCheckBox?.IsChecked ?? false;
            if (!SetEnabled(enabled))
            {
                // Revert on failure
                isUpdating = true;
                if (EnableCheckBox != null)
                    EnableCheckBox.IsChecked = !enabled;
                isUpdating = false;
            }
        }

        private void OnProbeRatioChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = ProbeRatioComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null && double.TryParse(selectedItem.Tag.ToString(), out double ratio))
            {
                SetProbeRatio(ratio);
            }
        }

        private void OnVerticalScaleChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = VerticalScaleComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null && double.TryParse(selectedItem.Tag.ToString(), out double scale))
            {
                SetVerticalScale(scale);
            }
        }

        private void OnCouplingChanged(object sender, SelectionChangedEventArgs e)  // Changed from OnUnitsChanged
        {
            if (isUpdating) return;

            var selectedItem = CouplingComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                SetCoupling(selectedItem.Tag.ToString());
            }
        }

        private void OnVerticalOffsetSliderChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdating) return;

            double newValue = e.NewValue;
            HandleVerticalOffsetChanged(newValue);
            UpdateSliderValueDisplay();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Helper method to select an item in a ComboBox by value
        /// </summary>
        private void SelectComboBoxItem(ComboBox comboBox, string value)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i].ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// Helper method to select an item in a ComboBox by tag value
        /// </summary>
        private void SelectComboBoxByValue(ComboBox comboBox, string tagValue)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Tag?.ToString().Equals(tagValue, StringComparison.OrdinalIgnoreCase) == true)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// Disable event handlers during updates
        /// </summary>
        private void DisableEventHandlers()
        {
            if (EnableCheckBox != null)
            {
                EnableCheckBox.Checked -= OnEnableChanged;
                EnableCheckBox.Unchecked -= OnEnableChanged;
            }

            if (ProbeRatioComboBox != null)
                ProbeRatioComboBox.SelectionChanged -= OnProbeRatioChanged;

            if (VerticalScaleComboBox != null)
                VerticalScaleComboBox.SelectionChanged -= OnVerticalScaleChanged;

            if (CouplingComboBox != null)
                CouplingComboBox.SelectionChanged -= OnCouplingChanged;

            if (VerticalOffsetSlider != null)
                VerticalOffsetSlider.ValueChanged -= OnVerticalOffsetSliderChanged;
        }

        /// <summary>
        /// Enable event handlers after updates
        /// </summary>
        private void EnableEventHandlers()
        {
            if (EnableCheckBox != null)
            {
                EnableCheckBox.Checked += OnEnableChanged;
                EnableCheckBox.Unchecked += OnEnableChanged;
            }

            if (ProbeRatioComboBox != null)
                ProbeRatioComboBox.SelectionChanged += OnProbeRatioChanged;

            if (VerticalScaleComboBox != null)
                VerticalScaleComboBox.SelectionChanged += OnVerticalScaleChanged;

            if (CouplingComboBox != null)
                CouplingComboBox.SelectionChanged += OnCouplingChanged;

            if (VerticalOffsetSlider != null)
                VerticalOffsetSlider.ValueChanged += OnVerticalOffsetSliderChanged;
        }

        /// <summary>
        /// Update all UI elements when settings change
        /// </summary>
        public void UpdateAllUIElements()
        {
            UpdateSliderRange();
            UpdateCurrentSettingsDisplay();
        }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Get current Channel 1 settings
        /// </summary>
        public Ch1Settings GetSettings()
        {
            return settings.Clone();
        }

        /// <summary>
        /// Set Channel 1 settings (sends commands to oscilloscope)
        /// </summary>
        public void SetSettings(Ch1Settings newSettings)
        {
            if (newSettings == null) return;

            SetEnabled(newSettings.IsEnabled);
            SetProbeRatio(newSettings.ProbeRatio);
            SetVerticalScale(newSettings.VerticalScale);
            SetVerticalOffset(newSettings.VerticalOffset);
            SetCoupling(newSettings.Coupling);  // Changed from SetUnits
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            DisableEventHandlers();
        }

        #endregion

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}