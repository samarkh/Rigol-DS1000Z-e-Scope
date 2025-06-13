using System;
using System.Globalization;
using System.Windows.Controls;
using Rigol_DS1000Z_E_Control;

namespace DS1000Z_E_USB_Control.Trigger
{
    /// <summary>
    /// Enhanced trigger controller for managing trigger settings with full UI support
    /// </summary>
    public class TriggerController
    {
        private readonly RigolDS1000ZE oscilloscope;
        private readonly TriggerSettings settings;
        private bool isUpdating = false;

        public event EventHandler<string> LogEvent;
        public event EventHandler SettingsChanged;
        private OscilloscopeSettingsManager settingsManager;

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
        #endregion

        public TriggerController(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope;
            this.settingsManager = settingsManager;
            this.settings = new TriggerSettings();
        }

        /// <summary>
        /// Get enhanced trigger range using channel settings
        /// </summary>
        private (double min, double max) GetEnhancedTriggerRange()
        {
            if (settingsManager?.Channel1Settings != null && settingsManager?.Channel2Settings != null)
            {
                return settings.GetTriggerLevelRange(settingsManager.Channel1Settings, settingsManager.Channel2Settings);
            }

            // Fallback to default range
            return settings.GetTriggerLevelRange();
        }



        #region UI Initialization and Management

        /// <summary>
        /// Initialize UI controls and set up event handlers
        /// </summary>
        public void InitializeControls()
        {
            if (TriggerModeComboBox != null)
            {
                PopulateTriggerModeOptions();
                TriggerModeComboBox.SelectionChanged += OnTriggerModeChanged;
            }

            if (TriggerSweepComboBox != null)
            {
                TriggerSweepComboBox.SelectionChanged += OnTriggerSweepChanged;
            }

            if (EdgeSourceComboBox != null)
            {
                EdgeSourceComboBox.SelectionChanged += OnEdgeSourceChanged;
            }

            if (EdgeSlopeComboBox != null)
            {
                EdgeSlopeComboBox.SelectionChanged += OnEdgeSlopeChanged;
            }

            if (TriggerCouplingComboBox != null)
            {
                TriggerCouplingComboBox.SelectionChanged += OnTriggerCouplingChanged;
            }

            if (NoiseRejectCheckBox != null)
            {
                NoiseRejectCheckBox.Checked += OnNoiseRejectChanged;
                NoiseRejectCheckBox.Unchecked += OnNoiseRejectChanged;
            }

            if (TriggerLevelSlider != null)
            {
                TriggerLevelSlider.ValueChanged += OnTriggerLevelSliderChanged;
                UpdateSliderRange();
            }

            if (HoldoffTextBox != null)
            {
                HoldoffTextBox.LostFocus += OnHoldoffTextChanged;
            }

            if (ForceTriggerButton != null)
            {
                ForceTriggerButton.Click += OnForceTriggerClicked;
            }

            if (QuickZeroLevelButton != null)
            {
                QuickZeroLevelButton.Click += OnQuickZeroLevelClicked;
            }
        }

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

