using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;
using Rigol_DS1000Z_E_Control;

namespace DS1000Z_E_USB_Control.TimeBase
{
    /// <summary>
    /// Controller class for TimeBase operations and UI management
    /// Implements SCPI commands for Rigol DS1000Z-E timebase control
    /// </summary>
    public class TimeBaseController
    {
        private readonly RigolDS1000ZE oscilloscope;
        private readonly TimeBaseSettings settings;
        private bool isUpdating = false;

        public event EventHandler<string> LogEvent;
        public event EventHandler SettingsChanged;

        // UI Control references
        public ComboBox HorizontalScaleComboBox { get; set; }
        public ComboBox DelayedScaleComboBox { get; set; }
        public CheckBox DelayEnabledCheckBox { get; set; }
        public RadioButton MainModeRadioButton { get; set; }
        public RadioButton DelayedModeRadioButton { get; set; }
        public Slider HorizontalOffsetSlider { get; set; }
        public TextBox DelayedOffsetTextBox { get; set; }
        public TextBlock CurrentTimeBaseSettingsText { get; set; }
        public TextBlock CurrentScaleText { get; set; }
        public TextBlock CurrentDelayedScaleText { get; set; }
        public TextBlock OffsetValueText { get; set; }
        public TextBlock TimeWindowText { get; set; }
        public TextBlock SampleRateText { get; set; }

        // Enhanced UI Control references
        public TextBlock MaxOffsetDisplay { get; set; }
        public TextBlock MinOffsetDisplay { get; set; }
        public TextBlock OffsetRangeText { get; set; }
        public TextBlock DelayedOffsetDisplayText { get; set; }
        public TextBlock DelayedStatusText { get; set; }
        public Button AutoScaleButton { get; set; }
        public Button ResetTimeBaseButton { get; set; }
        public Button CenterTimeBaseButton { get; set; }
        public Button QuickZeroOffsetButton { get; set; }

        public TimeBaseController(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope;
            this.settings = new TimeBaseSettings();
        }

        /// <summary>
        /// Handle horizontal offset changes from slider or other sources
        /// </summary>
        public void HandleHorizontalOffsetChanged(double offset)
        {
            if (!oscilloscope.IsConnected) return;

            string command = $":TIMebase:MAIN:OFFSet {offset.ToString(CultureInfo.InvariantCulture)}";

            if (oscilloscope.SendCommand(command))
            {
                settings.MainOffset = offset;
                Log($"TimeBase main offset set to {offset:F6}s");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set TimeBase main offset");
                UpdateSliderFromSettings();
            }
        }

        /// <summary>
        /// Initialize UI controls and set up event handlers
        /// </summary>
        public void InitializeControls()
        {
            if (HorizontalScaleComboBox != null)
            {
                PopulateHorizontalScaleOptions();
                HorizontalScaleComboBox.SelectionChanged += OnHorizontalScaleChanged;
            }

            if (DelayedScaleComboBox != null)
            {
                PopulateDelayedScaleOptions();
                DelayedScaleComboBox.SelectionChanged += OnDelayedScaleChanged;
            }

            if (DelayEnabledCheckBox != null)
            {
                DelayEnabledCheckBox.Checked += OnDelayEnabledChanged;
                DelayEnabledCheckBox.Unchecked += OnDelayEnabledChanged;
            }

            if (MainModeRadioButton != null)
            {
                MainModeRadioButton.Checked += OnModeChanged;
            }

            if (DelayedModeRadioButton != null)
            {
                DelayedModeRadioButton.Checked += OnModeChanged;
            }

            if (HorizontalOffsetSlider != null)
            {
                HorizontalOffsetSlider.ValueChanged += OnHorizontalOffsetSliderChanged;
                UpdateSliderRange();
            }

            if (DelayedOffsetTextBox != null)
            {
                DelayedOffsetTextBox.LostFocus += OnDelayedOffsetTextBoxLostFocus;
            }
        }

