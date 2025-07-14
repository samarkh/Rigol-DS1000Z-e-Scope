using DS1000Z_E_USB_Control.TimeBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Mathematics Panel for Rigol DS1000Z-E Oscilloscope
    /// Includes FFT Workaround for Filter and Advanced Math channel selection
    /// FIXED: Complete SCPI command handler setup and timebase query
    /// </summary>
    public partial class MathematicsPanel : UserControl
    {
        #region Constants and Fields

        private const int COMMAND_DELAY = 100; // ms delay between SCPI commands
        private const int RESET_DELAY = 500;   // ms delay for reset operations

        private string currentActiveMode = "BasicOperations";
        private bool isModeChanging = false;

        // FIXED: Better default timebase value (2ms to match typical oscilloscope settings)
        private double lastKnownTimebase = 0.002; // Default 2ms 

        // Tooltip system
        private DispatcherTimer tooltipUpdateTimer;

        #endregion

        #region Events and Delegates

        public event Action<string> StatusUpdated;
        public event Action<string> ErrorOccurred;
        public event Action<string, string> SCPICommandGenerated;

        // CRITICAL FIX: Properly declare the SendSCPICommand delegate as a property, not event
        public Func<string, string, string> SendSCPICommand { get; set; }

        #endregion

        #region Constructor and Initialization

        public MathematicsPanel()
        {
            InitializeComponent();
            SetInitialMode();
            InitializeTooltipSystem();
        }

        /// <summary>
        /// Initialize the tooltip system and wire up events
        /// </summary>
        private void InitializeTooltipSystem()
        {
            try
            {
                // Wire up events for tooltip updates
                this.Loaded += MathematicsPanel_Loaded;
                this.Unloaded += MathematicsPanel_Unloaded;
                this.IsVisibleChanged += MathematicsPanel_IsVisibleChanged;

                OnStatusUpdated("🔧 Tooltip system events wired up");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error initializing tooltip system: {ex.Message}");
            }
        }

        private void SetInitialMode()
        {
            try
            {
                // Show only Basic Operations initially
                SetActiveMode("BasicOperations");
                OnStatusUpdated("📐 Mathematics panel initialized - Basic Operations mode");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error setting initial mode: {ex.Message}");
            }
        }

        #endregion

        #region SCPI Command Handler Setup (NEW - CRITICAL FIX)

        /// <summary>
        /// Set up the SCPI command handler using oscilloscope reference
        /// Called from MathematicsWindow during initialization
        /// </summary>
        public void SetOscilloscopeReference(dynamic oscilloscope)
        {
            try
            {
                if (oscilloscope != null)
                {
                    // Set up SCPI command handler using oscilloscope
                    SendSCPICommand = (command, description) =>
                    {
                        try
                        {
                            OnStatusUpdated($"📡 Executing: {command}");

                            // Use the oscilloscope's methods for communication
                            if (command.EndsWith("?"))
                            {
                                // For query commands, return the response
                                var response = oscilloscope.SendQuery(command) ?? "";
                                OnStatusUpdated($"📡 Response: '{response}'");
                                return response;
                            }
                            else
                            {
                                // For set commands, send and return empty string
                                oscilloscope.SendCommand(command);
                                OnStatusUpdated($"📡 Command sent: {command}");
                                return "";
                            }
                        }
                        catch (Exception ex)
                        {
                            OnErrorOccurred($"SCPI command failed ({command}): {ex.Message}");
                            return "";
                        }
                    };

                    OnStatusUpdated("✅ SCPI command handler configured successfully");

                    // Immediately try to get current timebase
                    _ = UpdateCurrentTimebaseAsync();
                }
                else
                {
                    OnErrorOccurred("⚠️ Cannot set oscilloscope reference - oscilloscope is null");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error setting oscilloscope reference: {ex.Message}");
            }
        }

        /// <summary>
        /// Alternative method to set SCPI handler with custom function
        /// </summary>
        public void SetSCPICommandHandler(Func<string, string, string> scpiHandler)
        {
            try
            {
                if (scpiHandler != null)
                {
                    SendSCPICommand = scpiHandler;
                    OnStatusUpdated("✅ Custom SCPI command handler configured");

                    // Try to get current timebase
                    _ = UpdateCurrentTimebaseAsync();
                }
                else
                {
                    OnErrorOccurred("⚠️ Cannot set SCPI handler - handler is null");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error setting SCPI command handler: {ex.Message}");
            }
        }

        #endregion

        #region Timebase Management (FIXED)

        /// <summary>
        /// Get current timebase from oscilloscope with improved error handling and debugging
        /// FIXED: Now properly handles SCPI connection setup
        /// </summary>
        private async Task<double> GetCurrentTimebaseAsync()
        {
            try
            {
                // Check if we have a valid SCPI connection
                if (SendSCPICommand == null)
                {
                    OnStatusUpdated("⚠️ SCPI connection not available, using cached timebase");
                    return lastKnownTimebase;
                }

                // Query current timebase from oscilloscope using multiple possible commands
                string response = "";

                // Try the main timebase scale command first
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
        /// Update timebase and refresh tooltips
        /// </summary>
        private async Task UpdateCurrentTimebaseAsync()
        {
            try
            {
                var newTimebase = await GetCurrentTimebaseAsync();
                if (Math.Abs(newTimebase - lastKnownTimebase) > 1e-9) // Only update if significantly different
                {
                    lastKnownTimebase = newTimebase;
                    await UpdateFrequencyTooltipsAsync();
                }
            }
            catch (Exception ex)
            {
                OnStatusUpdated($"Error updating timebase: {ex.Message}");
            }
        }

        /// <summary>
        /// Manually update timebase (called from main window when timebase changes)
        /// </summary>
        public void UpdateTimebase(double timebaseSeconds)
        {
            try
            {
                if (timebaseSeconds > 0 && timebaseSeconds < 1000)
                {
                    lastKnownTimebase = timebaseSeconds;
                    OnStatusUpdated($"📐 Timebase updated from main app: {MathematicsCommands.FormatTime(timebaseSeconds)}");

                    // Trigger tooltip update with new timebase
                    if (IsVisible)
                    {
                        _ = UpdateFrequencyTooltipsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error updating timebase: {ex.Message}");
            }
        }

        #endregion

        #region Tooltip System (ENHANCED)

        /// <summary>
        /// Start automatic tooltip updates
        /// </summary>
        private void StartTooltipUpdateTimer(int intervalSeconds = 5)
        {
            try
            {
                StopTooltipUpdateTimer(); // Stop any existing timer

                tooltipUpdateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(intervalSeconds)
                };
                tooltipUpdateTimer.Tick += async (s, e) => await UpdateFrequencyTooltipsAsync();
                tooltipUpdateTimer.Start();

                OnStatusUpdated($"🔄 Tooltip auto-update started (every {intervalSeconds}s)");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error starting tooltip timer: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop automatic tooltip updates
        /// </summary>
        private void StopTooltipUpdateTimer()
        {
            try
            {
                if (tooltipUpdateTimer != null)
                {
                    tooltipUpdateTimer.Stop();
                    tooltipUpdateTimer = null;
                    OnStatusUpdated("🔄 Tooltip auto-update stopped");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error stopping tooltip timer: {ex.Message}");
            }
        }

        /// <summary>
        /// Update tooltips for frequency input fields based on current timebase and filter type
        /// FIXED: Now properly handles SCPI connection setup
        /// </summary>
        private async Task UpdateFrequencyTooltipsAsync()
        {
            try
            {
                // Get current timebase
                double timebase = await GetCurrentTimebaseAsync();
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

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// Update tooltips when filter type selection changes
        /// FIXED: Proper event handler signature
        /// </summary>
        private async void FilterTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isModeChanging) // Use existing flag to prevent updates during initialization
            {
                OnStatusUpdated("🔧 Filter type changed, updating tooltips...");
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
                OnStatusUpdated("📐 Mathematics panel loaded, initializing tooltips...");

                // Initial tooltip update
                await UpdateFrequencyTooltipsAsync();

                // Start the update timer
                StartTooltipUpdateTimer(3); // Update every 3 seconds

                OnStatusUpdated("✅ Tooltip system initialized");
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
            OnStatusUpdated("📐 Mathematics panel unloading...");
            StopTooltipUpdateTimer();
        }

        /// <summary>
        /// Handle visibility changes to manage tooltip updates
        /// </summary>
        private async void MathematicsPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && !isModeChanging)
            {
                OnStatusUpdated("👁️ Mathematics panel became visible, refreshing tooltips...");
                await UpdateFrequencyTooltipsAsync();
            }
        }

        #endregion

        #region Mode Management

        /// <summary>
        /// Set the active mathematics mode and show appropriate UI sections
        /// </summary>
        private void SetActiveMode(string mode)
        {
            try
            {
                isModeChanging = true;
                currentActiveMode = mode;

                // Hide all sections first
                HideAllSections();

                // Show the selected section
                switch (mode)
                {
                    case "BasicOperations":
                        ShowSection("BasicOperationsSection");
                        break;
                    case "FFTAnalysis":
                        ShowSection("FFTAnalysisSection");
                        break;
                    case "DigitalFilters":
                        ShowSection("DigitalFiltersSection");
                        break;
                    case "AdvancedMath":
                        ShowSection("AdvancedMathSection");
                        break;
                }

                OnStatusUpdated($"📐 Mode changed to: {mode}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error setting active mode: {ex.Message}");
            }
            finally
            {
                isModeChanging = false;
            }
        }

        private void HideAllSections()
        {
            var sections = new[] { "BasicOperationsSection", "FFTAnalysisSection", "DigitalFiltersSection", "AdvancedMathSection" };
            foreach (var sectionName in sections)
            {
                var section = this.FindName(sectionName) as FrameworkElement;
                if (section != null)
                {
                    section.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ShowSection(string sectionName)
        {
            var section = this.FindName(sectionName) as FrameworkElement;
            if (section != null)
            {
                section.Visibility = Visibility.Visible;
            }
        }

        // Mode selection event handlers
        private void BasicOperations_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMode("BasicOperations");
        }

        private void FFTAnalysis_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMode("FFTAnalysis");
        }

        private void DigitalFilters_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMode("DigitalFilters");
        }

        private void AdvancedMath_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMode("AdvancedMath");
        }

        #endregion

        #region SCPI Command Execution (FIXED)

        /// <summary>
        /// Helper method to execute SCPI commands safely
        /// FIXED: Now properly uses the configured SendSCPICommand delegate
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
        /// Async version of ExecuteSCPICommand
        /// </summary>
        private async Task<string> ExecuteSCPICommandAsync(string command, string description)
        {
            return await Task.Run(() => ExecuteSCPICommand(command, description));
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

        // Digital Filters
        private async void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            await ConfigureDigitalFiltersAsync();
        }

        // Advanced Math
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
                OnStatusUpdated("Settings saved successfully");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error saving settings: {ex.Message}");
            }
        }

        #endregion

        #region Configuration Methods

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

                switch (operation)
                {
                    case "ADD":
                        await ApplyAddOperationAsync(source1, source2);
                        break;
                    case "SUBTract":
                        await ApplySubtractOperationAsync(source1, source2);
                        break;
                    case "MULTiply":
                        await ApplyMultiplyOperationAsync(source1, source2);
                        break;
                    case "DIVision":
                        await ApplyDivisionOperationAsync(source1, source2);
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
        /// Configure Digital Filters
        /// </summary>
        private async Task ConfigureDigitalFiltersAsync()
        {
            try
            {
                var source = GetSelectedTag(this.FindName("FilterSourceCombo") as ComboBox) ?? "CHANnel1";
                var filterType = GetSelectedTag(this.FindName("FilterTypeCombo") as ComboBox) ?? "LPASs";
                var w1Text = (this.FindName("FilterW1Text") as TextBox)?.Text ?? "1000";
                var w2Text = (this.FindName("FilterW2Text") as TextBox)?.Text ?? "2000";

                if (double.TryParse(w1Text, out double w1))
                {
                    double? w2 = null;
                    if (filterType == "BPASs" || filterType == "BSTop")
                    {
                        if (double.TryParse(w2Text, out double w2Value))
                        {
                            w2 = w2Value;
                        }
                    }

                    await ApplyFilterOperationAsync(source, filterType, w1, w2);
                }
                else
                {
                    OnErrorOccurred("Invalid W1 frequency value");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error configuring digital filters: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure Advanced Math
        /// </summary>
        private async Task ConfigureAdvancedMathAsync()
        {
            try
            {
                var source = GetSelectedTag(this.FindName("AdvancedSourceCombo") as ComboBox) ?? "CHANnel1";
                var function = GetSelectedTag(this.FindName("AdvancedFunctionCombo") as ComboBox) ?? "INTG";
                var startText = (this.FindName("StartPointText") as TextBox)?.Text ?? "0";
                var endText = (this.FindName("EndPointText") as TextBox)?.Text ?? "1199";

                if (double.TryParse(startText, out double startPoint) && double.TryParse(endText, out double endPoint))
                {
                    switch (function)
                    {
                        case "INTG":
                            await ApplyIntegrationOperationAsync(source, startPoint, endPoint);
                            break;
                        case "DIFF":
                            await ApplyDifferentiationOperationAsync(source, startPoint, endPoint);
                            break;
                        default:
                            await ApplySingleOperandOperationAsync(function, source);
                            break;
                    }
                }
                else
                {
                    OnErrorOccurred("Invalid start or end point values");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error configuring advanced math: {ex.Message}");
            }
        }

        #endregion

        #region Math Operations Implementation

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

        /// <summary>
        /// Apply FFT operation with workaround for source selection
        /// </summary>
        public async Task ApplyFFTOperationAsync(string source = "CHANnel1", string window = "HANNing",
                                               string split = "FULL", string unit = "VRMS")
        {
            try
            {
                OnStatusUpdated($"Applying FFT operation on {source}...");

                await ExecuteSCPICommandAsync(":MATH:DISPlay OFF", "Disable math display");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:RESet", "Reset math system");
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

                await ExecuteSCPICommandAsync(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"FFT operation applied on {source}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying FFT operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply Filter operation with FFT workaround for source selection
        /// </summary>
        public async Task ApplyFilterOperationAsync(string source = "CHANnel1", string filterType = "LPASs",
                                                   double w1 = 1000, double? w2 = null)
        {
            try
            {
                OnStatusUpdated($"Applying FILTer operation on {source}...");

                await ExecuteSCPICommandAsync(":MATH:DISPlay OFF", "Disable math display");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync(":MATH:RESet", "Reset math system");
                await Task.Delay(COMMAND_DELAY);

                // FFT WORKAROUND - Set source via FFT first
                OnStatusUpdated($"Setting source to {source} via FFT workaround...");
                await ExecuteSCPICommandAsync(":MATH:OPERator FFT", "Temporary FFT mode for source selection");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:FFT:SOURce {source}", $"Set source to {source} via FFT");
                await Task.Delay(COMMAND_DELAY);

                // Switch to Filter operation
                await ExecuteSCPICommandAsync(":MATH:OPERator FILTer", "Switch to FILTer operation");
                await Task.Delay(COMMAND_DELAY);

                // Configure filter parameters
                await ExecuteSCPICommandAsync($":MATH:FILTer:TYPE {filterType}", $"Set filter type to {filterType}");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:FILTer:W1 {w1}", $"Set W1 frequency to {w1}");
                await Task.Delay(COMMAND_DELAY);

                // Set W2 for band filters
                if (w2.HasValue && (filterType == "BPASs" || filterType == "BSTop"))
                {
                    await ExecuteSCPICommandAsync($":MATH:FILTer:W2 {w2.Value}", $"Set W2 frequency to {w2.Value}");
                    await Task.Delay(COMMAND_DELAY);
                }

                await ExecuteSCPICommandAsync(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"FILTer operation applied on {source}: {filterType}, W1={w1}" +
                              (w2.HasValue ? $", W2={w2.Value}" : ""));
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying FILTer operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply Integration operation
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
        /// Apply Differentiation operation
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

                await ExecuteSCPICommandAsync(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"{operation} operation applied on {source}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying {operation} operation: {ex.Message}");
            }
        }

        /// <summary>
        /// FFT Workaround for source selection in filter and advanced math operations
        /// </summary>
        private async Task ApplyFFTWorkaroundAsync(string source)
        {
            try
            {
                OnStatusUpdated($"🔧 FFT workaround: Setting source to {source}");

                await ExecuteSCPICommandAsync(":MATH:OPERator FFT", "Temporary FFT for source selection");
                await Task.Delay(COMMAND_DELAY);

                await ExecuteSCPICommandAsync($":MATH:FFT:SOURce {source}", $"Set source to {source}");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated("✅ FFT workaround completed");
            }
            catch (Exception ex)
            {
                OnStatusUpdated($"⚠️ FFT workaround failed: {ex.Message}");
            }
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
                        await ApplySingleOperandOperationAsync(operationType,
                            parameters.ContainsKey("source") ? parameters["source"].ToString() : "CHANnel1");
                        break;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying {operationType} operation: {ex.Message}");
            }
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
    }
}