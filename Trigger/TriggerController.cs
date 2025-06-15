using DS1000Z_E_USB_Control.Controls;
using Rigol_DS1000Z_E_Control;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Trigger
{
    /// <summary>
    /// Enhanced trigger controller for managing trigger settings with full UI support
    /// FIXED: Added missing methods - ForceTrigger, SetEdgeLevel, constructor overloads
    /// </summary>
    public class TriggerController
    {
        private readonly RigolDS1000ZE oscilloscope;
        private readonly OscilloscopeSettingsManager settingsManager;
        private readonly TriggerSettings settings;
        private bool isUpdating = false;

        public event EventHandler<string> LogEvent;
        public event EventHandler SettingsChanged;

        #region UI Control References
        // Main UI controls
        public ComboBox TriggerModeComboBox { get; set; }
        public ComboBox TriggerSweepComboBox { get; set; }
        public ComboBox EdgeSourceComboBox { get; set; }
        public ComboBox EdgeSlopeComboBox { get; set; }
        public ComboBox TriggerCouplingComboBox { get; set; }
        public CheckBox NoiseRejectCheckBox { get; set; }
        public TextBox HoldoffTextBox { get; set; }
        public Slider TriggerLevelSlider { get; set; }
        public TextBlock LevelValueText { get; set; }
        public TextBlock CurrentTriggerSettingsText { get; set; }
        public TextBlock TriggerStatusText { get; set; }

        // Enhanced UI controls
        public TextBlock MaxLevelDisplay { get; set; }
        public TextBlock MinLevelDisplay { get; set; }
        public TextBlock LevelRangeText { get; set; }
        public TextBlock HoldoffDisplayText { get; set; }
        public Button ForceTriggerButton { get; set; }
        public Button QuickZeroLevelButton { get; set; }
        public GraticuleArrowControl TriggerLevelArrows { get; set; }

        // Alternative UI control references for compatibility
        public ComboBox SourceComboBox { get; set; }
        public ComboBox SlopeComboBox { get; set; }
        public ComboBox ModeComboBox { get; set; }
        public ComboBox CouplingComboBox { get; set; }
        public TextBlock CurrentSettingsTextBlock { get; set; }
        #endregion

        #region Constructors

        /// <summary>
        /// ADDED: Constructor with both oscilloscope and settings manager (fixes CS1501 compilation error)
        /// </summary>
        public TriggerController(RigolDS1000ZE oscilloscope, OscilloscopeSettingsManager settingsManager)
        {
            this.oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));
            this.settingsManager = settingsManager; // Can be null
            this.settings = new TriggerSettings();

            // Subscribe to settings manager events if available
            if (this.settingsManager != null)
            {
                this.settingsManager.LogEvent += (sender, message) => Log($"SettingsManager: {message}");
            }
        }

        /// <summary>
        /// Constructor with oscilloscope only (backward compatibility)
        /// </summary>
        public TriggerController(RigolDS1000ZE oscilloscope)
            : this(oscilloscope, null)
        {
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Handle trigger level changes from slider or other sources
        /// </summary>
        public void HandleTriggerLevelChanged(double level)
        {
            if (!oscilloscope.IsConnected) return;

            string command = $":TRIGger:EDGe:LEVel {level.ToString(CultureInfo.InvariantCulture)}";

            if (oscilloscope.SendCommand(command))
            {
                settings.EdgeLevel = level;
                Log($"Trigger level set to {level:F3}V");
                UpdateCurrentSettingsDisplay();
                UpdateSettingsManagerIfAvailable();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger level");
                UpdateSliderFromSettings();
            }
        }


        /*Fix 2: TriggerController.cs(CS1061 error)
          Add this method anywhere in the TriggerController class: */
        /// <summary>
        /// Set trigger settings (sends commands to oscilloscope)
        /// </summary>
        public void SetSettings(TriggerSettings newSettings)
        {
            if (newSettings == null) return;

            try
            {
                Log("Applying trigger settings to oscilloscope...");

                SetMode(newSettings.Mode);
                SetSweep(newSettings.Sweep);
                SetSource(newSettings.EdgeSource);
                SetSlope(newSettings.EdgeSlope);
                SetEdgeLevel(newSettings.EdgeLevel);
                SetCoupling(newSettings.Coupling);
                SetHoldoff(newSettings.Holdoff);
                SetNoiseReject(newSettings.NoiseReject);

                Log("Trigger settings applied successfully");
            }
            catch (Exception ex)
            {
                Log($"Error applying trigger settings: {ex.Message}");
            }
        }


        /// <summary>
        /// ADDED: Missing ForceTrigger method - CS1061 fix
        /// </summary>
        public void ForceTrigger()
        {
            try
            {
                Log("Executing force trigger...");

                if (oscilloscope.SendCommand(":TFORce"))
                {
                    Log("Force trigger executed successfully");
                }
                else
                {
                    Log("Failed to execute force trigger command");
                }
            }
            catch (Exception ex)
            {
                Log($"Error executing force trigger: {ex.Message}");
            }
        }

        /// <summary>
        /// ADDED: Missing SetEdgeLevel method - CS1061 fix
        /// </summary>
        public void SetEdgeLevel(double level)
        {
            try
            {
                string command = $":TRIGger:EDGe:LEVel {level.ToString(CultureInfo.InvariantCulture)}";

                if (oscilloscope.SendCommand(command))
                {
                    settings.EdgeLevel = level;
                    Log($"Trigger edge level set to {level:F3}V");

                    // Update UI if available
                    UpdateUIFromSettings();
                    UpdateSettingsManagerIfAvailable();
                    SettingsChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Log($"Failed to set trigger edge level to {level:F3}V");
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting trigger edge level: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize UI controls and set up event handlers
        /// </summary>
        public void InitializeControls()
        {
            PopulateTriggerModeOptions();
            PopulateTriggerSweepOptions();
            PopulateEdgeSourceOptions();
            PopulateEdgeSlopeOptions();
            PopulateTriggerCouplingOptions();
            UpdateUIFromSettings();
            EnableEventHandlers();
        }

        /// <summary>
        /// Query and update current settings from oscilloscope
        /// </summary>
        public bool QueryAndUpdateSettings()
        {
            if (!oscilloscope.IsConnected) return false;

            try
            {
                Log("Reading trigger settings from oscilloscope...");

                // Read trigger mode
                string mode = oscilloscope.SendQuery(":TRIGger:MODE?");
                if (!string.IsNullOrEmpty(mode))
                {
                    settings.Mode = mode.Trim();
                }

                // Read trigger sweep
                string sweep = oscilloscope.SendQuery(":TRIGger:SWEep?");
                if (!string.IsNullOrEmpty(sweep))
                {
                    settings.Sweep = sweep.Trim();
                }

                // Read trigger coupling
                string coupling = oscilloscope.SendQuery(":TRIGger:COUPling?");
                if (!string.IsNullOrEmpty(coupling))
                {
                    settings.Coupling = coupling.Trim();
                }

                // Read edge source
                string source = oscilloscope.SendQuery(":TRIGger:EDGe:SOURce?");
                if (!string.IsNullOrEmpty(source))
                {
                    settings.EdgeSource = source.Trim();
                }

                // Read edge slope
                string slope = oscilloscope.SendQuery(":TRIGger:EDGe:SLOPe?");
                if (!string.IsNullOrEmpty(slope))
                {
                    settings.EdgeSlope = slope.Trim();
                }

                // Read edge level
                string level = oscilloscope.SendQuery(":TRIGger:EDGe:LEVel?");
                if (!string.IsNullOrEmpty(level) && double.TryParse(level, NumberStyles.Float, CultureInfo.InvariantCulture, out double edgeLevel))
                {
                    settings.EdgeLevel = edgeLevel;
                }

                // Read holdoff
                string holdoff = oscilloscope.SendQuery(":TRIGger:HOLDoff?");
                if (!string.IsNullOrEmpty(holdoff) && double.TryParse(holdoff, NumberStyles.Float, CultureInfo.InvariantCulture, out double holdoffValue))
                {
                    settings.Holdoff = holdoffValue;
                }

                // Read noise reject
                string noiseReject = oscilloscope.SendQuery(":TRIGger:NREJect?");
                if (!string.IsNullOrEmpty(noiseReject))
                {
                    settings.NoiseReject = noiseReject.Trim().ToUpper() == "ON" || noiseReject.Trim() == "1";
                }

                UpdateUIFromSettings();
                UpdateSettingsManagerIfAvailable();
                Log("Trigger settings updated from oscilloscope");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error querying trigger settings: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Settings Management

        /// <summary>
        /// Get current trigger settings
        /// </summary>
        public TriggerSettings GetSettings()
        {
            return settings.Clone();
        }

        /// <summary>
        /// Update settings from provided settings object
        /// </summary>
        public void UpdateFromSettings(TriggerSettings newSettings)
        {
            if (newSettings == null) return;

            // Update internal settings
            settings.Mode = newSettings.Mode;
            settings.Sweep = newSettings.Sweep;
            settings.Coupling = newSettings.Coupling;
            settings.EdgeSource = newSettings.EdgeSource;
            settings.EdgeSlope = newSettings.EdgeSlope;
            settings.EdgeLevel = newSettings.EdgeLevel;
            settings.Holdoff = newSettings.Holdoff;
            settings.NoiseReject = newSettings.NoiseReject;

            // Update UI
            UpdateUIFromSettings();
        }

        public bool SetMode(string mode)
        {
            string command = $":TRIGger:MODE {mode}";

            if (oscilloscope.SendCommand(command))
            {
                settings.Mode = mode;
                Log($"Trigger mode set to {mode}");
                UpdateCurrentSettingsDisplay();
                UpdateSettingsManagerIfAvailable();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set trigger mode");
                return false;
            }
        }

        public bool SetSweep(string sweep)
        {
            string command = $":TRIGger:SWEep {sweep}";

            if (oscilloscope.SendCommand(command))
            {
                settings.Sweep = sweep;
                Log($"Trigger sweep set to {sweep}");
                UpdateCurrentSettingsDisplay();
                UpdateSettingsManagerIfAvailable();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set trigger sweep");
                return false;
            }
        }

        public bool SetSource(string source)
        {
            string command = $":TRIGger:EDGe:SOURce {source}";

            if (oscilloscope.SendCommand(command))
            {
                settings.EdgeSource = source;
                Log($"Trigger source set to {source}");
                UpdateCurrentSettingsDisplay();
                UpdateSettingsManagerIfAvailable();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set trigger source");
                return false;
            }
        }

        public bool SetSlope(string slope)
        {
            string command = $":TRIGger:EDGe:SLOPe {slope}";

            if (oscilloscope.SendCommand(command))
            {
                settings.EdgeSlope = slope;
                Log($"Trigger slope set to {slope}");
                UpdateCurrentSettingsDisplay();
                UpdateSettingsManagerIfAvailable();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set trigger slope");
                return false;
            }
        }

        public bool SetCoupling(string coupling)
        {
            string command = $":TRIGger:COUPling {coupling}";

            if (oscilloscope.SendCommand(command))
            {
                settings.Coupling = coupling;
                Log($"Trigger coupling set to {coupling}");
                UpdateCurrentSettingsDisplay();
                UpdateSettingsManagerIfAvailable();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set trigger coupling");
                return false;
            }
        }

        public bool SetHoldoff(double holdoff)
        {
            string command = $":TRIGger:HOLDoff {holdoff.ToString(CultureInfo.InvariantCulture)}";

            if (oscilloscope.SendCommand(command))
            {
                settings.Holdoff = holdoff;
                Log($"Trigger holdoff set to {holdoff:F6}s");
                UpdateCurrentSettingsDisplay();
                UpdateSettingsManagerIfAvailable();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set trigger holdoff");
                return false;
            }
        }

        public bool SetNoiseReject(bool enabled)
        {
            string command = $":TRIGger:NREJect {(enabled ? "ON" : "OFF")}";

            if (oscilloscope.SendCommand(command))
            {
                settings.NoiseReject = enabled;
                Log($"Trigger noise reject {(enabled ? "enabled" : "disabled")}");
                UpdateCurrentSettingsDisplay();
                UpdateSettingsManagerIfAvailable();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set trigger noise reject");
                return false;
            }
        }
        #endregion

        #region UI Population Methods

        private void PopulateTriggerModeOptions()
        {
            // Use either ModeComboBox or TriggerModeComboBox
            var comboBox = ModeComboBox ?? TriggerModeComboBox;
            if (comboBox == null) return;

            comboBox.Items.Clear();
            var modeOptions = new[] { "EDGE", "PULS", "RUNT", "WIND", "NEDG", "SLOP", "VID", "PATT", "DEL", "TIM", "DUR", "SHOL", "RS232", "IIC", "SPI" };

            foreach (var mode in modeOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = mode,
                    Tag = mode
                };
                comboBox.Items.Add(item);

                if (mode == settings.Mode)
                {
                    comboBox.SelectedItem = item;
                }
            }
        }

        private void PopulateTriggerSweepOptions()
        {
            if (TriggerSweepComboBox == null) return;

            TriggerSweepComboBox.Items.Clear();
            var sweepOptions = new[] { "AUTO", "NORM", "SING" };

            foreach (var sweep in sweepOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = sweep,
                    Tag = sweep
                };
                TriggerSweepComboBox.Items.Add(item);

                if (sweep == settings.Sweep)
                {
                    TriggerSweepComboBox.SelectedItem = item;
                }
            }
        }

        private void PopulateEdgeSourceOptions()
        {
            // Use either SourceComboBox or EdgeSourceComboBox
            var comboBox = SourceComboBox ?? EdgeSourceComboBox;
            if (comboBox == null) return;

            comboBox.Items.Clear();
            var sourceOptions = new[] { "CHAN1", "CHAN2", "EXT", "EXT5", "AC" };

            foreach (var source in sourceOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = source,
                    Tag = source
                };
                comboBox.Items.Add(item);

                if (source == settings.EdgeSource)
                {
                    comboBox.SelectedItem = item;
                }
            }
        }

        private void PopulateEdgeSlopeOptions()
        {
            // Use either SlopeComboBox or EdgeSlopeComboBox
            var comboBox = SlopeComboBox ?? EdgeSlopeComboBox;
            if (comboBox == null) return;

            comboBox.Items.Clear();
            var slopeOptions = new[] { "POS", "NEG", "RFAL" };

            foreach (var slope in slopeOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = slope,
                    Tag = slope
                };
                comboBox.Items.Add(item);

                if (slope == settings.EdgeSlope)
                {
                    comboBox.SelectedItem = item;
                }
            }
        }

        private void PopulateTriggerCouplingOptions()
        {
            // Use either CouplingComboBox or TriggerCouplingComboBox
            var comboBox = CouplingComboBox ?? TriggerCouplingComboBox;
            if (comboBox == null) return;

            comboBox.Items.Clear();
            var couplingOptions = new[] { "DC", "AC", "HFR", "LFR" };

            foreach (var coupling in couplingOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = coupling,
                    Tag = coupling
                };
                comboBox.Items.Add(item);

                if (coupling == settings.Coupling)
                {
                    comboBox.SelectedItem = item;
                }
            }
        }
        #endregion

        #region UI Update Methods

        /// <summary>
        /// Update UI controls from current settings WITHOUT sending commands to oscilloscope
        /// </summary>
        public void UpdateUIFromSettings()
        {
            if (isUpdating) return;

            try
            {
                isUpdating = true;
                DisableEventHandlers();

                // Update combo boxes
                PopulateTriggerModeOptions();
                PopulateTriggerSweepOptions();
                PopulateEdgeSourceOptions();
                PopulateEdgeSlopeOptions();
                PopulateTriggerCouplingOptions();

                // Update checkbox
                if (NoiseRejectCheckBox != null)
                    NoiseRejectCheckBox.IsChecked = settings.NoiseReject;

                // Update slider
                UpdateSliderRange();
                if (TriggerLevelSlider != null)
                    TriggerLevelSlider.Value = settings.EdgeLevel;

                // Update text boxes
                if (HoldoffTextBox != null)
                    HoldoffTextBox.Text = settings.Holdoff.ToString("F6");

                UpdateSliderValueDisplay();
                UpdateCurrentSettingsDisplay();
                UpdateRangeDisplays();

                Log($"Trigger UI updated from settings: {settings}");
            }
            catch (Exception ex)
            {
                Log($"Error updating trigger UI: {ex.Message}");
            }
            finally
            {
                EnableEventHandlers();
                isUpdating = false;
            }
        }

        private void UpdateSliderRange()
        {
            if (TriggerLevelSlider == null) return;

            // Set reasonable trigger level range
            double minLevel = -5.0;
            double maxLevel = 5.0;

            isUpdating = true;
            TriggerLevelSlider.Minimum = minLevel;
            TriggerLevelSlider.Maximum = maxLevel;
            TriggerLevelSlider.TickFrequency = 0.5;
            isUpdating = false;
        }

        private void UpdateSliderValueDisplay()
        {
            if (LevelValueText != null && TriggerLevelSlider != null)
            {
                LevelValueText.Text = $"{TriggerLevelSlider.Value:F3}V";
            }
        }

        private void UpdateCurrentSettingsDisplay()
        {
            var textBlock = CurrentSettingsTextBlock ?? CurrentTriggerSettingsText;
            if (textBlock != null)
            {
                textBlock.Text = $"Trigger: {settings.Mode}, {settings.EdgeSource}, " +
                    $"{settings.EdgeSlope}, Level: {settings.EdgeLevel:F3}V, {settings.Sweep}";
            }
        }

        private void UpdateRangeDisplays()
        {
            if (MaxLevelDisplay != null)
                MaxLevelDisplay.Text = "5.000V";
            if (MinLevelDisplay != null)
                MinLevelDisplay.Text = "-5.000V";
            if (LevelRangeText != null)
                LevelRangeText.Text = "Range: ±5.000V";
        }

        private void UpdateSliderFromSettings()
        {
            if (TriggerLevelSlider != null && !isUpdating)
            {
                isUpdating = true;
                TriggerLevelSlider.Value = settings.EdgeLevel;
                UpdateSliderValueDisplay();
                isUpdating = false;
            }
        }
        #endregion

        #region Event Handlers

        private void OnTriggerModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var comboBox = sender as ComboBox;
            var selectedItem = comboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                SetMode(selectedItem.Tag.ToString());
            }
        }

        private void OnTriggerSweepChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = TriggerSweepComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                SetSweep(selectedItem.Tag.ToString());
            }
        }

        private void OnEdgeSourceChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var comboBox = sender as ComboBox;
            var selectedItem = comboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                SetSource(selectedItem.Tag.ToString());
            }
        }

        private void OnEdgeSlopeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var comboBox = sender as ComboBox;
            var selectedItem = comboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                SetSlope(selectedItem.Tag.ToString());
            }
        }

        private void OnTriggerCouplingChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var comboBox = sender as ComboBox;
            var selectedItem = comboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                SetCoupling(selectedItem.Tag.ToString());
            }
        }

        private void OnNoiseRejectChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (isUpdating) return;

            bool enabled = NoiseRejectCheckBox?.IsChecked ?? false;
            SetNoiseReject(enabled);
        }

        private void OnTriggerLevelSliderChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdating) return;

            HandleTriggerLevelChanged(e.NewValue);
            UpdateSliderValueDisplay();
        }

        /// <summary>
        /// Handle holdoff text changes
        /// </summary>
        private void OnHoldoffTextChanged(object sender, RoutedEventArgs e)
        {
            if (isUpdating) return;

            if (HoldoffTextBox != null && double.TryParse(HoldoffTextBox.Text, out double holdoff))
            {
                SetHoldoff(holdoff);
            }
        }

        private void OnForceTriggerClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            ForceTrigger();
        }

        private void OnQuickZeroLevelClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            SetEdgeLevel(0.0);
        }

        private void DisableEventHandlers()
        {
            // Disable mode combo box events
            var modeCombo = ModeComboBox ?? TriggerModeComboBox;
            if (modeCombo != null)
                modeCombo.SelectionChanged -= OnTriggerModeChanged;

            if (TriggerSweepComboBox != null)
                TriggerSweepComboBox.SelectionChanged -= OnTriggerSweepChanged;

            // Disable source combo box events
            var sourceCombo = SourceComboBox ?? EdgeSourceComboBox;
            if (sourceCombo != null)
                sourceCombo.SelectionChanged -= OnEdgeSourceChanged;

            // Disable slope combo box events
            var slopeCombo = SlopeComboBox ?? EdgeSlopeComboBox;
            if (slopeCombo != null)
                slopeCombo.SelectionChanged -= OnEdgeSlopeChanged;

            // Disable coupling combo box events
            var couplingCombo = CouplingComboBox ?? TriggerCouplingComboBox;
            if (couplingCombo != null)
                couplingCombo.SelectionChanged -= OnTriggerCouplingChanged;

            if (NoiseRejectCheckBox != null)
            {
                NoiseRejectCheckBox.Checked -= OnNoiseRejectChanged;
                NoiseRejectCheckBox.Unchecked -= OnNoiseRejectChanged;
            }
            if (TriggerLevelSlider != null)
                TriggerLevelSlider.ValueChanged -= OnTriggerLevelSliderChanged;
            if (HoldoffTextBox != null)
                HoldoffTextBox.LostFocus -= OnHoldoffTextChanged;
            if (ForceTriggerButton != null)
                ForceTriggerButton.Click -= OnForceTriggerClicked;
            if (QuickZeroLevelButton != null)
                QuickZeroLevelButton.Click -= OnQuickZeroLevelClicked;
        }

        private void EnableEventHandlers()
        {
            // Enable mode combo box events
            var modeCombo = ModeComboBox ?? TriggerModeComboBox;
            if (modeCombo != null)
                modeCombo.SelectionChanged += OnTriggerModeChanged;

            if (TriggerSweepComboBox != null)
                TriggerSweepComboBox.SelectionChanged += OnTriggerSweepChanged;

            // Enable source combo box events
            var sourceCombo = SourceComboBox ?? EdgeSourceComboBox;
            if (sourceCombo != null)
                sourceCombo.SelectionChanged += OnEdgeSourceChanged;

            // Enable slope combo box events
            var slopeCombo = SlopeComboBox ?? EdgeSlopeComboBox;
            if (slopeCombo != null)
                slopeCombo.SelectionChanged += OnEdgeSlopeChanged;

            // Enable coupling combo box events
            var couplingCombo = CouplingComboBox ?? TriggerCouplingComboBox;
            if (couplingCombo != null)
                couplingCombo.SelectionChanged += OnTriggerCouplingChanged;

            if (NoiseRejectCheckBox != null)
            {
                NoiseRejectCheckBox.Checked += OnNoiseRejectChanged;
                NoiseRejectCheckBox.Unchecked += OnNoiseRejectChanged;
            }
            if (TriggerLevelSlider != null)
                TriggerLevelSlider.ValueChanged += OnTriggerLevelSliderChanged;
            if (HoldoffTextBox != null)
                HoldoffTextBox.LostFocus += OnHoldoffTextChanged;
            if (ForceTriggerButton != null)
                ForceTriggerButton.Click += OnForceTriggerClicked;
            if (QuickZeroLevelButton != null)
                QuickZeroLevelButton.Click += OnQuickZeroLevelClicked;
        }
        #endregion

        #region Helper Methods

        /// <summary>
        /// Update settings manager if available
        /// </summary>
        private void UpdateSettingsManagerIfAvailable()
        {
            try
            {
                if (settingsManager != null)
                {
                    settingsManager.TriggerSettings = settings.Clone();
                }
            }
            catch (Exception ex)
            {
                Log($"Error updating settings manager: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            DisableEventHandlers();
        }
        #endregion
    }
}