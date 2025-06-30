using System;
using System.Windows;
using System.Windows.Controls;
using Rigol_DS1000Z_E_Control;
using DS1000Z_E_USB_Control.Controls;

namespace DS1000Z_E_USB_Control.Channels.Ch2
{
    /// <summary>
    /// Simplified Ch2ControlPanel with multimedia arrow controls
    /// Clean implementation focused on voltage controls only (no time data)
    /// Updated to use EnableChannelButton instead of EnableCheckBox
    /// </summary>
    public partial class Ch2ControlPanel : UserControl
    {
        private Ch2Controller controller;
        private bool isUpdating = false;
        private bool isInitialized = false;
        private bool isChannelEnabled = false;

        public event EventHandler<string> LogEvent;

        public Ch2ControlPanel()
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
            controller = new Ch2Controller(oscilloscope);
            controller.LogEvent += (sender, message) => LogEvent?.Invoke(this, message);
            controller.SettingsChanged += (sender, e) => LogEvent?.Invoke(this, "Channel 2 settings changed");

            // Wire up basic UI controls to the controller
            controller.ProbeRatioComboBox = ProbeRatioComboBox;
            controller.VerticalScaleComboBox = VerticalScaleComboBox;
            controller.CouplingComboBox = CouplingComboBox;
            controller.CurrentSettingsTextBlock = CurrentSettingsText;

            // Wire up offset arrows control
            if (VerticalOffsetArrows != null)
            {
                VerticalOffsetArrows.GraticuleMovement += VerticalOffsetArrows_GraticuleMovement;
                controller.OffsetArrowsControl = VerticalOffsetArrows;
            }

            // Initialize the controller
            controller.InitializeControls();

            // Update offset control
            UpdateOffsetControl();

            // Update button appearance based on current state
            UpdateButtonAppearance();

            isInitialized = true;
            LogEvent?.Invoke(this, "Channel 2 control panel initialized");
        }

        /// <summary>
        /// Handle the EnableChannelButton click event
        /// </summary>
        private void EnableChannelButton_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null || isUpdating) return;

            // Toggle the channel enabled state
            isChannelEnabled = !isChannelEnabled;

            // Send command to oscilloscope
            bool success = controller.SetEnabled(isChannelEnabled);

            if (success)
            {
                UpdateButtonAppearance();
                LogEvent?.Invoke(this, $"Channel 2 {(isChannelEnabled ? "enabled" : "disabled")}");
            }
            else
            {
                // Revert the state if command failed
                isChannelEnabled = !isChannelEnabled;
                LogEvent?.Invoke(this, "Failed to change Channel 2 state");
            }
        }

        /// <summary>
        /// Update the button appearance based on channel state
        /// </summary>
        private void UpdateButtonAppearance()
        {
            if (EnableChannelButton != null)
            {
                if (isChannelEnabled)
                {
                    EnableChannelButton.Content = "Disable CH2";
                    EnableChannelButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x44, 0x44)); // Red text
                    EnableChannelButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xE6, 0xE6)); // Light red background
                    EnableChannelButton.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x44, 0x44)); // Red border
                }
                else
                {
                    EnableChannelButton.Content = "Enable CH2";
                    EnableChannelButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x41, 0x69, 0xE1)); // Blue text
                    EnableChannelButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE8, 0xF4, 0xFD)); // Light blue background
                    EnableChannelButton.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x41, 0x69, 0xE1)); // Blue border
                }
            }
        }

        /// <summary>
        /// Handle offset arrows movement
        /// </summary>
        private void VerticalOffsetArrows_GraticuleMovement(object sender, GraticuleMovementEventArgs e)
        {
            if (controller != null && !isUpdating)
            {
                // Use the new value from the event args directly
                double newOffset = e.NewValue;

                // Apply the new offset
                controller.SetVerticalOffset(newOffset);

                // Update displays
                UpdateDisplays();

                LogEvent?.Invoke(this, $"CH2 offset changed by {e.Increment:F3}V to {newOffset:F3}V (Movement: {e.MovementType})");
            }
        }

        /// <summary>
        /// Handle when controller settings change
        /// </summary>
        private void OnControllerSettingsChanged()
        {
            if (!isUpdating)
            {
                isUpdating = true;
                try
                {
                    UpdateDisplays();
                }
                finally
                {
                    isUpdating = false;
                }
            }
        }

        /// <summary>
        /// Update offset control with current settings
        /// </summary>
        public void UpdateOffsetControl()
        {
            if (VerticalOffsetArrows == null || controller == null || isUpdating) return;

            var settings = controller.GetSettings();
            var (minOffset, maxOffset) = settings.GetOffsetRange();

            VerticalOffsetArrows.GraticuleSize = settings.VerticalScale;
            VerticalOffsetArrows.UpdateRange(minOffset, maxOffset);
            VerticalOffsetArrows.SetValue(settings.VerticalOffset);
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
                LogEvent?.Invoke(this, "CH2 offset reset to zero");
            }
        }

        /// <summary>
        /// Format voltage values for display
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
        public Ch2Controller GetController()
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

            if (controller != null)
            {
                isChannelEnabled = enabled;
                controller.SetEnabled(enabled);
                UpdateButtonAppearance();
            }
        }

        /// <summary>
        /// Get the current channel enabled state
        /// </summary>
        public bool IsChannelEnabled => isChannelEnabled;

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