                // Update trigger mode
                if (TriggerModeComboBox != null)
                {
                    foreach (ComboBoxItem item in TriggerModeComboBox.Items)
                    {
                        if (item.Tag.ToString().ToUpper() == settings.Mode.ToUpper())
                        {
                            TriggerModeComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update trigger sweep
                if (TriggerSweepComboBox != null)
                {
                    foreach (ComboBoxItem item in TriggerSweepComboBox.Items)
                    {
                        if (item.Tag.ToString().ToUpper() == settings.Sweep.ToUpper())
                        {
                            TriggerSweepComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update edge source
                if (EdgeSourceComboBox != null)
                {
                    foreach (ComboBoxItem item in EdgeSourceComboBox.Items)
                    {
                        if (item.Tag.ToString().ToUpper() == settings.EdgeSource.ToUpper())
                        {
                            EdgeSourceComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update edge slope
                if (EdgeSlopeComboBox != null)
                {
                    foreach (ComboBoxItem item in EdgeSlopeComboBox.Items)
                    {
                        if (item.Tag.ToString().ToUpper() == settings.EdgeSlope.ToUpper())
                        {
                            EdgeSlopeComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update trigger coupling
                if (TriggerCouplingComboBox != null)
                {
                    foreach (ComboBoxItem item in TriggerCouplingComboBox.Items)
                    {
                        if (item.Tag.ToString().ToUpper() == settings.Coupling.ToUpper())
                        {
                            TriggerCouplingComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update noise reject
                if (NoiseRejectCheckBox != null)
                {
                    NoiseRejectCheckBox.IsChecked = settings.NoiseReject;
                }

                // Update holdoff
                if (HoldoffTextBox != null)
                {
                    HoldoffTextBox.Text = settings.Holdoff.ToString("E3");
                }

                // Update trigger level slider
                UpdateSliderRange();
                if (TriggerLevelSlider != null)
                {
                    TriggerLevelSlider.Value = settings.EdgeLevel;
                    UpdateSliderValueDisplay();
                }

                // Update current settings display
                UpdateCurrentSettingsDisplay();

                Log($"Updated Trigger UI from settings: {settings}");
            }
            catch (Exception ex)
            {
                Log($"Error updating Trigger UI: {ex.Message}");
            }
            finally
            {
                EnableEventHandlers();
                isUpdating = false;
            }
        }

        /// <summary>
        /// Update settings object from provided settings and then update UI
        /// </summary>
        public void UpdateFromSettings(TriggerSettings newSettings)
        {
            if (newSettings == null) return;

            // Update internal settings object
            settings.Mode = newSettings.Mode;
            settings.Coupling = newSettings.Coupling;
            settings.Sweep = newSettings.Sweep;
            settings.EdgeSource = newSettings.EdgeSource;
            settings.EdgeSlope = newSettings.EdgeSlope;
            settings.EdgeLevel = newSettings.EdgeLevel;
            settings.Holdoff = newSettings.Holdoff;
            settings.NoiseReject = newSettings.NoiseReject;
            settings.Status = newSettings.Status;
            settings.Position = newSettings.Position;

            // Update UI to reflect these settings
            UpdateUIFromSettings();
        }

        #endregion

        #region Slider and Level Management

        /// <summary>
        /// Handle trigger level changes from slider or other sources
        /// </summary>
        public void HandleTriggerLevelChanged(double level)
        {
            if (!oscilloscope.IsConnected || isUpdating) return;

            SetEdgeLevel(level);
        }

        /// <summary>
        /// Update the slider range based on trigger source
        /// </summary>
        public void UpdateSliderRange()
        {
            if (TriggerLevelSlider == null) return;

            var (minLevel, maxLevel) = GetEnhancedTriggerRange();

            isUpdating = true;
            TriggerLevelSlider.Minimum = minLevel;
            TriggerLevelSlider.Maximum = maxLevel;

            // Clamp current value to new range
            if (TriggerLevelSlider.Value < minLevel)
                TriggerLevelSlider.Value = minLevel;
            else if (TriggerLevelSlider.Value > maxLevel)
                TriggerLevelSlider.Value = maxLevel;

            isUpdating = false;

            Log($"Trigger level slider range updated: {minLevel:F3}V to {maxLevel:F3}V");
        }

        /// <summary>
        /// Get enhanced trigger range using channel settings if available
        /// </summary>
        private (double min, double max) GetEnhancedTriggerRange()
        {
            // Try to get channel settings from the main window or settings manager
            // You'll need to add a reference to access the channel settings
            // For now, use the default method
            return settings.GetTriggerLevelRange();
        }


        /// <summary>
        /// Update the slider value display text
        /// </summary>
        private void UpdateSliderValueDisplay()
        {
            if (LevelValueText != null && TriggerLevelSlider != null)
            {
                LevelValueText.Text = $"{TriggerLevelSlider.Value:F3} V";
            }
        }

        /// <summary>
        /// Update slider value from current settings
        /// </summary>
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

        #region Settings Display Updates

        /// <summary>
        /// Update the current settings display text
        /// </summary>
        private void UpdateCurrentSettingsDisplay()
        {
            if (CurrentTriggerSettingsText != null)
            {
                CurrentTriggerSettingsText.Text = $"Current: Mode={settings.Mode}, Source={settings.EdgeSource}, " +
                    $"Slope={settings.EdgeSlope}, Level={settings.EdgeLevelDisplay}, " +
                    $"Sweep={settings.Sweep}, Coupling={settings.Coupling}";
            }

            if (TriggerStatusText != null)
            {
                TriggerStatusText.Text = $"Status: {settings.Status}";
            }
        }

        /// <summary>
        /// Populate trigger mode options
        /// </summary>
        private void PopulateTriggerModeOptions()
        {
            if (TriggerModeComboBox == null) return;

            var modeOptions = TriggerSettings.GetModeOptions();

            TriggerModeComboBox.Items.Clear();
            foreach (var option in modeOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = option.display,
                    Tag = option.value
                };
                TriggerModeComboBox.Items.Add(item);

                // Select Edge as default
                if (option.value == "EDGe")
                {
                    TriggerModeComboBox.SelectedItem = item;
                }
            }
        }

        #endregion

        #region Original Trigger Methods (Enhanced)

        /// <summary>
        /// Set the trigger mode
        /// </summary>
        public bool SetTriggerMode(string mode)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:MODE {mode}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Mode = mode;
                Log($"Trigger mode set to {mode}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger mode");
            }

            return success;
        }

        /// <summary>
        /// Set the trigger sweep mode
        /// </summary>
        public bool SetTriggerSweep(string sweep)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:SWEep {sweep}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Sweep = sweep;
                Log($"Trigger sweep set to {sweep}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger sweep");
            }

            return success;
        }

        /// <summary>
        /// Set the edge trigger source
        /// </summary>
        public bool SetEdgeSource(string source)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:EDGe:SOURce {source}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.EdgeSource = source;
                Log($"Edge trigger source set to {source}");
                UpdateSliderRange();
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set edge trigger source");
            }

            return success;
        }

        /// <summary>
        /// Set the edge trigger slope
        /// </summary>
        public bool SetEdgeSlope(string slope)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:EDGe:SLOPe {slope}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.EdgeSlope = slope;
                Log($"Edge trigger slope set to {slope}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set edge trigger slope");
            }

            return success;
        }

        /// <summary>
        /// Set the edge trigger level
        /// </summary>
        public bool SetEdgeLevel(double level)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:EDGe:LEVel {level.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.EdgeLevel = level;
                Log($"Edge trigger level set to {settings.EdgeLevelDisplay}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set edge trigger level");
            }

            return success;
        }

        /// <summary>
        /// Set the trigger coupling
        /// </summary>
        public bool SetTriggerCoupling(string coupling)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:COUPling {coupling}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Coupling = coupling;
                Log($"Trigger coupling set to {coupling}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger coupling");
            }

            return success;
        }

        /// <summary>
        /// Set the trigger holdoff time
        /// </summary>
        public bool SetHoldoff(double holdoff)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:HOLDoff {holdoff.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Holdoff = holdoff;
                Log($"Trigger holdoff set to {settings.HoldoffDisplay}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger holdoff");
            }

            return success;
        }

