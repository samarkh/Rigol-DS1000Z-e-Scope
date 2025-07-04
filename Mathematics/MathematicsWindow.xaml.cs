// ============================================================================
// File: Mathematics/MathematicsWindow.xaml.cs - SIMPLIFIED VERSION
// ============================================================================
using System;
using System.Threading.Tasks;
using System.Windows;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Mathematics Window - Simplified to avoid missing control references
    /// </summary>
    public partial class MathematicsWindow : Window
    {
        #region Fields
        private bool isInitialized = false;
        #endregion

        #region Events
        public event EventHandler<SCPICommandEventArgs> SCPICommandGenerated;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;
        public event EventHandler<StatusEventArgs> StatusUpdated;
        #endregion

        #region Constructor
        public MathematicsWindow()
        {
            InitializeComponent();
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            OnStatusUpdated("🔥 STEP 1: Method started");

            try
            {
                OnStatusUpdated("🔥 STEP 2: Inside try block");

                // Subscribe to panel events
                if (MathPanel != null)
                {
                    OnStatusUpdated("🔥 STEP 3: MathPanel exists");
                    MathPanel.SCPICommandGenerated += OnMathPanelSCPICommand;
                    MathPanel.StatusUpdated += OnMathPanelStatus;
                    MathPanel.ErrorOccurred += OnMathPanelError;
                }
                else
                {
                    OnStatusUpdated("🔥 STEP 3: MathPanel is NULL");
                }

                isInitialized = true;
                OnStatusUpdated("🔥 STEP 4: About to call final message");
                OnStatusUpdated("Mathematics window initialized");
                OnStatusUpdated("🔥 STEP 5: Method completed successfully");
            }
            catch (Exception ex)
            {
                OnStatusUpdated($"🔥 EXCEPTION: {ex.Message}");
                OnErrorOccurred($"Window initialization failed: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods
        public async Task<bool> InitializeAsync(object visaManager)
        {
            try
            {
                OnStatusUpdated("Mathematics window ready");
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Async initialization failed: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Event Handlers
        private void OnMathPanelSCPICommand(object sender, SCPICommandEventArgs e)
        {
            try
            {
                SCPICommandGenerated?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error forwarding SCPI command: {ex.Message}");
            }
        }

        private void OnMathPanelStatus(object sender, StatusEventArgs e)
        {
            try
            {
                StatusUpdated?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error forwarding status: {ex.Message}");
            }
        }

        private void OnMathPanelError(object sender, ErrorEventArgs e)
        {
            try
            {
                ErrorOccurred?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in error handler: {ex.Message}");
            }
        }

        // Menu event handlers - SIMPLIFIED
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            string helpText = @"MATHEMATICS FUNCTIONS HELP

Select mode → Configure parameters → Apply
Modes: Basic Operations, FFT Analysis, Digital Filters, Advanced Math

SCPI Timing: 150ms reset, 500ms mode change, 50ms commands";

            MessageBox.Show(helpText, "Mathematics Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            string aboutText = @"MATHEMATICS FUNCTIONS
Version: 2.0 (Clean Implementation)
Compatible with Rigol DS1000Z-E Series";

            MessageBox.Show(aboutText, "About Mathematics", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void MathPanel_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                OnStatusUpdated("📊 MathPanel_Loaded event fired - starting debug");

                // Debug: Check if MathPanel exists
                if (MathPanel == null)
                {
                    OnStatusUpdated("❌ DEBUG: MathPanel is NULL!");
                    return;
                }
                else
                {
                    OnStatusUpdated("✅ DEBUG: MathPanel exists");
                }

                // Debug: Check what mode we're in
                string currentMode = MathPanel.GetCurrentMathMode();
                OnStatusUpdated($"🔍 DEBUG: Current math mode = '{currentMode}'");

                // Debug: Check the comparison
                bool isBasicOperations = currentMode == "BasicOperations";
                OnStatusUpdated($"🔍 DEBUG: Is BasicOperations? {isBasicOperations}");

                // Wait a moment for the panel to fully settle
                await Task.Delay(200);

                // Apply default basic math operation if conditions are met
                if (MathPanel != null && MathPanel.GetCurrentMathMode() == "BasicOperations")
                {
                    OnStatusUpdated("✅ DEBUG: Conditions met - applying default math operation");
                    // Trigger the apply basic operation to activate math immediately
                    await MathPanel.ChangeMathModeAsync("BasicOperations");
                    OnStatusUpdated("✅ Default math operation applied: CHANnel1 + CHANnel2");
                }
                else
                {
                    OnStatusUpdated($"❌ DEBUG: Conditions NOT met - MathPanel={MathPanel != null}, Mode='{MathPanel?.GetCurrentMathMode()}'");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error in MathPanel_Loaded debug: {ex.Message}");
            }
        }
        #endregion

        #region Helper Methods
        private void OnErrorOccurred(string error)
        {
            try
            {
                var args = new ErrorEventArgs(error)
                {
                    Source = "MathematicsWindow",
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

        private void OnStatusUpdated(string message)
        {
            try
            {
                var args = new StatusEventArgs(message, StatusLevel.Info, "MathematicsWindow", "MATH");
                StatusUpdated?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating status: {ex.Message}");
            }
        }
        #endregion

        #region Cleanup
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Unsubscribe from events
                if (MathPanel != null)
                {
                    MathPanel.SCPICommandGenerated -= OnMathPanelSCPICommand;
                    MathPanel.StatusUpdated -= OnMathPanelStatus;
                    MathPanel.ErrorOccurred -= OnMathPanelError;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
            finally
            {
                base.OnClosed(e);
            }
        }
        #endregion
    }
}