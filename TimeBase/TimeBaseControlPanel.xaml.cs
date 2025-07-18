using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Rigol_DS1000Z_E_Control;
using DS1000Z_E_USB_Control.Controls;

namespace DS1000Z_E_USB_Control.TimeBase
{
    /// <summary>
    /// Interaction logic for TimeBaseControlPanel.xaml
    /// TimeBase UI UserControl with Arrow Control for Offset
    /// </summary>
    public partial class TimeBaseControlPanel : UserControl
    {
        private TimeBaseController controller;
        private bool isUpdating = false;
        private bool isInitialized = false;

        public event EventHandler<string> LogEvent;
        public event EventHandler SettingsChanged;
        // Add this new event:
        public event EventHandler<double> TimebaseChanged;


        public TimeBaseControlPanel()
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
            controller = new TimeBaseController(oscilloscope);
            controller.LogEvent += (sender, message) => LogEvent?.Invoke(this, message);
            controller.SettingsChanged += (sender, e) => LogEvent?.Invoke(this, "TimeBase settings changed");
            controller.TimebaseChanged += OnTimebaseChanged;



            // Wire up UI controls to the controller
            controller.TimeBaseModeComboBox = TimeBaseModeComboBox;
            controller.HorizontalScaleComboBox = HorizontalScaleComboBox;

            // Wire up display controls
            controller.TimeWindowText = TimeWindowText;
            controller.SampleRateText = SampleRateText;
            controller.MemoryDepthText = MemoryDepthText;
            controller.AcquisitionTypeText = AcquisitionTypeText;
            controller.CurrentTimeBaseSettingsText = CurrentTimeBaseSettingsText;
            controller.OffsetRangeText = OffsetRangeText;
            controller.MinOffsetDisplay = MinOffsetDisplay;
            controller.MaxOffsetDisplay = MaxOffsetDisplay;


            // Wire up additional UI elements
            WireUpAdditionalControls();

            // Initialize the controller
            controller.InitializeControls();

            // Initialize UI elements
            InitializeUI();

            isInitialized = true;
            LogEvent?.Invoke(this, "TimeBase control panel initialized with arrow controls");
        }

        /// <summary>
        /// Wire up additional controls not handled by the base controller
        /// </summary>
        private void WireUpAdditionalControls()
        {
            if (TimeBaseModeComboBox != null)
            {
                TimeBaseModeComboBox.SelectionChanged += TimeBaseMode_SelectionChanged;
            }

            if (HorizontalScaleComboBox != null)
            {
                HorizontalScaleComboBox.SelectionChanged += HorizontalScale_SelectionChanged;
            }

            // REPLACED: Slider event handler with arrow control handler
            if (HorizontalOffsetArrows != null)
            {
                HorizontalOffsetArrows.GraticuleMovement += HorizontalOffsetArrows_GraticuleMovement;
            }

            // Subscribe to settings changes to update arrow control
            if (controller != null)
            {
                controller.SettingsChanged += (sender, e) => UpdateHorizontalOffsetArrowControl();
            }
        }

        /// <summary>
        /// Initialize the UI elements
        /// </summary>
        private void InitializeUI()
        {
            PopulateHorizontalScaleOptions();
            UpdateHorizontalOffsetArrowControl();
            UpdateDisplays();
        }

        /// <summary>
        /// Populate horizontal scale options
        /// </summary>
        private void PopulateHorizontalScaleOptions()
        {
            if (HorizontalScaleComboBox == null) return;

            var scaleOptions = TimeBaseSettings.GetHorizontalScaleOptions();

            HorizontalScaleComboBox.Items.Clear();
            foreach (var option in scaleOptions)
            {
                var item = new ComboBoxItem
                {
                    Content = option.display,
                    Tag = option.value.ToString(CultureInfo.InvariantCulture)
                };
                HorizontalScaleComboBox.Items.Add(item);

                // Select 1ms/div as default
                if (Math.Abs(option.value - 1e-3) < 1e-9)
                {
                    HorizontalScaleComboBox.SelectedItem = item;
                }
            }
        }

