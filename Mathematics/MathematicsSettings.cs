using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Mathematics configuration settings model
    /// Handles all math function parameters, persistence, and validation
    /// </summary>
    public partial class MathematicsSettings : INotifyPropertyChanged
    {
        #region Private Fields

        private string _source1 = "CHANnel1";
        private string _source2 = "CHANnel2";
        private string _operation = "ADD";

        private string _fftSource = "CHANnel1";
        private string _fftWindow = "RECTangular";
        private string _fftSplit = "FULL";
        private string _fftUnit = "VRMS";

        private string _filterType = "LPASs";
        private string _filterW1 = "1000";
        private string _filterW2 = "10000";

        private string _advancedFunction = "INTG";
        private string _startPoint = "0";
        private string _endPoint = "100";

        private bool _mathDisplayEnabled = true;
        private bool _invertWaveform = false;
        private string _scale = "1.0";
        private string _offset = "0.0";

        private string _configurationName = "Default";
        private string _description = "";
        private DateTime _lastModified = DateTime.Now;

        // Active mode property for mutual exclusivity
        private string _activeMode = "BasicOperations";

        // Nested settings private fields
        private BasicOperationsSettings _basicOperations;
        private FFTAnalysisSettings _fftAnalysis;
        private DigitalFiltersSettings _digitalFilters;
        private AdvancedMathSettings _advancedMath;

        #endregion

        #region Basic Operations Properties

        /// <summary>
        /// First source channel for basic operations
        /// </summary>
        [JsonPropertyName("source1")]
        public string Source1
        {
            get => _source1;
            set => SetProperty(ref _source1, value);
        }

        /// <summary>
        /// Second source channel for basic operations
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

        #region Digital Filter Properties

        /// <summary>
        /// Digital filter type
        /// </summary>
        [JsonPropertyName("filterType")]
        public string FilterType
        {
            get => _filterType;
            set => SetProperty(ref _filterType, value);
        }

        /// <summary>
        /// Lower cutoff frequency for filters
        /// </summary>
        [JsonPropertyName("filterW1")]
        public string FilterW1
        {
            get => _filterW1;
            set => SetProperty(ref _filterW1, value);
        }

        /// <summary>
        /// Upper cutoff frequency for filters
        /// </summary>
        [JsonPropertyName("filterW2")]
        public string FilterW2
        {
            get => _filterW2;
            set => SetProperty(ref _filterW2, value);
        }

        #endregion

        #region Advanced Math Properties

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
        /// Start point for integration/advanced math operations
        /// </summary>
        [JsonPropertyName("startPoint")]
        public string StartPoint
        {
            get => _startPoint;
            set => SetProperty(ref _startPoint, value);
        }

        /// <summary>
        /// End point for integration/advanced math operations
        /// </summary>
        [JsonPropertyName("endPoint")]
        public string EndPoint
        {
            get => _endPoint;
            set => SetProperty(ref _endPoint, value);
        }

        #endregion

        #region Display Control Properties

        /// <summary>
        /// Enable or disable math display
        /// </summary>
        [JsonPropertyName("mathDisplayEnabled")]
        public bool MathDisplayEnabled
        {
            get => _mathDisplayEnabled;
            set => SetProperty(ref _mathDisplayEnabled, value);
        }

        /// <summary>
        /// Invert math waveform display
        /// </summary>
        [JsonPropertyName("invertWaveform")]
        public bool InvertWaveform
        {
            get => _invertWaveform;
            set => SetProperty(ref _invertWaveform, value);
        }

        /// <summary>
        /// Vertical scale for math waveform
        /// </summary>
        [JsonPropertyName("scale")]
        public string Scale
        {
            get => _scale;
            set => SetProperty(ref _scale, value);
        }

        /// <summary>
        /// Vertical offset for math waveform
        /// </summary>
        [JsonPropertyName("offset")]
        public string Offset
        {
            get => _offset;
            set => SetProperty(ref _offset, value);
        }

        #endregion

        #region Mode Control Properties

        /// <summary>
        /// Currently active mathematics mode (BasicOperations, FFTAnalysis, DigitalFilters, AdvancedMath)
        /// </summary>
        [JsonPropertyName("activeMode")]
        public string ActiveMode
        {
            get => _activeMode;
            set => SetProperty(ref _activeMode, value);
        }

        #endregion

        #region Configuration Metadata

        /// <summary>
        /// Name of the configuration
        /// </summary>
        [JsonPropertyName("configurationName")]
        public string ConfigurationName
        {
            get => _configurationName;
            set => SetProperty(ref _configurationName, value);
        }

        /// <summary>
        /// Description of the configuration
        /// </summary>
        [JsonPropertyName("description")]
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// Last modification timestamp
        /// </summary>
        [JsonPropertyName("lastModified")]
        public DateTime LastModified
        {
            get => _lastModified;
            set => SetProperty(ref _lastModified, value);
        }

        #endregion

        #region Nested Settings Properties

        /// <summary>
        /// Basic operations settings (ADD, SUB, MUL, DIV)
        /// </summary>
        [JsonPropertyName("basicOperations")]
        public BasicOperationsSettings BasicOperations
        {
            get => _basicOperations ??= new BasicOperationsSettings();
            set => SetProperty(ref _basicOperations, value);
        }

        /// <summary>
        /// FFT analysis settings
        /// </summary>
        [JsonPropertyName("fftAnalysis")]
        public FFTAnalysisSettings FFTAnalysis
        {
            get => _fftAnalysis ??= new FFTAnalysisSettings();
            set => SetProperty(ref _fftAnalysis, value);
        }

        /// <summary>
        /// Digital filters settings
        /// </summary>
        [JsonPropertyName("digitalFilters")]
        public DigitalFiltersSettings DigitalFilters
        {
            get => _digitalFilters ??= new DigitalFiltersSettings();
            set => SetProperty(ref _digitalFilters, value);
        }

        /// <summary>
        /// Advanced math settings
        /// </summary>
        [JsonPropertyName("advancedMath")]
        public AdvancedMathSettings AdvancedMath
        {
            get => _advancedMath ??= new AdvancedMathSettings();
            set => SetProperty(ref _advancedMath, value);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public MathematicsSettings()
        {
            // Initialize all nested settings to prevent null reference exceptions
            _basicOperations = new BasicOperationsSettings();
            _fftAnalysis = new FFTAnalysisSettings();
            _digitalFilters = new DigitalFiltersSettings();
            _advancedMath = new AdvancedMathSettings();
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// Property changed event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raise property changed notification
        /// </summary>
        /// <param name="propertyName">Property name</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Set property with change notification
        /// </summary>
        /// <typeparam name="T">Property type</typeparam>
        /// <param name="field">Field reference</param>
        /// <param name="value">New value</param>
        /// <param name="propertyName">Property name</param>
        /// <returns>True if value changed</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region Synchronization Methods

        /// <summary>
        /// Synchronize individual properties with nested settings
        /// Keeps backward compatibility with existing flat properties
        /// </summary>
        public void SynchronizeSettings()
        {
            // Sync Basic Operations
            if (BasicOperations != null)
            {
                BasicOperations.Source1 = Source1;
                BasicOperations.Source2 = Source2;
                BasicOperations.Operation = Operation;
            }

            // Sync FFT Analysis
            if (FFTAnalysis != null)
            {
                FFTAnalysis.Source = FFTSource;
                FFTAnalysis.Window = FFTWindow;
                FFTAnalysis.Split = FFTSplit;
                FFTAnalysis.Unit = FFTUnit;
            }

            // Sync Digital Filters
            if (DigitalFilters != null)
            {
                DigitalFilters.FilterType = FilterType;
                DigitalFilters.W1 = FilterW1;
                DigitalFilters.W2 = FilterW2;
            }

            // Sync Advanced Math
            if (AdvancedMath != null)
            {
                AdvancedMath.Function = AdvancedFunction;
                AdvancedMath.StartPoint = StartPoint;
                AdvancedMath.EndPoint = EndPoint;
            }
        }

        /// <summary>
        /// Update flat properties from nested settings
        /// For reverse synchronization
        /// </summary>
        public void UpdateFromNestedSettings()
        {
            // Update from Basic Operations
            if (BasicOperations != null)
            {
                Source1 = BasicOperations.Source1;
                Source2 = BasicOperations.Source2;
                Operation = BasicOperations.Operation;
            }

            // Update from FFT Analysis
            if (FFTAnalysis != null)
            {
                FFTSource = FFTAnalysis.Source;
                FFTWindow = FFTAnalysis.Window;
                FFTSplit = FFTAnalysis.Split;
                FFTUnit = FFTAnalysis.Unit;
            }

            // Update from Digital Filters
            if (DigitalFilters != null)
            {
                FilterType = DigitalFilters.FilterType;
                FilterW1 = DigitalFilters.W1;
                FilterW2 = DigitalFilters.W2;
            }

            // Update from Advanced Math
            if (AdvancedMath != null)
            {
                AdvancedFunction = AdvancedMath.Function;
                StartPoint = AdvancedMath.StartPoint;
                EndPoint = AdvancedMath.EndPoint;
            }
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validate current settings
        /// </summary>
        /// <returns>Validation results</returns>
        public ValidationResult ValidateSettings()
        {
            var result = new ValidationResult();

            try
            {
                // Validate sources
                if (!IsValidSource(Source1))
                    result.AddError("Source1", $"Invalid source: {Source1}");

                if (!IsValidSource(Source2))
                    result.AddError("Source2", $"Invalid source: {Source2}");

                // Validate operation
                if (!IsValidOperation(Operation))
                    result.AddError("Operation", $"Invalid operation: {Operation}");

                // Validate FFT settings
                if (!IsValidSource(FFTSource))
                    result.AddError("FFTSource", $"Invalid FFT source: {FFTSource}");

                // Validate filter frequencies
                if (!double.TryParse(FilterW1, out double w1) || w1 <= 0)
                    result.AddError("FilterW1", $"Invalid lower frequency: {FilterW1}");

                if (!double.TryParse(FilterW2, out double w2) || w2 <= 0)
                    result.AddError("FilterW2", $"Invalid upper frequency: {FilterW2}");

                if (w1 >= w2)
                    result.AddError("FilterFrequencies", "Lower frequency must be less than upper frequency");

                // Validate advanced math points
                if (!double.TryParse(StartPoint, out double start))
                    result.AddError("StartPoint", $"Invalid start point: {StartPoint}");

                if (!double.TryParse(EndPoint, out double end))
                    result.AddError("EndPoint", $"Invalid end point: {EndPoint}");

                if (start >= end)
                    result.AddError("MathPoints", "Start point must be less than end point");

                // Validate scale and offset
                if (!double.TryParse(Scale, out double scale) || scale <= 0)
                    result.AddError("Scale", $"Invalid scale: {Scale}");

                if (!double.TryParse(Offset, out _))
                    result.AddError("Offset", $"Invalid offset: {Offset}");

                // Validate active mode
                if (!IsValidActiveMode(ActiveMode))
                    result.AddError("ActiveMode", $"Invalid active mode: {ActiveMode}");
            }
            catch (Exception ex)
            {
                result.AddError("Validation", $"Validation error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validate settings and return simple error list
        /// </summary>
        /// <returns>List of validation errors</returns>
        public List<string> ValidateSimple()
        {
            var errors = new List<string>();

            try
            {
                // Validate sources
                if (!IsValidSource(Source1))
                    errors.Add($"Invalid Source 1: {Source1}");

                if (!IsValidSource(Source2))
                    errors.Add($"Invalid Source 2: {Source2}");

                // Validate operation
                if (!IsValidOperation(Operation))
                    errors.Add($"Invalid Operation: {Operation}");

                // Validate filter frequencies
                if (!double.TryParse(FilterW1, out double w1) || w1 <= 0)
                    errors.Add($"Invalid Lower Frequency: {FilterW1}");

                if (!double.TryParse(FilterW2, out double w2) || w2 <= 0)
                    errors.Add($"Invalid Upper Frequency: {FilterW2}");

                if (w1 >= w2)
                    errors.Add("Lower frequency must be less than upper frequency");

                // Validate advanced math points
                if (!double.TryParse(StartPoint, out double start))
                    errors.Add($"Invalid Start Point: {StartPoint}");

                if (!double.TryParse(EndPoint, out double end))
                    errors.Add($"Invalid End Point: {EndPoint}");

                if (start >= end)
                    errors.Add("Start point must be less than end point");

                // Validate scale and offset
                if (!double.TryParse(Scale, out double scale) || scale <= 0)
                    errors.Add($"Invalid Scale: {Scale}");

                if (!double.TryParse(Offset, out _))
                    errors.Add($"Invalid Offset: {Offset}");

                // Validate active mode
                if (!IsValidActiveMode(ActiveMode))
                    errors.Add($"Invalid Active Mode: {ActiveMode}");
            }
            catch (Exception ex)
            {
                errors.Add($"Validation error: {ex.Message}");
            }

            return errors;
        }

        private bool IsValidSource(string source)
        {
            return source == "CHANnel1" || source == "CHANnel2" ||
                   source == "CHANnel3" || source == "CHANnel4";
        }

        private bool IsValidOperation(string operation)
        {
            return operation == "ADD" || operation == "SUBtract" ||
                   operation == "MULtiply" || operation == "DIVide";
        }

        private bool IsValidActiveMode(string mode)
        {
            return mode == "BasicOperations" || mode == "FFTAnalysis" ||
                   mode == "DigitalFilters" || mode == "AdvancedMath";
        }

        #endregion

        #region Serialization Methods

        /// <summary>
        /// Save settings to JSON file
        /// </summary>
        /// <param name="filePath">File path to save to</param>
        public void SaveToFile(string filePath)
        {
            try
            {
                LastModified = DateTime.Now;
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load settings from JSON file
        /// </summary>
        /// <param name="filePath">File path to load from</param>
        /// <returns>Loaded settings</returns>
        public static MathematicsSettings LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"Settings file not found: {filePath}");

                var json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var settings = JsonSerializer.Deserialize<MathematicsSettings>(json, options);
                if (settings == null)
                    throw new InvalidOperationException("Failed to deserialize settings");

                // Ensure nested settings are initialized
                settings.SynchronizeSettings();

                return settings;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load settings: {ex.Message}", ex);
            }
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Create default settings for Basic Operations
        /// </summary>
        public static MathematicsSettings CreateBasicOperationsDefault()
        {
            var settings = new MathematicsSettings
            {
                ActiveMode = "BasicOperations",
                Source1 = "CHANnel1",
                Source2 = "CHANnel2",
                Operation = "ADD",
                ConfigurationName = "Basic Addition Default"
            };

            settings.SynchronizeSettings();
            return settings;
        }

        /// <summary>
        /// Create default settings for FFT Analysis
        /// </summary>
        public static MathematicsSettings CreateFFTAnalysisDefault()
        {
            var settings = new MathematicsSettings
            {
                ActiveMode = "FFTAnalysis",
                FFTSource = "CHANnel1",
                FFTWindow = "HANNing",
                FFTSplit = "FULL",
                FFTUnit = "VRMS",
                ConfigurationName = "FFT Analysis Default"
            };

            settings.SynchronizeSettings();
            return settings;
        }

        /// <summary>
        /// Create default settings for Digital Filters
        /// </summary>
        public static MathematicsSettings CreateDigitalFiltersDefault()
        {
            var settings = new MathematicsSettings
            {
                ActiveMode = "DigitalFilters",
                FilterType = "LPASs",
                FilterW1 = "1000",
                FilterW2 = "10000",
                ConfigurationName = "Low Pass Filter Default"
            };

            settings.SynchronizeSettings();
            return settings;
        }

        /// <summary>
        /// Create default settings for Advanced Math
        /// </summary>
        public static MathematicsSettings CreateAdvancedMathDefault()
        {
            var settings = new MathematicsSettings
            {
                ActiveMode = "AdvancedMath",
                AdvancedFunction = "INTG",
                StartPoint = "0",
                EndPoint = "100",
                ConfigurationName = "Integration Default"
            };

            settings.SynchronizeSettings();
            return settings;
        }

        /// <summary>
        /// Get factory preset configurations
        /// </summary>
        public static Dictionary<string, MathematicsSettings> GetFactoryPresets()
        {
            return new Dictionary<string, MathematicsSettings>
            {
                { "Basic Addition", CreateBasicOperationsDefault() },
                { "FFT Spectrum Analysis", CreateFFTAnalysisDefault() },
                { "Low Pass Filter", CreateDigitalFiltersDefault() },
                { "Signal Integration", CreateAdvancedMathDefault() },
                
                // Additional presets
                { "Signal Subtraction", new MathematicsSettings
                    {
                        ActiveMode = "BasicOperations",
                        Operation = "SUBtract",
                        ConfigurationName = "Signal Subtraction"
                    }
                },
                { "Power Calculation", new MathematicsSettings
                    {
                        ActiveMode = "BasicOperations",
                        Operation = "MULtiply",
                        ConfigurationName = "Power Calculation"
                    }
                },
                { "FFT with Hanning", new MathematicsSettings
                    {
                        ActiveMode = "FFTAnalysis",
                        FFTWindow = "HANNing",
                        FFTUnit = "DB",
                        ConfigurationName = "FFT Hanning dB"
                    }
                },
                { "High Pass Filter", new MathematicsSettings
                    {
                        ActiveMode = "DigitalFilters",
                        FilterType = "HPASs",
                        FilterW1 = "100",
                        FilterW2 = "1000",
                        ConfigurationName = "High Pass Filter"
                    }
                },
                { "Signal Differentiation", new MathematicsSettings
                    {
                        ActiveMode = "AdvancedMath",
                        AdvancedFunction = "DIFF",
                        ConfigurationName = "Signal Differentiation"
                    }
                }
            };
        }

        #endregion

        #region Comparison and Equality

        /// <summary>
        /// Check if settings are equal to another settings object
        /// </summary>
        /// <param name="other">Other settings to compare</param>
        /// <returns>True if equal</returns>
        public bool Equals(MathematicsSettings other)
        {
            if (other == null) return false;

            return Source1 == other.Source1 &&
                   Source2 == other.Source2 &&
                   Operation == other.Operation &&
                   FFTSource == other.FFTSource &&
                   FFTWindow == other.FFTWindow &&
                   FFTSplit == other.FFTSplit &&
                   FFTUnit == other.FFTUnit &&
                   FilterType == other.FilterType &&
                   FilterW1 == other.FilterW1 &&
                   FilterW2 == other.FilterW2 &&
                   AdvancedFunction == other.AdvancedFunction &&
                   StartPoint == other.StartPoint &&
                   EndPoint == other.EndPoint &&
                   MathDisplayEnabled == other.MathDisplayEnabled &&
                   InvertWaveform == other.InvertWaveform &&
                   Scale == other.Scale &&
                   Offset == other.Offset &&
                   ActiveMode == other.ActiveMode;
        }

        /// <summary>
        /// Create a deep copy of the settings
        /// </summary>
        /// <returns>Cloned settings</returns>
        public MathematicsSettings Clone()
        {
            try
            {
                var json = JsonSerializer.Serialize(this);
                var clone = JsonSerializer.Deserialize<MathematicsSettings>(json);
                clone?.SynchronizeSettings();
                return clone ?? new MathematicsSettings();
            }
            catch
            {
                return new MathematicsSettings();
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Validation result class for settings validation
    /// </summary>
    public class ValidationResult
    {
        private readonly Dictionary<string, List<string>> _errors = new();

        public bool IsValid => _errors.Count == 0;
        public Dictionary<string, List<string>> Errors => _errors;

        public void AddError(string property, string message)
        {
            if (!_errors.ContainsKey(property))
                _errors[property] = new List<string>();

            _errors[property].Add(message);
        }

        public string GetErrorSummary()
        {
            if (IsValid) return "All settings are valid.";

            var summary = new StringBuilder();
            summary.AppendLine("Validation errors found:");

            foreach (var kvp in _errors)
            {
                summary.AppendLine($"• {kvp.Key}: {string.Join(", ", kvp.Value)}");
            }

            return summary.ToString();
        }
    }

    #endregion
}