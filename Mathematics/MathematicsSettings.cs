using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Mathematics configuration settings model
    /// Handles all math function parameters, persistence, and validation
    /// </summary>
    public class MathematicsSettings : INotifyPropertyChanged
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

        // ADDED: Active mode property for mutual exclusivity
        private string _activeMode = "BasicOperations";

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
        /// ADDED: For mutual exclusivity control
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

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
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

            // Validate scale and offset
            if (!double.TryParse(Scale, out double scale) || scale <= 0)
                result.AddError("Scale", $"Invalid scale: {Scale}");

            if (!double.TryParse(Offset, out _))
                result.AddError("Offset", $"Invalid offset: {Offset}");

            // Validate active mode
            if (!IsValidActiveMode(ActiveMode))
                result.AddError("ActiveMode", $"Invalid active mode: {ActiveMode}");

            return result;
        }

        /// <summary>
        /// Simple validation that returns list of errors - Alternative method for compatibility
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> ValidateSettingsSimple()
        {
            var errors = new List<string>();

            // Validate sources
            if (!IsValidSource(Source1))
                errors.Add($"Invalid Source1: {Source1}");

            if (!IsValidSource(Source2))
                errors.Add($"Invalid Source2: {Source2}");

            // Validate operation
            if (!IsValidOperation(Operation))
                errors.Add($"Invalid Operation: {Operation}");

            // Validate FFT settings
            if (!IsValidSource(FFTSource))
                errors.Add($"Invalid FFT Source: {FFTSource}");

            // Validate filter frequencies
            if (!double.TryParse(FilterW1, out double w1) || w1 <= 0)
                errors.Add($"Invalid Filter W1: {FilterW1}");

            if (!double.TryParse(FilterW2, out double w2) || w2 <= 0)
                errors.Add($"Invalid Filter W2: {FilterW2}");

            if (double.TryParse(FilterW1, out w1) && double.TryParse(FilterW2, out w2) && w1 >= w2)
                errors.Add("Lower frequency must be less than upper frequency");

            // Validate advanced math points
            if (!double.TryParse(StartPoint, out _))
                errors.Add($"Invalid Start Point: {StartPoint}");

            if (!double.TryParse(EndPoint, out _))
                errors.Add($"Invalid End Point: {EndPoint}");

            // Validate scale and offset
            if (!double.TryParse(Scale, out double scale) || scale <= 0)
                errors.Add($"Invalid Scale: {Scale}");

            if (!double.TryParse(Offset, out _))
                errors.Add($"Invalid Offset: {Offset}");

            // Validate active mode
            if (!IsValidActiveMode(ActiveMode))
                errors.Add($"Invalid Active Mode: {ActiveMode}");

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
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
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
                return JsonSerializer.Deserialize<MathematicsSettings>(json) ?? new MathematicsSettings();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load settings: {ex.Message}", ex);
            }
        }

        #endregion

        #region Clone and Copy Methods

        /// <summary>
        /// Create a deep copy of the current settings - Required by MathematicsWindow
        /// </summary>
        public MathematicsSettings Clone()
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = false });
                var clone = JsonSerializer.Deserialize<MathematicsSettings>(json) ?? new MathematicsSettings();
                clone.LastModified = DateTime.Now;
                return clone;
            }
            catch (Exception)
            {
                // Fallback to manual copy if JSON serialization fails
                return new MathematicsSettings
                {
                    Source1 = this.Source1,
                    Source2 = this.Source2,
                    Operation = this.Operation,
                    FFTSource = this.FFTSource,
                    FFTWindow = this.FFTWindow,
                    FFTSplit = this.FFTSplit,
                    FFTUnit = this.FFTUnit,
                    FilterType = this.FilterType,
                    FilterW1 = this.FilterW1,
                    FilterW2 = this.FilterW2,
                    AdvancedFunction = this.AdvancedFunction,
                    StartPoint = this.StartPoint,
                    EndPoint = this.EndPoint,
                    MathDisplayEnabled = this.MathDisplayEnabled,
                    InvertWaveform = this.InvertWaveform,
                    Scale = this.Scale,
                    Offset = this.Offset,
                    ActiveMode = this.ActiveMode,
                    ConfigurationName = this.ConfigurationName + " (Copy)",
                    Description = this.Description,
                    LastModified = DateTime.Now
                };
            }
        }

        /// <summary>
        /// Check if this settings object is equivalent to another - Required by MathematicsWindow
        /// </summary>
        public bool IsEquivalentTo(MathematicsSettings other)
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
        public static MathematicsSettings CreateDigitalFiltersDefault()
        {
            return new MathematicsSettings
            {
                ActiveMode = "DigitalFilters",
                FilterType = "LPASs",
                FilterW1 = "1000",
                FilterW2 = "10000",
                ConfigurationName = "Low Pass Filter Default"
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

        /// <summary>
        /// Get factory preset configurations - Required by MathematicsWindow
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
    }

    /// <summary>
    /// Validation result class for settings validation
    /// ADDED: For proper validation feedback
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

            var summary = new System.Text.StringBuilder();
            summary.AppendLine("Validation errors found:");

            foreach (var kvp in _errors)
            {
                summary.AppendLine($"• {kvp.Key}: {string.Join(", ", kvp.Value)}");
            }

            return summary.ToString();
        }
    }


    public partial class MathematicsSettings : INotifyPropertyChanged
    {
        #region ADDED: Missing Nested Settings Properties

        // Private fields for nested settings
        private BasicOperationsSettings _basicOperations;
        private FFTAnalysisSettings _fftAnalysis;
        private DigitalFiltersSettings _digitalFilters;
        private AdvancedMathSettings _advancedMath;

        /// <summary>
        /// Basic operations settings (ADD, SUB, MUL, DIV)
        /// ADDED: Missing property that MathematicsWindow expects
        /// </summary>
        [JsonPropertyName("basicOperations")]
        public BasicOperationsSettings BasicOperations
        {
            get => _basicOperations ??= new BasicOperationsSettings();
            set => SetProperty(ref _basicOperations, value);
        }

        /// <summary>
        /// FFT analysis settings
        /// ADDED: Missing property that MathematicsWindow expects
        /// </summary>
        [JsonPropertyName("fftAnalysis")]
        public FFTAnalysisSettings FFTAnalysis
        {
            get => _fftAnalysis ??= new FFTAnalysisSettings();
            set => SetProperty(ref _fftAnalysis, value);
        }

        /// <summary>
        /// Digital filters settings
        /// ADDED: Missing property that MathematicsWindow expects
        /// </summary>
        [JsonPropertyName("digitalFilters")]
        public DigitalFiltersSettings DigitalFilters
        {
            get => _digitalFilters ??= new DigitalFiltersSettings();
            set => SetProperty(ref _digitalFilters, value);
        }

        /// <summary>
        /// Advanced math settings
        /// ADDED: Missing property that MathematicsWindow expects
        /// </summary>
        [JsonPropertyName("advancedMath")]
        public AdvancedMathSettings AdvancedMath
        {
            get => _advancedMath ??= new AdvancedMathSettings();
            set => SetProperty(ref _advancedMath, value);
        }

        #endregion

        #region UPDATED: Constructor

        /// <summary>
        /// Default constructor - UPDATED to initialize nested settings
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

        #region UPDATED: Synchronization Methods

        /// <summary>
        /// Synchronize individual properties with nested settings
        /// ADDED: Keep backward compatibility with existing flat properties
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
        /// ADDED: For reverse synchronization
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

        #region UPDATED: Factory Methods

        /// <summary>
        /// Create default settings for Basic Operations - UPDATED
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
        /// Create default settings for FFT Analysis - UPDATED
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
        /// Create default settings for Digital Filters - UPDATED
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
        /// Create default settings for Advanced Math - UPDATED
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

        #endregion

        #region FIXED: Property Change Notifications

        /// <summary>
        /// Set property with change notification - FIXED to handle all types
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Property changed notification
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Property changed event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }

}