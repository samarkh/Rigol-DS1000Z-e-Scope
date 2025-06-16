using DS1000Z_E_USB_Control.Channels.Ch2;
using DS1000Z_E_USB_Control.Controls;
using Rigol_DS1000Z_E_Control;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Channels.Ch1
{
    /// <summary>
    /// Controller class for Channel 1 operations and UI management
    /// Updated to work with button instead of checkbox
    /// </summary>
    public class Ch1Controller : IDisposable
    {
        private readonly RigolDS1000ZE oscilloscope;
        private readonly Ch1Settings settings;
        private bool isUpdating = false;
        private bool disposed = false;

        public event EventHandler<string> LogEvent;
        public event EventHandler SettingsChanged;

        #region UI Control References
        // Basic UI Control references (removed EnableCheckBox)
        public ComboBox ProbeRatioComboBox { get; set; }
        public ComboBox VerticalScaleComboBox { get; set; }
        public ComboBox CouplingComboBox { get; set; }
        public TextBlock CurrentSettingsTextBlock { get; set; }
        public Slider VerticalOffsetSlider { get; set; }
        public TextBlock SliderValueText { get; set; }

        // Enhanced UI Control References
        public TextBlock MaxValueDisplay { get; set; }
        public TextBlock MinValueDisplay { get; set; }
        public TextBlock OffsetRangeText { get; set; }
        public Button QuickZeroButton { get; set; }
        public EmojiArrows OffsetArrowsControl { get; set; }
        #endregion

        #region Constructor
        public Ch1Controller(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));
            this.settings = new Ch1Settings();
        }
        #endregion

        #region Public API Methods

        /// <summary>
        /// Initialize UI controls and set up event handlers
        /// </summary>
        public void InitializeControls()
        {
            if (ProbeRatioComboBox != null)
            {
                PopulateProbeRatioOptions();
                ProbeRatioComboBox.SelectionChanged += OnProbeRatioChanged;
            }

            if (VerticalScaleComboBox != null)
            {
                UpdateVerticalScaleOptions();
                VerticalScaleComboBox.SelectionChanged += OnVerticalScaleChanged;
            }

            if (CouplingComboBox != null)
            {
                PopulateCouplingOptions();
                CouplingComboBox.SelectionChanged += OnCouplingChanged;
            }

            if (VerticalOffsetSlider != null)
            {
                VerticalOffsetSlider.ValueChanged += OnVerticalOffsetSliderChanged;
                UpdateSliderRange();
            }

            if (QuickZeroButton != null)
            {
                QuickZeroButton.Click += QuickZeroButton_Click;
            }

            // Initialize current settings
            RefreshSettings();
            UpdateCurrentSettingsDisplay();
        }

        /// <summary>
        /// Enable or disable Channel 1
        /// </summary>
        public bool SetEnabled(bool enabled)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel1:DISPlay {(enabled ? "ON" : "OFF")}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.IsEnabled = enabled;
                Log($"Channel 1 {(enabled ? "enabled" : "disabled")}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to change Channel 1 enable state");
            }

            return success;
        }

        /// <summary>
        /// Set the probe ratio
        /// </summary>
        public bool SetProbeRatio(double ratio)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel1:PROBe {ratio.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.ProbeRatio = ratio;
                Log($"Channel 1 probe ratio set to {ratio}X");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 probe ratio");
            }

            return success;
        }

        /// <summary>
        /// Set the vertical scale
        /// </summary>
        public bool SetVerticalScale(double scale)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel1:SCALe {scale.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.VerticalScale = scale;
                Log($"Channel 1 vertical scale set to {scale:F3}V/div");
                UpdateCurrentSettingsDisplay();
                UpdateSliderRange();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 vertical scale");
            }

            return success;
        }

        /// <summary>
        /// Set the coupling mode
        /// </summary>
        public bool SetCoupling(string coupling)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel1:COUPling {coupling}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Coupling = coupling;
                Log($"Channel 1 coupling set to {coupling}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 coupling");
            }

            return success;
        }

        /// <summary>
        /// Set the vertical offset
        /// </summary>
        public bool SetVerticalOffset(double offset)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel1:OFFSet {offset.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.VerticalOffset = offset;
                Log($"Channel 1 vertical offset set to {offset:F3}V");
                UpdateCurrentSettingsDisplay();
                UpdateSliderFromSettings();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 vertical offset");
            }

            return success;
        }

        /// <summary>
        /// Get current settings
        /// </summary>
        public Ch1Settings GetSettings()
        {
            return settings;
        }

        /// <summary>
        /// Refresh settings from oscilloscope
        /// </summary>
        public void RefreshSettings()
        {
            if (!oscilloscope.IsConnected) return;

            try
            {
                // Query current settings from oscilloscope
                var enableResponse = oscilloscope.SendQuery(":CHANnel1:DISPlay?");
                if (int.TryParse(enableResponse?.Trim(), out int enableValue))
                {
                    settings.IsEnabled = enableValue == 1;
                }

                var probeResponse = oscilloscope.SendQuery(":CHANnel1:PROBe?");
                if (double.TryParse(probeResponse?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double probeValue))
                {
                    settings.ProbeRatio = probeValue;
                }

                var scaleResponse = oscilloscope.SendQuery(":CHANnel1:SCALe?");
                if (double.TryParse(scaleResponse?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double scaleValue))
                {
                    settings.VerticalScale = scaleValue;
                }

                var couplingResponse = oscilloscope.SendQuery(":CHANnel1:COUPling?");
                if (!string.IsNullOrEmpty(couplingResponse))
                {
                    settings.Coupling = couplingResponse.Trim();
                }

                var offsetResponse = oscilloscope.SendQuery(":CHANnel1:OFFSet?");
                if (double.TryParse(offsetResponse?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double offsetValue))
                {
                    settings.VerticalOffset = offsetValue;
                }

                UpdateCurrentSettingsDisplay();
                UpdateSliderFromSettings();
            }
            catch (Exception ex)
            {
                Log($"Error refreshing Channel 1 settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Update settings object from provided settings and then update UI
        /// </summary>
        public void UpdateFromSettings(Ch1Settings newSettings)
        {
            if (newSettings == null) return;

            // Update internal settings object
            settings.IsEnabled = newSettings.IsEnabled;
            settings.ProbeRatio = newSettings.ProbeRatio;
            settings.VerticalScale = newSettings.VerticalScale;
            settings.VerticalOffset = newSettings.VerticalOffset;
            settings.Coupling = newSettings.Coupling;
            settings.BandwidthLimit = newSettings.BandwidthLimit;
            settings.Units = newSettings.Units;
            settings.InvertEnabled = newSettings.InvertEnabled;
            settings.VernierEnabled = newSettings.VernierEnabled;

            // Update UI to reflect these settings
            UpdateUIFromSettings();
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

                // Update probe ratio combo box
                if (ProbeRatioComboBox != null)
                {
                    foreach (ComboBoxItem item in ProbeRatioComboBox.Items)
                    {
                        if (item.Tag != null && double.TryParse(item.Tag.ToString(), out double ratio))
                        {
                            if (Math.Abs(ratio - settings.ProbeRatio) < 0.01)
                            {
                                ProbeRatioComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }

                // Update vertical scale combo box
                if (VerticalScaleComboBox != null)
                {
                    foreach (ComboBoxItem item in VerticalScaleComboBox.Items)
                    {
                        if (item.Tag != null && double.TryParse(item.Tag.ToString(), out double scale))
                        {
                            if (Math.Abs(scale - settings.VerticalScale) < 0.0001)
                            {
                                VerticalScaleComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }

                // Update coupling combo box
                if (CouplingComboBox != null)
                {
                    foreach (ComboBoxItem item in CouplingComboBox.Items)
                    {
                        if (item.Tag != null && item.Tag.ToString().Equals(settings.Coupling, StringComparison.OrdinalIgnoreCase))
                        {
                            CouplingComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update vertical offset slider
                UpdateSliderRange();
                if (VerticalOffsetSlider != null)
                {
                    VerticalOffsetSlider.Value = settings.VerticalOffset;
                }

                // Update current settings display
                UpdateCurrentSettingsDisplay();

                Log($"Updated Ch1 UI from settings: {settings}");
            }
            catch (Exception ex)
            {
                Log($"Error updating Ch1 UI: {ex.Message}");
            }
            finally
            {
                EnableEventHandlers();
                isUpdating = false;
            }
        }

        #endregion

        #region Private Event Handlers

        private void OnProbeRatioChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = ProbeRatioComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null && double.TryParse(selectedItem.Tag.ToString(), out double ratio))
            {
                SetProbeRatio(ratio);
            }
        }

        private void OnVerticalScaleChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = VerticalScaleComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null && double.TryParse(selectedItem.Tag.ToString(), out double scale))
            {
                SetVerticalScale(scale);
            }
        }

        private void OnCouplingChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = CouplingComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                SetCoupling(selectedItem.Tag.ToString());
            }
        }

        private void OnVerticalOffsetSliderChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdating) return;

            HandleVerticalOffsetChanged(e.NewValue);
            UpdateSliderValueDisplay();
        }

        private void QuickZeroButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetVerticalOffset(0);
        }

        #endregion

        #region Private Helper Methods

        private void PopulateProbeRatioOptions()
        {
            if (ProbeRatioComboBox == null) return;

            var ratios = new Dictionary<string, double>
            {
                { "1X", 1.0 },
                { "10X", 10.0 },
                { "100X", 100.0 },
            };

            ProbeRatioComboBox.Items.Clear();
            foreach (var ratio in ratios)
            {
                var item = new ComboBoxItem
                {
                    Content = ratio.Key,
                    Tag = ratio.Value
                };
                ProbeRatioComboBox.Items.Add(item);

                if (Math.Abs(ratio.Value - settings.ProbeRatio) < 0.01)
                {
                    ProbeRatioComboBox.SelectedItem = item;
                }
            }
        }

        private void UpdateVerticalScaleOptions()
        {
            if (VerticalScaleComboBox == null) return;

            var scales = new[]
            {
                0.001, 0.002, 0.005, 0.01, 0.02, 0.05,
                0.1, 0.2, 0.5, 1.0, 2.0, 5.0, 10.0
            };

            VerticalScaleComboBox.Items.Clear();
            foreach (var scale in scales)
            {
                var item = new ComboBoxItem
                {
                    Content = FormatVoltageScale(scale),
                    Tag = scale
                };
                VerticalScaleComboBox.Items.Add(item);

                if (Math.Abs(scale - settings.VerticalScale) < 0.0001)
                {
                    VerticalScaleComboBox.SelectedItem = item;
                }
            }
        }

        private void PopulateCouplingOptions()
        {
            if (CouplingComboBox == null) return;

            var couplings = new Dictionary<string, string>
            {
                { "DC", "DC" },
                { "AC", "AC" },
                { "GND", "GND" }
            };

            CouplingComboBox.Items.Clear();
            foreach (var coupling in couplings)
            {
                var item = new ComboBoxItem
                {
                    Content = coupling.Key,
                    Tag = coupling.Value
                };
                CouplingComboBox.Items.Add(item);

                if (string.Equals(coupling.Value, settings.Coupling, StringComparison.OrdinalIgnoreCase))
                {
                    CouplingComboBox.SelectedItem = item;
                }
            }
        }

        private string FormatVoltageScale(double scale)
        {
            if (scale >= 1.0)
                return $"{scale:F0}V/div";
            else if (scale >= 0.001)
                return $"{scale * 1000:F0}mV/div";
            else
                return $"{scale * 1000000:F0}μV/div";
        }

        private void UpdateCurrentSettingsDisplay()
        {
            if (CurrentSettingsTextBlock == null) return;

            string enableStatus = settings.IsEnabled ? "ON" : "OFF";
            string probeRatio = $"{settings.ProbeRatio}X";
            string scale = FormatVoltageScale(settings.VerticalScale);
            string offset = FormatVoltage(settings.VerticalOffset);
            string coupling = settings.Coupling;

            CurrentSettingsTextBlock.Text = $"CH1: {enableStatus}, {probeRatio}, {scale}, Offset={offset}, {coupling}";
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

        private void UpdateSliderRange()
        {
            if (VerticalOffsetSlider == null) return;

            var (minOffset, maxOffset) = settings.GetOffsetRange();
            VerticalOffsetSlider.Minimum = minOffset;
            VerticalOffsetSlider.Maximum = maxOffset;

            // Update display elements
            if (MaxValueDisplay != null)
                MaxValueDisplay.Text = FormatVoltage(maxOffset);
            if (MinValueDisplay != null)
                MinValueDisplay.Text = FormatVoltage(minOffset);
            if (OffsetRangeText != null)
                OffsetRangeText.Text = $"Range: {FormatVoltage(minOffset)} to {FormatVoltage(maxOffset)}";
        }

        private void UpdateSliderFromSettings()
        {
            if (VerticalOffsetSlider == null) return;

            isUpdating = true;
            try
            {
                VerticalOffsetSlider.Value = settings.VerticalOffset;
                UpdateSliderValueDisplay();
            }
            finally
            {
                isUpdating = false;
            }
        }

        private void UpdateSliderValueDisplay()
        {
            if (SliderValueText != null)
            {
                SliderValueText.Text = FormatVoltage(settings.VerticalOffset);
            }
        }

        /// <summary>
        /// Handle vertical offset changes from slider or other sources
        /// </summary>
        public void HandleVerticalOffsetChanged(double offset)
        {
            if (!oscilloscope.IsConnected) return;

            string command = $":CHANnel1:OFFSet {offset.ToString(CultureInfo.InvariantCulture)}";

            if (oscilloscope.SendCommand(command))
            {
                settings.VerticalOffset = offset;
                Log($"Channel 1 vertical offset set to {offset:F3}V");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 vertical offset");
                UpdateSliderFromSettings();
            }
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }

        private void DisableEventHandlers()
        {
            if (ProbeRatioComboBox != null)
                ProbeRatioComboBox.SelectionChanged -= OnProbeRatioChanged;
            if (VerticalScaleComboBox != null)
                VerticalScaleComboBox.SelectionChanged -= OnVerticalScaleChanged;
            if (CouplingComboBox != null)
                CouplingComboBox.SelectionChanged -= OnCouplingChanged;
            if (VerticalOffsetSlider != null)
                VerticalOffsetSlider.ValueChanged -= OnVerticalOffsetSliderChanged;
        }

        private void EnableEventHandlers()
        {
            if (ProbeRatioComboBox != null)
                ProbeRatioComboBox.SelectionChanged += OnProbeRatioChanged;
            if (VerticalScaleComboBox != null)
                VerticalScaleComboBox.SelectionChanged += OnVerticalScaleChanged;
            if (CouplingComboBox != null)
                CouplingComboBox.SelectionChanged += OnCouplingChanged;
            if (VerticalOffsetSlider != null)
                VerticalOffsetSlider.ValueChanged += OnVerticalOffsetSliderChanged;
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Clean up event handlers
                    DisableEventHandlers();
                    if (QuickZeroButton != null)
                        QuickZeroButton.Click -= QuickZeroButton_Click;
                }
                disposed = true;
            }
        }

        #endregion
    }
}