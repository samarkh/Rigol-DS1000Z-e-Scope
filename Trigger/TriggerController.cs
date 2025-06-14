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
        #endregion

        #region Constructors

        /// <summary>
        /// Constructor with both oscilloscope and settings manager (fixes compilation error)
        /// </summary>
        public TriggerController(RigolDS1000ZE oscilloscope, OscilloscopeSettingsManager settingsManager)
        {
            this.oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.settings = new TriggerSettings();
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
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set trigger level");
                UpdateSliderFromSettings();
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

                // Read edge trigger settings if in edge mode
                if (settings.Mode.ToUpper() == "EDGE")
                {
                    string edgeSource = oscilloscope.SendQuery(":TRIGger:EDGe:SOURce?");
                    if (!string.IsNullOrEmpty(edgeSource))
                    {
                        settings.EdgeSource = edgeSource.Trim();
                    }

                    string edgeSlope = oscilloscope.SendQuery(":TRIGger:EDGe:SLOPe?");
                    if (!string.IsNullOrEmpty(edgeSlope))
                    {
                        settings.EdgeSlope = edgeSlope.Trim();
                    }

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
                    settings.NoiseReject = noiseReject.Trim().ToUpper() == "ON";
                }

                UpdateUIFromSettings();
                Log("Trigger settings read successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error reading trigger settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update settings from provided settings object
        /// </summary>
        public void UpdateFromSettings(TriggerSettings newSettings)
        {
            if (newSettings == null) return;

            // Update internal settings object
            settings.Mode = newSettings.Mode;
            settings.Sweep = newSettings.Sweep;
            settings.Coupling = newSettings.Coupling;
            settings.EdgeSource = newSettings.EdgeSource;
            settings.EdgeSlope = newSettings.EdgeSlope;
            settings.EdgeLevel = newSettings.EdgeLevel;
            settings.Holdoff = newSettings.Holdoff;
            settings.NoiseReject = newSettings.NoiseReject;

            // Update UI to reflect these settings
            UpdateUIFromSettings();
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
        /// Set trigger settings (sends commands to oscilloscope)
        /// </summary>
        public void SetSettings(TriggerSettings newSettings)
        {
            if (newSettings == null) return;

            SetTriggerMode(newSettings.Mode);
            SetTriggerSweep(newSettings.Sweep);
            SetTriggerCoupling(newSettings.Coupling);

            if (newSettings.Mode.ToUpper() == "EDGE")
            {
                SetEdgeSource(newSettings.EdgeSource);
                SetEdgeSlope(newSettings.EdgeSlope);
                SetTriggerLevel(newSettings.EdgeLevel);
            }

            SetHoldoff(newSettings.Holdoff);
            SetNoiseReject(newSettings.NoiseReject);
        }

        public bool SetTriggerMode(string mode)
        {
            string command = $":TRIGger:MODE {mode}";

            if (oscilloscope.SendCommand(command))
            {
                settings.Mode = mode;
                Log($"Trigger mode set to {mode}");
                UpdateSliderRange(); // Range may change based on mode
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set trigger mode");
                return false;
            }
        }

        public bool SetTriggerSweep(string sweep)
        {
            string command = $":TRIGger:SWEep {sweep}";

            if (oscilloscope.SendCommand(command))
            {
                settings.Sweep = sweep;
                Log($"Trigger sweep set to {sweep}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set trigger sweep");
                return false;
            }
        }

        public bool SetTriggerCoupling(string coupling)
        {
            string command = $":TRIGger:COUPling {coupling}";

            if (oscilloscope.SendCommand(command))
            {
                settings.Coupling = coupling;
                Log($"Trigger coupling set to {coupling}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set trigger coupling");
                return false;
            }
        }

        public bool SetEdgeSource(string source)
        {
            string command = $":TRIGger:EDGe:SOURce {source}";

            if (oscilloscope.SendCommand(command))
            {
                settings.EdgeSource = source;
                Log($"Edge trigger source set to {source}");
                UpdateSliderRange(); // Range may change based on source
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set edge trigger source");
                return false;
            }
        }

        public bool SetEdgeSlope(string slope)
        {
            string command = $":TRIGger:EDGe:SLOPe {slope}";

            if (oscilloscope.SendCommand(command))
            {
                settings.EdgeSlope = slope;
                Log($"Edge trigger slope set to {slope}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set edge trigger slope");
                return false;
            }
        }

        public bool SetTriggerLevel(double level)
        {
            string command = $":TRIGger:EDGe:LEVel {level.ToString(CultureInfo.InvariantCulture)}";

            if (oscilloscope.SendCommand(command))
            {
                settings.EdgeLevel = level;
                Log($"Trigger level set to {level:F3}V");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Log("Failed to set trigger level");
                return false;
            }
        }

        public bool SetHoldoff(double holdoff)
        {
            string command = $":TRIGger:HOLDoff {holdoff.ToString(CultureInfo.InvariantCulture)}";

            if (oscilloscope.SendCommand(command))
            {
                settings.Holdoff = holdoff;
                Log($"Trigger holdoff set to {holdoff:E2}s");
                UpdateCurrentSettingsDisplay();
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
            if (TriggerModeComboBox == null) return;

            TriggerModeComboBox.Items.Clear();
            var modes = new[] { "EDGE", "PULSE", "RUNT", "WIND", "NEDG", "SLOP", "VID", "PATT", "RS232", "I2C", "SPI" };

            foreach (var mode in modes)
            {
                var item = new ComboBoxItem
                {
                    Content = mode,
                    Tag = mode
                };
                TriggerModeComboBox.Items.Add(item);

                if (mode == settings.Mode)
                {
                    TriggerModeComboBox.SelectedItem = item;
                }
            }
        }

        private void PopulateTriggerSweepOptions()
        {
            if (TriggerSweepComboBox == null) return;

            TriggerSweepComboBox.Items.Clear();
            var sweeps = new[] { "AUTO", "NORM", "SING" };

            foreach (var sweep in sweeps)
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
            if (EdgeSourceComboBox == null) return;

            EdgeSourceComboBox.Items.Clear();
            var sources = new[] { "CHAN1", "CHAN2", "EXT", "ACL" };

            foreach (var source in sources)
            {
                var item = new ComboBoxItem
                {
                    Content = source,
                    Tag = source
                };
                EdgeSourceComboBox.Items.Add(item);

                if (source == settings.EdgeSource)
                {
                    EdgeSourceComboBox.SelectedItem = item;
                }
            }
        }

        private void PopulateEdgeSlopeOptions()
        {
            if (EdgeSlopeComboBox == null) return;

            EdgeSlopeComboBox.Items.Clear();
            var slopes = new[] { "POS", "NEG", "RFAL" };

            foreach (var slope in slopes)
            {
                var item = new ComboBoxItem
                {
                    Content = slope,
                    Tag = slope
                };
                EdgeSlopeComboBox.Items.Add(item);

                if (slope == settings.EdgeSlope)
                {
                    EdgeSlopeComboBox.SelectedItem = item;
                }
            }
        }

        private void PopulateTriggerCouplingOptions()
        {
            if (TriggerCouplingComboBox == null) return;

            TriggerCouplingComboBox.Items.Clear();
            var couplings = new[] { "DC", "AC", "HFR", "LFR" };

            foreach (var coupling in couplings)
            {
                var item = new ComboBoxItem
                {
                    Content = coupling,
                    Tag = coupling
                };
                TriggerCouplingComboBox.Items.Add(item);

                if (coupling == settings.Coupling)
                {
                    TriggerCouplingComboBox.SelectedItem = item;
                }
            }
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// Update all UI elements from current settings
        /// </summary>
        public void UpdateUIFromSettings()
        {
            if (isUpdating) return;

            try
            {
                isUpdating = true;
                DisableEventHandlers();

                // Update ComboBoxes
                PopulateTriggerModeOptions();
                PopulateTriggerSweepOptions();
                PopulateEdgeSourceOptions();
                PopulateEdgeSlopeOptions();
                PopulateTriggerCouplingOptions();

                // Update CheckBox
                if (NoiseRejectCheckBox != null)
                    NoiseRejectCheckBox.IsChecked = settings.NoiseReject;

                // Update Slider
                UpdateSliderRange();
                if (TriggerLevelSlider != null)
                    TriggerLevelSlider.Value = settings.EdgeLevel;

                // Update TextBox
                if (HoldoffTextBox != null)
                    HoldoffTextBox.Text = settings.Holdoff.ToString("E2", CultureInfo.InvariantCulture);

                UpdateLevelValueDisplay();
                UpdateCurrentSettingsDisplay();
                UpdateRangeDisplays();
                UpdateHoldoffDisplay();

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

        /// <summary>
        /// Update the trigger level slider range
        /// </summary>
        public void UpdateSliderRange()
        {
            if (TriggerLevelSlider == null) return;

            var (minLevel, maxLevel) = GetEnhancedTriggerRange();

            isUpdating = true;
            TriggerLevelSlider.Minimum = minLevel;
            TriggerLevelSlider.Maximum = maxLevel;

            // Calculate smart tick frequency
            double range = maxLevel - minLevel;
            double tickFreq = range <= 4 ? 0.1 : range <= 40 ? 1 : range <= 200 ? 10 : 100;
            TriggerLevelSlider.TickFrequency = tickFreq;

            // Clamp current value to new range
            if (TriggerLevelSlider.Value < minLevel)
                TriggerLevelSlider.Value = minLevel;
            else if (TriggerLevelSlider.Value > maxLevel)
                TriggerLevelSlider.Value = maxLevel;

            isUpdating = false;

            Log($"Trigger level slider range updated: {minLevel:F1}V to {maxLevel:F1}V");
        }

        private void UpdateSliderFromSettings()
        {
            if (TriggerLevelSlider != null && !isUpdating)
            {
                isUpdating = true;
                TriggerLevelSlider.Value = settings.EdgeLevel;
                UpdateLevelValueDisplay();
                isUpdating = false;
            }
        }

        private void UpdateLevelValueDisplay()
        {
            if (LevelValueText != null && TriggerLevelSlider != null)
            {
                LevelValueText.Text = $"{TriggerLevelSlider.Value:F3}V";
            }
        }

        private void UpdateCurrentSettingsDisplay()
        {
            if (CurrentTriggerSettingsText != null)
            {
                CurrentTriggerSettingsText.Text = settings.ToString();
            }
        }

        /// <summary>
        /// Update the min/max range displays
        /// </summary>
        public void UpdateRangeDisplays()
        {
            var (minLevel, maxLevel) = GetEnhancedTriggerRange();

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

        private void UpdateHoldoffDisplay()
        {
            if (HoldoffDisplayText != null)
            {
                HoldoffDisplayText.Text = FormatTime(settings.Holdoff);
            }
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
                LevelValueText.Text = $"{newValue:F3}V";
            }

            // Send the command to the oscilloscope
            HandleTriggerLevelChanged(newValue);
        }

        private void OnHoldoffTextChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (isUpdating) return;

            if (HoldoffTextBox != null && double.TryParse(HoldoffTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double holdoff))
            {
                SetHoldoff(holdoff);
            }
        }

        private void OnForceTriggerClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (oscilloscope.SendCommand(":TFORce"))
            {
                Log("Force trigger executed");
            }
            else
            {
                Log("Failed to force trigger");
            }
        }

        private void OnQuickZeroLevelClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            SetTriggerLevel(0.0);
            Log("Trigger level set to 0V");
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
            if (ForceTriggerButton != null)
                ForceTriggerButton.Click -= OnForceTriggerClicked;
            if (QuickZeroLevelButton != null)
                QuickZeroLevelButton.Click -= OnQuickZeroLevelClicked;
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
            if (ForceTriggerButton != null)
                ForceTriggerButton.Click += OnForceTriggerClicked;
            if (QuickZeroLevelButton != null)
                QuickZeroLevelButton.Click += OnQuickZeroLevelClicked;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get enhanced trigger range using channel settings if available
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

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
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
        /// Helper method to format time values
        /// </summary>
        private string FormatTime(double time)
        {
            if (time == 0) return "0s";

            double absTime = Math.Abs(time);
            if (absTime >= 1.0)
                return $"{time:F3}s";
            else if (absTime >= 1e-3)
                return $"{time * 1000:F3}ms";
            else if (absTime >= 1e-6)
                return $"{time * 1000000:F3}μs";
            else if (absTime >= 1e-9)
                return $"{time * 1000000000:F3}ns";
            else
                return $"{time:E2}s";
        }

        #endregion

        #region Disposal

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