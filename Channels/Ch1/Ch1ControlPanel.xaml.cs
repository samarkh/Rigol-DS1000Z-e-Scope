using System;
using System.Windows;
using System.Windows.Controls;
using Rigol_DS1000Z_E_Control;

namespace DS1000Z_E_USB_Control.Channels.Ch1
{
    /// <summary>
    /// Interaction logic for Ch1ControlPanel.xaml
    /// Channel 1 UI UserControl
    /// </summary>
    public partial class Ch1ControlPanel : UserControl
    {
        private Ch1Controller controller;
        private bool isInitialized = false;

        public event EventHandler<string> LogEvent;

        public Ch1ControlPanel()
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
            controller = new Ch1Controller(oscilloscope);
            controller.LogEvent += (sender, message) => LogEvent?.Invoke(this, message);
            controller.SettingsChanged += (sender, e) => LogEvent?.Invoke(this, "Channel 1 settings changed");

            // Wire up UI controls to the controller
            controller.EnableCheckBox = EnableCheckBox;
            controller.ProbeRatioComboBox = ProbeRatioComboBox;
            controller.VerticalScaleComboBox = VerticalScaleComboBox;
            controller.UnitsComboBox = UnitsComboBox;
            controller.CurrentSettingsTextBlock = CurrentSettingsText;
            controller.VerticalOffsetSlider = VerticalOffsetSlider;
            controller.SliderValueText = SliderValueText;

            // Wire up enhanced UI controls to the controller
            controller.MaxValueDisplay = MaxValueDisplay;
            controller.MinValueDisplay = MinValueDisplay;
            controller.OffsetRangeText = OffsetRangeText;
            controller.PercentageDisplay = PercentageDisplay;
            controller.QuickZeroButton = QuickZeroButton;

            // Wire up additional UI elements specific to this UserControl
            WireUpAdditionalControls();

            // Initialize the controller
            controller.InitializeControls();

            // Set up additional UI elements
            SetupEnhancedUI();

            isInitialized = true;
            LogEvent?.Invoke(this, "Channel 1 control panel initialized");
        }

        /// <summary>
        /// Wire up additional controls not handled by the base controller
        /// </summary>
        private void WireUpAdditionalControls()
        {
            if (QuickZeroButton != null)
            {
                QuickZeroButton.Click += QuickZero_Click;
            }

            if (VerticalOffsetSlider != null)
            {
                VerticalOffsetSlider.ValueChanged += VerticalOffsetSlider_ValueChanged;
            }

            // Subscribe to settings changes to update range displays
            if (controller != null)
            {
                controller.SettingsChanged += (sender, e) => UpdateRangeDisplays();
            }
        }

        /// <summary>
        /// Set up enhanced UI elements
        /// </summary>
        private void SetupEnhancedUI()
        {
            UpdateRangeDisplays();
        }

        /// <summary>
        /// Handle vertical offset slider changes
        /// </summary>
        private void VerticalOffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (controller == null) return;

            UpdateSliderValueDisplay();
            controller.HandleVerticalOffsetChanged(e.NewValue);
        }

        /// <summary>
        /// Quick zero button handler
        /// </summary>
        private void QuickZero_Click(object sender, RoutedEventArgs e)
        {
            controller?.SetVerticalOffset(0);
            LogEvent?.Invoke(this, "Channel 1 offset zeroed");
        }

        /// <summary>
        /// Update the enhanced slider value display
        /// </summary>
        private void UpdateSliderValueDisplay()
        {
            if (SliderValueText == null || VerticalOffsetSlider == null) return;

            double value = VerticalOffsetSlider.Value;
            SliderValueText.Text = FormatVoltage(value);

            // Update percentage display
            if (PercentageDisplay != null && controller != null)
            {
                var settings = controller.GetSettings();
                var (minOffset, maxOffset) = settings.GetOffsetRange();
                double range = maxOffset - minOffset;
                double percentage = range > 0 ? (value / (range / 2.0)) * 100 : 0;
                PercentageDisplay.Text = $"({percentage:F0}%)";
            }
        }

        /// <summary>
        /// Update the min/max range displays
        /// </summary>
        public void UpdateRangeDisplays()
        {
            if (controller == null) return;

            var settings = controller.GetSettings();
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
        /// Smart voltage formatting
        /// </summary>
        private string FormatVoltage(double voltage)
        {
            if (Math.Abs(voltage) >= 1000)
                return $"{voltage / 1000:F1}kV";
            else if (Math.Abs(voltage) >= 1)
                return $"{voltage:F1}V";
            else if (Math.Abs(voltage) >= 0.001)
                return $"{voltage * 1000:F0}mV";
            else
                return $"{voltage * 1000000:F0}µV";
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
            UpdateRangeDisplays();
            UpdateSliderValueDisplay();
        }

        /// <summary>
        /// Get current channel settings
        /// </summary>
        public Ch1Settings GetSettings()
        {
            return controller?.GetSettings();
        }

        /// <summary>
        /// Set channel settings
        /// </summary>
        public void SetSettings(Ch1Settings settings)
        {
            controller?.SetSettings(settings);
            UpdateRangeDisplays();
            UpdateSliderValueDisplay();
        }

        /// <summary>
        /// Apply a preset configuration
        /// </summary>
        public void ApplyPreset(Ch1Settings preset)
        {
            SetSettings(preset);
            LogEvent?.Invoke(this, $"Applied Channel 1 preset: {preset}");
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