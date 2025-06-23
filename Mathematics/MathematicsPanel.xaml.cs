using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Mathematics Panel - Main UI controller for math functions
    /// Handles all UI interactions and coordinates with MathematicsController
    /// </summary>
    public partial class MathematicsPanel : UserControl
    {
        #region Private Fields

        private MathematicsController controller;
        private MathematicsSettings settings;
        private bool isCollapsed = false;
        private bool isInitializing = false;

        #endregion

        #region Events

        /// <summary>
        /// Event raised when SCPI command is generated
        /// </summary>
        public event EventHandler<string> SCPICommandGenerated;

        /// <summary>
        /// Event raised when an error occurs
        /// </summary>
        public event EventHandler<string> ErrorOccurred;

        /// <summary>
        /// Event raised for status updates
        /// </summary>
        public event EventHandler<string> StatusUpdated;

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
        /// Initialize panel components and settings
        /// </summary>
        private void InitializePanel()
        {
            isInitializing = true;

            try
            {
                // Initialize controller
                controller = new MathematicsController();
                controller.SCPICommandGenerated += OnControllerSCPICommand;
                controller.ErrorOccurred += OnControllerError;
                controller.StatusUpdated += OnControllerStatus;

                // Initialize settings
                settings = new MathematicsSettings();

                // Load settings to UI
                LoadSettingsToUI();

                // Set initial status
                UpdateSCPIDisplay("Ready to generate SCPI commands...");

                OnStatusUpdated("Mathematics panel initialized successfully");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to initialize mathematics panel: {ex.Message}");
            }
            finally
            {
                isInitializing = false;
            }
        }

        #endregion

        #region UI Event Handlers - Header Controls

        /// <summary>
        /// Toggle panel collapse/expand
        /// </summary>
        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                isCollapsed = !isCollapsed;
                MainContent.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
                ToggleIcon.Text = isCollapsed ? "🔼" : "🔽";
                ToggleText.Text = isCollapsed ? "Expand" : "Collapse";

                OnStatusUpdated($"Mathematics panel {(isCollapsed ? "collapsed" : "expanded")}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error toggling panel: {ex.Message}");
            }
        }

        #endregion

        #region UI Event Handlers - Basic Operations

        /// <summary>
        /// Apply basic math operation (ADD, SUB, MUL, DIV)
        /// </summary>
        private void ApplyBasicOperation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var source1 = GetSelectedComboBoxTag(Source1Combo);
                var source2 = GetSelectedComboBoxTag(Source2Combo);
                var operation = GetSelectedComboBoxTag(OperationCombo);

                if (string.IsNullOrEmpty(source1) || string.IsNullOrEmpty(source2) || string.IsNullOrEmpty(operation))
                {
                    throw new InvalidOperationException("Please select valid sources and operation");
                }

                var commands = controller.ApplyBasicOperation(source1, source2, operation);
                UpdateSCPIDisplay(commands);

                // Update UI to show applied settings
                HighlightAppliedOperation("Basic Operation");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying basic operation: {ex.Message}");
                ShowErrorMessage("Basic Operation Error", ex.Message);
            }
        }

        #endregion

        #region UI Event Handlers - FFT Analysis

        /// <summary>
        /// Apply FFT analysis
        /// </summary>
        private void ApplyFFT_Click(object sender, RoutedEventArgs e)
        {
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

                var commands = controller.ApplyFFTAnalysis(source, window, split, unit);
                UpdateSCPIDisplay(commands);

                // Update UI to show applied settings
                HighlightAppliedOperation("FFT Analysis");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying FFT analysis: {ex.Message}");
                ShowErrorMessage("FFT Analysis Error", ex.Message);
            }
        }

        #endregion

        #region UI Event Handlers - Digital Filters

        /// <summary>
        /// Apply digital filter
        /// </summary>
        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
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

                var commands = controller.ApplyDigitalFilter(filterType, w1Text, w2Text);
                UpdateSCPIDisplay(commands);

                // Update UI to show applied settings
                HighlightAppliedOperation("Digital Filter");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying digital filter: {ex.Message}");
                ShowErrorMessage("Digital Filter Error", ex.Message);
            }
        }

        #endregion

        #region UI Event Handlers - Advanced Math

        /// <summary>
        /// Apply advanced math function
        /// </summary>
        private void ApplyAdvancedMath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var function = GetSelectedComboBoxTag(AdvancedFunctionCombo);
                var startText = StartPointText.Text?.Trim();
                var endText = EndPointText.Text?.Trim();

                if (string.IsNullOrEmpty(function))
                {
                    throw new InvalidOperationException("Please select an advanced function");
                }

                if (string.IsNullOrEmpty(startText) || string.IsNullOrEmpty(endText))
                {
                    throw new InvalidOperationException("Please enter valid start and end points");
                }

                var commands = controller.ApplyAdvancedMathFunction(function, startText, endText);
                UpdateSCPIDisplay(commands);

                // Update UI to show applied settings
                HighlightAppliedOperation("Advanced Math");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying advanced math function: {ex.Message}");
                ShowErrorMessage("Advanced Math Error", ex.Message);
            }
        }

        #endregion

        #region UI Event Handlers - Display Control

        /// <summary>
        /// Update math display settings
        /// </summary>
        private void UpdateDisplay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var enableDisplay = MathDisplayCheckbox.IsChecked ?? false;
                var invert = InvertCheckbox.IsChecked ?? false;
                var scaleText = ScaleText.Text?.Trim();
                var offsetText = OffsetText.Text?.Trim();

                if (string.IsNullOrEmpty(scaleText) || string.IsNullOrEmpty(offsetText))
                {
                    throw new InvalidOperationException("Please enter valid scale and offset values");
                }

                var commands = controller.UpdateDisplaySettings(enableDisplay, invert, scaleText, offsetText);
                UpdateSCPIDisplay(commands);

                // Update UI to show applied settings
                HighlightAppliedOperation("Display Update");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error updating display settings: {ex.Message}");
                ShowErrorMessage("Display Update Error", ex.Message);
            }
        }

        /// <summary>
        /// Reset all math settings
        /// </summary>
        private void ResetMath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Reset all mathematics settings to defaults?\n\nThis will clear all current configurations.",
                    "Confirm Reset",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var command = controller.ResetMathSettings();
                    UpdateSCPIDisplay(command);

                    // Reset UI to defaults
                    settings = new MathematicsSettings();
                    LoadSettingsToUI();

                    HighlightAppliedOperation("Reset Complete");
                    OnStatusUpdated("All math settings reset to defaults");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error resetting math settings: {ex.Message}");
                ShowErrorMessage("Reset Error", ex.Message);
            }
        }

        #endregion

        #region UI Event Handlers - Configuration Management

        /// <summary>
        /// Save math configuration to file
        /// </summary>
        private void SaveMathSetup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog dialog = new SaveFileDialog
                {
                    Filter = "Math Config Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = $"MathSetup_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                    Title = "Save Mathematics Configuration"
                };

                if (dialog.ShowDialog() == true)
                {
                    SaveSettingsFromUI();
                    settings.SaveToFile(dialog.FileName);

                    OnStatusUpdated($"Math configuration saved to {dialog.FileName}");
                    MessageBox.Show("Math setup saved successfully!", "Save Complete",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error saving math setup: {ex.Message}");
                ShowErrorMessage("Save Error", ex.Message);
            }
        }

        /// <summary>
        /// Load math configuration from file
        /// </summary>
        private void LoadMathSetup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "Math Config Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json",
                    Title = "Load Mathematics Configuration"
                };

                if (dialog.ShowDialog() == true)
                {
                    settings = MathematicsSettings.LoadFromFile(dialog.FileName);
                    LoadSettingsToUI();
                    controller.LoadSettings(settings);

                    OnStatusUpdated($"Math configuration loaded from {dialog.FileName}");
                    MessageBox.Show("Math setup loaded successfully!", "Load Complete",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error loading math setup: {ex.Message}");
                ShowErrorMessage("Load Error", ex.Message);
            }
        }

        #endregion

        #region UI Event Handlers - Command Management

        /// <summary>
        /// Copy SCPI commands to clipboard
        /// </summary>
        private void CopyCommands_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var commandText = SCPICommandsText.Text;
                if (!string.IsNullOrEmpty(commandText))
                {
                    Clipboard.SetText(commandText);
                    OnStatusUpdated("SCPI commands copied to clipboard");

                    // Visual feedback
                    var originalBackground = SCPICommandsText.Background;
                    SCPICommandsText.Background = new SolidColorBrush(Colors.LightGreen);

                    // Reset color after brief delay
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(500)
                    };
                    timer.Tick += (s, args) =>
                    {
                        SCPICommandsText.Background = originalBackground;
                        timer.Stop();
                    };
                    timer.Start();
                }
                else
                {
                    MessageBox.Show("No commands to copy.", "Copy Commands",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error copying commands: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear SCPI commands display
        /// </summary>
        private void ClearCommands_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SCPICommandsText.Text = "Ready to generate SCPI commands...";
                controller.ClearCommandHistory();
                OnStatusUpdated("Command display cleared");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error clearing commands: {ex.Message}");
            }
        }

        #endregion

        #region Controller Event Handlers

        /// <summary>
        /// Handle SCPI command generated by controller
        /// </summary>
        private void OnControllerSCPICommand(object sender, string command)
        {
            try
            {
                // Forward to main application
                SCPICommandGenerated?.Invoke(this, command);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error forwarding SCPI command: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle controller errors
        /// </summary>
        private void OnControllerError(object sender, string error)
        {
            OnErrorOccurred($"Controller error: {error}");
        }

        /// <summary>
        /// Handle controller status updates
        /// </summary>
        private void OnControllerStatus(object sender, string status)
        {
            OnStatusUpdated($"Math: {status}");
        }

        #endregion

        #region Settings Management

        /// <summary>
        /// Load settings from model to UI controls
        /// </summary>
        private void LoadSettingsToUI()
        {
            try
            {
                isInitializing = true;

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
        }

        #endregion

        #region UI Helper Methods

        /// <summary>
        /// Get selected tag value from ComboBox
        /// </summary>
        private string GetSelectedComboBoxTag(ComboBox comboBox)
        {
            return (comboBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        }

        /// <summary>
        /// Select ComboBox item by tag value
        /// </summary>
        private void SelectComboBoxItemByTag(ComboBox comboBox, string tagValue)
        {
            if (comboBox == null || string.IsNullOrEmpty(tagValue))
                return;

            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Tag?.ToString() == tagValue)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// Update SCPI commands display
        /// </summary>
        private void UpdateSCPIDisplay(string commands)
        {
            if (!string.IsNullOrEmpty(commands))
            {
                SCPICommandsText.Text = commands;

                // Auto-scroll to bottom
                SCPICommandsText.ScrollToEnd();
            }
        }

        /// <summary>
        /// Highlight applied operation with visual feedback
        /// </summary>
        private void HighlightAppliedOperation(string operationName)
        {
            try
            {
                // Add timestamp and operation info to command display
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var separator = new string('=', 50);
                var header = $"\n{separator}\n[{timestamp}] {operationName} Applied\n{separator}\n";

                SCPICommandsText.Text = header + SCPICommandsText.Text;
                SCPICommandsText.ScrollToHome();
            }
            catch (Exception ex)
            {
                // Don't let visual feedback errors break functionality
                System.Diagnostics.Debug.WriteLine($"Error highlighting operation: {ex.Message}");
            }
        }

        /// <summary>
        /// Show error message dialog
        /// </summary>
        private void ShowErrorMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validate all current UI inputs
        /// </summary>
        private List<string> ValidateUIInputs()
        {
            var errors = new List<string>();

            // Validate frequency inputs
            if (!IsValidNumericInput(FilterW1Text.Text, out _))
                errors.Add("Invalid lower frequency value");

            if (!IsValidNumericInput(FilterW2Text.Text, out _))
                errors.Add("Invalid upper frequency value");

            // Validate math points
            if (!IsValidNumericInput(StartPointText.Text, out _))
                errors.Add("Invalid start point value");

            if (!IsValidNumericInput(EndPointText.Text, out _))
                errors.Add("Invalid end point value");

            // Validate scale and offset
            if (!IsValidNumericInput(ScaleText.Text, out _))
                errors.Add("Invalid scale value");

            if (!IsValidNumericInput(OffsetText.Text, out _))
                errors.Add("Invalid offset value");

            return errors;
        }

        /// <summary>
        /// Check if text input is valid numeric value
        /// </summary>
        private bool IsValidNumericInput(string input, out double value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            return double.TryParse(input.Trim(), out value) && !double.IsInfinity(value) && !double.IsNaN(value);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Apply preset configuration
        /// </summary>
        /// <param name="presetName">Name of preset to apply</param>
        public void ApplyPreset(string presetName)
        {
            try
            {
                var commands = controller.ApplyPreset(presetName);
                UpdateSCPIDisplay(commands);
                HighlightAppliedOperation($"Preset: {presetName}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying preset {presetName}: {ex.Message}");
                ShowErrorMessage("Preset Error", ex.Message);
            }
        }

        /// <summary>
        /// Get current panel settings
        /// </summary>
        public MathematicsSettings GetCurrentSettings()
        {
            SaveSettingsFromUI();
            return settings;
        }

        /// <summary>
        /// Load settings into panel
        /// </summary>
        public void LoadSettings(MathematicsSettings newSettings)
        {
            if (newSettings != null)
            {
                settings = newSettings;
                LoadSettingsToUI();
                controller.LoadSettings(settings);
            }
        }

        /// <summary>
        /// Validate panel configuration
        /// </summary>
        public bool ValidateConfiguration(out List<string> errors)
        {
            errors = ValidateUIInputs();
            return errors.Count == 0;
        }

        #endregion

        #region Event Raising Methods

        /// <summary>
        /// Raise error occurred event
        /// </summary>
        protected virtual void OnErrorOccurred(string error)
        {
            ErrorOccurred?.Invoke(this, error);
        }

        /// <summary>
        /// Raise status updated event
        /// </summary>
        protected virtual void OnStatusUpdated(string status)
        {
            StatusUpdated?.Invoke(this, status);
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup resources when panel is unloaded
        /// </summary>
        private void OnPanelUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (controller != null)
                {
                    controller.SCPICommandGenerated -= OnControllerSCPICommand;
                    controller.ErrorOccurred -= OnControllerError;
                    controller.StatusUpdated -= OnControllerStatus;
                    controller.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during panel cleanup: {ex.Message}");
            }
        }

        #endregion
    }
}