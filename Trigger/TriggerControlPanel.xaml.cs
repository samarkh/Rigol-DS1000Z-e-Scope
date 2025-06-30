using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;
using DS1000Z_E_USB_Control.Controls;
using Rigol_DS1000Z_E_Control;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Trigger
{
    /// <summary>
    /// Enhanced TriggerControlPanel with support for all trigger modes
    /// Updated to handle Edge, Pulse, Slope, Video, Pattern, Advanced, and Serial triggers
    /// CORRECTED: Using actual method names from existing codebase
    /// </summary>
    public partial class TriggerControlPanel : UserControl
    {
        #region Fields

        private TriggerController controller;
        private RigolDS1000ZE oscilloscope;
        private bool isInitialized = false;
        private bool isUpdating = false;
        private string currentTriggerMode = "EDGe";

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
                this.oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));

                // Create the controller
                controller = new TriggerController(oscilloscope);
                controller.LogEvent += (sender, message) => LogEvent?.Invoke(this, message);
                controller.SettingsChanged += (sender, e) => LogEvent?.Invoke(this, "Trigger settings changed");

                // Initialize trigger mode combo box dynamically
                InitializeTriggerModeComboBox();

                // Wire up UI controls to the controller
                WireUpControlsToController();

                // Wire up additional event handlers
                WireUpAdditionalControls();

                // Initialize the controller
                controller.InitializeControls();

                // Set up UI elements
                SetupUI();

                // Initialize parameter panels (hide all except Edge)
                InitializeParameterPanels();

                isInitialized = true;
                LogEvent?.Invoke(this, "Enhanced trigger control panel initialized with all trigger modes");




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
        /// Initialize trigger mode combo box dynamically from TriggerSettings
        /// </summary>
        private void InitializeTriggerModeComboBox()
        {
            if (TriggerModeComboBox == null) return;

            TriggerModeComboBox.Items.Clear();

            // Get all available trigger modes from settings
            var triggerModes = TriggerSettings.GetModeOptions();

            // Group them by category for better UX
            var basicModes = new[] { "EDGe", "PULSe", "SLOPe" };
            var patternModes = new[] { "PATTern", "VIDeo" };
            var advancedModes = new[] { "DURATion", "TIMeout", "RUNT", "WINDows", "DELay", "SHOLd", "NEDGe" };
            var serialModes = new[] { "RS232", "IIC", "SPI" };

            // Add basic modes
            AddTriggerModeGroup(triggerModes, basicModes);

            // Add separator and pattern modes  
            if (TriggerModeComboBox.Items.Count > 0)
                TriggerModeComboBox.Items.Add(new Separator());
            AddTriggerModeGroup(triggerModes, patternModes);

            // Add separator and advanced modes
            if (TriggerModeComboBox.Items.Count > 0)
                TriggerModeComboBox.Items.Add(new Separator());
            AddTriggerModeGroup(triggerModes, advancedModes);

            // Add separator and serial protocol modes
            if (TriggerModeComboBox.Items.Count > 0)
                TriggerModeComboBox.Items.Add(new Separator());
            AddTriggerModeGroup(triggerModes, serialModes);

            // Set default selection to Edge
            SelectComboBoxItemByTag(TriggerModeComboBox, "EDGe");
        }

        /// <summary>
        /// Add trigger mode group to combo box
        /// </summary>
        private void AddTriggerModeGroup(System.Collections.Generic.List<(string value, string display)> allModes,
                                        string[] groupModes)
        {
            foreach (var mode in groupModes)
            {
                var triggerMode = allModes.FirstOrDefault(m => m.value == mode);
                if (triggerMode != default)
                {
                    var item = new ComboBoxItem
                    {
                        Content = triggerMode.display,
                        Tag = triggerMode.value
                    };
                    TriggerModeComboBox.Items.Add(item);
                }
            }
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

            // ComboBox event handlers - Main trigger controls
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

            // Wire up parameter-specific controls
            WireUpParameterControls();

            // Subscribe to controller settings changes
            if (controller != null)
            {
                controller.SettingsChanged -= (sender, e) => UpdateTriggerLevelArrowControl();  // Unsubscribe first
                controller.SettingsChanged += (sender, e) => UpdateTriggerLevelArrowControl();   // Then subscribe
            }
        }

        /// <summary>
        /// Wire up parameter-specific controls for different trigger modes
        /// </summary>
        private void WireUpParameterControls()
        {
            // Pulse trigger controls
            if (PulseWidthConditionCombo != null)
                PulseWidthConditionCombo.SelectionChanged += PulseWidthCondition_SelectionChanged;
            if (PulseWidthLowTextBox != null)
                PulseWidthLowTextBox.LostFocus += PulseWidthLow_LostFocus;
            if (PulseWidthHighTextBox != null)
                PulseWidthHighTextBox.LostFocus += PulseWidthHigh_LostFocus;

            // Slope trigger controls
            if (SlopeTimeTextBox != null)
                SlopeTimeTextBox.LostFocus += SlopeTime_LostFocus;
            if (SlopeConditionCombo != null)
                SlopeConditionCombo.SelectionChanged += SlopeCondition_SelectionChanged;
            if (SlopeWhenCombo != null)
                SlopeWhenCombo.SelectionChanged += SlopeWhen_SelectionChanged;

            // Video trigger controls
            if (VideoStandardCombo != null)
                VideoStandardCombo.SelectionChanged += VideoStandard_SelectionChanged;
            if (VideoSyncCombo != null)
                VideoSyncCombo.SelectionChanged += VideoSync_SelectionChanged;
            if (VideoLineNumberTextBox != null)
                VideoLineNumberTextBox.LostFocus += VideoLineNumber_LostFocus;

            // Pattern trigger controls
            if (PatternCH1Combo != null)
                PatternCH1Combo.SelectionChanged += PatternCH1_SelectionChanged;
            if (PatternCH2Combo != null)
                PatternCH2Combo.SelectionChanged += PatternCH2_SelectionChanged;

            // Serial protocol controls
            WireUpSerialProtocolControls();

            // Advanced trigger controls
            if (AdvancedConditionCombo != null)
                AdvancedConditionCombo.SelectionChanged += AdvancedCondition_SelectionChanged;
            if (AdvancedTimeLowTextBox != null)
                AdvancedTimeLowTextBox.LostFocus += AdvancedTimeLow_LostFocus;
            if (AdvancedTimeHighTextBox != null)
                AdvancedTimeHighTextBox.LostFocus += AdvancedTimeHigh_LostFocus;
        }

        /// <summary>
        /// Wire up serial protocol specific controls
        /// </summary>
        private void WireUpSerialProtocolControls()
        {
            // RS232 controls
            if (RS232BaudRateCombo != null)
                RS232BaudRateCombo.SelectionChanged += RS232BaudRate_SelectionChanged;
            if (RS232DataBitsCombo != null)
                RS232DataBitsCombo.SelectionChanged += RS232DataBits_SelectionChanged;
            if (RS232ParityCombo != null)
                RS232ParityCombo.SelectionChanged += RS232Parity_SelectionChanged;
            if (RS232StopBitsCombo != null)
                RS232StopBitsCombo.SelectionChanged += RS232StopBits_SelectionChanged;

            // I2C controls
            if (I2CAddressWidthCombo != null)
                I2CAddressWidthCombo.SelectionChanged += I2CAddressWidth_SelectionChanged;
            if (I2CAddressModeCombo != null)
                I2CAddressModeCombo.SelectionChanged += I2CAddressMode_SelectionChanged;

            // SPI controls
            if (SPIModeCombo != null)
                SPIModeCombo.SelectionChanged += SPIMode_SelectionChanged;
            if (SPIClockEdgeCombo != null)
                SPIClockEdgeCombo.SelectionChanged += SPIClockEdge_SelectionChanged;
            if (SPIDataWidthTextBox != null)
                SPIDataWidthTextBox.LostFocus += SPIDataWidth_LostFocus;
            if (SPIEndianCombo != null)
                SPIEndianCombo.SelectionChanged += SPIEndian_SelectionChanged;
        }

        /// <summary>
        /// Initialize parameter panels - hide all except Edge
        /// </summary>
        private void InitializeParameterPanels()
        {
            HideAllParameterPanels();
            if (EdgeParametersPanel != null)
                EdgeParametersPanel.Visibility = Visibility.Visible;
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

        #region Main Event Handlers

        /// <summary>
        /// Handle trigger mode selection changes
        /// </summary>
        private void TriggerMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;

            var comboBox = sender as ComboBox;
            var selectedItem = comboBox?.SelectedItem as ComboBoxItem;
            var mode = selectedItem?.Tag?.ToString();

            if (string.IsNullOrEmpty(mode) || mode == currentTriggerMode) return;

            currentTriggerMode = mode;

            // Hide all parameter panels first
            HideAllParameterPanels();

            // Show appropriate panel based on trigger mode
            ShowParameterPanelForMode(mode);

            // Update the oscilloscope settings
            UpdateTriggerMode(mode);

            LogEvent?.Invoke(this, $"Trigger mode changed to: {mode}");
        }

        /// <summary>
        /// Handle trigger sweep selection changes
        /// </summary>
        private void TriggerSweep_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;

            var comboBox = sender as ComboBox;
            var selectedItem = comboBox?.SelectedItem as ComboBoxItem;
            var sweep = selectedItem?.Tag?.ToString();

            if (!string.IsNullOrEmpty(sweep))
            {
                controller?.SetSweep(sweep);
            }
        }


        /// <summary>
        /// Handle edge source selection changes
        /// FIXED: Removed call to non-existent UpdateTriggerStepsFromUI
        /// </summary>
        private void EdgeSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;

            var comboBox = sender as ComboBox;
            var selectedItem = comboBox?.SelectedItem as ComboBoxItem;
            var source = selectedItem?.Tag?.ToString();

            if (!string.IsNullOrEmpty(source))
            {
                controller?.SetEdgeSource(source);
                TriggerSourceChanged?.Invoke(this, EventArgs.Empty);

                // FIXED: Removed the problematic UpdateTriggerStepsFromUI() call
                LogEvent?.Invoke(this, $"Trigger source changed to: {source}");
            }
        }



        ///// <summary>
        ///// Handle edge source selection changes
        ///// CORRECTED: Using SetEdgeSource method
        ///// </summary>
        //private void EdgeSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (isUpdating || !isInitialized) return;

        //    var comboBox = sender as ComboBox;
        //    var selectedItem = comboBox?.SelectedItem as ComboBoxItem;
        //    var source = selectedItem?.Tag?.ToString();

        //    if (!string.IsNullOrEmpty(source))
        //    {
        //        controller?.SetEdgeSource(source);  // CORRECTED: SetEdgeSource not SetSource
        //        TriggerSourceChanged?.Invoke(this, EventArgs.Empty);
        //        UpdateTriggerStepsFromUI();
        //    }
        //}

        /// <summary>
        /// Handle edge slope selection changes
        /// CORRECTED: Using SetEdgeSlope method
        /// </summary>
        private void EdgeSlope_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;

            var comboBox = sender as ComboBox;
            var selectedItem = comboBox?.SelectedItem as ComboBoxItem;
            var slope = selectedItem?.Tag?.ToString();

            if (!string.IsNullOrEmpty(slope))
            {
                controller?.SetEdgeSlope(slope);  // CORRECTED: SetEdgeSlope not SetSlope
            }
        }

        /// <summary>
        /// Handle trigger coupling selection changes
        /// </summary>
        private void TriggerCoupling_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;

            var comboBox = sender as ComboBox;
            var selectedItem = comboBox?.SelectedItem as ComboBoxItem;
            var coupling = selectedItem?.Tag?.ToString();

            if (!string.IsNullOrEmpty(coupling))
            {
                controller?.SetCoupling(coupling);
            }
        }

        /// <summary>
        /// Handle trigger level arrow movements
        /// CORRECTED: Using HandleTriggerLevelChanged method
        /// </summary>
        private void TriggerLevelArrows_GraticuleMovement(object sender, GraticuleMovementEventArgs e)
        {
            if (isUpdating || !isInitialized) return;

            try
            {
                var newLevel = e.NewValue;
                controller?.HandleTriggerLevelChanged(newLevel);  // CORRECTED: HandleTriggerLevelChanged not SetLevel
                LogEvent?.Invoke(this, $"Trigger level adjusted to {newLevel:F3}V");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error adjusting trigger level: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle force trigger button click
        /// </summary>
        private void ForceTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (!isInitialized) return;

            controller?.ForceTrigger();
            LogEvent?.Invoke(this, "Force trigger executed");
        }

        /// <summary>
        /// Handle holdoff text changes
        /// </summary>
        private void HoldoffTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Just validate the input, don't send to oscilloscope until focus is lost
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                // Valid number, could add visual feedback here
            }
        }

        /// <summary>
        /// Handle holdoff text box focus lost
        /// </summary>
        private void HoldoffTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;

            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                controller?.SetHoldoff(value);
            }
        }

        /// <summary>
        /// Handle holdoff units selection change
        /// </summary>
        private void HoldOff_Units_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateHoldoffDisplay();
        }

        #endregion

        #region Parameter Panel Management

        /// <summary>
        /// Hide all parameter panels
        /// </summary>
        private void HideAllParameterPanels()
        {
            if (EdgeParametersPanel != null)
                EdgeParametersPanel.Visibility = Visibility.Collapsed;
            if (PulseParametersPanel != null)
                PulseParametersPanel.Visibility = Visibility.Collapsed;
            if (SlopeParametersPanel != null)
                SlopeParametersPanel.Visibility = Visibility.Collapsed;
            if (VideoParametersPanel != null)
                VideoParametersPanel.Visibility = Visibility.Collapsed;
            if (PatternParametersPanel != null)
                PatternParametersPanel.Visibility = Visibility.Collapsed;
            if (SerialParametersPanel != null)
                SerialParametersPanel.Visibility = Visibility.Collapsed;
            if (AdvancedParametersPanel != null)
                AdvancedParametersPanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Show parameter panel for specific trigger mode
        /// </summary>
        private void ShowParameterPanelForMode(string mode)
        {
            switch (mode)
            {
                case "EDGe":
                    EdgeParametersPanel.Visibility = Visibility.Visible;
                    break;

                case "PULSe":
                    PulseParametersPanel.Visibility = Visibility.Visible;
                    break;

                case "SLOPe":
                    SlopeParametersPanel.Visibility = Visibility.Visible;
                    break;

                case "VIDeo":
                    VideoParametersPanel.Visibility = Visibility.Visible;
                    break;

                case "PATTern":
                    PatternParametersPanel.Visibility = Visibility.Visible;
                    break;

                case "RS232":
                    SerialParametersPanel.Visibility = Visibility.Visible;
                    RS232Panel.Visibility = Visibility.Visible;
                    I2CPanel.Visibility = Visibility.Collapsed;
                    SPIPanel.Visibility = Visibility.Collapsed;
                    break;

                case "IIC":
                    SerialParametersPanel.Visibility = Visibility.Visible;
                    RS232Panel.Visibility = Visibility.Collapsed;
                    I2CPanel.Visibility = Visibility.Visible;
                    SPIPanel.Visibility = Visibility.Collapsed;
                    break;

                case "SPI":
                    SerialParametersPanel.Visibility = Visibility.Visible;
                    RS232Panel.Visibility = Visibility.Collapsed;
                    I2CPanel.Visibility = Visibility.Collapsed;
                    SPIPanel.Visibility = Visibility.Visible;
                    break;

                case "DURATion":
                case "TIMeout":
                case "RUNT":
                case "WINDows":
                case "DELay":
                case "SHOLd":
                case "NEDGe":
                    AdvancedParametersPanel.Visibility = Visibility.Visible;
                    break;

                default:
                    EdgeParametersPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        #endregion

        #region Parameter-Specific Event Handlers

        // Pulse Trigger Events
        private void PulseWidthCondition_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var condition = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(condition))
            {
                SendSCPICommand($":TRIGger:PULSe:WHEN {condition}");
                LogEvent?.Invoke(this, $"Pulse width condition set to: {condition}");
            }
        }

        private void PulseWidthLow_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                SendSCPICommand($":TRIGger:PULSe:WIDTh {value:E3}");
                LogEvent?.Invoke(this, $"Pulse width (low) set to: {value:E3}s");
            }
        }

        private void PulseWidthHigh_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                SendSCPICommand($":TRIGger:PULSe:UWIDth {value:E3}");
                LogEvent?.Invoke(this, $"Pulse width (high) set to: {value:E3}s");
            }
        }

        // Slope Trigger Events
        private void SlopeTime_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                SendSCPICommand($":TRIGger:SLOPe:TIME {value:E3}");
                LogEvent?.Invoke(this, $"Slope time set to: {value:E3}s");
            }
        }

        private void SlopeCondition_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var condition = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(condition))
            {
                SendSCPICommand($":TRIGger:SLOPe:SLOPe {condition}");
                LogEvent?.Invoke(this, $"Slope condition set to: {condition}");
            }
        }

        private void SlopeWhen_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var when = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(when))
            {
                SendSCPICommand($":TRIGger:SLOPe:WHEN {when}");
                LogEvent?.Invoke(this, $"Slope when set to: {when}");
            }
        }

        // Video Trigger Events
        private void VideoStandard_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var standard = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(standard))
            {
                SendSCPICommand($":TRIGger:VIDeo:STANdard {standard}");
                LogEvent?.Invoke(this, $"Video standard set to: {standard}");
            }
        }

        private void VideoSync_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var sync = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(sync))
            {
                SendSCPICommand($":TRIGger:VIDeo:SYNC {sync}");
                LogEvent?.Invoke(this, $"Video sync set to: {sync}");
            }
        }

        private void VideoLineNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var textBox = sender as TextBox;
            if (textBox != null && int.TryParse(textBox.Text, out int lineNumber))
            {
                SendSCPICommand($":TRIGger:VIDeo:LINE {lineNumber}");
                LogEvent?.Invoke(this, $"Video line number set to: {lineNumber}");
            }
        }

        // Pattern Trigger Events
        private void PatternCH1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var pattern = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(pattern))
            {
                SendSCPICommand($":TRIGger:PATTern:PATTern CHANnel1,{pattern}");
                LogEvent?.Invoke(this, $"Pattern CH1 set to: {pattern}");
            }
        }

        private void PatternCH2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var pattern = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(pattern))
            {
                SendSCPICommand($":TRIGger:PATTern:PATTern CHANnel2,{pattern}");
                LogEvent?.Invoke(this, $"Pattern CH2 set to: {pattern}");
            }
        }

        // Serial Protocol Events (RS232)
        private void RS232BaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var baudRate = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(baudRate))
            {
                SendSCPICommand($":TRIGger:RS232:BAUD {baudRate}");
                LogEvent?.Invoke(this, $"RS232 baud rate set to: {baudRate}");
            }
        }

        private void RS232DataBits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var dataBits = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(dataBits))
            {
                SendSCPICommand($":TRIGger:RS232:DATA {dataBits}");
                LogEvent?.Invoke(this, $"RS232 data bits set to: {dataBits}");
            }
        }

        private void RS232Parity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var parity = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(parity))
            {
                SendSCPICommand($":TRIGger:RS232:PARity {parity}");
                LogEvent?.Invoke(this, $"RS232 parity set to: {parity}");
            }
        }

        private void RS232StopBits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var stopBits = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(stopBits))
            {
                SendSCPICommand($":TRIGger:RS232:STOP {stopBits}");
                LogEvent?.Invoke(this, $"RS232 stop bits set to: {stopBits}");
            }
        }

        // Serial Protocol Events (I2C)
        private void I2CAddressWidth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var width = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(width))
            {
                SendSCPICommand($":TRIGger:IIC:AWIDth {width}");
                LogEvent?.Invoke(this, $"I2C address width set to: {width}");
            }
        }

        private void I2CAddressMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var mode = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(mode))
            {
                SendSCPICommand($":TRIGger:IIC:ADDRess {mode}");
                LogEvent?.Invoke(this, $"I2C address mode set to: {mode}");
            }
        }

        // Serial Protocol Events (SPI)
        private void SPIMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var mode = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(mode))
            {
                SendSCPICommand($":TRIGger:SPI:MODE {mode}");
                LogEvent?.Invoke(this, $"SPI mode set to: {mode}");
            }
        }

        private void SPIClockEdge_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var edge = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(edge))
            {
                SendSCPICommand($":TRIGger:SPI:EDGE {edge}");
                LogEvent?.Invoke(this, $"SPI clock edge set to: {edge}");
            }
        }

        private void SPIDataWidth_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var textBox = sender as TextBox;
            if (textBox != null && int.TryParse(textBox.Text, out int width))
            {
                SendSCPICommand($":TRIGger:SPI:WIDTh {width}");
                LogEvent?.Invoke(this, $"SPI data width set to: {width}");
            }
        }

        private void SPIEndian_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var endian = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(endian))
            {
                SendSCPICommand($":TRIGger:SPI:ENDian {endian}");
                LogEvent?.Invoke(this, $"SPI endian set to: {endian}");
            }
        }

        // Advanced Trigger Events
        private void AdvancedCondition_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var combo = sender as ComboBox;
            var condition = (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(condition))
            {
                // The SCPI command will depend on the current trigger mode
                var command = GetAdvancedTriggerCommand("WHEN", condition);
                if (!string.IsNullOrEmpty(command))
                {
                    SendSCPICommand(command);
                    LogEvent?.Invoke(this, $"Advanced trigger condition set to: {condition}");
                }
            }
        }

        private void AdvancedTimeLow_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                var command = GetAdvancedTriggerCommand("TIME", value.ToString("E3"));
                if (!string.IsNullOrEmpty(command))
                {
                    SendSCPICommand(command);
                    LogEvent?.Invoke(this, $"Advanced trigger time (low) set to: {value:E3}s");
                }
            }
        }

        private void AdvancedTimeHigh_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isUpdating || !isInitialized) return;
            var textBox = sender as TextBox;
            if (textBox != null && double.TryParse(textBox.Text, out double value))
            {
                var command = GetAdvancedTriggerCommand("UPPer", value.ToString("E3"));
                if (!string.IsNullOrEmpty(command))
                {
                    SendSCPICommand(command);
                    LogEvent?.Invoke(this, $"Advanced trigger time (high) set to: {value:E3}s");
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Update trigger mode on oscilloscope
        /// </summary>
        private void UpdateTriggerMode(string mode)
        {
            try
            {
                SendSCPICommand($":TRIGger:MODE {mode}");
                controller?.RefreshSettings();  // CORRECTED: RefreshSettings not RefreshSettingsFromOscilloscope
                LogEvent?.Invoke(this, $"Oscilloscope trigger mode updated to: {mode}");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error updating trigger mode: {ex.Message}");
            }
        }

        /// <summary>
        /// Send SCPI command to oscilloscope
        /// </summary>
        private bool SendSCPICommand(string command)
        {
            try
            {
                if (oscilloscope?.IsConnected == true)
                {
                    return oscilloscope.SendCommand(command);
                }
                LogEvent?.Invoke(this, "Cannot send command - oscilloscope not connected");
                return false;
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error sending SCPI command '{command}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get advanced trigger command based on current mode
        /// </summary>
        private string GetAdvancedTriggerCommand(string parameter, string value)
        {
            switch (currentTriggerMode)
            {
                case "DURATion":
                    return $":TRIGger:DURATion:{parameter} {value}";
                case "TIMeout":
                    return $":TRIGger:TIMeout:{parameter} {value}";
                case "RUNT":
                    return $":TRIGger:RUNT:{parameter} {value}";
                case "WINDows":
                    return $":TRIGger:WINDows:{parameter} {value}";
                case "DELay":
                    return $":TRIGger:DELay:{parameter} {value}";
                case "SHOLd":
                    return $":TRIGger:SHOLd:{parameter} {value}";
                case "NEDGe":
                    return $":TRIGger:NEDGe:{parameter} {value}";
                default:
                    return "";
            }
        }

        /// <summary>
        /// Select combo box item by tag value
        /// </summary>
        private void SelectComboBoxItemByTag(ComboBox comboBox, string tag)
        {
            if (comboBox == null || string.IsNullOrEmpty(tag)) return;

            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboItem && comboItem.Tag?.ToString() == tag)
                {
                    comboBox.SelectedItem = comboItem;
                    break;
                }
            }
        }

        // FIND this method in your TriggerControlPanel.xaml.cs (around line 1035) and REPLACE it:

        /// <summary>
        /// Update trigger level arrow control
        /// FIXED: Removed call to non-existent UpdateTriggerStepsFromUI
        /// </summary>
        private void UpdateTriggerLevelArrowControl()
        {
            // FIXED: Removed the problematic UpdateTriggerStepsFromUI() call
            // The step size updates will happen through the MainWindow 
            // channel-trigger connection system when channel settings change

            LogEvent?.Invoke(this, "🎯 Trigger level arrow control refreshed");
        }



        ///// <summary>
        ///// Update trigger level arrow control
        ///// </summary>
        //private void UpdateTriggerLevelArrowControl()
        //{
        //    UpdateTriggerStepsFromUI();
        //}



        /// <summary>
        /// Update holdoff display
        /// </summary>
        private void UpdateHoldoffDisplay()
        {
            // Implementation for holdoff display update if needed
        }

        #endregion

        #region Legacy Compatibility Methods

        /// <summary>
        /// Update trigger level control settings - FIXED Implementation
        /// </summary>
        public void UpdateTriggerLevelControl(Ch1Settings ch1Settings, Ch2Settings ch2Settings)
        {
            try
            {
                if (controller == null)
                {
                    LogEvent?.Invoke(this, "❌ Cannot update trigger control - controller is null");
                    return;
                }

                // FIXED: Call the controller's actual method instead of the non-existent UpdateTriggerStepsFromUI
                controller.UpdateTriggerLevelControl(ch1Settings, ch2Settings);

                LogEvent?.Invoke(this, "✅ Trigger level control updated with dynamic steps from channel settings");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"❌ Error updating trigger level control: {ex.Message}");
            }
        }

        ///// <summary>
        ///// Update trigger level control settings - FIXED to properly call controller method
        ///// </summary>
        //public void UpdateTriggerLevelControl(Ch1Settings ch1Settings, Ch2Settings ch2Settings)
        //{
        //    try
        //    {
        //        if (controller == null)
        //        {
        //            LogEvent?.Invoke(this, "❌ Cannot update trigger steps - controller is null");
        //            return;
        //        }

        //        // Call the controller's proper method (not the legacy UpdateTriggerStepsFromUI)
        //        controller.UpdateTriggerLevelControl(ch1Settings, ch2Settings);

        //        LogEvent?.Invoke(this, "✅ Trigger level control updated with dynamic steps from channel settings");
        //    }
        //    catch (Exception ex)
        //    {
        //        LogEvent?.Invoke(this, $"❌ Error updating trigger level control: {ex.Message}");
        //    }
        //}

        // ADD this new method to your TriggerControlPanel class:

        /// <summary>
        /// Method for MainWindow to call when trigger source changes
        /// This ensures step sizes are updated when source changes
        /// </summary>
        public void OnTriggerSourceChanged(Ch1Settings ch1Settings, Ch2Settings ch2Settings)
        {
            try
            {
                if (controller == null) return;

                // Update trigger control with current channel settings
                controller.UpdateTriggerLevelControl(ch1Settings, ch2Settings);

                LogEvent?.Invoke(this, "🔄 Trigger step sizes updated for new source");
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"❌ Error updating trigger steps after source change: {ex.Message}");
            }
        }



        /// <summary>
        /// Update from settings (legacy compatibility)
        /// </summary>
        public void UpdateFromSettings(object triggerSettings)
        {
            controller?.RefreshSettings();  // CORRECTED: RefreshSettings not RefreshSettingsFromOscilloscope
            LogEvent?.Invoke(this, "Trigger panel updated from settings");
        }

        /// <summary>
        /// Set enabled state (for MainWindow compatibility)
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.IsEnabled = enabled;
            LogEvent?.Invoke(this, $"Trigger control panel {(enabled ? "enabled" : "disabled")}");
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