        /// <summary>
        /// NEW: Handle graticule arrow movement for horizontal offset
        /// </summary>
        private void HorizontalOffsetArrows_GraticuleMovement(object sender, GraticuleMovementEventArgs e)
        {
            if (controller == null) return;

            controller.HandleHorizontalOffsetChanged(e.NewValue);
            LogEvent?.Invoke(this, $"Horizontal offset moved {e.GraticuleMultiplier:F1} graticule to {FormatTime(e.NewValue)}");
        }

        /// <summary>
        /// NEW: Update the horizontal offset arrow control
        /// </summary>
        public void UpdateHorizontalOffsetArrowControl()
        {
            if (HorizontalOffsetArrows == null || controller == null) return;

            var settings = controller.GetSettings();
            var (minOffset, maxOffset) = settings.GetOffsetRange();

            // Update arrow control properties
            HorizontalOffsetArrows.GraticuleSize = settings.MainScale; // 1 graticule = 1 time scale
           // HorizontalOffsetArrows.Units = "s";
            HorizontalOffsetArrows.UpdateRange(minOffset, maxOffset);
            HorizontalOffsetArrows.SetValue(settings.MainOffset);

            // Update range displays
            if (MinOffsetDisplay != null)
                MinOffsetDisplay.Text = FormatTime(minOffset);
            if (MaxOffsetDisplay != null)
                MaxOffsetDisplay.Text = FormatTime(maxOffset);
            if (OffsetRangeText != null)
            {
                string rangeText;
                if (Math.Abs(minOffset) == Math.Abs(maxOffset))
                {
                    rangeText = $"Range: ±{FormatTime(Math.Abs(maxOffset))}";
                }
                else
                {
                    rangeText = $"Range: {FormatTime(minOffset)} to {FormatTime(maxOffset)}";
                }
                OffsetRangeText.Text = rangeText;
            }
            if (OffsetRangeDisplay != null)
            {
                string rangeText;
                if (Math.Abs(minOffset) == Math.Abs(maxOffset))
                {
                    rangeText = $"Range: ±{FormatTime(Math.Abs(maxOffset))}";
                }
                else
                {
                    rangeText = $"Range: {FormatTime(minOffset)} to {FormatTime(maxOffset)}";
                }
                OffsetRangeDisplay.Text = rangeText;
            }

            LogEvent?.Invoke(this, $"TimeBase offset range updated: {FormatTime(minOffset)} to {FormatTime(maxOffset)}");
        }

        /// <summary>
        /// Update all display elements
        /// </summary>
        private void UpdateDisplays()
        {
            if (controller == null) return;

            var settings = controller.GetSettings();

            // Update time window
            if (TimeWindowText != null)
            {
                TimeWindowText.Text = FormatTime(settings.TimeWindow);
            }

            // Update current settings display
            if (CurrentTimeBaseSettingsText != null)
            {
                CurrentTimeBaseSettingsText.Text =
                    $"Current: Mode={settings.Mode}, Scale={settings.MainScaleDisplay}, " +
                    $"Offset={FormatTime(settings.MainOffset)}, Window={FormatTime(settings.TimeWindow)}";
            }
        }

        /// <summary>
        /// Notify parent window to update mathematics panel timebase
        /// </summary>
        private void NotifyMathematicsPanelTimebaseChanged(double newTimebaseSeconds)
        {
            // Get reference to MainWindow and call the notification method
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.NotifyMathematicsPanelTimebaseChanged(newTimebaseSeconds);
        }

        private void OnTimebaseChanged(object sender, double newTimebaseSeconds)
        {
            // Notify the mathematics panel about the timebase change
            NotifyMathematicsPanelTimebaseChanged(newTimebaseSeconds);
        }

        #region Event Handlers

