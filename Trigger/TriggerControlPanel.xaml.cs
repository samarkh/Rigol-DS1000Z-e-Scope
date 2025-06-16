using System;
using System.Windows;
using System.Windows.Controls;
using Rigol_DS1000Z_E_Control;
using DS1000Z_E_USB_Control.Controls;

namespace DS1000Z_E_USB_Control.Trigger
{
    /// <summary>
    /// Interaction logic for TriggerControlPanel.xaml
    /// Updated to use EmojiArrows multimedia controls instead of slider
    /// </summary>
    public partial class TriggerControlPanel : UserControl
    {
        private TriggerController controller;
        private bool isInitialized = false;

        public event EventHandler<string> LogEvent;

        public TriggerControlPanel()
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
                controller = new TriggerController(oscilloscope);
                controller.LogEvent += (sender, message) => LogEvent?.Invoke(this, message);
                controller.SettingsChanged += (sender, e) => LogEvent?.Invoke(this, "Trigger settings changed");

                // Wire up UI controls to the controller
                WireUpControlsToController();

                // Wire up additional event handlers
                WireUpAdditionalControls();

                // Initialize the controller
                controller.InitializeControls();

                // Set up UI elements
                SetupUI();

                isInitialized = true;
                LogEvent?.Invoke(this, "Trigger control panel initialized");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error initializing Trigger control panel: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize with settings manager (overload for MainWindow compatibility)
        /// </summary>
        public void Initialize(RigolDS1000ZE oscilloscope, object settingsManager)
        {
            // Just call the main Initialize method - we don't use the settings manager
            Initialize(oscilloscope);
        }

        /// <summary>
        /// Wire up UI controls to the controller
        /// </summary>
        private void WireUpControlsToController()
        {
            // Basic combo box controls
            controller.TriggerModeComboBox = TriggerModeComboBox;
            controller.TriggerSweepComboBox = TriggerSweepComboBox;
            controller.EdgeSourceComboBox = EdgeSourceComboBox;
            controller.EdgeSlopeComboBox = EdgeSlopeComboBox;
            controller.TriggerCouplingComboBox = TriggerCouplingComboBox;

            // Text controls
            controller.HoldoffTextBox = HoldoffTextBox;
            controller.CurrentTriggerSettingsText = CurrentTriggerSettingsText;

            // Multimedia controls
            controller.TriggerLevelArrows = TriggerLevelArrows;
            controller.LevelValueText = LevelValueText;

            // Button controls
            controller.ForceTriggerButton = ForceTriggerButton;
        }

        /// <summary>
        /// Wire up additional controls and event handlers
        /// </summary>
        private void WireUpAdditionalControls()
        {
            // Force trigger button
            if (ForceTriggerButton != null)
            {
                ForceTriggerButton.Click += ForceTrigger_Click;
            }

            // Trigger level arrows (multimedia control)
            if (TriggerLevelArrows != null)
            {
                TriggerLevelArrows.GraticuleMovement += TriggerLevelArrows_GraticuleMovement;
            }

            // Holdoff text box
            if (HoldoffTextBox != null)
            {
                HoldoffTextBox.TextChanged += HoldoffTextBox_TextChanged;
                HoldoffTextBox.LostFocus += HoldoffTextBox_LostFocus;
            }

            // Subscribe to controller settings changes
            if (controller != null)
            {
                controller.SettingsChanged += (sender, e) => UpdateTriggerLevelArrowControl();
            }
        }

        /// <summary>
        /// Set up UI elements
        /// </summary>
        private void SetupUI()
        {
            UpdateHoldoffDisplay();
            UpdateTriggerLevelArrowControl();
        }

        #region Event Handlers

        /// <summary>
        /// Handle trigger level arrow movement
        /// </summary>
        private void TriggerLevelArrows_GraticuleMovement(object sender, GraticuleMovementEventArgs e)
        {
            if (controller == null) return;

            controller.HandleTriggerLevelChanged(e.NewValue);
            UpdateLevelValueDisplay();
            LogEvent?.Invoke(this, $"Trigger level moved {e.GraticuleMultiplier:F1} graticule to {e.NewValue:F1}V");
        }

        /// <summary>
        /// Handle holdoff text box changes
        /// </summary>
        private void HoldoffTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateHoldoffDisplay();
        }

        /// <summary>
        /// Handle holdoff text box lost focus (commit the value)
        /// </summary>
        private void HoldoffTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (controller == null || HoldoffTextBox == null) return;

