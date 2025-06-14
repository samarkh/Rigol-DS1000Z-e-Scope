using System;
using System.Windows;
using System.Windows.Controls;
using Rigol_DS1000Z_E_Control;
using DS1000Z_E_USB_Control.Controls;

namespace DS1000Z_E_USB_Control.Channels.Ch1
{
    /// <summary>
    /// Interaction logic for Ch1ControlPanel.xaml
    /// Channel 1 UI UserControl with Arrow Control for Offset
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
            controller.CouplingComboBox = CouplingComboBox;
            controller.CurrentSettingsTextBlock = CurrentSettingsText;

            // REMOVED: Slider assignments (now using arrow control)
            // controller.VerticalOffsetSlider = VerticalOffsetSlider;
            // controller.SliderValueText = SliderValueText;

            // Wire up enhanced UI controls to the controller
            controller.MaxValueDisplay = MaxValueDisplay;
            controller.MinValueDisplay = MinValueDisplay;
            controller.OffsetRangeText = OffsetRangeText;
            // REMOVED: controller.PercentageDisplay = PercentageDisplay;
            controller.QuickZeroButton = QuickZeroButton;

            // Wire up additional UI elements specific to this UserControl
            WireUpAdditionalControls();

            // Initialize the controller
            controller.InitializeControls();

            // Set up additional UI elements
            SetupEnhancedUI();

            isInitialized = true;
            LogEvent?.Invoke(this, "Channel 1 control panel initialized with arrow controls");
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

            // REPLACED: Slider event handler with arrow control handler
            if (VerticalOffsetArrows != null)
            {
                VerticalOffsetArrows.GraticuleMovement += VerticalOffsetArrows_GraticuleMovement;
            }

            // Subscribe to settings changes to update arrow control
            if (controller != null)
            {
                controller.SettingsChanged += (sender, e) => UpdateOffsetArrowControl();
            }
        }

        /// <summary>
        /// Set up enhanced UI elements
        /// </summary>
        private void SetupEnhancedUI()
        {
            UpdateOffsetArrowControl();
        }

        /// <summary>
        /// NEW: Handle graticule arrow movement
        /// </summary>
        private void VerticalOffsetArrows_GraticuleMovement(object sender, GraticuleMovementEventArgs e)
        {
            if (controller == null) return;

            controller.HandleVerticalOffsetChanged(e.NewValue);
            LogEvent?.Invoke(this, $"Channel 1 offset moved {e.GraticuleMultiplier:F1} graticule to {e.NewValue:F3}V");
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
        /// NEW: Update the arrow control instead of slider
        /// </summary>
        public void UpdateOffsetArrowControl()
        {
            if (VerticalOffsetArrows == null || controller == null) return;

            var settings = controller.GetSettings();
            var (minOffset, maxOffset) = settings.GetOffsetRange();

            // Update arrow control properties
            VerticalOffsetArrows.GraticuleSize = settings.VerticalScale; // 1 graticule = 1 vertical scale
            VerticalOffsetArrows.Units = "V";
            VerticalOffsetArrows.UpdateRange(minOffset, maxOffset);
            VerticalOffsetArrows.SetValue(settings.VerticalOffset);

            // Update range displays
            if (MinValueDisplay != null)
                MinValueDisplay.Text = FormatVoltage(minOffset);
            if (MaxValueDisplay != null)
                MaxValueDisplay.Text = FormatVoltage(maxOffset);
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

        /// <summary>
        /// Get the current controller (for external access)
        /// </summary>
        public Ch1Controller GetController()
        {
            return controller;
        }

        /// <summary>
        /// Cleanup method
        /// </summary>
        public void Cleanup()
        {
            if (VerticalOffsetArrows != null)
            {
                VerticalOffsetArrows.GraticuleMovement -= VerticalOffsetArrows_GraticuleMovement;
            }

            if (QuickZeroButton != null)
            {
                QuickZeroButton.Click -= QuickZero_Click;
            }

            if (controller != null)
            {
                controller.SettingsChanged -= (sender, e) => UpdateOffsetArrowControl();
            }

            isInitialized = false;
        }

        /// <summary>
        /// Force update of all displays
        /// </summary>
        public void RefreshDisplays()
        {
            UpdateOffsetArrowControl();
        }

        /// <summary>
        /// Check if the panel is properly initialized
        /// </summary>
        public bool IsInitialized => isInitialized;
    }
}