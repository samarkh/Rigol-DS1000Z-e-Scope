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
    /// FIXED: Added missing methods - RefreshSettings, ApplyPreset, GetCurrentSettings
    /// </summary>
    public class Ch1Controller
    {
        private readonly RigolDS1000ZE oscilloscope;
        private readonly Ch1Settings settings;
        private bool isUpdating = false;

        public event EventHandler<string> LogEvent;
        public event EventHandler SettingsChanged;

        #region UI Control References
        // Basic UI Control references
        public CheckBox EnableCheckBox { get; set; }
        public ComboBox ProbeRatioComboBox { get; set; }
        public ComboBox VerticalScaleComboBox { get; set; }
        public ComboBox CouplingComboBox { get; set; }
        public TextBlock CurrentSettingsTextBlock { get; set; }
        public Slider VerticalOffsetSlider { get; set; }
        public TextBlock SliderValueText { get; set; }

        // Enhanced UI Control References
        public TextBlock MaxValueDisplay { get; set; }
        public TextBlock MinValueDisplay { get; set; }
        public TextBlock OffsetRangeText { get; set; }
        public Button QuickZeroButton { get; set; }
        #endregion

        #region Constructor
        public Ch1Controller(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));
            this.settings = new Ch1Settings();
        }
        #endregion

        #region Core Methods

        /// <summary>
        /// Handle vertical offset changes from slider, arrow controls, or other sources
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
                // Revert slider to last known good value
                UpdateSliderFromSettings();
            }
        }

        /// <summary>
        /// Initialize UI controls and set up event handlers
        /// </summary>
        public void InitializeControls()
        {
            PopulateProbeRatioOptions();
            PopulateVerticalScaleOptions();
            PopulateCouplingOptions();
            UpdateUIFromSettings();
            EnableEventHandlers();
        }

        /// <summary>
        /// ADDED: Missing RefreshSettings method - CS1061 fix
        /// </summary>
        public void RefreshSettings()
        {
            try
            {
                Log("Refreshing Channel 1 settings from oscilloscope...");

                // Read current settings from oscilloscope
                string enabledQuery = oscilloscope.SendQuery(":CHANnel1:DISPlay?");
                if (!string.IsNullOrEmpty(enabledQuery))
                {
                    settings.IsEnabled = enabledQuery.Trim().ToUpper() == "ON" || enabledQuery.Trim() == "1";
                }

                string scaleQuery = oscilloscope.SendQuery(":CHANnel1:SCALe?");
                if (!string.IsNullOrEmpty(scaleQuery) && double.TryParse(scaleQuery, NumberStyles.Float, CultureInfo.InvariantCulture, out double scale))
                {
                    settings.VerticalScale = scale;
                }

                string offsetQuery = oscilloscope.SendQuery(":CHANnel1:OFFSet?");
                if (!string.IsNullOrEmpty(offsetQuery) && double.TryParse(offsetQuery, NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
                {
                    settings.VerticalOffset = offset;
                }

                string couplingQuery = oscilloscope.SendQuery(":CHANnel1:COUPling?");
                if (!string.IsNullOrEmpty(couplingQuery))
                {
                    settings.Coupling = couplingQuery.Trim().ToUpper();
                }

                string probeQuery = oscilloscope.SendQuery(":CHANnel1:PROBe?");
                if (!string.IsNullOrEmpty(probeQuery) && double.TryParse(probeQuery, NumberStyles.Float, CultureInfo.InvariantCulture, out double probe))
                {
                    settings.ProbeRatio = probe;
                }

                // Update UI controls with refreshed settings
                UpdateUIFromSettings();

                Log("Channel 1 settings refreshed from oscilloscope");
            }
            catch (Exception ex)
            {
                Log($"Error refreshing Channel 1 settings: {ex.Message}");
            }
        }

        /// <summary>
        /// ADDED: Missing ApplyPreset method - CS1061 fix
        /// </summary>
        public void ApplyPreset(string presetName)
        {
            try
            {
                Log($"Applying preset '{presetName}' to Channel 1...");

                // Define common presets
                switch (presetName.ToLower())
                {
                    case "default":
                        SetEnabled(true);
                        SetProbeRatio(1.0);
                        SetVerticalScale(1.0);
                        SetVerticalOffset(0.0);
                        SetCoupling("DC");
                        break;

                    case "sensitive":
                        SetEnabled(true);
                        SetProbeRatio(1.0);
                        SetVerticalScale(0.01); // 10mV/div
                        SetVerticalOffset(0.0);
                        SetCoupling("DC");
                        break;

                    case "high_voltage":
                        SetEnabled(true);
                        SetProbeRatio(10.0);
                        SetVerticalScale(10.0); // 10V/div
                        SetVerticalOffset(0.0);
                        SetCoupling("DC");
                        break;

                    case "ac_coupled":
                        SetEnabled(true);
                        SetProbeRatio(1.0);
                        SetVerticalScale(1.0);
                        SetVerticalOffset(0.0);
                        SetCoupling("AC");
                        break;

                    default:
                        Log($"Unknown preset: {presetName}");
                        return;
                }

                Log($"Applied preset '{presetName}' to Channel 1");
            }
            catch (Exception ex)
            {
                Log($"Error applying preset '{presetName}': {ex.Message}");
            }
        }

        /// <summary>
        /// ADDED: Missing GetCurrentSettings method - CS1061 fix
        /// </summary>
        public Ch1Settings GetCurrentSettings()
        {
            RefreshSettings(); // Ensure we have latest settings
            return settings.Clone(); // Return a copy
        }

        /// <summary>
        /// Set vertical offset (used by arrow controls and other sources)
        /// </summary>
        public void SetVerticalOffset(double offset)
        {
            HandleVerticalOffsetChanged(offset);
        }

        /// <summary>
        /// Update slider from current settings (missing method from compilation errors)
        /// </summary>
        private void UpdateSliderFromSettings()
        {
            if (VerticalOffsetSlider != null && !isUpdating)
            {
                isUpdating = true;
                VerticalOffsetSlider.Value = settings.VerticalOffset;
                UpdateSliderValueDisplay();
                isUpdating = false;
            }
        }

        /// <summary>
        /// Update settings from provided settings - single argument overload (missing from compilation errors)
        /// </summary>
        public void UpdateFromSettings(Ch1Settings newSettings)
        {
            if (newSettings == null) return;

            // Update internal settings object
            settings.IsEnabled = newSettings.IsEnabled;
            settings.ProbeRatio = newSettings.ProbeRatio;
            settings.VerticalScale = newSettings.VerticalScale;
            settings.VerticalOffset = newSettings.VerticalOffset;
            settings.Coupling = newSettings.Coupling;
            settings.BandwidthLimit = newSettings.BandwidthLimit;
            settings.Units = newSettings.Units;
            settings.InvertEnabled = newSettings.InvertEnabled;
            settings.VernierEnabled = newSettings.VernierEnabled;

            // Update UI to reflect these settings
            UpdateUIFromSettings();
        }
        #endregion

        #region Settings Management

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
            SetCoupling(newSettings.Coupling);
        }

        public bool SetEnabled(bool enabled)
        {
            string command = $":CHANnel1:DISPlay {(enabled ? "ON" : "OFF")}";

            if (oscilloscope.SendCommand(command))
            {
                settings.IsEnabled = enabled;
                Log($"Channel 1 {(enabled ? "enabled" : "disabled")}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set Channel 1 enabled state");
                return false;
            }
        }

        public bool SetProbeRatio(double ratio)
        {
            string command = $":CHANnel1:PROBe {ratio.ToString(CultureInfo.InvariantCulture)}";

            if (oscilloscope.SendCommand(command))
            {
                settings.ProbeRatio = ratio;
                Log($"Channel 1 probe ratio set to {ratio}:1");

                // Update vertical scale options when probe ratio changes
                PopulateVerticalScaleOptions();
                UpdateSliderRange();
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set Channel 1 probe ratio");
                return false;
            }
        }

        public bool SetVerticalScale(double scale)
        {
            string command = $":CHANnel1:SCALe {scale.ToString(CultureInfo.InvariantCulture)}";

            if (oscilloscope.SendCommand(command))
            {
                settings.VerticalScale = scale;
                Log($"Channel 1 vertical scale set to {FormatVoltage(scale)}/div");

                UpdateSliderRange();
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set Channel 1 vertical scale");
                return false;
            }
        }

        public bool SetCoupling(string coupling)
        {
            string command = $":CHANnel1:COUPling {coupling}";

            if (oscilloscope.SendCommand(command))
            {
                settings.Coupling = coupling;
                Log($"Channel 1 coupling set to {coupling}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set Channel 1 coupling");
                return false;
            }
        }
        #endregion

        #region UI Population Methods

        private void PopulateProbeRatioOptions()
        {
            if (ProbeRatioComboBox == null) return;

            ProbeRatioComboBox.Items.Clear();
            var probeOptions = new[] { 1.0, 10.0, 100.0, 1000.0 };

            foreach (var ratio in probeOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = $"{ratio}:1",
                    Tag = ratio
                };
                ProbeRatioComboBox.Items.Add(item);

                if (Math.Abs(ratio - settings.ProbeRatio) < 0.01)
                {
                    ProbeRatioComboBox.SelectedItem = item;
                }
            }
        }

        private void PopulateVerticalScaleOptions()
        {
            if (VerticalScaleComboBox == null) return;

            var scaleOptions = Ch1Settings.GetScaleOptionsForProbeRatio(settings.ProbeRatio);

            VerticalScaleComboBox.Items.Clear();
            foreach (var option in scaleOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = option.display,
                    Tag = option.value
                };
                VerticalScaleComboBox.Items.Add(item);

                if (Math.Abs(option.value - settings.VerticalScale) < 1e-9)
                {
                    VerticalScaleComboBox.SelectedItem = item;
                }
            }
        }

        private void PopulateCouplingOptions()
        {
            if (CouplingComboBox == null) return;

            CouplingComboBox.Items.Clear();
            var couplingOptions = new[] { "DC", "AC", "GND" };

            foreach (var coupling in couplingOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = coupling,
                    Tag = coupling
                };
                CouplingComboBox.Items.Add(item);

                if (coupling == settings.Coupling)
                {
                    CouplingComboBox.SelectedItem = item;
                }
            }
        }
        #endregion

        #region UI Update Methods

        /// <summary>
        /// Update UI controls from current settings WITHOUT sending commands to oscilloscope
        /// </summary>
        public void UpdateUIFromSettings()
        {
            if (isUpdating) return;

            try
            {
                isUpdating = true;
                DisableEventHandlers();

                // Update basic controls
                if (EnableCheckBox != null)
                    EnableCheckBox.IsChecked = settings.IsEnabled;

                // Update ComboBoxes (these methods handle the selection)
                PopulateProbeRatioOptions();
                PopulateVerticalScaleOptions();
                PopulateCouplingOptions();

                // Update slider
                UpdateSliderRange();
                if (VerticalOffsetSlider != null)
                    VerticalOffsetSlider.Value = settings.VerticalOffset;

                UpdateSliderValueDisplay();
                UpdateCurrentSettingsDisplay();
                UpdateRangeDisplays();

                Log($"Channel 1 UI updated from settings: {settings}");
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

        /// <summary>
        /// Update the slider range based on current probe ratio and vertical scale
        /// </summary>
        public void UpdateSliderRange()
        {
            if (VerticalOffsetSlider == null) return;

            var (minOffset, maxOffset) = settings.GetOffsetRange();

            isUpdating = true;
            VerticalOffsetSlider.Minimum = minOffset;
            VerticalOffsetSlider.Maximum = maxOffset;

            // Calculate smart tick frequency
            double range = maxOffset - minOffset;
            double tickFreq = range <= 4 ? 0.2 : range <= 40 ? 2 : range <= 200 ? 20 : 100;
            VerticalOffsetSlider.TickFrequency = tickFreq;

            isUpdating = false;
        }

        private void UpdateSliderValueDisplay()
        {
            if (SliderValueText != null && VerticalOffsetSlider != null)
            {
                SliderValueText.Text = FormatVoltage(VerticalOffsetSlider.Value);
            }
        }

        private void UpdateCurrentSettingsDisplay()
        {
            if (CurrentSettingsTextBlock != null)
            {
                CurrentSettingsTextBlock.Text = $"Ch1: {(settings.IsEnabled ? "ON" : "OFF")}, " +
                    $"{settings.ProbeRatio}:1, {FormatVoltage(settings.VerticalScale)}/div, " +
                    $"Offset: {FormatVoltage(settings.VerticalOffset)}, {settings.Coupling}";
            }
        }

        private void UpdateRangeDisplays()
        {
            var (minOffset, maxOffset) = settings.GetOffsetRange();

            if (MaxValueDisplay != null)
                MaxValueDisplay.Text = FormatVoltage(maxOffset);
            if (MinValueDisplay != null)
                MinValueDisplay.Text = FormatVoltage(minOffset);
            if (OffsetRangeText != null)
            {
                string rangeText;
                if (Math.Abs(minOffset) == Math.Abs(maxOffset))
                {
                    rangeText = $"Range: ±{FormatVoltage(Math.Abs(maxOffset))}";
                }
                else
                {
                    rangeText = $"Range: {FormatVoltage(minOffset)} to {FormatVoltage(maxOffset)}";
                }
                OffsetRangeText.Text = rangeText;
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

        private void OnCouplingChanged(object sender, SelectionChangedEventArgs e)
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

            HandleVerticalOffsetChanged(e.NewValue);
            UpdateSliderValueDisplay();
        }

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
        #endregion

        #region Helper Methods

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }

        /// <summary>
        /// Helper method to format voltage values
        /// </summary>
        private string FormatVoltage(double voltage)
        {
            if (Math.Abs(voltage) >= 1.0)
                return $"{voltage:F3}V";
            else if (Math.Abs(voltage) >= 0.001)
                return $"{voltage * 1000:F1}mV";
            else
                return $"{voltage * 1000000:F1}μV";
        }
        #endregion

        #region Disposal

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            DisableEventHandlers();
        }
        #endregion
    }
}