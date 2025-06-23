using System;
using System.Collections.Generic;
using System.Globalization;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Static class containing all MATH SCPI commands for Rigol DS1000Z-E oscilloscope
    /// Based on DS1000Z-E Programming Guide MATH Commands section
    /// </summary>
    public static class MathematicsCommands
    {
        #region Base Math Commands

        /// <summary>
        /// Enable or disable math display
        /// </summary>
        public const string MATH_DISPLAY = ":MATH:DISPlay";

        /// <summary>
        /// Set math operation type
        /// </summary>
        public const string MATH_OPERATOR = ":MATH:OPERator";

        /// <summary>
        /// Set source 1 for math operations
        /// </summary>
        public const string MATH_SOURCE1 = ":MATH:SOURce1";

        /// <summary>
        /// Set source 2 for math operations
        /// </summary>
        public const string MATH_SOURCE2 = ":MATH:SOURce2";

        /// <summary>
        /// Set logical source 1 (for advanced operations)
        /// </summary>
        public const string MATH_LSOURCE1 = ":MATH:LSOUrce1";

        /// <summary>
        /// Set logical source 2 (for advanced operations)
        /// </summary>
        public const string MATH_LSOURCE2 = ":MATH:LSOUrce2";

        /// <summary>
        /// Set math waveform vertical scale
        /// </summary>
        public const string MATH_SCALE = ":MATH:SCALe";

        /// <summary>
        /// Set math waveform vertical offset
        /// </summary>
        public const string MATH_OFFSET = ":MATH:OFFSet";

        /// <summary>
        /// Invert math waveform
        /// </summary>
        public const string MATH_INVERT = ":MATH:INVert";

        /// <summary>
        /// Reset all math settings to defaults
        /// </summary>
        public const string MATH_RESET = ":MATH:RESet";

        #endregion

        #region FFT Commands

        /// <summary>
        /// Set FFT source channel
        /// </summary>
        public const string MATH_FFT_SOURCE = ":MATH:FFT:SOURce";

        /// <summary>
        /// Set FFT windowing function
        /// </summary>
        public const string MATH_FFT_WINDOW = ":MATH:FFT:WINDow";

        /// <summary>
        /// Set FFT display mode (full screen or center)
        /// </summary>
        public const string MATH_FFT_SPLIT = ":MATH:FFT:SPLit";

        /// <summary>
        /// Set FFT unit (VRMS or dB)
        /// </summary>
        public const string MATH_FFT_UNIT = ":MATH:FFT:UNIT";

        /// <summary>
        /// Set FFT horizontal scale
        /// </summary>
        public const string MATH_FFT_HSCALE = ":MATH:FFT:HSCale";

        /// <summary>
        /// Set FFT horizontal center frequency
        /// </summary>
        public const string MATH_FFT_HCENTER = ":MATH:FFT:HCENter";

        /// <summary>
        /// Set FFT mode
        /// </summary>
        public const string MATH_FFT_MODE = ":MATH:FFT:MODE";

        #endregion

        #region Digital Filter Commands

        /// <summary>
        /// Set digital filter type
        /// </summary>
        public const string MATH_FILTER_TYPE = ":MATH:FILTer:TYPE";

        /// <summary>
        /// Set filter lower cutoff frequency (W1)
        /// </summary>
        public const string MATH_FILTER_W1 = ":MATH:FILTer:W1";

        /// <summary>
        /// Set filter upper cutoff frequency (W2)
        /// </summary>
        public const string MATH_FILTER_W2 = ":MATH:FILTer:W2";

        #endregion

        #region Advanced Math Option Commands

        /// <summary>
        /// Set start point for integration/advanced math
        /// </summary>
        public const string MATH_OPTION_START = ":MATH:OPTion:STARt";

        /// <summary>
        /// Set end point for integration/advanced math
        /// </summary>
        public const string MATH_OPTION_END = ":MATH:OPTion:END";

        /// <summary>
        /// Invert option for advanced math
        /// </summary>
        public const string MATH_OPTION_INVERT = ":MATH:OPTion:INVert";

        /// <summary>
        /// Set sensitivity for advanced math
        /// </summary>
        public const string MATH_OPTION_SENSITIVITY = ":MATH:OPTion:SENSitivity";

        /// <summary>
        /// Set distance for advanced math
        /// </summary>
        public const string MATH_OPTION_DISTANCE = ":MATH:OPTion:DIStance";

        /// <summary>
        /// Set auto scale for advanced math
        /// </summary>
        public const string MATH_OPTION_ASCALE = ":MATH:OPTion:ASCale";

        /// <summary>
        /// Set threshold 1 for advanced math
        /// </summary>
        public const string MATH_OPTION_THRESHOLD1 = ":MATH:OPTion:THReshold1";

        /// <summary>
        /// Set threshold 2 for advanced math
        /// </summary>
        public const string MATH_OPTION_THRESHOLD2 = ":MATH:OPTion:THReshold2";

        /// <summary>
        /// Set FX source 1 for advanced functions
        /// </summary>
        public const string MATH_OPTION_FX_SOURCE1 = ":MATH:OPTion:FX:SOURce1";

        /// <summary>
        /// Set FX source 2 for advanced functions
        /// </summary>
        public const string MATH_OPTION_FX_SOURCE2 = ":MATH:OPTion:FX:SOURce2";

        /// <summary>
        /// Set FX operator for advanced functions
        /// </summary>
        public const string MATH_OPTION_FX_OPERATOR = ":MATH:OPTion:FX:OPERator";

        #endregion

        #region Parameter Constants

        /// <summary>
        /// Boolean parameter values
        /// </summary>
        public static class BooleanValues
        {
            public const string ON = "ON";
            public const string OFF = "OFF";
            public const string TRUE = "1";
            public const string FALSE = "0";
        }

        /// <summary>
        /// Basic math operators
        /// </summary>
        public static class Operators
        {
            public const string ADD = "ADD";
            public const string SUBTRACT = "SUBtract";
            public const string MULTIPLY = "MULtiply";
            public const string DIVIDE = "DIVide";
        }

        /// <summary>
        /// Channel source options
        /// </summary>
        public static class Sources
        {
            public const string CHANNEL1 = "CHANnel1";
            public const string CHANNEL2 = "CHANnel2";
            public const string MATH = "MATH";
        }

        /// <summary>
        /// FFT windowing functions
        /// </summary>
        public static class FFTWindows
        {
            public const string RECTANGULAR = "RECTangular";
            public const string BLACKMAN = "BLACkman";
            public const string HANNING = "HANNing";
            public const string HAMMING = "HAMMing";
        }

        /// <summary>
        /// FFT display modes
        /// </summary>
        public static class FFTSplitModes
        {
            public const string FULL = "FULL";
            public const string CENTER = "CENTer";
        }

        /// <summary>
        /// FFT unit types
        /// </summary>
        public static class FFTUnits
        {
            public const string VRMS = "VRMS";
            public const string DB = "DB";
        }

        /// <summary>
        /// Digital filter types
        /// </summary>
        public static class FilterTypes
        {
            public const string LOW_PASS = "LPASs";
            public const string HIGH_PASS = "HPASs";
            public const string BAND_PASS = "BPASs";
            public const string BAND_STOP = "BSTop";
        }

        /// <summary>
        /// Advanced math function operators
        /// </summary>
        public static class AdvancedOperators
        {
            public const string INTEGRATION = "INTG";
            public const string DIFFERENTIATION = "DIFF";
            public const string SQUARE_ROOT = "SQRT";
            public const string LOGARITHM_10 = "LG";
            public const string NATURAL_LOG = "LN";
            public const string EXPONENTIAL = "EXP";
            public const string ABSOLUTE_VALUE = "ABS";
        }

        #endregion

        #region Command Building Methods

        /// <summary>
        /// Build a complete MATH display command
        /// </summary>
        /// <param name="enable">True to enable, false to disable</param>
        /// <returns>Complete SCPI command string</returns>
        public static string BuildDisplayCommand(bool enable)
        {
            return $"{MATH_DISPLAY} {(enable ? BooleanValues.ON : BooleanValues.OFF)}";
        }

        /// <summary>
        /// Build a basic math operation command set
        /// </summary>
        /// <param name="source1">First source channel</param>
        /// <param name="source2">Second source channel</param>
        /// <param name="operation">Math operation</param>
        /// <returns>List of SCPI commands</returns>
        public static List<string> BuildBasicOperationCommands(string source1, string source2, string operation)
        {
            var commands = new List<string>
            {
                BuildDisplayCommand(true),
                $"{MATH_SOURCE1} {source1}",
                $"{MATH_SOURCE2} {source2}",
                $"{MATH_OPERATOR} {operation}"
            };
            return commands;
        }

        /// <summary>
        /// Build FFT analysis command set
        /// </summary>
        /// <param name="source">FFT source channel</param>
        /// <param name="window">Windowing function</param>
        /// <param name="split">Display mode</param>
        /// <param name="unit">Measurement unit</param>
        /// <returns>List of SCPI commands</returns>
        public static List<string> BuildFFTCommands(string source, string window, string split, string unit)
        {
            var commands = new List<string>
            {
                BuildDisplayCommand(true),
                $"{MATH_FFT_SOURCE} {source}",
                $"{MATH_FFT_WINDOW} {window}",
                $"{MATH_FFT_SPLIT} {split}",
                $"{MATH_FFT_UNIT} {unit}"
            };
            return commands;
        }

        /// <summary>
        /// Build digital filter command set
        /// </summary>
        /// <param name="filterType">Type of filter</param>
        /// <param name="w1">Lower cutoff frequency</param>
        /// <param name="w2">Upper cutoff frequency</param>
        /// <returns>List of SCPI commands</returns>
        public static List<string> BuildFilterCommands(string filterType, double w1, double w2)
        {
            var commands = new List<string>
            {
                BuildDisplayCommand(true),
                $"{MATH_FILTER_TYPE} {filterType}",
                $"{MATH_FILTER_W1} {w1.ToString("G", CultureInfo.InvariantCulture)}",
                $"{MATH_FILTER_W2} {w2.ToString("G", CultureInfo.InvariantCulture)}"
            };
            return commands;
        }

        /// <summary>
        /// Build advanced math function command set
        /// </summary>
        /// <param name="function">Advanced function type</param>
        /// <param name="startPoint">Start point for integration/analysis</param>
        /// <param name="endPoint">End point for integration/analysis</param>
        /// <returns>List of SCPI commands</returns>
        public static List<string> BuildAdvancedMathCommands(string function, double startPoint, double endPoint)
        {
            var commands = new List<string>
            {
                BuildDisplayCommand(true),
                $"{MATH_OPTION_FX_OPERATOR} {function}",
                $"{MATH_OPTION_START} {startPoint.ToString("G", CultureInfo.InvariantCulture)}",
                $"{MATH_OPTION_END} {endPoint.ToString("G", CultureInfo.InvariantCulture)}"
            };
            return commands;
        }

        /// <summary>
        /// Build display control command set
        /// </summary>
        /// <param name="enable">Enable math display</param>
        /// <param name="invert">Invert waveform</param>
        /// <param name="scale">Vertical scale</param>
        /// <param name="offset">Vertical offset</param>
        /// <returns>List of SCPI commands</returns>
        public static List<string> BuildDisplayControlCommands(bool enable, bool invert, double scale, double offset)
        {
            var commands = new List<string>
            {
                BuildDisplayCommand(enable),
                $"{MATH_INVERT} {(invert ? BooleanValues.ON : BooleanValues.OFF)}",
                $"{MATH_SCALE} {scale.ToString("G", CultureInfo.InvariantCulture)}",
                $"{MATH_OFFSET} {offset.ToString("G", CultureInfo.InvariantCulture)}"
            };
            return commands;
        }

        /// <summary>
        /// Build math reset command
        /// </summary>
        /// <returns>Reset command string</returns>
        public static string BuildResetCommand()
        {
            return MATH_RESET;
        }

        #endregion

        #region Query Commands

        /// <summary>
        /// Query commands for reading current settings
        /// </summary>
        public static class Queries
        {
            public const string MATH_DISPLAY_QUERY = ":MATH:DISPlay?";
            public const string MATH_OPERATOR_QUERY = ":MATH:OPERator?";
            public const string MATH_SOURCE1_QUERY = ":MATH:SOURce1?";
            public const string MATH_SOURCE2_QUERY = ":MATH:SOURce2?";
            public const string MATH_SCALE_QUERY = ":MATH:SCALe?";
            public const string MATH_OFFSET_QUERY = ":MATH:OFFSet?";
            public const string MATH_INVERT_QUERY = ":MATH:INVert?";

            public const string MATH_FFT_SOURCE_QUERY = ":MATH:FFT:SOURce?";
            public const string MATH_FFT_WINDOW_QUERY = ":MATH:FFT:WINDow?";
            public const string MATH_FFT_SPLIT_QUERY = ":MATH:FFT:SPLit?";
            public const string MATH_FFT_UNIT_QUERY = ":MATH:FFT:UNIT?";

            public const string MATH_FILTER_TYPE_QUERY = ":MATH:FILTer:TYPE?";
            public const string MATH_FILTER_W1_QUERY = ":MATH:FILTer:W1?";
            public const string MATH_FILTER_W2_QUERY = ":MATH:FILTer:W2?";

            public const string MATH_OPTION_FX_OPERATOR_QUERY = ":MATH:OPTion:FX:OPERator?";
            public const string MATH_OPTION_START_QUERY = ":MATH:OPTion:STARt?";
            public const string MATH_OPTION_END_QUERY = ":MATH:OPTion:END?";
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validate if a source is valid
        /// </summary>
        /// <param name="source">Source to validate</param>
        /// <returns>True if valid</returns>
        public static bool IsValidSource(string source)
        {
            return source == Sources.CHANNEL1 ||
                   source == Sources.CHANNEL2 ||
                   source == Sources.MATH;
        }

        /// <summary>
        /// Validate if an operator is valid
        /// </summary>
        /// <param name="op">Operator to validate</param>
        /// <returns>True if valid</returns>
        public static bool IsValidOperator(string op)
        {
            return op == Operators.ADD ||
                   op == Operators.SUBTRACT ||
                   op == Operators.MULTIPLY ||
                   op == Operators.DIVIDE;
        }

        /// <summary>
        /// Validate if an FFT window is valid
        /// </summary>
        /// <param name="window">Window to validate</param>
        /// <returns>True if valid</returns>
        public static bool IsValidFFTWindow(string window)
        {
            return window == FFTWindows.RECTANGULAR ||
                   window == FFTWindows.BLACKMAN ||
                   window == FFTWindows.HANNING ||
                   window == FFTWindows.HAMMING;
        }

        /// <summary>
        /// Validate if a filter type is valid
        /// </summary>
        /// <param name="filterType">Filter type to validate</param>
        /// <returns>True if valid</returns>
        public static bool IsValidFilterType(string filterType)
        {
            return filterType == FilterTypes.LOW_PASS ||
                   filterType == FilterTypes.HIGH_PASS ||
                   filterType == FilterTypes.BAND_PASS ||
                   filterType == FilterTypes.BAND_STOP;
        }

        /// <summary>
        /// Validate if an advanced operator is valid
        /// </summary>
        /// <param name="op">Advanced operator to validate</param>
        /// <returns>True if valid</returns>
        public static bool IsValidAdvancedOperator(string op)
        {
            return op == AdvancedOperators.INTEGRATION ||
                   op == AdvancedOperators.DIFFERENTIATION ||
                   op == AdvancedOperators.SQUARE_ROOT ||
                   op == AdvancedOperators.LOGARITHM_10 ||
                   op == AdvancedOperators.NATURAL_LOG ||
                   op == AdvancedOperators.EXPONENTIAL ||
                   op == AdvancedOperators.ABSOLUTE_VALUE;
        }

        /// <summary>
        /// Validate frequency value
        /// </summary>
        /// <param name="frequency">Frequency to validate</param>
        /// <returns>True if valid (positive number)</returns>
        public static bool IsValidFrequency(double frequency)
        {
            return frequency > 0 && !double.IsInfinity(frequency) && !double.IsNaN(frequency);
        }

        /// <summary>
        /// Validate scale value
        /// </summary>
        /// <param name="scale">Scale to validate</param>
        /// <returns>True if valid (positive number)</returns>
        public static bool IsValidScale(double scale)
        {
            return scale > 0 && !double.IsInfinity(scale) && !double.IsNaN(scale);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Convert commands list to single string
        /// </summary>
        /// <param name="commands">List of commands</param>
        /// <param name="separator">Separator between commands (default: newline)</param>
        /// <returns>Combined command string</returns>
        public static string CombineCommands(List<string> commands, string separator = "\n")
        {
            if (commands == null || commands.Count == 0)
                return string.Empty;

            return string.Join(separator, commands);
        }

        /// <summary>
        /// Parse numeric value from string with error handling
        /// </summary>
        /// <param name="value">String value to parse</param>
        /// <param name="defaultValue">Default value if parsing fails</param>
        /// <returns>Parsed double value</returns>
        public static double ParseNumericValue(string value, double defaultValue = 0.0)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                return result;

            return defaultValue;
        }

        /// <summary>
        /// Get all available operators as display strings
        /// </summary>
        /// <returns>Dictionary of operator tags and display names</returns>
        public static Dictionary<string, string> GetOperatorDisplayNames()
        {
            return new Dictionary<string, string>
            {
                { Operators.ADD, "Add (CH1 + CH2)" },
                { Operators.SUBTRACT, "Subtract (CH1 - CH2)" },
                { Operators.MULTIPLY, "Multiply (CH1 × CH2)" },
                { Operators.DIVIDE, "Divide (CH1 ÷ CH2)" }
            };
        }

        /// <summary>
        /// Get all available FFT windows as display strings
        /// </summary>
        /// <returns>Dictionary of window tags and display names</returns>
        public static Dictionary<string, string> GetFFTWindowDisplayNames()
        {
            return new Dictionary<string, string>
            {
                { FFTWindows.RECTANGULAR, "Rectangular" },
                { FFTWindows.BLACKMAN, "Blackman" },
                { FFTWindows.HANNING, "Hanning" },
                { FFTWindows.HAMMING, "Hamming" }
            };
        }

        /// <summary>
        /// Get all available filter types as display strings
        /// </summary>
        /// <returns>Dictionary of filter tags and display names</returns>
        public static Dictionary<string, string> GetFilterTypeDisplayNames()
        {
            return new Dictionary<string, string>
            {
                { FilterTypes.LOW_PASS, "Low Pass" },
                { FilterTypes.HIGH_PASS, "High Pass" },
                { FilterTypes.BAND_PASS, "Band Pass" },
                { FilterTypes.BAND_STOP, "Band Stop" }
            };
        }

        /// <summary>
        /// Get all available advanced operators as display strings
        /// </summary>
        /// <returns>Dictionary of operator tags and display names</returns>
        public static Dictionary<string, string> GetAdvancedOperatorDisplayNames()
        {
            return new Dictionary<string, string>
            {
                { AdvancedOperators.INTEGRATION, "Integration" },
                { AdvancedOperators.DIFFERENTIATION, "Differentiation" },
                { AdvancedOperators.SQUARE_ROOT, "Square Root" },
                { AdvancedOperators.LOGARITHM_10, "Logarithm (base 10)" },
                { AdvancedOperators.NATURAL_LOG, "Natural Log" },
                { AdvancedOperators.EXPONENTIAL, "Exponential" },
                { AdvancedOperators.ABSOLUTE_VALUE, "Absolute Value" }
            };
        }

        #endregion

        #region Command Templates

        /// <summary>
        /// Pre-defined command templates for common operations
        /// </summary>
        public static class Templates
        {
            /// <summary>
            /// Template for basic addition operation
            /// </summary>
            public static List<string> BasicAddition => BuildBasicOperationCommands(
                Sources.CHANNEL1, Sources.CHANNEL2, Operators.ADD);

            /// <summary>
            /// Template for FFT analysis with default settings
            /// </summary>
            public static List<string> DefaultFFT => BuildFFTCommands(
                Sources.CHANNEL1, FFTWindows.HANNING, FFTSplitModes.FULL, FFTUnits.VRMS);

            /// <summary>
            /// Template for low-pass filter
            /// </summary>
            public static List<string> DefaultLowPassFilter => BuildFilterCommands(
                FilterTypes.LOW_PASS, 1000, 10000);

            /// <summary>
            /// Template for integration function
            /// </summary>
            public static List<string> DefaultIntegration => BuildAdvancedMathCommands(
                AdvancedOperators.INTEGRATION, 0, 100);
        }

        #endregion
    }
}