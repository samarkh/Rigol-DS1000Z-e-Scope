using System;
using System.Windows;
using System.Windows.Controls;
using Rigol_DS1000Z_E_Control;

namespace DS1000Z_E_USB_Control.Trigger
{
    /// <summary>
    /// Interaction logic for TriggerControlPanel.xaml
    /// Trigger control UI UserControl
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

            // Create the controller
            controller = new TriggerController(oscilloscope);
            controller.LogEvent += (sender, message) => LogEvent?.Invoke(this, message);
            controller.SettingsChanged += (sender, e) => LogEvent?.Invoke(this, "Trigger settings changed");

            // Wire up UI controls to the controller
            controller.TriggerModeComboBox = TriggerModeComboBox;
            controller.TriggerSweepComboBox = TriggerSweepComboBox;
            controller.EdgeSourceComboBox = EdgeSourceComboBox;
            controller.EdgeSlopeComboBox = EdgeSlopeComboBox;
            controller.TriggerCouplingComboBox = TriggerCouplingComboBox;
            controller.NoiseRejectCheckBox = NoiseRejectCheckBox;
            controller.HoldoffTextBox = HoldoffTextBox;
            controller.TriggerLevelSlider = TriggerLevelSlider;
            controller.LevelValueText = LevelValueText;
            controller.CurrentTriggerSettingsText = CurrentTriggerSettingsText;
            controller.TriggerStatusText = TriggerStatusText;

            // Wire up enhanced UI controls
            controller.MaxLevelDisplay = MaxLevelDisplay;
            controller.MinLevelDisplay = MinLevelDisplay;
            controller.LevelRangeText = LevelRangeText;
            controller.HoldoffDisplayText = HoldoffDisplayText;
            controller.ForceTriggerButton = ForceTriggerButton;
            controller.QuickZeroLevelButton = QuickZeroLevelButton;

            // Wire up additional controls not handled by the base controller
            WireUpAdditionalControls();

            // Initialize the controller
            controller.InitializeControls();

            // Set up additional UI elements
            SetupEnhancedUI();

