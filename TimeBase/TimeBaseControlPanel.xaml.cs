using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Rigol_DS1000Z_E_Control;

namespace DS1000Z_E_USB_Control.TimeBase
{
    /// <summary>
    /// Interaction logic for TimeBaseControlPanel.xaml
    /// TimeBase control UI UserControl
    /// </summary>
    public partial class TimeBaseControlPanel : UserControl
    {
        private TimeBaseController controller;
        private bool isInitialized = false;
        private bool isUpdating = false;

        public event EventHandler<string> LogEvent;

        public TimeBaseControlPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the control panel with the oscilloscope controller
        /// </summary>
        public void Initialize(RigolDS1000ZE oscilloscope)
        {
            if (isInitialized) return;

            // Create the controller
            controller = new TimeBaseController(oscilloscope);
            controller.LogEvent += (sender, message) => LogEvent?.Invoke(this, message);
            controller.SettingsChanged += (sender, e) =>
            {
                LogEvent?.Invoke(this, "TimeBase settings changed");
                UpdateDisplays();
            };

            // Wire up UI controls to the controller
            WireUpControls();

            // Initialize the controller and UI
            InitializeUI();

            isInitialized = true;
            LogEvent?.Invoke(this, "TimeBase control panel initialized");
        }

        /// <summary>
        /// Wire up UI controls to event handlers
        /// </summary>
        private void WireUpControls()
        {
            if (TimeBaseModeComboBox != null)
            {
                TimeBaseModeComboBox.SelectionChanged += TimeBaseMode_SelectionChanged;
            }

            if (HorizontalScaleComboBox != null)
            {
                HorizontalScaleComboBox.SelectionChanged += HorizontalScale_SelectionChanged;
            }

            if (HorizontalOffsetSlider != null)
            {
                HorizontalOffsetSlider.ValueChanged += HorizontalOffsetSlider_ValueChanged;
            }

            if (QuickZeroOffsetButton != null)
            {
                QuickZeroOffsetButton.Click += QuickZeroOffset_Click;
            }
        }

        /// <summary>
        /// Initialize the UI elements
        /// </summary>
        private void InitializeUI()
        {
            PopulateHorizontalScaleOptions();
            UpdateOffsetSliderRange();
            UpdateDisplays();
        }

