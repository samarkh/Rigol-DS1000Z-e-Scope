using DS1000Z_E_USB_Control.Channels.Ch1;
using Rigol_DS1000Z_E_Control;
using System;
using System.Globalization;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Channels.Ch2
{
    /// <summary>
    /// Controller class for Channel 2 operations and UI management
    /// Complete implementation based on Ch1Controller pattern
    /// </summary>
    public class Ch2Controller
    {
        private readonly RigolDS1000ZE oscilloscope;
        private readonly Ch2Settings settings;
        private bool isUpdating = false;

        public event EventHandler<string> LogEvent;
        public event EventHandler SettingsChanged;

        // UI Control references
        public CheckBox EnableCheckBox { get; set; }
        public ComboBox ProbeRatioComboBox { get; set; }
        public ComboBox VerticalScaleComboBox { get; set; }
        public ComboBox UnitsComboBox { get; set; }
        public TextBlock CurrentSettingsTextBlock { get; set; }
        public Slider VerticalOffsetSlider { get; set; }
        public TextBlock SliderValueText { get; set; }

        public Ch2Controller(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope;
            this.settings = new Ch2Settings();
        }

        /// <summary>
        /// Handle vertical offset changes from slider or other sources
        /// </summary>
        public void HandleVerticalOffsetChanged(double offset)
        {
            if (!oscilloscope.IsConnected) return;

            string command = $":CHANnel2:OFFSet {offset.ToString(CultureInfo.InvariantCulture)}";

            if (oscilloscope.SendCommand(command))
            {
                settings.VerticalOffset = offset;
                Log($"Channel 2 vertical offset set to {offset:F3}V");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 2 vertical offset");
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

            if (UnitsComboBox != null)
            {
                UnitsComboBox.SelectionChanged += OnUnitsChanged;
            }

            if (VerticalOffsetSlider != null)
            {
                VerticalOffsetSlider.ValueChanged += OnVerticalOffsetSliderChanged;
                UpdateSliderRange();
            }
        }

        /// <summary>
        /// Enable or disable Channel 2
        /// </summary>
        public bool SetEnabled(bool enabled)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel2:DISPlay {(enabled ? "ON" : "OFF")}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.IsEnabled = enabled;
                Log($"Channel 2 {(enabled ? "enabled" : "disabled")}");
            }
            else
            {
                Log("Failed to change Channel 2 enable state");
            }

            return success;
        }

        /// <summary>
        /// Set the probe ratio for Channel 2
        /// </summary>
        public bool SetProbeRatio(double ratio)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel2:PROBe {ratio.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.ProbeRatio = ratio;
                Log($"Channel 2 probe ratio set to {ratio}×");

                UpdateVerticalScaleOptions();
                UpdateSliderRange();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 2 probe ratio");
            }

            return success;
        }

        /// <summary>
        /// Set the vertical scale for Channel 1 (or 2)
        /// </summary>
        public bool SetVerticalScale(double scale)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel2:SCALe {scale.ToString(CultureInfo.InvariantCulture)}"; // Change to CHANnel2 for Ch2
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.VerticalScale = scale;
                Log($"Channel 2 vertical scale set to {scale}V/div"); // Change to Channel 2 for Ch2

                // IMPORTANT: Update slider range immediately after scale changes
                UpdateSliderRange();

                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 2 vertical scale"); // Change to Channel 2 for Ch2
            }

            return success;
        }

        /// <summary>
        /// Set the vertical offset for Channel 2
        /// </summary>
        public bool SetVerticalOffset(double offset)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel2:OFFSet {offset.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.VerticalOffset = offset;

                // Update the slider UI immediately
                if (VerticalOffsetSlider != null)
                {
                    isUpdating = true;
                    VerticalOffsetSlider.Value = offset;
                    isUpdating = false;
                }

                // Update the slider value display
                UpdateSliderValueDisplay();

                Log($"Channel 2 vertical offset set to {offset}V");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 2 vertical offset");
            }

            return success;
        }

        /// <summary>
        /// Set the display units for Channel 2
        /// </summary>
        public bool SetUnits(string units)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel2:UNITs {units}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Units = units;
                Log($"Channel 2 display units set to {units}");
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 2 display units");
            }

            return success;
        }

        /// <summary>
        /// Update the slider range based on current probe ratio and vertical scale
        /// </summary>
        public void UpdateSliderRange()
        {
            if (VerticalOffsetSlider == null) return;

            // Use the GetOffsetRange method from settings for consistent range calculation
            var (minOffset, maxOffset) = settings.GetOffsetRange();

            Log($"Updating slider range: scale={settings.VerticalScale}V/div, probe={settings.ProbeRatio}×, range={minOffset}V to {maxOffset}V");

            isUpdating = true;
            VerticalOffsetSlider.Minimum = minOffset;
            VerticalOffsetSlider.Maximum = maxOffset;

            // Clamp current value to new range
            if (VerticalOffsetSlider.Value < minOffset)
                VerticalOffsetSlider.Value = minOffset;
            else if (VerticalOffsetSlider.Value > maxOffset)
                VerticalOffsetSlider.Value = maxOffset;

            isUpdating = false;

            Log($"Channel slider range updated: {VerticalOffsetSlider.Minimum}V to {VerticalOffsetSlider.Maximum}V");
        }

        /// <summary>
        /// Query all Channel 2 settings from the oscilloscope
        /// </summary>
        public void QueryAndUpdateSettings()
        {
            if (!oscilloscope.IsConnected || isUpdating) return;

            try
            {
                isUpdating = true;
                DisableEventHandlers();

                string enableState = oscilloscope.SendQuery(":CHANnel2:DISPlay?");
                string probeRatio = oscilloscope.SendQuery(":CHANnel2:PROBe?");
                string verticalScale = oscilloscope.SendQuery(":CHANnel2:SCALe?");
                string verticalOffset = oscilloscope.SendQuery(":CHANnel2:OFFSet?");
                string units = oscilloscope.SendQuery(":CHANnel2:UNITs?");

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

                if (!string.IsNullOrEmpty(units))
                {
                    settings.Units = units.Trim();
                }

                UpdateUIFromSettings();
                Log("Channel 2 settings updated from oscilloscope");
            }
            catch (Exception ex)
            {
                Log($"Error querying Channel 2 settings: {ex.Message}");
            }
            finally
            {
                EnableEventHandlers();
                isUpdating = false;
            }
        }

      #region Private Helper Methods

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

        private void UpdateSliderValueDisplay()
        {
            if (SliderValueText != null && VerticalOffsetSlider != null)
            {
                SliderValueText.Text = $"{VerticalOffsetSlider.Value:F3} V";
            }
        }

        private void UpdateUIFromSettings()
        {
            if (EnableCheckBox != null)
            {
                EnableCheckBox.IsChecked = settings.IsEnabled;
            }

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

            if (VerticalOffsetSlider != null)
            {
                VerticalOffsetSlider.Value = settings.VerticalOffset;
                UpdateSliderValueDisplay();
            }

            if (UnitsComboBox != null)
            {
                string unitsUpper = settings.Units.ToUpper();
                foreach (ComboBoxItem item in UnitsComboBox.Items)
                {
                    if (item.Tag.ToString().ToUpper() == unitsUpper)
                    {
                        UnitsComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            UpdateCurrentSettingsDisplay();
        }

        public void UpdateVerticalScaleOptions()
        {
            if (VerticalScaleComboBox == null) return;

            var scaleOptions = Ch2Settings.GetScaleOptionsForProbeRatio(settings.ProbeRatio);

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

        private void UpdateCurrentSettingsDisplay()
        {
            if (CurrentSettingsTextBlock != null)
            {
                double range = settings.VerticalScale * 8;
                CurrentSettingsTextBlock.Text = $"Current: Scale={settings.VerticalScale:F3}V/div, Offset={settings.VerticalOffset:F3}V, Range={range:F1}V";
            }
        }

        private void PopulateProbeRatioOptions()
        {
            if (ProbeRatioComboBox == null) return;

            var probeOptions = Ch2Settings.GetProbeRatioOptions();

            ProbeRatioComboBox.Items.Clear();
            foreach (var option in probeOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = option.display,
                    Tag = option.value.ToString(CultureInfo.InvariantCulture)
                };
                ProbeRatioComboBox.Items.Add(item);

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

        private void OnUnitsChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = UnitsComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                SetUnits(selectedItem.Tag.ToString());
            }
        }

        private void OnVerticalOffsetSliderChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdating) return;

            double newValue = e.NewValue;

            if (SliderValueText != null)
            {
                SliderValueText.Text = $"{newValue:F3} V";
            }

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
            if (UnitsComboBox != null)
                UnitsComboBox.SelectionChanged -= OnUnitsChanged;
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
            if (UnitsComboBox != null)
                UnitsComboBox.SelectionChanged += OnUnitsChanged;
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

            Log($"Channel 2 slider range updated: {minOffset:F1}V to {maxOffset:F1}V (ticks: {tickFreq})");
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


        #region Public API

        public Ch2Settings GetSettings()
        {
            return settings.Clone();
        }

        public void SetSettings(Ch2Settings newSettings)
        {
            if (newSettings == null) return;

            SetEnabled(newSettings.IsEnabled);
            SetProbeRatio(newSettings.ProbeRatio);
            SetVerticalScale(newSettings.VerticalScale);
            SetVerticalOffset(newSettings.VerticalOffset);
            SetUnits(newSettings.Units);
        }

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