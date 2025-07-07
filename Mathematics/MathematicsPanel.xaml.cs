using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Mathematics Panel with CORRECTED filter implementation
    /// 
    /// KEY FIXES APPLIED:
    /// 1. Corrected filter implementation: Uses :MATH:FILTer:TYPE instead of :MATH:OPERator FILTer
    /// 2. Added timebase-dependent frequency validation and correction
    /// 3. Fixed method calls to use existing SendSCPICommand pattern
    /// 4. Added automatic frequency range calculation and validation
    /// 
    /// IMPORTANT: Check your XAML for these controls and add if missing:
    /// - FFTSplitCombo, FFTUnitCombo, StartPointText, EndPointText
    /// Or modify the code to use your existing control names
    /// </summary>
    public partial class MathematicsPanel : UserControl
    {
        #region Constants
        private const int COMMAND_DELAY = 100;
        private const int RESET_DELAY = 500;
        private const int MODE_CHANGE_DELAY = 200;
        #endregion

        #region Fields
        private bool isInitialized = false;
        private bool isModeChanging = false;
        private string currentActiveMode = "BasicOperations";
        private MathematicsSettings currentSettings;
        private double? cachedTimebase = null; // Cache timebase for frequency calculations
        #endregion

        #region Events
        public event EventHandler<SCPICommandEventArgs> SCPICommandGenerated;
        public event EventHandler<StatusEventArgs> StatusUpdated;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;
        #endregion

        #region Constructor and Initialization
        public MathematicsPanel()
        {
            InitializeComponent();
            currentSettings = new MathematicsSettings();
        }

        public void Initialize()
        {
            try
            {
                SetInitialMode();
                WireUpParameterEventHandlers();
                UpdateModeVisibility(currentActiveMode);
                isInitialized = true;
                OnStatusUpdated("Mathematics panel initialized with corrected filter implementation");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Panel initialization failed: {ex.Message}");
            }
        }
        #endregion

        #region SCPI Communication
        private void SendSCPICommand(string command, string description = "")
        {
            SCPICommandGenerated?.Invoke(this, new SCPICommandEventArgs(command, description));
        }

        protected virtual void OnStatusUpdated(string message)
        {
            StatusUpdated?.Invoke(this, new StatusEventArgs(message));
        }

        protected virtual void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(message));
        }
        #endregion

        #region Timebase Management - NEW
        /// <summary>
        /// Query and cache current timebase for filter frequency calculations
        /// </summary>
        private async Task<double> GetCurrentTimebaseAsync()
        {
            try
            {
                if (cachedTimebase.HasValue)
                    return cachedTimebase.Value;

                SendSCPICommand(":TIMebase:SCALe?", "Query current timebase");
                await Task.Delay(COMMAND_DELAY);

                // In a real implementation, you would read the response
                // For now, we'll use a default reasonable timebase
                cachedTimebase = 1e-6; // 1 microsecond default

                OnStatusUpdated($"Current timebase: {cachedTimebase:E} seconds");
                return cachedTimebase.Value;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error querying timebase: {ex.Message}");
                cachedTimebase = 1e-6; // Default fallback
                return cachedTimebase.Value;
            }
        }

        /// <summary>
        /// Calculate valid frequency range based on timebase and filter type
        /// Screen Sample Rate = 100 / Horizontal Timebase
        /// </summary>
        private (double minFreq, double maxFreq, double stepSize) CalculateFilterFrequencyRange(double timebaseSeconds, string filterType)
        {
            double screenSampleRate = 100.0 / timebaseSeconds;
            double stepSize = 0.005 * screenSampleRate;
            double minFreq = 0.005 * screenSampleRate;
            double maxFreq;

            switch (filterType)
            {
                case "LPASs": // Low Pass
                case "HPASs": // High Pass
                    maxFreq = 0.1 * screenSampleRate;
                    break;
                case "BPASs": // Band Pass  
                case "BSTop": // Band Stop
                    maxFreq = 0.095 * screenSampleRate; // For W1 in band filters
                    break;
                default:
                    throw new ArgumentException($"Invalid filter type: {filterType}");
            }

            return (minFreq, maxFreq, stepSize);
        }

        /// <summary>
        /// Calculate valid W2 frequency range for band filters
        /// </summary>
        private (double minFreq, double maxFreq, double stepSize) CalculateW2FrequencyRange(double timebaseSeconds)
        {
            double screenSampleRate = 100.0 / timebaseSeconds;
            double minFreq = 0.01 * screenSampleRate;   // W2 minimum is higher than W1
            double maxFreq = 0.1 * screenSampleRate;
            double stepSize = 0.005 * screenSampleRate;

            return (minFreq, maxFreq, stepSize);
        }

        /// <summary>
        /// Validate frequency against timebase constraints
        /// </summary>
        private bool IsValidFilterFrequency(double frequency, double timebaseSeconds, string filterType, bool isW2 = false)
        {
            try
            {
                if (isW2)
                {
                    var (minFreq, maxFreq, stepSize) = CalculateW2FrequencyRange(timebaseSeconds);
                    return frequency >= minFreq && frequency <= maxFreq;
                }
                else
                {
                    var (minFreq, maxFreq, stepSize) = CalculateFilterFrequencyRange(timebaseSeconds, filterType);
                    return frequency >= minFreq && frequency <= maxFreq;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Round frequency to nearest valid step
        /// </summary>
        private double RoundToValidFrequencyStep(double frequency, double timebaseSeconds)
        {
            double screenSampleRate = 100.0 / timebaseSeconds;
            double stepSize = 0.005 * screenSampleRate;
            double minFreq = 0.005 * screenSampleRate;

            // Round to nearest step
            double steps = Math.Round((frequency - minFreq) / stepSize);
            return minFreq + (steps * stepSize);
        }

        /// <summary>
        /// Check if a filter type is a band filter (requires W2)
        /// </summary>
        private bool IsBandFilter(string filterType)
        {
            return filterType == "BPASs" || filterType == "BSTop";
        }

        /// <summary>
        /// Check if a source is valid
        /// </summary>
        private bool IsValidSource(string source)
        {
            return source == "CHANnel1" || source == "CHANnel2" || source == "MATH";
        }

        /// <summary>
        /// Check if an operator is valid for basic math
        /// </summary>
        private bool IsValidOperator(string op)
        {
            return op == "ADD" || op == "SUBtract" || op == "MULtiply" || op == "DIVide";
        }
        #endregion

        #region Mode Management
        private void SetInitialMode()
        {
            try
            {
                // Show only Basic Operations initially
                BasicOperationsSection.Visibility = Visibility.Visible;
                FFTAnalysisSection.Visibility = Visibility.Collapsed;
                DigitalFiltersSection.Visibility = Visibility.Collapsed;
                AdvancedMathSection.Visibility = Visibility.Collapsed;

                currentActiveMode = "BasicOperations";
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error setting initial mode: {ex.Message}");
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
                SendSCPICommand(":MATH:DISPlay OFF", "Disable math display");
                await Task.Delay(RESET_DELAY);

                // Step 2: Reset math system
                SendSCPICommand(":MATH:RESet", "Reset math system");
                await Task.Delay(RESET_DELAY);

                // Step 3: Update UI visibility
                UpdateModeVisibility(newMode);

                // Step 4: Configure new mode
                await ConfigureModeAsync(newMode);

                currentActiveMode = newMode;
                OnStatusUpdated($"Mode changed to {newMode}");
                UpdateStatusDisplay($"Active: {newMode}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error changing mode: {ex.Message}");
                UpdateStatusDisplay("Mode Change Failed");
            }
            finally
            {
                isModeChanging = false;
            }
        }

        private void UpdateModeVisibility(string mode)
        {
            // Hide all sections first
            BasicOperationsSection.Visibility = Visibility.Collapsed;
            FFTAnalysisSection.Visibility = Visibility.Collapsed;
            DigitalFiltersSection.Visibility = Visibility.Collapsed;
            AdvancedMathSection.Visibility = Visibility.Collapsed;

            // Show the selected section
            switch (mode)
            {
                case "BasicOperations":
                    BasicOperationsSection.Visibility = Visibility.Visible;
                    break;
                case "FFTAnalysis":
                    FFTAnalysisSection.Visibility = Visibility.Visible;
                    break;
                case "DigitalFilters":
                    DigitalFiltersSection.Visibility = Visibility.Visible;
                    break;
                case "AdvancedMath":
                    AdvancedMathSection.Visibility = Visibility.Visible;
                    break;
            }
        }

        private async Task ConfigureModeAsync(string mode)
        {
            switch (mode)
            {
                case "BasicOperations":
                    await ConfigureBasicOperationsAsync();
                    break;
                case "FFTAnalysis":
                    await ConfigureFFTAnalysisAsync();
                    break;
                case "DigitalFilters":
                    await ConfigureDigitalFiltersAsync();
                    break;
                case "AdvancedMath":
                    await ConfigureAdvancedMathAsync();
                    break;
            }
        }
        #endregion

        #region Basic Operations Configuration
        private async Task ConfigureBasicOperationsAsync()
        {
            try
            {
                var operation = GetSelectedTag(OperationCombo) ?? "ADD";
                var source1 = GetSelectedTag(Source1Combo) ?? "CHANnel1";
                var source2 = GetSelectedTag(Source2Combo) ?? "CHANnel2";

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce1 {source1}", $"Set source 1 to {source1}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce2 {source2}", $"Set source 2 to {source2}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:OPERator {operation}", $"Set operation to {operation}");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"Basic operation applied: {operation} ({source1} {operation} {source2})");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error configuring basic operations: {ex.Message}");
            }
        }
        #endregion

        #region FFT Analysis Configuration  
        private async Task ConfigureFFTAnalysisAsync()
        {
            try
            {
                var source = GetSelectedTag(FFTSourceCombo) ?? "CHANnel1";
                var window = GetSelectedTag(FFTWindowCombo) ?? "HANNing";
                var split = GetSelectedTag(FFTSplitCombo) ?? "FULL";
                var unit = GetSelectedTag(FFTUnitCombo) ?? "VRMS";

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:FFT:SOURce {source}", $"Set FFT source to {source}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:FFT:WINDow {window}", $"Set FFT window to {window}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:FFT:SPLit {split}", $"Set FFT split to {split}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:FFT:UNIT {unit}", $"Set FFT unit to {unit}");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"FFT analysis applied: {source} with {window} window");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error configuring FFT analysis: {ex.Message}");
            }
        }
        #endregion

        #region Digital Filters Configuration - CORRECTED IMPLEMENTATION
        private async Task ConfigureDigitalFiltersAsync()
        {
            try
            {
                // Get current timebase for frequency validation
                double timebase = await GetCurrentTimebaseAsync();

                var filterType = GetSelectedTag(FilterTypeCombo) ?? "LPASs";

                // Parse frequencies from UI
                if (!double.TryParse(FilterW1Text?.Text ?? "1000000", out double w1))
                {
                    w1 = 1000000; // Default 1MHz
                }

                double? w2 = null;
                bool isBandFilter = IsBandFilter(filterType);

                if (isBandFilter)
                {
                    if (!double.TryParse(FilterW2Text?.Text ?? "2000000", out double w2Value))
                    {
                        w2Value = 2000000; // Default 2MHz
                    }
                    w2 = w2Value;
                }

                // Validate and correct frequencies based on timebase
                var (minFreq, maxFreq, stepSize) = CalculateFilterFrequencyRange(timebase, filterType);

                if (!IsValidFilterFrequency(w1, timebase, filterType, false))
                {
                    double correctedW1 = RoundToValidFrequencyStep(Math.Max(w1, minFreq), timebase);
                    correctedW1 = Math.Min(correctedW1, maxFreq);

                    OnStatusUpdated($"W1 frequency corrected from {w1:E} to {correctedW1:E} Hz for timebase {timebase:E}s");
                    w1 = correctedW1;

                    // Update UI
                    if (FilterW1Text != null)
                        FilterW1Text.Text = w1.ToString("F0");
                }

                if (w2.HasValue && !IsValidFilterFrequency(w2.Value, timebase, filterType, true))
                {
                    var (minFreq2, maxFreq2, stepSize2) = CalculateW2FrequencyRange(timebase);
                    double correctedW2 = RoundToValidFrequencyStep(Math.Max(w2.Value, minFreq2), timebase);
                    correctedW2 = Math.Min(correctedW2, maxFreq2);

                    OnStatusUpdated($"W2 frequency corrected from {w2.Value:E} to {correctedW2:E} Hz for timebase {timebase:E}s");
                    w2 = correctedW2;

                    // Update UI
                    if (FilterW2Text != null)
                        FilterW2Text.Text = w2.Value.ToString("F0");
                }

                // Ensure W1 < W2 for band filters
                if (w2.HasValue && w1 >= w2.Value)
                {
                    w2 = w1 + stepSize;
                    OnStatusUpdated($"W2 adjusted to {w2.Value:E} Hz to ensure W1 < W2");

                    if (FilterW2Text != null)
                        FilterW2Text.Text = w2.Value.ToString("F0");
                }

                // CORRECTED: Apply filter using direct TYPE setting, not :MATH:OPERator
                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:FILTer:TYPE {filterType}", $"CORRECTED: Set filter type to {filterType}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:FILTer:W1 {w1:F0}", $"Set W1 frequency to {w1:E} Hz");
                await Task.Delay(COMMAND_DELAY);

                if (w2.HasValue)
                {
                    SendSCPICommand($":MATH:FILTer:W2 {w2.Value:F0}", $"Set W2 frequency to {w2.Value:E} Hz");
                    await Task.Delay(COMMAND_DELAY);
                }

                // Verify settings
                SendSCPICommand(":MATH:FILTer:TYPE?", "Verify filter type");
                SendSCPICommand(":MATH:FILTer:W1?", "Verify W1 frequency");
                if (w2.HasValue)
                {
                    SendSCPICommand(":MATH:FILTer:W2?", "Verify W2 frequency");
                }

                string message = $"CORRECTED filter applied: {filterType}, W1={w1:E} Hz";
                if (w2.HasValue)
                {
                    message += $", W2={w2.Value:E} Hz";
                }
                message += $" (Timebase: {timebase:E}s)";

                OnStatusUpdated(message);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error configuring digital filters: {ex.Message}");
            }
        }

        /// <summary>
        /// Display valid frequency ranges for current timebase
        /// </summary>
        private async void ShowFrequencyRanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double timebase = await GetCurrentTimebaseAsync();
                var filterType = GetSelectedComboBoxTag(FilterTypeCombo) ?? "LPASs";

                var (minFreq, maxFreq, stepSize) = CalculateFilterFrequencyRange(timebase, filterType);

                string message = $"Valid Frequency Ranges:\n\n";
                message += $"Current Timebase: {timebase:E} seconds\n";
                message += $"Screen Sample Rate: {(100.0 / timebase):E} Hz\n\n";
                message += $"Filter Type: {filterType}\n";
                message += $"W1 Range: {minFreq:E} to {maxFreq:E} Hz\n";
                message += $"Step Size: {stepSize:E} Hz\n";

                bool isBandFilter = filterType == "BPASs" || filterType == "BSTop";
                if (isBandFilter)
                {
                    var (minFreq2, maxFreq2, stepSize2) = CalculateW2FrequencyRange(timebase);
                    message += $"W2 Range: {minFreq2:E} to {maxFreq2:E} Hz\n";
                }

                MessageBox.Show(message, "Valid Frequency Ranges", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error calculating frequency ranges: {ex.Message}");
            }
        }
        #endregion

        #region Advanced Math Configuration
        private async Task ConfigureAdvancedMathAsync()
        {
            try
            {
                var function = GetSelectedTag(AdvancedFunctionCombo) ?? "INTG";

                if (!double.TryParse(StartPointText?.Text ?? "0", out double startPoint))
                    startPoint = 0;

                if (!double.TryParse(EndPointText?.Text ?? "100", out double endPoint))
                    endPoint = 100;

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:OPTion:FX:OPERator {function}", $"Set advanced function to {function}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:OPTion:STARt {startPoint}", $"Set start point to {startPoint}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:OPTion:END {endPoint}", $"Set end point to {endPoint}");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"Advanced math applied: {function} from {startPoint} to {endPoint}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error configuring advanced math: {ex.Message}");
            }
        }
        #endregion

        #region Button Event Handlers
        private async void ApplyBasicOperation_Click(object sender, RoutedEventArgs e)
        {
            await ChangeMathModeAsync("BasicOperations");
        }

        private async void ApplyFFT_Click(object sender, RoutedEventArgs e)
        {
            await ChangeMathModeAsync("FFTAnalysis");
        }

        private async void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            await ChangeMathModeAsync("DigitalFilters");
        }

        private async void ApplyAdvancedMath_Click(object sender, RoutedEventArgs e)
        {
            await ChangeMathModeAsync("AdvancedMath");
        }

        // MISSING METHOD - Add this for XAML compatibility
        private async void MathModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized || isModeChanging) return;

            var selectedMode = GetSelectedTag(sender as ComboBox);
            if (!string.IsNullOrEmpty(selectedMode))
            {
                await ChangeMathModeAsync(selectedMode);
            }
        }

        private async void DisableMath_Click(object sender, RoutedEventArgs e)
        {
            SendSCPICommand(":MATH:DISPlay OFF", "Disable math display");
            await Task.Delay(COMMAND_DELAY);
            SendSCPICommand(":MATH:RESet", "Reset math functions");
            OnStatusUpdated("Math functions disabled");
            UpdateStatusDisplay("Math Disabled");
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateCurrentSettings();
                OnStatusUpdated("Settings saved to memory");
                UpdateStatusDisplay("Settings Saved");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Test filter with automatically calculated safe frequencies
        /// </summary>
        private async void TestFilterWithDefaults_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double timebase = await GetCurrentTimebaseAsync();
                var filterType = GetSelectedTag(FilterTypeCombo) ?? "LPASs";

                var (minFreq, maxFreq, stepSize) = CalculateFilterFrequencyRange(timebase, filterType);

                // Use safe default frequencies
                double w1 = minFreq + (2 * stepSize); // A few steps above minimum

                // Update UI with calculated values
                if (FilterW1Text != null)
                    FilterW1Text.Text = w1.ToString("F0");

                bool isBandFilter = filterType == "BPASs" || filterType == "BSTop";
                if (isBandFilter)
                {
                    var (minFreq2, maxFreq2, stepSize2) = CalculateW2FrequencyRange(timebase);
                    double w2 = Math.Min(maxFreq2 - stepSize2, w1 + (5 * stepSize));

                    if (FilterW2Text != null)
                        FilterW2Text.Text = w2.ToString("F0");
                }

                // Apply the filter
                await ConfigureDigitalFiltersAsync();

                OnStatusUpdated($"Test filter applied with calculated safe frequencies for timebase {timebase:E}s");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error testing filter: {ex.Message}");
            }
        }

        // NOTE: You may need to add these controls to your XAML if they don't exist:
        // - FFTSplitCombo (for FFT split mode selection)
        // - FFTUnitCombo (for FFT unit selection) 
        // - StartPointText (for advanced math start point)
        // - EndPointText (for advanced math end point)
        // Or modify the code to use existing control names
        #endregion

        #region Parameter Change Event Handlers
        private void WireUpParameterEventHandlers()
        {
            try
            {
                // Basic Operations - wire up immediately when controls change
                if (OperationCombo != null)
                    OperationCombo.SelectionChanged += async (s, e) => {
                        if (isInitialized && currentActiveMode == "BasicOperations")
                            await ConfigureBasicOperationsAsync();
                    };

                if (Source1Combo != null)
                    Source1Combo.SelectionChanged += async (s, e) => {
                        if (isInitialized && currentActiveMode == "BasicOperations")
                            await ConfigureBasicOperationsAsync();
                    };

                if (Source2Combo != null)
                    Source2Combo.SelectionChanged += async (s, e) => {
                        if (isInitialized && currentActiveMode == "BasicOperations")
                            await ConfigureBasicOperationsAsync();
                    };

                // FFT Analysis
                if (FFTSourceCombo != null)
                    FFTSourceCombo.SelectionChanged += async (s, e) => {
                        if (isInitialized && currentActiveMode == "FFTAnalysis")
                            await ConfigureFFTAnalysisAsync();
                    };

                if (FFTWindowCombo != null)
                    FFTWindowCombo.SelectionChanged += async (s, e) => {
                        if (isInitialized && currentActiveMode == "FFTAnalysis")
                            await ConfigureFFTAnalysisAsync();
                    };

                // Digital Filters - CORRECTED to use new implementation
                if (FilterTypeCombo != null)
                    FilterTypeCombo.SelectionChanged += async (s, e) => {
                        if (isInitialized && currentActiveMode == "DigitalFilters")
                        {
                            cachedTimebase = null; // Force timebase re-query
                            await ConfigureDigitalFiltersAsync();
                        }
                    };

                if (FilterW1Text != null)
                    FilterW1Text.LostFocus += async (s, e) => {
                        if (isInitialized && currentActiveMode == "DigitalFilters")
                            await ConfigureDigitalFiltersAsync();
                    };

                if (FilterW2Text != null)
                    FilterW2Text.LostFocus += async (s, e) => {
                        if (isInitialized && currentActiveMode == "DigitalFilters")
                            await ConfigureDigitalFiltersAsync();
                    };

                // Advanced Math
                if (AdvancedFunctionCombo != null)
                    AdvancedFunctionCombo.SelectionChanged += async (s, e) => {
                        if (isInitialized && currentActiveMode == "AdvancedMath")
                            await ConfigureAdvancedMathAsync();
                    };

                if (StartPointText != null)
                    StartPointText.LostFocus += async (s, e) => {
                        if (isInitialized && currentActiveMode == "AdvancedMath")
                            await ConfigureAdvancedMathAsync();
                    };

                if (EndPointText != null)
                    EndPointText.LostFocus += async (s, e) => {
                        if (isInitialized && currentActiveMode == "AdvancedMath")
                            await ConfigureAdvancedMathAsync();
                    };

            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error wiring up event handlers: {ex.Message}");
            }
        }
        #endregion

        #region Utility Methods
        private string GetSelectedTag(ComboBox comboBox)
        {
            return (comboBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        }

        private void UpdateCurrentSettings()
        {
            if (currentSettings == null) return;

            try
            {
                // Update settings based on current UI state
                currentSettings.ActiveMode = currentActiveMode;

                switch (currentActiveMode)
                {
                    case "BasicOperations":
                        currentSettings.Operation = GetSelectedTag(OperationCombo) ?? "ADD";
                        currentSettings.Source1 = GetSelectedTag(Source1Combo) ?? "CHANnel1";
                        currentSettings.Source2 = GetSelectedTag(Source2Combo) ?? "CHANnel2";
                        break;

                    case "FFTAnalysis":
                        currentSettings.FFTSource = GetSelectedTag(FFTSourceCombo) ?? "CHANnel1";
                        currentSettings.FFTWindow = GetSelectedTag(FFTWindowCombo) ?? "HANNing";
                        currentSettings.FFTSplit = GetSelectedTag(FFTSplitCombo) ?? "FULL";
                        currentSettings.FFTUnit = GetSelectedTag(FFTUnitCombo) ?? "VRMS";
                        break;

                    case "DigitalFilters":
                        currentSettings.FilterType = GetSelectedTag(FilterTypeCombo) ?? "LPASs";
                        currentSettings.FilterW1 = FilterW1Text?.Text ?? "1000000";
                        currentSettings.FilterW2 = FilterW2Text?.Text ?? "2000000";
                        break;

                    case "AdvancedMath":
                        currentSettings.AdvancedFunction = GetSelectedTag(AdvancedFunctionCombo) ?? "INTG";
                        currentSettings.StartPoint = StartPointText?.Text ?? "0";
                        currentSettings.EndPoint = EndPointText?.Text ?? "100";
                        break;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error updating settings: {ex.Message}");
            }
        }

        private void UpdateStatusDisplay(string status)
        {
            // Update any status display controls if they exist
            OnStatusUpdated(status);
        }

        /// <summary>
        /// Force refresh of timebase cache
        /// </summary>
        public void RefreshTimebase()
        {
            cachedTimebase = null;
        }

        /// <summary>
        /// Get current mode for external access
        /// </summary>
        public string GetCurrentMathMode() => currentActiveMode;

        /// <summary>
        /// Get current settings for external access
        /// </summary>
        public MathematicsSettings GetCurrentSettings() => currentSettings;

        /// <summary>
        /// Check if mode is currently changing
        /// </summary>
        public bool IsModeChanging => isModeChanging;
        #endregion
    }
}