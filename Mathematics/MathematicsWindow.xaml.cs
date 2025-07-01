using Microsoft.Win32;
using Rigol_DS1000Z_E_Control;
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
        private VisaManager visaManager; // VISA manager for oscilloscope communication

        // Constants
        private const int MAX_RECENT_COMMANDS = 100;
        private const string WINDOW_TITLE_PREFIX = "Mathematics Functions - ";
        private const string DEFAULT_CONFIG_NAME = "New Configuration";

        #endregion

        #region MISSING EVENT HANDLERS - Window Events

        /// <summary>
        /// MISSING METHOD: Handle math panel loaded event - Referenced in XAML
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

        /// <summary>
        /// MISSING METHOD: Show about dialog - Referenced in XAML
        /// </summary>
        private void About_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                OnErrorOccurred($"Error showing about dialog: {ex.Message}");
            }
        }

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
        /// MISSING METHOD: Initialize async with VISA manager
        /// </summary>
        public async Task<bool> InitializeAsync(VisaManager visaManager)
        {
            try
            {
                this.visaManager = visaManager;
                isConnectedToOscilloscope = visaManager?.IsConnected ?? false;

                UpdateConnectionStatus(isConnectedToOscilloscope);

                if (isConnectedToOscilloscope)
                {
                    OnStatusUpdated("Mathematics window initialized with oscilloscope connection");

                    // Start status polling if connected
                    await Task.Delay(100); // Small delay to ensure UI is ready
                    StartStatusPolling();
                }
                else
                {
                    OnStatusUpdated("Mathematics window initialized without oscilloscope connection");
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error initializing mathematics window: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Subscribe to mathematics panel events
        /// </summary>
        private void SubscribeToMathPanelEvents()
        {
            try
            {
                if (MathPanel != null)
                {
                    MathPanel.SCPICommandGenerated += OnMathPanelSCPICommand;
                    MathPanel.ErrorOccurred += OnMathPanelError;
                    MathPanel.StatusUpdated += OnMathPanelStatus;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error subscribing to panel events: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize status and timestamp timers
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

        #endregion

        #region MISSING EVENT HANDLERS - Main Menu Actions

        /// <summary>
        /// MISSING METHOD: Save configuration to file
        /// </summary>
        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(currentConfigurationFile))
                {
                    // Save to existing file
                    SaveConfigurationToFile(currentConfigurationFile);
                }
                else
                {
                    // Show save dialog for new file
                    var dialog = new SaveFileDialog
                    {
                        Title = "Save Mathematics Configuration",
                        Filter = "Mathematics Config Files (*.mathconfig)|*.mathconfig|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
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
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error saving configuration: {ex.Message}");
                MessageBox.Show($"Error saving configuration: {ex.Message}",
                              "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// MISSING METHOD: Load configuration from file (FIXED: Now async)
        /// </summary>
        private async void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (hasUnsavedChanges && !ConfirmDiscardChanges())
                    return;

                var dialog = new OpenFileDialog
                {
                    Title = "Load Mathematics Configuration",
                    Filter = "Mathematics Config Files (*.mathconfig)|*.mathconfig|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = ".mathconfig",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() == true)
                {
                    await LoadConfigurationFromFileAsync(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error loading configuration: {ex.Message}");
                MessageBox.Show($"Error loading configuration: {ex.Message}",
                              "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// MISSING METHOD: Show preset configurations
        /// </summary>
        private void ShowPresets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var presetMenu = new ContextMenu();
                var presets = MathematicsSettings.GetFactoryPresets();

                foreach (var preset in presets)
                {
                    var menuItem = new MenuItem
                    {
                        Header = $"📊 {preset.Key}",
                        Tag = preset.Value
                    };
                    menuItem.Click += (s, args) => ApplyPresetAsync(preset.Value, preset.Key);
                    presetMenu.Items.Add(menuItem);
                }

                // Add separator and custom options
                presetMenu.Items.Add(new Separator());

                var customItem = new MenuItem { Header = "📁 Load Custom Preset..." };
                customItem.Click += LoadConfig_Click;
                presetMenu.Items.Add(customItem);

                // Show menu at button
                if (sender is Button button)
                {
                    presetMenu.PlacementTarget = button;
                    presetMenu.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error showing presets: {ex.Message}");
            }
        }

        /// <summary>
        /// MISSING METHOD: Show mathematics templates
        /// </summary>
        private void ShowTemplates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var templatesWindow = new Window
                {
                    Title = "Mathematics Function Templates",
                    Width = 600,
                    Height = 400,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var content = new TextBlock
                {
                    Text = CreateTemplatesContent(),
                    Margin = new Thickness(20),
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12
                };

                var scrollViewer = new ScrollViewer
                {
                    Content = content,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };

                templatesWindow.Content = scrollViewer;
                templatesWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error showing templates: {ex.Message}");
            }
        }

        /// <summary>
        /// MISSING METHOD: Validate current configuration
        /// </summary>
        private void ValidateConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = GetPanelSettings();
                if (settings == null)
                {
                    MessageBox.Show("No configuration available to validate.",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var issues = ValidateSettings(settings);

                if (issues.Count == 0)
                {
                    MessageBox.Show("✅ Configuration is valid!\n\nAll settings are properly configured and ready for use.",
                                  "Validation Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    OnStatusUpdated("Configuration validation passed");
                }
                else
                {
                    var message = "⚠️ Configuration has the following issues:\n\n" +
                                string.Join("\n", issues) +
                                "\n\nPlease review and correct these settings.";

                    MessageBox.Show(message, "Validation Issues",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    OnStatusUpdated($"Configuration validation found {issues.Count} issues");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error validating configuration: {ex.Message}");
                MessageBox.Show($"Error during validation: {ex.Message}",
                              "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// MISSING METHOD: Copy SCPI commands to clipboard
        /// </summary>
        private void CopyCommands_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var commands = GetCurrentSCPICommands();
                if (string.IsNullOrEmpty(commands))
                {
                    MessageBox.Show("No SCPI commands available to copy.",
                                  "Copy Commands", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Clipboard.SetText(commands);
                MessageBox.Show($"SCPI commands copied to clipboard!\n\n{commands.Split('\n').Length} commands copied.",
                              "Copy Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                OnStatusUpdated("SCPI commands copied to clipboard");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error copying commands: {ex.Message}");
                MessageBox.Show($"Error copying commands: {ex.Message}",
                              "Copy Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region MISSING EVENT HANDLERS - Preset Applications

        /// <summary>
        /// MISSING METHOD: Apply Basic Addition preset (FIXED: Now async)
        /// </summary>
        private async void ApplyPreset_BasicAddition(object sender, RoutedEventArgs e)
        {
            try
            {
                var preset = MathematicsSettings.CreateBasicOperationsDefault();
                await ApplyPresetAsync(preset, "Basic Addition");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying Basic Addition preset: {ex.Message}");
            }
        }

        /// <summary>
        /// MISSING METHOD: Apply FFT Analysis preset (FIXED: Now async)
        /// </summary>
        private async void ApplyPreset_FFTAnalysis(object sender, RoutedEventArgs e)
        {
            try
            {
                var preset = MathematicsSettings.CreateFFTAnalysisDefault();
                await ApplyPresetAsync(preset, "FFT Analysis");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying FFT Analysis preset: {ex.Message}");
            }
        }

        /// <summary>
        /// MISSING METHOD: Apply Low Pass Filter preset (FIXED: Now async)
        /// </summary>
        private async void ApplyPreset_LowPassFilter(object sender, RoutedEventArgs e)
        {
            try
            {
                var preset = MathematicsSettings.CreateDigitalFiltersDefault();
                await ApplyPresetAsync(preset, "Low Pass Filter");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying Low Pass Filter preset: {ex.Message}");
            }
        }

        /// <summary>
        /// MISSING METHOD: Apply Integration preset (FIXED: Now async)
        /// </summary>
        private async void ApplyPreset_Integration(object sender, RoutedEventArgs e)
        {
            try
            {
                var preset = MathematicsSettings.CreateAdvancedMathDefault();
                await ApplyPresetAsync(preset, "Integration");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying Integration preset: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Apply a preset configuration (FIXED: Now async)
        /// </summary>
        private async Task ApplyPresetAsync(MathematicsSettings preset, string presetName)
        {
            try
            {
                if (hasUnsavedChanges && !ConfirmDiscardChanges())
                    return;

                await LoadSettingsIntoPanelAsync(preset);

                // Update tracking
                lastSavedSettings = CloneSettings(preset);
                hasUnsavedChanges = false;
                currentConfigurationFile = string.Empty;

                UpdateWindowTitle($"{presetName} Preset");
                OnStatusUpdated($"Applied preset: {presetName}");

                MessageBox.Show($"✅ {presetName} preset applied successfully!",
                              "Preset Applied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error applying preset {presetName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current SCPI commands from the panel
        /// </summary>
        private string GetCurrentSCPICommands()
        {
            try
            {
                var commands = new StringBuilder();
                var settings = GetPanelSettings();

                if (settings != null)
                {
                    // Generate commands based on current settings
                    commands.AppendLine(":MATH:DISPlay ON");
                    commands.AppendLine($":MATH:OPERator {settings.ActiveMode}");

                    switch (settings.ActiveMode?.ToUpper())
                    {
                        case "BASICOPERATIONS":
                            commands.AppendLine($":MATH:SOURce1 {settings.Source1}");
                            commands.AppendLine($":MATH:SOURce2 {settings.Source2}");
                            commands.AppendLine($":MATH:OPERation {settings.Operation}");
                            break;

                        case "FFTANALYSIS":
                            commands.AppendLine($":MATH:FFT:SOURce {settings.FFTSource}");
                            commands.AppendLine($":MATH:FFT:WINDow {settings.FFTWindow}");
                            commands.AppendLine($":MATH:FFT:SPLit {settings.FFTSplit}");
                            commands.AppendLine($":MATH:FFT:UNIT {settings.FFTUnit}");
                            break;

                        case "DIGITALFILTERS":
                            commands.AppendLine($":MATH:FILTer:TYPE {settings.FilterType}");
                            commands.AppendLine($":MATH:FILTer:W1 {settings.FilterW1}");
                            commands.AppendLine($":MATH:FILTer:W2 {settings.FilterW2}");
                            break;

                        case "ADVANCEDMATH":
                            commands.AppendLine($":MATH:ADVanced:FUNCtion {settings.AdvancedFunction}");
                            commands.AppendLine($":MATH:ADVanced:STARt {settings.StartPoint}");
                            commands.AppendLine($":MATH:ADVanced:END {settings.EndPoint}");
                            break;
                    }

                    commands.AppendLine($":MATH:SCALe {settings.Scale}");
                    commands.AppendLine($":MATH:OFFSet {settings.Offset}");
                }

                return commands.ToString();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error generating SCPI commands: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Create templates content for display
        /// </summary>
        private string CreateTemplatesContent()
        {
            return @"MATHEMATICS FUNCTION TEMPLATES

BASIC OPERATIONS:
• Addition: CH1 + CH2 → Combine two signals
• Subtraction: CH1 - CH2 → Find signal differences  
• Multiplication: CH1 × CH2 → Calculate power or modulation
• Division: CH1 ÷ CH2 → Find signal ratios

FFT ANALYSIS:
• Spectrum Analysis: Convert time domain to frequency domain
• Window Functions: Rectangular, Hanning, Blackman, Hamming
• Units: VRMS (voltage), dB (decibels)
• Display: Full spectrum or center frequency

DIGITAL FILTERS:
• Low Pass: Remove high frequency noise
• High Pass: Remove DC and low frequencies
• Band Pass: Extract specific frequency range
• Band Stop: Remove specific frequency range

ADVANCED MATHEMATICS:
• Integration: ∫ signal dt → Area under curve
• Differentiation: d/dt signal → Rate of change
• Square Root: √signal → Root mean square
• Logarithms: log₁₀, ln → Compress dynamic range

SCPI COMMANDS:
:MATH:DISPlay {ON|OFF}
:MATH:OPERator {ADD|SUBtract|MULtiply|DIVide|FFT|FILTer|ADVanced}
:MATH:SOURce1 {CHANnel1|CHANnel2}
:MATH:SOURce2 {CHANnel1|CHANnel2}
:MATH:SCALe <scale>
:MATH:OFFSet <offset>

For detailed programming information, refer to the 
Rigol DS1000Z-E Programming Guide.";
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
                if (settings.ActiveMode == "BasicOperations")
                {
                    if (string.IsNullOrEmpty(settings.Source1))
                        issues.Add("Basic Operations: Source 1 not selected");
                    if (string.IsNullOrEmpty(settings.Source2))
                        issues.Add("Basic Operations: Source 2 not selected");
                    if (string.IsNullOrEmpty(settings.Operation))
                        issues.Add("Basic Operations: Operation not selected");
                }

                // Validate FFT settings
                if (settings.ActiveMode == "FFTAnalysis")
                {
                    if (string.IsNullOrEmpty(settings.FFTSource))
                        issues.Add("FFT Analysis: Source not selected");
                    if (string.IsNullOrEmpty(settings.FFTWindow))
                        issues.Add("FFT Analysis: Window function not selected");
                }

                // Validate filter settings
                if (settings.ActiveMode == "DigitalFilters")
                {
                    if (string.IsNullOrEmpty(settings.FilterType))
                        issues.Add("Digital Filters: Filter type not selected");

                    if (double.TryParse(settings.FilterW1, out double w1) &&
                        double.TryParse(settings.FilterW2, out double w2))
                    {
                        if (w1 <= 0) issues.Add("Digital Filters: W1 frequency must be positive");
                        if (w2 <= 0) issues.Add("Digital Filters: W2 frequency must be positive");
                        if (w1 >= w2) issues.Add("Digital Filters: W1 frequency must be less than W2");
                    }
                    else
                    {
                        issues.Add("Digital Filters: Invalid frequency values");
                    }
                }

                // Validate advanced math settings
                if (settings.ActiveMode == "AdvancedMath")
                {
                    if (string.IsNullOrEmpty(settings.AdvancedFunction))
                        issues.Add("Advanced Math: Function not selected");

                    if (double.TryParse(settings.StartPoint, out double start) &&
                        double.TryParse(settings.EndPoint, out double end))
                    {
                        if (start >= end) issues.Add("Advanced Math: Start point must be less than end point");
                    }
                    else
                    {
                        issues.Add("Advanced Math: Invalid start/end point values");
                    }
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Validation error: {ex.Message}");
            }

            return issues;
        }

        /// <summary>
        /// Start status polling for connected oscilloscope
        /// </summary>
        private void StartStatusPolling()
        {
            try
            {
                if (isConnectedToOscilloscope && visaManager != null)
                {
                    // Start periodic status updates
                    var pollingTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(2)
                    };
                    pollingTimer.Tick += (s, e) => PollOscilloscopeStatus();
                    pollingTimer.Start();
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error starting status polling: {ex.Message}");
            }
        }

        /// <summary>
        /// Poll oscilloscope for status updates
        /// </summary>
        private void PollOscilloscopeStatus()
        {
            try
            {
                if (visaManager?.IsConnected == true)
                {
                    // Poll basic status
                    var mathStatus = visaManager.SendQuery(":MATH:DISPlay?");
                    UpdateConnectionStatus(true);
                    OnStatusUpdated($"Math display: {(mathStatus?.Trim() == "1" ? "ON" : "OFF")}");
                }
                else
                {
                    UpdateConnectionStatus(false);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error polling oscilloscope status: {ex.Message}");
                UpdateConnectionStatus(false);
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
        /// Load configuration from specified file (FIXED: Now async)
        /// </summary>
        private async Task LoadConfigurationFromFileAsync(string filePath)
        {
            try
            {
                var settings = LoadSettingsFromFile(filePath);
                await LoadSettingsIntoPanelAsync(settings);

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
        /// Load settings into the math panel (FIXED: Now uses async method)
        /// </summary>
        private async Task LoadSettingsIntoPanelAsync(MathematicsSettings settings)
        {
            try
            {
                // Load settings into panel if available - FIXED: Using async method
                if (MathPanel != null)
                {
                    await MathPanel.LoadSettingsAsync(settings);
                }

                // Update tracking variables
                lastSavedSettings = CloneSettings(settings);
                hasUnsavedChanges = false;

                UpdateStatus("Settings loaded into panel");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load settings into panel: {ex.Message}", ex);
            }
        }

        #endregion

        #region Status and UI Updates

        /// <summary>
        /// Update window title
        /// </summary>
        private void UpdateWindowTitle(string configName)
        {
            try
            {
                string title = WINDOW_TITLE_PREFIX + configName;
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
        /// Update status display
        /// </summary>
        private void UpdateStatus(string message)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (StatusText != null)
                        StatusText.Text = message;
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error updating status: {ex.Message}");
            }
        }

        /// <summary>
        /// Update timestamp display
        /// </summary>
        private void UpdateTimestamp()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (TimestampText != null)
                        TimestampText.Text = $"Last Updated: {DateTime.Now:HH:mm:ss}";
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error updating timestamp: {ex.Message}");
            }
        }

        /// <summary>
        /// Update connection status display
        /// </summary>
        private void UpdateConnectionStatus(bool isConnected)
        {
            try
            {
                isConnectedToOscilloscope = isConnected;

                Dispatcher.Invoke(() =>
                {
                    if (StatusIcon != null)
                    {
                        StatusIcon.Text = isConnected ? "🟢" : "🔴";
                    }
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error updating connection status: {ex.Message}");
            }
        }

        #endregion

        #region Timer Event Handlers

        /// <summary>
        /// Status timer tick handler
        /// </summary>
        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Update connection status periodically
                UpdateConnectionStatus(visaManager?.IsConnected ?? false);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error in status timer: {ex.Message}");
            }
        }

        /// <summary>
        /// Timestamp timer tick handler
        /// </summary>
        private void TimestampTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                UpdateTimestamp();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error in timestamp timer: {ex.Message}");
            }
        }

        #endregion

        #region Panel Event Handlers

        /// <summary>
        /// Handle SCPI commands from math panel
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
        /// </summary>
        private void OnMathPanelError(object sender, ErrorEventArgs e)
        {
            try
            {
                // Forward error to parent
                ErrorOccurred?.Invoke(this, e);

                // Update status with error indication
                UpdateStatus($"Error: {e.Error}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in error handler: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle status updates from math panel
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

        #region Utility Methods

        /// <summary>
        /// Get panel settings safely
        /// </summary>
        private MathematicsSettings GetPanelSettings()
        {
            try
            {
                return MathPanel?.GetCurrentSettings() ?? new MathematicsSettings();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error getting panel settings: {ex.Message}");
                return new MathematicsSettings();
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
            catch (Exception ex)
            {
                OnErrorOccurred($"Error cloning settings: {ex.Message}");
                return new MathematicsSettings();
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
        /// Add command to recent commands list
        /// </summary>
        private void AddToRecentCommands(string command)
        {
            try
            {
                if (string.IsNullOrEmpty(command)) return;

                recentCommands.Insert(0, command);
                if (recentCommands.Count > MAX_RECENT_COMMANDS)
                {
                    recentCommands.RemoveAt(recentCommands.Count - 1);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error adding to recent commands: {ex.Message}");
            }
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

        #region Window Event Handlers

        /// <summary>
        /// Handle window closing
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (hasUnsavedChanges && !ConfirmDiscardChanges())
                {
                    e.Cancel = true;
                    return;
                }

                CleanupResources();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error during window closing: {ex.Message}");
                // Don't cancel closing due to cleanup errors
            }

            base.OnClosing(e);
        }

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

        #region Additional Missing Methods

        /// <summary>
        /// Reset all settings - called from XAML (FIXED: Now async)
        /// </summary>
        private async void ResetAll_Click(object sender, RoutedEventArgs e)
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
                    await LoadSettingsIntoPanelAsync(defaultSettings);

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
        /// Export commands to file - called from XAML  
        /// </summary>
        private void ExportCommands_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Export SCPI Commands",
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = ".txt",
                    FileName = $"MathCommands_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (dialog.ShowDialog() == true)
                {
                    var commands = GetCurrentSCPICommands();
                    var exportContent = GenerateCommandExportContent(commands);
                    File.WriteAllText(dialog.FileName, exportContent);

                    MessageBox.Show("SCPI commands exported successfully!",
                                  "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    OnStatusUpdated("SCPI commands exported to file");
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
        /// Generate export content with metadata
        /// </summary>
        private string GenerateCommandExportContent(string commands)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# RIGOL DS1000Z-E Mathematics SCPI Commands");
                sb.AppendLine($"# Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"# Configuration: {GetPanelSettings()?.ConfigurationName ?? "Unknown"}");
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
        /// Show help dialog - called from XAML
        /// </summary>
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                OnErrorOccurred($"Error showing help: {ex.Message}");
            }
        }

        #endregion
    }
}