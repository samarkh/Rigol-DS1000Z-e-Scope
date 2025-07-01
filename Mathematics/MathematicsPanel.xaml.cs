using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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

        #endregion

        #region Events

        /// <summary>
        /// Event raised when SCPI command is generated
        /// </summary>
        public event EventHandler<string> SCPICommandGenerated;

        /// <summary>
        /// Event raised for status updates
        /// </summary>
        public event EventHandler<string> StatusUpdated;

        /// <summary>
        /// Event raised when error occurs
        /// </summary>
        public event EventHandler<string> ErrorOccurred;

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
                LoadDefaultSettings();
                SetInitialModeState();
            }
        }

        /// <summary>
        /// Set initial mode state - Basic Operations visible
        /// </summary>
        private void SetInitialModeState()
        {
            try
            {
                currentActiveMode = "BasicOperations";
                ShowOnlySection(currentActiveMode);
                UpdateStatusIndicator(currentActiveMode, "Basic Operations Mode Active", "#2ECC71");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error setting initial mode state: {ex.Message}");
            }
        }

        /// <summary>
        /// Load default settings for all sections - with safe control access
        /// </summary>
        private void LoadDefaultSettings()
        {
            try
            {
                // Basic Operations defaults - with null checks
                SafeSetComboBoxSelection("Source1Combo", 0);
                SafeSetComboBoxSelection("Source2Combo", 1);
                SafeSetComboBoxSelection("OperationCombo", 0);

                // FFT Analysis defaults - with null checks
                SafeSetComboBoxSelection("FFTSourceCombo", 0);
                SafeSetComboBoxSelection("FFTWindowCombo", 2);
                SafeSetComboBoxSelection("FFTSplitCombo", 0);
                SafeSetComboBoxSelection("FFTUnitCombo", 0);

                // Digital Filters defaults - try multiple control name variations
                SafeSetComboBoxSelection("FilterTypeCombo", 0);
                SafeSetTextBoxValue("FilterW1Text", "1000");
                SafeSetTextBoxValue("W1FrequencyText", "1000");
                SafeSetTextBoxValue("FilterW2Text", "10000");
                SafeSetTextBoxValue("W2FrequencyText", "10000");

                // Advanced Math defaults - with null checks
                SafeSetComboBoxSelection("AdvancedFunctionCombo", 0);
                SafeSetTextBoxValue("StartPointText", "0");
                SafeSetTextBoxValue("EndPointText", "100");

                // Common controls defaults - with null checks
                SafeSetCheckBoxValue("MathDisplayCheckbox", true);
                SafeSetCheckBoxValue("InvertCheckbox", false);
                SafeSetTextBoxValue("ScaleText", "1.0");
                SafeSetTextBoxValue("OffsetText", "0.0");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error loading default settings: {ex.Message}");
            }
        }

        #endregion

        #region Safe Control Access Methods

        /// <summary>
        /// Safely set ComboBox selection by control name
        /// </summary>
        private void SafeSetComboBoxSelection(string controlName, int index)
        {
            try
            {
                var control = this.FindName(controlName) as ComboBox;
                if (control != null && control.Items.Count > index)
                {
                    control.SelectedIndex = index;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting {controlName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely set TextBox value by control name
        /// </summary>
        private void SafeSetTextBoxValue(string controlName, string value)
        {
            try
            {
                var control = this.FindName(controlName) as TextBox;
                if (control != null)
                {
                    control.Text = value;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting {controlName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely set CheckBox value by control name
        /// </summary>
        private void SafeSetCheckBoxValue(string controlName, bool value)
        {
            try
            {
                var control = this.FindName(controlName) as CheckBox;
                if (control != null)
                {
                    control.IsChecked = value;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting {controlName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely get ComboBox tag value by control name
        /// </summary>
        private string SafeGetComboBoxTag(string controlName)
        {
            try
            {
                var control = this.FindName(controlName) as ComboBox;
                return (control?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Safely get TextBox value by control name
        /// </summary>
        private string SafeGetTextBoxValue(string controlName)
        {
            try
            {
                var control = this.FindName(controlName) as TextBox;
                return control?.Text?.Trim();
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Math Mode Selection and Switching

        /// <summary>
        /// Handle math mode selection change with proper SCPI sequence
        /// </summary>
        private async void MathModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized || isModeChanging) return;

            var comboBox = sender as ComboBox;
            var selectedItem = comboBox?.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag == null) return;

            string newMode = selectedItem.Tag.ToString();
            if (newMode == currentActiveMode) return;

            isModeChanging = true;

            try
            {
                OnStatusUpdated($"Switching from {GetModeDisplayName(currentActiveMode)} to {GetModeDisplayName(newMode)}...");

                // Execute the proper mode change sequence
                await ChangeMathModeAsync(newMode);

                currentActiveMode = newMode;
                OnStatusUpdated($"Successfully switched to {GetModeDisplayName(newMode)} mode");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error switching math modes: {ex.Message}");
                UpdateStatusIndicator(currentActiveMode, "Error occurred during mode switch", "#E74C3C");
            }
            finally
            {
                isModeChanging = false;
            }
        }

        /// <summary>
        /// Change math mode with proper SCPI sequence following DS1000Z-E specifications
        /// </summary>
        /// <param name="newMode">The new mode to activate</param>
        private async Task ChangeMathModeAsync(string newMode)
        {
            try
            {
                // Step 1: Disable current math function and reset
                UpdateStatusIndicator(newMode, "Disabling current mode...", "#F39C12");
                await SendScpiAsync(":MATH:DISPlay OFF; :MATH:RESet");
                await Task.Delay(RESET_DELAY); // 150ms delay as specified

                // Step 2: Collapse all UI sections
                await CollapseAllModes();

                // Step 3: Enable new operator based on mode
                UpdateStatusIndicator(newMode, "Enabling new mode...", "#F39C12");
                string operatorCommand = GetOperatorForMode(newMode);
                await SendScpiAsync($":MATH:DISPlay ON; :MATH:OPERator {operatorCommand}");
                await Task.Delay(MODE_CHANGE_DELAY); // 500ms delay before enabling controls

                // Step 4: Mode-specific setup
                await ConfigureModeSpecificSettings(newMode);

                // Step 5: Show the appropriate UI section
                ShowOnlySection(newMode);
                UpdateStatusIndicator(newMode, $"{GetModeDisplayName(newMode)} Mode Active", "#2ECC71");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to change math mode to {newMode}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get the SCPI operator command for each mode
        /// </summary>
        /// <param name="mode">The mode to get operator for</param>
        /// <returns>SCPI operator string</returns>
        private string GetOperatorForMode(string mode)
        {
            return mode switch
            {
                "BasicOperations" => "ADD", // Default to ADD, will be changed by specific operation
                "FFTAnalysis" => "FFT",
                "DigitalFilters" => "LPASs", // Default to Low Pass, will be changed by filter type
                "AdvancedMath" => "INTG", // Default to Integration, will be changed by function
                _ => throw new ArgumentException($"Unknown mode: {mode}")
            };
        }

        /// <summary>
        /// Configure mode-specific SCPI settings with proper timing
        /// </summary>
        /// <param name="mode">The mode to configure</param>
        private async Task ConfigureModeSpecificSettings(string mode)
        {
            switch (mode)
            {
                case "FFTAnalysis":
                    await ConfigureFFTMode();
                    break;
                case "DigitalFilters":
                    await ConfigureDigitalFilterMode();
                    break;
                case "AdvancedMath":
                    await ConfigureAdvancedMathMode();
                    break;
                case "BasicOperations":
                    await ConfigureBasicOperationsMode();
                    break;
            }
        }

        #endregion

        #region Mode-Specific Configuration Methods

        /// <summary>
        /// Configure FFT mode with exact sequence from specifications
        /// </summary>
        private async Task ConfigureFFTMode()
        {
            try
            {
                // FFT-specific setup sequence as specified
                await SendScpiAsync(":MATH:FFT:SOURce CHANnel1; :MATH:FFT:WINDow HANNing");
                await Task.Delay(COMMAND_DELAY); // 50ms delay

                await SendScpiAsync(":MATH:FFT:SPLit FULL");
                await Task.Delay(COMMAND_DELAY); // 50ms delay

                await SendScpiAsync(":MATH:FFT:UNIT VRMS");

                OnStatusUpdated("FFT mode configured: Source=CH1, Window=Hanning, Split=Full, Unit=VRMS");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure FFT mode: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Configure Digital Filter mode
        /// </summary>
        private async Task ConfigureDigitalFilterMode()
        {
            try
            {
                // Set default filter configuration
                await SendScpiAsync(":MATH:FILTer:TYPE LPASs"); // Default to Low Pass
                await Task.Delay(COMMAND_DELAY);

                await SendScpiAsync(":MATH:FILTer:W1 1000"); // Default 1kHz cutoff
                await Task.Delay(COMMAND_DELAY);

                await SendScpiAsync(":MATH:FILTer:W2 10000"); // Default 10kHz cutoff

                OnStatusUpdated("Digital Filter mode configured: Type=Low Pass, W1=1kHz, W2=10kHz");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure Digital Filter mode: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Configure Advanced Math mode
        /// </summary>
        private async Task ConfigureAdvancedMathMode()
        {
            try
            {
                // Set default advanced math configuration
                await SendScpiAsync(":MATH:OPTion:STARt 0");
                await Task.Delay(COMMAND_DELAY);

                await SendScpiAsync(":MATH:OPTion:END 100");

                OnStatusUpdated("Advanced Math mode configured: Function=Integration, Start=0, End=100");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure Advanced Math mode: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Configure Basic Operations mode
        /// </summary>
        private async Task ConfigureBasicOperationsMode()
        {
            try
            {
                // Set default sources for basic operations
                await SendScpiAsync(":MATH:SOURce1 CHANnel1");
                await Task.Delay(COMMAND_DELAY);

                await SendScpiAsync(":MATH:SOURce2 CHANnel2");

                OnStatusUpdated("Basic Operations mode configured: Source1=CH1, Source2=CH2, Operation=ADD");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure Basic Operations mode: {ex.Message}", ex);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Apply basic math operation (ADD, SUB, MUL, DIV)
        /// </summary>
        private async void ApplyBasicOperation_Click(object sender, RoutedEventArgs e)
        {
            if (currentActiveMode != "BasicOperations")
            {
                ShowModeError("Basic Operations");
                return;
            }

            try
            {
                var source1 = SafeGetComboBoxTag("Source1Combo");
                var source2 = SafeGetComboBoxTag("Source2Combo");
                var operation = SafeGetComboBoxTag("OperationCombo");

                if (string.IsNullOrEmpty(source1) || string.IsNullOrEmpty(source2) || string.IsNullOrEmpty(operation))
                {
                    throw new InvalidOperationException("Please select valid sources and operation");
                }

                OnStatusUpdated($"Applying basic operation: {source1} {operation} {source2}");

                // Send commands in sequence
                await SendScpiAsync($":MATH:SOURce1 {source1}");
                await Task.Delay(COMMAND_DELAY);
                await SendScpiAsync($":MATH:SOURce2 {source2}");
                await Task.Delay(COMMAND_DELAY);
                await SendScpiAsync($":MATH:OPERator {operation}");

                UpdateStatusIndicator("BasicOperations", $"Basic operation applied: {operation}", "#27AE60");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying basic operation: {ex.Message}");
                UpdateStatusIndicator("BasicOperations", "Error applying operation", "#E74C3C");
            }
        }

        /// <summary>
        /// Apply FFT analysis settings
        /// </summary>
        private async void ApplyFFT_Click(object sender, RoutedEventArgs e)
        {
            if (currentActiveMode != "FFTAnalysis")
            {
                ShowModeError("FFT Analysis");
                return;
            }

            try
            {
                var source = SafeGetComboBoxTag("FFTSourceCombo");
                var window = SafeGetComboBoxTag("FFTWindowCombo");
                var split = SafeGetComboBoxTag("FFTSplitCombo");
                var unit = SafeGetComboBoxTag("FFTUnitCombo");

                if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(window) ||
                    string.IsNullOrEmpty(split) || string.IsNullOrEmpty(unit))
                {
                    throw new InvalidOperationException("Please select all FFT parameters");
                }

                OnStatusUpdated($"Applying FFT analysis: {source}, {window}, {split}, {unit}");

                // Send FFT commands in sequence
                await SendScpiAsync($":MATH:FFT:SOURce {source}; :MATH:FFT:WINDow {window}");
                await Task.Delay(COMMAND_DELAY);
                await SendScpiAsync($":MATH:FFT:SPLit {split}");
                await Task.Delay(COMMAND_DELAY);
                await SendScpiAsync($":MATH:FFT:UNIT {unit}");

                UpdateStatusIndicator("FFTAnalysis", $"FFT applied: {window} window", "#3498DB");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying FFT analysis: {ex.Message}");
                UpdateStatusIndicator("FFTAnalysis", "Error applying FFT", "#E74C3C");
            }
        }

        /// <summary>
        /// Apply digital filter settings
        /// </summary>
        private async void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            if (currentActiveMode != "DigitalFilters")
            {
                ShowModeError("Digital Filters");
                return;
            }

            try
            {
                var filterType = SafeGetComboBoxTag("FilterTypeCombo");

                // Try multiple control name variations for frequency inputs
                var w1Text = SafeGetTextBoxValue("FilterW1Text") ?? SafeGetTextBoxValue("W1FrequencyText") ?? "1000";
                var w2Text = SafeGetTextBoxValue("FilterW2Text") ?? SafeGetTextBoxValue("W2FrequencyText") ?? "10000";

                if (string.IsNullOrEmpty(filterType))
                {
                    throw new InvalidOperationException("Please select a filter type");
                }

                OnStatusUpdated($"Applying digital filter: {filterType} ({w1Text}Hz - {w2Text}Hz)");

                // Send filter commands in sequence
                await SendScpiAsync($":MATH:FILTer:TYPE {filterType}");
                await Task.Delay(COMMAND_DELAY);
                await SendScpiAsync($":MATH:FILTer:W1 {w1Text}");
                await Task.Delay(COMMAND_DELAY);
                await SendScpiAsync($":MATH:FILTer:W2 {w2Text}");

                UpdateStatusIndicator("DigitalFilters", $"Filter applied: {filterType}", "#9C27B0");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying digital filter: {ex.Message}");
                UpdateStatusIndicator("DigitalFilters", "Error applying filter", "#E74C3C");
            }
        }

        /// <summary>
        /// Apply advanced math function
        /// </summary>
        private async void ApplyAdvancedMath_Click(object sender, RoutedEventArgs e)
        {
            if (currentActiveMode != "AdvancedMath")
            {
                ShowModeError("Advanced Math");
                return;
            }

            try
            {
                var function = SafeGetComboBoxTag("AdvancedFunctionCombo");
                var startPoint = SafeGetTextBoxValue("StartPointText") ?? "0";
                var endPoint = SafeGetTextBoxValue("EndPointText") ?? "100";

                if (string.IsNullOrEmpty(function))
                {
                    throw new InvalidOperationException("Please select an advanced math function");
                }

                OnStatusUpdated($"Applying advanced math: {function} ({startPoint} to {endPoint})");

                // Send advanced math commands in sequence
                await SendScpiAsync($":MATH:OPTion:FX:OPERator {function}");
                if (!string.IsNullOrEmpty(startPoint))
                {
                    await Task.Delay(COMMAND_DELAY);
                    await SendScpiAsync($":MATH:OPTion:STARt {startPoint}");
                }
                if (!string.IsNullOrEmpty(endPoint))
                {
                    await Task.Delay(COMMAND_DELAY);
                    await SendScpiAsync($":MATH:OPTion:END {endPoint}");
                }

                UpdateStatusIndicator("AdvancedMath", $"Advanced math applied: {function}", "#E67E22");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying advanced math: {ex.Message}");
                UpdateStatusIndicator("AdvancedMath", "Error applying function", "#E74C3C");
            }
        }

        /// <summary>
        /// Disable all math functions
        /// </summary>
        private async void DisableMath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OnStatusUpdated("Disabling all math functions...");

                await SendScpiAsync(":MATH:DISPlay OFF; :MATH:RESet");
                await Task.Delay(200);

                UpdateStatusIndicator(currentActiveMode, "Math functions disabled", "#95A5A6");
                OnStatusUpdated("All math functions disabled");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error disabling math functions: {ex.Message}");
            }
        }

        /// <summary>
        /// Save current settings (placeholder)
        /// </summary>
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OnStatusUpdated("Mathematics settings saved successfully");
                UpdateStatusIndicator(currentActiveMode, "Settings saved", "#27AE60");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error saving settings: {ex.Message}");
                UpdateStatusIndicator(currentActiveMode, "Error saving settings", "#E74C3C");
            }
        }

        #endregion

        #region SCPI Communication

        /// <summary>
        /// Send SCPI command asynchronously with error handling
        /// </summary>
        /// <param name="command">SCPI command to send</param>
        private async Task SendScpiAsync(string command)
        {
            try
            {
                // Raise event for SCPI command generation
                OnSCPICommandGenerated(command);

                // Simulate sending command - replace with actual VISA/USB communication
                await Task.Run(() =>
                {
                    // Your actual SCPI sending logic here
                    // For example: visaConnection.WriteString(command);
                });

                OnStatusUpdated($"SCPI Command sent: {command}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to send SCPI command '{command}': {ex.Message}", ex);
            }
        }

        #endregion

        #region UI Management Methods

        /// <summary>
        /// Collapse all math mode sections
        /// </summary>
        private async Task CollapseAllModes()
        {
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    SafeSetVisibility("BasicOperationsSection", Visibility.Collapsed);
                    SafeSetVisibility("FFTAnalysisSection", Visibility.Collapsed);
                    SafeSetVisibility("DigitalFiltersSection", Visibility.Collapsed);
                    SafeSetVisibility("AdvancedMathSection", Visibility.Collapsed);
                });
            });
        }

        /// <summary>
        /// Show only the specified section
        /// </summary>
        private void ShowOnlySection(string mode)
        {
            try
            {
                // First ensure all sections are collapsed
                SafeSetVisibility("BasicOperationsSection", Visibility.Collapsed);
                SafeSetVisibility("FFTAnalysisSection", Visibility.Collapsed);
                SafeSetVisibility("DigitalFiltersSection", Visibility.Collapsed);
                SafeSetVisibility("AdvancedMathSection", Visibility.Collapsed);

                // Show only the selected section
                switch (mode)
                {
                    case "BasicOperations":
                        SafeSetVisibility("BasicOperationsSection", Visibility.Visible);
                        break;
                    case "FFTAnalysis":
                        SafeSetVisibility("FFTAnalysisSection", Visibility.Visible);
                        break;
                    case "DigitalFilters":
                        SafeSetVisibility("DigitalFiltersSection", Visibility.Visible);
                        break;
                    case "AdvancedMath":
                        SafeSetVisibility("AdvancedMathSection", Visibility.Visible);
                        break;
                    default:
                        OnErrorOccurred($"Unknown mode: {mode}");
                        break;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error showing section for mode {mode}: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely set control visibility
        /// </summary>
        private void SafeSetVisibility(string controlName, Visibility visibility)
        {
            try
            {
                var control = this.FindName(controlName) as FrameworkElement;
                if (control != null)
                {
                    control.Visibility = visibility;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting visibility for {controlName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Update status indicator with color coding
        /// </summary>
        private void UpdateStatusIndicator(string mode, string message, string color = "#2ECC71")
        {
            try
            {
                // Try to find status controls by different possible names
                var statusIndicator = this.FindName("StatusIndicator");
                var statusText = this.FindName("StatusText");

                // Handle different control types for status indicator
                if (statusIndicator is Ellipse ellipse)
                {
                    ellipse.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                }
                else if (statusIndicator is TextBlock statusBlock)
                {
                    statusBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                }

                // Update status text
                if (statusText is TextBlock textBlock)
                {
                    textBlock.Text = message;
                }
            }
            catch (Exception ex)
            {
                // Fail silently for status updates to avoid cascading errors
                System.Diagnostics.Debug.WriteLine($"Status update error: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Show mode mismatch error
        /// </summary>
        private void ShowModeError(string expectedMode)
        {
            var message = $"This function is only available in {expectedMode} mode. Please select {expectedMode} from the mode dropdown.";
            MessageBox.Show(message, "Mode Mismatch", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Get display name for mode
        /// </summary>
        private string GetModeDisplayName(string mode)
        {
            return mode switch
            {
                "BasicOperations" => "Basic Operations",
                "FFTAnalysis" => "FFT Analysis",
                "DigitalFilters" => "Digital Filters",
                "AdvancedMath" => "Advanced Math",
                _ => "Unknown Mode"
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Handle leaving the Mathematics Panel - disable all math functions
        /// </summary>
        public async Task OnPanelClosingAsync()
        {
            try
            {
                OnStatusUpdated("Disabling all mathematics functions...");
                await SendScpiAsync(":MATH:DISPlay OFF; :MATH:RESet");
                OnStatusUpdated("Mathematics functions disabled");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error disabling math functions on panel close: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current math mode
        /// </summary>
        public string GetCurrentMathMode()
        {
            return currentActiveMode;
        }

        /// <summary>
        /// Check if mode is currently changing
        /// </summary>
        public bool IsModeChanging()
        {
            return isModeChanging;
        }

        /// <summary>
        /// Get current settings (simplified version)
        /// </summary>
        public string GetCurrentSettings()
        {
            try
            {
                return $"Current Mode: {currentActiveMode}";
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error getting current settings: {ex.Message}");
                return "Error retrieving settings";
            }
        }

        /// <summary>
        /// Apply preset (simplified implementation)
        /// </summary>
        public async Task ApplyPreset(string presetName)
        {
            try
            {
                OnStatusUpdated($"Applied preset: {presetName}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying preset: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers for Status Updates

        /// <summary>
        /// Event handler for SCPI command generation
        /// </summary>
        private void OnSCPICommandGenerated(string command)
        {
            // Create the appropriate EventArgs and invoke the event
            var eventArgs = new SCPICommandEventArgs(command, "MathematicsPanel", "MATH");
            SCPICommandGenerated?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// Event handler for status updates
        /// </summary>
        private void OnStatusUpdated(string message)
        {
            // Create the appropriate EventArgs and invoke the event
            var eventArgs = new StatusEventArgs(message, StatusLevel.Info, "MathematicsPanel", "MATH");
            StatusUpdated?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// Event handler for error occurrences
        /// </summary>
        private void OnErrorOccurred(string error)
        {
            // Create the appropriate ErrorEventArgs and invoke the event
            var eventArgs = new ErrorEventArgs(error);
            ErrorOccurred?.Invoke(this, eventArgs);
        }

        #endregion
    }

    public partial class MathematicsPanel : UserControl
    {
        #region Events - UPDATED SIGNATURES

        /// <summary>
        /// Event raised when SCPI command is generated
        /// FIXED: Changed from EventHandler<string> to EventHandler<SCPICommandEventArgs>
        /// </summary>
        public event EventHandler<SCPICommandEventArgs> SCPICommandGenerated;

        /// <summary>
        /// Event raised for status updates
        /// FIXED: Changed from EventHandler<string> to EventHandler<StatusEventArgs>
        /// </summary>
        public event EventHandler<StatusEventArgs> StatusUpdated;

        /// <summary>
        /// Event raised when error occurs
        /// FIXED: Changed from EventHandler<string> to EventHandler<ErrorEventArgs>
        /// </summary>
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        #endregion

        #region Event Handlers for Status Updates - UPDATED IMPLEMENTATIONS

        /// <summary>
        /// Event handler for SCPI command generation
        /// FIXED: Now creates proper SCPICommandEventArgs
        /// </summary>
        private void OnSCPICommandGenerated(string command)
        {
            // Create the appropriate EventArgs and invoke the event
            var eventArgs = new SCPICommandEventArgs(command, "MathematicsPanel", "MATH");
            SCPICommandGenerated?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// Event handler for status updates
        /// FIXED: Now creates proper StatusEventArgs
        /// </summary>
        private void OnStatusUpdated(string message)
        {
            // Create the appropriate EventArgs and invoke the event
            var eventArgs = new StatusEventArgs(message, StatusLevel.Info, "MathematicsPanel", "MATH");
            StatusUpdated?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// Event handler for error occurrences
        /// FIXED: Now creates proper ErrorEventArgs
        /// </summary>
        private void OnErrorOccurred(string error)
        {
            // Create the appropriate ErrorEventArgs and invoke the event
            var eventArgs = new ErrorEventArgs(error);
            eventArgs.Source = "MathematicsPanel";
            eventArgs.Category = "MATH";
            ErrorOccurred?.Invoke(this, eventArgs);
        }

        #endregion
    }



}