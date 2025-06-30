using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Mathematics Window - Container for the Mathematics Panel
    /// Handles window-level functionality, menus, status, and file operations
    /// </summary>
    public partial class MathematicsWindow : Window
    {
        #region Private Fields

        private bool isInitialized = false;
        private string currentConfigurationFile = string.Empty;
        private MathematicsSettings lastSavedSettings;
        private DispatcherTimer statusTimer;
        private bool isConnectedToOscilloscope = false;
       // private MathematicsWindow _mathematicsWindow;

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

        /// <summary>
        /// Event raised when window requests connection status update
        /// </summary>
        public event EventHandler<bool> ConnectionStatusRequested;

        #endregion

        #region Constructor and Initialization

        /// <summary>
        /// Initialize the Mathematics Window
        /// </summary>
        public MathematicsWindow()
        {
            InitializeComponent();
            InitializeWindow();
        }

        /// <summary>
        /// Initialize window components and settings
        /// </summary>
        private void InitializeWindow()
        {
            try
            {
                // Subscribe to panel events
                if (MathPanel != null)
                {
                    MathPanel.SCPICommandGenerated += OnMathPanelSCPICommand;
                    MathPanel.ErrorOccurred += OnMathPanelError;
                    MathPanel.StatusUpdated += OnMathPanelStatus;
                }

                // Initialize status timer
                InitializeStatusTimer();

                // Set initial window state
                SetInitialWindowState();

                // Update initial status
                UpdateStatus("Mathematics Functions window initialized");
                UpdateConnectionStatus(false);

                // Store initial settings for change tracking
                lastSavedSettings = MathPanel?.GetCurrentSettings()?.Clone();

                isInitialized = true;

                OnStatusUpdated("Mathematics window opened and ready");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to initialize mathematics window: {ex.Message}");
                MessageBox.Show($"Error initializing Mathematics window: {ex.Message}",
                              "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Initialize status update timer
        /// </summary>
        private void InitializeStatusTimer()
        {
            statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            statusTimer.Tick += StatusTimer_Tick;
            statusTimer.Start();
        }

        /// <summary>
        /// Set initial window appearance and state
        /// </summary>
        private void SetInitialWindowState()
        {
            // Set window properties
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Update timestamp
            UpdateTimestamp();

            // Set initial status icon
            StatusIcon.Text = "🧮";
        }

        #endregion

        #region Event Handlers - Menu Actions

        /// <summary>
        /// Reset all mathematics settings to defaults
        /// </summary>
        private void ResetAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Reset all mathematics settings to factory defaults?\n\n" +
                    "This will:\n" +
                    "• Clear all current configurations\n" +
                    "• Reset to default values\n" +
                    "• Cannot be undone\n\n" +
                    "Continue with reset?",
                    "Confirm Reset All Settings",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (result == MessageBoxResult.Yes)
                {
                    // Reset panel settings
                    var defaultSettings = new MathematicsSettings();
                    MathPanel?.LoadSettings(defaultSettings);

                    // Clear current configuration file
                    currentConfigurationFile = string.Empty;
                    lastSavedSettings = defaultSettings.Clone();

                    // Update window title
                    UpdateWindowTitle("New Configuration");

                    UpdateStatus("All mathematics settings reset to factory defaults");

                    MessageBox.Show("All mathematics settings have been reset to defaults.",
                                  "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error resetting settings: {ex.Message}");
                ShowErrorDialog("Reset Error", ex.Message);
            }
        }

        /// <summary>
        /// Save current configuration to file
        /// </summary>
        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog dialog = new SaveFileDialog
                {
                    Filter = "Math Config Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = GenerateDefaultFileName(),
                    Title = "Save Mathematics Configuration",
                    InitialDirectory = GetConfigurationDirectory()
                };

                if (dialog.ShowDialog() == true)
                {
                    SaveConfigurationToFile(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error saving configuration: {ex.Message}");
                ShowErrorDialog("Save Error", ex.Message);
            }
        }

        /// <summary>
        /// Load configuration from file
        /// </summary>
        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "Math Config Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json",
                    Title = "Load Mathematics Configuration",
                    InitialDirectory = GetConfigurationDirectory()
                };

                if (dialog.ShowDialog() == true)
                {
                    LoadConfigurationFromFile(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error loading configuration: {ex.Message}");
                ShowErrorDialog("Load Error", ex.Message);
            }
        }

        /// <summary>
        /// Show presets selection dialog
        /// </summary>
        private void ShowPresets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var presets = MathematicsSettings.GetFactoryPresets();
                var presetDialog = CreatePresetSelectionDialog(presets);

                if (presetDialog.ShowDialog() == true)
                {
                    // Preset applied through dialog
                    UpdateStatus("Factory preset applied successfully");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error showing presets: {ex.Message}");
                ShowErrorDialog("Presets Error", ex.Message);
            }
        }

        /// <summary>
        /// Show templates selection dialog
        /// </summary>
        private void ShowTemplates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var templatesInfo = GetTemplateInformation();
                MessageBox.Show(templatesInfo, "Mathematics Function Templates",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error showing templates: {ex.Message}");
                ShowErrorDialog("Templates Error", ex.Message);
            }
        }

        /// <summary>
        /// Validate current configuration
        /// </summary>
        private void ValidateConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> errors = new List<string>(); // Initialize the errors list

                if (MathPanel?.ValidateConfiguration(out errors) == true)
                {
                    MessageBox.Show("✅ Configuration validation passed!\n\nAll settings are valid and ready for use.",
                                  "Validation Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateStatus("Configuration validation: PASSED");
                }
                else
                {
                    var errorMessage = "❌ Configuration validation failed:\n\n• " + string.Join("\n• ", errors);
                    MessageBox.Show(errorMessage, "Validation Errors",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    UpdateStatus($"Configuration validation: FAILED ({errors.Count} errors)");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error validating configuration: {ex.Message}");
                ShowErrorDialog("Validation Error", ex.Message);
            }
        }

        /// <summary>
        /// Export SCPI commands to text file
        /// </summary>
        private void ExportCommands_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog dialog = new SaveFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|SCPI Files (*.scpi)|*.scpi|All Files (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = $"MathCommands_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    Title = "Export SCPI Commands"
                };

                if (dialog.ShowDialog() == true)
                {
                    ExportSCPICommands(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error exporting commands: {ex.Message}");
                ShowErrorDialog("Export Error", ex.Message);
            }
        }

        /// <summary>
        /// Show help documentation
        /// </summary>
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowHelpDialog();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error showing help: {ex.Message}");
                ShowErrorDialog("Help Error", ex.Message);
            }
        }

        /// <summary>
        /// Show about dialog
        /// </summary>
        private void About_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowAboutDialog();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error showing about dialog: {ex.Message}");
                ShowErrorDialog("About Error", ex.Message);
            }
        }

        #endregion

        #region Event Handlers - Preset Applications

        /// <summary>
        /// Apply Basic Addition preset
        /// </summary>
        private void ApplyPreset_BasicAddition(object sender, RoutedEventArgs e)
        {
            try
            {
                MathPanel?.ApplyPreset("BASIC_ADD");
                UpdateStatus("Applied Basic Addition preset");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying Basic Addition preset: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply FFT Analysis preset
        /// </summary>
        private void ApplyPreset_FFTAnalysis(object sender, RoutedEventArgs e)
        {
            try
            {
                MathPanel?.ApplyPreset("DEFAULT_FFT");
                UpdateStatus("Applied FFT Analysis preset");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying FFT Analysis preset: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply Low Pass Filter preset
        /// </summary>
        private void ApplyPreset_LowPassFilter(object sender, RoutedEventArgs e)
        {
            try
            {
                MathPanel?.ApplyPreset("LOW_PASS_FILTER");
                UpdateStatus("Applied Low Pass Filter preset");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying Low Pass Filter preset: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply Integration preset
        /// </summary>
        private void ApplyPreset_Integration(object sender, RoutedEventArgs e)
        {
            try
            {
                MathPanel?.ApplyPreset("INTEGRATION");
                UpdateStatus("Applied Integration preset");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying Integration preset: {ex.Message}");
            }
        }

        /// <summary>
        /// Copy SCPI commands to clipboard
        /// </summary>
        private void CopyCommands_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // This would be implemented to get commands from the panel
                var commands = GetCurrentSCPICommands();
                if (!string.IsNullOrEmpty(commands))
                {
                    Clipboard.SetText(commands);
                    UpdateStatus("SCPI commands copied to clipboard");

                    // Visual feedback
                    ShowTemporaryStatusMessage("Commands copied!", TimeSpan.FromSeconds(2));
                }
                else
                {
                    MessageBox.Show("No SCPI commands available to copy.", "Copy Commands",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error copying commands: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers - Panel Integration

        /// <summary>
        /// Handle SCPI command generated by mathematics panel
        /// </summary>
        private void OnMathPanelSCPICommand(object sender, string command)
        {
            try
            {
                // Forward to main application
                SCPICommandGenerated?.Invoke(this, command);

                // Update status
                var commandName = command.Split(' ')[0];
                UpdateStatus($"Command sent: {commandName}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error forwarding SCPI command: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle error from mathematics panel
        /// </summary>
        private void OnMathPanelError(object sender, string error)
        {
            OnErrorOccurred($"Math Panel: {error}");
        }

        /// <summary>
        /// Handle status update from mathematics panel
        /// </summary>
        private void OnMathPanelStatus(object sender, string status)
        {
            UpdateStatus($"Panel: {status}");
        }

        #endregion

        #region Event Handlers - Window Events

        /// <summary>
        /// Handle window closing
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Check for unsaved changes
                if (HasUnsavedChanges())
                {
                    var result = MessageBox.Show(
                        "You have unsaved changes to your mathematics configuration.\n\n" +
                        "Do you want to save before closing?",
                        "Unsaved Changes",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            SaveConfig_Click(this, new RoutedEventArgs());
                            break;
                        case MessageBoxResult.Cancel:
                            e.Cancel = true;
                            return;
                    }
                }

                // Cleanup
                CleanupResources();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error during window closing: {ex.Message}");
            }

            base.OnClosing(e);
        }

        /// <summary>
        /// Handle status timer tick
        /// </summary>
        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                UpdateTimestamp();

                // Request connection status update periodically
                ConnectionStatusRequested?.Invoke(this, isConnectedToOscilloscope);
            }
            catch (Exception ex)
            {
                // Don't let timer errors break the application
                System.Diagnostics.Debug.WriteLine($"Status timer error: {ex.Message}");
            }
        }

        #endregion

        #region File Operations

        /// <summary>
        /// Save configuration to specified file
        /// </summary>
        private void SaveConfigurationToFile(string filePath)
        {
            try
            {
                var settings = MathPanel?.GetCurrentSettings();
                if (settings != null)
                {
                    settings.SaveToFile(filePath);

                    currentConfigurationFile = filePath;
                    lastSavedSettings = settings.Clone();

                    UpdateWindowTitle(Path.GetFileNameWithoutExtension(filePath));
                    UpdateStatus($"Configuration saved to {Path.GetFileName(filePath)}");

                    MessageBox.Show("Mathematics configuration saved successfully!",
                                  "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    throw new InvalidOperationException("No configuration available to save");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load configuration from specified file
        /// </summary>
        private void LoadConfigurationFromFile(string filePath)
        {
            try
            {
                var settings = MathematicsSettings.LoadFromFile(filePath);
                MathPanel?.LoadSettings(settings);

                currentConfigurationFile = filePath;
                lastSavedSettings = settings.Clone();

                UpdateWindowTitle(Path.GetFileNameWithoutExtension(filePath));
                UpdateStatus($"Configuration loaded from {Path.GetFileName(filePath)}");

                MessageBox.Show("Mathematics configuration loaded successfully!",
                              "Load Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Export SCPI commands to file
        /// </summary>
        private void ExportSCPICommands(string filePath)
        {
            try
            {
                var commands = GetCurrentSCPICommands();
                var exportContent = GenerateCommandExportContent(commands);

                File.WriteAllText(filePath, exportContent);

                UpdateStatus($"SCPI commands exported to {Path.GetFileName(filePath)}");
                MessageBox.Show("SCPI commands exported successfully!",
                              "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to export commands: {ex.Message}", ex);
            }
        }

        #endregion

        #region Helper Methods - File Operations

        /// <summary>
        /// Generate default filename for saving
        /// </summary>
        private string GenerateDefaultFileName()
        {
            var settings = MathPanel?.GetCurrentSettings();
            var configName = settings?.ConfigurationName ?? "MathConfig";
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{configName}_{timestamp}.json";
        }

        /// <summary>
        /// Get configuration directory
        /// </summary>
        private string GetConfigurationDirectory()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configPath = Path.Combine(appDataPath, "DS1000Z_E_Control", "Mathematics");

            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }

            return configPath;
        }

        /// <summary>
        /// Get current SCPI commands from panel
        /// </summary>
        private string GetCurrentSCPICommands()
        {
            // This would need to be implemented to get commands from the panel
            // For now, return a placeholder
            return "// SCPI Commands would be retrieved from MathematicsPanel";
        }

        /// <summary>
        /// Generate export content for commands
        /// </summary>
        private string GenerateCommandExportContent(string commands)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Rigol DS1000Z-E Mathematics SCPI Commands");
            sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"# Configuration: {MathPanel?.GetCurrentSettings()?.ConfigurationName ?? "Unknown"}");
            sb.AppendLine();
            sb.AppendLine("# Commands:");
            sb.AppendLine(commands);
            sb.AppendLine();
            sb.AppendLine("# End of file");

            return sb.ToString();
        }

        #endregion

        #region Helper Methods - UI Dialogs

        /// <summary>
        /// Create preset selection dialog
        /// </summary>
        private Window CreatePresetSelectionDialog(Dictionary<string, MathematicsSettings> presets)
        {
            var dialog = new Window
            {
                Title = "Select Mathematics Preset",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            // Implementation would create a ListBox with presets
            // For now, return basic dialog
            return dialog;
        }

        /// <summary>
        /// Show help dialog
        /// </summary>
        private void ShowHelpDialog()
        {
            var helpText = @"MATHEMATICS FUNCTIONS HELP

BASIC OPERATIONS:
• Add: Channel 1 + Channel 2
• Subtract: Channel 1 - Channel 2  
• Multiply: Channel 1 × Channel 2
• Divide: Channel 1 ÷ Channel 2

FFT ANALYSIS:
• Rectangular: Basic windowing, good for transients
• Blackman: Excellent frequency resolution
• Hanning: Good general purpose windowing
• Hamming: Similar to Hanning, slightly different response

DIGITAL FILTERS:
• Low Pass: Passes frequencies below cutoff
• High Pass: Passes frequencies above cutoff
• Band Pass: Passes frequencies between W1 and W2
• Band Stop: Blocks frequencies between W1 and W2

ADVANCED FUNCTIONS:
• Integration: Calculate area under curve
• Differentiation: Calculate rate of change
• Square Root: Mathematical square root function
• Logarithms: Base 10 and natural logarithms
• Exponential: e^x function
• Absolute Value: |x| function

CONFIGURATION:
• Save/Load: Store configurations for reuse
• Presets: Quick apply common setups
• Validation: Check parameter validity
• Export: Save SCPI commands to file

For detailed SCPI command reference, see the Rigol DS1000Z-E Programming Guide.";

            MessageBox.Show(helpText, "Mathematics Functions Help",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Show about dialog
        /// </summary>
        private void ShowAboutDialog()
        {
            var aboutText = @"MATHEMATICS FUNCTIONS MODULE

Version: 1.0.0
Part of: DS1000Z-E USB Control Application

FEATURES:
✓ Basic arithmetic operations (Add, Subtract, Multiply, Divide)
✓ FFT analysis with multiple windowing functions
✓ Digital filtering (Low/High/Band pass and stop)
✓ Advanced math functions (Integration, Differentiation)
✓ Configuration save/load with JSON format
✓ Factory presets for common operations
✓ Real-time SCPI command generation
✓ Input validation and error handling

COMPATIBILITY:
• Rigol DS1000Z-E Series Digital Oscilloscopes
• SCPI Command Protocol
• USB and LAN communication

SUPPORT:
• Built-in help system
• Configuration validation
• Error reporting and logging
• Export functionality for commands

© 2024 - Mathematics Functions Module";

            MessageBox.Show(aboutText, "About Mathematics Functions",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Show error dialog
        /// </summary>
        private void ShowErrorDialog(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Get template information
        /// </summary>
        private string GetTemplateInformation()
        {
            return @"MATHEMATICS FUNCTION TEMPLATES

Available templates for quick configuration:

📊 BASIC OPERATIONS:
• Simple Addition (CH1 + CH2)
• Signal Subtraction (CH1 - CH2)
• Power Calculation (CH1 × CH2)
• Ratio Analysis (CH1 ÷ CH2)

📈 FFT ANALYSIS:
• Spectrum Analysis (Hanning window, dB scale)
• Frequency Response (Full display)
• Harmonic Analysis (Blackman window)

🔧 DIGITAL FILTERS:
• Anti-Aliasing (Low Pass, 1kHz-10kHz)
• High Frequency Analysis (High Pass)
• Bandlimited Analysis (Band Pass)
• Notch Filtering (Band Stop)

🔬 ADVANCED MATH:
• Signal Integration (Area calculation)
• Edge Detection (Differentiation)
• Magnitude Analysis (Absolute value)
• Logarithmic Scaling (Log functions)

Select a template from the presets menu to apply these configurations automatically.";
        }

        #endregion

        #region Status Management

        /// <summary>
        /// Update main status text
        /// </summary>
        private void UpdateStatus(string message)
        {
            try
            {
                if (StatusText != null)
                {
                    StatusText.Text = message;
                    UpdateTimestamp();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating status: {ex.Message}");
            }
        }

        /// <summary>
        /// Update connection status indicator
        /// </summary>
        public void UpdateConnectionStatus(bool isConnected)
        {
            try
            {
                isConnectedToOscilloscope = isConnected;

                if (ConnectionIndicator != null && ConnectionText != null)
                {
                    ConnectionIndicator.Fill = new SolidColorBrush(isConnected ? Colors.Green : Colors.Red);
                    ConnectionText.Text = isConnected ? "Connected" : "Disconnected";
                    ConnectionText.Foreground = new SolidColorBrush(isConnected ? Colors.LightGreen : Colors.LightCoral);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating connection status: {ex.Message}");
            }
        }

        /// <summary>
        /// Update timestamp display
        /// </summary>
        private void UpdateTimestamp()
        {
            try
            {
                if (TimestampText != null)
                {
                    TimestampText.Text = $"Last Updated: {DateTime.Now:HH:mm:ss}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating timestamp: {ex.Message}");
            }
        }

        /// <summary>
        /// Show temporary status message
        /// </summary>
        private void ShowTemporaryStatusMessage(string message, TimeSpan duration)
        {
            try
            {
                var originalMessage = StatusText?.Text;
                UpdateStatus(message);

                var timer = new DispatcherTimer
                {
                    Interval = duration
                };
                timer.Tick += (s, e) =>
                {
                    UpdateStatus(originalMessage ?? "Ready");
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing temporary message: {ex.Message}");
            }
        }

        /// <summary>
        /// Update window title
        /// </summary>
        private void UpdateWindowTitle(string configurationName = null)
        {
            try
            {
                var baseTitle = "Mathematics Functions - Rigol DS1000Z-E";

                if (!string.IsNullOrEmpty(configurationName))
                {
                    this.Title = $"{baseTitle} - {configurationName}";
                }
                else
                {
                    this.Title = baseTitle;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating window title: {ex.Message}");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if there are unsaved changes
        /// </summary>
        private bool HasUnsavedChanges()
        {
            try
            {
                var currentSettings = MathPanel?.GetCurrentSettings();

                if (currentSettings == null || lastSavedSettings == null)
                    return false;

                return !currentSettings.IsEquivalentTo(lastSavedSettings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking unsaved changes: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set connection status from external source
        /// </summary>
        /// <param name="isConnected">Connection status</param>
        public void SetConnectionStatus(bool isConnected)
        {
            UpdateConnectionStatus(isConnected);
        }

        /// <summary>
        /// Apply specific preset by name
        /// </summary>
        /// <param name="presetName">Name of preset to apply</param>
        public void ApplyPreset(string presetName)
        {
            try
            {
                MathPanel?.ApplyPreset(presetName);
                UpdateStatus($"Applied preset: {presetName}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying preset {presetName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Load configuration file by path
        /// </summary>
        /// <param name="filePath">Path to configuration file</param>
        public void LoadConfiguration(string filePath)
        {
            try
            {
                LoadConfigurationFromFile(filePath);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error loading configuration from {filePath}: {ex.Message}");
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup window resources
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                // Stop timer
                statusTimer?.Stop();

                // Unsubscribe from panel events
                if (MathPanel != null)
                {
                    MathPanel.SCPICommandGenerated -= OnMathPanelSCPICommand;
                    MathPanel.ErrorOccurred -= OnMathPanelError;
                    MathPanel.StatusUpdated -= OnMathPanelStatus;
                }

                OnStatusUpdated("Mathematics window closed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
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

        private void MathPanel_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}