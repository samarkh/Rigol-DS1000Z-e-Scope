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
        /// Basic math operation (ADD, SUBtract, MULtiply, DIVide)
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
        /// FFT source channel
        /// </summary>
        [JsonPropertyName("fftSource")]
        public string FFTSource
        {
            get => _fftSource;
            set => SetProperty(ref _fftSource, value);
        }

        /// <summary>
        /// FFT windowing function (RECTangular, BLACkman, HANNing, HAMMing)
        /// </summary>
        [JsonPropertyName("fftWindow")]
        public string FFTWindow
        {
            get => _fftWindow;
            set => SetProperty(ref _fftWindow, value);
        }

        /// <summary>
        /// FFT display mode (FULL, CENTer)
        /// </summary>
        [JsonPropertyName("fftSplit")]
        public string FFTSplit
        {
            get => _fftSplit;
            set => SetProperty(ref _fftSplit, value);
        }

        /// <summary>
        /// FFT measurement unit (VRMS, DB)
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
        /// Digital filter type (LPASs, HPASs, BPASs, BSTop)
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

        #region Metadata Properties

        /// <summary>
        /// Configuration name for identification
        /// </summary>
        [JsonPropertyName("configurationName")]
        public string ConfigurationName
        {
            get => _configurationName;
            set => SetProperty(ref _configurationName, value);
        }

        /// <summary>
        /// Description of this configuration
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

        /// <summary>
        /// Version of the settings format
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        #endregion

        #region Constructors

        /// <summary>
        /// Initialize with default settings
        /// </summary>
        public MathematicsSettings()
        {
            ResetToDefaults();
        }

        /// <summary>
        /// Initialize with custom configuration name
        /// </summary>
        /// <param name="configName">Configuration name</param>
        public MathematicsSettings(string configName) : this()
        {
            ConfigurationName = configName ?? "Custom";
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="source">Source settings to copy</param>
        public MathematicsSettings(MathematicsSettings source) : this()
        {
            if (source != null)
            {
                CopyFrom(source);
            }
        }

        #endregion

        #region Default Settings Management

        /// <summary>
        /// Reset all settings to default values
        /// </summary>
        public void ResetToDefaults()
        {
            // Basic operations defaults
            Source1 = "CHANnel1";
            Source2 = "CHANnel2";
            Operation = "ADD";

            // FFT defaults
            FFTSource = "CHANnel1";
            FFTWindow = "RECTangular";
            FFTSplit = "FULL";
            FFTUnit = "VRMS";

            // Filter defaults
            FilterType = "LPASs";
            FilterW1 = "1000";
            FilterW2 = "10000";

            // Advanced math defaults
            AdvancedFunction = "INTG";
            StartPoint = "0";
            EndPoint = "100";

            // Display defaults
            MathDisplayEnabled = true;
            InvertWaveform = false;
            Scale = "1.0";
            Offset = "0.0";

            // Metadata defaults
            ConfigurationName = "Default";
            Description = "Default mathematics configuration";
            LastModified = DateTime.Now;
        }

        /// <summary>
        /// Get factory preset configurations
        /// </summary>
        /// <returns>Dictionary of preset name and settings</returns>
        public static Dictionary<string, MathematicsSettings> GetFactoryPresets()
        {
            var presets = new Dictionary<string, MathematicsSettings>();

            // Basic Addition Preset
            var basicAdd = new MathematicsSettings("Basic Addition")
            {
                Operation = "ADD",
                Source1 = "CHANnel1",
                Source2 = "CHANnel2",
                Description = "Simple addition of Channel 1 and Channel 2"
            };
            presets.Add("Basic Addition", basicAdd);

            // FFT Analysis Preset
            var fftAnalysis = new MathematicsSettings("FFT Analysis")
            {
                FFTSource = "CHANnel1",
                FFTWindow = "HANNing",
                FFTSplit = "FULL",
                FFTUnit = "DB",
                Description = "FFT analysis with Hanning window in dB"
            };
            presets.Add("FFT Analysis", fftAnalysis);

            // Low Pass Filter Preset
            var lowPassFilter = new MathematicsSettings("Low Pass Filter")
            {
                FilterType = "LPASs",
                FilterW1 = "1000",
                FilterW2 = "5000",
                Description = "Low pass filter with 1kHz to 5kHz range"
            };
            presets.Add("Low Pass Filter", lowPassFilter);

            // Integration Preset
            var integration = new MathematicsSettings("Integration")
            {
                AdvancedFunction = "INTG",
                StartPoint = "0",
                EndPoint = "100",
                Description = "Integration from 0 to 100"
            };
            presets.Add("Integration", integration);

            return presets;
        }

        #endregion

        #region File Operations

        /// <summary>
        /// Save settings to JSON file
        /// </summary>
        /// <param name="filePath">Path to save file</param>
        public void SaveToFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty", nameof(filePath));

            try
            {
                // Update metadata before saving
                LastModified = DateTime.Now;

                // Configure JSON options
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                // Serialize to JSON
                var json = JsonSerializer.Serialize(this, options);

                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write to file
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save mathematics settings to '{filePath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load settings from JSON file
        /// </summary>
        /// <param name="filePath">Path to load file</param>
        /// <returns>Loaded settings object</returns>
        public static MathematicsSettings LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Settings file not found: {filePath}");

            try
            {
                // Read file content
                var json = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json))
                    throw new InvalidDataException("Settings file is empty");

                // Configure JSON options
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                // Deserialize from JSON
                var settings = JsonSerializer.Deserialize<MathematicsSettings>(json, options);

                if (settings == null)
                    throw new InvalidDataException("Failed to deserialize settings from file");

                // Validate loaded settings
                var validationErrors = settings.ValidateSettings();
                if (validationErrors.Count > 0)
                {
                    // Log validation errors but don't fail - use defaults for invalid values
                    settings.FixInvalidSettings();
                }

                return settings;
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Invalid JSON in settings file '{filePath}': {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load mathematics settings from '{filePath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Save settings to memory stream (for backup/undo functionality)
        /// </summary>
        /// <returns>Memory stream containing serialized settings</returns>
        public MemoryStream SaveToMemoryStream()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(this, options);
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.Write(json);
                writer.Flush();
                stream.Position = 0;
                return stream;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save settings to memory stream: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load settings from memory stream
        /// </summary>
        /// <param name="stream">Memory stream containing serialized settings</param>
        /// <returns>Loaded settings object</returns>
        public static MathematicsSettings LoadFromMemoryStream(MemoryStream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                stream.Position = 0;
                var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<MathematicsSettings>(json, options) ?? new MathematicsSettings();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load settings from memory stream: {ex.Message}", ex);
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate all settings values
        /// </summary>
        /// <returns>List of validation errors (empty if valid)</returns>
        public List<string> ValidateSettings()
        {
            var errors = new List<string>();

            // Validate basic operations
            if (!MathematicsCommands.IsValidSource(Source1))
                errors.Add($"Invalid Source1: {Source1}");

            if (!MathematicsCommands.IsValidSource(Source2))
                errors.Add($"Invalid Source2: {Source2}");

            if (!MathematicsCommands.IsValidOperator(Operation))
                errors.Add($"Invalid Operation: {Operation}");

            // Validate FFT settings
            if (!MathematicsCommands.IsValidSource(FFTSource))
                errors.Add($"Invalid FFT Source: {FFTSource}");

            if (!MathematicsCommands.IsValidFFTWindow(FFTWindow))
                errors.Add($"Invalid FFT Window: {FFTWindow}");

            // Validate filter settings
            if (!MathematicsCommands.IsValidFilterType(FilterType))
                errors.Add($"Invalid Filter Type: {FilterType}");

            if (!IsValidNumericString(FilterW1))
                errors.Add($"Invalid Filter W1: {FilterW1}");

            if (!IsValidNumericString(FilterW2))
                errors.Add($"Invalid Filter W2: {FilterW2}");

            // Validate advanced math
            if (!MathematicsCommands.IsValidAdvancedOperator(AdvancedFunction))
                errors.Add($"Invalid Advanced Function: {AdvancedFunction}");

            if (!IsValidNumericString(StartPoint))
                errors.Add($"Invalid Start Point: {StartPoint}");

            if (!IsValidNumericString(EndPoint))
                errors.Add($"Invalid End Point: {EndPoint}");

            // Validate display settings
            if (!IsValidNumericString(Scale))
                errors.Add($"Invalid Scale: {Scale}");

            if (!IsValidNumericString(Offset))
                errors.Add($"Invalid Offset: {Offset}");

            // Validate scale is positive
            if (IsValidNumericString(Scale))
            {
                var scaleValue = MathematicsCommands.ParseNumericValue(Scale);
                if (!MathematicsCommands.IsValidScale(scaleValue))
                    errors.Add($"Scale must be positive: {Scale}");
            }

            return errors;
        }

        /// <summary>
        /// Fix invalid settings by replacing with defaults
        /// </summary>
        public void FixInvalidSettings()
        {
            var defaultSettings = new MathematicsSettings();

            // Fix basic operations
            if (!MathematicsCommands.IsValidSource(Source1))
                Source1 = defaultSettings.Source1;

            if (!MathematicsCommands.IsValidSource(Source2))
                Source2 = defaultSettings.Source2;

            if (!MathematicsCommands.IsValidOperator(Operation))
                Operation = defaultSettings.Operation;

            // Fix FFT settings
            if (!MathematicsCommands.IsValidSource(FFTSource))
                FFTSource = defaultSettings.FFTSource;

            if (!MathematicsCommands.IsValidFFTWindow(FFTWindow))
                FFTWindow = defaultSettings.FFTWindow;

            // Fix filter settings
            if (!MathematicsCommands.IsValidFilterType(FilterType))
                FilterType = defaultSettings.FilterType;

            if (!IsValidNumericString(FilterW1))
                FilterW1 = defaultSettings.FilterW1;

            if (!IsValidNumericString(FilterW2))
                FilterW2 = defaultSettings.FilterW2;

            // Fix advanced math
            if (!MathematicsCommands.IsValidAdvancedOperator(AdvancedFunction))
                AdvancedFunction = defaultSettings.AdvancedFunction;

            if (!IsValidNumericString(StartPoint))
                StartPoint = defaultSettings.StartPoint;

            if (!IsValidNumericString(EndPoint))
                EndPoint = defaultSettings.EndPoint;

            // Fix display settings
            if (!IsValidNumericString(Scale))
                Scale = defaultSettings.Scale;

            if (!IsValidNumericString(Offset))
                Offset = defaultSettings.Offset;

            // Fix scale if not positive
            if (IsValidNumericString(Scale))
            {
                var scaleValue = MathematicsCommands.ParseNumericValue(Scale);
                if (!MathematicsCommands.IsValidScale(scaleValue))
                    Scale = defaultSettings.Scale;
            }
        }

        /// <summary>
        /// Check if string represents valid numeric value
        /// </summary>
        private bool IsValidNumericString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return double.TryParse(value.Trim(), out double result) &&
                   !double.IsInfinity(result) &&
                   !double.IsNaN(result);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Copy settings from another instance
        /// </summary>
        /// <param name="source">Source settings to copy</param>
        public void CopyFrom(MathematicsSettings source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // Basic operations
            Source1 = source.Source1;
            Source2 = source.Source2;
            Operation = source.Operation;

            // FFT settings
            FFTSource = source.FFTSource;
            FFTWindow = source.FFTWindow;
            FFTSplit = source.FFTSplit;
            FFTUnit = source.FFTUnit;

            // Filter settings
            FilterType = source.FilterType;
            FilterW1 = source.FilterW1;
            FilterW2 = source.FilterW2;

            // Advanced math
            AdvancedFunction = source.AdvancedFunction;
            StartPoint = source.StartPoint;
            EndPoint = source.EndPoint;

            // Display settings
            MathDisplayEnabled = source.MathDisplayEnabled;
            InvertWaveform = source.InvertWaveform;
            Scale = source.Scale;
            Offset = source.Offset;

            // Metadata (except LastModified which should be updated)
            ConfigurationName = source.ConfigurationName;
            Description = source.Description;
            Version = source.Version;
            LastModified = DateTime.Now;
        }

        /// <summary>
        /// Create a deep copy of the settings
        /// </summary>
        /// <returns>New settings instance with copied values</returns>
        public MathematicsSettings Clone()
        {
            return new MathematicsSettings(this);
        }

        /// <summary>
        /// Compare settings for equality
        /// </summary>
        /// <param name="other">Other settings to compare</param>
        /// <returns>True if settings are equivalent</returns>
        public bool IsEquivalentTo(MathematicsSettings other)
        {
            if (other == null)
                return false;

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
                   Offset == other.Offset;
        }

        /// <summary>
        /// Get summary of current configuration
        /// </summary>
        /// <returns>Human-readable summary string</returns>
        public string GetConfigurationSummary()
        {
            var lines = new List<string>
            {
                $"Configuration: {ConfigurationName}",
                $"Operation: {Operation} ({Source1} {GetOperationSymbol()} {Source2})",
                $"FFT: {FFTSource} with {FFTWindow} window ({FFTUnit})",
                $"Filter: {FilterType} ({FilterW1} Hz to {FilterW2} Hz)",
                $"Advanced: {AdvancedFunction} ({StartPoint} to {EndPoint})",
                $"Display: {(MathDisplayEnabled ? "On" : "Off")}, Scale: {Scale}, Offset: {Offset}",
                $"Modified: {LastModified:yyyy-MM-dd HH:mm:ss}"
            };

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Get operation symbol for display
        /// </summary>
        private string GetOperationSymbol()
        {
            return Operation switch
            {
                "ADD" => "+",
                "SUBtract" => "-",
                "MULtiply" => "×",
                "DIVide" => "÷",
                _ => Operation
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Get string representation of settings
        /// </summary>
        /// <returns>Settings summary</returns>
        public override string ToString()
        {
            return $"Mathematics Settings - {ConfigurationName}: {Operation}, FFT: {FFTSource}/{FFTWindow}, Filter: {FilterType}, Display: {(MathDisplayEnabled ? "On" : "Off")}";
        }

        /// <summary>
        /// Get detailed string representation
        /// </summary>
        /// <returns>Detailed settings information</returns>
        public string ToDetailedString()
        {
            return GetConfigurationSummary();
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// Property changed event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Set property value and raise PropertyChanged event
        /// </summary>
        /// <typeparam name="T">Property type</typeparam>
        /// <param name="field">Field reference</param>
        /// <param name="value">New value</param>
        /// <param name="propertyName">Property name (auto-filled)</param>
        /// <returns>True if value changed</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);

            // Update last modified timestamp when any property changes
            if (propertyName != nameof(LastModified))
            {
                _lastModified = DateTime.Now;
                OnPropertyChanged(nameof(LastModified));
            }

            return true;
        }

        /// <summary>
        /// Raise PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Property name</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}