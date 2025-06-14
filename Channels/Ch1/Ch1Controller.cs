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

        #region UI Control References
        // Basic UI Control references
        public CheckBox EnableCheckBox { get; set; }
        public ComboBox ProbeRatioComboBox { get; set; }
        public ComboBox VerticalScaleComboBox { get; set; }
        public ComboBox CouplingComboBox { get; set; }
        public TextBlock CurrentSettingsTextBlock { get; set; }
        public Slider VerticalOffsetSlider { get; set; }
        public TextBlock SliderValueText { get; set; }

        // Enhanced UI Control References (missing from original implementation)
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
                Log("Failed to change Channel 1 enable state");
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
                UpdateVerticalScaleOptions();
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
                Log($"Channel 1 vertical scale set to {scale}V/div");
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

        private void UpdateVerticalScaleOptions()
        {
            PopulateVerticalScaleOptions();
        }
        #endregion

        #region UI Update Methods

        /// <summary>
        /// Update all UI elements from current settings
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
            double tickFreq = range <= 4 ? 0.2 : range <= 40 ? 2 : range <= 200 ? 20 : 200;
            VerticalOffsetSlider.TickFrequency = tickFreq;

            // Clamp current value to new range
            if (VerticalOffsetSlider.Value < minOffset)
                VerticalOffsetSlider.Value = minOffset;
            else if (VerticalOffsetSlider.Value > maxOffset)
                VerticalOffsetSlider.Value = maxOffset;

            isUpdating = false;

            Log($"Channel 1 slider range updated: {minOffset:F1}V to {maxOffset:F1}V (ticks: {tickFreq})");
        }

        private void UpdateSliderValueDisplay()
        {
            if (SliderValueText != null && VerticalOffsetSlider != null)
            {
                SliderValueText.Text = $"{VerticalOffsetSlider.Value:F3}V";
            }
        }

        private void UpdateCurrentSettingsDisplay()
        {
            if (CurrentSettingsTextBlock != null)
            {
                CurrentSettingsTextBlock.Text = settings.ToString();
            }
        }

        /// <summary>
        /// Update the min/max range displays (for arrow controls)
        /// </summary>
        public void UpdateRangeDisplays()
        {
            if (settings == null) return;

            var (minOffset, maxOffset) = settings.GetOffsetRange();

            if (MaxValueDisplay != null)
            {
                MaxValueDisplay.Text = FormatVoltage(maxOffset);
            }

            if (MinValueDisplay != null)
            {
                MinValueDisplay.Text = FormatVoltage(minOffset);
            }

            if (OffsetRangeText != null)
            {
                string rangeText;
                if (Math.Abs(minOffset) == Math.Abs(maxOffset))
                {
                    rangeText = $"Range: ±{FormatVoltage(maxOffset).Replace("+", "")}";
                }
                else
                {
                    rangeText = $"Range: {FormatVoltage(minOffset)} to {FormatVoltage(maxOffset)}";
                }
                OffsetRangeText.Text = rangeText;
            }
        }

        /// <summary>
        /// Update all UI elements when settings change
        /// </summary>
        public void UpdateAllDisplays()
        {
            UpdateSliderRange();
            UpdateCurrentSettingsDisplay();
            UpdateRangeDisplays();
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

            double newValue = e.NewValue;

            // Update the value display immediately
            if (SliderValueText != null)
            {
                SliderValueText.Text = $"{newValue:F3}V";
            }

            // Send the command to the oscilloscope
            HandleVerticalOffsetChanged(newValue);
        }
        #endregion

        #region Event Handler Management

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