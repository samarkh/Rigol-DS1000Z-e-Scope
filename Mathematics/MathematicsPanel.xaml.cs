using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text;
using System.Globalization;

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
        private MathematicsSettings settings;
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
                settings = new MathematicsSettings();
                isInitialized = true;

                OnStatusUpdated("Mathematics panel initialized - Basic Operations mode active");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error initializing mathematics panel: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the template is applied - set up initial state
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (isInitialized)
            {
                LoadDefaultSettings();
                SetInitialModeState();
            }
        }

        /// <summary>
        /// Handle Loaded event to ensure controls are available
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
        /// Load default settings for all sections - with null checks
        /// </summary>
        private void LoadDefaultSettings()
        {
            try
            {
                // Basic Operations defaults - with null checks
                if (Source1Combo != null && Source1Combo.Items.Count > 0)
                    Source1Combo.SelectedIndex = 0; // Channel 1
                if (Source2Combo != null && Source2Combo.Items.Count > 1)
                    Source2Combo.SelectedIndex = 1; // Channel 2
                if (OperationCombo != null && OperationCombo.Items.Count > 0)
                    OperationCombo.SelectedIndex = 0; // ADD

                // FFT Analysis defaults - with null checks
                if (FFTSourceCombo != null && FFTSourceCombo.Items.Count > 0)
                    FFTSourceCombo.SelectedIndex = 0; // Channel 1
                if (FFTWindowCombo != null && FFTWindowCombo.Items.Count > 2)
                    FFTWindowCombo.SelectedIndex = 2; // Hanning
                if (FFTSplitCombo != null && FFTSplitCombo.Items.Count > 0)
                    FFTSplitCombo.SelectedIndex = 0; // Full
                if (FFTUnitCombo != null && FFTUnitCombo.Items.Count > 0)
                    FFTUnitCombo.SelectedIndex = 0; // VRMS

                // Digital Filters defaults - with null checks
                if (FilterTypeCombo != null && FilterTypeCombo.Items.Count > 0)
                    FilterTypeCombo.SelectedIndex = 0; // Low Pass
                if (W1FrequencyText != null)
                    W1FrequencyText.Text = "1000";
                if (W2FrequencyText != null)
                    W2FrequencyText.Text = "10000";

                // Advanced Math defaults - with null checks
                if (AdvancedFunctionCombo != null && AdvancedFunctionCombo.Items.Count > 0)
                    AdvancedFunctionCombo.SelectedIndex = 0; // Integration
                if (StartPointText != null)
                    StartPointText.Text = "0";
                if (EndPointText != null)
                    EndPointText.Text = "100";

                // Common controls defaults - with null checks
                if (MathDisplayCheckbox != null)
                    MathDisplayCheckbox.IsChecked = true;
                if (InvertCheckbox != null)
                    InvertCheckbox.IsChecked = false;
                if (ScaleText != null)
                    ScaleText.Text = "1.0";
                if (OffsetText != null)
                    OffsetText.Text = "0.0";
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error loading default settings: {ex.Message}");
            }
        }

        #endregion

        #region Math Mode Selection and Switching

        /// <summary>
        /// Handle math mode selection change with proper SCPI sequence
        /// </summary>
        private async void MathModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isModeChanging || !isInitialized) return;

            var selectedItem = MathModeCombo.SelectedItem as ComboBoxItem;
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
                await Task.Delay(150); // 150ms delay as specified

                // Step 2: Collapse all UI sections
                await CollapseAllModes();

                // Step 3: Enable new operator based on mode
                UpdateStatusIndicator(newMode, "Enabling new mode...", "#F39C12");
                string operatorCommand = GetOperatorForMode(newMode);
                await SendScpiAsync($":MATH:DISPlay ON; :MATH:OPERator {operatorCommand}");
                await Task.Delay(500); // 500ms delay before enabling controls

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



        #region Mode Configuration Methods
        /// <summary>
        /// Configure FFT mode with exact sequence from specifications
        /// </summary>
        private async Task ConfigureFFTMode()
        {
            try
            {
                // FFT-specific setup sequence as specified
                await SendScpiAsync(":MATH:FFT:SOURce CHANnel1; :MATH:FFT:WINDow HANNing");
                await Task.Delay(50); // 50ms delay

                await SendScpiAsync(":MATH:FFT:SPLit FULL");
                await Task.Delay(50); // 50ms delay

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
                await Task.Delay(50);

                await SendScpiAsync(":MATH:FILTer:W1 1000"); // Default 1kHz cutoff
                await Task.Delay(50);

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
                await Task.Delay(50);

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
                await Task.Delay(50);

                await SendScpiAsync(":MATH:SOURce2 CHANnel2");

                OnStatusUpdated("Basic Operations mode configured: Source1=CH1, Source2=CH2, Operation=ADD");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure Basic Operations mode: {ex.Message}", ex);
            }
        }