            isInitialized = true;
            LogEvent?.Invoke(this, "Trigger control panel initialized");
        }

        /// <summary>
        /// Wire up additional controls not handled by the base controller
        /// </summary>
        private void WireUpAdditionalControls()
        {
            if (ForceTriggerButton != null)
            {
                ForceTriggerButton.Click += ForceTrigger_Click;
            }

            if (QuickZeroLevelButton != null)
            {
                QuickZeroLevelButton.Click += QuickZeroLevel_Click;
            }

            if (TriggerLevelSlider != null)
            {
                TriggerLevelSlider.ValueChanged += TriggerLevelSlider_ValueChanged;
            }

            if (HoldoffTextBox != null)
            {
                HoldoffTextBox.TextChanged += HoldoffTextBox_TextChanged;
                HoldoffTextBox.LostFocus += HoldoffTextBox_LostFocus;
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
            UpdateHoldoffDisplay();
        }

        /// <summary>
        /// Handle trigger level slider changes
        /// </summary>
        private void TriggerLevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (controller == null) return;

            UpdateLevelValueDisplay();
            controller.HandleTriggerLevelChanged(e.NewValue);
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

        /// <summary>
        /// Quick zero level button handler
        /// </summary>
        private void QuickZeroLevel_Click(object sender, RoutedEventArgs e)
        {
            controller?.SetEdgeLevel(0);
            LogEvent?.Invoke(this, "Trigger level zeroed");
        }

        /// <summary>
        /// Update the trigger level value display
        /// </summary>
        private void UpdateLevelValueDisplay()
        {
            if (LevelValueText == null || TriggerLevelSlider == null) return;

            double value = TriggerLevelSlider.Value;
            LevelValueText.Text = FormatVoltage(value);
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

        /// <summary>
        /// Update the min/max range displays
        /// </summary>
        public void UpdateRangeDisplays()
        {
            if (controller == null) return;

            var settings = controller.GetSettings();
            var (minLevel, maxLevel) = settings.GetTriggerLevelRange();

            if (MaxLevelDisplay != null)
            {
                MaxLevelDisplay.Text = FormatVoltage(maxLevel);
            }

            if (MinLevelDisplay != null)
            {
                MinLevelDisplay.Text = FormatVoltage(minLevel);
            }

            if (LevelRangeText != null)
            {
                string rangeText;
                if (Math.Abs(minLevel) == Math.Abs(maxLevel))
                {
                    rangeText = $"Range: ±{FormatVoltage(maxLevel).Replace("+", "")}";
                }
                else
                {
                    rangeText = $"Range: {FormatVoltage(minLevel)} to {FormatVoltage(maxLevel)}";
                }
                LevelRangeText.Text = rangeText;
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
                return $"{voltage:F3}V";
            else if (Math.Abs(voltage) >= 0.001)
                return $"{voltage * 1000:F1}mV";
            else
                return $"{voltage * 1000000:F0}µV";
        }

        /// <summary>
        /// Smart time formatting
        /// </summary>
        private string FormatTime(double time)
        {
            if (time >= 1.0)
                return $"{time:F3} s";
            else if (time >= 1e-3)
                return $"{time * 1000:F1} ms";
            else if (time >= 1e-6)
                return $"{time * 1000000:F1} µs";
            else
                return $"{time * 1000000000:F1} ns";
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
            UpdateLevelValueDisplay();
            UpdateHoldoffDisplay();
        }

        /// <summary>
        /// Get current trigger settings
        /// </summary>
        public TriggerSettings GetSettings()
        {
            return controller?.GetSettings();
        }

        /// <summary>
        /// Set trigger settings (sends commands to oscilloscope)
        /// </summary>
        public void SetSettings(TriggerSettings settings)
        {
            controller?.SetSettings(settings);
            UpdateRangeDisplays();
            UpdateLevelValueDisplay();
            UpdateHoldoffDisplay();
        }

        /// <summary>
        /// Update UI from settings (does NOT send commands to oscilloscope)
        /// </summary>
        public void UpdateFromSettings(TriggerSettings settings)
        {
            controller?.UpdateFromSettings(settings);
            UpdateRangeDisplays();
            UpdateLevelValueDisplay();
            UpdateHoldoffDisplay();
        }

        /// <summary>
        /// Apply a preset configuration
        /// </summary>
        public void ApplyPreset(TriggerSettings preset)
        {
            SetSettings(preset);
            LogEvent?.Invoke(this, $"Applied trigger preset: {preset}");
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Cleanup()
        {
            controller?.Dispose();
            controller = null;
        }


        // ============================================================================
        // FIX 1: Add missing Initialize overload to TriggerControlPanel.xaml.cs
        // ============================================================================

        // Add this method to TriggerControlPanel class:
        /// <summary>
        /// ADDED: Initialize overload with settings manager - fixes CS1501 error on line 72
        /// </summary>
        public void Initialize(RigolDS1000ZE oscilloscope, OscilloscopeSettingsManager settingsManager)
        {
            if (isInitialized) return;

            try
            {
                // Create the controller with settings manager reference
                controller = new TriggerController(oscilloscope, settingsManager);
                controller.LogEvent += (sender, message) => LogEvent?.Invoke(this, message);
                controller.SettingsChanged += (sender, e) => LogEvent?.Invoke(this, "Trigger settings changed");

                // Wire up UI controls to the controller (same as single-parameter version)
                controller.TriggerModeComboBox = TriggerModeComboBox;
                controller.TriggerSweepComboBox = TriggerSweepComboBox;
                controller.EdgeSourceComboBox = EdgeSourceComboBox;
                controller.EdgeSlopeComboBox = EdgeSlopeComboBox;
                controller.TriggerCouplingComboBox = TriggerCouplingComboBox;
                controller.NoiseRejectCheckBox = NoiseRejectCheckBox;
                controller.HoldoffTextBox = HoldoffTextBox;
                controller.TriggerLevelSlider = TriggerLevelSlider;
                controller.LevelValueText = LevelValueText;
                controller.CurrentTriggerSettingsText = CurrentTriggerSettingsText;
                controller.TriggerStatusText = TriggerStatusText;

                // Wire up enhanced UI controls if they exist
                if (MaxLevelDisplay != null) controller.MaxLevelDisplay = MaxLevelDisplay;
                if (MinLevelDisplay != null) controller.MinLevelDisplay = MinLevelDisplay;
                if (LevelRangeText != null) controller.LevelRangeText = LevelRangeText;
                if (HoldoffDisplayText != null) controller.HoldoffDisplayText = HoldoffDisplayText;
                if (ForceTriggerButton != null) controller.ForceTriggerButton = ForceTriggerButton;
                if (QuickZeroLevelButton != null) controller.QuickZeroLevelButton = QuickZeroLevelButton;

                // Wire up additional controls and initialize
                WireUpAdditionalControls();
                controller.InitializeControls();
                SetupEnhancedUI();

                isInitialized = true;
                LogEvent?.Invoke(this, "Trigger control panel initialized with settings manager");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error initializing Trigger control panel: {ex.Message}");
            }
        }


    }
}