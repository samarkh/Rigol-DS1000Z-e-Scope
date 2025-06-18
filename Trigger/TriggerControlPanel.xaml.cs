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
        #region Fields

        private TriggerController controller;
        private bool isInitialized = false;
        private bool isUpdating = false;

        #endregion

        #region Events

        public event EventHandler<string> LogEvent;
        public event EventHandler TriggerSourceChanged;

        #endregion

        #region Constructor

        public TriggerControlPanel()
        {
            InitializeComponent();
        }

        #endregion

        #region Initialization

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
                ForceTriggerButton.Click -= ForceTrigger_Click;  // Unsubscribe first
                ForceTriggerButton.Click += ForceTrigger_Click;   // Then subscribe
            }

            // Trigger level arrows (multimedia control)
            if (TriggerLevelArrows != null)
            {
                TriggerLevelArrows.GraticuleMovement -= TriggerLevelArrows_GraticuleMovement;  // Unsubscribe first
                TriggerLevelArrows.GraticuleMovement += TriggerLevelArrows_GraticuleMovement;   // Then subscribe
            }

            // Holdoff text box
            if (HoldoffTextBox != null)
            {
                HoldoffTextBox.TextChanged -= HoldoffTextBox_TextChanged;   // Unsubscribe first
                HoldoffTextBox.LostFocus -= HoldoffTextBox_LostFocus;       // Unsubscribe first
                HoldoffTextBox.TextChanged += HoldoffTextBox_TextChanged;   // Then subscribe
                HoldoffTextBox.LostFocus += HoldoffTextBox_LostFocus;       // Then subscribe
            }

            // ComboBox event handlers - FIXED: Prevent duplicates
            if (TriggerModeComboBox != null)
            {
                TriggerModeComboBox.SelectionChanged -= TriggerMode_SelectionChanged;  // Unsubscribe first
                TriggerModeComboBox.SelectionChanged += TriggerMode_SelectionChanged;   // Then subscribe
            }

            if (TriggerSweepComboBox != null)
            {
                TriggerSweepComboBox.SelectionChanged -= TriggerSweep_SelectionChanged;  // Unsubscribe first
                TriggerSweepComboBox.SelectionChanged += TriggerSweep_SelectionChanged;   // Then subscribe
            }

            if (EdgeSourceComboBox != null)
            {
                EdgeSourceComboBox.SelectionChanged -= EdgeSource_SelectionChanged;  // Unsubscribe first
                EdgeSourceComboBox.SelectionChanged += EdgeSource_SelectionChanged;   // Then subscribe
            }

            if (EdgeSlopeComboBox != null)
            {
                EdgeSlopeComboBox.SelectionChanged -= EdgeSlope_SelectionChanged;  // Unsubscribe first
                EdgeSlopeComboBox.SelectionChanged += EdgeSlope_SelectionChanged;   // Then subscribe
            }

            if (TriggerCouplingComboBox != null)
            {
                TriggerCouplingComboBox.SelectionChanged -= TriggerCoupling_SelectionChanged;  // Unsubscribe first
                TriggerCouplingComboBox.SelectionChanged += TriggerCoupling_SelectionChanged;   // Then subscribe
            }

            // Subscribe to controller settings changes - FIXED: Prevent duplicates
            if (controller != null)
            {
                controller.SettingsChanged -= (sender, e) => UpdateTriggerLevelArrowControl();  // Unsubscribe first
                controller.SettingsChanged += (sender, e) => UpdateTriggerLevelArrowControl();   // Then subscribe
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

        #endregion

        #region Simple Trigger Step Methods (NEW)

        /// <summary>
        /// SIMPLE: Update trigger step size by reading the current trigger source channel's ComboBox
        /// </summary>
        public void UpdateTriggerStepsFromUI()
        {
            if (TriggerLevelArrows == null || EdgeSourceComboBox == null) return;

            try
            {
                // Get current trigger source
                var sourceItem = EdgeSourceComboBox.SelectedItem as ComboBoxItem;
                string source = sourceItem?.Tag?.ToString() ?? "CHANnel1";

                // Simple step size based on source
                double stepSize = source.ToUpper() switch
                {
                    "CHAN1" or "CHANnel1" => 0.1,  // 100mV steps for CH1
                    "CHAN2" or "CHANnel2" => 0.1,  // 100mV steps for CH2
                    _ => 0.1  // Default 100mV
                };

                // Update the arrow control with simple steps
                TriggerLevelArrows.GraticuleSize = stepSize;

                LogEvent?.Invoke(this, $"Updated trigger steps for {source}: {stepSize:F3}V");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error updating trigger steps: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle trigger level arrow movement
        /// </summary>
        private void TriggerLevelArrows_GraticuleMovement(object sender, GraticuleMovementEventArgs e)
        {
            if (controller == null) return;

            controller.SetEdgeLevel(e.NewValue);
            UpdateLevelValueDisplay();
            LogEvent?.Invoke(this, $"Trigger level changed to: {e.NewValue:F3}V");
        }

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

        /// <summary>
        /// Handle edge source selection changes
        /// UPDATED: Now includes simple trigger step update
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
                    LogEvent?.Invoke(this, $"✅ Trigger source changed to: {source}");

                    // SIMPLE: Just read the ComboBox and update steps immediately!
                    UpdateTriggerStepsFromUI();

                    // Notify listeners that trigger source changed
                    TriggerSourceChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    LogEvent?.Invoke(this, $"❌ Failed to change trigger source to: {source}");
                    // Revert the ComboBox selection if the command failed
                    if (!isUpdating)
                    {
                        isUpdating = true;
                        controller.RefreshSettings(); // This will update UI with actual oscilloscope state
                        isUpdating = false;
                    }
                }
            }
        }

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
        private void UpdateTriggerLevelArrowControl()
        {
            if (TriggerLevelArrows == null || controller == null) return;

            try
            {
                var settings = controller.GetSettings();
                var (minLevel, maxLevel) = settings.GetTriggerLevelRange();

                // Update arrow control properties
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
        /// Update the trigger level value display
        /// </summary>
        private void UpdateLevelValueDisplay()
        {
            if (LevelValueText == null || TriggerLevelArrows == null) return;

            double value = TriggerLevelArrows.CurrentValue;
            LevelValueText.Text = $"{value:F2}"; // Simple format: "0.00"
        }

        #endregion

        #region HoldOff Units Handling

        /// <summary>
        /// Handle holdoff units selection changes
        /// </summary>
        private void HoldOff_Units_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateHoldoffDisplay();
        }

        /// <summary>
        /// Get the currently selected holdoff units
        /// </summary>
        private string GetSelectedHoldoffUnits()
        {
            if (HoldOffUnitsComboBox?.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Tag?.ToString() ?? "ns";
            }
            return "ns"; // Default to nanoseconds
        }

        /// <summary>
        /// Update the holdoff display text with proper formatting
        /// </summary>
        private void UpdateHoldoffDisplay()
        {
            if (HoldoffDisplayText == null || HoldoffTextBox == null) return;

            if (double.TryParse(HoldoffTextBox.Text, out double holdoff))
            {
                string selectedUnits = GetSelectedHoldoffUnits();
                string formattedValue = FormatTimeWithUnits(holdoff, selectedUnits);
                HoldoffDisplayText.Text = $"({formattedValue})";
            }
            else
            {
                HoldoffDisplayText.Text = "(Invalid)";
            }
        }

        /// <summary>
        /// Format time value with specified units to 2 decimal places
        /// </summary>
        private string FormatTimeWithUnits(double timeInSeconds, string units)
        {
            if (timeInSeconds == 0) return "0.00s";

            double value;
            string unitSymbol;

            switch (units.ToLower())
            {
                case "s":
                    value = timeInSeconds;
                    unitSymbol = "s";
                    break;
                case "ms":
                    value = timeInSeconds * 1000;
                    unitSymbol = "ms";
                    break;
                case "us":
                    value = timeInSeconds * 1000000;
                    unitSymbol = "μs";
                    break;
                case "ns":
                    value = timeInSeconds * 1000000000;
                    unitSymbol = "ns";
                    break;
                default:
                    // Auto-select best units if invalid unit specified
                    return FormatTimeAuto(timeInSeconds);
            }

            return $"{value:F2}{unitSymbol}";
        }

        /// <summary>
        /// Auto-format time value with appropriate units (fallback method)
        /// </summary>
        private string FormatTimeAuto(double timeInSeconds)
        {
            if (timeInSeconds == 0) return "0.00s";

            double absTime = Math.Abs(timeInSeconds);

            if (absTime >= 1.0)
                return $"{timeInSeconds:F2}s";
            else if (absTime >= 1e-3)
                return $"{timeInSeconds * 1000:F2}ms";
            else if (absTime >= 1e-6)
                return $"{timeInSeconds * 1000000:F2}μs";
            else if (absTime >= 1e-9)
                return $"{timeInSeconds * 1000000000:F2}ns";
            else
                return $"{timeInSeconds:E2}s";
        }

        /// <summary>
        /// Set the holdoff units combo box to match a time value (helper method)
        /// </summary>
        private void SetOptimalHoldoffUnits(double timeInSeconds)
        {
            if (HoldOffUnitsComboBox == null) return;

            double absTime = Math.Abs(timeInSeconds);
            string optimalUnit;

            if (absTime >= 1.0)
                optimalUnit = "s";
            else if (absTime >= 1e-3)
                optimalUnit = "ms";
            else if (absTime >= 1e-6)
                optimalUnit = "us";
            else
                optimalUnit = "ns";

            // Select the optimal unit in the combo box
            foreach (ComboBoxItem item in HoldOffUnitsComboBox.Items)
            {
                if (item.Tag?.ToString() == optimalUnit)
                {
                    HoldOffUnitsComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// Format time value with appropriate units (updated for 2 decimal places)
        /// </summary>
        private string FormatTime(double time)
        {
            return FormatTimeAuto(time);
        }

        #endregion

        #region Legacy Compatibility Methods

        /// <summary>
        /// Update trigger level control with current channel settings
        /// (Legacy method for compatibility - now redirects to simple method)
        /// </summary>
        public void UpdateTriggerLevelControl(Ch1Settings ch1Settings, Ch2Settings ch2Settings)
        {
            // Use the simple method instead of complex event chains
            UpdateTriggerStepsFromUI();
            LogEvent?.Invoke(this, "Trigger level control updated with dynamic steps");
        }

        /// <summary>
        /// Update from settings (legacy compatibility)
        /// </summary>
        public void UpdateFromSettings(object triggerSettings)
        {
            // Refresh the controller with oscilloscope instead of using the passed settings
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

        /// <summary>
        /// Get the trigger controller for external access
        /// </summary>
        public TriggerController GetController()
        {
            return controller;
        }

        #endregion

    } // End of TriggerControlPanel class
} // End of namespace DS1000Z_E_USB_Control.Trigger