        /// <summary>
        /// Populate horizontal scale options
        /// </summary>
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
                if (Math.Abs(option.value - 1e-3) < 1e-9)
                {
                    HorizontalScaleComboBox.SelectedItem = item;
                }
            }
        }

        /// <summary>
        /// Update the horizontal offset slider range based on current scale
        /// </summary>
        private void UpdateOffsetSliderRange()
        {
            if (HorizontalOffsetSlider == null || controller == null) return;

            var settings = controller.GetSettings();
            var (minOffset, maxOffset) = settings.GetOffsetRange();

            isUpdating = true;

            // Set the range
            HorizontalOffsetSlider.Minimum = minOffset;
            HorizontalOffsetSlider.Maximum = maxOffset;

            // Calculate smart tick frequency based on range
            double range = maxOffset - minOffset;
            double tickFreq;

            if (range <= 1e-6) tickFreq = 100e-9;      // For ns ranges
            else if (range <= 1e-3) tickFreq = 100e-6; // For µs ranges
            else if (range <= 1.0) tickFreq = 100e-3;  // For ms ranges
            else tickFreq = 1.0;                       // For s ranges

            HorizontalOffsetSlider.TickFrequency = tickFreq;

            // Clamp current value to new range
            if (HorizontalOffsetSlider.Value < minOffset)
                HorizontalOffsetSlider.Value = minOffset;
            else if (HorizontalOffsetSlider.Value > maxOffset)
                HorizontalOffsetSlider.Value = maxOffset;

            isUpdating = false;

            // Update range displays
            if (MinOffsetDisplay != null)
                MinOffsetDisplay.Text = FormatTime(minOffset);
            if (MaxOffsetDisplay != null)
                MaxOffsetDisplay.Text = FormatTime(maxOffset);
            if (OffsetRangeText != null)
                OffsetRangeText.Text = $"Range: ±{FormatTime(Math.Abs(maxOffset))}";

            LogEvent?.Invoke(this, $"TimeBase offset range updated: {FormatTime(minOffset)} to {FormatTime(maxOffset)}");
        }

        /// <summary>
        /// Update all display elements
        /// </summary>
        private void UpdateDisplays()
        {
            if (controller == null) return;

            var settings = controller.GetSettings();

            // Update time window
            if (TimeWindowText != null)
            {
                TimeWindowText.Text = FormatTime(settings.TimeWindow);
            }

            // Update offset value and percentage
            UpdateOffsetValueDisplay();

            // Update current settings display
            if (CurrentTimeBaseSettingsText != null)
            {
                CurrentTimeBaseSettingsText.Text =
                    $"Current: Mode={settings.Mode}, Scale={settings.MainScaleDisplay}, " +
                    $"Offset={FormatTime(settings.MainOffset)}, Window={FormatTime(settings.TimeWindow)}";
            }
        }

        /// <summary>
        /// Update the offset value display
        /// </summary>
        private void UpdateOffsetValueDisplay()
        {
            if (OffsetValueText == null || HorizontalOffsetSlider == null) return;

            double value = HorizontalOffsetSlider.Value;
            OffsetValueText.Text = FormatTime(value);

            // Update percentage display
            if (OffsetPercentageDisplay != null && controller != null)
            {
                var settings = controller.GetSettings();
                var (minOffset, maxOffset) = settings.GetOffsetRange();
                double range = maxOffset - minOffset;
                double percentage = range > 0 ? (value / (range / 2.0)) * 100 : 0;
                OffsetPercentageDisplay.Text = $"({percentage:F0}%)";
            }
        }

        #region Event Handlers

        /// <summary>
        /// Handle timebase mode changes
        /// </summary>
        private void TimeBaseMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || controller == null) return;

            var selectedItem = TimeBaseModeComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string mode = selectedItem.Tag.ToString();
                controller.SetMode(mode);
            }
        }

        /// <summary>
        /// Handle horizontal scale changes
        /// </summary>
        private void HorizontalScale_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || controller == null) return;

            var selectedItem = HorizontalScaleComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null && double.TryParse(selectedItem.Tag.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double scale))
            {
                controller.SetHorizontalScale(scale);
                UpdateOffsetSliderRange(); // Update range when scale changes
            }
        }

        /// <summary>
        /// Handle horizontal offset slider changes
        /// </summary>
        private void HorizontalOffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdating || controller == null) return;

            UpdateOffsetValueDisplay();
            controller.SetHorizontalOffset(e.NewValue);
        }

        /// <summary>
        /// Quick zero offset button handler
        /// </summary>
        private void QuickZeroOffset_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            controller.SetHorizontalOffset(0);

            if (HorizontalOffsetSlider != null)
            {
                isUpdating = true;
                HorizontalOffsetSlider.Value = 0;
                isUpdating = false;
                UpdateOffsetValueDisplay();
            }

            LogEvent?.Invoke(this, "TimeBase offset zeroed");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Enable or disable the control panel
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.IsEnabled = enabled;
        }

        /// <summary>
        /// Query and update settings from oscilloscope
        /// </summary>
        public void QueryAndUpdateSettings()
        {
            if (controller == null) return;

            controller.QueryAndUpdateSettings();
            UpdateFromCurrentSettings();
        }

        /// <summary>
        /// Update UI from current controller settings
        /// </summary>
        private void UpdateFromCurrentSettings()
        {
            if (controller == null) return;

            try
            {
                isUpdating = true;

                var settings = controller.GetSettings();

                // Update mode
                if (TimeBaseModeComboBox != null)
                {
                    foreach (ComboBoxItem item in TimeBaseModeComboBox.Items)
                    {
                        if (item.Tag.ToString().Equals(settings.Mode, StringComparison.OrdinalIgnoreCase))
                        {
                            TimeBaseModeComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update horizontal scale
                if (HorizontalScaleComboBox != null)
                {
                    foreach (ComboBoxItem item in HorizontalScaleComboBox.Items)
                    {
                        if (double.TryParse(item.Tag.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double itemScale) &&
                            Math.Abs(itemScale - settings.MainScale) < settings.MainScale * 0.01)
                        {
                            HorizontalScaleComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update horizontal offset
                UpdateOffsetSliderRange();
                if (HorizontalOffsetSlider != null)
                {
                    HorizontalOffsetSlider.Value = settings.MainOffset;
                }

                UpdateDisplays();

                LogEvent?.Invoke(this, $"Updated TimeBase UI: {settings}");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error updating TimeBase UI: {ex.Message}");
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Get current timebase settings
        /// </summary>
        public TimeBaseSettings GetSettings()
        {
            return controller?.GetSettings();
        }

        /// <summary>
        /// Set timebase settings (sends commands to oscilloscope)
        /// </summary>
        public void SetSettings(TimeBaseSettings settings)
        {
            if (controller == null || settings == null) return;

            controller.SetSettings(settings);
            UpdateFromCurrentSettings();
        }

        /// <summary>
        /// Update UI from settings (does NOT send commands to oscilloscope)
        /// </summary>
        public void UpdateFromSettings(TimeBaseSettings settings)
        {
            if (settings == null) return;

            // This would update the controller's internal settings and then refresh the UI
            // For now, we'll just update the UI directly
            UpdateFromCurrentSettings();
        }

        /// <summary>
        /// Apply a preset configuration
        /// </summary>
        public void ApplyPreset(TimeBaseSettings preset)
        {
            SetSettings(preset);
            LogEvent?.Invoke(this, $"Applied TimeBase preset: {preset}");
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Cleanup()
        {
            // Remove event handlers
            if (TimeBaseModeComboBox != null)
                TimeBaseModeComboBox.SelectionChanged -= TimeBaseMode_SelectionChanged;
            if (HorizontalScaleComboBox != null)
                HorizontalScaleComboBox.SelectionChanged -= HorizontalScale_SelectionChanged;
            if (HorizontalOffsetSlider != null)
                HorizontalOffsetSlider.ValueChanged -= HorizontalOffsetSlider_ValueChanged;
            if (QuickZeroOffsetButton != null)
                QuickZeroOffsetButton.Click -= QuickZeroOffset_Click;

            controller = null;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Smart time formatting
        /// </summary>
        private string FormatTime(double time)
        {
            if (Math.Abs(time) >= 1.0)
                return $"{time:F3} s";
            else if (Math.Abs(time) >= 1e-3)
                return $"{time * 1000:F1} ms";
            else if (Math.Abs(time) >= 1e-6)
                return $"{time * 1000000:F1} µs";
            else
                return $"{time * 1000000000:F1} ns";
        }

        /// <summary>
        /// Update additional displays with acquisition information
        /// </summary>
        public void UpdateAcquisitionInfo(string sampleRate, string memoryDepth, string acquisitionType)
        {
            if (SampleRateText != null)
                SampleRateText.Text = sampleRate;
            if (MemoryDepthText != null)
                MemoryDepthText.Text = memoryDepth;
            if (AcquisitionTypeText != null)
                AcquisitionTypeText.Text = acquisitionType;
        }

        #endregion
    }
}