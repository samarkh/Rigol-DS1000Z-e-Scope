using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;
using Rigol_DS1000Z_E_Control;

namespace DS1000Z_E_USB_Control.Channels.Ch1
{
    /// <summary>
    /// Controller class for Channel 1 operations and UI management
    /// </summary>
    public class Ch1Controller
    {
        private readonly RigolDS1000ZE oscilloscope;
        private readonly Ch1Settings settings;
        private bool isUpdating = false;

        public event EventHandler<string> LogEvent;
        public event EventHandler SettingsChanged;

        // UI Control references
        public CheckBox EnableCheckBox { get; set; }
        public ComboBox ProbeRatioComboBox { get; set; }
        public ComboBox VerticalScaleComboBox { get; set; }
        public TextBox VerticalOffsetTextBox { get; set; }
        public ComboBox UnitsComboBox { get; set; }
        public TextBlock CurrentSettingsTextBlock { get; set; }

        public Ch1Controller(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope;
            this.settings = new Ch1Settings();
        }

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

            if (EnableCheckBox != null)
            {
                EnableCheckBox.Checked += OnEnableChanged;
                EnableCheckBox.Unchecked += OnEnableChanged;
            }

            if (UnitsComboBox != null)
            {
                UnitsComboBox.SelectionChanged += OnUnitsChanged;
            }
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
            }
            else
            {
                Log("Failed to change Channel 1 enable state");
            }

