using System;
using System.Windows;
using System.Windows.Controls;
using Rigol_DS1000Z_E_Control;
using DS1000Z_E_USB_Control.Controls;

namespace DS1000Z_E_USB_Control.Channels.Ch1
{
    /// <summary>
    /// Simplified Ch1ControlPanel with multimedia arrow controls
    /// Clean implementation focused on voltage controls only (no time data)
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

            // Wire up basic UI controls to the controller
            controller.EnableCheckBox = EnableCheckBox;
            controller.ProbeRatioComboBox = ProbeRatioComboBox;
            controller.VerticalScaleComboBox = VerticalScaleComboBox;
            controller.CouplingComboBox = CouplingComboBox;
            controller.CurrentSettingsTextBlock = CurrentSettingsText;

            // Wire up range display controls
            controller.MaxValueDisplay = MaxValueDisplay;
            controller.MinValueDisplay = MinValueDisplay;
            controller.OffsetRangeText = OffsetRangeText;
            controller.QuickZeroButton = QuickZeroButton;

            // Set up event handlers
            WireUpEventHandlers();

            // Initialize the controller
            controller.InitializeControls();

            // Update displays
            UpdateDisplays();

            isInitialized = true;
            LogEvent?.Invoke(this, "Channel 1 control panel initialized (simplified)");
        }

        /// <summary>
        /// Wire up event handlers for controls
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
                PresetButton1.Click += (s, e) => ApplyPreset("±5V");

            if (PresetButton2 != null)
                PresetButton2.Click += (s, e) => ApplyPreset("±10V");

            // Subscribe to controller events for UI updates
            if (controller != null)
            {
                controller.SettingsChanged += (sender, e) => UpdateOffsetControl();
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

                LogEvent?.Invoke(this, $"CH1 offset: {FormatVoltage(e.NewValue)}");
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Update the offset control based on current settings
        /// </summary>
        public void UpdateOffsetControl()
        {
            if (VerticalOffsetArrows == null || controller == null || isUpdating) return;

            var settings = controller.GetSettings();
            var (minOffset, maxOffset) = settings.GetOffsetRange();

            // Update arrow control properties
            VerticalOffsetArrows.GraticuleSize = settings.VerticalScale;
            VerticalOffsetArrows.UpdateRange(minOffset, maxOffset);
            VerticalOffsetArrows.SetValue(settings.VerticalOffset);

            // Update range displays
            UpdateRangeDisplays(minOffset, maxOffset);
        }

        /// <summary>
        /// Update range display elements
        /// </summary>
        private void UpdateRangeDisplays(double minOffset, double maxOffset)
        {
            if (MaxValueDisplay != null)
                MaxValueDisplay.Text = FormatVoltage(maxOffset);

            if (MinValueDisplay != null)
                MinValueDisplay.Text = FormatVoltage(minOffset);

            if (OffsetRangeText != null)
            {
                if (Math.Abs(minOffset) == Math.Abs(maxOffset))
                {
                    OffsetRangeText.Text = $"±{FormatVoltage(maxOffset).Replace("+", "")}";
                }
                else
                {
                    OffsetRangeText.Text = $"{FormatVoltage(minOffset)} to {FormatVoltage(maxOffset)}";
                }
            }
        }

        /// <summary>
        /// Update all displays
        /// </summary>
        private void UpdateDisplays()
        {
            if (controller != null)
            {
                UpdateOffsetControl();
            }
        }

        /// <summary>
        /// Quick zero button handler
        /// </summary>
        private void QuickZero_Click(object sender, RoutedEventArgs e)
        {
            if (controller != null)
            {
                controller.SetVerticalOffset(0);
                LogEvent?.Invoke(this, "CH1 offset reset to zero");
            }
        }

        /// <summary>
        /// Apply simple presets
        /// </summary>
        private void ApplyPreset(string preset)
        {
            if (controller == null) return;

            try
            {
                switch (preset)
                {
                    case "±5V":
                        controller.SetVerticalScale(1.25); // 5V in 4 divisions
                        controller.SetVerticalOffset(0);
                        break;
                    case "±10V":
                        controller.SetVerticalScale(2.5); // 10V in 4 divisions  
                        controller.SetVerticalOffset(0);
                        break;
                }
                LogEvent?.Invoke(this, $"Applied {preset} preset to CH1");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error applying preset: {ex.Message}");
            }
        }

        /// <summary>
        /// Format voltage values with appropriate units
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

        #region Public API for MainWindow Compatibility

        /// <summary>
        /// Get the current controller (for external access)
        /// </summary>
        public Ch1Controller GetController()
        {
            return controller;
        }

        /// <summary>
        /// Check if the panel is properly initialized
        /// </summary>
        public new bool IsInitialized => isInitialized;

        /// <summary>
        /// Enable or disable the entire control panel
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.IsEnabled = enabled;

            if (controller != null && EnableCheckBox != null)
            {
                EnableCheckBox.IsChecked = enabled;
                controller.SetEnabled(enabled);
            }
        }

        /// <summary>
        /// Force update of all displays
        /// </summary>
        public void RefreshDisplays()
        {
            if (isInitialized && !isUpdating)
            {
                UpdateDisplays();
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
                controller.SettingsChanged -= (sender, e) => UpdateOffsetControl();
                controller.Dispose();
            }

            controller = null;
            isInitialized = false;
        }

        #endregion
    }
}