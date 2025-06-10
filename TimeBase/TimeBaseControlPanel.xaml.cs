using System;
using System.Windows;
using System.Windows.Controls;
using Rigol_DS1000Z_E_Control;

namespace DS1000Z_E_USB_Control.TimeBase
{
    /// <summary>
    /// Interaction logic for TimeBaseControlPanel.xaml
    /// TimeBase UI UserControl for Rigol DS1000Z-E
    /// </summary>
    public partial class TimeBaseControlPanel : UserControl
    {
        private TimeBaseController controller;
        private bool isInitialized = false;

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
            controller.SettingsChanged += (sender, e) => LogEvent?.Invoke(this, "TimeBase settings changed");

            // Wire up UI controls to the controller
            controller.HorizontalScaleComboBox = HorizontalScaleComboBox;
            controller.DelayedScaleComboBox = DelayedScaleComboBox;
            controller.DelayEnabledCheckBox = DelayEnabledCheckBox;
            controller.MainModeRadioButton = MainModeRadioButton;
            controller.DelayedModeRadioButton = DelayedModeRadioButton;
            controller.HorizontalOffsetSlider = HorizontalOffsetSlider;
            controller.DelayedOffsetTextBox = DelayedOffsetTextBox;
            controller.CurrentTimeBaseSettingsText = CurrentTimeBaseSettingsText;
            controller.CurrentScaleText = CurrentScaleText;
            controller.CurrentDelayedScaleText = CurrentDelayedScaleText;
            controller.OffsetValueText = OffsetValueText;
            controller.TimeWindowText = TimeWindowText;
            controller.SampleRateText = SampleRateText;

            // Wire up enhanced UI controls
            controller.MaxOffsetDisplay = MaxOffsetDisplay;
            controller.MinOffsetDisplay = MinOffsetDisplay;
            controller.OffsetRangeText = OffsetRangeText;
            controller.DelayedOffsetDisplayText = DelayedOffsetDisplayText;
            controller.DelayedStatusText = DelayedStatusText;
            controller.AutoScaleButton = AutoScaleButton;
            controller.ResetTimeBaseButton = ResetTimeBaseButton;
            controller.CenterTimeBaseButton = CenterTimeBaseButton;
            controller.QuickZeroOffsetButton = QuickZeroOffsetButton;

            // Wire up additional controls not handled by the base controller
            WireUpAdditionalControls();

            // Initialize the controller
            controller.InitializeControls();

            // Set up additional UI elements
            SetupEnhancedUI();

            isInitialized = true;
            LogEvent?.Invoke(this, "TimeBase control panel initialized");
        }

        /// <summary>
        /// Wire up additional controls not handled by the base controller
        /// </summary>
        private void WireUpAdditionalControls()
        {
            if (AutoScaleButton != null)
            {
                AutoScaleButton.Click += AutoScale_Click;
            }

            if (ResetTimeBaseButton != null)
            {
                ResetTimeBaseButton.Click += ResetTimeBase_Click;
            }

            if (CenterTimeBaseButton != null)
            {
                CenterTimeBaseButton.Click += CenterTimeBase_Click;
            }

            if (QuickZeroOffsetButton != null)
            {
                QuickZeroOffsetButton.Click += QuickZeroOffset_Click;
            }

            if (HorizontalOffsetSlider != null)
            {
                HorizontalOffsetSlider.ValueChanged += HorizontalOffsetSlider_ValueChanged;
            }

            if (DelayedOffsetTextBox != null)
            {
                DelayedOffsetTextBox.TextChanged += DelayedOffsetTextBox_TextChanged;
                DelayedOffsetTextBox.LostFocus += DelayedOffsetTextBox_LostFocus;
            }

            // Subscribe to settings changes to update displays
            if (controller != null)
            {
                controller.SettingsChanged += (sender, e) => UpdateDisplays();
            }
        }

        /// <summary>
        /// Set up enhanced UI elements
        /// </summary>
        private void SetupEnhancedUI()
        {
            UpdateDisplays();
        }

        /// <summary>
        /// Handle horizontal offset slider changes
        /// </summary>
        private void HorizontalOffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (controller == null) return;

