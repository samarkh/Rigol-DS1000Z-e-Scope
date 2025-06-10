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
                // Revert slider to last known good value
                UpdateSliderFromSettings();
            }
        }

        /// <summary>
        /// Update the slider range based on current probe ratio and vertical scale
        /// </summary>
        public void UpdateSliderRange()
        {
            if (VerticalOffsetSlider == null) return;

            // Calculate the valid offset range based on probe ratio and vertical scale
            // According to the manual, the range depends on probe ratio and vertical scale
            double minOffset, maxOffset;

            if (settings.ProbeRatio == 1.0)
            {
                // 1X probe
                if (settings.VerticalScale < 0.5) // <500mV/div
                {
                    minOffset = -2.0;
                    maxOffset = 2.0;
                }
                else if (settings.VerticalScale >= 5.0) // >=5V/div
                {
                    minOffset = -1000.0;
                    maxOffset = 1000.0;
                }
                else // 500mV/div to <5V/div
                {
                    minOffset = -20.0;
                    maxOffset = 20.0;
                }
            }
            else
            {
                // 10X probe (and others)
                if (settings.VerticalScale < 0.5) // <500mV/div
                {
                    minOffset = -20.0;
                    maxOffset = 20.0;
                }
                else if (settings.VerticalScale >= 5.0) // >=5V/div
                {
                    minOffset = -1000.0;
                    maxOffset = 1000.0;
                }
                else // 500mV/div to <5V/div
                {
                    minOffset = -100.0;
                    maxOffset = 100.0;
                }
            }

            isUpdating = true;
            VerticalOffsetSlider.Minimum = minOffset;
            VerticalOffsetSlider.Maximum = maxOffset;

            // Clamp current value to new range
            if (VerticalOffsetSlider.Value < minOffset)
                VerticalOffsetSlider.Value = minOffset;
            else if (VerticalOffsetSlider.Value > maxOffset)
                VerticalOffsetSlider.Value = maxOffset;

            isUpdating = false;

            Log($"Channel 1 slider range updated: {minOffset}V to {maxOffset}V");
        }

        /// <summary>
        /// Update slider value from current settings
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
            }
            else
            {
                Log("Failed to change Channel 1 enable state");
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

                // Update scale options when probe ratio changes
                UpdateVerticalScaleOptions();
                UpdateSliderRange();
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
                Log($"Channel 1 vertical offset set to {offset}V");
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
                string coupling = oscilloscope.SendQuery(":CHANnel1:COUPling?");  // Changed from units

                // Update settings object
                if (!string.IsNullOrEmpty(enableState))
                {
                    settings.IsEnabled = enableState.Trim() == "1";
                }

                if (!string.IsNullOrEmpty(probeRatio) && double.TryParse(probeRatio, NumberStyles.Float, CultureInfo.InvariantCulture, out double probe))
                {
                    settings.ProbeRatio = probe;
                }

                if (!string.IsNullOrEmpty(verticalScale) && double.TryParse(verticalScale, NumberStyles.Float, CultureInfo.InvariantCulture, out double scale))
                {
                    settings.VerticalScale = scale;
                }

                if (!string.IsNullOrEmpty(verticalOffset) && double.TryParse(verticalOffset, NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
                {
                    settings.VerticalOffset = offset;
                }

                if (!string.IsNullOrEmpty(coupling))
                {
                    settings.Coupling = coupling.Trim();
                }

                // Update UI controls
                UpdateUIFromSettings();
                Log("Channel 1 settings updated from oscilloscope");
            }
            catch (Exception ex)
            {
                Log($"Error querying Channel 1 settings: {ex.Message}");
            }
            finally
            {
                EnableEventHandlers();
                isUpdating = false;
            }
        }

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

                // Update enable checkbox
                if (EnableCheckBox != null)
                {
                    EnableCheckBox.IsChecked = settings.IsEnabled;
                }

                // Update probe ratio
                if (ProbeRatioComboBox != null)
                {
                    foreach (ComboBoxItem item in ProbeRatioComboBox.Items)
                    {
                        if (double.TryParse(item.Tag.ToString(), out double itemProbe) &&
                            Math.Abs(itemProbe - settings.ProbeRatio) < 0.001)
                        {
                            ProbeRatioComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update vertical scale options and selection
                UpdateVerticalScaleOptions();
                if (VerticalScaleComboBox != null)
                {
                    foreach (ComboBoxItem item in VerticalScaleComboBox.Items)
                    {
                        if (double.TryParse(item.Tag.ToString(), out double itemScale) &&
                            Math.Abs(itemScale - settings.VerticalScale) < 0.0001)
                        {
                            VerticalScaleComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update vertical offset slider
                UpdateSliderRange();
                if (VerticalOffsetSlider != null)
                {
                    VerticalOffsetSlider.Value = settings.VerticalOffset;
                    UpdateSliderValueDisplay();
                }

                // Update coupling
                if (CouplingComboBox != null)
                {
                    string couplingUpper = settings.Coupling.ToUpper();
                    foreach (ComboBoxItem item in CouplingComboBox.Items)
                    {
                        if (item.Tag.ToString().ToUpper() == couplingUpper)
                        {
                            CouplingComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update current settings display
                UpdateCurrentSettingsDisplay();

                Log($"Updated Channel 1 UI from settings: {settings}");
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
        /// Update settings object from provided settings and then update UI
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

        /// <summary>
        /// Update the vertical scale options based on current probe ratio
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

            // Update the value display immediately
            if (SliderValueText != null)
            {
                SliderValueText.Text = $"{newValue:F3} V";
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
            if (CouplingComboBox != null)  // Changed from UnitsComboBox
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
            if (CouplingComboBox != null)  // Changed from UnitsComboBox
                CouplingComboBox.SelectionChanged += OnCouplingChanged;
            if (VerticalOffsetSlider != null)
                VerticalOffsetSlider.ValueChanged += OnVerticalOffsetSliderChanged;
        }

        #endregion

        #region Enhanced UI Control References
        /// <summary>
        /// Additional UI control references for enhanced features
        /// Set these from the UserControl
        /// </summary>
        public TextBlock MaxValueDisplay { get; set; }
        public TextBlock MinValueDisplay { get; set; }
        public TextBlock OffsetRangeText { get; set; }
        public TextBlock PercentageDisplay { get; set; }
        public Button QuickZeroButton { get; set; }

        #endregion

        #region Enhanced UI Support Methods

        /// <summary>
        /// Enhanced UpdateSliderRange with better scaling and display updates
        /// </summary>
        public void UpdateSliderRangeEnhanced()
        {
            if (VerticalOffsetSlider == null) return;

            var (minOffset, maxOffset) = settings.GetOffsetRange();

            isUpdating = true;

            // Set the range
            VerticalOffsetSlider.Minimum = minOffset;
            VerticalOffsetSlider.Maximum = maxOffset;

            // Calculate smart tick frequency based on range
            double range = maxOffset - minOffset;
            double tickFreq;

            if (range <= 4) tickFreq = 0.2;    // For ±2V range
            else if (range <= 40) tickFreq = 2;      // For ±20V range  
            else if (range <= 200) tickFreq = 20;     // For ±100V range
            else tickFreq = 200;    // For ±1000V range

            VerticalOffsetSlider.TickFrequency = tickFreq;

            // Clamp current value to new range
            if (VerticalOffsetSlider.Value < minOffset)
                VerticalOffsetSlider.Value = minOffset;
            else if (VerticalOffsetSlider.Value > maxOffset)
                VerticalOffsetSlider.Value = maxOffset;

            isUpdating = false;

            Log($"Channel slider range updated: {minOffset:F1}V to {maxOffset:F1}V (ticks: {tickFreq})");
        }

        /// <summary>
        /// Update all UI elements when settings change
        /// </summary>
        public void UpdateAllUIElements()
        {
            UpdateSliderRangeEnhanced();
            UpdateCurrentSettingsDisplay();
        }

        #endregion

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

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            DisableEventHandlers();
        }
    }
}