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
        private bool isUpdating = false;

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

            try
            {
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

                // Wire up enhanced UI controls to the controller (if they exist)
                if (MaxValueDisplay != null)
                    controller.MaxValueDisplay = MaxValueDisplay;
                if (MinValueDisplay != null)
                    controller.MinValueDisplay = MinValueDisplay;
                if (OffsetRangeText != null)
                    controller.OffsetRangeText = OffsetRangeText;
                if (QuickZeroButton != null)
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
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error initializing Channel 1 control panel: {ex.Message}");
            }
        }

        /// <summary>
        /// Enable or disable the channel control panel
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            try
            {
                isUpdating = true;

                // Enable/disable the entire panel
                this.IsEnabled = enabled;

                // Also ensure individual controls are properly enabled/disabled
                if (EnableCheckBox != null)
                    EnableCheckBox.IsEnabled = enabled;
                if (ProbeRatioComboBox != null)
                    ProbeRatioComboBox.IsEnabled = enabled;
                if (VerticalScaleComboBox != null)
                    VerticalScaleComboBox.IsEnabled = enabled;
                if (CouplingComboBox != null)
                    CouplingComboBox.IsEnabled = enabled;
                if (VerticalOffsetArrows != null)
                    VerticalOffsetArrows.IsEnabled = enabled;
                if (QuickZeroButton != null)
                    QuickZeroButton.IsEnabled = enabled;

                // If we have a controller, also set its enabled state
                if (controller != null && enabled)
                {
                    // When enabling, refresh the controller state
                    controller.RefreshSettings();
                }

                LogEvent?.Invoke(this, $"Channel 1 control panel {(enabled ? "enabled" : "disabled")}");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error setting Channel 1 panel enabled state: {ex.Message}");
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Update the UI controls from current oscilloscope settings
        /// </summary>
        public void UpdateFromSettings()
        {
            if (controller != null && isInitialized && !isUpdating)
            {
                try
                {
                    isUpdating = true;

                    // Tell the controller to refresh its settings from the oscilloscope
                    controller.RefreshSettings();

                    // Update our local UI elements
                    UpdateOffsetArrowControl();
                    UpdateRangeDisplays();

                    LogEvent?.Invoke(this, "Channel 1 settings updated from oscilloscope");
                }
                catch (Exception ex)
                {
                    LogEvent?.Invoke(this, $"Error updating Channel 1 settings: {ex.Message}");
                }
                finally
                {
                    isUpdating = false;
                }
            }
        }

        /// <summary>
        /// Apply a preset configuration to Channel 1
        /// </summary>
        public void ApplyPreset(string presetName)
        {
            if (controller != null && isInitialized)
            {
                try
                {
                    // Use the controller's preset system
                    controller.ApplyPreset(presetName);

                    // Update our UI after applying the preset
                    UpdateFromSettings();

                    LogEvent?.Invoke(this, $"Applied preset '{presetName}' to Channel 1");
                }
                catch (Exception ex)
                {
                    LogEvent?.Invoke(this, $"Error applying preset to Channel 1: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get current settings from the channel
        /// </summary>
        public object GetSettings()
        {
            if (controller != null && isInitialized)
            {
                try
                {
                    return controller.GetCurrentSettings();
                }
                catch (Exception ex)
                {
                    LogEvent?.Invoke(this, $"Error getting Channel 1 settings: {ex.Message}");
                    return null;
                }
            }
            return null;
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
            UpdateRangeDisplays();
        }

        /// <summary>
        /// Handle graticule arrow movement
        /// </summary>
        private void VerticalOffsetArrows_GraticuleMovement(object sender, GraticuleMovementEventArgs e)
        {
            if (controller == null || isUpdating) return;

            try
            {
                double currentOffset = VerticalOffsetArrows?.CurrentValue ?? 0.0;
                double graticuleSize = VerticalOffsetArrows?.GraticuleSize ?? 1.0;

                double newOffset = currentOffset;

                switch (e.MovementType)
                {
                    case GraticuleMovementType.LargeUp:
                        newOffset += graticuleSize;
                        break;
                    case GraticuleMovementType.SmallUp:
                        newOffset += graticuleSize * 0.1;
                        break;
                    case GraticuleMovementType.SmallDown:
                        newOffset -= graticuleSize * 0.1;
                        break;
                    case GraticuleMovementType.LargeDown:
                        newOffset -= graticuleSize;
                        break;
                    case GraticuleMovementType.Zero:
                        newOffset = 0.0;
                        break;
                }

                // Clamp to the arrow control's range
                double minValue = VerticalOffsetArrows?.MinValue ?? -10.0;
                double maxValue = VerticalOffsetArrows?.MaxValue ?? 10.0;
                newOffset = Math.Max(minValue, Math.Min(maxValue, newOffset));

                // Update the arrow control display
                VerticalOffsetArrows.CurrentValue = newOffset;

                // Send the change to the oscilloscope via controller
                controller.HandleVerticalOffsetChanged(newOffset);

                LogEvent?.Invoke(this, $"Channel 1 offset changed to {newOffset:F3}V via arrow control");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error handling vertical offset arrow movement: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle quick zero button click
        /// </summary>
        private void QuickZero_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            try
            {
                // Set offset to zero
                controller.SetVerticalOffset(0.0);

                // Update arrow control
                if (VerticalOffsetArrows != null)
                {
                    VerticalOffsetArrows.CurrentValue = 0.0;
                }

                LogEvent?.Invoke(this, "Channel 1 offset set to zero");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error setting Channel 1 offset to zero: {ex.Message}");
            }
        }

        /// <summary>
        /// Update the offset arrow control based on current settings
        /// </summary>
        private void UpdateOffsetArrowControl()
        {
            if (VerticalOffsetArrows == null || controller == null || isUpdating) return;

            try
            {
                var settings = controller.GetSettings();
                if (settings != null)
                {
                    // Update arrow control properties
                    VerticalOffsetArrows.CurrentValue = settings.VerticalOffset;
                    VerticalOffsetArrows.GraticuleSize = settings.VerticalScale;
                    VerticalOffsetArrows.Units = "V";

                    // Calculate range based on scale and probe ratio
                    double maxOffset = settings.VerticalScale * settings.ProbeRatio * 5.0; // ±5 divisions
                    VerticalOffsetArrows.MinValue = -maxOffset;
                    VerticalOffsetArrows.MaxValue = maxOffset;
                }
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error updating offset arrow control: {ex.Message}");
            }
        }

        /// <summary>
        /// Update range displays (if they exist)
        /// </summary>
        private void UpdateRangeDisplays()
        {
            try
            {
                if (controller != null)
                {
                    var settings = controller.GetSettings();
                    if (settings != null)
                    {
                        // Update max/min value displays if they exist
                        double maxOffset = settings.VerticalScale * settings.ProbeRatio * 5.0;
                        double minOffset = -maxOffset;

                        if (MaxValueDisplay != null)
                        {
                            MaxValueDisplay.Text = FormatVoltage(maxOffset);
                        }

                        if (MinValueDisplay != null)
                        {
                            MinValueDisplay.Text = FormatVoltage(minOffset);
                        }

                        // Update range text if it exists
                        if (OffsetRangeText != null)
                        {
                            string rangeText;
                            if (Math.Abs(maxOffset) < 0.001)
                            {
                                rangeText = $"Range: {FormatVoltage(minOffset)} to {FormatVoltage(maxOffset)}";
                            }
                            else
                            {
                                rangeText = $"Range: {FormatVoltage(minOffset)} to {FormatVoltage(maxOffset)}";
                            }
                            OffsetRangeText.Text = rangeText;
                        }
                    }
                }
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
        /// Check if the panel is properly initialized
        /// </summary>
        public bool IsInitialized => isInitialized;

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
        /// Enable or disable the channel
        /// </summary>
        public void SetChannelEnabled(bool enabled)
        {
            if (EnableCheckBox != null && controller != null)
            {
                EnableCheckBox.IsChecked = enabled;
                controller.SetEnabled(enabled);
            }
        }

        /// <summary>
        /// Get current vertical scale
        /// </summary>
        public double GetCurrentScale()
        {
            if (controller != null)
            {
                var settings = controller.GetSettings();
                return settings?.VerticalScale ?? 1.0;
            }
            return 1.0;
        }

        /// <summary>
        /// Set vertical scale
        /// </summary>
        public void SetCurrentScale(double scale)
        {
            if (controller != null)
            {
                controller.SetVerticalScale(scale);
                UpdateOffsetArrowControl(); // Update arrow control after scale change
            }
        }

        /// <summary>
        /// Cleanup method
        /// </summary>
        public void Cleanup()
        {
            try
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
                    controller.Dispose();
                }

                isInitialized = false;
                LogEvent?.Invoke(this, "Channel 1 control panel cleaned up");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error during Channel 1 cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle when the user control is unloaded
        /// </summary>
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Cleanup();
        }
    }
}