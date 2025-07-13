using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Settings class for mathematics operations with corrected filter implementation
    /// </summary>
    public class MathematicsSettings : INotifyPropertyChanged
    {
        #region Private Fields

        private string _configurationName = "Default Math Configuration";
        private string _activeMode = "None";

        // Basic Math Properties
        private string _source1 = "CHANnel1";
        private string _source2 = "CHANnel2";
        private string _operation = "ADD";

        // FFT Analysis Properties
        private string _fftSource = "CHANnel1";
        private string _fftWindow = "HANNing";
        private string _fftSplit = "FULL";
        private string _fftUnit = "VRMS";

        // Digital Filter Properties - CORRECTED
        private string _filterType = "LPASs";
        private string _filterW1 = "1000000";
        private string _filterW2 = "2000000";
        private string _currentTimebase = "";
        private string _calculatedMinFreq = "";
        private string _calculatedMaxFreq = "";
        private string _calculatedStepSize = "";

        // Advanced Math Properties
        private string _advancedFunction = "INTG";
        private string _startPoint = "0";
        private string _endPoint = "100";

        // Display Properties
        private bool _mathDisplayEnabled = true;
        private bool _invertWaveform = false;
        private string _scale = "1.0";
        private string _offset = "0.0";

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName ?? typeof(T).Name);
            return true;
        }

        #endregion

        #region Configuration Properties

        /// <summary>
        /// Configuration name for saving/loading
        /// </summary>
        [JsonPropertyName("configurationName")]
        public string ConfigurationName
        {
            get => _configurationName;
            set => SetProperty(ref _configurationName, value);
        }

        /// <summary>
        /// Currently active math mode
        /// </summary>
        [JsonPropertyName("activeMode")]
        public string ActiveMode
        {
            get => _activeMode;
            set => SetProperty(ref _activeMode, value);
        }

        #endregion

        #region Basic Math Properties

        /// <summary>
        /// Source 1 for basic math operations
        /// </summary>
        [JsonPropertyName("source1")]
        public string Source1
        {
            get => _source1;
            set => SetProperty(ref _source1, value);
        }

        /// <summary>
        /// Source 2 for basic math operations
        /// </summary>
        [JsonPropertyName("source2")]
        public string Source2
        {
            get => _source2;
            set => SetProperty(ref _source2, value);
        }

        /// <summary>
        /// Math operation (ADD, SUBtract, MULtiply, DIVide)
        /// </summary>
        [JsonPropertyName("operation")]
        public string Operation
        {
            get => _operation;
            set => SetProperty(ref _operation, value);
        }

        #endregion

        #region FFT Analysis Properties

        /// <summary>
        /// Source channel for FFT analysis
        /// </summary>
        [JsonPropertyName("fftSource")]
        public string FFTSource
        {
            get => _fftSource;
            set => SetProperty(ref _fftSource, value);
        }

        /// <summary>
        /// FFT windowing function
        /// </summary>
        [JsonPropertyName("fftWindow")]
        public string FFTWindow
        {
            get => _fftWindow;
            set => SetProperty(ref _fftWindow, value);
        }

        /// <summary>
        /// FFT display mode (FULL or CENTer)
        /// </summary>
        [JsonPropertyName("fftSplit")]
        public string FFTSplit
        {
            get => _fftSplit;
            set => SetProperty(ref _fftSplit, value);
        }

        /// <summary>
        /// FFT measurement unit (VRMS or dB)
        /// </summary>
        [JsonPropertyName("fftUnit")]
        public string FFTUnit
        {
            get => _fftUnit;
            set => SetProperty(ref _fftUnit, value);
        }

        #endregion

        #region Digital Filter Properties - CORRECTED

        ///// <summary>
        ///// Digital filter type (LPASs, HPASs, BPASs, BSTop)
        ///// </summary>
        //[JsonPropertyName("filterType")]
        //public string FilterType
        //{
        //    get => _filterType;
        //    set => SetProperty(ref _filterType, value);
        //}

        ///// <summary>
        ///// Filter cutoff frequency 1 (W1) - Timebase dependent
        ///// </summary>
        //[JsonPropertyName("filterW1")]
        //public string FilterW1
        //{
        //    get => _filterW1;
        //    set => SetProperty(ref _filterW1, value);
        //}

        ///// <summary>
        ///// Filter cutoff frequency 2 (W2) - For band filters only
        ///// </summary>
        //[JsonPropertyName("filterW2")]
        //public string FilterW2
        //{
        //    get => _filterW2;
        //    set => SetProperty(ref _filterW2, value);
        //}

        /// <summary>
        /// Current timebase setting (for frequency validation)
        /// </summary>
        [JsonPropertyName("currentTimebase")]
        public string CurrentTimebase
        {
            get => _currentTimebase;
            set => SetProperty(ref _currentTimebase, value);
        }

        /// <summary>
        /// Calculated minimum frequency for current timebase
        /// </summary>
        [JsonPropertyName("calculatedMinFreq")]
        public string CalculatedMinFreq
        {
            get => _calculatedMinFreq;
            set => SetProperty(ref _calculatedMinFreq, value);
        }

        /// <summary>
        /// Calculated maximum frequency for current timebase
        /// </summary>
        [JsonPropertyName("calculatedMaxFreq")]
        public string CalculatedMaxFreq
        {
            get => _calculatedMaxFreq;
            set => SetProperty(ref _calculatedMaxFreq, value);
        }

        /// <summary>
        /// Calculated frequency step size for current timebase
        /// </summary>
        [JsonPropertyName("calculatedStepSize")]
        public string CalculatedStepSize
        {
            get => _calculatedStepSize;
            set => SetProperty(ref _calculatedStepSize, value);
        }

        #endregion

        #region Advanced Math Properties

        ///// <summary>
        ///// Advanced math function (INTG, DIFF, SQRT, LG, LN, EXP, ABS)
        ///// </summary>
        //[JsonPropertyName("advancedFunction")]
        //public string AdvancedFunction
        //{
        //    get => _advancedFunction;
        //    set => SetProperty(ref _advancedFunction, value);
        //}

        ///// <summary>
        ///// Start point for integration/advanced math operations
        ///// </summary>
        //[JsonPropertyName("startPoint")]
        //public string StartPoint
        //{
        //    get => _startPoint;
        //    set => SetProperty(ref _startPoint, value);
        //}

        ///// <summary>
        ///// End point for integration/advanced math operations
        ///// </summary>
        //[JsonPropertyName("endPoint")]
        //public string EndPoint
        //{
        //    get => _endPoint;
        //    set => SetProperty(ref _endPoint, value);
        //}

        #endregion

        #region Display Properties

        /// <summary>
        /// Math display enabled state
        /// </summary>
        [JsonPropertyName("mathDisplayEnabled")]
        public bool MathDisplayEnabled
        {
            get => _mathDisplayEnabled;
            set => SetProperty(ref _mathDisplayEnabled, value);
        }

        /// <summary>
        /// Invert math waveform
        /// </summary>
        [JsonPropertyName("invertWaveform")]
        public bool InvertWaveform
        {
            get => _invertWaveform;
            set => SetProperty(ref _invertWaveform, value);
        }

        /// <summary>
        /// Math waveform vertical scale
        /// </summary>
        [JsonPropertyName("scale")]
        public string Scale
        {
            get => _scale;
            set => SetProperty(ref _scale, value);
        }

        /// <summary>
        /// Math waveform vertical offset
        /// </summary>
        [JsonPropertyName("offset")]
        public string Offset
        {
            get => _offset;
            set => SetProperty(ref _offset, value);
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validate basic math operation settings
        /// </summary>
        /// <returns>Validation result with error message if invalid</returns>
        public (bool isValid, string errorMessage) ValidateBasicMath()
        {
            if (!IsValidSource(Source1))
                return (false, $"Invalid Source1: {Source1}");

            if (!IsValidSource(Source2))
                return (false, $"Invalid Source2: {Source2}");

            if (!IsValidOperator(Operation))
                return (false, $"Invalid Operation: {Operation}");

            return (true, "");
        }

        /// <summary>
        /// Validate FFT analysis settings
        /// </summary>
        /// <returns>Validation result with error message if invalid</returns>
        public (bool isValid, string errorMessage) ValidateFFT()
        {
            if (!IsValidSource(FFTSource))
                return (false, $"Invalid FFT Source: {FFTSource}");

            if (!IsValidFFTWindow(FFTWindow))
                return (false, $"Invalid FFT Window: {FFTWindow}");

            return (true, "");
        }

        ///// <summary>
        ///// Validate filter settings including timebase dependencies
        ///// </summary>
        ///// <returns>Validation result with error message if invalid</returns>
        //public (bool isValid, string errorMessage) ValidateFilter()
        //{
        //    if (!IsValidFilterType(FilterType))
        //        return (false, $"Invalid Filter Type: {FilterType}");

        //    // Parse frequency values
        //    if (!double.TryParse(FilterW1, out double w1))
        //        return (false, $"Invalid W1 frequency: {FilterW1}");

        //    // Check if W2 is required for band filters
        //    bool isBandFilter = FilterType == "BPASs" || FilterType == "BSTop";

        //    if (isBandFilter)
        //    {
        //        if (!double.TryParse(FilterW2, out double w2))
        //            return (false, $"Invalid W2 frequency: {FilterW2}");

        //        if (w1 >= w2)
        //            return (false, "W1 must be less than W2 for band filters");
        //    }

        //    return (true, "");
        //}

        ///// <summary>
        ///// Validate advanced math function settings
        ///// </summary>
        ///// <returns>Validation result with error message if invalid</returns>
        //public (bool isValid, string errorMessage) ValidateAdvancedMath()
        //{
        //    if (!IsValidAdvancedOperator(AdvancedFunction))
        //        return (false, $"Invalid Advanced Function: {AdvancedFunction}");

        //    if (!double.TryParse(StartPoint, out double start))
        //        return (false, $"Invalid Start Point: {StartPoint}");

        //    if (!double.TryParse(EndPoint, out double end))
        //        return (false, $"Invalid End Point: {EndPoint}");

        //    if (start >= end)
        //        return (false, "Start point must be less than end point");

        //    return (true, "");
        //}

        ///// <summary>
        ///// Validate if a source is valid
        ///// </summary>
        ///// <param name="source">Source to validate</param>
        ///// <returns>True if valid</returns>
        //private static bool IsValidSource(string source)
        //{
        //    return source == "CHANnel1" ||
        //           source == "CHANnel2" ||
        //           source == "MATH";
        //}

        /// <summary>
        /// Validate if an operator is valid for :MATH:OPERator command
        /// </summary>
        /// <param name="op">Operator to validate</param>
        /// <returns>True if valid</returns>
        private static bool IsValidOperator(string op)
        {
            return op == "ADD" ||
                   op == "SUBtract" ||
                   op == "MULtiply" ||
                   op == "DIVide";
        }

        /// <summary>
        /// Validate if an FFT window is valid
        /// </summary>
        /// <param name="window">Window to validate</param>
        /// <returns>True if valid</returns>
        private static bool IsValidFFTWindow(string window)
        {
            return window == "RECTangular" ||
                   window == "BLACkman" ||
                   window == "HANNing" ||
                   window == "HAMMing";
        }

        /// <summary>
        /// Validate if a filter type is valid
        /// </summary>
        /// <param name="filterType">Filter type to validate</param>
        /// <returns>True if valid</returns>
        private static bool IsValidFilterType(string filterType)
        {
            return filterType == "LPASs" ||
                   filterType == "HPASs" ||
                   filterType == "BPASs" ||
                   filterType == "BSTop";
        }

        /// <summary>
        /// Validate if an advanced operator is valid
        /// </summary>
        /// <param name="op">Advanced operator to validate</param>
        /// <returns>True if valid</returns>
        private static bool IsValidAdvancedOperator(string op)
        {
            return op == "INTG" ||
                   op == "DIFF" ||
                   op == "SQRT" ||
                   op == "LG" ||
                   op == "LN" ||
                   op == "EXP" ||
                   op == "ABS";
        }

        /// <summary>
        /// Update frequency range calculations based on current timebase
        /// </summary>
        /// <param name="timebaseSeconds">Current timebase in seconds</param>
        public void UpdateFrequencyCalculations(double timebaseSeconds)
        {
            try
            {
                CurrentTimebase = timebaseSeconds.ToString("E", System.Globalization.CultureInfo.InvariantCulture);

                (double minFreq, double maxFreq, double stepSize) = MathematicsCommands.CalculateFilterFrequencyRange(timebaseSeconds, FilterType);

                CalculatedMinFreq = minFreq.ToString("E", System.Globalization.CultureInfo.InvariantCulture);
                CalculatedMaxFreq = maxFreq.ToString("E", System.Globalization.CultureInfo.InvariantCulture);
                CalculatedStepSize = stepSize.ToString("E", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                CalculatedMinFreq = $"Error: {ex.Message}";
                CalculatedMaxFreq = "";
                CalculatedStepSize = "";
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Create a deep copy of the current settings
        /// </summary>
        /// <returns>New instance with copied values</returns>
        public MathematicsSettings Clone()
        {
            return new MathematicsSettings
            {
                ConfigurationName = ConfigurationName,
                ActiveMode = ActiveMode,
                Source1 = Source1,
                Source2 = Source2,
                Operation = Operation,
                FFTSource = FFTSource,
                FFTWindow = FFTWindow,
                FFTSplit = FFTSplit,
                FFTUnit = FFTUnit,
                FilterType = FilterType,
                FilterW1 = FilterW1,
                FilterW2 = FilterW2,
                CurrentTimebase = CurrentTimebase,
                CalculatedMinFreq = CalculatedMinFreq,
                CalculatedMaxFreq = CalculatedMaxFreq,
                CalculatedStepSize = CalculatedStepSize,
                AdvancedFunction = AdvancedFunction,
                StartPoint = StartPoint,
                EndPoint = EndPoint,
                MathDisplayEnabled = MathDisplayEnabled,
                InvertWaveform = InvertWaveform,
                Scale = Scale,
                Offset = Offset
            };
        }

        /// <summary>
        /// Reset all settings to defaults
        /// </summary>
        //public void Reset()
        //{
        //    ConfigurationName = "Default Math Configuration";
        //    ActiveMode = "None";
        //    Source1 = "CHANnel1";
        //    Source2 = "CHANnel2";
        //    Operation = "ADD";
        //    FFTSource = "CHANnel1";
        //    FFTWindow = "HANNing";
        //    FFTSplit = "FULL";
        //    FFTUnit = "VRMS";
        //    FilterType = "LPASs";
        //    FilterW1 = "1000000";
        //    FilterW2 = "2000000";
        //    CurrentTimebase = "";
        //    CalculatedMinFreq = "";
        //    CalculatedMaxFreq = "";
        //    CalculatedStepSize = "";
        //    AdvancedFunction = "INTG";
        //    StartPoint = "0";
        //    EndPoint = "100";
        //    MathDisplayEnabled = true;
        //    InvertWaveform = false;
        //    Scale = "1.0";
        //    Offset = "0.0";
        //}

        /// <summary>
        /// Check if current settings are equal to another instance
        /// </summary>
        /// <param name="other">Settings to compare with</param>
        /// <returns>True if all settings match</returns>
        public bool Equals(MathematicsSettings other)
        {
            if (other == null) return false;

            return ConfigurationName == other.ConfigurationName &&
                   ActiveMode == other.ActiveMode &&
                   Source1 == other.Source1 &&
                   Source2 == other.Source2 &&
                   Operation == other.Operation &&
                   FFTSource == other.FFTSource &&
                   FFTWindow == other.FFTWindow &&
                   FFTSplit == other.FFTSplit &&
                   FFTUnit == other.FFTUnit &&
                   FilterType == other.FilterType &&
                   FilterW1 == other.FilterW1 &&
                   FilterW2 == other.FilterW2 &&
                   CurrentTimebase == other.CurrentTimebase &&
                   AdvancedFunction == other.AdvancedFunction &&
                   StartPoint == other.StartPoint &&
                   EndPoint == other.EndPoint &&
                   MathDisplayEnabled == other.MathDisplayEnabled &&
                   InvertWaveform == other.InvertWaveform &&
                   Scale == other.Scale &&
                   Offset == other.Offset;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Create default settings for Basic Operations
        /// </summary>
        public static MathematicsSettings CreateBasicOperationsDefault()
        {
            return new MathematicsSettings
            {
                ActiveMode = "BasicOperations",
                Source1 = "CHANnel1",
                Source2 = "CHANnel2",
                Operation = "ADD",
                ConfigurationName = "Basic Addition Default"
            };
        }

        /// <summary>
        /// Create default settings for FFT Analysis
        /// </summary>
        public static MathematicsSettings CreateFFTAnalysisDefault()
        {
            return new MathematicsSettings
            {
                ActiveMode = "FFTAnalysis",
                FFTSource = "CHANnel1",
                FFTWindow = "HANNing",
                FFTSplit = "FULL",
                FFTUnit = "VRMS",
                ConfigurationName = "FFT Analysis Default"
            };
        }

        /// <summary>
        /// Create default settings for Digital Filters
        /// </summary>
        public static MathematicsSettings CreateDigitalFilterDefault()
        {
            return new MathematicsSettings
            {
                ActiveMode = "DigitalFilter",
                FilterType = "LPASs",
                FilterW1 = "1000000",
                ConfigurationName = "Low Pass Filter Default"
            };
        }

        /// <summary>
        /// Create default settings for Band Pass Filter
        /// </summary>
        public static MathematicsSettings CreateBandPassFilterDefault()
        {
            return new MathematicsSettings
            {
                ActiveMode = "DigitalFilter",
                FilterType = "BPASs",
                FilterW1 = "500000",
                FilterW2 = "2000000",
                ConfigurationName = "Band Pass Filter Default"
            };
        }

        /// <summary>
        /// Create default settings for Advanced Math
        /// </summary>
        public static MathematicsSettings CreateAdvancedMathDefault()
        {
            return new MathematicsSettings
            {
                ActiveMode = "AdvancedMath",
                AdvancedFunction = "INTG",
                StartPoint = "0",
                EndPoint = "100",
                ConfigurationName = "Integration Default"
            };
        }

        #endregion























        #region Digital Filter Properties - UPDATED with Source

        /// <summary>
        /// Digital filter source channel (CHANnel1, CHANnel2)
        /// </summary>
        [JsonPropertyName("filterSource")]
        public string FilterSource
        {
            get => _filterSource;
            set => SetProperty(ref _filterSource, value);
        }
        private string _filterSource = "CHANnel1";

        /// <summary>
        /// Digital filter type (LPASs, HPASs, BPASs, BSTop)
        /// </summary>
        [JsonPropertyName("filterType")]
        public string FilterType
        {
            get => _filterType;
            set => SetProperty(ref _filterType, value);
        }

        /// <summary>
        /// Filter cutoff frequency 1 (W1) - Timebase dependent
        /// </summary>
        [JsonPropertyName("filterW1")]
        public string FilterW1
        {
            get => _filterW1;
            set => SetProperty(ref _filterW1, value);
        }

        /// <summary>
        /// Filter cutoff frequency 2 (W2) - For band filters only
        /// </summary>
        [JsonPropertyName("filterW2")]
        public string FilterW2
        {
            get => _filterW2;
            set => SetProperty(ref _filterW2, value);
        }

        #endregion

        #region Advanced Math Properties - UPDATED with Source

        /// <summary>
        /// Advanced math source channel (CHANnel1, CHANnel2)
        /// </summary>
        [JsonPropertyName("advancedSource")]
        public string AdvancedSource
        {
            get => _advancedSource;
            set => SetProperty(ref _advancedSource, value);
        }
        private string _advancedSource = "CHANnel1";

        /// <summary>
        /// Advanced math function (INTG, DIFF, SQRT, LG, LN, EXP, ABS)
        /// </summary>
        [JsonPropertyName("advancedFunction")]
        public string AdvancedFunction
        {
            get => _advancedFunction;
            set => SetProperty(ref _advancedFunction, value);
        }

        /// <summary>
        /// Start point for integration/advanced math operations (0-1199)
        /// </summary>
        [JsonPropertyName("startPoint")]
        public string StartPoint
        {
            get => _startPoint;
            set => SetProperty(ref _startPoint, value);
        }

        /// <summary>
        /// End point for integration/advanced math operations (0-1199)
        /// </summary>
        [JsonPropertyName("endPoint")]
        public string EndPoint
        {
            get => _endPoint;
            set => SetProperty(ref _endPoint, value);
        }

        #endregion

        #region UPDATED: Validation Methods with Source

        /// <summary>
        /// Validate filter settings including source channel
        /// </summary>
        /// <returns>Validation result with error message if invalid</returns>
        public (bool isValid, string errorMessage) ValidateFilter()
        {
            if (!IsValidSource(FilterSource))
                return (false, $"Invalid Filter Source: {FilterSource}");

            if (!IsValidFilterType(FilterType))
                return (false, $"Invalid Filter Type: {FilterType}");

            // Parse frequency values
            if (!double.TryParse(FilterW1, out double w1))
                return (false, $"Invalid W1 frequency: {FilterW1}");

            // Check if W2 is required for band filters
            bool isBandFilter = FilterType == "BPASs" || FilterType == "BSTop";

            if (isBandFilter)
            {
                if (!double.TryParse(FilterW2, out double w2))
                    return (false, $"Invalid W2 frequency: {FilterW2}");

                if (w1 >= w2)
                    return (false, "W1 must be less than W2 for band filters");
            }

            return (true, "");
        }

        /// <summary>
        /// Validate advanced math function settings including source
        /// </summary>
        /// <returns>Validation result with error message if invalid</returns>
        public (bool isValid, string errorMessage) ValidateAdvancedMath()
        {
            if (!IsValidSource(AdvancedSource))
                return (false, $"Invalid Advanced Math Source: {AdvancedSource}");

            if (!IsValidAdvancedOperator(AdvancedFunction))
                return (false, $"Invalid Advanced Function: {AdvancedFunction}");

            if (!double.TryParse(StartPoint, out double start))
                return (false, $"Invalid Start Point: {StartPoint}");

            if (!double.TryParse(EndPoint, out double end))
                return (false, $"Invalid End Point: {EndPoint}");

            // Validate memory depth limits (0-1199)
            if (start < 0 || start > 1199)
                return (false, $"Start Point must be between 0-1199: {start}");

            if (end < 0 || end > 1199)
                return (false, $"End Point must be between 0-1199: {end}");

            if (start >= end)
                return (false, "Start point must be less than end point");

            return (true, "");
        }

        /// <summary>
        /// Validate if a source is valid for Filter/Advanced Math
        /// </summary>
        /// <param name="source">Source to validate</param>
        /// <returns>True if valid</returns>
        private static bool IsValidSource(string source)
        {
            return source == "CHANnel1" || source == "CHANnel2";
        }

        #endregion

        #region UPDATED: Factory Methods with Source

        /// <summary>
        /// Create default settings for Digital Filters with source
        /// </summary>
        public static MathematicsSettings CreateDigitalFilterDefault(string source = "CHANnel1")
        {
            return new MathematicsSettings
            {
                ActiveMode = "DigitalFilter",
                FilterSource = source,
                FilterType = "LPASs",
                FilterW1 = "1000",
                ConfigurationName = $"Low Pass Filter on {source}"
            };
        }

        /// <summary>
        /// Create default settings for Advanced Math with source
        /// </summary>
        public static MathematicsSettings CreateAdvancedMathDefault(string source = "CHANnel1")
        {
            return new MathematicsSettings
            {
                ActiveMode = "AdvancedMath",
                AdvancedSource = source,
                AdvancedFunction = "INTG",
                StartPoint = "0",
                EndPoint = "1199",
                ConfigurationName = $"Integration on {source}"
            };
        }

        #endregion

        #region UPDATED: Reset Method

        /// <summary>
        /// Reset all settings to defaults
        /// </summary>
        public void Reset()
        {
            ConfigurationName = "Default Math Configuration";
            ActiveMode = "None";
            Source1 = "CHANnel1";
            Source2 = "CHANnel2";
            Operation = "ADD";
            FFTSource = "CHANnel1";
            FFTWindow = "HANNing";
            FFTSplit = "FULL";
            FFTUnit = "VRMS";
            FilterSource = "CHANnel1";        // NEW
            FilterType = "LPASs";
            FilterW1 = "1000";
            FilterW2 = "10000";
            CurrentTimebase = "";
            CalculatedMinFreq = "";
            CalculatedMaxFreq = "";
            CalculatedStepSize = "";
            AdvancedSource = "CHANnel1";      // NEW
            AdvancedFunction = "INTG";
            StartPoint = "0";
            EndPoint = "1199";                // UPDATED limit
            MathDisplayEnabled = true;
            InvertWaveform = false;
            Scale = "1.0";
            Offset = "0.0";
        }

        #endregion
    }
}