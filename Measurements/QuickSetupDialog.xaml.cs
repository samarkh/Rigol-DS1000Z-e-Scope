using System;
using System.Windows;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Measurements
{
    /// <summary>
    /// Quick setup dialog for measurement presets
    /// Provides easy access to common measurement configurations
    /// </summary>
    public partial class QuickSetupDialog : Window
    {
        #region Properties

        /// <summary>
        /// The selected preset name, or null if dialog was cancelled
        /// </summary>
        public string SelectedPreset { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize the quick setup dialog
        /// </summary>
        public QuickSetupDialog()
        {
            InitializeComponent();

            // Set initial state
            SelectedPreset = null;

            // Set focus to first button for keyboard navigation
            TimeDomainButton.Focus();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle preset button clicks
        /// </summary>
        private void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string presetName)
                {
                    SelectedPreset = presetName;
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting preset: {ex.Message}",
                              "Selection Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle cancel button click
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPreset = null;
            DialogResult = false;
            Close();
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Show the dialog and return the selected preset
        /// </summary>
        /// <param name="owner">Parent window</param>
        /// <returns>Selected preset name, or null if cancelled</returns>
        public static string ShowDialog(Window owner = null)
        {
            try
            {
                var dialog = new QuickSetupDialog();

                if (owner != null)
                {
                    dialog.Owner = owner;
                }

                var result = dialog.ShowDialog();

                return result == true ? dialog.SelectedPreset : null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening quick setup dialog: {ex.Message}",
                              "Dialog Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                return null;
            }
        }

        #endregion

        #region Preset Definitions

        /// <summary>
        /// Get measurement list for the specified preset
        /// </summary>
        /// <param name="presetName">Name of the preset</param>
        /// <returns>Array of measurement names</returns>
        public static string[] GetPresetMeasurements(string presetName)
        {
            return presetName?.ToLower() switch
            {
                "timedomain" => new[]
                {
                    "FREQ",     // Frequency
                    "PER",      // Period  
                    "RTIM",     // Rise Time
                    "FTIM",     // Fall Time
                    "PWID",     // Positive Pulse Width
                    "NWID",     // Negative Pulse Width
                    "PDUT",     // Positive Duty Cycle
                    "NDUT"      // Negative Duty Cycle
                },

                "voltage" => new[]
                {
                    "VMAX",     // Maximum Voltage
                    "VMIN",     // Minimum Voltage
                    "VPP",      // Peak-to-Peak Voltage
                    "VTOP",     // Top Voltage
                    "VBAS",     // Base Voltage
                    "VAMP",     // Amplitude
                    "VAVG",     // Average Voltage
                    "VRMS"      // RMS Voltage
                },

                "comprehensive" => new[]
                {
                    // Time Domain
                    "FREQ", "PER", "RTIM", "FTIM", "PWID", "NWID", "PDUT", "NDUT",
                    // Voltage
                    "VMAX", "VMIN", "VPP", "VTOP", "VBAS", "VAMP", "VAVG", "VRMS",
                    // Additional
                    "OVER",     // Overshoot
                    "PRES",     // Preshoot
                    "VAR"       // Variance
                },

                _ => new string[0]
            };
        }

        /// <summary>
        /// Get description for the specified preset
        /// </summary>
        /// <param name="presetName">Name of the preset</param>
        /// <returns>Human-readable description</returns>
        public static string GetPresetDescription(string presetName)
        {
            return presetName?.ToLower() switch
            {
                "timedomain" => "Analyzes timing characteristics of signals including frequency, period, rise/fall times, pulse widths, and duty cycles.",
                "voltage" => "Measures voltage-related parameters including maximum, minimum, peak-to-peak, average, and RMS values.",
                "comprehensive" => "Includes all common measurements for complete signal analysis with statistical tracking enabled.",
                _ => "Unknown preset"
            };
        }

        #endregion

        #region Window Events

        /// <summary>
        /// Handle window loaded event
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Additional initialization if needed
        }

        /// <summary>
        /// Handle Escape key to cancel dialog
        /// </summary>
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                CancelButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        #endregion
    }
}