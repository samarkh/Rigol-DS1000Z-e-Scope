using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;

namespace DS1000Z_E_USB_Control.Mathematics
{
    public partial class MathematicsPanel : UserControl
    {
        #region Fields and Properties

        private MathematicsController controller;
        private MathematicsSettings settings;
        private bool isInitializing = false;
        private bool isModeChanging = false;
        private string currentActiveMode = "BasicOperations";

        // Events
        public event EventHandler<string> StatusUpdated;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<string> SCPICommandGenerated;

        #endregion

        #region Constructor and Initialization

        public MathematicsPanel()
        {
            InitializeComponent();
            InitializePanel();
        }

        private void InitializePanel()
        {
            try
            {
                controller = new MathematicsController();
                settings = new MathematicsSettings();

                LoadDefaultSettings();
                SetInitialModeState();

                OnStatusUpdated("Mathematics panel initialized - Basic Operations mode active");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error initializing mathematics panel: {ex.Message}");
            }
        }

        private void SetInitialModeState()
        {
            // Ensure Basic Operations is initially visible
            ShowOnlySection("BasicOperations");
            UpdateStatusIndicator("BasicOperations", "Basic Operations Mode Active");
        }

        #endregion

        #region Math Mode Selection Logic

        /// <summary>
        /// Handle math mode selection change - implements mutual exclusivity with 500ms delay
        /// </summary>
        private async void MathModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializing || isModeChanging) return;

