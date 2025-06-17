using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;
using DS1000Z_E_USB_Control.Controls;
using Rigol_DS1000Z_E_Control;
using System;
using System.Windows;
using System.Windows.Controls;

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
        private bool isUpdating = false;  // 🔧 ADD THIS LINE

        public event EventHandler<string> LogEvent;
        // Add this event to the class
        public event EventHandler TriggerSourceChanged;
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

        ///// <summary>
        ///// Update the trigger level arrow control
        ///// </summary>
        //public void UpdateTriggerLevelArrowControl()
        //{
        //    if (TriggerLevelArrows == null || controller == null) return;

        //    var settings = controller.GetSettings();
        //    var (minLevel, maxLevel) = GetTriggerLevelRange();

        //    // Update arrow control properties
        //    TriggerLevelArrows.GraticuleSize = 0.1; // 0.1V per graticule step
        //    TriggerLevelArrows.UpdateRange(minLevel, maxLevel);
        //    TriggerLevelArrows.SetValue(settings.EdgeLevel);
        //}

        /// <summary>
        /// Update the trigger level value display
        /// </summary>
        private void UpdateLevelValueDisplay()
        {
            if (LevelValueText == null || TriggerLevelArrows == null) return;

            double value = TriggerLevelArrows.CurrentValue;
            LevelValueText.Text = $"{value:F2}"; // Simple format: "0.00"
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

        #region Trigger Send Methods

        // ADD these methods to your TriggerControlPanel.xaml.cs

        /// <summary>
        /// Wire up additional controls and event handlers  
        /// UPDATED VERSION - Add missing ComboBox SelectionChanged events
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

            // 🔧 ADD MISSING COMBOBOX EVENT HANDLERS:

            // Trigger Mode ComboBox
            if (TriggerModeComboBox != null)
            {
                TriggerModeComboBox.SelectionChanged += TriggerMode_SelectionChanged;
            }

            // Trigger Sweep ComboBox  
            if (TriggerSweepComboBox != null)
            {
                TriggerSweepComboBox.SelectionChanged += TriggerSweep_SelectionChanged;
            }

            // Edge Source ComboBox
            if (EdgeSourceComboBox != null)
            {
                EdgeSourceComboBox.SelectionChanged += EdgeSource_SelectionChanged;
            }

            // Edge Slope ComboBox
            if (EdgeSlopeComboBox != null)
            {
                EdgeSlopeComboBox.SelectionChanged += EdgeSlope_SelectionChanged;
            }

            // Trigger Coupling ComboBox
            if (TriggerCouplingComboBox != null)
            {
                TriggerCouplingComboBox.SelectionChanged += TriggerCoupling_SelectionChanged;
            }

            // Subscribe to controller settings changes
            if (controller != null)
            {
                controller.SettingsChanged += (sender, e) => UpdateTriggerLevelArrowControl();
            }
        }

        // 🔧 ADD THESE NEW EVENT HANDLER METHODS:

        /// <summary>
        /// Handle trigger mode selection changes
        /// </summary>
        private void TriggerMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (controller == null || isUpdating) return;

            var selectedItem = TriggerModeComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag != null)
            {
                string mode = selectedItem.Tag.ToString();
                controller.SetMode(mode);
                LogEvent?.Invoke(this, $"Trigger mode changed to: {mode}");
            }
        }

        /// <summary>
        /// Handle trigger sweep selection changes  
        /// </summary>
        private void TriggerSweep_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (controller == null || isUpdating) return;

            var selectedItem = TriggerSweepComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag != null)
            {
                string sweep = selectedItem.Tag.ToString();
                controller.SetSweep(sweep);
                LogEvent?.Invoke(this, $"Trigger sweep changed to: {sweep}");
            }
        }

        ///// <summary>
        ///// Handle edge source selection changes
        ///// </summary>
        //private void EdgeSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (controller == null || isUpdating) return;

        //    var selectedItem = EdgeSourceComboBox.SelectedItem as ComboBoxItem;
        //    if (selectedItem?.Tag != null)
        //    {
        //        string source = selectedItem.Tag.ToString();
        //        controller.SetEdgeSource(source);
        //        LogEvent?.Invoke(this, $"Trigger source changed to: {source}");
        //    }
        //}

        /// <summary>
        /// Handle edge slope selection changes
        /// </summary>
        private void EdgeSlope_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (controller == null || isUpdating) return;

            var selectedItem = EdgeSlopeComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag != null)
            {
                string slope = selectedItem.Tag.ToString();
                controller.SetEdgeSlope(slope);
                LogEvent?.Invoke(this, $"Trigger slope changed to: {slope}");
            }
        }

        /// <summary>
        /// Handle trigger coupling selection changes
        /// </summary>
        private void TriggerCoupling_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (controller == null || isUpdating) return;

            var selectedItem = TriggerCouplingComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag != null)
            {
                string coupling = selectedItem.Tag.ToString();
                controller.SetCoupling(coupling);
                LogEvent?.Invoke(this, $"Trigger coupling changed to: {coupling}");
            }
        }


        #endregion

        // Add these methods to your TriggerControlPanel.xaml.cs

        #region Dynamic Step Integration

        /// <summary>
        /// Update trigger level control with current channel settings
        /// Call this whenever channel settings or trigger source changes
        /// </summary>
        public void UpdateTriggerLevelControl(Ch1Settings ch1Settings, Ch2Settings ch2Settings)
        {
            if (controller == null || ch1Settings == null || ch2Settings == null) return;

            try
            {
                controller.UpdateTriggerLevelControl(ch1Settings, ch2Settings);
                LogEvent?.Invoke(this, "Trigger level control updated with dynamic steps");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error updating trigger level control: {ex.Message}");
            }
        }

        /// <summary>
        /// UPDATED: Enhanced UpdateTriggerLevelArrowControl with dynamic steps
        /// </summary>
        private void UpdateTriggerLevelArrowControl()
        {
            if (TriggerLevelArrows == null || controller == null) return;

            try
            {
                var settings = controller.GetSettings();

                // Use default range if channel settings not available
                var (minLevel, maxLevel) = settings.GetTriggerLevelRange();

                // Try to get current channel settings from MainWindow if available
                // This is a fallback - ideally MainWindow should call UpdateTriggerLevelControl directly
                TriggerLevelArrows.UpdateRange(minLevel, maxLevel);
                TriggerLevelArrows.SetValue(settings.EdgeLevel);

                UpdateLevelValueDisplay();
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error updating trigger level arrow control: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle trigger source changes - update step size when source changes
        /// UPDATED: EdgeSource_SelectionChanged with dynamic step update
        /// </summary>
        private void EdgeSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (controller == null || isUpdating) return;

            var selectedItem = EdgeSourceComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag != null)
            {
                string source = selectedItem.Tag.ToString();
                bool success = controller.SetEdgeSource(source);

                if (success)
                {
                    LogEvent?.Invoke(this, $"Trigger source changed to: {source}");

                    // Request MainWindow to update trigger level control with current channel settings
                    TriggerSourceChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        #endregion




    }
}