        /// <summary>
        /// Handle timebase mode changes
        /// </summary>
        private void TimeBaseMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || controller == null) return;

            var selectedItem = TimeBaseModeComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string mode = selectedItem.Tag.ToString();
                controller.SetMode(mode);
            }
        }

        /// <summary>
        /// Handle horizontal scale changes
        /// </summary>
        private void HorizontalScale_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || controller == null) return;

            var selectedItem = HorizontalScaleComboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem != null && double.TryParse(selectedItem.Tag.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double scale))
            {
                controller.SetHorizontalScale(scale);
                UpdateHorizontalOffsetArrowControl(); // Update range when scale changes
            }
        }

        /// <summary>
        /// Quick zero offset button handler
        /// </summary>
        private void QuickZeroOffset_Click(object sender, RoutedEventArgs e)
        {
            if (controller == null) return;

            controller.SetHorizontalOffset(0);
            LogEvent?.Invoke(this, "TimeBase offset zeroed");
        }

        #endregion

        #region Public API

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
            if (controller == null) return;

            controller.QueryAndUpdateSettings();
            UpdateFromCurrentSettings();
        }

        /// <summary>
        /// Update UI from current controller settings
        /// </summary>
        private void UpdateFromCurrentSettings()
        {
            if (controller == null) return;

            try
            {
                isUpdating = true;

                var settings = controller.GetSettings();

                // Update mode
                if (TimeBaseModeComboBox != null)
                {
                    foreach (ComboBoxItem item in TimeBaseModeComboBox.Items)
                    {
                        if (item.Tag.ToString().Equals(settings.Mode, StringComparison.OrdinalIgnoreCase))
                        {
                            TimeBaseModeComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update horizontal scale
                if (HorizontalScaleComboBox != null)
                {
                    foreach (ComboBoxItem item in HorizontalScaleComboBox.Items)
                    {
                        if (double.TryParse(item.Tag.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double itemScale) &&
                            Math.Abs(itemScale - settings.MainScale) < settings.MainScale * 0.01)
                        {
                            HorizontalScaleComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Update horizontal offset arrow control
                UpdateHorizontalOffsetArrowControl();

                UpdateDisplays();

                LogEvent?.Invoke(this, $"Updated TimeBase UI: {settings}");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error updating TimeBase UI: {ex.Message}");
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Get current timebase settings
        /// </summary>
        public TimeBaseSettings GetSettings()
        {
            return controller?.GetSettings();
        }

        /// <summary>
        /// Set timebase settings (sends commands to oscilloscope)
        /// </summary>
        public void SetSettings(TimeBaseSettings settings)
        {
            if (controller == null || settings == null) return;

            controller.SetSettings(settings);
            UpdateFromCurrentSettings();
        }

        /// <summary>
        /// Update UI from settings (does NOT send commands to oscilloscope)
        /// </summary>
        public void UpdateFromSettings(TimeBaseSettings settings)
        {
            if (settings == null) return;

            // This would update the controller's internal settings and then refresh the UI
            UpdateFromCurrentSettings();
        }

        /// <summary>
        /// Apply a preset configuration
        /// </summary>
        public void ApplyPreset(TimeBaseSettings preset)
        {
            SetSettings(preset);
            LogEvent?.Invoke(this, $"Applied TimeBase preset: {preset}");
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Cleanup()
        {
            // Remove event handlers
            if (TimeBaseModeComboBox != null)
                TimeBaseModeComboBox.SelectionChanged -= TimeBaseMode_SelectionChanged;
            if (HorizontalScaleComboBox != null)
                HorizontalScaleComboBox.SelectionChanged -= HorizontalScale_SelectionChanged;
            if (HorizontalOffsetArrows != null)
                HorizontalOffsetArrows.GraticuleMovement -= HorizontalOffsetArrows_GraticuleMovement;


            if (controller != null)
            {
                controller.SettingsChanged -= (sender, e) => UpdateHorizontalOffsetArrowControl();
            }

            controller = null;
            isInitialized = false;
        }

        /// <summary>
        /// Force update of all displays
        /// </summary>
        public void RefreshDisplays()
        {
            UpdateHorizontalOffsetArrowControl();
            UpdateDisplays();
        }

        /// <summary>
        /// Check if the panel is properly initialized
        /// </summary>
        public new bool IsInitialized => isInitialized;

        /// <summary>
        /// Get the current controller (for external access)
        /// </summary>
        public TimeBaseController GetController()
        {
            return controller;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Smart time formatting
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

        /// <summary>
        /// Update additional displays with acquisition information
        /// </summary>
        public void UpdateAcquisitionInfo(string sampleRate, string memoryDepth, string acquisitionType)
        {
            if (SampleRateText != null)
                SampleRateText.Text = sampleRate;
            if (MemoryDepthText != null)
                MemoryDepthText.Text = memoryDepth;
            if (AcquisitionTypeText != null)
                AcquisitionTypeText.Text = acquisitionType;
        }

        #endregion
    }
}