        /// <summary>
        /// Set the main horizontal scale (:TIMebase[:MAIN]:SCALe)
        /// </summary>
        public bool SetHorizontalScale(double scale)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TIMebase:MAIN:SCALe {scale.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.MainScale = scale;
                Log($"TimeBase main scale set to {settings.MainScaleDisplay}");
                UpdateSliderRange();
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set TimeBase main scale");
            }

            return success;
        }

        /// <summary>
        /// Set the main horizontal offset (:TIMebase[:MAIN]:OFFSet)
        /// </summary>
        public bool SetHorizontalOffset(double offset)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TIMebase:MAIN:OFFSet {offset.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.MainOffset = offset;
                Log($"TimeBase main offset set to {offset:F6}s");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set TimeBase main offset");
            }

            return success;
        }

        /// <summary>
        /// Enable or disable delayed timebase (:TIMebase:DELay:ENABle)
        /// </summary>
        public bool SetDelayEnabled(bool enabled)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TIMebase:DELay:ENABle {(enabled ? "ON" : "OFF")}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.DelayEnabled = enabled;
                Log($"TimeBase delayed timebase {(enabled ? "enabled" : "disabled")}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set TimeBase delayed timebase enable state");
            }

            return success;
        }

        /// <summary>
        /// Set the delayed timebase scale (:TIMebase:DELay:SCALe)
        /// </summary>
        public bool SetDelayScale(double scale)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TIMebase:DELay:SCALe {scale.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.DelayScale = scale;
                Log($"TimeBase delayed scale set to {settings.DelayScaleDisplay}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set TimeBase delayed scale");
            }

            return success;
        }

        /// <summary>
        /// Set the delayed timebase offset (:TIMebase:DELay:OFFSet)
        /// </summary>
        public bool SetDelayedOffset(double offset)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TIMebase:DELay:OFFSet {offset.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.DelayOffset = offset;
                Log($"TimeBase delayed offset set to {offset:F6}s");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set TimeBase delayed offset");
            }

            return success;
        }

        /// <summary>
        /// Set the timebase mode (:TIMebase:MODE)
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
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set TimeBase mode");
            }

            return success;
        }

        /// <summary>
        /// Execute auto scale function (:AUToscale)
        /// </summary>
        public bool AutoScale()
        {
            if (!oscilloscope.IsConnected) return false;

            bool success = oscilloscope.SendCommand(":AUToscale");

            if (success)
            {
                Log("TimeBase auto scale executed");
                // Query updated settings after auto scale
                System.Threading.Tasks.Task.Delay(500).ContinueWith(t => QueryAndUpdateSettings());
            }
            else
            {
                Log("Failed to execute TimeBase auto scale");
            }

            return success;
        }

        /// <summary>
        /// Reset timebase to default settings
        /// </summary>
        public void ResetToDefaults()
        {
            var defaults = TimeBaseSettings.Presets.GeneralPurpose;
            SetSettings(defaults);
        }

        /// <summary>
        /// Update the slider range based on current main scale
        /// </summary>
        public void UpdateSliderRange()
        {
            if (HorizontalOffsetSlider == null) return;

            var (minOffset, maxOffset) = settings.GetOffsetRange();

            isUpdating = true;
            HorizontalOffsetSlider.Minimum = minOffset;
            HorizontalOffsetSlider.Maximum = maxOffset;

            // Clamp current value to new range
            if (HorizontalOffsetSlider.Value < minOffset)
                HorizontalOffsetSlider.Value = minOffset;
            else if (HorizontalOffsetSlider.Value > maxOffset)
                HorizontalOffsetSlider.Value = maxOffset;

            isUpdating = false;

            Log($"TimeBase slider range updated: {minOffset:E3}s to {maxOffset:E3}s");
        }

        /// <summary>
        /// Query all TimeBase settings from the oscilloscope
        /// </summary>
        public void QueryAndUpdateSettings()
        {
            if (!oscilloscope.IsConnected || isUpdating) return;

            try
            {
                isUpdating = true;
                DisableEventHandlers();

                // Query current settings using SCPI commands
                string mainScale = oscilloscope.SendQuery(":TIMebase:MAIN:SCALe?");
                string mainOffset = oscilloscope.SendQuery(":TIMebase:MAIN:OFFSet?");
                string delayEnabled = oscilloscope.SendQuery(":TIMebase:DELay:ENABle?");
                string delayScale = oscilloscope.SendQuery(":TIMebase:DELay:SCALe?");
                string delayOffset = oscilloscope.SendQuery(":TIMebase:DELay:OFFSet?");
                string mode = oscilloscope.SendQuery(":TIMebase:MODE?");

                // Update settings object
                if (!string.IsNullOrEmpty(mainScale) && double.TryParse(mainScale, NumberStyles.Float, CultureInfo.InvariantCulture, out double scale))
                {
                    settings.MainScale = scale;
                }

                if (!string.IsNullOrEmpty(mainOffset) && double.TryParse(mainOffset, NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
                {
                    settings.MainOffset = offset;
                }

                if (!string.IsNullOrEmpty(delayEnabled))
                {
                    settings.DelayEnabled = delayEnabled.Trim() == "1" || delayEnabled.Trim().ToUpper() == "ON";
                }

                if (!string.IsNullOrEmpty(delayScale) && double.TryParse(delayScale, NumberStyles.Float, CultureInfo.InvariantCulture, out double dScale))
                {
                    settings.DelayScale = dScale;
                }

                if (!string.IsNullOrEmpty(delayOffset) && double.TryParse(delayOffset, NumberStyles.Float, CultureInfo.InvariantCulture, out double dOffset))
                {
                    settings.DelayOffset = dOffset;
                }

                if (!string.IsNullOrEmpty(mode))
                {
                    settings.Mode = mode.Trim();
                }

                // Update UI controls
                UpdateUIFromSettings();
                Log("TimeBase settings updated from oscilloscope");
            }
            catch (Exception ex)
            {
                Log($"Error querying TimeBase settings: {ex.Message}");
            }
            finally
            {
                EnableEventHandlers();
                isUpdating = false;
            }
        }



        // Add these properties to your TimeBaseController class (in the UI Control references section)

        /// <summary>
        /// ComboBox for selecting time base mode (Main/Delayed)
        /// </summary>
        public ComboBox TimeBaseModeComboBox { get; set; }

        /// <summary>
        /// TextBlock for displaying current memory depth
        /// </summary>
        public TextBlock MemoryDepthText { get; set; }

        /// <summary>
        /// TextBlock for displaying acquisition type information
        /// </summary>
        public TextBlock AcquisitionTypeText { get; set; }



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

                // Update horizontal scale selection
                if (HorizontalScaleComboBox != null)
                {
                    foreach (ComboBoxItem item in HorizontalScaleComboBox.Items)
                    {
                        if (double.TryParse(item.Tag.ToString(), out double itemScale) &&
                            Math.Abs(itemScale - settings.MainScale) < 1e-12)
                        {
                            HorizontalScaleComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update delayed scale selection
                if (DelayedScaleComboBox != null)
                {
                    foreach (ComboBoxItem item in DelayedScaleComboBox.Items)
                    {
                        if (double.TryParse(item.Tag.ToString(), out double itemScale) &&
                            Math.Abs(itemScale - settings.DelayScale) < 1e-12)
                        {
                            DelayedScaleComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update delay enabled checkbox
                if (DelayEnabledCheckBox != null)
                {
                    DelayEnabledCheckBox.IsChecked = settings.DelayEnabled;
                }

                // Update mode radio buttons
                if (MainModeRadioButton != null && DelayedModeRadioButton != null)
                {
                    if (settings.Mode.ToUpper() == "MAIN")
                    {
                        MainModeRadioButton.IsChecked = true;
                    }
                    else
                    {
                        DelayedModeRadioButton.IsChecked = true;
                    }
                }

                // Update horizontal offset slider
                UpdateSliderRange();
                if (HorizontalOffsetSlider != null)
                {
                    HorizontalOffsetSlider.Value = settings.MainOffset;
                }

                // Update delayed offset text box
                if (DelayedOffsetTextBox != null)
                {
                    DelayedOffsetTextBox.Text = settings.DelayOffset.ToString("F6");
                }

                // Update current settings display
                UpdateCurrentSettingsDisplay();

                Log($"Updated TimeBase UI from settings: {settings}");
            }
            catch (Exception ex)
            {
                Log($"Error updating TimeBase UI: {ex.Message}");
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
        public void UpdateFromSettings(TimeBaseSettings newSettings)
        {
            if (newSettings == null) return;

            // Update internal settings object
            settings.MainScale = newSettings.MainScale;
            settings.MainOffset = newSettings.MainOffset;
            settings.DelayEnabled = newSettings.DelayEnabled;
            settings.DelayScale = newSettings.DelayScale;
            settings.DelayOffset = newSettings.DelayOffset;
            settings.Mode = newSettings.Mode;

            // Update UI to reflect these settings
            UpdateUIFromSettings();
        }

        #region Private Helper Methods

        private void UpdateSliderFromSettings()
        {
            if (HorizontalOffsetSlider != null && !isUpdating)
            {
                isUpdating = true;
                HorizontalOffsetSlider.Value = settings.MainOffset;
                isUpdating = false;
            }
        }

        private void PopulateHorizontalScaleOptions()
        {
            if (HorizontalScaleComboBox == null) return;

            var scaleOptions = TimeBaseSettings.GetHorizontalScaleOptions();

            HorizontalScaleComboBox.Items.Clear();
            foreach (var option in scaleOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = option.display,
                    Tag = option.value.ToString(CultureInfo.InvariantCulture)
                };
                HorizontalScaleComboBox.Items.Add(item);

                // Select 1ms/div as default
                if (Math.Abs(option.value - 0.001) < 1e-12)
                {
                    HorizontalScaleComboBox.SelectedItem = item;
                }
            }
        }

        private void PopulateDelayedScaleOptions()
        {
            if (DelayedScaleComboBox == null) return;

            var scaleOptions = TimeBaseSettings.GetHorizontalScaleOptions();

            DelayedScaleComboBox.Items.Clear();
            foreach (var option in scaleOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = option.display,
                    Tag = option.value.ToString(CultureInfo.InvariantCulture)
                };
                DelayedScaleComboBox.Items.Add(item);

                // Select 1μs/div as default for delayed
                if (Math.Abs(option.value - 1e-6) < 1e-12)
                {
                    DelayedScaleComboBox.SelectedItem = item;
                }
            }
        }

        private void UpdateCurrentSettingsDisplay()
        {
            if (CurrentTimeBaseSettingsText != null)
            {
                CurrentTimeBaseSettingsText.Text = $"Current: Scale={settings.MainScaleDisplay}, Offset={settings.MainOffset:F6}s, Mode={settings.Mode}, Window={settings.TimeWindow:E3}s, Delayed={settings.DelayEnabled}";
            }

            if (CurrentScaleText != null)
            {
                CurrentScaleText.Text = $"Current: {settings.MainScaleDisplay}";
            }

            if (CurrentDelayedScaleText != null)
            {
                CurrentDelayedScaleText.Text = $"Current: {settings.DelayScaleDisplay}";
            }

            if (TimeWindowText != null)
            {
                TimeWindowText.Text = $"Total Window: {FormatTimeValue(settings.TimeWindow)} (12 divisions × {settings.MainScaleDisplay})";
            }
        }

        private string FormatTimeValue(double time)
        {
            if (time == 0) return "0 s";

            double absTime = Math.Abs(time);
            if (absTime >= 1.0)
                return $"{time:F3} s";
            else if (absTime >= 1e-3)
                return $"{time * 1000:F3} ms";
            else if (absTime >= 1e-6)
                return $"{time * 1000000:F3} μs";
            else if (absTime >= 1e-9)
                return $"{time * 1000000000:F3} ns";
            else
                return $"{time:E3} s";
        }

        #endregion

        #region Event Handlers

        private void OnHorizontalScaleChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = HorizontalScaleComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null && double.TryParse(selectedItem.Tag.ToString(), out double scale))
            {
                SetHorizontalOffset(scale);
            }
        }

        private void OnDelayedScaleChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = DelayedScaleComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null && double.TryParse(selectedItem.Tag.ToString(), out double scale))
            {
                SetDelayScale(scale);
            }
        }

        private void OnDelayEnabledChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (isUpdating) return;

            bool enabled = DelayEnabledCheckBox?.IsChecked ?? false;
            if (!SetDelayEnabled(enabled))
            {
                // Revert on failure
                isUpdating = true;
                if (DelayEnabledCheckBox != null)
                    DelayEnabledCheckBox.IsChecked = !enabled;
                isUpdating = false;
            }
        }

        private void OnModeChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (isUpdating) return;

            string mode = "MAIN";
            if (DelayedModeRadioButton?.IsChecked == true)
            {
                mode = "DELayed";
            }

            SetMode(mode);
        }

        private void OnHorizontalOffsetSliderChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdating) return;

            HandleHorizontalOffsetChanged(e.NewValue);
        }

        private void OnDelayedOffsetTextBoxLostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (isUpdating) return;

            if (double.TryParse(DelayedOffsetTextBox?.Text, out double offset))
            {
                SetDelayedOffset(offset);
            }
        }

        #endregion

        #region Event Handler Management

        private void DisableEventHandlers()
        {
            if (HorizontalScaleComboBox != null)
                HorizontalScaleComboBox.SelectionChanged -= OnHorizontalScaleChanged;
            if (DelayedScaleComboBox != null)
                DelayedScaleComboBox.SelectionChanged -= OnDelayedScaleChanged;
            if (DelayEnabledCheckBox != null)
            {
                DelayEnabledCheckBox.Checked -= OnDelayEnabledChanged;
                DelayEnabledCheckBox.Unchecked -= OnDelayEnabledChanged;
            }
            if (MainModeRadioButton != null)
                MainModeRadioButton.Checked -= OnModeChanged;
            if (DelayedModeRadioButton != null)
                DelayedModeRadioButton.Checked -= OnModeChanged;
            if (HorizontalOffsetSlider != null)
                HorizontalOffsetSlider.ValueChanged -= OnHorizontalOffsetSliderChanged;
            if (DelayedOffsetTextBox != null)
                DelayedOffsetTextBox.LostFocus -= OnDelayedOffsetTextBoxLostFocus;
        }

        private void EnableEventHandlers()
        {
            if (HorizontalScaleComboBox != null)
                HorizontalScaleComboBox.SelectionChanged += OnHorizontalScaleChanged;
            if (DelayedScaleComboBox != null)
                DelayedScaleComboBox.SelectionChanged += OnDelayedScaleChanged;
            if (DelayEnabledCheckBox != null)
            {
                DelayEnabledCheckBox.Checked += OnDelayEnabledChanged;
                DelayEnabledCheckBox.Unchecked += OnDelayEnabledChanged;
            }
            if (MainModeRadioButton != null)
                MainModeRadioButton.Checked += OnModeChanged;
            if (DelayedModeRadioButton != null)
                DelayedModeRadioButton.Checked += OnModeChanged;
            if (HorizontalOffsetSlider != null)
                HorizontalOffsetSlider.ValueChanged += OnHorizontalOffsetSliderChanged;
            if (DelayedOffsetTextBox != null)
                DelayedOffsetTextBox.LostFocus += OnDelayedOffsetTextBoxLostFocus;
        }

        #endregion

        #region Public API

        public TimeBaseSettings GetSettings()
        {
            return settings.Clone();
        }

        public void SetSettings(TimeBaseSettings newSettings)
        {
            if (newSettings == null) return;

            SetHorizontalScale(newSettings.MainScale);     // Changed from SetMainScale
            SetHorizontalOffset(newSettings.MainOffset);   // Changed from SetMainOffset
            SetDelayEnabled(newSettings.DelayEnabled);
            SetDelayScale(newSettings.DelayScale);
            SetDelayedOffset(newSettings.DelayOffset);
            SetMode(newSettings.Mode);
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