            if (double.TryParse(HoldoffTextBox.Text, out double holdoff))
            {
                controller.SetHoldoff(holdoff);
            }
            else
            {
                // Reset to last known good value
                var settings = controller.GetSettings();
                HoldoffTextBox.Text = settings.Holdoff.ToString("E3");
                LogEvent?.Invoke(this, "Invalid holdoff value - reset to previous value");
            }
        }

        /// <summary>
        /// Force trigger button handler
        /// </summary>
        private void ForceTrigger_Click(object sender, RoutedEventArgs e)
        {
            controller?.ForceTrigger();
            LogEvent?.Invoke(this, "Trigger forced");
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// Update the trigger level arrow control
        /// </summary>
        public void UpdateTriggerLevelArrowControl()
        {
            if (TriggerLevelArrows == null || controller == null) return;

            var settings = controller.GetSettings();
            var (minLevel, maxLevel) = GetTriggerLevelRange();

            // Update arrow control properties
            TriggerLevelArrows.GraticuleSize = 0.1; // 0.1V per graticule step
            TriggerLevelArrows.UpdateRange(minLevel, maxLevel);
            TriggerLevelArrows.SetValue(settings.EdgeLevel);
        }

        /// <summary>
        /// Update the trigger level value display
        /// </summary>
        private void UpdateLevelValueDisplay()
        {
            if (LevelValueText == null || TriggerLevelArrows == null) return;

            double value = TriggerLevelArrows.CurrentValue;
            LevelValueText.Text = $"{value:F1}"; // Simple format: "0.0"
        }

        /// <summary>
        /// Update the holdoff display text
        /// </summary>
        private void UpdateHoldoffDisplay()
        {
            if (HoldoffDisplayText == null || HoldoffTextBox == null) return;

            if (double.TryParse(HoldoffTextBox.Text, out double holdoff))
            {
                HoldoffDisplayText.Text = $"({FormatTime(holdoff)})";
            }
            else
            {
                HoldoffDisplayText.Text = "(Invalid)";
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get trigger level range based on source channel settings
        /// </summary>
        private (double min, double max) GetTriggerLevelRange()
        {
            // You'll need to get channel settings from the main application
            // For now, return reasonable default range
            return (-5.0, 5.0);

            // TODO: Replace with actual channel-based range calculation:
            // var ch1Settings = GetChannel1Settings(); // Get from main app
            // var ch2Settings = GetChannel2Settings(); // Get from main app
            // var settings = controller.GetSettings();
            // return settings.GetTriggerLevelRange(ch1Settings, ch2Settings);
        }

        /// <summary>
        /// Format time value for display
        /// </summary>
        private string FormatTime(double time)
        {
            if (time >= 1.0)
                return $"{time:F3}s";
            else if (time >= 0.001)
                return $"{time * 1000:F1}ms";
            else if (time >= 0.000001)
                return $"{time * 1000000:F1}µs";
            else
                return $"{time * 1000000000:F1}ns";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get the controller for external access
        /// </summary>
        public TriggerController GetController()
        {
            return controller;
        }

        /// <summary>
        /// Update trigger level range from external source
        /// </summary>
        public void UpdateTriggerLevelRange(double minLevel, double maxLevel)
        {
            if (TriggerLevelArrows == null) return;

            TriggerLevelArrows.UpdateRange(minLevel, maxLevel);
            LogEvent?.Invoke(this, $"Trigger level range updated: {minLevel:F1}V to {maxLevel:F1}V");
        }

        /// <summary>
        /// Refresh settings from oscilloscope
        /// </summary>
        public void RefreshSettings()
        {
            controller?.RefreshSettings();
        }

        /// <summary>
        /// Update from settings (for MainWindow compatibility)
        /// </summary>
        public void UpdateFromSettings()
        {
            controller?.RefreshSettings();
        }

        /// <summary>
        /// Update from settings with TriggerSettings parameter (for MainWindow compatibility)
        /// </summary>
        public void UpdateFromSettings(object triggerSettings)
        {
            // For now, just refresh from oscilloscope instead of using the passed settings
            // This maintains compatibility while using our simplified approach
            controller?.RefreshSettings();
            LogEvent?.Invoke(this, "Trigger panel updated from settings");
        }

        /// <summary>
        /// Set enabled state (for MainWindow compatibility)
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.IsEnabled = enabled;

            if (enabled)
            {
                LogEvent?.Invoke(this, "Trigger control panel enabled");
            }
            else
            {
                LogEvent?.Invoke(this, "Trigger control panel disabled");
            }
        }

        #endregion
    }
}