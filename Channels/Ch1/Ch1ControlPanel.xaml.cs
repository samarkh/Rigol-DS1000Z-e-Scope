using System;
using System.Windows;
using System.Windows.Controls;
using Rigol_DS1000Z_E_Control;
using DS1000Z_E_USB_Control.Controls;

namespace DS1000Z_E_USB_Control.Channels.Ch1
{
    /// <summary>
    /// Enhanced Ch1ControlPanel with rotated multimedia arrow controls
    /// Uses EmojiTimeBaseArrows with Orientation="Vertical" for voltage offset control
    /// </summary>
    public partial class Ch1ControlPanel : UserControl
    {
        private Ch1Controller controller;
        private bool isUpdating = false;
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
            // Note: Removed SliderValueText - we're not displaying current offset value

            // Wire up additional enhanced UI elements
            controller.MaxValueDisplay = MaxValueDisplay;
            controller.MinValueDisplay = MinValueDisplay;
            controller.OffsetRangeText = OffsetRangeText;
            controller.QuickZeroButton = QuickZeroButton;

            // Set up event handlers
            WireUpEventHandlers();

            // Initialize the controller
            controller.InitializeControls();

            // Set up enhanced UI elements
            SetupEnhancedUI();

            isInitialized = true;
            LogEvent?.Invoke(this, "Channel 1 control panel initialized with multimedia controls");
        }

        /// <summary>
        /// Wire up event handlers for all controls
        /// </summary>
        private void WireUpEventHandlers()
        {
            // Arrow control for vertical offset
            if (VerticalOffsetArrows != null)
            {
                VerticalOffsetArrows.GraticuleMovement += VerticalOffsetArrows_GraticuleMovement;
            }

            // Quick action buttons
            if (QuickZeroButton != null)
                QuickZeroButton.Click += QuickZero_Click;

            if (PresetButton1 != null)
                PresetButton1.Click += (s, e) => ApplyVoltagePreset(5.0);

            if (PresetButton2 != null)
                PresetButton2.Click += (s, e) => ApplyVoltagePreset(10.0);

            if (PresetButton3 != null)
                PresetButton3.Click += (s, e) => ApplyGroundingPreset();

            // Subscribe to controller events for UI updates
            if (controller != null)
            {
                controller.SettingsChanged += (sender, e) => UpdateOffsetArrowControl();
            }
        }

