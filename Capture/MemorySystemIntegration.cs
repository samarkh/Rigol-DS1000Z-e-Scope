using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace OscilloscopeControl.Capture
{
    /// <summary>
    /// Integration class that connects the waveform capture system with the existing oscilloscope control application.
    /// This class acts as the main entry point for all capture-related functionality.
    /// </summary>
    public class MemorySystemIntegration
    {
        #region Private Fields

        private readonly IOscilloscopeInterface oscilloscope;
        private WaveformMemoryManager memoryManager;
        private WaveformMemoryManager memoryController;
        private bool isInitialized = false;

        #endregion

        #region Events

        /// <summary>
        /// Raised when the memory system needs to log a message
        /// </summary>
        public event EventHandler<string> LogEvent;

        /// <summary>
        /// Raised when a waveform is captured
        /// </summary>
        public event EventHandler<WaveformData> WaveformCaptured;

        /// <summary>
        /// Raised when memory is cleared
        /// </summary>
        public event EventHandler MemoryCleared;

        /// <summary>
        /// Raised when memory status changes (count, size, etc.)
        /// </summary>
        public event EventHandler<MemoryStatusEventArgs> MemoryStatusChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize the memory system integration with the existing oscilloscope interface
        /// </summary>
        /// <param name="oscilloscope">The existing oscilloscope interface from your main application</param>
        public MemorySystemIntegration(IOscilloscopeInterface oscilloscope)
        {
            this.oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));

            Log("🔧 Memory system integration created");
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the memory system components. Call this after your main application is set up.
        /// </summary>
        public void Initialize()
        {
            try
            {
                if (isInitialized)
                {
                    Log("⚠️ Memory system already initialized");
                    return;
                }

                // Create the core memory manager
                memoryManager = new WaveformMemoryManager(oscilloscope);
                memoryManager.LogEvent += (s, e) => Log(e);
                memoryManager.WaveformCaptured += OnWaveformCaptured;
                memoryManager.MemoryCleared += (s, e) => MemoryCleared?.Invoke(this, EventArgs.Empty);

                // Create the UI controller (will be connected to UI later)
                memoryController = new WaveformMemoryManager(memoryManager);
                memoryController.LogEvent += (s, e) => Log(e);

                isInitialized = true;
                Log("✅ Memory system components initialized");
            }
            catch (Exception ex)
            {
                Log($"❌ Error initializing memory system: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Connect the memory system to your existing UI. Call this in your Window_Loaded event.
        /// </summary>
        /// <param name="memoryPanel">The UserControl containing the memory UI (WaveformMemoryPanel)</param>
        public void ConnectToUI(UserControl memoryPanel)
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException("Memory system must be initialized before connecting to UI. Call Initialize() first.");
            }

            try
            {
                Log("🔌 Connecting memory system to UI...");

                // Find and assign UI controls from your XAML
                ConnectUIControls(memoryPanel);

                // Initialize the UI controller with the connected controls
                memoryController.InitializeUI();

                Log("✅ Memory system connected to UI successfully");
            }
            catch (Exception ex)
            {
                Log($"❌ Error connecting to UI: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Find and connect all the UI controls from the loaded XAML
        /// </summary>
        private void ConnectUIControls(UserControl memoryPanel)
        {
            // Connect all the controls defined in WaveformMemoryPanel.xaml
            memoryController.ChannelSelectionComboBox = memoryPanel.FindName("ChannelSelectionComboBox") as ComboBox;
            memoryController.WaveformModeComboBox = memoryPanel.FindName("WaveformModeComboBox") as ComboBox;
            memoryController.WaveformFormatComboBox = memoryPanel.FindName("WaveformFormatComboBox") as ComboBox;
            memoryController.CaptureWaveformButton = memoryPanel.FindName("CaptureWaveformButton") as Button;
            memoryController.ClearMemoryButton = memoryPanel.FindName("ClearMemoryButton") as Button;
            memoryController.ExportSelectedButton = memoryPanel.FindName("ExportSelectedButton") as Button;
            memoryController.StoredWaveformsListBox = memoryPanel.FindName("StoredWaveformsListBox") as ListBox;
            memoryController.MemoryStatusTextBlock = memoryPanel.FindName("MemoryStatusTextBlock") as TextBlock;
            memoryController.WaveformDetailsTextBlock = memoryPanel.FindName("WaveformDetailsTextBlock") as TextBlock;
            memoryController.CaptureProgressBar = memoryPanel.FindName("CaptureProgressBar") as ProgressBar;
            memoryController.MemoryLimitSlider = memoryPanel.FindName("MemoryLimitSlider") as Slider;
            memoryController.MemoryLimitTextBlock = memoryPanel.FindName("MemoryLimitTextBlock") as TextBlock;
            memoryController.AutoCaptureCheckBox = memoryPanel.FindName("AutoCaptureCheckBox") as CheckBox;
            memoryController.FilterChannelComboBox = memoryPanel.FindName("FilterChannelComboBox") as ComboBox;

            // Verify critical controls are found
            if (memoryController.CaptureWaveformButton == null)
                Log("⚠️ Warning: CaptureWaveformButton not found in UI");
            if (memoryController.StoredWaveformsListBox == null)
                Log("⚠️ Warning: StoredWaveformsListBox not found in UI");
        }

        #endregion

        #region Integration with Existing Application

        /// <summary>
        /// Call this when your oscilloscope connection status changes
        /// </summary>
        /// <param name="isConnected">True if oscilloscope is connected and ready</param>
        public void UpdateConnectionStatus(bool isConnected)
        {
            if (!isInitialized) return;

            try
            {
                memoryController?.UpdateConnectionStatus(isConnected);

                if (isConnected)
                {
                    Log("🔗 Oscilloscope connected - capture functionality enabled");
                }
                else
                {
                    Log("🔌 Oscilloscope disconnected - capture functionality disabled");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error updating connection status: {ex.Message}");
            }
        }

        /// <summary>
        /// Add memory-related menu items to your existing main menu
        /// </summary>
        /// <param name="mainMenu">Your application's main Menu control</param>
        public void AddToMainMenu(Menu mainMenu)
        {
            if (!isInitialized) return;

            try
            {
                // Find existing Tools menu or create new Memory menu
                MenuItem targetMenu = FindOrCreateMenu(mainMenu, "Tools", "Memory");

                if (targetMenu.Items.Count > 0)
                    targetMenu.Items.Add(new Separator());

                // Quick Capture menu item
                var quickCaptureItem = new MenuItem
                {
                    Header = "📊 _Quick Capture",
                    ToolTip = "Capture waveform from both channels using current settings",
                    InputGestureText = "Ctrl+Shift+C"
                };
                quickCaptureItem.Click += (s, e) => QuickCapture();
                targetMenu.Items.Add(quickCaptureItem);

                // Clear Memory menu item
                var clearMemoryItem = new MenuItem
                {
                    Header = "🗑️ _Clear Memory",
                    ToolTip = "Clear all stored waveforms",
                    InputGestureText = "Ctrl+Shift+Del"
                };
                clearMemoryItem.Click += (s, e) => ClearMemoryWithConfirmation();
                targetMenu.Items.Add(clearMemoryItem);

                // Export All menu item
                var exportAllItem = new MenuItem
                {
                    Header = "📤 _Export All...",
                    ToolTip = "Export all stored waveforms to files"
                };
                exportAllItem.Click += (s, e) => ExportAllWaveforms();
                targetMenu.Items.Add(exportAllItem);

                // Separator and Info
                targetMenu.Items.Add(new Separator());
                var infoItem = new MenuItem
                {
                    Header = "ℹ️ Memory _Status...",
                    ToolTip = "Show memory usage and statistics"
                };
                infoItem.Click += (s, e) => ShowMemoryStatus();
                targetMenu.Items.Add(infoItem);

                Log("📋 Memory menu items added to main menu");
            }
            catch (Exception ex)
            {
                Log($"❌ Error adding menu items: {ex.Message}");
            }
        }

        /// <summary>
        /// Add keyboard shortcuts to your main window
        /// </summary>
        /// <param name="mainWindow">Your application's main Window</param>
        public void AddKeyboardShortcuts(Window mainWindow)
        {
            if (!isInitialized) return;

            try
            {
                // Quick Capture: Ctrl+Shift+C
                var quickCaptureCommand = new RoutedCommand("QuickCapture", typeof(MemorySystemIntegration));
                var quickCaptureBinding = new CommandBinding(quickCaptureCommand, (s, e) => QuickCapture());
                var quickCaptureKey = new KeyBinding(quickCaptureCommand, Key.C, ModifierKeys.Control | ModifierKeys.Shift);

                mainWindow.CommandBindings.Add(quickCaptureBinding);
                mainWindow.InputBindings.Add(quickCaptureKey);

                // Clear Memory: Ctrl+Shift+Delete
                var clearMemoryCommand = new RoutedCommand("ClearMemory", typeof(MemorySystemIntegration));
                var clearMemoryBinding = new CommandBinding(clearMemoryCommand, (s, e) => ClearMemoryWithConfirmation());
                var clearMemoryKey = new KeyBinding(clearMemoryCommand, Key.Delete, ModifierKeys.Control | ModifierKeys.Shift);

                mainWindow.CommandBindings.Add(clearMemoryBinding);
                mainWindow.InputBindings.Add(clearMemoryKey);

                Log("⌨️ Memory keyboard shortcuts added");
            }
            catch (Exception ex)
            {
                Log($"❌ Error adding keyboard shortcuts: {ex.Message}");
            }
        }

        /// <summary>
        /// Update your application's status bar with memory information
        /// </summary>
        /// <param name="statusBar">Your application's StatusBar control</param>
        public void UpdateStatusBar(StatusBar statusBar)
        {
            if (!isInitialized || memoryManager == null) return;

            try
            {
                // Find or create memory status item
                StatusBarItem memoryStatusItem = null;
                foreach (var item in statusBar.Items)
                {
                    if (item is StatusBarItem sbi && sbi.Name == "MemoryStatusItem")
                    {
                        memoryStatusItem = sbi;
                        break;
                    }
                }

                if (memoryStatusItem == null)
                {
                    memoryStatusItem = new StatusBarItem
                    {
                        Name = "MemoryStatusItem"
                    };
                    statusBar.Items.Add(memoryStatusItem);
                }

                // Update memory status
                var waveforms = memoryManager.GetStoredWaveforms();
                var ch1Count = waveforms.Count(w => w.ChannelName.Contains("1"));
                var ch2Count = waveforms.Count(w => w.ChannelName.Contains("2"));

                memoryStatusItem.Content = $"📊 Memory: {waveforms.Count} ({ch1Count}×CH1, {ch2Count}×CH2)";

                // Raise status changed event
                MemoryStatusChanged?.Invoke(this, new MemoryStatusEventArgs
                {
                    TotalWaveforms = waveforms.Count,
                    Channel1Count = ch1Count,
                    Channel2Count = ch2Count
                });
            }
            catch (Exception ex)
            {
                Log($"❌ Error updating status bar: {ex.Message}");
            }
        }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Programmatically trigger a waveform capture
        /// </summary>
        /// <param name="channel">Which channel(s) to capture</param>
        /// <param name="mode">Capture mode</param>
        /// <param name="format">Data format</param>
        /// <returns>List of captured waveforms</returns>
        public List<WaveformData> CaptureWaveform(WaveformChannel channel = WaveformChannel.Both,
                                                  WaveformMode mode = WaveformMode.NORMal,
                                                  WaveformFormat format = WaveformFormat.BYTE)
        {
            if (!isInitialized || memoryManager == null)
            {
                Log("❌ Memory system not initialized");
                return new List<WaveformData>();
            }

            return memoryManager.CaptureWaveforms(channel, mode, format);
        }

        /// <summary>
        /// Get all stored waveforms
        /// </summary>
        public List<WaveformData> GetStoredWaveforms()
        {
            if (!isInitialized || memoryManager == null)
                return new List<WaveformData>();

            return memoryManager.GetStoredWaveforms();
        }

        /// <summary>
        /// Get stored waveforms for a specific channel
        /// </summary>
        public List<WaveformData> GetStoredWaveforms(string channelName)
        {
            if (!isInitialized || memoryManager == null)
                return new List<WaveformData>();

            return memoryManager.GetStoredWaveforms(channelName);
        }

        /// <summary>
        /// Clear all stored waveforms
        /// </summary>
        public void ClearMemory()
        {
            if (!isInitialized || memoryManager == null) return;

            memoryManager.ClearMemory();
        }

        /// <summary>
        /// Set the memory limit
        /// </summary>
        public void SetMemoryLimit(int limit)
        {
            if (!isInitialized || memoryManager == null) return;

            memoryManager.SetMemoryLimit(limit);
        }

        /// <summary>
        /// Export a waveform to CSV file
        /// </summary>
        public bool ExportWaveformToCsv(WaveformData waveform, string filePath)
        {
            if (!isInitialized || memoryManager == null) return false;

            return memoryManager.ExportToCSV(waveform, filePath);
        }

        #endregion

        #region Command Methods (for menu items and shortcuts)

        /// <summary>
        /// Quick capture using default settings (both channels, normal mode)
        /// </summary>
        private void QuickCapture()
        {
            try
            {
                Log("🎯 Quick capture triggered");
                var captured = CaptureWaveform(WaveformChannel.Both, WaveformMode.NORMal, WaveformFormat.BYTE);

                if (captured.Count > 0)
                {
                    Log($"✅ Quick capture successful: {captured.Count} waveforms captured");
                    memoryController?.UpdateWaveformList();
                    UpdateStatusBar(null); // Will find status bar automatically
                }
                else
                {
                    Log("⚠️ Quick capture: No waveforms captured");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Quick capture error: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear memory with user confirmation
        /// </summary>
        private void ClearMemoryWithConfirmation()
        {
            if (!isInitialized || memoryManager == null) return;

            var waveforms = GetStoredWaveforms();
            if (waveforms.Count == 0)
            {
                MessageBox.Show("Memory is already empty.", "Clear Memory",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to clear all {waveforms.Count} stored waveforms?\n\nThis action cannot be undone.",
                "Clear Memory Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                ClearMemory();
                memoryController?.UpdateWaveformList();
                Log($"🗑️ Memory cleared: {waveforms.Count} waveforms removed");
            }
        }

        /// <summary>
        /// Export all stored waveforms to files
        /// </summary>
        private void ExportAllWaveforms()
        {
            if (!isInitialized || memoryManager == null) return;

            var waveforms = GetStoredWaveforms();
            if (waveforms.Count == 0)
            {
                MessageBox.Show("No waveforms to export.", "Export All",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Use folder browser dialog
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = $"Select folder to export {waveforms.Count} waveforms",
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    int successCount = 0;
                    foreach (var waveform in waveforms)
                    {
                        string fileName = $"Waveform_{waveform.ChannelName.Replace(" ", "")}_{waveform.CaptureTime:yyyyMMdd_HHmmss}_{waveform.Id.ToString()[0..8]}.csv";
                        string filePath = System.IO.Path.Combine(folderDialog.SelectedPath, fileName);

                        if (ExportWaveformToCsv(waveform, filePath))
                            successCount++;
                    }

                    MessageBox.Show(
                        $"Export completed!\n\nSuccessfully exported: {successCount} of {waveforms.Count} waveforms\nLocation: {folderDialog.SelectedPath}",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    Log($"📤 Bulk export completed: {successCount}/{waveforms.Count} waveforms exported");
                }
                catch (Exception ex)
                {
                    Log($"❌ Bulk export error: {ex.Message}");
                    MessageBox.Show($"Error during export: {ex.Message}", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Show memory status dialog
        /// </summary>
        private void ShowMemoryStatus()
        {
            if (!isInitialized || memoryManager == null) return;

            try
            {
                var waveforms = GetStoredWaveforms();
                var totalPoints = waveforms.Sum(w => w.PointCount);
                var totalSize = waveforms.Sum(w => w.EstimatedCsvSize);
                var ch1Count = waveforms.Count(w => w.ChannelName.Contains("1"));
                var ch2Count = waveforms.Count(w => w.ChannelName.Contains("2"));

                var oldestCapture = waveforms.Count > 0 ? waveforms.Min(w => w.CaptureTime) : DateTime.MinValue;
                var newestCapture = waveforms.Count > 0 ? waveforms.Max(w => w.CaptureTime) : DateTime.MinValue;

                string statusMessage = $"""
                    Memory Status Report
                    ==================
                    
                    Stored Waveforms: {waveforms.Count:N0}
                      • Channel 1: {ch1Count:N0}
                      • Channel 2: {ch2Count:N0}
                    
                    Data Points: {totalPoints:N0} total
                    Estimated Size: {totalSize / 1024.0 / 1024.0:F1} MB (if exported to CSV)
                    
                    Capture Time Range:
                      • Oldest: {(oldestCapture != DateTime.MinValue ? oldestCapture.ToString("yyyy-MM-dd HH:mm:ss") : "None")}
                      • Newest: {(newestCapture != DateTime.MinValue ? newestCapture.ToString("yyyy-MM-dd HH:mm:ss") : "None")}
                    
                    Memory Limit: {memoryManager.GetMemoryLimit()} waveforms
                    """;

                MessageBox.Show(statusMessage, "Memory Status", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log($"❌ Error showing memory status: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Find existing menu or create new one
        /// </summary>
        private MenuItem FindOrCreateMenu(Menu mainMenu, params string[] menuNames)
        {
            MenuItem targetMenu = null;

            foreach (string menuName in menuNames)
            {
                foreach (MenuItem item in mainMenu.Items)
                {
                    if (item.Header.ToString().Replace("_", "").Equals(menuName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetMenu = item;
                        break;
                    }
                }

                if (targetMenu != null) break;
            }

            if (targetMenu == null)
            {
                targetMenu = new MenuItem { Header = $"_{menuNames[^1]}" };
                mainMenu.Items.Add(targetMenu);
            }

            return targetMenu;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle waveform captured event
        /// </summary>
        private void OnWaveformCaptured(object sender, WaveformData waveform)
        {
            WaveformCaptured?.Invoke(this, waveform);
            memoryController?.UpdateWaveformList();
        }

        #endregion

        #region Logging

        /// <summary>
        /// Log a message through the event system
        /// </summary>
        private void Log(string message)
        {
            LogEvent?.Invoke(this, $"[Memory] {message}");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Check if the memory system is initialized and ready
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Get the underlying memory manager (for advanced operations)
        /// </summary>
        public WaveformMemoryManager MemoryManager => memoryManager;

        /// <summary>
        /// Get the underlying memory controller (for advanced UI operations)
        /// </summary>
        public WaveformMemoryManager MemoryController => memoryController;

        #endregion
    }

    /// <summary>
    /// Event arguments for memory status changes
    /// </summary>
    public class MemoryStatusEventArgs : EventArgs
    {
        public int TotalWaveforms { get; set; }
        public int Channel1Count { get; set; }
        public int Channel2Count { get; set; }
        public long TotalDataPoints => Channel1Count + Channel2Count;
    }
}