using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Controller for managing mathematics operations and SCPI command generation
    /// Handles all math functions available on Rigol DS1000Z-E oscilloscope
    /// </summary>
    public class MathematicsController
    {
        #region Events

        /// <summary>
        /// Event raised when a SCPI command is generated - FIXED TO USE SCPICommandEventArgs
        /// </summary>
        public event EventHandler<SCPICommandEventArgs> SCPICommandGenerated;

        /// <summary>
        /// Event raised when an error occurs - FIXED TO USE ErrorEventArgs
        /// </summary>
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// Event raised for logging/status updates - FIXED TO USE StatusEventArgs
        /// </summary>
        public event EventHandler<StatusEventArgs> StatusUpdated;

        #endregion

        #region Private Fields

        private readonly List<string> commandHistory;
        private MathematicsSettings currentSettings;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize the mathematics controller
        /// </summary>
        public MathematicsController()
        {
            commandHistory = new List<string>();
            currentSettings = new MathematicsSettings();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Get command history
        /// </summary>
        public IReadOnlyList<string> CommandHistory => commandHistory.AsReadOnly();

        /// <summary>
        /// Get current settings
        /// </summary>
        public MathematicsSettings CurrentSettings => currentSettings;

        #endregion

        #region Basic Operations

        /// <summary>
        /// Apply basic math operation (ADD, SUB, MUL, DIV)
        /// </summary>
        /// <param name="source1">First source channel</param>
        /// <param name="source2">Second source channel</param>
        /// <param name="operation">Math operation</param>
        /// <returns>Generated command string</returns>
        public string ApplyBasicOperation(string source1, string source2, string operation)
        {
            try
            {
                // Validate inputs
                if (!MathematicsCommands.IsValidSource(source1))
                    throw new ArgumentException($"Invalid source1: {source1}");

                if (!MathematicsCommands.IsValidSource(source2))
                    throw new ArgumentException($"Invalid source2: {source2}");

                if (!MathematicsCommands.IsValidOperator(operation))
                    throw new ArgumentException($"Invalid operation: {operation}");

                // Generate commands
                var commands = MathematicsCommands.BuildBasicOperationCommands(source1, source2, operation);
                var commandString = MathematicsCommands.CombineCommands(commands);

                // Update settings
                currentSettings.Source1 = source1;
                currentSettings.Source2 = source2;
                currentSettings.Operation = operation;

                // Log and execute
                LogCommand("Basic Operation", commandString);
                SendCommand(commandString);

                OnStatusUpdated($"Applied basic operation: {operation} with {source1} and {source2}");
                return commandString;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying basic operation: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region FFT Analysis

        /// <summary>
        /// Apply FFT analysis
        /// </summary>
        /// <param name="source">FFT source channel</param>
        /// <param name="window">Windowing function</param>
        /// <param name="split">Display mode</param>
        /// <param name="unit">Measurement unit</param>
        /// <returns>Generated command string</returns>
        public string ApplyFFTAnalysis(string source, string window, string split, string unit)
        {
            try
            {
                // Validate inputs
                if (!MathematicsCommands.IsValidSource(source))
                    throw new ArgumentException($"Invalid FFT source: {source}");

                if (!MathematicsCommands.IsValidFFTWindow(window))
                    throw new ArgumentException($"Invalid FFT window: {window}");

                // Validate split and unit (basic validation)
                if (string.IsNullOrWhiteSpace(split))
                    throw new ArgumentException("FFT split mode cannot be empty");

                if (string.IsNullOrWhiteSpace(unit))
                    throw new ArgumentException("FFT unit cannot be empty");

                // Generate commands
                var commands = MathematicsCommands.BuildFFTCommands(source, window, split, unit);
                var commandString = MathematicsCommands.CombineCommands(commands);

                // Update settings
                currentSettings.FFTSource = source;
                currentSettings.FFTWindow = window;
                currentSettings.FFTSplit = split;
                currentSettings.FFTUnit = unit;

                // Log and execute
                LogCommand("FFT Analysis", commandString);
                SendCommand(commandString);

                OnStatusUpdated($"Applied FFT analysis on {source} with {window} window");
                return commandString;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying FFT analysis: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Set FFT horizontal scale
        /// </summary>
        /// <param name="scale">Horizontal scale value</param>
        /// <returns>Generated command string</returns>
        public string SetFFTHorizontalScale(double scale)
        {
            try
            {
                if (!MathematicsCommands.IsValidScale(scale))
                    throw new ArgumentException($"Invalid FFT horizontal scale: {scale}");

                var command = $"{MathematicsCommands.MATH_FFT_HSCALE} {scale.ToString("G", CultureInfo.InvariantCulture)}";

                LogCommand("FFT H-Scale", command);
                SendCommand(command);

                OnStatusUpdated($"Set FFT horizontal scale to {scale}");
                return command;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error setting FFT horizontal scale: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Set FFT center frequency
        /// </summary>
        /// <param name="centerFreq">Center frequency value</param>
        /// <returns>Generated command string</returns>
        public string SetFFTCenterFrequency(double centerFreq)
        {
            try
            {
                if (!MathematicsCommands.IsValidFrequency(centerFreq))
                    throw new ArgumentException($"Invalid FFT center frequency: {centerFreq}");

                var command = $"{MathematicsCommands.MATH_FFT_HCENTER} {centerFreq.ToString("G", CultureInfo.InvariantCulture)}";

                LogCommand("FFT Center", command);
                SendCommand(command);

                OnStatusUpdated($"Set FFT center frequency to {centerFreq} Hz");
                return command;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error setting FFT center frequency: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Digital Filters

        /// <summary>
        /// Apply digital filter
        /// </summary>
        /// <param name="filterType">Type of filter</param>
        /// <param name="w1">Lower cutoff frequency</param>
        /// <param name="w2">Upper cutoff frequency</param>
        /// <returns>Generated command string</returns>
        public string ApplyDigitalFilter(string filterType, string w1Text, string w2Text)
        {
            try
            {
                // Validate filter type
                if (!MathematicsCommands.IsValidFilterType(filterType))
                    throw new ArgumentException($"Invalid filter type: {filterType}");

                // Parse frequencies
                var w1 = MathematicsCommands.ParseNumericValue(w1Text, 1000);
                var w2 = MathematicsCommands.ParseNumericValue(w2Text, 10000);

                // Validate frequencies
                if (!MathematicsCommands.IsValidFrequency(w1))
                    throw new ArgumentException($"Invalid lower frequency: {w1Text}");

                if (!MathematicsCommands.IsValidFrequency(w2))
                    throw new ArgumentException($"Invalid upper frequency: {w2Text}");

                // For band pass and band stop, ensure w1 < w2
                if ((filterType == MathematicsCommands.FilterTypes.BAND_PASS ||
                     filterType == MathematicsCommands.FilterTypes.BAND_STOP) && w1 >= w2)
                {
                    throw new ArgumentException("Lower frequency must be less than upper frequency for band filters");
                }

                // Generate commands
                var commands = MathematicsCommands.BuildFilterCommands(filterType, w1, w2);
                var commandString = MathematicsCommands.CombineCommands(commands);

                // Update settings
                currentSettings.FilterType = filterType;
                currentSettings.FilterW1 = w1Text;
                currentSettings.FilterW2 = w2Text;

                // Log and execute
                LogCommand("Digital Filter", commandString);
                SendCommand(commandString);

                OnStatusUpdated($"Applied {filterType} filter: {w1} Hz to {w2} Hz");
                return commandString;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying digital filter: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Advanced Math Functions

        /// <summary>
        /// Apply advanced math function
        /// </summary>
        /// <param name="function">Advanced function type</param>
        /// <param name="startPointText">Start point for analysis</param>
        /// <param name="endPointText">End point for analysis</param>
        /// <returns>Generated command string</returns>
        public string ApplyAdvancedMathFunction(string function, string startPointText, string endPointText)
        {
            try
            {
                // Validate function
                if (!MathematicsCommands.IsValidAdvancedOperator(function))
                    throw new ArgumentException($"Invalid advanced function: {function}");

                // Parse points
                var startPoint = MathematicsCommands.ParseNumericValue(startPointText, 0);
                var endPoint = MathematicsCommands.ParseNumericValue(endPointText, 100);

                // Validate points (for integration, start should be less than end)
                if (function == MathematicsCommands.AdvancedOperators.INTEGRATION && startPoint >= endPoint)
                {
                    throw new ArgumentException("Start point must be less than end point for integration");
                }

                // Generate commands
                var commands = MathematicsCommands.BuildAdvancedMathCommands(function, startPoint, endPoint);
                var commandString = MathematicsCommands.CombineCommands(commands);

                // Update settings
                currentSettings.AdvancedFunction = function;
                currentSettings.StartPoint = startPointText;
                currentSettings.EndPoint = endPointText;

                // Log and execute
                LogCommand("Advanced Math", commandString);
                SendCommand(commandString);

                OnStatusUpdated($"Applied advanced function: {function} from {startPoint} to {endPoint}");
                return commandString;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying advanced math function: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Display Control

        /// <summary>
        /// Update math display settings
        /// </summary>
        /// <param name="enableDisplay">Enable math display</param>
        /// <param name="invert">Invert waveform</param>
        /// <param name="scaleText">Vertical scale</param>
        /// <param name="offsetText">Vertical offset</param>
        /// <returns>Generated command string</returns>
        public string UpdateDisplaySettings(bool enableDisplay, bool invert, string scaleText, string offsetText)
        {
            try
            {
                // Parse scale and offset
                var scale = MathematicsCommands.ParseNumericValue(scaleText, 1.0);
                var offset = MathematicsCommands.ParseNumericValue(offsetText, 0.0);

                // Validate scale (must be positive)
                if (!MathematicsCommands.IsValidScale(scale))
                    throw new ArgumentException($"Invalid scale value: {scaleText}");

                // Generate commands
                var commands = MathematicsCommands.BuildDisplayControlCommands(enableDisplay, invert, scale, offset);
                var commandString = MathematicsCommands.CombineCommands(commands);

                // Update settings
                currentSettings.MathDisplayEnabled = enableDisplay;
                currentSettings.InvertWaveform = invert;
                currentSettings.Scale = scaleText;
                currentSettings.Offset = offsetText;

                // Log and execute
                LogCommand("Display Control", commandString);
                SendCommand(commandString);

                OnStatusUpdated($"Updated display: Enable={enableDisplay}, Invert={invert}, Scale={scale}, Offset={offset}");
                return commandString;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error updating display settings: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Enable or disable math display
        /// </summary>
        /// <param name="enable">True to enable, false to disable</param>
        /// <returns>Generated command string</returns>
        public string SetMathDisplay(bool enable)
        {
            try
            {
                var command = MathematicsCommands.BuildDisplayCommand(enable);

                currentSettings.MathDisplayEnabled = enable;

                LogCommand("Math Display", command);
                SendCommand(command);

                OnStatusUpdated($"Math display {(enable ? "enabled" : "disabled")}");
                return command;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error setting math display: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Reset and Utility

        /// <summary>
        /// Reset all math settings to defaults
        /// </summary>
        /// <returns>Generated command string</returns>
        public string ResetMathSettings()
        {
            try
            {
                var command = MathematicsCommands.BuildResetCommand();

                // Reset local settings to defaults
                currentSettings = new MathematicsSettings();

                LogCommand("Reset Math", command);
                SendCommand(command);

                OnStatusUpdated("All math settings reset to defaults");
                return command;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error resetting math settings: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Apply quick preset configuration
        /// </summary>
        /// <param name="presetName">Name of the preset</param>
        /// <returns>Generated command string</returns>
        public string ApplyPreset(string presetName)
        {
            try
            {
                List<string> commands;

                switch (presetName?.ToUpperInvariant())
                {
                    case "BASIC_ADD":
                        commands = MathematicsCommands.Templates.BasicAddition;
                        break;
                    case "DEFAULT_FFT":
                        commands = MathematicsCommands.Templates.DefaultFFT;
                        break;
                    case "LOW_PASS_FILTER":
                        commands = MathematicsCommands.Templates.DefaultLowPassFilter;
                        break;
                    case "INTEGRATION":
                        commands = MathematicsCommands.Templates.DefaultIntegration;
                        break;
                    default:
                        throw new ArgumentException($"Unknown preset: {presetName}");
                }

                var commandString = MathematicsCommands.CombineCommands(commands);

                LogCommand($"Preset: {presetName}", commandString);
                SendCommand(commandString);

                OnStatusUpdated($"Applied preset configuration: {presetName}");
                return commandString;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying preset: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Command History and Management

        /// <summary>
        /// Get formatted command history
        /// </summary>
        /// <returns>Formatted command history string</returns>
        public string GetCommandHistory()
        {
            if (commandHistory.Count == 0)
                return "No commands executed yet.";

            var sb = new StringBuilder();
            sb.AppendLine($"=== Math Command History ({commandHistory.Count} commands) ===");

            for (int i = 0; i < commandHistory.Count; i++)
            {
                sb.AppendLine($"{i + 1:D3}: {commandHistory[i]}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Clear command history
        /// </summary>
        public void ClearCommandHistory()
        {
            commandHistory.Clear();
            OnStatusUpdated("Command history cleared");
        }

        /// <summary>
        /// Get last executed command
        /// </summary>
        /// <returns>Last command or empty string if none</returns>
        public string GetLastCommand()
        {
            return commandHistory.Count > 0 ? commandHistory.Last() : string.Empty;
        }

        #endregion

        #region Query Operations

        /// <summary>
        /// Generate query commands to read current oscilloscope settings
        /// </summary>
        /// <returns>List of query commands</returns>
        public List<string> GenerateQueryCommands()
        {
            var queries = new List<string>
            {
                MathematicsCommands.Queries.MATH_DISPLAY_QUERY,
                MathematicsCommands.Queries.MATH_OPERATOR_QUERY,
                MathematicsCommands.Queries.MATH_SOURCE1_QUERY,
                MathematicsCommands.Queries.MATH_SOURCE2_QUERY,
                MathematicsCommands.Queries.MATH_SCALE_QUERY,
                MathematicsCommands.Queries.MATH_OFFSET_QUERY,
                MathematicsCommands.Queries.MATH_FFT_SOURCE_QUERY,
                MathematicsCommands.Queries.MATH_FFT_WINDOW_QUERY,
                MathematicsCommands.Queries.MATH_FILTER_TYPE_QUERY
            };

            return queries;
        }

        #endregion

        #region Settings Management

        /// <summary>
        /// Load settings into controller
        /// </summary>
        /// <param name="settings">Settings to load</param>
        public void LoadSettings(MathematicsSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            currentSettings = settings;
            OnStatusUpdated("Settings loaded into controller");
        }

        /// <summary>
        /// Export current settings
        /// </summary>
        /// <returns>Current settings object</returns>
        public MathematicsSettings ExportSettings()
        {
            return currentSettings;
        }

        /// <summary>
        /// Validate current settings
        /// </summary>
        /// <returns>List of validation errors (empty if valid)</returns>
        public List<string> ValidateCurrentSettings()
        {
            var errors = new List<string>();

            if (!MathematicsCommands.IsValidSource(currentSettings.Source1))
                errors.Add($"Invalid Source1: {currentSettings.Source1}");

            if (!MathematicsCommands.IsValidSource(currentSettings.Source2))
                errors.Add($"Invalid Source2: {currentSettings.Source2}");

            if (!MathematicsCommands.IsValidOperator(currentSettings.Operation))
                errors.Add($"Invalid Operation: {currentSettings.Operation}");

            if (!MathematicsCommands.IsValidFFTWindow(currentSettings.FFTWindow))
                errors.Add($"Invalid FFT Window: {currentSettings.FFTWindow}");

            if (!MathematicsCommands.IsValidFilterType(currentSettings.FilterType))
                errors.Add($"Invalid Filter Type: {currentSettings.FilterType}");

            if (!MathematicsCommands.IsValidAdvancedOperator(currentSettings.AdvancedFunction))
                errors.Add($"Invalid Advanced Function: {currentSettings.AdvancedFunction}");

            // Validate numeric values
            var scale = MathematicsCommands.ParseNumericValue(currentSettings.Scale, 1.0);
            if (!MathematicsCommands.IsValidScale(scale))
                errors.Add($"Invalid Scale: {currentSettings.Scale}");

            return errors;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Log command to history
        /// </summary>
        /// <param name="operation">Operation name</param>
        /// <param name="command">Command string</param>
        private void LogCommand(string operation, string command)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"[{timestamp}] {operation}: {command}";
            commandHistory.Add(logEntry);

            // Keep history size manageable (last 100 commands)
            if (commandHistory.Count > 100)
            {
                commandHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Send command and raise event - FIXED TO USE SCPICommandEventArgs
        /// </summary>
        /// <param name="command">Command to send</param>
        private void SendCommand(string command)
        {
            var eventArgs = new SCPICommandEventArgs(command, "MathematicsController", "MATH");
            SCPICommandGenerated?.Invoke(this, eventArgs);
        }

        #endregion

        #region Event Helpers

        /// <summary>
        /// Raise error occurred event
        /// </summary>
        /// <param name="error">Error message</param>
        protected virtual void OnErrorOccurred(string error)
        {
            ErrorOccurred?.Invoke(this, error);
        }

        /// <summary>
        /// Raise status updated event - FIXED TO USE StatusEventArgs
        /// </summary>
        /// <param name="status">Status message</param>
        protected virtual void OnStatusUpdated(string status)
        {
            var eventArgs = new StatusEventArgs(status, StatusLevel.Info, "MathematicsController", "MATH");
            StatusUpdated?.Invoke(this, eventArgs);
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Dispose of controller resources
        /// </summary>
        public void Dispose()
        {
            commandHistory?.Clear();
            currentSettings = null;
        }

        #endregion
    }
}