            return success;
        }

        /// <summary>
        /// Set the probe ratio for Channel 1
        /// </summary>
        public bool SetProbeRatio(double ratio)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel1:PROBe {ratio.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.ProbeRatio = ratio;
                Log($"Channel 1 probe ratio set to {ratio}×");

                // Update scale options when probe ratio changes
                UpdateVerticalScaleOptions();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 probe ratio");
            }

            return success;
        }

        /// <summary>
        /// Set the vertical scale for Channel 1
        /// </summary>
        public bool SetVerticalScale(double scale)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel1:SCALe {scale.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.VerticalScale = scale;
                Log($"Channel 1 vertical scale set to {scale}V/div");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 vertical scale");
            }

            return success;
        }

        /// <summary>
        /// Set the vertical offset for Channel 1
        /// </summary>
        public bool SetVerticalOffset(double offset)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel1:OFFSet {offset.ToString(CultureInfo.InvariantCulture)}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.VerticalOffset = offset;
                Log($"Channel 1 vertical offset set to {offset}V");
                UpdateCurrentSettingsDisplay();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 vertical offset");
            }

            return success;
        }

        /// <summary>
        /// Set the display units for Channel 1
        /// </summary>
        public bool SetUnits(string units)
        {
            if (!oscilloscope.IsConnected) return false;

            string command = $":CHANnel1:UNITs {units}";
            bool success = oscilloscope.SendCommand(command);

            if (success)
            {
                settings.Units = units;
                Log($"Channel 1 display units set to {units}");
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log("Failed to set Channel 1 display units");
            }

            return success;
        }

        /// <summary>
        /// Query all Channel 1 settings from the oscilloscope
        /// </summary>
        public void QueryAndUpdateSettings()
        {
            if (!oscilloscope.IsConnected || isUpdating) return;

            try
            {
                isUpdating = true;
                DisableEventHandlers();

                // Query current settings
                string enableState = oscilloscope.SendQuery(":CHANnel1:DISPlay?");
                string probeRatio = oscilloscope.SendQuery(":CHANnel1:PROBe?");
                string verticalScale = oscilloscope.SendQuery(":CHANnel1:SCALe?");
                string verticalOffset = oscilloscope.SendQuery(":CHANnel1:OFFSet?");
                string units = oscilloscope.SendQuery(":CHANnel1:UNITs?");

                // Update settings object
                if (!string.IsNullOrEmpty(enableState))
                {
                    settings.IsEnabled = enableState.Trim() == "1";
                }

                if (!string.IsNullOrEmpty(probeRatio) && double.TryParse(probeRatio, NumberStyles.Float, CultureInfo.InvariantCulture, out double probe))
                {
                    settings.ProbeRatio = probe;
                }

                if (!string.IsNullOrEmpty(verticalScale) && double.TryParse(verticalScale, NumberStyles.Float, CultureInfo.InvariantCulture, out double scale))
                {
                    settings.VerticalScale = scale;
                }

                if (!string.IsNullOrEmpty(verticalOffset) && double.TryParse(verticalOffset, NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
                {
                    settings.VerticalOffset = offset;
                }

                if (!string.IsNullOrEmpty(units))
                {
                    settings.Units = units.Trim();
                }

                // Update UI controls
                UpdateUIFromSettings();
                Log("Channel 1 settings updated from oscilloscope");
            }
            catch (Exception ex)
            {
                Log($"Error querying Channel 1 settings: {ex.Message}");
            }
            finally
            {
                EnableEventHandlers();
                isUpdating = false;
            }
        }

        /// <summary>
        /// Update UI controls from current settings
        /// </summary>
        private void UpdateUIFromSettings()
        {
            // Update enable checkbox
            if (EnableCheckBox != null)
            {
                EnableCheckBox.IsChecked = settings.IsEnabled;
            }

            // Update probe ratio
            if (ProbeRatioComboBox != null)
            {
                foreach (ComboBoxItem item in ProbeRatioComboBox.Items)
                {
                    if (double.TryParse(item.Tag.ToString(), out double itemProbe) &&
                        Math.Abs(itemProbe - settings.ProbeRatio) < 0.001)
                    {
                        ProbeRatioComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            // Update vertical scale options and selection
            UpdateVerticalScaleOptions();
            if (VerticalScaleComboBox != null)
            {
                foreach (ComboBoxItem item in VerticalScaleComboBox.Items)
                {
                    if (double.TryParse(item.Tag.ToString(), out double itemScale) &&
                        Math.Abs(itemScale - settings.VerticalScale) < 0.0001)
                    {
                        VerticalScaleComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            // Update vertical offset
            if (VerticalOffsetTextBox != null)
            {
                VerticalOffsetTextBox.Text = settings.VerticalOffset.ToString("F3", CultureInfo.InvariantCulture);
            }

            // Update units
            if (UnitsComboBox != null)
            {
                string unitsUpper = settings.Units.ToUpper();
                foreach (ComboBoxItem item in UnitsComboBox.Items)
                {
                    if (item.Tag.ToString().ToUpper() == unitsUpper)
                    {
                        UnitsComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            // Update current settings display
            UpdateCurrentSettingsDisplay();
        }

        /// <summary>
        /// Update the vertical scale options based on current probe ratio
        /// </summary>
        public void UpdateVerticalScaleOptions()
        {
            if (VerticalScaleComboBox == null) return;

            var scaleOptions = Ch1Settings.GetScaleOptionsForProbeRatio(settings.ProbeRatio);

            VerticalScaleComboBox.Items.Clear();
            foreach (var option in scaleOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = option.display,
                    Tag = option.value.ToString(CultureInfo.InvariantCulture)
                };
                VerticalScaleComboBox.Items.Add(item);
            }

            // Select 1V/div as default if nothing is selected
            if (VerticalScaleComboBox.SelectedItem == null)
            {
                foreach (ComboBoxItem item in VerticalScaleComboBox.Items)
                {
                    if (item.Tag.ToString() == "1")
                    {
                        VerticalScaleComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Update the current settings display text
        /// </summary>
        private void UpdateCurrentSettingsDisplay()
        {
            if (CurrentSettingsTextBlock != null)
            {
                double range = settings.VerticalScale * 8; // 8 divisions
                CurrentSettingsTextBlock.Text = $"Current: Scale={settings.VerticalScale:F3}V/div, Offset={settings.VerticalOffset:F3}V, Range={range:F1}V";
            }
        }

        /// <summary>
        /// Populate probe ratio options
        /// </summary>
        private void PopulateProbeRatioOptions()
        {
            if (ProbeRatioComboBox == null) return;

            var probeOptions = Ch1Settings.GetProbeRatioOptions();

            ProbeRatioComboBox.Items.Clear();
            foreach (var option in probeOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = option.display,
                    Tag = option.value.ToString(CultureInfo.InvariantCulture)
                };
                ProbeRatioComboBox.Items.Add(item);

                // Select 10× as default
                if (option.value == 10.0)
                {
                    ProbeRatioComboBox.SelectedItem = item;
                }
            }
        }

        #region Event Handlers

        private void OnEnableChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (isUpdating) return;

            bool enabled = EnableCheckBox?.IsChecked ?? false;
            if (!SetEnabled(enabled))
            {
                // Revert on failure
                isUpdating = true;
                if (EnableCheckBox != null)
                    EnableCheckBox.IsChecked = !enabled;
                isUpdating = false;
            }
        }

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

        private void OnUnitsChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating) return;

            var selectedItem = UnitsComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                SetUnits(selectedItem.Tag.ToString());
            }
        }

        #endregion

        #region Event Handler Management

        private void DisableEventHandlers()
        {
            if (EnableCheckBox != null)
            {
                EnableCheckBox.Checked -= OnEnableChanged;
                EnableCheckBox.Unchecked -= OnEnableChanged;
            }
            if (ProbeRatioComboBox != null)
                ProbeRatioComboBox.SelectionChanged -= OnProbeRatioChanged;
            if (VerticalScaleComboBox != null)
                VerticalScaleComboBox.SelectionChanged -= OnVerticalScaleChanged;
            if (UnitsComboBox != null)
                UnitsComboBox.SelectionChanged -= OnUnitsChanged;
        }

        private void EnableEventHandlers()
        {
            if (EnableCheckBox != null)
            {
                EnableCheckBox.Checked += OnEnableChanged;
                EnableCheckBox.Unchecked += OnEnableChanged;
            }
            if (ProbeRatioComboBox != null)
                ProbeRatioComboBox.SelectionChanged += OnProbeRatioChanged;
            if (VerticalScaleComboBox != null)
                VerticalScaleComboBox.SelectionChanged += OnVerticalScaleChanged;
            if (UnitsComboBox != null)
                UnitsComboBox.SelectionChanged += OnUnitsChanged;
        }

        #endregion

        /// <summary>
        /// Get current Channel 1 settings
        /// </summary>
        public Ch1Settings GetSettings()
        {
            return settings.Clone();
        }

        /// <summary>
        /// Set Channel 1 settings
        /// </summary>
        public void SetSettings(Ch1Settings newSettings)
        {
            if (newSettings == null) return;

            SetEnabled(newSettings.IsEnabled);
            SetProbeRatio(newSettings.ProbeRatio);
            SetVerticalScale(newSettings.VerticalScale);
            SetVerticalOffset(newSettings.VerticalOffset);
            SetUnits(newSettings.Units);
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
    }
}