            UpdateOffsetValueDisplay();
            controller.HandleHorizontalOffsetChanged(e.NewValue);
        }

        /// <summary>
        /// Handle delayed offset text box changes
        /// </summary>
        private void DelayedOffsetTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDelayedOffsetDisplay();
        }

        /// <summary>
        /// Handle delayed offset text box lost focus (commit the value)
        /// </summary>
        private void DelayedOffsetTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (controller == null || DelayedOffsetTextBox == null) return;

            if (double.TryParse(DelayedOffsetTextBox.Text, out double offset))
            {
                controller.SetDelayedOffset(offset);
            }
            else
            {
                // Reset to last known good value
                var settings = controller.GetSettings();
                DelayedOffsetTextBox.Text = settings.DelayOffset.ToString("F6");
                LogEvent?.Invoke(this, "Invalid delayed offset value - reset to previous value");
            }
        }

        /// <summary>
        /// Auto scale button handler
        /// </summary>
        private void AutoScale_Click(object sender, RoutedEventArgs e)
        {
            controller?.AutoScale();
            LogEvent?.Invoke(this, "TimeBase auto scale executed");
        }

        /// <summary>
        /// Reset timebase button handler
        /// </summary>
        private void ResetTimeBase_Click(object sender, RoutedEventArgs e)
        {
            controller?.ResetToDefaults();
            LogEvent?.Invoke(this, "TimeBase reset to defaults");
        }

        /// <summary>
        /// Center timebase button handler
        /// </summary>
        private void CenterTimeBase_Click(object sender, RoutedEventArgs e)
        {
            controller?.SetMainOffset(0);
            LogEvent?.Invoke(this, "TimeBase centered");
        }

        /// <summary>
        /// Quick zero offset button handler
        /// </summary>
        private void QuickZeroOffset_Click(object sender, RoutedEventArgs e)
        {
            controller?.SetMainOffset(0);
            LogEvent?.Invoke(this, "TimeBase offset zeroed");
        }

        /// <summary>
        /// Update the offset value display
        /// </summary>
        private void UpdateOffsetValueDisplay()
        {
            if (OffsetValueText == null || HorizontalOffsetSlider == null) return;

            double value = HorizontalOffsetSlider.Value;
            OffsetValueText.Text = FormatTimeValue(value);
        }

        /// <summary>
        /// Update the delayed offset display text
        /// </summary>
        private void UpdateDelayedOffsetDisplay()
        {
            if (DelayedOffsetDisplayText == null || DelayedOffsetTextBox == null) return;

            if (double.TryParse(DelayedOffsetTextBox.Text, out double offset))
            {
                DelayedOffsetDisplayText.Text = $"({FormatTimeValue(offset)})";
            }
            else
            {
                DelayedOffsetDisplayText.Text = "(Invalid)";
            }
        }

        /// <summary>
        /// Update all display elements
        /// </summary>
        public void UpdateDisplays()
        {
            if (controller == null) return;

            var settings = controller.GetSettings();

            // Update offset range displays
            var (minOffset, maxOffset) = settings.GetOffsetRange();

            if (MaxOffsetDisplay != null)
            {
                MaxOffsetDisplay.Text = FormatTimeValue(maxOffset);
            }

            if (MinOffsetDisplay != null)
            {
                MinOffsetDisplay.Text = FormatTimeValue(minOffset);
            }

            if (OffsetRangeText != null)
            {
                string rangeText;
                if (Math.Abs(minOffset) == Math.Abs(maxOffset))
                {
                    rangeText = $"Range: ±{FormatTimeValue(maxOffset).Replace("+", "")}";
                }
                else
                {
                    rangeText = $"Range: {FormatTimeValue(minOffset)} to {FormatTimeValue(maxOffset)}";
                }
                OffsetRangeText.Text = rangeText;
            }

            // Update time window display
            if (TimeWindowText != null)
            {
                double timeWindow = settings.TimeWindow;
                TimeWindowText.Text = $"Total Window: {FormatTimeValue(timeWindow)} (12 divisions × {settings.MainScaleDisplay})";
            }

            // Update delayed status
            if (DelayedStatusText != null)
            {
                DelayedStatusText.Text = settings.DelayEnabled ?
                    $"Status: Enabled ({settings.DelayScaleDisplay})" :
                    "Status: Disabled";
            }
        }

        /// <summary>
        /// Smart time value formatting
        /// </summary>
        private string FormatTimeValue(double time)
        {
            if (time == 0) return "0 s";

            double absTime = Math.Abs(time);
            string sign = time < 0 ? "-" : "";

            if (absTime >= 1.0)
                return $"{sign}{absTime:F3} s";
            else if (absTime >= 1e-3)
                return $"{sign}{(absTime * 1000):F3} ms";
            else if (absTime >= 1e-6)
                return $"{sign}{(absTime * 1000000):F3} μs";
            else if (absTime >= 1e-9)
                return $"{sign}{(absTime * 1000000000):F3} ns";
            else
                return $"{sign}{absTime:E3} s";
        }

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
            controller?.QueryAndUpdateSettings();
            UpdateDisplays();
            UpdateOffsetValueDisplay();
            UpdateDelayedOffsetDisplay();
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
            controller?.SetSettings(settings);
            UpdateDisplays();
            UpdateOffsetValueDisplay();
            UpdateDelayedOffsetDisplay();
        }

        /// <summary>
        /// Update UI from settings (does NOT send commands to oscilloscope)
        /// </summary>
        public void UpdateFromSettings(TimeBaseSettings settings)
        {
            controller?.UpdateFromSettings(settings);
            UpdateDisplays();
            UpdateOffsetValueDisplay();
            UpdateDelayedOffsetDisplay();
        }

        /// <summary>
        /// Apply a preset configuration
        /// </summary>
        public void ApplyPreset(TimeBaseSettings preset)
        {
            SetSettings(preset);
            LogEvent?.Invoke(this, $"Applied timebase preset: {preset}");
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Cleanup()
        {
            controller?.Dispose();
            controller = null;
        }
    }
}