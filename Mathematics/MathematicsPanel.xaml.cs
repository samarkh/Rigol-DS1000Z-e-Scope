// ============================================================================
// File: Mathematics/MathematicsPanel.xaml.cs - CORRECTED VERSION
// ============================================================================
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Mathematics Panel - CORRECTED to match actual XAML structure
    /// </summary>
    public partial class MathematicsPanel : UserControl
    {
        #region Fields
        private bool isInitialized = false;
        private bool isModeChanging = false;
        private string currentActiveMode = "BasicOperations";
        private MathematicsSettings currentSettings;

        // SCPI timing configuration (milliseconds)
        private const int RESET_DELAY = 150;
        private const int MODE_CHANGE_DELAY = 500;
        private const int COMMAND_DELAY = 50;
        #endregion

        #region Events
        public event EventHandler<SCPICommandEventArgs> SCPICommandGenerated;
        public event EventHandler<StatusEventArgs> StatusUpdated;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;
        #endregion

        #region Constructor
        public MathematicsPanel()
        {
            InitializeComponent();
            InitializePanel();
        }

        private void InitializePanel()
        {
            try
            {
                currentSettings = new MathematicsSettings();
                SetInitialMode();
                isInitialized = true;
                OnStatusUpdated("Mathematics panel initialized");
                UpdateStatusDisplay("Basic Operations Mode Active");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Panel initialization failed: {ex.Message}");
            }
        }
        #endregion

        #region Properties
        public string GetCurrentMathMode() => currentActiveMode;
        public bool IsModeChanging => isModeChanging;
        public MathematicsSettings GetCurrentSettings() => currentSettings;
        #endregion

        #region Mode Management
        private void SetInitialMode()
        {
            try
            {
                // Show only Basic Operations initially
                BasicOperationsSection.Visibility = Visibility.Visible;
                FFTAnalysisSection.Visibility = Visibility.Collapsed;
                DigitalFiltersSection.Visibility = Visibility.Collapsed;
                AdvancedMathSection.Visibility = Visibility.Collapsed;

                currentActiveMode = "BasicOperations";
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error setting initial mode: {ex.Message}");
            }
        }

        public async Task ChangeMathModeAsync(string newMode)
        {
            if (isModeChanging || currentActiveMode == newMode) return;

            try
            {
                isModeChanging = true;
                OnStatusUpdated($"Changing mode to {newMode}...");
                UpdateStatusDisplay($"Switching to {newMode}...");

                // Step 1: Disable current math display
                OnSCPICommandGenerated(":MATH:DISPlay OFF");
                await Task.Delay(RESET_DELAY);

                // Step 2: Reset math system
                OnSCPICommandGenerated(":MATH:RESet");
                await Task.Delay(RESET_DELAY);

                // Step 3: Update UI visibility
                UpdateModeVisibility(newMode);

                // Step 4: Configure new mode
                await ConfigureModeAsync(newMode);

                currentActiveMode = newMode;
                OnStatusUpdated($"Mode changed to {newMode}");
                UpdateStatusDisplay($"{newMode} Mode Active");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error changing mode: {ex.Message}");
                UpdateStatusDisplay($"Error: {ex.Message}");
            }
            finally
            {
                isModeChanging = false;
            }
        }

        private void UpdateModeVisibility(string mode)
        {
            // Hide all sections
            BasicOperationsSection.Visibility = Visibility.Collapsed;
            FFTAnalysisSection.Visibility = Visibility.Collapsed;
            DigitalFiltersSection.Visibility = Visibility.Collapsed;
            AdvancedMathSection.Visibility = Visibility.Collapsed;

            // Show selected section
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
        #endregion

        #region SCPI Configuration Methods
        private async Task ConfigureBasicOperationsAsync()
        {
            var operation = GetSelectedTag(OperationCombo);
            var source1 = GetSelectedTag(Source1Combo);
            var source2 = GetSelectedTag(Source2Combo);

            OnSCPICommandGenerated($":MATH:OPERator {operation}");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated($":MATH:SOURce1 {source1}");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated($":MATH:SOURce2 {source2}");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated(":MATH:DISPlay ON");
            await Task.Delay(COMMAND_DELAY);
        }

        private async Task ConfigureFFTAnalysisAsync()
        {
            var source = GetSelectedTag(FFTSourceCombo);
            var window = GetSelectedTag(FFTWindowCombo);

            OnSCPICommandGenerated(":MATH:OPERator FFT");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated($":MATH:FFT:SOURce {source}");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated($":MATH:FFT:WINDow {window}");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated(":MATH:FFT:SPLit FULL");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated(":MATH:DISPlay ON");
            await Task.Delay(COMMAND_DELAY);
        }

        private async Task ConfigureDigitalFiltersAsync()
        {
            var filterType = GetSelectedTag(FilterTypeCombo);
            var w1 = FilterW1Text.Text;
            var w2 = FilterW2Text.Text;

            OnSCPICommandGenerated($":MATH:OPERator {filterType}");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated($":MATH:FILTer:W1 {w1}");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated($":MATH:FILTer:W2 {w2}");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated(":MATH:DISPlay ON");
            await Task.Delay(COMMAND_DELAY);
        }

        private async Task ConfigureAdvancedMathAsync()
        {
            var function = GetSelectedTag(AdvancedFunctionCombo);
            var start = StartPointText.Text;  // CORRECTED: was AdvancedStartText
            var end = EndPointText.Text;      // CORRECTED: was AdvancedEndText

            OnSCPICommandGenerated($":MATH:OPERator {function}");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated($":MATH:OPTion:STARt {start}");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated($":MATH:OPTion:END {end}");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated(":MATH:DISPlay ON");
            await Task.Delay(COMMAND_DELAY);
        }
        #endregion

        #region Event Handlers - CORRECTED TO MATCH XAML

        // CORRECTED: Main mode selection handler
        private async void MathModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized || isModeChanging) return;

            try
            {
                var combo = sender as ComboBox;
                var selectedMode = GetSelectedTag(combo);

                if (!string.IsNullOrEmpty(selectedMode))
                {
                    await ChangeMathModeAsync(selectedMode);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error in mode selection: {ex.Message}");
            }
        }

        private async void ApplyBasicOperation_Click(object sender, RoutedEventArgs e)
        {
            await ChangeMathModeAsync("BasicOperations");
        }

        // CORRECTED: Event handler name to match XAML
        private async void ApplyFFT_Click(object sender, RoutedEventArgs e)
        {
            await ChangeMathModeAsync("FFTAnalysis");
        }

        // CORRECTED: Event handler name to match XAML  
        private async void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            await ChangeMathModeAsync("DigitalFilters");
        }

        private async void ApplyAdvancedMath_Click(object sender, RoutedEventArgs e)
        {
            await ChangeMathModeAsync("AdvancedMath");
        }

        private async void DisableMath_Click(object sender, RoutedEventArgs e)
        {
            OnSCPICommandGenerated(":MATH:DISPlay OFF");
            await Task.Delay(COMMAND_DELAY);
            OnSCPICommandGenerated(":MATH:RESet");
            OnStatusUpdated("Math functions disabled");
            UpdateStatusDisplay("Math Disabled");
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateCurrentSettings();
                OnStatusUpdated("Settings saved to memory");
                UpdateStatusDisplay("Settings Saved");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error saving settings: {ex.Message}");
            }
        }
        #endregion

        #region Settings Management
        public async Task LoadSettingsAsync(MathematicsSettings settings)
        {
            try
            {
                currentSettings = settings;
                await ChangeMathModeAsync(settings.ActiveMode);
                OnStatusUpdated("Settings loaded");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error loading settings: {ex.Message}");
            }
        }

        private void UpdateCurrentSettings()
        {
            currentSettings.ActiveMode = currentActiveMode;
            currentSettings.LastModified = DateTime.Now;

            // Update mode-specific settings
            switch (currentActiveMode)
            {
                case "BasicOperations":
                    currentSettings.Operation = GetSelectedTag(OperationCombo);
                    currentSettings.Source1 = GetSelectedTag(Source1Combo);
                    currentSettings.Source2 = GetSelectedTag(Source2Combo);
                    break;
                case "FFTAnalysis":
                    currentSettings.FFTSource = GetSelectedTag(FFTSourceCombo);
                    currentSettings.FFTWindow = GetSelectedTag(FFTWindowCombo);
                    break;
                case "DigitalFilters":
                    currentSettings.FilterType = GetSelectedTag(FilterTypeCombo);
                    currentSettings.FilterW1 = FilterW1Text.Text;
                    currentSettings.FilterW2 = FilterW2Text.Text;
                    break;
                case "AdvancedMath":
                    currentSettings.AdvancedFunction = GetSelectedTag(AdvancedFunctionCombo);
                    currentSettings.StartPoint = StartPointText.Text;  // CORRECTED
                    currentSettings.EndPoint = EndPointText.Text;      // CORRECTED
                    break;
            }

            // Update display settings
            currentSettings.DisplayEnabled = MathDisplayCheckbox.IsChecked == true;
            currentSettings.InvertEnabled = InvertCheckbox.IsChecked == true;
            currentSettings.Scale = ScaleText.Text;
            currentSettings.Offset = OffsetText.Text;
        }
        #endregion

        #region Helper Methods
        private string GetSelectedTag(ComboBox combo)
        {
            return (combo?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
        }

        // ADDED: Update status display in UI
        private void UpdateStatusDisplay(string message)
        {
            try
            {
                if (StatusText != null)
                    StatusText.Text = message;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating status display: {ex.Message}");
            }
        }

        private void OnSCPICommandGenerated(string command)
        {
            try
            {
                var args = new SCPICommandEventArgs(command, "MathematicsPanel", "MATH");
                SCPICommandGenerated?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating SCPI command: {ex.Message}");
            }
        }

        private void OnStatusUpdated(string message)
        {
            try
            {
                var args = new StatusEventArgs(message, StatusLevel.Info, "MathematicsPanel", "MATH");
                StatusUpdated?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating status: {ex.Message}");
            }
        }

        private void OnErrorOccurred(string error)
        {
            try
            {
                var args = new ErrorEventArgs(error)
                {
                    Source = "MathematicsPanel",
                    Category = "MATH",
                    Severity = ErrorSeverity.Error
                };
                ErrorOccurred?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reporting error: {ex.Message}");
            }
        }
        #endregion
    }
}