        /// <summary>
        /// Enable or disable noise reject
        /// </summary>
        public bool SetNoiseReject(bool enable)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":TRIGger:NREJect {(enable ? "ON" : "OFF")}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.NoiseReject = enable;
                Log($"Trigger noise reject {(enable ? "enabled" : "disabled")}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger noise reject");
            }

            return success;
        }

        /// <summary>
        /// Force a trigger
        /// </summary>
        public bool ForceTrigger()
        {
            if (!oscilloscope.IsConnected) return false;

            bool success = oscilloscope.SendCommand(":TFORce");

            if (success)
            {
                Log("Trigger forced");
            }
            else
            {
                Log("Failed to force trigger");
            }

            return success;
        }

        #endregion

        #region Event Handlers

        private void OnTriggerModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = TriggerModeComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                SetTriggerMode(selectedItem.Tag.ToString());
            }
        }

        private void OnTriggerSweepChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = TriggerSweepComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                SetTriggerSweep(selectedItem.Tag.ToString());
            }
        }

        private void OnEdgeSourceChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = EdgeSourceComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                SetEdgeSource(selectedItem.Tag.ToString());
            }
        }

        private void OnEdgeSlopeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = EdgeSlopeComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                SetEdgeSlope(selectedItem.Tag.ToString());
            }
        }

        private void OnTriggerCouplingChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = TriggerCouplingComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                SetTriggerCoupling(selectedItem.Tag.ToString());
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

            double newValue = e.NewValue;

            // Update the value display immediately
            if (LevelValueText != null)
            {
                LevelValueText.Text = $"{newValue:F3} V";
            }

            // Send the command to the oscilloscope
            HandleTriggerLevelChanged(newValue);
        }

        private void OnHoldoffTextChanged(object sender, System.Windows.RoutedEventArgs e)
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
            SetEdgeLevel(0);
        }

        #endregion

        #region Event Handler Management

        private void DisableEventHandlers()
        {
            if (TriggerModeComboBox != null)
                TriggerModeComboBox.SelectionChanged -= OnTriggerModeChanged;
            if (TriggerSweepComboBox != null)
                TriggerSweepComboBox.SelectionChanged -= OnTriggerSweepChanged;
            if (EdgeSourceComboBox != null)
                EdgeSourceComboBox.SelectionChanged -= OnEdgeSourceChanged;
            if (EdgeSlopeComboBox != null)
                EdgeSlopeComboBox.SelectionChanged -= OnEdgeSlopeChanged;
            if (TriggerCouplingComboBox != null)
                TriggerCouplingComboBox.SelectionChanged -= OnTriggerCouplingChanged;
            if (NoiseRejectCheckBox != null)
            {
                NoiseRejectCheckBox.Checked -= OnNoiseRejectChanged;
                NoiseRejectCheckBox.Unchecked -= OnNoiseRejectChanged;
            }
            if (TriggerLevelSlider != null)
                TriggerLevelSlider.ValueChanged -= OnTriggerLevelSliderChanged;
            if (HoldoffTextBox != null)
                HoldoffTextBox.LostFocus -= OnHoldoffTextChanged;
        }

        private void EnableEventHandlers()
        {
            if (TriggerModeComboBox != null)
                TriggerModeComboBox.SelectionChanged += OnTriggerModeChanged;
            if (TriggerSweepComboBox != null)
                TriggerSweepComboBox.SelectionChanged += OnTriggerSweepChanged;
            if (EdgeSourceComboBox != null)
                EdgeSourceComboBox.SelectionChanged += OnEdgeSourceChanged;
            if (EdgeSlopeComboBox != null)
                EdgeSlopeComboBox.SelectionChanged += OnEdgeSlopeChanged;
            if (TriggerCouplingComboBox != null)
                TriggerCouplingComboBox.SelectionChanged += OnTriggerCouplingChanged;
            if (NoiseRejectCheckBox != null)
            {
                NoiseRejectCheckBox.Checked += OnNoiseRejectChanged;
                NoiseRejectCheckBox.Unchecked += OnNoiseRejectChanged;
            }
            if (TriggerLevelSlider != null)
                TriggerLevelSlider.ValueChanged += OnTriggerLevelSliderChanged;
            if (HoldoffTextBox != null)
                HoldoffTextBox.LostFocus += OnHoldoffTextChanged;
        }

        #endregion

        #region Query and Update Methods

        /// <summary>
        /// Query and update all trigger settings from oscilloscope
        /// </summary>
        public bool QueryAndUpdateSettings()
        {
            if (!oscilloscope.IsConnected) return false;

            try
            {
                // Read trigger mode
                string mode = oscilloscope.SendQuery(":TRIGger:MODE?");
                if (!string.IsNullOrEmpty(mode))
                {
                    settings.Mode = mode.Trim();
                }

                // Read trigger coupling
                string coupling = oscilloscope.SendQuery(":TRIGger:COUPling?");
                if (!string.IsNullOrEmpty(coupling))
                {
                    settings.Coupling = coupling.Trim();
                }

                // Read trigger sweep mode
                string sweep = oscilloscope.SendQuery(":TRIGger:SWEep?");
                if (!string.IsNullOrEmpty(sweep))
                {
                    settings.Sweep = sweep.Trim();
                }

                // Read trigger status
                string status = oscilloscope.SendQuery(":TRIGger:STATus?");
                if (!string.IsNullOrEmpty(status))
                {
                    settings.Status = status.Trim();
                }

                // For edge trigger mode, read edge-specific settings
                if (settings.Mode.ToUpper() == "EDGE")
                {
                    // Read edge trigger source
                    string edgeSource = oscilloscope.SendQuery(":TRIGger:EDGe:SOURce?");
                    if (!string.IsNullOrEmpty(edgeSource))
                    {
                        settings.EdgeSource = edgeSource.Trim();
                    }

                    // Read edge trigger slope
                    string edgeSlope = oscilloscope.SendQuery(":TRIGger:EDGe:SLOPe?");
                    if (!string.IsNullOrEmpty(edgeSlope))
                    {
                        settings.EdgeSlope = edgeSlope.Trim();
                    }

                    // Read edge trigger level
                    string edgeLevel = oscilloscope.SendQuery(":TRIGger:EDGe:LEVel?");
                    if (!string.IsNullOrEmpty(edgeLevel) &&
                        double.TryParse(edgeLevel, NumberStyles.Float, CultureInfo.InvariantCulture, out double level))
                    {
                        settings.EdgeLevel = level;
                    }
                }

                // Read holdoff
                string holdoff = oscilloscope.SendQuery(":TRIGger:HOLDoff?");
                if (!string.IsNullOrEmpty(holdoff) &&
                    double.TryParse(holdoff, NumberStyles.Float, CultureInfo.InvariantCulture, out double holdoffVal))
                {
                    settings.Holdoff = holdoffVal;
                }

                // Read noise reject
                string noiseReject = oscilloscope.SendQuery(":TRIGger:NREJect?");
                if (!string.IsNullOrEmpty(noiseReject))
                {
                    settings.NoiseReject = noiseReject.Trim() == "1" || noiseReject.Trim().ToUpper() == "ON";
                }

                // Update UI with new settings
                UpdateUIFromSettings();

                Log($"Trigger settings updated: {settings}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error querying Trigger settings: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current trigger settings
        /// </summary>
        public TriggerSettings GetSettings()
        {
            return settings.Clone();
        }

        /// <summary>
        /// Set trigger settings
        /// </summary>
        public void SetSettings(TriggerSettings newSettings)
        {
            if (newSettings == null) return;

            SetTriggerMode(newSettings.Mode);
            SetTriggerCoupling(newSettings.Coupling);
            SetTriggerSweep(newSettings.Sweep);

            if (newSettings.Mode.ToUpper() == "EDGE")
            {
                SetEdgeSource(newSettings.EdgeSource);
                SetEdgeSlope(newSettings.EdgeSlope);
                SetEdgeLevel(newSettings.EdgeLevel);
            }

            SetHoldoff(newSettings.Holdoff);
            SetNoiseReject(newSettings.NoiseReject);
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            DisableEventHandlers();
        }

        #endregion

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}