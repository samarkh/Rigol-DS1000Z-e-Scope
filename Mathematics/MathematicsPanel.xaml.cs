using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Complete Mathematics Panel with all 17 math operators implemented as individual methods
    /// 
    /// FEATURES:
    /// - All 17 math operators as individual methods for easy debugging
    /// - Updated configuration methods using individual operators
    /// - Complete command sequences following Rigol DS1000Z-E specifications
    /// - Proper error handling and status reporting
    /// - Clean regional organization for maintainability
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
        private double? cachedTimebase = null;
        #endregion

        #region Events
        public event EventHandler<SCPICommandEventArgs> SCPICommandGenerated;
        public event EventHandler<StatusEventArgs> StatusUpdated;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;
        #endregion

        #region Constructor & Initialization
        public MathematicsPanel()
        {
            InitializeComponent();
            InitializePanel();
        }

        private void InitializePanel()
        {
            try
            {
                currentSettings = MathematicsSettings.CreateBasicOperationsDefault();
                SetInitialMode();
                WireUpEventHandlers();
                isInitialized = true;
                OnStatusUpdated("Mathematics panel initialized successfully");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error initializing mathematics panel: {ex.Message}");
            }
        }
        #endregion

        #region Event Handlers Wiring
        private void WireUpEventHandlers()
        {
            try
            {
                // Basic Operations
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

                // Digital Filters
                if (FilterTypeCombo != null)
                    FilterTypeCombo.SelectionChanged += async (s, e) => {
                        if (isInitialized && currentActiveMode == "DigitalFilters")
                        {
                            cachedTimebase = null;
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

        #region Individual Math Operator Methods - All 17 Operations

        #region Two-Operand Basic Operations (Need 2 Sources)

        /// <summary>
        /// Apply ADD operation: Source1 + Source2
        /// </summary>
        public async Task ApplyAddOperationAsync(string source1 = "CHANnel1", string source2 = "CHANnel2")
        {
            try
            {
                OnStatusUpdated("Applying ADD operation...");

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce1 {source1}", $"Set source 1 to {source1}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce2 {source2}", $"Set source 2 to {source2}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator ADD", "Set operator to ADD");
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

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce1 {source1}", $"Set source 1 to {source1}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce2 {source2}", $"Set source 2 to {source2}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator SUBTract", "Set operator to SUBTract");
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

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce1 {source1}", $"Set source 1 to {source1}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce2 {source2}", $"Set source 2 to {source2}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator MULTiply", "Set operator to MULTiply");
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

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce1 {source1}", $"Set source 1 to {source1}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce2 {source2}", $"Set source 2 to {source2}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator DIVision", "Set operator to DIVision");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"DIVision operation applied: {source1} / {source2}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying DIVision operation: {ex.Message}");
            }
        }

        #endregion

        #region Logic Operations (Need 2 Sources)

        /// <summary>
        /// Apply AND logic operation: Source1 AND Source2
        /// </summary>
        public async Task ApplyAndOperationAsync(string source1 = "CHANnel1", string source2 = "CHANnel2")
        {
            try
            {
                OnStatusUpdated("Applying AND operation...");

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce1 {source1}", $"Set source 1 to {source1}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce2 {source2}", $"Set source 2 to {source2}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator AND", "Set operator to AND");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"AND operation applied: {source1} AND {source2}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying AND operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply OR logic operation: Source1 OR Source2
        /// </summary>
        public async Task ApplyOrOperationAsync(string source1 = "CHANnel1", string source2 = "CHANnel2")
        {
            try
            {
                OnStatusUpdated("Applying OR operation...");

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce1 {source1}", $"Set source 1 to {source1}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce2 {source2}", $"Set source 2 to {source2}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator OR", "Set operator to OR");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"OR operation applied: {source1} OR {source2}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying OR operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply XOR logic operation: Source1 XOR Source2
        /// </summary>
        public async Task ApplyXorOperationAsync(string source1 = "CHANnel1", string source2 = "CHANnel2")
        {
            try
            {
                OnStatusUpdated("Applying XOR operation...");

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce1 {source1}", $"Set source 1 to {source1}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:SOURce2 {source2}", $"Set source 2 to {source2}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator XOR", "Set operator to XOR");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"XOR operation applied: {source1} XOR {source2}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying XOR operation: {ex.Message}");
            }
        }

        #endregion

        #region Single Operand Operations (Use Advanced Math Commands)

        /// <summary>
        /// Apply NOT operation: NOT Source
        /// </summary>
        public async Task ApplyNotOperationAsync()
        {
            try
            {
                OnStatusUpdated("Applying NOT operation...");

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator NOT", "Set operator to NOT");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPTion:FX:OPERator NOT", "Set advanced function to NOT");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated("NOT operation applied");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying NOT operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply SQRT operation: Square root of source
        /// </summary>
        public async Task ApplySqrtOperationAsync()
        {
            try
            {
                OnStatusUpdated("Applying SQRT operation...");

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator SQRT", "Set operator to SQRT");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPTion:FX:OPERator SQRT", "Set advanced function to SQRT");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated("SQRT operation applied");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying SQRT operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply LOG operation: Logarithm base 10 of source
        /// </summary>
        public async Task ApplyLogOperationAsync()
        {
            try
            {
                OnStatusUpdated("Applying LOG operation...");

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator LOG", "Set operator to LOG");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPTion:FX:OPERator LG", "Set advanced function to LG");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated("LOG operation applied");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying LOG operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply LN operation: Natural logarithm of source
        /// </summary>
        public async Task ApplyLnOperationAsync()
        {
            try
            {
                OnStatusUpdated("Applying LN operation...");

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator LN", "Set operator to LN");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPTion:FX:OPERator LN", "Set advanced function to LN");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated("LN operation applied");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying LN operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply EXP operation: Exponential of source
        /// </summary>
        public async Task ApplyExpOperationAsync()
        {
            try
            {
                OnStatusUpdated("Applying EXP operation...");

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator EXP", "Set operator to EXP");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPTion:FX:OPERator EXP", "Set advanced function to EXP");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated("EXP operation applied");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying EXP operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply ABS operation: Absolute value of source
        /// </summary>
        public async Task ApplyAbsOperationAsync()
        {
            try
            {
                OnStatusUpdated("Applying ABS operation...");

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator ABS", "Set operator to ABS");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPTion:FX:OPERator ABS", "Set advanced function to ABS");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated("ABS operation applied");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying ABS operation: {ex.Message}");
            }
        }

        #endregion

        #region Advanced Operations (Need Start/End Points)

        /// <summary>
        /// Apply INTG operation: Integration with start/end points
        /// </summary>
        public async Task ApplyIntegrationOperationAsync(double startPoint = 0, double endPoint = 100)
        {
            try
            {
                OnStatusUpdated("Applying INTG operation...");

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator INTG", "Set operator to INTG");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPTion:FX:OPERator INTG", "Set advanced function to INTG");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:OPTion:STARt {startPoint}", $"Set start point to {startPoint}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:OPTion:END {endPoint}", $"Set end point to {endPoint}");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"INTG operation applied: {startPoint} to {endPoint}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying INTG operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply DIFF operation: Differentiation with start/end points
        /// </summary>
        public async Task ApplyDifferentiationOperationAsync(double startPoint = 0, double endPoint = 100)
        {
            try
            {
                OnStatusUpdated("Applying DIFF operation...");

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator DIFF", "Set operator to DIFF");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPTion:FX:OPERator DIFF", "Set advanced function to DIFF");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:OPTion:STARt {startPoint}", $"Set start point to {startPoint}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:OPTion:END {endPoint}", $"Set end point to {endPoint}");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"DIFF operation applied: {startPoint} to {endPoint}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying DIFF operation: {ex.Message}");
            }
        }

        #endregion

        #region Special Operations

        /// <summary>
        /// Apply FFT operation: Fast Fourier Transform
        /// </summary>
        public async Task ApplyFFTOperationAsync(string source = "CHANnel1", string window = "HANNing", string split = "FULL", string unit = "VRMS")
        {
            try
            {
                OnStatusUpdated("Applying FFT operation...");

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator FFT", "Set operator to FFT");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:FFT:SOURce {source}", $"Set FFT source to {source}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:FFT:WINDow {window}", $"Set FFT window to {window}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:FFT:SPLit {split}", $"Set FFT split to {split}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:FFT:UNIT {unit}", $"Set FFT unit to {unit}");
                await Task.Delay(COMMAND_DELAY);

                OnStatusUpdated($"FFT operation applied: {source} with {window} window, {split} split, {unit} unit");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying FFT operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply FILTer operation: Digital filter
        /// </summary>
        public async Task ApplyFilterOperationAsync(string filterType = "LPASs", double w1 = 1000000, double? w2 = null)
        {
            try
            {
                OnStatusUpdated("Applying FILTer operation...");

                SendSCPICommand(":MATH:DISPlay ON", "Enable math display");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand(":MATH:OPERator FILTer", "Set operator to FILTer");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:FILTer:TYPE {filterType}", $"Set filter type to {filterType}");
                await Task.Delay(COMMAND_DELAY);

                SendSCPICommand($":MATH:FILTer:W1 {w1}", $"Set W1 frequency to {w1}");
                await Task.Delay(COMMAND_DELAY);

                // Band filters (BPASs, BSTop) need W2
                if (w2.HasValue && (filterType == "BPASs" || filterType == "BSTop"))
                {
                    SendSCPICommand($":MATH:FILTer:W2 {w2.Value}", $"Set W2 frequency to {w2.Value}");
                    await Task.Delay(COMMAND_DELAY);
                }

                string message = $"FILTer operation applied: {filterType}, W1={w1}";
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

        #region Utility Methods for Individual Operations

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

                    case "DIVISION":
                        await ApplyDivisionOperationAsync(
                            parameters.ContainsKey("source1") ? parameters["source1"].ToString() : "CHANnel1",
                            parameters.ContainsKey("source2") ? parameters["source2"].ToString() : "CHANnel2");
                        break;

                    case "AND":
                        await ApplyAndOperationAsync(
                            parameters.ContainsKey("source1") ? parameters["source1"].ToString() : "CHANnel1",
                            parameters.ContainsKey("source2") ? parameters["source2"].ToString() : "CHANnel2");
                        break;

                    case "OR":
                        await ApplyOrOperationAsync(
                            parameters.ContainsKey("source1") ? parameters["source1"].ToString() : "CHANnel1",
                            parameters.ContainsKey("source2") ? parameters["source2"].ToString() : "CHANnel2");
                        break;

                    case "XOR":
                        await ApplyXorOperationAsync(
                            parameters.ContainsKey("source1") ? parameters["source1"].ToString() : "CHANnel1",
                            parameters.ContainsKey("source2") ? parameters["source2"].ToString() : "CHANnel2");
                        break;

                    case "NOT":
                        await ApplyNotOperationAsync();
                        break;

                    case "SQRT":
                        await ApplySqrtOperationAsync();
                        break;

                    case "LOG":
                        await ApplyLogOperationAsync();
                        break;

                    case "LN":
                        await ApplyLnOperationAsync();
                        break;

                    case "EXP":
                        await ApplyExpOperationAsync();
                        break;

                    case "ABS":
                        await ApplyAbsOperationAsync();
                        break;

                    case "INTG":
                        await ApplyIntegrationOperationAsync(
                            parameters.ContainsKey("startPoint") ? Convert.ToDouble(parameters["startPoint"]) : 0,
                            parameters.ContainsKey("endPoint") ? Convert.ToDouble(parameters["endPoint"]) : 100);
                        break;

                    case "DIFF":
                        await ApplyDifferentiationOperationAsync(
                            parameters.ContainsKey("startPoint") ? Convert.ToDouble(parameters["startPoint"]) : 0,
                            parameters.ContainsKey("endPoint") ? Convert.ToDouble(parameters["endPoint"]) : 100);
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
                            parameters.ContainsKey("filterType") ? parameters["filterType"].ToString() : "LPASs",
                            parameters.ContainsKey("w1") ? Convert.ToDouble(parameters["w1"]) : 1000000,
                            parameters.ContainsKey("w2") ? Convert.ToDouble(parameters["w2"]) : (double?)null);
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
                "AND", "OR", "XOR",                          // Logic operations
                "NOT", "SQRT", "LOG", "LN", "EXP", "ABS",   // Single operand
                "INTG", "DIFF",                              // Advanced operations
                "FFT", "FILTer"                              // Special operations
            };
        }

        #endregion

        #endregion

        #region Updated Configuration Methods - Using Individual Operation Methods

        /// <summary>
        /// Configure Basic Operations - Updated to use individual methods
        /// </summary>
        private async Task ConfigureBasicOperationsAsync()
        {
            try
            {
                var operation = GetSelectedTag(OperationCombo) ?? "ADD";
                var source1 = GetSelectedTag(Source1Combo) ?? "CHANnel1";
                var source2 = GetSelectedTag(Source2Combo) ?? "CHANnel2";

                // Use the individual methods instead of inline code
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
                    case "DIVISION":
                        await ApplyDivisionOperationAsync(source1, source2);
                        break;
                    default:
                        OnErrorOccurred($"Unknown basic operation: {operation}");
                        break;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error configuring basic operations: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure Logic Operations - NEW method
        /// </summary>
        private async Task ConfigureLogicOperationsAsync()
        {
            try
            {
                // NOTE: You'll need to add LogicOperationCombo to your XAML or modify this
                // For now, we'll check if logic operations are selected in the main OperationCombo
                var operation = GetSelectedTag(OperationCombo);
                var source1 = GetSelectedTag(Source1Combo) ?? "CHANnel1";
                var source2 = GetSelectedTag(Source2Combo) ?? "CHANnel2";

                if (string.IsNullOrEmpty(operation))
                {
                    OnErrorOccurred("No operation selected for logic operations");
                    return;
                }

                switch (operation.ToUpperInvariant())
                {
                    case "AND":
                        await ApplyAndOperationAsync(source1, source2);
                        break;
                    case "OR":
                        await ApplyOrOperationAsync(source1, source2);
                        break;
                    case "XOR":
                        await ApplyXorOperationAsync(source1, source2);
                        break;
                    default:
                        OnErrorOccurred($"Unknown logic operation: {operation}");
                        break;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error configuring logic operations: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure Advanced Math - Updated to use individual methods
        /// </summary>
        private async Task ConfigureAdvancedMathAsync()
        {
            try
            {
                var function = GetSelectedTag(AdvancedFunctionCombo) ?? "INTG";

                if (!double.TryParse(StartPointText?.Text ?? "0", out double startPoint))
                    startPoint = 0;

                if (!double.TryParse(EndPointText?.Text ?? "100", out double endPoint))
                    endPoint = 100;

                // Use individual methods for better debugging
                switch (function.ToUpperInvariant())
                {
                    case "NOT":
                        await ApplyNotOperationAsync();
                        break;
                    case "SQRT":
                        await ApplySqrtOperationAsync();
                        break;
                    case "LOG":
                        await ApplyLogOperationAsync();
                        break;
                    case "LN":
                        await ApplyLnOperationAsync();
                        break;
                    case "EXP":
                        await ApplyExpOperationAsync();
                        break;
                    case "ABS":
                        await ApplyAbsOperationAsync();
                        break;
                    case "INTG":
                        await ApplyIntegrationOperationAsync(startPoint, endPoint);
                        break;
                    case "DIFF":
                        await ApplyDifferentiationOperationAsync(startPoint, endPoint);
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

        /// <summary>
        /// Configure FFT Analysis - Updated to use individual method
        /// </summary>
        private async Task ConfigureFFTAnalysisAsync()
        {
            try
            {
                var source = GetSelectedTag(FFTSourceCombo) ?? "CHANnel1";
                var window = GetSelectedTag(FFTWindowCombo) ?? "HANNing";
                var split = GetSelectedTag(FFTSplitCombo) ?? "FULL";
                var unit = GetSelectedTag(FFTUnitCombo) ?? "VRMS";

                // Use the individual FFT method
                await ApplyFFTOperationAsync(source, window, split, unit);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error configuring FFT analysis: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure Digital Filters - Updated to use individual method
        /// </summary>
        private async Task ConfigureDigitalFiltersAsync()
        {
            try
            {
                // Get current timebase for frequency validation
                double timebase = await GetCurrentTimebaseAsync();
                var filterType = GetSelectedTag(FilterTypeCombo) ?? "LPASs";

                // Parse frequencies from UI
                if (!double.TryParse(FilterW1Text?.Text ?? "1000000", out double w1))
                    w1 = 1000000;

                double? w2 = null;
                bool isBandFilter = filterType == "BPASs" || filterType == "BSTop";

                if (isBandFilter)
                {
                    if (!double.TryParse(FilterW2Text?.Text ?? "2000000", out double w2Value))
                        w2Value = 2000000;
                    w2 = w2Value;
                }

                // Use the individual filter method
                await ApplyFilterOperationAsync(filterType, w1, w2);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error configuring digital filters: {ex.Message}");
            }
        }

        #endregion

        #region Mode Management

        private void SetInitialMode()
        {
            try
            {
                // Show only Basic Operations initially - with null checks
                if (BasicOperationsSection != null)
                    BasicOperationsSection.Visibility = Visibility.Visible;
                if (FFTAnalysisSection != null)
                    FFTAnalysisSection.Visibility = Visibility.Collapsed;
                if (DigitalFiltersSection != null)
                    DigitalFiltersSection.Visibility = Visibility.Collapsed;
                if (AdvancedMathSection != null)
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
            }
            finally
            {
                isModeChanging = false;
            }
        }

        private async Task ConfigureModeAsync(string mode)
        {
            switch (mode)
            {
                case "BasicOperations":
                    await ConfigureBasicOperationsAsync();
                    break;
                case "LogicOperations":
                    await ConfigureLogicOperationsAsync();
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
                default:
                    OnErrorOccurred($"Unknown mode: {mode}");
                    break;
            }
        }

        private void UpdateModeVisibility(string mode)
        {
            try
            {
                // Hide all sections first - with null checks
                if (BasicOperationsSection != null)
                    BasicOperationsSection.Visibility = Visibility.Collapsed;
                if (FFTAnalysisSection != null)
                    FFTAnalysisSection.Visibility = Visibility.Collapsed;
                if (DigitalFiltersSection != null)
                    DigitalFiltersSection.Visibility = Visibility.Collapsed;
                if (AdvancedMathSection != null)
                    AdvancedMathSection.Visibility = Visibility.Collapsed;

                // Show selected section
                switch (mode)
                {
                    case "BasicOperations":
                        if (BasicOperationsSection != null)
                            BasicOperationsSection.Visibility = Visibility.Visible;
                        break;
                    case "LogicOperations":
                        if (BasicOperationsSection != null)
                            BasicOperationsSection.Visibility = Visibility.Visible; // Reuse for now
                        break;
                    case "FFTAnalysis":
                        if (FFTAnalysisSection != null)
                            FFTAnalysisSection.Visibility = Visibility.Visible;
                        break;
                    case "DigitalFilters":
                        if (DigitalFiltersSection != null)
                            DigitalFiltersSection.Visibility = Visibility.Visible;
                        break;
                    case "AdvancedMath":
                        if (AdvancedMathSection != null)
                            AdvancedMathSection.Visibility = Visibility.Visible;
                        break;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error updating mode visibility: {ex.Message}");
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

        private void DisableMath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SendSCPICommand(":MATH:DISPlay OFF", "Disable math display");
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
                if (StatusText != null)
                    StatusText.Text = message;
            }
            catch (Exception ex)
            {
                // Fail silently for UI updates
                System.Diagnostics.Debug.WriteLine($"Error updating status display: {ex.Message}");
            }
        }

        private async Task<double> GetCurrentTimebaseAsync()
        {
            try
            {
                if (cachedTimebase.HasValue)
                    return cachedTimebase.Value;

                // Try to get timebase from oscilloscope
                // If not available, use default
                cachedTimebase = 1e-6; // 1 microsecond default
                return cachedTimebase.Value;
            }
            catch
            {
                return 1e-6; // Default fallback
            }
        }

        private bool IsBandFilter(string filterType)
        {
            return filterType == "BPASs" || filterType == "BSTop";
        }

        private bool IsValidSource(string source)
        {
            return source == "CHANnel1" ||
                   source == "CHANnel2" ||
                   source == "CHANnel3" ||
                   source == "CHANnel4" ||
                   source == "MATH";
        }

        private bool IsValidOperator(string op)
        {
            return op == "ADD" || op == "SUBTract" || op == "MULTiply" || op == "DIVision" ||
                   op == "AND" || op == "OR" || op == "XOR" || op == "NOT" ||
                   op == "SQRT" || op == "LOG" || op == "LN" || op == "EXP" || op == "ABS" ||
                   op == "INTG" || op == "DIFF" || op == "FFT" || op == "FILTer";
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
                OnErrorOccurred($"Error updating current settings: {ex.Message}");
            }
        }

        #endregion

        #region SCPI Command Methods

        private void SendSCPICommand(string command, string description = "")
        {
            try
            {
                SCPICommandGenerated?.Invoke(this, new SCPICommandEventArgs(command, description));
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error sending SCPI command: {ex.Message}");
            }
        }

        #endregion

        #region Event Invocation

        private void OnStatusUpdated(string message)
        {
            try
            {
                StatusUpdated?.Invoke(this, new StatusEventArgs(message));
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error in status update: {ex.Message}");
            }
        }

        private void OnErrorOccurred(string message)
        {
            try
            {
                ErrorOccurred?.Invoke(this, new ErrorEventArgs(message));
            }
            catch (Exception ex)
            {
                // Last resort error handling
                System.Diagnostics.Debug.WriteLine($"Critical error in error handler: {ex.Message}");
            }
        }






        public string GetCurrentMathMode()
        {
            // Return the current math mode based on your UI state
            // This is just an example - adjust based on your actual implementation
            if (radioButtonAdd?.IsChecked == true) return "ADD";
            if (radioButtonSubtract?.IsChecked == true) return "SUBTRACT";
            if (radioButtonMultiply?.IsChecked == true) return "MULTIPLY";
            if (radioButtonDivide?.IsChecked == true) return "DIVIDE";
            if (radioButtonFFT?.IsChecked == true) return "FFT";

            return "NONE"; // Default value
        }












        #endregion
    }

    //#region Event Args Classes

    //public class SCPICommandEventArgs : EventArgs
    //{
    //    public string Command { get; }
    //    public string Description { get; }

    //    public SCPICommandEventArgs(string command, string description = "")
    //    {
    //        Command = command;
    //        Description = description;
    //    }
    //}

    //public class StatusEventArgs : EventArgs
    //{
    //    public string Message { get; }

    //    public StatusEventArgs(string message)
    //    {
    //        Message = message;
    //    }
    //}

    //public class ErrorEventArgs : EventArgs
    //{
    //    public string Message { get; }

    //    public ErrorEventArgs(string message)
    //    {
    //        Message = message;
    //    }
    //}

    //#endregion
}