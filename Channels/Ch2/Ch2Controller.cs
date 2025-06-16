using DS1000Z_E_USB_Control.Channels.Ch2;
using DS1000Z_E_USB_Control.Controls;
using Rigol_DS1000Z_E_Control;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Channels.Ch2
{
    /// <summary>
    /// Controller class for Channel 2 operations and UI management
    /// Updated to work with button instead of checkbox
    /// </summary>
    public class Ch2Controller : IDisposable
    {
        private readonly RigolDS1000ZE oscilloscope;
        private readonly Ch2Settings settings;
        private bool isUpdating = false;
        private bool disposed = false;

        public event EventHandler<string> LogEvent;
        public event EventHandler SettingsChanged;

        // UI Control references
        public ComboBox ProbeRatioComboBox { get; set; }
        public ComboBox VerticalScaleComboBox { get; set; }
        public ComboBox CouplingComboBox { get; set; }
        public TextBlock CurrentSettingsTextBlock { get; set; }
        public Slider VerticalOffsetSlider { get; set; }
        public TextBlock SliderValueText { get; set; }
        public TextBlock MaxValueDisplay { get; set; }
        public TextBlock MinValueDisplay { get; set; }
        public TextBlock OffsetRangeText { get; set; }
        public Button QuickZeroButton { get; set; }
        public EmojiArrows OffsetArrowsControl { get; set; }

        public Ch2Controller(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));
            this.settings = new Ch2Settings();
        }

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

            RefreshSettings();
            UpdateCurrentSettingsDisplay();
        }

        public bool SetEnabled(bool enabled)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel2:DISPlay {(enabled ? "ON" : "OFF")}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.IsEnabled = enabled;
                Log($"Channel 2 {(enabled ? "enabled" : "disabled")}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to change Channel 2 enable state");
            }

            return success;
        }

        public bool SetProbeRatio(double ratio)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel2:PROBe {ratio.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.ProbeRatio = ratio;
                Log($"Channel 2 probe ratio set to {ratio}X");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 2 probe ratio");
            }

            return success;
        }

        public bool SetVerticalScale(double scale)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel2:SCALe {scale.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.VerticalScale = scale;
                Log($"Channel 2 vertical scale set to {scale:F3}V/div");
                UpdateCurrentSettingsDisplay();
                UpdateSliderRange();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 2 vertical scale");
            }

            return success;
        }

        public bool SetCoupling(string coupling)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel2:COUPling {coupling}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Coupling = coupling;
                Log($"Channel 2 coupling set to {coupling}");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 2 coupling");
            }

            return success;
        }

        public bool SetVerticalOffset(double offset)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel2:OFFSet {offset.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.VerticalOffset = offset;
                Log($"Channel 2 vertical offset set to {offset:F3}V");
                UpdateCurrentSettingsDisplay();
                UpdateSliderFromSettings();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 2 vertical offset");
            }

            return success;
        }

        public Ch2Settings GetSettings() => settings;

        public void RefreshSettings()
        {
            if (!oscilloscope.IsConnected) return;

            try
            {
                var enableResponse = oscilloscope.SendQuery(":CHANnel2:DISPlay?");
                if (int.TryParse(enableResponse?.Trim(), out int enableValue))
                {
                    settings.IsEnabled = enableValue == 1;
                }

                var probeResponse = oscilloscope.SendQuery(":CHANnel2:PROBe?");
                if (double.TryParse(probeResponse?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double probeValue))
                {
                    settings.ProbeRatio = probeValue;
                }

                var scaleResponse = oscilloscope.SendQuery(":CHANnel2:SCALe?");
                if (double.TryParse(scaleResponse?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double scaleValue))
                {
                    settings.VerticalScale = scaleValue;
                }

                var couplingResponse = oscilloscope.SendQuery(":CHANnel2:COUPling?");
                if (!string.IsNullOrEmpty(couplingResponse))
                {
                    settings.Coupling = couplingResponse.Trim();
                }

                var offsetResponse = oscilloscope.SendQuery(":CHANnel2:OFFSet?");
                if (double.TryParse(offsetResponse?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double offsetValue))
                {
                    settings.VerticalOffset = offsetValue;
                }

                UpdateCurrentSettingsDisplay();
                UpdateSliderFromSettings();
            }
            catch (Exception ex)
            {
                Log($"Error refreshing Channel 2 settings: {ex.Message}");
            }
        }

        public void UpdateFromSettings(Ch2Settings newSettings)
        {
            if (newSettings == null) return;

            settings.IsEnabled = newSettings.IsEnabled;
            settings.ProbeRatio = newSettings.ProbeRatio;
            settings.VerticalScale = newSettings.VerticalScale;
            settings.VerticalOffset = newSettings.VerticalOffset;
            settings.Coupling = newSettings.Coupling;
            settings.BandwidthLimit = newSettings.BandwidthLimit;
            settings.Units = newSettings.Units;
            settings.InvertEnabled = newSettings.InvertEnabled;
            settings.VernierEnabled = newSettings.VernierEnabled;

            UpdateUIFromSettings();
        }

        public void UpdateUIFromSettings()
        {
            if (isUpdating) return;

            try
            {
                isUpdating = true;
                DisableEventHandlers();

                if (ProbeRatioComboBox != null)
                {
                    foreach (ComboBoxItem item in ProbeRatioComboBox.Items)
                    {
                        if (item.Tag != null && double.TryParse(item.Tag.ToString(), out double ratio) &&
                            Math.Abs(ratio - settings.ProbeRatio) < 0.01)
                        {
                            ProbeRatioComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                if (VerticalScaleComboBox != null)
                {
                    foreach (ComboBoxItem item in VerticalScaleComboBox.Items)
                    {
                        if (item.Tag != null && double.TryParse(item.Tag.ToString(), out double scale) &&
                            Math.Abs(scale - settings.VerticalScale) < 0.0001)
                        {
                            VerticalScaleComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

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

                UpdateSliderRange();
                if (VerticalOffsetSlider != null)
                {
                    VerticalOffsetSlider.Value = settings.VerticalOffset;
                }

                UpdateCurrentSettingsDisplay();
                Log($"Updated Ch2 UI from settings: {settings}");
            }
            catch (Exception ex)
            {
                Log($"Error updating Ch2 UI: {ex.Message}");
            }
            finally
            {
                EnableEventHandlers();
                isUpdating = false;
            }
        }

        private void OnProbeRatioChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            if (ProbeRatioComboBox?.SelectedItem is ComboBoxItem item &&
                double.TryParse(item.Tag.ToString(), out double ratio))
            {
                SetProbeRatio(ratio);
            }
        }

        private void OnVerticalScaleChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            if (VerticalScaleComboBox?.SelectedItem is ComboBoxItem item &&
                double.TryParse(item.Tag.ToString(), out double scale))
            {
                SetVerticalScale(scale);
            }
        }

        private void OnCouplingChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            if (CouplingComboBox?.SelectedItem is ComboBoxItem item)
            {
                SetCoupling(item.Tag.ToString());
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

        private void PopulateProbeRatioOptions()
        {
            if (ProbeRatioComboBox == null) return;

            var ratios = new Dictionary<string, double>
            {
                { "1X", 1.0 }, { "10X", 10.0 }, { "100X", 100.0 }, { "1000X", 1000.0 }
            };

            ProbeRatioComboBox.Items.Clear();
            foreach (var ratio in ratios)
            {
                var item = new ComboBoxItem { Content = ratio.Key, Tag = ratio.Value };
                ProbeRatioComboBox.Items.Add(item);

                if (Math.Abs(ratio.Value - settings.ProbeRatio) < 0.01)
                    ProbeRatioComboBox.SelectedItem = item;
            }
        }

        private void UpdateVerticalScaleOptions()
        {
            if (VerticalScaleComboBox == null) return;

            double[] scales = { 0.001, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1.0, 2.0, 5.0, 10.0 };

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
                    VerticalScaleComboBox.SelectedItem = item;
            }
        }

        private void PopulateCouplingOptions()
        {
            if (CouplingComboBox == null) return;

            var couplings = new Dictionary<string, string>
            {
                { "DC", "DC" }, { "AC", "AC" }, { "GND", "GND" }
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
                    CouplingComboBox.SelectedItem = item;
            }
        }

        private void UpdateCurrentSettingsDisplay()
        {
            if (CurrentSettingsTextBlock == null) return;

            string enableStatus = settings.IsEnabled ? "ON" : "OFF";
            string probeRatio = $"{settings.ProbeRatio}X";
            string scale = FormatVoltageScale(settings.VerticalScale);
            string offset = FormatVoltage(settings.VerticalOffset);
            string coupling = settings.Coupling;

            CurrentSettingsTextBlock.Text = $"CH2: {enableStatus}, {probeRatio}, {scale}, Offset={offset}, {coupling}";
        }

        private string FormatVoltageScale(double scale)
        {
            return scale >= 1.0
                ? $"{scale:F0}V/div"
                : scale >= 0.001
                    ? $"{scale * 1000:F0}mV/div"
                    : $"{scale * 1000000:F0}μV/div";
        }

        private string FormatVoltage(double voltage)
        {
            string sign = voltage >= 0 ? "+" : "";
            return Math.Abs(voltage) >= 1000
                ? $"{sign}{voltage / 1000:F2}kV"
                : Math.Abs(voltage) >= 1.0
                    ? $"{sign}{voltage:F3}V"
                    : Math.Abs(voltage) >= 0.001
                        ? $"{sign}{voltage * 1000:F1}mV"
                        : $"{sign}{voltage * 1000000:F1}μV";
        }

        private void UpdateSliderRange()
        {
            if (VerticalOffsetSlider == null) return;

            var (minOffset, maxOffset) = settings.GetOffsetRange();
            VerticalOffsetSlider.Minimum = minOffset;
            VerticalOffsetSlider.Maximum = maxOffset;

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
                SliderValueText.Text = FormatVoltage(settings.VerticalOffset);
        }

        public void HandleVerticalOffsetChanged(double offset)
        {
            if (!oscilloscope.IsConnected) return;

            string command = $":CHANnel2:OFFSet {offset.ToString(CultureInfo.InvariantCulture)}";

            if (oscilloscope.SendCommand(command))
            {
                settings.VerticalOffset = offset;
                Log($"Channel 2 vertical offset set to {offset:F3}V");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 2 vertical offset");
                UpdateSliderFromSettings();
            }
        }

        private void Log(string message) => LogEvent?.Invoke(this, message);

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
                    DisableEventHandlers();
                    if (QuickZeroButton != null)
                        QuickZeroButton.Click -= QuickZeroButton_Click;
                }
                disposed = true;
            }
        }
    }
}