#endregion

        #region Basic Operations Event Handlers

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
                var source1 = GetSelectedComboBoxTag(Source1Combo);
                var source2 = GetSelectedComboBoxTag(Source2Combo);
                var operation = GetSelectedComboBoxTag(OperationCombo);

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

        #endregion

        #region FFT Analysis Event Handlers

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
                var source = GetSelectedComboBoxTag(FFTSourceCombo);
                var window = GetSelectedComboBoxTag(FFTWindowCombo);
                var split = GetSelectedComboBoxTag(FFTSplitCombo);
                var unit = GetSelectedComboBoxTag(FFTUnitCombo);

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

        #endregion

        #region Digital Filters Event Handlers

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
                var filterType = GetSelectedComboBoxTag(FilterTypeCombo);
                var w1Text = W1FrequencyText?.Text?.Trim();
                var w2Text = W2FrequencyText?.Text?.Trim();

                if (string.IsNullOrEmpty(filterType))
                {
                    throw new InvalidOperationException("Please select a filter type");
                }

                if (string.IsNullOrEmpty(w1Text) || string.IsNullOrEmpty(w2Text))
                {
                    throw new InvalidOperationException("Please enter valid frequency values");
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

        #endregion

        #region Advanced Math Event Handlers

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
                var function = GetSelectedComboBoxTag(AdvancedFunctionCombo);
                var startPoint = StartPointText?.Text?.Trim();
                var endPoint = EndPointText?.Text?.Trim();

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

        #endregion

        #region Common Control Event Handlers

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
        /// Save current settings
        /// </summary>
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveSettingsFromUI();
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
                    BasicOperationsSection.Visibility = Visibility.Collapsed;
                    FFTAnalysisSection.Visibility = Visibility.Collapsed;
                    DigitalFiltersSection.Visibility = Visibility.Collapsed;
                    AdvancedMathSection.Visibility = Visibility.Collapsed;
                });
            });
        }

        /// <summary>
        /// Show only the specified section
        /// </summary>
        private void ShowOnlySection(string mode)
        {
            // First ensure all sections are collapsed
            BasicOperationsSection.Visibility = Visibility.Collapsed;
            FFTAnalysisSection.Visibility = Visibility.Collapsed;
            DigitalFiltersSection.Visibility = Visibility.Collapsed;
            AdvancedMathSection.Visibility = Visibility.Collapsed;

            // Show only the selected section
            switch (mode)
            {
                case "BasicOperations":
                    BasicOperationsSection.Visibility = Visibility.Visible;
                    break;
                case "FFTAnalysis":
                    FFTAnalysisSection.Visibility = Visibility.Visible;
                    break;
                case "DigitalFilters":
                    DigitalFiltersSection.Visibility = Visibility.Visible;
                    break;
                case "AdvancedMath":
                    AdvancedMathSection.Visibility = Visibility.Visible;
                    break;
                default:
                    OnErrorOccurred($"Unknown mode: {mode}");
                    break;
            }
        }


        /// <summary>
        /// Update status indicator with color coding
        /// </summary>
        private void UpdateStatusIndicator(string mode, string message, string color = "#2ECC71")
        {
            StatusIndicator.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            StatusText.Text = message;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get selected ComboBox tag value with null safety
        /// </summary>
        private string GetSelectedComboBoxTag(ComboBox comboBox)
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

        /// <summary>
        /// Save current UI settings to settings object with null safety
        /// </summary>
        private void SaveSettingsFromUI()
        {
            if (settings == null) return;

            try
            {
                // Save basic operation settings
                if (settings.BasicOperations != null)
                {
                    settings.BasicOperations.Source1 = GetSelectedComboBoxTag(Source1Combo) ?? "CHANnel1";
                    settings.BasicOperations.Source2 = GetSelectedComboBoxTag(Source2Combo) ?? "CHANnel2";
                    settings.BasicOperations.Operation = GetSelectedComboBoxTag(OperationCombo) ?? "ADD";
                }

                // Save FFT settings
                if (settings.FFTAnalysis != null)
                {
                    settings.FFTAnalysis.Source = GetSelectedComboBoxTag(FFTSourceCombo) ?? "CHANnel1";
                    settings.FFTAnalysis.Window = GetSelectedComboBoxTag(FFTWindowCombo) ?? "HANNing";
                    settings.FFTAnalysis.Split = GetSelectedComboBoxTag(FFTSplitCombo) ?? "FULL";
                    settings.FFTAnalysis.Unit = GetSelectedComboBoxTag(FFTUnitCombo) ?? "VRMS";
                }

                // Save filter settings
                if (settings.DigitalFilters != null)
                {
                    settings.DigitalFilters.FilterType = GetSelectedComboBoxTag(FilterTypeCombo) ?? "LPASs";
                    if (double.TryParse(W1FrequencyText?.Text, out double w1))
                        settings.DigitalFilters.W1Frequency = w1;
                    if (double.TryParse(W2FrequencyText?.Text, out double w2))
                        settings.DigitalFilters.W2Frequency = w2;
                }

                // Save advanced math settings
                if (settings.AdvancedMath != null)
                {
                    settings.AdvancedMath.Function = GetSelectedComboBoxTag(AdvancedFunctionCombo) ?? "INTG";
                    if (double.TryParse(StartPointText?.Text, out double start))
                        settings.AdvancedMath.StartPoint = start;
                    if (double.TryParse(EndPointText?.Text, out double end))
                        settings.AdvancedMath.EndPoint = end;
                }

                // Save common settings
                settings.MathDisplay = MathDisplayCheckbox?.IsChecked ?? true;
                settings.InvertWaveform = InvertCheckbox?.IsChecked ?? false;
                if (double.TryParse(ScaleText?.Text, out double scale))
                    settings.Scale = scale;
                if (double.TryParse(OffsetText?.Text, out double offset))
                    settings.Offset = offset;

                settings.CurrentMode = currentActiveMode;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error saving settings: {ex.Message}", ex);
            }
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
        /// Force disable all math functions (emergency stop)
        /// </summary>
        public async Task ForceDisableAllMathAsync()
        {
            try
            {
                isModeChanging = true;
                await SendScpiAsync(":MATH:DISPlay OFF; :MATH:RESet");
                await CollapseAllModes();
                UpdateStatusIndicator(currentActiveMode, "All math functions disabled", "#95A5A6");
                OnStatusUpdated("Emergency math function disable completed");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error during emergency disable: {ex.Message}");
            }
            finally
            {
                isModeChanging = false;
            }
        }

        /// <summary>
        /// Get current mathematics settings
        /// </summary>
        public MathematicsSettings GetCurrentSettings()
        {
            try
            {
                SaveSettingsFromUI();
                return settings;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error getting current settings: {ex.Message}");
                return settings ?? new MathematicsSettings();
            }
        }

        #endregion

        #region Event Handlers for Status Updates

        /// <summary>
        /// Event handler for SCPI command generation
        /// </summary>
        private void OnSCPICommandGenerated(string command)
        {
            SCPICommandGenerated?.Invoke(this, new SCPICommandEventArgs(command));
        }

        /// <summary>
        /// Event handler for status updates
        /// </summary>
        private void OnStatusUpdated(string message)
        {
            StatusUpdated?.Invoke(this, new StatusEventArgs(message));
        }

        /// <summary>
        /// Event handler for error occurrences
        /// </summary>
        private void OnErrorOccurred(string error)
        {
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(error));
        }

        #endregion
    }

    #region Event Argument Classes

    /// <summary>
    /// Event args for SCPI command generation
    /// </summary>
    public class SCPICommandEventArgs : EventArgs
    {
        public string Command { get; }
        public DateTime Timestamp { get; }

        public SCPICommandEventArgs(string command)
        {
            Command = command;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Event args for status updates
    /// </summary>
    public class StatusEventArgs : EventArgs
    {
        public string Message { get; }
        public DateTime Timestamp { get; }

        public StatusEventArgs(string message)
        {
            Message = message;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Event args for error reporting
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public string Error { get; }
        public DateTime Timestamp { get; }
        public Exception Exception { get; }

        public ErrorEventArgs(string error, Exception exception = null)
        {
            Error = error;
            Exception = exception;
            Timestamp = DateTime.Now;
        }
    }

    #endregion
}