            try
            {
                isModeChanging = true;
                var selectedItem = MathModeCombo.SelectedItem as ComboBoxItem;
                if (selectedItem?.Tag == null) return;

                string newMode = selectedItem.Tag.ToString();
                if (newMode == currentActiveMode) return;

                OnStatusUpdated($"Switching from {GetModeDisplayName(currentActiveMode)} to {GetModeDisplayName(newMode)}...");

                // Step 1: Collapse all other modes
                await CollapseAllModes();

                // Step 2: Disable current active math function
                await DisableCurrentMathFunction();

                // Step 3: Wait 500ms for clean disengagement
                UpdateStatusIndicator(newMode, "Switching modes - please wait...", "#F39C12");
                await Task.Delay(500);

                // Step 4: Activate newly selected mode
                await ActivateNewMode(newMode);

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
        /// Disable current active math function via SCPI
        /// </summary>
        private async Task DisableCurrentMathFunction()
        {
            try
            {
                // Send MATH:DISPLAY OFF command to disable current math function
                string command = ":MATH:DISPlay OFF";
                OnSCPICommandGenerated(command);

                // Simulate command execution delay
                await Task.Delay(100);

                OnStatusUpdated("Current math function disabled");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error disabling current math function: {ex.Message}");
            }
        }

        /// <summary>
        /// Activate the newly selected math mode
        /// </summary>
        private async Task ActivateNewMode(string mode)
        {
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    ShowOnlySection(mode);
                    UpdateStatusIndicator(mode, $"{GetModeDisplayName(mode)} Mode Active", "#2ECC71");
                });
            });

            OnStatusUpdated($"{GetModeDisplayName(mode)} mode activated and ready");
        }

        /// <summary>
        /// Show only the specified section
        /// </summary>
        private void ShowOnlySection(string mode)
        {
            // Hide all sections first
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

                var command = controller.ApplyBasicOperation(source1, source2, operation);
                OnSCPICommandGenerated(command);
                await Task.Delay(100); // Small delay for command execution

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
        /// Apply FFT analysis
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
                    throw new InvalidOperationException("Please select valid FFT parameters");
                }

                OnStatusUpdated($"Applying FFT analysis: {source} with {window} window");

                var command = controller.ApplyFFTAnalysis(source, window, split, unit);
                OnSCPICommandGenerated(command);
                await Task.Delay(100); // Small delay for command execution

                UpdateStatusIndicator("FFTAnalysis", $"FFT analysis applied: {window} window", "#2196F3");
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
        /// Apply digital filter
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
                var w1Text = FilterW1Text.Text?.Trim();
                var w2Text = FilterW2Text.Text?.Trim();

                if (string.IsNullOrEmpty(filterType))
                {
                    throw new InvalidOperationException("Please select a filter type");
                }

                if (string.IsNullOrEmpty(w1Text) || string.IsNullOrEmpty(w2Text))
                {
                    throw new InvalidOperationException("Please enter valid frequency values");
                }

                OnStatusUpdated($"Applying digital filter: {filterType} ({w1Text}Hz - {w2Text}Hz)");

                var command = controller.ApplyDigitalFilter(filterType, w1Text, w2Text);
                OnSCPICommandGenerated(command);
                await Task.Delay(100); // Small delay for command execution

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
                var startPoint = StartPointText.Text?.Trim();
                var endPoint = EndPointText.Text?.Trim();

                if (string.IsNullOrEmpty(function))
                {
                    throw new InvalidOperationException("Please select an advanced math function");
                }

                OnStatusUpdated($"Applying advanced math: {function} ({startPoint} to {endPoint})");

                var command = controller.ApplyAdvancedMathFunction(function, startPoint, endPoint);
                OnSCPICommandGenerated(command);
                await Task.Delay(100); // Small delay for command execution

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

                string command = ":MATH:DISPlay OFF";
                OnSCPICommandGenerated(command);

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

        #region Helper Methods

        /// <summary>
        /// Get selected ComboBox tag value
        /// </summary>
        private string GetSelectedComboBoxTag(ComboBox comboBox)
        {
            return (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        }

        /// <summary>
        /// Show mode mismatch error
        /// </summary>
        private void ShowModeError(string expectedMode)
        {
            var message = $"This function is only available in {expectedMode} mode. Please select {expectedMode} from the mode dropdown.";
            MessageBox.Show(message, "Wrong Mathematics Mode", MessageBoxButton.OK, MessageBoxImage.Warning);
            OnErrorOccurred($"Attempted to use {expectedMode} function while in {GetModeDisplayName(currentActiveMode)} mode");
        }

        /// <summary>
        /// Load default settings to UI controls
        /// </summary>
        private void LoadDefaultSettings()
        {
            isInitializing = true;
            try
            {
                // Set default values for all controls
                Source1Combo.SelectedIndex = 0; // Channel 1
                Source2Combo.SelectedIndex = 1; // Channel 2
                OperationCombo.SelectedIndex = 0; // ADD

                FFTSourceCombo.SelectedIndex = 0; // Channel 1
                FFTWindowCombo.SelectedIndex = 0; // Rectangular
                FFTSplitCombo.SelectedIndex = 0; // Full
                FFTUnitCombo.SelectedIndex = 0; // VRMS

                FilterTypeCombo.SelectedIndex = 0; // Low Pass
                FilterW1Text.Text = "1000";
                FilterW2Text.Text = "10000";

                AdvancedFunctionCombo.SelectedIndex = 0; // Integration
                StartPointText.Text = "0";
                EndPointText.Text = "100";

                MathDisplayCheckbox.IsChecked = true;
                InvertCheckbox.IsChecked = false;
                ScaleText.Text = "1.0";
                OffsetText.Text = "0.0";
            }
            finally
            {
                isInitializing = false;
            }
        }

        /// <summary>
        /// Save UI control values to settings model
        /// </summary>
        private void SaveSettingsFromUI()
        {
            // Basic operation settings
            settings.Source1 = GetSelectedComboBoxTag(Source1Combo) ?? "CHANnel1";
            settings.Source2 = GetSelectedComboBoxTag(Source2Combo) ?? "CHANnel2";
            settings.Operation = GetSelectedComboBoxTag(OperationCombo) ?? "ADD";

            // FFT settings
            settings.FFTSource = GetSelectedComboBoxTag(FFTSourceCombo) ?? "CHANnel1";
            settings.FFTWindow = GetSelectedComboBoxTag(FFTWindowCombo) ?? "RECTangular";
            settings.FFTSplit = GetSelectedComboBoxTag(FFTSplitCombo) ?? "FULL";
            settings.FFTUnit = GetSelectedComboBoxTag(FFTUnitCombo) ?? "VRMS";

            // Filter settings
            settings.FilterType = GetSelectedComboBoxTag(FilterTypeCombo) ?? "LPASs";
            settings.FilterW1 = FilterW1Text.Text ?? "1000";
            settings.FilterW2 = FilterW2Text.Text ?? "10000";

            // Advanced math settings
            settings.AdvancedFunction = GetSelectedComboBoxTag(AdvancedFunctionCombo) ?? "INTG";
            settings.StartPoint = StartPointText.Text ?? "0";
            settings.EndPoint = EndPointText.Text ?? "100";

            // Display settings
            settings.MathDisplayEnabled = MathDisplayCheckbox.IsChecked ?? true;
            settings.InvertWaveform = InvertCheckbox.IsChecked ?? false;
            settings.Scale = ScaleText.Text ?? "1.0";
            settings.Offset = OffsetText.Text ?? "0.0";

            // Store current active mode
            settings.ActiveMode = currentActiveMode;
        }

        /// <summary>
        /// Load settings from UI to settings model  
        /// </summary>
        private void LoadSettingsToUI()
        {
            isInitializing = true;
            try
            {
                // Basic operation settings
                SelectComboBoxItemByTag(Source1Combo, settings.Source1);
                SelectComboBoxItemByTag(Source2Combo, settings.Source2);
                SelectComboBoxItemByTag(OperationCombo, settings.Operation);

                // FFT settings
                SelectComboBoxItemByTag(FFTSourceCombo, settings.FFTSource);
                SelectComboBoxItemByTag(FFTWindowCombo, settings.FFTWindow);
                SelectComboBoxItemByTag(FFTSplitCombo, settings.FFTSplit);
                SelectComboBoxItemByTag(FFTUnitCombo, settings.FFTUnit);

                // Filter settings
                SelectComboBoxItemByTag(FilterTypeCombo, settings.FilterType);
                FilterW1Text.Text = settings.FilterW1;
                FilterW2Text.Text = settings.FilterW2;

                // Advanced math settings
                SelectComboBoxItemByTag(AdvancedFunctionCombo, settings.AdvancedFunction);
                StartPointText.Text = settings.StartPoint;
                EndPointText.Text = settings.EndPoint;

                // Display settings
                MathDisplayCheckbox.IsChecked = settings.MathDisplayEnabled;
                InvertCheckbox.IsChecked = settings.InvertWaveform;
                ScaleText.Text = settings.Scale;
                OffsetText.Text = settings.Offset;
            }
            finally
            {
                isInitializing = false;
            }
        }

        /// <summary>
        /// Select ComboBox item by tag value
        /// </summary>
        private void SelectComboBoxItemByTag(ComboBox comboBox, string tag)
        {
            if (comboBox == null || string.IsNullOrEmpty(tag)) return;

            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Tag?.ToString() == tag)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }
        

        #endregion

        #region Event Raising Methods

        protected virtual void OnStatusUpdated(string message)
        {
            StatusUpdated?.Invoke(this, message);
        }

        protected virtual void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, message);
        }

        protected virtual void OnSCPICommandGenerated(string command)
        {
            SCPICommandGenerated?.Invoke(this, command);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the active math mode programmatically
        /// </summary>
        public async Task SetMathModeAsync(string mode)
        {
            if (currentActiveMode == mode) return;

            // Find and select the corresponding ComboBox item
            foreach (ComboBoxItem item in MathModeCombo.Items)
            {
                if (item.Tag?.ToString() == mode)
                {
                    MathModeCombo.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// Get the current active math mode
        /// </summary>
        public string GetCurrentMathMode()
        {
            return currentActiveMode;
        }

        /// <summary>
        /// Reset all math functions and return to Basic Operations mode
        /// </summary>
        public async Task ResetToBasicOperationsAsync()
        {
            await SetMathModeAsync("BasicOperations");
            LoadDefaultSettings();
            OnStatusUpdated("Reset to Basic Operations mode with default settings");
        }

        /// <summary>
        /// Get current settings from UI - Required by MathematicsWindow
        /// </summary>
        public MathematicsSettings GetCurrentSettings()
        {
            SaveSettingsFromUI();
            return settings;
        }

        /// <summary>
        /// Load settings to UI - Required by MathematicsWindow
        /// </summary>
        public async Task LoadSettings(MathematicsSettings newSettings)
        {
            if (newSettings == null)
            {
                await Task.CompletedTask;
                return;
            }

            settings = newSettings;
            LoadSettingsToUI();

            // Set the active mode if it's different
            if (newSettings.ActiveMode != currentActiveMode)
            {
                await SetMathModeAsync(newSettings.ActiveMode);
            }
            else
            {
                // Ensure we always have an await in async method
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Apply preset configuration - Required by MathematicsWindow
        /// </summary>
        public async Task ApplyPreset(string presetName)
        {
            try
            {
                MathematicsSettings presetSettings = null;

                switch (presetName?.ToUpperInvariant())
                {
                    case "BASIC_ADD":
                        presetSettings = MathematicsSettings.CreateBasicOperationsDefault();
                        break;
                    case "FFT_ANALYSIS":
                        presetSettings = MathematicsSettings.CreateFFTAnalysisDefault();
                        break;
                    case "LOW_PASS_FILTER":
                        presetSettings = MathematicsSettings.CreateDigitalFiltersDefault();
                        break;
                    case "INTEGRATION":
                        presetSettings = MathematicsSettings.CreateAdvancedMathDefault();
                        break;
                    default:
                        throw new ArgumentException($"Unknown preset: {presetName}");
                }

                await LoadSettings(presetSettings);
                OnStatusUpdated($"Applied preset: {presetName}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying preset {presetName}: {ex.Message}");
                // Ensure we have await in async method
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Validate current configuration - Required by MathematicsWindow
        /// </summary>
        public bool ValidateConfiguration(out List<string> errors)
        {
            errors = new List<string>();

            try
            {
                SaveSettingsFromUI();
                errors = settings.ValidateSettingsSimple();

                if (errors.Count == 0)
                {
                    OnStatusUpdated("Configuration validation passed");
                    return true;
                }

                OnErrorOccurred($"Configuration validation failed: {string.Join(", ", errors)}");
                return false;
            }
            catch (Exception ex)
            {
                errors.Add($"Validation error: {ex.Message}");
                OnErrorOccurred($"Error during validation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validate current configuration - Overload without out parameter
        /// </summary>
        public bool ValidateConfiguration()
        {
            List<string> errors;
            return ValidateConfiguration(out errors);
        }

        #endregion
    }
}