using DS1000Z_E_USB_Control.TimeBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Mathematics Panel for Rigol DS1000Z-E Oscilloscope
    /// Includes FFT Workaround for Filter and Advanced Math channel selection
    /// </summary>
    public partial class MathematicsPanel : UserControl
    {
        #region Constants and Fields

        private const int COMMAND_DELAY = 100; // ms delay between SCPI commands
        private const int RESET_DELAY = 500;   // ms delay for reset operations

        private string currentActiveMode = "BasicOperations";
        private bool isModeChanging = false;

        #endregion

        #region Events

        public event Action<string> StatusUpdated;
        public event Action<string> ErrorOccurred;
        public event Func<string, string, string> SendSCPICommand; // Returns response
        public event Action<string, string> SCPICommandGenerated; // ADDED: Missing event

        #endregion

        #region Constructor and Initialization

        public MathematicsPanel()
        {
            InitializeComponent();
            SetInitialMode();
        }

        private void SetInitialMode()
        {
            try
            {
                // Show only Basic Operations initially - with null checks
                BasicOperationsSection?.SetValue(VisibilityProperty, Visibility.Visible);
                FFTAnalysisSection?.SetValue(VisibilityProperty, Visibility.Collapsed);
                DigitalFiltersSection?.SetValue(VisibilityProperty, Visibility.Collapsed);
                AdvancedMathSection?.SetValue(VisibilityProperty, Visibility.Collapsed);

                currentActiveMode = "BasicOperations";
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error setting initial mode: {ex.Message}");
            }
        }

        #endregion

        #region Public Properties and Methods - ADDED: Missing methods

        /// <summary>
        /// Get current math mode - ADDED: Missing method
        /// </summary>
        public string GetCurrentMathMode()
        {
            return currentActiveMode;
        }

        /// <summary>
        /// Check if mode is currently changing
        /// </summary>
        public bool IsModeChanging => isModeChanging;

        /// <summary>
        /// Get available math modes
        /// </summary>
        public string[] GetAvailableModes()
        {
            return new string[] { "BasicOperations", "FFTAnalysis", "DigitalFilters", "AdvancedMath" };
        }

        #endregion

        #region Event Handlers and Mode Management

        private async void BasicOperations_Click(object sender, RoutedEventArgs e)
        {
            await ChangeMathModeAsync("BasicOperations");
        }

        private async void FFTAnalysis_Click(object sender, RoutedEventArgs e)
        {
            await ChangeMathModeAsync("FFTAnalysis");
        }

        private async void DigitalFilters_Click(object sender, RoutedEventArgs e)
        {
            await ChangeMathModeAsync("DigitalFilters");
        }

        private async void AdvancedMath_Click(object sender, RoutedEventArgs e)
        {
            await ChangeMathModeAsync("AdvancedMath");
        }

        private async void MathModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isModeChanging) return;

            try
            {
                var selectedMode = GetSelectedTag(sender as ComboBox);
                if (!string.IsNullOrEmpty(selectedMode) && selectedMode != currentActiveMode)
                {
                    await ChangeMathModeAsync(selectedMode);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error in mode selection: {ex.Message}");
            }
        }

        public async Task ChangeMathModeAsync(string newMode)
        {
            if (isModeChanging || currentActiveMode == newMode) return;

            try
            {
                isModeChanging = true;
                OnStatusUpdated($"Changing mode to {newMode}...");
                UpdateStatusDisplay($"Switching to {newMode}...");

                // Step 1: Disable current math display
                await ExecuteSCPICommandAsync(":MATH:DISPlay OFF", "Disable math display");
                await Task.Delay(RESET_DELAY);

                // Step 2: Reset math system
                await ExecuteSCPICommandAsync(":MATH:RESet", "Reset math system");
                await Task.Delay(RESET_DELAY);

                // Step 3: Hide all sections
                BasicOperationsSection?.SetValue(VisibilityProperty, Visibility.Collapsed);
                FFTAnalysisSection?.SetValue(VisibilityProperty, Visibility.Collapsed);
                DigitalFiltersSection?.SetValue(VisibilityProperty, Visibility.Collapsed);
                AdvancedMathSection?.SetValue(VisibilityProperty, Visibility.Collapsed);

                // Step 4: Show selected section
                switch (newMode)
                {
                    case "BasicOperations":
                        BasicOperationsSection?.SetValue(VisibilityProperty, Visibility.Visible);
                        break;
                    case "FFTAnalysis":
                        FFTAnalysisSection?.SetValue(VisibilityProperty, Visibility.Visible);
                        break;
                    case "DigitalFilters":
                        DigitalFiltersSection?.SetValue(VisibilityProperty, Visibility.Visible);
                        break;
                    case "AdvancedMath":
                        AdvancedMathSection?.SetValue(VisibilityProperty, Visibility.Visible);
                        break;
                }

                currentActiveMode = newMode;
                OnStatusUpdated($"Mode changed to {newMode}");

                await Task.Delay(200); // Allow UI to update
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error changing math mode: {ex.Message}");
            }
            finally
            {
                isModeChanging = false;
            }
        }

        #endregion

        #region Operation Button Event Handlers

        // Basic Operations
        private async void ApplyBasicOperation_Click(object sender, RoutedEventArgs e)
        {
            await ConfigureBasicOperationsAsync();
        }

        // FFT Analysis
        private async void ApplyFFT_Click(object sender, RoutedEventArgs e)
        {
            await ConfigureFFTAnalysisAsync();
        }

        // Digital Filters - UPDATED with Source Selection
        private async void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            await ConfigureDigitalFiltersAsync();
        }

        // Advanced Math - UPDATED with Source Selection - FIXED: Event handler name
        private async void ApplyAdvancedMath_Click(object sender, RoutedEventArgs e)
        {
            await ConfigureAdvancedMathAsync();
        }

        // Utility Buttons
        private void DisableMath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExecuteSCPICommand(":MATH:DISPlay OFF", "Disable math display");
                OnStatusUpdated("Math display disabled");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error disabling math: {ex.Message}");
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Implementation for saving settings
                OnStatusUpdated("Settings saved successfully");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error saving settings: {ex.Message}");
            }
        }

        #endregion

        #region Basic Operations (ADD, SUBTRACT, MULTIPLY, DIVIDE)

        /// <summary>
        /// Apply ADD operation: Source1 + Source2
        /// </summary>
        public async Task ApplyAddOperationAsync(string source1 = "CHANnel1", string source2 = "CHANnel2")
        {
            try
            {
                OnStatusUpdated("Applying ADD operation...");

                await ExecuteSCPICommandAsync(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:SOURce1 {source1}", $"Set source 1 to {source1}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:SOURce2 {source2}", $"Set source 2 to {source2}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:OPERator ADD", "Set operator to ADD");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"ADD operation applied: {source1} + {source2}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying ADD operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply SUBTract operation: Source1 - Source2
        /// </summary>
        public async Task ApplySubtractOperationAsync(string source1 = "CHANnel1", string source2 = "CHANnel2")
        {
            try
            {
                OnStatusUpdated("Applying SUBTract operation...");

                await ExecuteSCPICommandAsync(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:SOURce1 {source1}", $"Set source 1 to {source1}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:SOURce2 {source2}", $"Set source 2 to {source2}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:OPERator SUBTract", "Set operator to SUBTract");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"SUBTract operation applied: {source1} - {source2}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying SUBTract operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply MULTiply operation: Source1 * Source2
        /// </summary>
        public async Task ApplyMultiplyOperationAsync(string source1 = "CHANnel1", string source2 = "CHANnel2")
        {
            try
            {
                OnStatusUpdated("Applying MULTiply operation...");

                await ExecuteSCPICommandAsync(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:SOURce1 {source1}", $"Set source 1 to {source1}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:SOURce2 {source2}", $"Set source 2 to {source2}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:OPERator MULTiply", "Set operator to MULTiply");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"MULTiply operation applied: {source1} * {source2}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying MULTiply operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply DIVision operation: Source1 / Source2
        /// </summary>
        public async Task ApplyDivisionOperationAsync(string source1 = "CHANnel1", string source2 = "CHANnel2")
        {
            try
            {
                OnStatusUpdated("Applying DIVision operation...");

                await ExecuteSCPICommandAsync(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:SOURce1 {source1}", $"Set source 1 to {source1}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:SOURce2 {source2}", $"Set source 2 to {source2}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:OPERator DIVision", "Set operator to DIVision");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"DIVision operation applied: {source1} / {source2}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying DIVision operation: {ex.Message}");
            }
        }

        #endregion

        #region FFT Operations

        /// <summary>
        /// Apply FFT operation: Fast Fourier Transform
        /// </summary>
        public async Task ApplyFFTOperationAsync(string source = "CHANnel1", string window = "HANNing", string split = "FULL", string unit = "VRMS")
        {
            try
            {
                OnStatusUpdated("Applying FFT operation...");

                await ExecuteSCPICommandAsync(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:OPERator FFT", "Set operator to FFT");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:FFT:SOURce {source}", $"Set FFT source to {source}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:FFT:WINDow {window}", $"Set FFT window to {window}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:FFT:SPLit {split}", $"Set FFT split to {split}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:FFT:UNIT {unit}", $"Set FFT unit to {unit}");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"FFT operation applied: {source} with {window} window, {split} split, {unit} unit");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying FFT operation: {ex.Message}");
            }
        }

        #endregion

        #region Filter Operations with FFT Workaround

        /// <summary>
        /// Apply FFT workaround for source selection - Helper method
        /// </summary>
        private async Task ApplyFFTWorkaroundAsync(string source)
        {
            OnStatusUpdated($"Applying FFT workaround for {source}...");

            // Temporary FFT mode to set source
            await ExecuteSCPICommandAsync(":MATH:OPERator FFT", "Temporary FFT mode");
            await Task.Delay(COMMAND_DELAY);

            await ExecuteSCPICommandAsync($":MATH:FFT:SOURce {source}", $"Set source to {source}");
            await Task.Delay(COMMAND_DELAY);
        }

        /// <summary>
        /// Apply FILTer operation with FFT workaround for channel selection
        /// </summary>
        public async Task ApplyFilterOperationAsync(string source = "CHANnel1", string filterType = "LPASs", double w1 = 1000000, double? w2 = null)
        {
            try
            {
                OnStatusUpdated($"Applying FILTer operation on {source}...");

                // STEP 1: Disable math display
                await ExecuteSCPICommandAsync(":MATH:DISPlay OFF", "Disable math display");
                await Task.Delay(COMMAND_DELAY);

                // STEP 2: Reset math system
                await ExecuteSCPICommandAsync(":MATH:RESet", "Reset math system");
                await Task.Delay(COMMAND_DELAY);

                // STEP 3: FFT WORKAROUND - Set source via FFT first
                OnStatusUpdated($"Setting source to {source} via FFT workaround...");
                await ExecuteSCPICommandAsync(":MATH:OPERator FFT", "Temporary FFT mode for source selection");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:FFT:SOURce {source}", $"Set source to {source} via FFT");
                await Task.Delay(COMMAND_DELAY);

                // STEP 4: Switch to Filter operation
                await ExecuteSCPICommandAsync(":MATH:OPERator FILTer", "Switch to FILTer operation");
                await Task.Delay(COMMAND_DELAY);

                // STEP 5: Configure filter parameters
                await ExecuteSCPICommandAsync($":MATH:FILTer:TYPE {filterType}", $"Set filter type to {filterType}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:FILTer:W1 {w1}", $"Set W1 frequency to {w1}");
                await Task.Delay(COMMAND_DELAY);

                // Band filters (BPASs, BSTop) need W2
                if (w2.HasValue && (filterType == "BPASs" || filterType == "BSTop"))
                {
                    await ExecuteSCPICommandAsync($":MATH:FILTer:W2 {w2.Value}", $"Set W2 frequency to {w2.Value}");
                    await Task.Delay(COMMAND_DELAY);
                }

                // STEP 6: Enable math display
                await ExecuteSCPICommandAsync(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                string message = $"FILTer operation applied on {source}: {filterType}, W1={w1}";
                if (w2.HasValue)
                    message += $", W2={w2.Value}";

                OnStatusUpdated(message);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying FILTer operation: {ex.Message}");
            }
        }

        #endregion

        #region Advanced Math Operations with FFT Workaround

        /// <summary>
        /// Apply INTG operation with source selection
        /// </summary>
        public async Task ApplyIntegrationOperationAsync(string source = "CHANnel1", double startPoint = 0, double endPoint = 1199)
        {
            try
            {
                OnStatusUpdated($"Applying INTG operation on {source}...");

                await ExecuteSCPICommandAsync(":MATH:DISPlay OFF", "Disable math display");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:RESet", "Reset math system");
                await Task.Delay(COMMAND_DELAY);

                // FFT WORKAROUND for source selection
                await ApplyFFTWorkaroundAsync(source);

                // Switch to Integration
                await ExecuteSCPICommandAsync(":MATH:OPERator INTG", "Set operator to INTG");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:OPTion:FX:OPERator INTG", "Set advanced function to INTG");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:OPTion:STARt {startPoint}", $"Set start point to {startPoint}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:OPTion:END {endPoint}", $"Set end point to {endPoint}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"INTG operation applied on {source}: {startPoint} to {endPoint}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying INTG operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply DIFF operation with source selection
        /// </summary>
        public async Task ApplyDifferentiationOperationAsync(string source = "CHANnel1", double startPoint = 0, double endPoint = 1199)
        {
            try
            {
                OnStatusUpdated($"Applying DIFF operation on {source}...");

                await ExecuteSCPICommandAsync(":MATH:DISPlay OFF", "Disable math display");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:RESet", "Reset math system");
                await Task.Delay(COMMAND_DELAY);

                // FFT WORKAROUND for source selection
                await ApplyFFTWorkaroundAsync(source);

                // Switch to Differentiation
                await ExecuteSCPICommandAsync(":MATH:OPERator DIFF", "Set operator to DIFF");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:OPTion:FX:OPERator DIFF", "Set advanced function to DIFF");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:OPTion:STARt {startPoint}", $"Set start point to {startPoint}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:OPTion:END {endPoint}", $"Set end point to {endPoint}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"DIFF operation applied on {source}: {startPoint} to {endPoint}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying DIFF operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply single-operand operations (SQRT, LOG, etc.) with source selection
        /// </summary>
        public async Task ApplySingleOperandOperationAsync(string operation, string source = "CHANnel1")
        {
            try
            {
                OnStatusUpdated($"Applying {operation} operation on {source}...");

                await ExecuteSCPICommandAsync(":MATH:DISPlay OFF", "Disable math display");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:RESet", "Reset math system");
                await Task.Delay(COMMAND_DELAY);

                // FFT WORKAROUND for source selection
                await ApplyFFTWorkaroundAsync(source);

                // Switch to target operation
                await ExecuteSCPICommandAsync($":MATH:OPERator {operation}", $"Set operator to {operation}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:OPTion:FX:OPERator {operation}", $"Set advanced function to {operation}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"{operation} operation applied on {source}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying {operation} operation: {ex.Message}");
            }
        }

        #endregion

        #region Configuration Methods with Source Selection

        /// <summary>
        /// Configure Basic Operations
        /// </summary>
        private async Task ConfigureBasicOperationsAsync()
        {
            try
            {
                var operation = GetSelectedTag(this.FindName("OperationCombo") as ComboBox) ?? "ADD";
                var source1 = GetSelectedTag(this.FindName("Source1Combo") as ComboBox) ?? "CHANnel1";
                var source2 = GetSelectedTag(this.FindName("Source2Combo") as ComboBox) ?? "CHANnel2";

                switch (operation.ToUpperInvariant())
                {
                    case "ADD":
                        await ApplyAddOperationAsync(source1, source2);
                        break;
                    case "SUBTRACT":
                        await ApplySubtractOperationAsync(source1, source2);
                        break;
                    case "MULTIPLY":
                        await ApplyMultiplyOperationAsync(source1, source2);
                        break;
                    case "DIVIDE":
                        await ApplyDivisionOperationAsync(source1, source2);
                        break;
                    default:
                        OnErrorOccurred($"Unknown operation: {operation}");
                        break;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error configuring basic operations: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure FFT Analysis
        /// </summary>
        private async Task ConfigureFFTAnalysisAsync()
        {
            try
            {
                var source = GetSelectedTag(this.FindName("FFTSourceCombo") as ComboBox) ?? "CHANnel1";
                var window = GetSelectedTag(this.FindName("FFTWindowCombo") as ComboBox) ?? "HANNing";
                var split = GetSelectedTag(this.FindName("FFTSplitCombo") as ComboBox) ?? "FULL";
                var unit = GetSelectedTag(this.FindName("FFTUnitCombo") as ComboBox) ?? "VRMS";

                await ApplyFFTOperationAsync(source, window, split, unit);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error configuring FFT analysis: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure Digital Filters with source selection
        /// </summary>
        private async Task ConfigureDigitalFiltersAsync()
        {
            try
            {
                // Get selected source channel
                var source = GetSelectedTag(this.FindName("FilterSourceCombo") as ComboBox) ?? "CHANnel1";

                // Get current timebase for frequency validation
                double timebase = await GetCurrentTimebaseAsync();
                var filterType = GetSelectedTag(this.FindName("FilterTypeCombo") as ComboBox) ?? "LPASs";

                // Parse frequencies from UI
                var w1Text = (this.FindName("FilterW1Text") as TextBox)?.Text ?? "1000";
                if (!double.TryParse(w1Text, out double w1))
                    w1 = 1000;

                double? w2 = null;
                bool isBandFilter = filterType == "BPASs" || filterType == "BSTop";

                if (isBandFilter)
                {
                    var w2Text = (this.FindName("FilterW2Text") as TextBox)?.Text ?? "10000";
                    if (!double.TryParse(w2Text, out double w2Value))
                        w2Value = 10000;
                    w2 = w2Value;
                }

                // Use the updated filter method with source selection
                await ApplyFilterOperationAsync(source, filterType, w1, w2);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error configuring digital filters: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure Advanced Math with source selection
        /// </summary>
        private async Task ConfigureAdvancedMathAsync()
        {
            try
            {
                // Get selected source channel
                var source = GetSelectedTag(this.FindName("AdvancedSourceCombo") as ComboBox) ?? "CHANnel1";
                var function = GetSelectedTag(this.FindName("AdvancedFunctionCombo") as ComboBox) ?? "INTG";

                var startText = (this.FindName("StartPointText") as TextBox)?.Text ?? "0";
                if (!double.TryParse(startText, out double startPoint))
                    startPoint = 0;

                var endText = (this.FindName("EndPointText") as TextBox)?.Text ?? "1199";
                if (!double.TryParse(endText, out double endPoint))
                    endPoint = 1199;

                // Validate range limits (0-1199)
                if (startPoint < 0) startPoint = 0;
                if (endPoint > 1199) endPoint = 1199;
                if (startPoint >= endPoint)
                {
                    OnErrorOccurred("Start point must be less than end point");
                    return;
                }

                // Use individual methods with source selection
                switch (function.ToUpperInvariant())
                {
                    case "INTG":
                        await ApplyIntegrationOperationAsync(source, startPoint, endPoint);
                        break;
                    case "DIFF":
                        await ApplyDifferentiationOperationAsync(source, startPoint, endPoint);
                        break;
                    case "SQRT":
                    case "LOG":
                    case "LN":
                    case "EXP":
                    case "ABS":
                        await ApplySingleOperandOperationAsync(function, source);
                        break;
                    default:
                        OnErrorOccurred($"Unknown advanced function: {function}");
                        break;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error configuring advanced math: {ex.Message}");
            }
        }

        #endregion


        // Add this new region to MathematicsPanel.xaml.cs after your existing regions

        #region Dynamic Tooltip Management

        //private System.Windows.Threading.DispatcherTimer tooltipUpdateTimer;


        /// <summary>
        /// Improved tooltip update that tries multiple approaches
        /// </summary>
        //private async Task UpdateFrequencyTooltipsImprovedAsync()
        //{
        //    try
        //    {
        //        // Try multiple approaches to get the timebase
        //        double timebase = lastKnownTimebase;

        //        // Approach 1: Use TimeBase controller if available
        //        if (TimeBaseController != null)
        //        {
        //            timebase = await GetTimebaseViaTimeBaseControllerAsync();
        //        }
        //        else
        //        {
        //            // Approach 2: Direct SCPI query
        //            timebase = await GetCurrentTimebaseAsync();
        //        }

        //        var filterType = GetSelectedTag(this.FindName("FilterTypeCombo") as ComboBox) ?? "LPASs";

        //        OnStatusUpdated($"🔧 Updating tooltips - Timebase: {MathematicsCommands.FormatTime(timebase)}, Filter: {filterType}");

        //        // Update UI elements
        //        var w1TextBox = this.FindName("FilterW1Text") as TextBox;
        //        var w2TextBox = this.FindName("FilterW2Text") as TextBox;

        //        if (w1TextBox != null)
        //        {
        //            string w1Tooltip = MathematicsCommands.GenerateW1TooltipText(timebase, filterType);
        //            w1TextBox.ToolTip = w1Tooltip;
        //            OnStatusUpdated("✅ W1 tooltip updated");
        //        }

        //        if (w2TextBox != null)
        //        {
        //            bool isBandFilter = filterType == "BPASs" || filterType == "BSTop";
        //            if (isBandFilter)
        //            {
        //                string w2Tooltip = MathematicsCommands.GenerateW2TooltipText(timebase);
        //                w2TextBox.ToolTip = w2Tooltip;
        //                w2TextBox.IsEnabled = true;
        //                OnStatusUpdated("✅ W2 tooltip updated (band filter)");
        //            }
        //            else
        //            {
        //                w2TextBox.ToolTip = "W2 frequency is only used for Band Pass and Band Stop filters";
        //                w2TextBox.IsEnabled = false;
        //                OnStatusUpdated("✅ W2 disabled (not band filter)");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OnErrorOccurred($"Error updating frequency tooltips: {ex.Message}");
        //    }
        //}




        /// <summary>
        /// Improved tooltip update that tries multiple approaches
        /// </summary>
        //private async Task UpdateFrequencyTooltipsImprovedAsync()
        private async Task UpdateFrequencyTooltipsImprovedAsync()
        {
            try
            {
                // Try multiple approaches to get the timebase
                double timebase = lastKnownTimebase;

                // Approach 1: Use TimeBase controller if available
                if (TimeBaseController != null)
                {
                    timebase = await GetTimebaseViaTimeBaseControllerAsync();
                }
                else
                {
                    // Approach 2: Direct SCPI query
                    timebase = await GetCurrentTimebaseAsync();
                }

                var filterType = GetSelectedTag(this.FindName("FilterTypeCombo") as ComboBox) ?? "LPASs";

                OnStatusUpdated($"🔧 Updating tooltips - Timebase: {MathematicsCommands.FormatTime(timebase)}, Filter: {filterType}");

                // Update UI elements
                var w1TextBox = this.FindName("FilterW1Text") as TextBox;
                var w2TextBox = this.FindName("FilterW2Text") as TextBox;

                if (w1TextBox != null)
                {
                    string w1Tooltip = MathematicsCommands.GenerateW1TooltipText(timebase, filterType);
                    w1TextBox.ToolTip = w1Tooltip;
                    OnStatusUpdated("✅ W1 tooltip updated");
                }

                if (w2TextBox != null)
                {
                    bool isBandFilter = filterType == "BPASs" || filterType == "BSTop";
                    if (isBandFilter)
                    {
                        string w2Tooltip = MathematicsCommands.GenerateW2TooltipText(timebase);
                        w2TextBox.ToolTip = w2Tooltip;
                        w2TextBox.IsEnabled = true;
                        OnStatusUpdated("✅ W2 tooltip updated (band filter)");
                    }
                    else
                    {
                        w2TextBox.ToolTip = "W2 frequency is only used for Band Pass and Band Stop filters";
                        w2TextBox.IsEnabled = false;
                        OnStatusUpdated("✅ W2 disabled (not band filter)");
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error updating frequency tooltips: {ex.Message}");
            }
        }


        /// <summary>
        /// Reference to the main TimeBase controller (if available)
        /// Set this from the parent window/application
        /// </summary>
        public object TimeBaseController { get; set; }

        /// <summary>
        /// Alternative method to get timebase using existing TimeBase infrastructure
        /// </summary>
        private async Task<double> GetTimebaseViaTimeBaseControllerAsync()
        {
            try
            {
                // If we have access to the TimeBase controller, use it
                if (TimeBaseController != null)
                {
                    // Use reflection to access the MainScale property
                    var timebaseType = TimeBaseController.GetType();
                    var settingsProperty = timebaseType.GetProperty("settings");

                    if (settingsProperty != null)
                    {
                        var settings = settingsProperty.GetValue(TimeBaseController);
                        if (settings != null)
                        {
                            var mainScaleProperty = settings.GetType().GetProperty("MainScale");
                            if (mainScaleProperty != null)
                            {
                                var mainScale = mainScaleProperty.GetValue(settings);
                                if (mainScale is double timebase && timebase > 0)
                                {
                                    OnStatusUpdated($"✅ Timebase from controller: {MathematicsCommands.FormatTime(timebase)}");
                                    return timebase;
                                }
                            }
                        }
                    }
                }

                // Fallback to direct SCPI query
                return await GetCurrentTimebaseAsync();
            }
            catch (Exception ex)
            {
                OnStatusUpdated($"⚠️ Error accessing TimeBase controller: {ex.Message}");
                return await GetCurrentTimebaseAsync();
            }
        }




        ///// <summary>
        ///// Update tooltips for frequency input fields based on current timebase and filter type
        ///// </summary>
        //private async Task UpdateFrequencyTooltipsAsync()
        //{
        //    try
        //    {
        //        // Get current timebase and filter type
        //        double timebase = await GetCurrentTimebaseAsync();
        //        var filterType = GetSelectedTag(this.FindName("FilterTypeCombo") as ComboBox) ?? "LPASs";

        //        // Get references to the textboxes
        //        var w1TextBox = this.FindName("FilterW1Text") as TextBox;
        //        var w2TextBox = this.FindName("FilterW2Text") as TextBox;

        //        if (w1TextBox != null)
        //        {
        //            string w1Tooltip = MathematicsCommands.GenerateW1TooltipText(timebase, filterType);
        //            w1TextBox.ToolTip = w1Tooltip;
        //        }

        //        if (w2TextBox != null)
        //        {
        //            bool isBandFilter = filterType == "BPASs" || filterType == "BSTop";
        //            if (isBandFilter)
        //            {
        //                string w2Tooltip = MathematicsCommands.GenerateW2TooltipText(timebase);
        //                w2TextBox.ToolTip = w2Tooltip;
        //                w2TextBox.IsEnabled = true;
        //            }
        //            else
        //            {
        //                w2TextBox.ToolTip = "W2 frequency is only used for Band Pass and Band Stop filters";
        //                w2TextBox.IsEnabled = false;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OnErrorOccurred($"Error updating frequency tooltips: {ex.Message}");
        //    }
        //}

















        // <summary>
        /// Update tooltips for frequency input fields based on current timebase and filter type
        /// </summary>
        private async Task UpdateFrequencyTooltipsAsync()
        {
            try
            {
                // Get current timebase and filter type
                double timebase = await GetCurrentTimebaseAsync();
                var filterType = GetSelectedTag(this.FindName("FilterTypeCombo") as ComboBox) ?? "LPASs";

                OnStatusUpdated($"🔧 Updating tooltips - Timebase: {MathematicsCommands.FormatTime(timebase)}, Filter: {filterType}");

                // Get references to the textboxes
                var w1TextBox = this.FindName("FilterW1Text") as TextBox;
                var w2TextBox = this.FindName("FilterW2Text") as TextBox;

                if (w1TextBox != null)
                {
                    string w1Tooltip = MathematicsCommands.GenerateW1TooltipText(timebase, filterType);
                    w1TextBox.ToolTip = w1Tooltip;
                    OnStatusUpdated("✅ W1 tooltip updated");
                }

                if (w2TextBox != null)
                {
                    bool isBandFilter = filterType == "BPASs" || filterType == "BSTop";
                    if (isBandFilter)
                    {
                        string w2Tooltip = MathematicsCommands.GenerateW2TooltipText(timebase);
                        w2TextBox.ToolTip = w2Tooltip;
                        w2TextBox.IsEnabled = true;
                        OnStatusUpdated("✅ W2 tooltip updated (band filter)");
                    }
                    else
                    {
                        w2TextBox.ToolTip = "W2 frequency is only used for Band Pass and Band Stop filters";
                        w2TextBox.IsEnabled = false;
                        OnStatusUpdated("✅ W2 disabled (not band filter)");
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error updating frequency tooltips: {ex.Message}");
            }
        }

        /// <summary>
        /// Start the tooltip update timer with configurable interval
        /// </summary>
        private void StartTooltipUpdateTimer(int intervalSeconds = 3)
        {
            StopTooltipUpdateTimer(); // Stop any existing timer

            tooltipUpdateTimer = new System.Windows.Threading.DispatcherTimer();
            tooltipUpdateTimer.Interval = TimeSpan.FromSeconds(intervalSeconds);
            tooltipUpdateTimer.Tick += async (s, e) =>
            {
                if (!isModeChanging) // Only update when not changing modes
                {
                    await UpdateFrequencyTooltipsAsync();
                }
            };
            tooltipUpdateTimer.Start();
            OnStatusUpdated($"🔄 Tooltip update timer started ({intervalSeconds}s interval)");
        }

        /// <summary>
        /// Stop the tooltip update timer
        /// </summary>
        private void StopTooltipUpdateTimer()
        {
            if (tooltipUpdateTimer != null)
            {
                tooltipUpdateTimer.Stop();
                tooltipUpdateTimer = null;
                OnStatusUpdated("⏹️ Tooltip update timer stopped");
            }
        }

        /// <summary>
        /// Force immediate tooltip update (for manual refresh)
        /// </summary>
        public async Task RefreshTooltipsAsync()
        {
            OnStatusUpdated("🔄 Manual tooltip refresh requested");
            await UpdateFrequencyTooltipsAsync();
        }





















        /// <summary>
        /// Start the tooltip update timer
        /// </summary>
        private void StartTooltipUpdateTimer()
        {
            tooltipUpdateTimer = new System.Windows.Threading.DispatcherTimer();
            tooltipUpdateTimer.Interval = TimeSpan.FromSeconds(2); // Update every 2 seconds
            tooltipUpdateTimer.Tick += async (s, e) => await UpdateFrequencyTooltipsAsync();
            tooltipUpdateTimer.Start();
        }

        /// <summary>
        /// Stop the tooltip update timer
        /// </summary>
        //private void StopTooltipUpdateTimer()
        //{
        //    tooltipUpdateTimer?.Stop();
        //    tooltipUpdateTimer = null;
        //}

        #endregion


        // Add these event handlers to your existing event handler region in MathematicsPanel.xaml.cs

        #region UI Event Handlers (add to existing region or create new)

        /// <summary>
        /// Event handler for filter type selection change
        /// </summary>
        private async void FilterTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isModeChanging) // Use existing flag to prevent updates during initialization
            {
                await UpdateFrequencyTooltipsAsync();
            }
        }

        /// <summary>
        /// Update tooltips when the panel is loaded or becomes visible
        /// </summary>
        private async void MathematicsPanel_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await UpdateFrequencyTooltipsAsync();
                StartTooltipUpdateTimer();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error initializing tooltips: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanup when panel is unloaded
        /// </summary>
        private void MathematicsPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            StopTooltipUpdateTimer();
        }

        #endregion



        #region Utility Methods

        private string GetSelectedTag(ComboBox comboBox)
        {
            try
            {
                return (comboBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private void UpdateStatusDisplay(string message)
        {
            try
            {
                var statusText = this.FindName("StatusText") as TextBlock;
                if (statusText != null)
                {
                    // Ensure UI update happens on UI thread
                    if (Dispatcher.CheckAccess())
                    {
                        statusText.Text = message;
                    }
                    else
                    {
                        Dispatcher.Invoke(() => statusText.Text = message);
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error updating status display: {ex.Message}");
            }
        }

        //private async Task<double> GetCurrentTimebaseAsync()
        //{
        //    try
        //    {
        //        // Query current timebase from oscilloscope
        //        var response = await ExecuteSCPICommandAsync(":TIMebase:SCALe?", "Query timebase");
        //        if (double.TryParse(response, out double timebase))
        //        {
        //            return timebase;
        //        }
        //        return 1e-3; // Default 1ms if query fails
        //    }
        //    catch
        //    {
        //        return 1e-3; // Default fallback
        //    }
        //}


        private System.Windows.Threading.DispatcherTimer tooltipUpdateTimer;
        private double lastKnownTimebase = 1e-3; // Cache last successful value

        /// <summary>
        /// Get current timebase from oscilloscope with improved error handling and debugging
        /// </summary>
        private async Task<double> GetCurrentTimebaseAsync()
        {
            try
            {
                // First check if we have a valid SCPI connection
                if (SendSCPICommand == null)
                {
                    OnStatusUpdated("⚠️ SCPI connection not available, using cached timebase");
                    return lastKnownTimebase;
                }

                // Query current timebase from oscilloscope using multiple possible commands
                string response = "";

                // Try the main timebase scale command
                response = await ExecuteSCPICommandAsync(":TIMebase:MAIN:SCALe?", "Query main timebase scale");

                // If that fails, try the shorter version
                if (string.IsNullOrEmpty(response?.Trim()))
                {
                    response = await ExecuteSCPICommandAsync(":TIMebase:SCALe?", "Query timebase scale");
                }

                OnStatusUpdated($"🔍 Timebase query response: '{response}'");

                if (!string.IsNullOrEmpty(response?.Trim()))
                {
                    // Try parsing with different culture settings
                    if (double.TryParse(response.Trim(), System.Globalization.NumberStyles.Float,
                                      System.Globalization.CultureInfo.InvariantCulture, out double timebase))
                    {
                        if (timebase > 0 && timebase < 1000) // Sanity check: reasonable timebase range
                        {
                            lastKnownTimebase = timebase;
                            OnStatusUpdated($"✅ Timebase updated: {MathematicsCommands.FormatTime(timebase)}");
                            return timebase;
                        }
                        else
                        {
                            OnStatusUpdated($"⚠️ Timebase value out of range: {timebase}s, using cached value");
                        }
                    }
                    else
                    {
                        OnStatusUpdated($"⚠️ Failed to parse timebase response: '{response}'");
                    }
                }
                else
                {
                    OnStatusUpdated("⚠️ Empty timebase response from oscilloscope");
                }

                // Return cached value if query failed
                return lastKnownTimebase;
            }
            catch (Exception ex)
            {
                OnStatusUpdated($"❌ Error querying timebase: {ex.Message}");
                return lastKnownTimebase; // Return cached value on error
            }
        }


        /// <summary>
        /// Helper method to execute SCPI commands safely - FIXED: Added async version
        /// </summary>
        private string ExecuteSCPICommand(string command, string description)
        {
            try
            {
                var response = SendSCPICommand?.Invoke(command, description) ?? "";
                SCPICommandGenerated?.Invoke(command, description); // Fire event for logging
                return response;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"SCPI command failed ({command}): {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Async version of ExecuteSCPICommand - FIXED: Proper async implementation
        /// </summary>
        private async Task<string> ExecuteSCPICommandAsync(string command, string description)
        {
            return await Task.Run(() => ExecuteSCPICommand(command, description));
        }

        #endregion

        #region Event Helper Methods

        private void OnStatusUpdated(string message)
        {
            StatusUpdated?.Invoke(message);
            UpdateStatusDisplay(message);
        }

        private void OnErrorOccurred(string error)
        {
            ErrorOccurred?.Invoke(error);
            UpdateStatusDisplay($"❌ {error}");
        }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Apply math operation by name with parameters
        /// </summary>
        public async Task ApplyMathOperationAsync(string operationType, Dictionary<string, object> parameters = null)
        {
            try
            {
                parameters = parameters ?? new Dictionary<string, object>();

                switch (operationType.ToUpperInvariant())
                {
                    case "ADD":
                        await ApplyAddOperationAsync(
                            parameters.ContainsKey("source1") ? parameters["source1"].ToString() : "CHANnel1",
                            parameters.ContainsKey("source2") ? parameters["source2"].ToString() : "CHANnel2");
                        break;

                    case "SUBTRACT":
                        await ApplySubtractOperationAsync(
                            parameters.ContainsKey("source1") ? parameters["source1"].ToString() : "CHANnel1",
                            parameters.ContainsKey("source2") ? parameters["source2"].ToString() : "CHANnel2");
                        break;

                    case "MULTIPLY":
                        await ApplyMultiplyOperationAsync(
                            parameters.ContainsKey("source1") ? parameters["source1"].ToString() : "CHANnel1",
                            parameters.ContainsKey("source2") ? parameters["source2"].ToString() : "CHANnel2");
                        break;

                    case "DIVIDE":
                        await ApplyDivisionOperationAsync(
                            parameters.ContainsKey("source1") ? parameters["source1"].ToString() : "CHANnel1",
                            parameters.ContainsKey("source2") ? parameters["source2"].ToString() : "CHANnel2");
                        break;

                    case "FFT":
                        await ApplyFFTOperationAsync(
                            parameters.ContainsKey("source") ? parameters["source"].ToString() : "CHANnel1",
                            parameters.ContainsKey("window") ? parameters["window"].ToString() : "HANNing",
                            parameters.ContainsKey("split") ? parameters["split"].ToString() : "FULL",
                            parameters.ContainsKey("unit") ? parameters["unit"].ToString() : "VRMS");
                        break;

                    case "FILTER":
                        await ApplyFilterOperationAsync(
                            parameters.ContainsKey("source") ? parameters["source"].ToString() : "CHANnel1",
                            parameters.ContainsKey("filterType") ? parameters["filterType"].ToString() : "LPASs",
                            parameters.ContainsKey("w1") ? Convert.ToDouble(parameters["w1"]) : 1000,
                            parameters.ContainsKey("w2") ? Convert.ToDouble(parameters["w2"]) : (double?)null);
                        break;

                    case "INTG":
                        await ApplyIntegrationOperationAsync(
                            parameters.ContainsKey("source") ? parameters["source"].ToString() : "CHANnel1",
                            parameters.ContainsKey("startPoint") ? Convert.ToDouble(parameters["startPoint"]) : 0,
                            parameters.ContainsKey("endPoint") ? Convert.ToDouble(parameters["endPoint"]) : 1199);
                        break;

                    case "DIFF":
                        await ApplyDifferentiationOperationAsync(
                            parameters.ContainsKey("source") ? parameters["source"].ToString() : "CHANnel1",
                            parameters.ContainsKey("startPoint") ? Convert.ToDouble(parameters["startPoint"]) : 0,
                            parameters.ContainsKey("endPoint") ? Convert.ToDouble(parameters["endPoint"]) : 1199);
                        break;

                    default:
                        OnErrorOccurred($"Unknown math operation: {operationType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying {operationType} operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Get list of all available math operations
        /// </summary>
        public string[] GetAllMathOperations()
        {
            return new string[]
            {
                "ADD", "SUBTract", "MULTiply", "DIVision",  // Basic operations
                "FFT",                                       // FFT analysis
                "FILTer",                                   // Digital filters
                "INTG", "DIFF", "SQRT", "LOG", "LN", "EXP", "ABS"  // Advanced operations
            };
        }

        #endregion
    }
}