        /// <summary>
        /// Handle vertical offset arrow control movements
        /// </summary>
        private void VerticalOffsetArrows_GraticuleMovement(object sender, GraticuleMovementEventArgs e)
        {
            if (controller == null || isUpdating) return;

            isUpdating = true;
            try
            {
                // Update the controller with the new offset value
                controller.HandleVerticalOffsetChanged(e.NewValue);

                string movementDescription = GetMovementDescription(e.MovementType, e.GraticuleMultiplier);
                LogEvent?.Invoke(this, $"CH1 vertical offset {movementDescription} to {FormatVoltage(e.NewValue)}");
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Get human-readable movement description
        /// </summary>
        private string GetMovementDescription(GraticuleMovementType movementType, double multiplier)
        {
            switch (movementType)
            {
                case GraticuleMovementType.LargeUp:
                    return "moved up (large step)";
                case GraticuleMovementType.SmallUp:
                    return "moved up (small step)";
                case GraticuleMovementType.SmallDown:
                    return "moved down (small step)";
                case GraticuleMovementType.LargeDown:
                    return "moved down (large step)";
                case GraticuleMovementType.VerticalUp:
                    return "moved up";
                case GraticuleMovementType.VerticalDown:
                    return "moved down";
                case GraticuleMovementType.Zero:
                    return "reset to zero";
                default:
                    return $"adjusted by {multiplier:F1} graticule";
            }
        }

        /// <summary>
        /// Update the arrow control based on current settings
        /// </summary>
        public void UpdateOffsetArrowControl()
        {
            if (VerticalOffsetArrows == null || controller == null || isUpdating) return;

            var settings = controller.GetSettings();
            var (minOffset, maxOffset) = settings.GetOffsetRange();

            // Update arrow control properties
            VerticalOffsetArrows.GraticuleSize = settings.VerticalScale; // 1 graticule = 1 voltage scale
            VerticalOffsetArrows.Units = "V";
            VerticalOffsetArrows.UpdateRange(minOffset, maxOffset);
            VerticalOffsetArrows.SetValue(settings.VerticalOffset);

            // Update range displays
            UpdateRangeDisplays();
        }

        /// <summary>
        /// Set up enhanced UI elements
        /// </summary>
        private void SetupEnhancedUI()
        {
            UpdateRangeDisplays();
            UpdateOffsetArrowControl();
        }

        /// <summary>
        /// Quick zero button handler
        /// </summary>
        private void QuickZero_Click(object sender, RoutedEventArgs e)
        {
            if (controller != null)
            {
                controller.SetVerticalOffset(0);
                LogEvent?.Invoke(this, "Channel 1 offset zeroed");
            }
        }

        /// <summary>
        /// Apply voltage range preset
        /// </summary>
        private void ApplyVoltagePreset(double voltage)
        {
            if (controller == null) return;

            try
            {
                // Set vertical scale to show the voltage range nicely
                double scale = voltage / 4.0; // 4 divisions to show full range
                controller.SetVerticalScale(scale);
                controller.SetVerticalOffset(0);

                LogEvent?.Invoke(this, $"Applied ±{voltage}V preset to Channel 1");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error applying voltage preset: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply grounding preset
        /// </summary>
        private void ApplyGroundingPreset()
        {
            if (controller == null) return;

            try
            {
                controller.SetCoupling("GND");
                controller.SetVerticalOffset(0);
                LogEvent?.Invoke(this, "Applied grounding preset to Channel 1");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error applying grounding preset: {ex.Message}");
            }
        }

        /// <summary>
        /// Update the min/max range displays
        /// </summary>
        public void UpdateRangeDisplays()
        {
            if (controller == null) return;

            try
            {
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

                LogEvent?.Invoke(this, $"CH1 offset range updated: {FormatVoltage(minOffset)} to {FormatVoltage(maxOffset)}");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error updating range displays: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to format voltage values
        /// </summary>
        private string FormatVoltage(double voltage)
        {
            string sign = voltage >= 0 ? "+" : "";

            if (Math.Abs(voltage) >= 1000)
                return $"{sign}{voltage / 1000:F2}kV";
            else if (Math.Abs(voltage) >= 1.0)
                return $"{sign}{voltage:F3}V";
            else if (Math.Abs(voltage) >= 0.001)
                return $"{sign}{voltage * 1000:F1}mV";
            else
                return $"{sign}{voltage * 1000000:F1}μV";
        }

        /// <summary>
        /// Get the current controller (for external access)
        /// </summary>
        public Ch1Controller GetController()
        {
            return controller;
        }

        /// <summary>
        /// Check if the panel is properly initialized (using 'new' to avoid CS0108 warning)
        /// </summary>
        public new bool IsInitialized => isInitialized;

        /// <summary>
        /// Force update of all displays
        /// </summary>
        public void RefreshDisplays()
        {
            if (isInitialized && !isUpdating)
            {
                UpdateOffsetArrowControl();
                UpdateRangeDisplays();
            }
        }

        /// <summary>
        /// Clean up resources and event handlers
        /// </summary>
        public void Cleanup()
        {
            if (VerticalOffsetArrows != null)
                VerticalOffsetArrows.GraticuleMovement -= VerticalOffsetArrows_GraticuleMovement;

            if (QuickZeroButton != null)
                QuickZeroButton.Click -= QuickZero_Click;

            if (controller != null)
            {
                controller.SettingsChanged -= (sender, e) => UpdateOffsetArrowControl();
                controller.Dispose();
            }

            controller = null;
            isInitialized = false;
        }

        /// <summary>
        /// Get current vertical offset value
        /// </summary>
        public double GetCurrentOffset()
        {
            return VerticalOffsetArrows?.CurrentValue ?? 0.0;
        }

        /// <summary>
        /// Set vertical offset value
        /// </summary>
        public void SetCurrentOffset(double offset)
        {
            if (VerticalOffsetArrows != null && controller != null)
            {
                VerticalOffsetArrows.CurrentValue = offset;
                controller.SetVerticalOffset(offset);
            }
        }

        /// <summary>
        /// Check if channel is currently enabled
        /// </summary>
        public bool IsChannelEnabled()
        {
            return EnableCheckBox?.IsChecked ?? false;
        }

        /// <summary>
        /// Enable or disable the entire control panel (for MainWindow compatibility)
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.IsEnabled = enabled;

            // Also update the controller if available
            if (controller != null && EnableCheckBox != null)
            {
                EnableCheckBox.IsChecked = enabled;
                controller.SetEnabled(enabled);
            }
        }

        /// <summary>
        /// Enable or disable just the channel (not the entire panel)
        /// </summary>
        public void SetChannelEnabled(bool enabled)
        {
            if (controller != null)
            {
                controller.SetEnabled(enabled);
            }
        }
    }
}