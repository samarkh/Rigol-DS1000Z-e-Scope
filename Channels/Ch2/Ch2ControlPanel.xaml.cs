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

        public void Initialize(RigolDS1000ZE oscilloscope)
        {
            if (isInitialized) return;

            controller = new Ch2Controller(oscilloscope);
            controller.LogEvent += (sender, message) => LogEvent?.Invoke(this, message);
            controller.SettingsChanged += (sender, e) => LogEvent?.Invoke(this, "Channel 2 settings changed");

            controller.ProbeRatioComboBox = ProbeRatioComboBox;
            controller.VerticalScaleComboBox = VerticalScaleComboBox;
            controller.CouplingComboBox = CouplingComboBox;
            controller.CurrentSettingsTextBlock = CurrentSettingsText;

            if (VerticalOffsetArrows != null)
            {
                VerticalOffsetArrows.GraticuleMovement += VerticalOffsetArrows_GraticuleMovement;
                controller.OffsetArrowsControl = VerticalOffsetArrows;
            }

            controller.InitializeControls();
            UpdateOffsetControl();
            UpdateButtonAppearance();

            isInitialized = true;
            LogEvent?.Invoke(this, "Channel 2 control panel initialized");
        }

        private void EnableChannelButton_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null || isUpdating) return;

            isChannelEnabled = !isChannelEnabled;
            bool success = controller.SetEnabled(isChannelEnabled);

            if (success)
            {
                UpdateButtonAppearance();
                LogEvent?.Invoke(this, $"Channel 2 {(isChannelEnabled ? "enabled" : "disabled")}");
            }
            else
            {
                isChannelEnabled = !isChannelEnabled;
                LogEvent?.Invoke(this, "Failed to change Channel 2 state");
            }
        }

        private void UpdateButtonAppearance()
        {
            if (EnableChannelButton != null)
            {
                if (isChannelEnabled)
                {
                    EnableChannelButton.Content = "Disable CH2";
                    EnableChannelButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x44, 0x44));
                    EnableChannelButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xE6, 0xE6));
                    EnableChannelButton.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x44, 0x44));
                }
                else
                {
                    EnableChannelButton.Content = "Enable CH2";
                    EnableChannelButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x41, 0x69, 0xE1));
                    EnableChannelButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE8, 0xF4, 0xFD));
                    EnableChannelButton.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x41, 0x69, 0xE1));
                }
            }
        }

        private void VerticalOffsetArrows_GraticuleMovement(object sender, GraticuleMovementEventArgs e)
        {
            if (controller != null && !isUpdating)
            {
                double newOffset = e.NewValue;
                controller.SetVerticalOffset(newOffset);
                UpdateDisplays();
                LogEvent?.Invoke(this, $"CH2 offset changed by {e.Increment:F3}V to {newOffset:F3}V (Movement: {e.MovementType})");
            }
        }

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

        public void UpdateOffsetControl()
        {
            if (VerticalOffsetArrows == null || controller == null || isUpdating) return;

            var settings = controller.GetSettings();
            var (minOffset, maxOffset) = settings.GetOffsetRange();

            VerticalOffsetArrows.GraticuleSize = settings.VerticalScale;
            VerticalOffsetArrows.UpdateRange(minOffset, maxOffset);
            VerticalOffsetArrows.SetValue(settings.VerticalOffset);
        }

        private void UpdateDisplays()
        {
            if (controller != null)
            {
                UpdateOffsetControl();
            }
        }

        private void QuickZero_Click(object sender, RoutedEventArgs e)
        {
            if (controller != null)
            {
                controller.SetVerticalOffset(0);
                LogEvent?.Invoke(this, "CH2 offset reset to zero");
            }
        }

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

        public Ch2Controller GetController() => controller;

        public new bool IsInitialized => isInitialized;

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

        public bool IsChannelEnabled => isChannelEnabled;

        public void RefreshDisplays()
        {
            if (isInitialized && !isUpdating)
            {
                UpdateDisplays();
            }
        }

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
