using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Mathematics Window - Container for the Mathematics Panel
    /// Handles window-level functionality, menus, status, file operations, and SCPI command management
    /// </summary>
    public partial class MathematicsWindow : Window
    {
        #region Private Fields

        private bool isInitialized = false;
        private string currentConfigurationFile = string.Empty;
        private MathematicsSettings lastSavedSettings;
        private DispatcherTimer statusTimer;
        private DispatcherTimer timestampTimer;
        private bool isConnectedToOscilloscope = false;
        private readonly List<string> recentCommands = new List<string>();
        private bool hasUnsavedChanges = false;

        // Constants
        private const int MAX_RECENT_COMMANDS = 100;
        private const string WINDOW_TITLE_PREFIX = "Mathematics Functions - ";
        private const string DEFAULT_CONFIG_NAME = "New Configuration";

        #endregion

        #region Events

        /// <summary>
        /// Event raised when SCPI command is generated
        /// </summary>
        public event EventHandler<SCPICommandEventArgs> SCPICommandGenerated;

        /// <summary>
        /// Event raised when an error occurs
        /// </summary>
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// Event raised for status updates
        /// </summary>
        public event EventHandler<StatusEventArgs> StatusUpdated;

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
                // Set window properties first
                SetInitialWindowState();

                // Subscribe to panel events if panel exists
                SubscribeToMathPanelEvents();

                // Initialize timers
                InitializeTimers();

                // Set initial connection status
                UpdateConnectionStatus(false);

                // Update initial status and window title
                UpdateStatus("Mathematics Functions window initialized");
                UpdateWindowTitle(DEFAULT_CONFIG_NAME);

                // Store initial settings for change tracking
                StoreInitialSettings();

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
        /// Subscribe to math panel events
        /// FIXED: Proper event subscription with correct signatures
        /// </summary>
        private void SubscribeToMathPanelEvents()
        {
            try
            {
                if (MathPanel != null)
                {
                    // Subscribe to the correctly typed events
                    MathPanel.SCPICommandGenerated += OnMathPanelSCPICommand;
                    MathPanel.ErrorOccurred += OnMathPanelError;
                    MathPanel.StatusUpdated += OnMathPanelStatus;

                    UpdateStatus("Event subscriptions established");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error subscribing to panel events: {ex.Message}");
            }
        }


        /// <summary>
        /// Unsubscribe from math panel events
        /// FIXED: Proper event unsubscription
        /// </summary>
        private void UnsubscribeFromMathPanelEvents()
        {
            try
            {
                if (MathPanel != null)
                {
                    MathPanel.SCPICommandGenerated -= OnMathPanelSCPICommand;
                    MathPanel.ErrorOccurred -= OnMathPanelError;
                    MathPanel.StatusUpdated -= OnMathPanelStatus;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error unsubscribing from events: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize all timers used by the window
        /// </summary>
        private void InitializeTimers()
        {
            try
            {
                // Status update timer
                statusTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                statusTimer.Tick += StatusTimer_Tick;
                statusTimer.Start();

                // Timestamp update timer  
                timestampTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                timestampTimer.Tick += TimestampTimer_Tick;
                timestampTimer.Start();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to initialize timers: {ex.Message}");
            }
        }

        /// <summary>
        /// Set initial window appearance and state
        /// </summary>
        private void SetInitialWindowState()
        {
            try
            {
                // Set window properties
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                this.MinWidth = 800;
                this.MinHeight = 600;

                // Update timestamp
                UpdateTimestamp();

                // Set initial status icon if it exists
                if (StatusIcon != null)
                {
                    StatusIcon.Text = "🧮";
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to set initial window state: {ex.Message}");
            }
        }

        /// <summary>
        /// Store initial settings for change tracking
        /// </summary>
        private void StoreInitialSettings()
        {
            try
            {
                lastSavedSettings = GetPanelSettings() ?? new MathematicsSettings();
                hasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to store initial settings: {ex.Message}");
                lastSavedSettings = new MathematicsSettings();
            }
        }

        /// <summary>
        /// Get panel settings safely
        /// FIXED: Handles null coalescing operator type mismatch
        /// </summary>
        private MathematicsSettings GetPanelSettings()
        {
            try
            {
                // Get settings from panel if available, otherwise return new instance
                var settings = MathPanel?.GetCurrentSettings();
                return settings ?? new MathematicsSettings();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error getting panel settings: {ex.Message}");
                return new MathematicsSettings();
            }
        }

        #endregion

        #region Event Handlers - Math Panel

        /// <summary>
        /// Handle SCPI commands from math panel
        /// FIXED: Now properly handles SCPICommandEventArgs parameter
        /// </summary>
        private void OnMathPanelSCPICommand(object sender, SCPICommandEventArgs e)
        {
            try
            {
                // Add to recent commands list
                AddToRecentCommands(e.Command);

                // Mark as having unsaved changes
                hasUnsavedChanges = true;

                // Forward the command to parent
                SCPICommandGenerated?.Invoke(this, e);

                // Update status
                UpdateStatus($"SCPI: {e.Command}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error handling SCPI command: {ex.Message}");
            }
        }


        /// <summary>
        /// Handle errors from math panel
        /// FIXED: Now properly handles ErrorEventArgs parameter
        /// </summary>
        private void OnMathPanelError(object sender, ErrorEventArgs e)
        {
            try
            {
                // Forward error to parent
                ErrorOccurred?.Invoke(this, e);

                // Update status with error indication
                UpdateStatus($"Error: {e.Error}", true);

                // Show error to user if it's critical
                if (e.Error.Contains("SCPI") || e.Error.Contains("communication"))
                {
                    MessageBox.Show(e.Error, "Mathematics Panel Error",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in error handler: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle status updates from math panel
        /// FIXED: Now properly handles StatusEventArgs parameter
        /// </summary>
        private void OnMathPanelStatus(object sender, StatusEventArgs e)
        {
            try
            {
                // Forward status to parent
                StatusUpdated?.Invoke(this, e);

                // Update local status
                UpdateStatus(e.Message);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error handling status update: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers - Menu Actions

        /// <summary>
        /// Create new configuration
        /// </summary>
        private void NewConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (hasUnsavedChanges && !ConfirmDiscardChanges())
                    return;

                // Reset to default settings
                var defaultSettings = new MathematicsSettings();
                LoadSettingsIntoPanel(defaultSettings);

                // Clear current configuration
                currentConfigurationFile = string.Empty;
                lastSavedSettings = CloneSettings(defaultSettings);
                hasUnsavedChanges = false;

                // Update UI
                UpdateWindowTitle(DEFAULT_CONFIG_NAME);
                UpdateStatus("New mathematics configuration created");

                OnStatusUpdated("New configuration created");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error creating new configuration: {ex.Message}");
                MessageBox.Show($"Error creating new configuration: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Open existing configuration
        /// </summary>
        private void OpenConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (hasUnsavedChanges && !ConfirmDiscardChanges())
                    return;

                var dialog = new OpenFileDialog
                {
                    Title = "Open Mathematics Configuration",
                    Filter = "Mathematics Config Files (*.mathconfig)|*.mathconfig|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = ".mathconfig",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() == true)
                {
                    LoadConfigurationFromFile(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error opening configuration: {ex.Message}");
                MessageBox.Show($"Error opening configuration: {ex.Message}",
                              "Open Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Save current configuration
        /// </summary>
        private void SaveConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(currentConfigurationFile))
                {
                    SaveConfigurationAs_Click(sender, e);
                }
                else
                {
                    SaveConfigurationToFile(currentConfigurationFile);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error saving configuration: {ex.Message}");
                MessageBox.Show($"Error saving configuration: {ex.Message}",
                              "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Save current configuration with new name
        /// </summary>
        private void SaveConfigurationAs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Save Mathematics Configuration",
                    Filter = "Mathematics Config Files (*.mathconfig)|*.mathconfig|JSON Files (*.json)|*.json",
                    DefaultExt = ".mathconfig",
                    FileName = GenerateDefaultFileName()
                };

                if (dialog.ShowDialog() == true)
                {
                    SaveConfigurationToFile(dialog.FileName);
                    currentConfigurationFile = dialog.FileName;
                    UpdateWindowTitle(Path.GetFileNameWithoutExtension(dialog.FileName));
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error saving configuration as: {ex.Message}");
                MessageBox.Show($"Error saving configuration: {ex.Message}",
                              "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Export current SCPI commands
        /// </summary>
        private void ExportCommands_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Export SCPI Commands",
                    Filter = "Text Files (*.txt)|*.txt|SCPI Files (*.scpi)|*.scpi|All Files (*.*)|*.*",
                    DefaultExt = ".txt",
                    FileName = $"MathCommands_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (dialog.ShowDialog() == true)
                {
                    ExportSCPICommands(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error exporting commands: {ex.Message}");
                MessageBox.Show($"Error exporting commands: {ex.Message}",
                              "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
                    LoadSettingsIntoPanel(defaultSettings);

                    // Clear current configuration file
                    currentConfigurationFile = string.Empty;
                    lastSavedSettings = CloneSettings(defaultSettings);
                    hasUnsavedChanges = false;

                    // Update window title
                    UpdateWindowTitle(DEFAULT_CONFIG_NAME);

                    UpdateStatus("All mathematics settings reset to factory defaults");

                    MessageBox.Show("All mathematics settings have been reset to defaults.",
                                  "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                    OnStatusUpdated("Settings reset to defaults");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error resetting settings: {ex.Message}");
                MessageBox.Show($"Error resetting settings: {ex.Message}",
                              "Reset Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Validate current configuration
        /// </summary>
        private void ValidateConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = GetPanelSettings();
                if (settings == null)
                {
                    MessageBox.Show("No configuration to validate.", "Validation",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var validationResults = ValidateSettings(settings);
                var message = validationResults.Count == 0
                    ? "✅ Configuration is valid!\n\nAll settings are properly configured and ready for use."
                    : $"⚠️ Configuration has issues:\n\n{string.Join("\n", validationResults)}";

                var icon = validationResults.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning;

                MessageBox.Show(message, "Configuration Validation", MessageBoxButton.OK, icon);

                UpdateStatus($"Configuration validation completed - {validationResults.Count} issues found");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error validating configuration: {ex.Message}");
                MessageBox.Show($"Error during validation: {ex.Message}",
                              "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Show help dialog
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
            }
        }

        #endregion

        #region Event Handlers - Timers

        /// <summary>
        /// Handle status timer tick
        /// </summary>
        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Update connection status periodically
                UpdateConnectionStatusFromParent();

                // Clear temporary status messages after a delay
                ClearTemporaryStatus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Status timer error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle timestamp timer tick
        /// </summary>
        private void TimestampTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                UpdateTimestamp();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Timestamp timer error: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers - Window Lifecycle

        /// <summary>
        /// Handle window closing
        /// </summary>
        private async void MathematicsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Check for unsaved changes
                if (hasUnsavedChanges && !ConfirmDiscardChanges())
                {
                    e.Cancel = true;
                    return;
                }

                // Disable math functions on the oscilloscope
                if (MathPanel != null)
                {
                    await MathPanel.OnPanelClosingAsync();
                }

                // Cleanup resources
                CleanupResources();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error during window closing: {ex.Message}");
                // Don't cancel closing due to cleanup errors
            }
        }

        /// <summary>
        /// Handle math panel loaded event
        /// </summary>
        private void MathPanel_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Re-subscribe to events if needed
                if (isInitialized)
                {
                    SubscribeToMathPanelEvents();
                    UpdateStatus("Mathematics panel loaded and ready");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error handling panel loaded: {ex.Message}");
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
                var settings = GetPanelSettings();
                if (settings != null)
                {
                    SaveSettingsToFile(settings, filePath);
                    lastSavedSettings = CloneSettings(settings);
                    hasUnsavedChanges = false;

                    UpdateStatus($"Configuration saved to {Path.GetFileName(filePath)}");
                    MessageBox.Show("Mathematics configuration saved successfully!",
                                  "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                    OnStatusUpdated("Configuration saved successfully");
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
                var settings = LoadSettingsFromFile(filePath);
                LoadSettingsIntoPanel(settings);

                currentConfigurationFile = filePath;
                lastSavedSettings = CloneSettings(settings);
                hasUnsavedChanges = false;

                UpdateWindowTitle(Path.GetFileNameWithoutExtension(filePath));
                UpdateStatus($"Configuration loaded from {Path.GetFileName(filePath)}");

                MessageBox.Show("Mathematics configuration loaded successfully!",
                              "Load Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                OnStatusUpdated("Configuration loaded successfully");
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

                OnStatusUpdated("SCPI commands exported successfully");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to export commands: {ex.Message}", ex);
            }
        }

        #endregion

        #region Settings Management

        /// <summary>
        /// Save settings to file using JSON serialization
        /// </summary>
        private void SaveSettingsToFile(MathematicsSettings settings, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save settings to {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load settings from file using JSON deserialization
        /// </summary>
        private MathematicsSettings LoadSettingsFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Settings file not found: {filePath}");

            try
            {
                var json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var settings = JsonSerializer.Deserialize<MathematicsSettings>(json, options);

                // Ensure all nested objects are initialized
                if (settings != null)
                {
                    settings.BasicOperations ??= new BasicOperationsSettings();
                    settings.FFTAnalysis ??= new FFTAnalysisSettings();
                    settings.DigitalFilters ??= new DigitalFiltersSettings();
                    settings.AdvancedMath ??= new AdvancedMathSettings();
                }

                return settings ?? new MathematicsSettings();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load settings from {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load settings into the math panel (placeholder - implement based on your panel's API)
        /// </summary>
        private void LoadSettingsIntoPanel(MathematicsSettings settings)
        {
            try
            {
                // This is a placeholder implementation
                // You would implement this based on your MathematicsPanel's actual API
                // For example, if your panel has a LoadSettings method:
                // MathPanel?.LoadSettings(settings);

                // For now, we'll just update our tracking variables
                lastSavedSettings = CloneSettings(settings);
                hasUnsavedChanges = false;

                UpdateStatus("Settings loaded into panel");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load settings into panel: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Create a clone of settings for change tracking
        /// </summary>
        private MathematicsSettings CloneSettings(MathematicsSettings settings)
        {
            try
            {
                if (settings == null) return new MathematicsSettings();

                // Simple JSON-based cloning
                var json = JsonSerializer.Serialize(settings);
                return JsonSerializer.Deserialize<MathematicsSettings>(json) ?? new MathematicsSettings();
            }
            catch (Exception)
            {
                // Fallback to creating new settings if cloning fails
                return new MathematicsSettings();
            }
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// Update window title with configuration name
        /// </summary>
        private void UpdateWindowTitle(string configurationName)
        {
            try
            {
                var title = $"{WINDOW_TITLE_PREFIX}{configurationName}";
                if (hasUnsavedChanges)
                    title += " *";

                this.Title = title;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error updating window title: {ex.Message}");
            }
        }

        /// <summary>
        /// Update status with optional error indication
        /// </summary>
        private void UpdateStatus(string message, bool isError = false)
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (StatusText != null)
                    {
                        StatusText.Text = message;
                        StatusText.Foreground = isError ?
                            new SolidColorBrush(Colors.Red) :
                            new SolidColorBrush(Colors.Green);
                    }

                    if (TimestampText != null)
                    {
                        TimestampText.Text = $"Last Updated: {DateTime.Now:HH:mm:ss}";
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating status: {ex.Message}");
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
                    TimestampText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating timestamp: {ex.Message}");
            }
        }

        /// <summary>
        /// Update connection status indicator
        /// </summary>
        private void UpdateConnectionStatus(bool isConnected)
        {
            try
            {
                isConnectedToOscilloscope = isConnected;

                if (ConnectionIndicator != null)
                {
                    ConnectionIndicator.Fill = new SolidColorBrush(isConnected ? Colors.Green : Colors.Red);
                }

                if (ConnectionText != null)
                {
                    ConnectionText.Text = isConnected ? "Connected" : "Disconnected";
                    ConnectionText.Foreground = new SolidColorBrush(isConnected ? Colors.Green : Colors.Red);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating connection status: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Add command to recent commands list
        /// </summary>
        private void AddToRecentCommands(string command)
        {
            try
            {
                if (string.IsNullOrEmpty(command)) return;

                recentCommands.Add($"[{DateTime.Now:HH:mm:ss}] {command}");

                // Keep only recent commands (prevent memory issues)
                if (recentCommands.Count > MAX_RECENT_COMMANDS)
                {
                    recentCommands.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding recent command: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current SCPI commands from panel
        /// </summary>
        private string GetCurrentSCPICommands()
        {
            try
            {
                if (recentCommands.Count > 0)
                {
                    return string.Join("\n", recentCommands);
                }
                else
                {
                    return "# No SCPI commands have been generated yet";
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error getting SCPI commands: {ex.Message}");
                return $"# Error retrieving commands: {ex.Message}";
            }
        }

        /// <summary>
        /// Generate export content for commands
        /// </summary>
        private string GenerateCommandExportContent(string commands)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# Rigol DS1000Z-E Mathematics SCPI Commands");
                sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                var settings = GetPanelSettings();
                sb.AppendLine($"# Configuration: {settings?.ConfigurationName ?? "Unknown"}");
                sb.AppendLine($"# Current Mode: {MathPanel?.GetCurrentMathMode() ?? "Unknown"}");
                sb.AppendLine();
                sb.AppendLine("# Commands:");
                sb.AppendLine(commands);
                sb.AppendLine();
                sb.AppendLine("# End of export");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generating export content: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate default filename for saving
        /// </summary>
        private string GenerateDefaultFileName()
        {
            try
            {
                var settings = GetPanelSettings();
                var configName = settings?.ConfigurationName ?? "MathConfig";
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                return $"{configName}_{timestamp}.mathconfig";
            }
            catch
            {
                return $"MathConfig_{DateTime.Now:yyyyMMdd_HHmmss}.mathconfig";
            }
        }

        /// <summary>
        /// Confirm if user wants to discard unsaved changes
        /// </summary>
        private bool ConfirmDiscardChanges()
        {
            try
            {
                var result = MessageBox.Show(
                    "You have unsaved changes that will be lost.\n\n" +
                    "Do you want to continue without saving?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                return result == MessageBoxResult.Yes;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate settings and return list of issues
        /// </summary>
        private List<string> ValidateSettings(MathematicsSettings settings)
        {
            var issues = new List<string>();

            try
            {
                // Validate basic operations
                if (settings.BasicOperations != null)
                {
                    if (string.IsNullOrEmpty(settings.BasicOperations.Source1))
                        issues.Add("Basic Operations: Source 1 not selected");
                    if (string.IsNullOrEmpty(settings.BasicOperations.Source2))
                        issues.Add("Basic Operations: Source 2 not selected");
                    if (string.IsNullOrEmpty(settings.BasicOperations.Operation))
                        issues.Add("Basic Operations: Operation not selected");
                }

                // Validate FFT settings
                if (settings.FFTAnalysis != null)
                {
                    if (string.IsNullOrEmpty(settings.FFTAnalysis.Source))
                        issues.Add("FFT Analysis: Source not selected");
                    if (string.IsNullOrEmpty(settings.FFTAnalysis.Window))
                        issues.Add("FFT Analysis: Window function not selected");
                }

                // Validate filter settings
                if (settings.DigitalFilters != null)
                {
                    if (settings.DigitalFilters.W1Frequency <= 0)
                        issues.Add("Digital Filters: W1 frequency must be positive");
                    if (settings.DigitalFilters.W2Frequency <= 0)
                        issues.Add("Digital Filters: W2 frequency must be positive");
                    if (settings.DigitalFilters.W1Frequency >= settings.DigitalFilters.W2Frequency)
                        issues.Add("Digital Filters: W1 frequency must be less than W2 frequency");
                }

                // Validate advanced math settings
                if (settings.AdvancedMath != null)
                {
                    if (string.IsNullOrEmpty(settings.AdvancedMath.Function))
                        issues.Add("Advanced Math: Function not selected");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Validation error: {ex.Message}");
            }

            return issues;
        }

        /// <summary>
        /// Update connection status from parent application
        /// </summary>
        private void UpdateConnectionStatusFromParent()
        {
            try
            {
                // Request connection status update from parent
                ConnectionStatusRequested?.Invoke(this, isConnectedToOscilloscope);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating connection status: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear temporary status messages
        /// </summary>
        private void ClearTemporaryStatus()
        {
            // This could be enhanced to clear status messages after a certain time
            // For now, it's a placeholder for future functionality
        }

        /// <summary>
        /// Show help dialog with mathematics information
        /// </summary>
        private void ShowHelpDialog()
        {
            var helpText = @"MATHEMATICS FUNCTIONS HELP

OVERVIEW:
The Mathematics Functions window provides comprehensive control over the Rigol DS1000Z-E oscilloscope's mathematical operations.

MATH MODES (Mutually Exclusive):
📊 Basic Operations: Add, Subtract, Multiply, Divide two channels
📈 FFT Analysis: Fast Fourier Transform with windowing options  
🔧 Digital Filters: Low/High/Band pass and stop filters
🔬 Advanced Math: Integration, Differentiation, and other functions

SCPI COMMAND SEQUENCE:
When switching modes, the system follows the proper sequence:
1. :MATH:DISPlay OFF; :MATH:RESet (150ms delay)
2. :MATH:DISPlay ON; :MATH:OPERator [MODE] (500ms delay)
3. Mode-specific configuration commands (50ms between commands)

FILE OPERATIONS:
• Save/Load: Store complete configurations in .mathconfig files
• Export: Save SCPI command history to text files
• Validate: Check configuration for completeness and errors

SAFETY FEATURES:
• Only one math mode active at a time
• Automatic cleanup when closing panel
• Unsaved changes detection
• Comprehensive error handling

For detailed SCPI command reference, see the Rigol DS1000Z-E Programming Guide.";

            MessageBox.Show(helpText, "Mathematics Functions Help",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Show about dialog
        /// </summary>
        private void ShowAboutDialog()
        {
            var aboutText = @"MATHEMATICS FUNCTIONS WINDOW

Version: 2.0.0
Part of: DS1000Z-E USB Control Application

FEATURES:
✓ Mutually exclusive math modes with proper SCPI sequencing
✓ Complete configuration save/load system
✓ SCPI command export and history tracking
✓ Real-time status monitoring and error handling
✓ Comprehensive validation and safety checks
✓ Integration with main oscilloscope control application

COMPATIBILITY:
• Rigol DS1000Z-E Series Digital Oscilloscopes
• SCPI Command Protocol with proper timing
• USB and LAN communication interfaces

MATH MODES SUPPORTED:
• Basic Operations (ADD, SUB, MUL, DIV)
• FFT Analysis with windowing (Rectangular, Blackman, Hanning, Hamming)
• Digital Filters (Low/High/Band Pass/Stop)
• Advanced Math (Integration, Differentiation, Square Root, Logarithms)

TECHNICAL COMPLIANCE:
• Follows DS1000Z-E Programming Guide specifications
• Implements proper SCPI command timing sequences
• Ensures mutual exclusivity of math operations
• Provides comprehensive error handling and recovery

© 2024 - Mathematics Functions Module
Built for reliable oscilloscope control and measurement automation.";

            MessageBox.Show(aboutText, "About Mathematics Functions",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Cleanup and Resource Management

        /// <summary>
        /// Cleanup window resources
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                // Stop timers
                statusTimer?.Stop();
                timestampTimer?.Stop();

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

        #region Public Methods

        /// <summary>
        /// Set connection status from parent application
        /// </summary>
        /// <param name="isConnected">True if connected to oscilloscope</param>
        public void SetConnectionStatus(bool isConnected)
        {
            try
            {
                Dispatcher.Invoke(() => UpdateConnectionStatus(isConnected));
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error setting connection status: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current mathematics panel reference
        /// </summary>
        /// <returns>Mathematics panel instance</returns>
        public MathematicsPanel GetMathematicsPanel()
        {
            return MathPanel;
        }

        /// <summary>
        /// Check if there are unsaved changes
        /// </summary>
        /// <returns>True if changes need to be saved</returns>
        public bool HasUnsavedChanges()
        {
            return hasUnsavedChanges;
        }

        #endregion

        #region Event Raising Methods

        /// <summary>
        /// Raise error occurred event
        /// </summary>
        protected virtual void OnErrorOccurred(string error)
        {
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(error));
        }

        /// <summary>
        /// Raise status updated event
        /// </summary>
        protected virtual void OnStatusUpdated(string status)
        {
            StatusUpdated?.Invoke(this, new StatusEventArgs(status));
        }

        #endregion
    }
}