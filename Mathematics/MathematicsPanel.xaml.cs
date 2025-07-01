using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Mathematics Panel for Rigol DS1000Z-E oscilloscope control
    /// Implements mutually exclusive math modes with proper SCPI command sequences
    /// </summary>
    public partial class MathematicsPanel : UserControl
    {
        #region Fields and Properties

        private MathematicsController controller;
        private bool isInitialized = false;
        private bool isModeChanging = false;
        private string currentActiveMode = "BasicOperations";

        // SCPI timing configuration (milliseconds)
        private const int RESET_DELAY = 150;
        private const int MODE_CHANGE_DELAY = 500;
        private const int COMMAND_DELAY = 50;

        /// <summary>
        /// Get current math mode
        /// </summary>
        public string GetCurrentMathMode() => currentActiveMode;

        /// <summary>
        /// Check if mode is currently changing
        /// </summary>
        public bool IsModeChanging => isModeChanging;

        #endregion

        #region Events

        /// <summary>
        /// Event raised when SCPI command is generated
        /// </summary>
        public event EventHandler<SCPICommandEventArgs> SCPICommandGenerated;

        /// <summary>
        /// Event raised for status updates
        /// </summary>
        public event EventHandler<StatusEventArgs> StatusUpdated;

        /// <summary>
        /// Event raised when error occurs
        /// </summary>
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        #endregion

        #region Constructor and Initialization

        /// <summary>
        /// Initialize the Mathematics Panel
        /// </summary>
        public MathematicsPanel()
        {
            InitializeComponent();
            InitializePanel();
        }

        /// <summary>
        /// Initialize panel components and default state
        /// </summary>
        private void InitializePanel()
        {
            try
            {
                controller = new MathematicsController();

                // Subscribe to controller events
                if (controller != null)
                {
                    controller.SCPICommandGenerated += Controller_SCPICommandGenerated;
                    controller.ErrorOccurred += Controller_ErrorOccurred;
                    controller.StatusUpdated += Controller_StatusUpdated;
                }

                isInitialized = true;
                OnStatusUpdated("Mathematics panel initialized - Basic Operations mode active");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error initializing mathematics panel: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the control is loaded
        /// </summary>
        private void MathematicsPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (isInitialized && !isModeChanging)
            {
                try
                {
                    // Initialize default mode
                    SetDefaultMode();
                    OnStatusUpdated("Mathematics panel ready for use");
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"Error during panel loading: {ex.Message}");
                }
            }
        }

        #endregion

        #region Mode Management

        /// <summary>
        /// Set default math mode on initialization
        /// </summary>
        private void SetDefaultMode()
        {
            try
            {
                currentActiveMode = "BasicOperations";
                OnStatusUpdated($"Default mode set: {currentActiveMode}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error setting default mode: {ex.Message}");
            }
        }

        /// <summary>
        /// Change math mode with proper SCPI sequencing
        /// </summary>
        /// <param name="newMode">Target mode</param>
        public async Task ChangeMathModeAsync(string newMode)
        {
            if (isModeChanging || currentActiveMode == newMode)
                return;

            try
            {
                isModeChanging = true;
                OnStatusUpdated($"Changing math mode from {currentActiveMode} to {newMode}...");

                // Step 1: Reset math system
                await ResetMathSystemAsync();

                // Step 2: Set new mode
                await SetMathModeAsync(newMode);

                // Step 3: Configure mode-specific settings
                await ConfigureModeAsync(newMode);

                currentActiveMode = newMode;
                OnStatusUpdated($"Math mode changed to {newMode}");
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

        /// <summary>
        /// Reset math system with proper timing
        /// </summary>
        private async Task ResetMathSystemAsync()
        {
            OnSCPICommandGenerated(":MATH:DISPlay OFF");
            await Task.Delay(RESET_DELAY);
            OnSCPICommandGenerated(":MATH:RESet");
            await Task.Delay(RESET_DELAY);
        }

        /// <summary>
        /// Set math mode with proper timing
        /// </summary>
        private async Task SetMathModeAsync(string mode)
        {
            var operatorCommand = GetOperatorForMode(mode);
            OnSCPICommandGenerated($":MATH:OPERator {operatorCommand}");
            await Task.Delay(MODE_CHANGE_DELAY);
            OnSCPICommandGenerated(":MATH:DISPlay ON");
            await Task.Delay(COMMAND_DELAY);
        }

        /// <summary>
        /// Configure mode-specific settings
        /// </summary>
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

        /// <summary>
        /// Get SCPI operator for mode
        /// </summary>
        private string GetOperatorForMode(string mode)
        {
            return mode switch
            {
                "BasicOperations" => "ADD",
                "FFTAnalysis" => "FFT",
                "DigitalFilters" => "LPASs",
                "AdvancedMath" => "INTG",
                _ => "ADD"
            };
        }

        #endregion

        #region Mode-Specific Configuration

        /// <summary>
        /// Configure basic operations mode
        /// </summary>
        private async Task ConfigureBasicOperationsAsync()
        {
            OnSCPICommandGenerated(":MATH:SOURce1 CHANnel1");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated(":MATH:SOURce2 CHANnel2");
            await Task.Delay(COMMAND_DELAY);
            OnStatusUpdated("Basic Operations mode configured");
        }

        /// <summary>
        /// Configure FFT analysis mode
        /// </summary>
        private async Task ConfigureFFTAnalysisAsync()
        {
            OnSCPICommandGenerated(":MATH:FFT:SOURce CHANnel1");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated(":MATH:FFT:WINDow HANNing");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated(":MATH:FFT:SPLit FULL");
            await Task.Delay(COMMAND_DELAY);
            OnStatusUpdated("FFT Analysis mode configured");
        }

        /// <summary>
        /// Configure digital filters mode
        /// </summary>
        private async Task ConfigureDigitalFiltersAsync()
        {
            OnSCPICommandGenerated(":MATH:FILTer:SOURce CHANnel1");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated(":MATH:FILTer:W1 1000");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated(":MATH:FILTer:W2 10000");
            await Task.Delay(COMMAND_DELAY);
            OnStatusUpdated("Digital Filters mode configured");
        }

        /// <summary>
        /// Configure advanced math mode
        /// </summary>
        private async Task ConfigureAdvancedMathAsync()
        {
            OnSCPICommandGenerated(":MATH:ADVanced:SOURce CHANnel1");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated(":MATH:ADVanced:STARt 0");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated(":MATH:ADVanced:END 100");
            await Task.Delay(COMMAND_DELAY);
            OnStatusUpdated("Advanced Math mode configured");
        }

        #endregion

        #region Settings Management

        /// <summary>
        /// Get current settings as MathematicsSettings object
        /// </summary>
        public MathematicsSettings GetCurrentSettings()
        {
            try
            {
                var settings = new MathematicsSettings
                {
                    ActiveMode = currentActiveMode,
                    ConfigurationName = $"Current {currentActiveMode} Configuration",
                    LastModified = DateTime.Now
                };

                // Set mode-specific settings based on active mode
                switch (currentActiveMode)
                {
                    case "BasicOperations":
                        settings.Operation = "ADD";
                        settings.Source1 = "CHANnel1";
                        settings.Source2 = "CHANnel2";
                        break;

                    case "FFTAnalysis":
                        settings.FFTSource = "CHANnel1";
                        settings.FFTWindow = "HANNing";
                        settings.FFTSplit = "FULL";
                        settings.FFTUnit = "VRMS";
                        break;

                    case "DigitalFilters":
                        settings.FilterType = "LPASs";
                        settings.FilterW1 = "1000";
                        settings.FilterW2 = "10000";
                        break;

                    case "AdvancedMath":
                        settings.AdvancedFunction = "INTG";
                        settings.StartPoint = "0";
                        settings.EndPoint = "100";
                        break;
                }

                return settings;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error getting current settings: {ex.Message}");
                return new MathematicsSettings();
            }
        }

        /// <summary>
        /// Load settings into panel
        /// </summary>
        public async Task LoadSettingsAsync(MathematicsSettings settings)
        {
            try
            {
                if (settings == null)
                {
                    OnErrorOccurred("Cannot load null settings");
                    return;
                }

                // Change to the settings mode if different
                if (settings.ActiveMode != currentActiveMode)
                {
                    await ChangeMathModeAsync(settings.ActiveMode);
                }

                // Apply mode-specific settings
                await ApplyModeSpecificSettingsAsync(settings);

                OnStatusUpdated($"Settings loaded for {settings.ActiveMode} mode");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error loading settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply mode-specific settings
        /// </summary>
        private async Task ApplyModeSpecificSettingsAsync(MathematicsSettings settings)
        {
            switch (settings.ActiveMode)
            {
                case "BasicOperations":
                    if (!string.IsNullOrEmpty(settings.Source1))
                    {
                        OnSCPICommandGenerated($":MATH:SOURce1 {settings.Source1}");
                        await Task.Delay(COMMAND_DELAY);
                    }
                    if (!string.IsNullOrEmpty(settings.Source2))
                    {
                        OnSCPICommandGenerated($":MATH:SOURce2 {settings.Source2}");
                        await Task.Delay(COMMAND_DELAY);
                    }
                    if (!string.IsNullOrEmpty(settings.Operation))
                    {
                        OnSCPICommandGenerated($":MATH:OPERator {settings.Operation}");
                        await Task.Delay(COMMAND_DELAY);
                    }
                    break;

                case "FFTAnalysis":
                    if (!string.IsNullOrEmpty(settings.FFTSource))
                    {
                        OnSCPICommandGenerated($":MATH:FFT:SOURce {settings.FFTSource}");
                        await Task.Delay(COMMAND_DELAY);
                    }
                    if (!string.IsNullOrEmpty(settings.FFTWindow))
                    {
                        OnSCPICommandGenerated($":MATH:FFT:WINDow {settings.FFTWindow}");
                        await Task.Delay(COMMAND_DELAY);
                    }
                    break;

                case "DigitalFilters":
                    if (!string.IsNullOrEmpty(settings.FilterType))
                    {
                        OnSCPICommandGenerated($":MATH:FILTer:TYPE {settings.FilterType}");
                        await Task.Delay(COMMAND_DELAY);
                    }
                    if (!string.IsNullOrEmpty(settings.FilterW1))
                    {
                        OnSCPICommandGenerated($":MATH:FILTer:W1 {settings.FilterW1}");
                        await Task.Delay(COMMAND_DELAY);
                    }
                    break;

                case "AdvancedMath":
                    if (!string.IsNullOrEmpty(settings.AdvancedFunction))
                    {
                        OnSCPICommandGenerated($":MATH:OPERator {settings.AdvancedFunction}");
                        await Task.Delay(COMMAND_DELAY);
                    }
                    break;
            }
        }

        /// <summary>
        /// Apply preset configuration
        /// </summary>
        public async Task ApplyPresetAsync(string presetName)
        {
            try
            {
                var presets = MathematicsSettings.GetFactoryPresets();
                if (presets.ContainsKey(presetName))
                {
                    await LoadSettingsAsync(presets[presetName]);
                    OnStatusUpdated($"Applied preset: {presetName}");
                }
                else
                {
                    OnErrorOccurred($"Unknown preset: {presetName}");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying preset: {ex.Message}");
            }
        }

        #endregion

        #region Controller Event Handlers

        /// <summary>
        /// Handle SCPI commands from controller
        /// </summary>
        private void Controller_SCPICommandGenerated(object sender, string command)
        {
            OnSCPICommandGenerated(command);
        }

        /// <summary>
        /// Handle errors from controller
        /// </summary>
        private void Controller_ErrorOccurred(object sender, string error)
        {
            OnErrorOccurred(error);
        }

        /// <summary>
        /// Handle status updates from controller
        /// </summary>
        private void Controller_StatusUpdated(object sender, string status)
        {
            OnStatusUpdated(status);
        }

        #endregion

        #region Event Handlers for Status Updates

        /// <summary>
        /// Event handler for SCPI command generation
        /// </summary>
        private void OnSCPICommandGenerated(string command)
        {
            try
            {
                if (string.IsNullOrEmpty(command)) return;

                var eventArgs = new SCPICommandEventArgs(command, "MathematicsPanel", "MATH");
                SCPICommandGenerated?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnSCPICommandGenerated: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for status updates
        /// </summary>
        private void OnStatusUpdated(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message)) return;

                var eventArgs = new StatusEventArgs(message, StatusLevel.Info, "MathematicsPanel", "MATH");
                StatusUpdated?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnStatusUpdated: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for error occurrences
        /// </summary>
        private void OnErrorOccurred(string error)
        {
            try
            {
                if (string.IsNullOrEmpty(error)) return;

                var eventArgs = new ErrorEventArgs(error)
                {
                    Source = "MathematicsPanel",
                    Category = "MATH",
                    Severity = ErrorSeverity.Error
                };
                ErrorOccurred?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnErrorOccurred: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enable math display
        /// </summary>
        public void EnableMathDisplay()
        {
            try
            {
                OnSCPICommandGenerated(":MATH:DISPlay ON");
                OnStatusUpdated("Math display enabled");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error enabling math display: {ex.Message}");
            }
        }

        /// <summary>
        /// Disable math display
        /// </summary>
        public void DisableMathDisplay()
        {
            try
            {
                OnSCPICommandGenerated(":MATH:DISPlay OFF");
                OnStatusUpdated("Math display disabled");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error disabling math display: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset math system
        /// </summary>
        public async Task ResetMathAsync()
        {
            try
            {
                await ResetMathSystemAsync();
                currentActiveMode = "BasicOperations";
                OnStatusUpdated("Math system reset to Basic Operations");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error resetting math: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate current configuration
        /// </summary>
        public List<string> ValidateConfiguration()
        {
            var issues = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(currentActiveMode))
                {
                    issues.Add("No active math mode selected");
                }

                if (!isInitialized)
                {
                    issues.Add("Panel not properly initialized");
                }

                if (controller == null)
                {
                    issues.Add("Controller not initialized");
                }

                // Add mode-specific validation
                switch (currentActiveMode)
                {
                    case "BasicOperations":
                        // Could add validation for source channels
                        break;
                    case "FFTAnalysis":
                        // Could add validation for FFT parameters
                        break;
                    case "DigitalFilters":
                        // Could add validation for filter parameters
                        break;
                    case "AdvancedMath":
                        // Could add validation for advanced math parameters
                        break;
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Validation error: {ex.Message}");
            }

            return issues;
        }

        #endregion


        #region missingEvents

        private void MathModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Implementation based on the current mode selection
            var combo = sender as ComboBox;
            var selectedMode = combo?.SelectedItem?.ToString();
            // Handle mode change logic
        }

        private async void ApplyBasicOperation_Click(object sender, RoutedEventArgs e)
        {
            await ConfigureBasicOperationsAsync();
        }

        private async void ApplyFFT_Click(object sender, RoutedEventArgs e)
        {
            await ConfigureFFTAnalysisAsync();
        }

        private async void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            await ConfigureDigitalFiltersAsync();
        }

        private async void ApplyAdvancedMath_Click(object sender, RoutedEventArgs e)
        {
            await ConfigureAdvancedMathAsync();
        }

        private void DisableMath_Click(object sender, RoutedEventArgs e)
        {
            DisableMathDisplay();
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            // Implement settings save logic
            var settings = GetCurrentSettings();
            // Save settings to file or registry
        }


        #endregion



        #region Cleanup

        /// <summary>
        /// Cleanup resources when panel is disposed
        /// </summary>
        public void Cleanup()
        {
            try
            {
                // Unsubscribe from controller events
                if (controller != null)
                {
                    controller.SCPICommandGenerated -= Controller_SCPICommandGenerated;
                    controller.ErrorOccurred -= Controller_ErrorOccurred;
                    controller.StatusUpdated -= Controller_StatusUpdated;
                }

                // Dispose controller
                controller?.Dispose();

                OnStatusUpdated("Mathematics panel cleaned up");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        #endregion
    }
}