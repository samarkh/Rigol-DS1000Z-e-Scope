using DS1000Z_E_USB_Control.Capture;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace OscilloscopeControl.Capture
{
    /// <summary>
    /// Core manager class that handles waveform capture from the Rigol DS1000Z-E oscilloscope.
    /// Implements SCPI commands for data retrieval, processing, and storage.
    /// UI Controller that manages the waveform capture interface and connects it to the WaveformMemoryManager.
    /// This class handles all UI interactions and provides the properties that MemorySystemIntegration expects.
    /// </summary>
    public class WaveformMemoryController
    {
        #region Private Fields

        private readonly WaveformMemoryManager memoryManager;
        private WaveformMemoryPanel memoryPanel;

        #endregion

        #region Events

        /// <summary>Event raised when a log message is generated</summary>
        public event EventHandler<string> LogEvent;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize the UI controller with a memory manager
        /// </summary>
        /// <param name="memoryManager">The memory manager to control</param>
        public WaveformMemoryController(WaveformMemoryManager memoryManager)
        {
            this.memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));

            // Forward events from memory manager
            this.memoryManager.LogEvent += (s, e) => LogEvent?.Invoke(this, e);
        }

        #endregion

        #region UI Properties (Expected by MemorySystemIntegration)

        /// <summary>Channel selection combo box</summary>
        public ComboBox ChannelSelectionComboBox => memoryPanel?.ChannelSelectionComboBox;

        /// <summary>Waveform mode combo box</summary>
        public ComboBox WaveformModeComboBox => memoryPanel?.WaveformModeComboBox;

        /// <summary>Waveform format combo box</summary>
        public ComboBox WaveformFormatComboBox => memoryPanel?.WaveformFormatComboBox;

        /// <summary>Capture waveform button</summary>
        public Button CaptureWaveformButton => memoryPanel?.CaptureWaveformButton;

        /// <summary>Clear memory button</summary>
        public Button ClearMemoryButton => memoryPanel?.ClearMemoryButton;

        /// <summary>Export selected button</summary>
        public Button ExportSelectedButton => memoryPanel?.ExportSelectedButton;

        /// <summary>Stored waveforms list box</summary>
        public ListBox StoredWaveformsListBox => memoryPanel?.StoredWaveformsListBox;

        /// <summary>Memory status text block</summary>
        public TextBlock MemoryStatusTextBlock => memoryPanel?.MemoryStatusTextBlock;

        /// <summary>Waveform details text block</summary>
        public TextBlock WaveformDetailsTextBlock => memoryPanel?.WaveformDetailsTextBlock;

        /// <summary>Capture progress bar</summary>
        public ProgressBar CaptureProgressBar => memoryPanel?.CaptureProgressBar;

        /// <summary>Memory limit slider</summary>
        public Slider MemoryLimitSlider => memoryPanel?.MemoryLimitSlider;

        /// <summary>Memory limit text block</summary>
        public TextBlock MemoryLimitTextBlock => memoryPanel?.MemoryLimitTextBlock;

        /// <summary>Auto capture checkbox</summary>
        public CheckBox AutoCaptureCheckBox => memoryPanel?.AutoCaptureCheckBox;

        /// <summary>Filter channel combo box</summary>
        public ComboBox FilterChannelComboBox => memoryPanel?.FilterChannelComboBox;

        #endregion

        #region UI Management Methods

        /// <summary>
        /// Initialize UI components and connect them to the memory manager
        /// </summary>
        /// <param name="panel">The WaveformMemoryPanel to control</param>
        public void InitializeUI(WaveformMemoryPanel panel)
        {
            this.memoryPanel = panel ?? throw new ArgumentNullException(nameof(panel));

            // Set the panel's DataContext to this controller
            memoryPanel.DataContext = this;

            Log("✅ UI controller initialized with memory panel");
        }

        /// <summary>
        /// Update connection status across UI components
        /// </summary>
        /// <param name="isConnected">Whether the oscilloscope is connected</param>
        public void UpdateConnectionStatus(bool isConnected)
        {
            try
            {
                if (CaptureWaveformButton != null)
                    CaptureWaveformButton.IsEnabled = isConnected;

                if (ClearMemoryButton != null)
                    ClearMemoryButton.IsEnabled = true; // Always allow clearing memory

                if (ExportSelectedButton != null)
                    ExportSelectedButton.IsEnabled = StoredWaveformsListBox?.SelectedItem != null;

                Log($"🔗 UI connection status updated: {(isConnected ? "Connected" : "Disconnected")}");
            }
            catch (Exception ex)
            {
                Log($"❌ Error updating UI connection status: {ex.Message}");
            }
        }

        /// <summary>
        /// Update the waveform list display
        /// </summary>
        public void UpdateWaveformList()
        {
            try
            {
                if (StoredWaveformsListBox == null) return;

                var waveforms = memoryManager.GetStoredWaveforms();

                // Simple approach: clear and repopulate
                StoredWaveformsListBox.Items.Clear();

                foreach (var waveform in waveforms)
                {
                    StoredWaveformsListBox.Items.Add($"{waveform.ChannelName} - {waveform.CaptureTime:HH:mm:ss} - {waveform.VoltageData?.Count ?? 0} pts");
                }

                // Update status
                if (MemoryStatusTextBlock != null)
                {
                    MemoryStatusTextBlock.Text = $"Stored: {waveforms.Count} waveforms";
                }

                Log($"📊 Updated waveform list: {waveforms.Count} items");
            }
            catch (Exception ex)
            {
                Log($"❌ Error updating waveform list: {ex.Message}");
            }
        }

        #endregion

        #region Public API (Delegate to Memory Manager)

        /// <summary>
        /// Capture waveforms using the underlying memory manager
        /// </summary>
        public List<WaveformData> CaptureWaveforms(WaveformChannel channel, WaveformMode mode, WaveformFormat format)
        {
            return memoryManager.CaptureWaveforms(channel, mode, format);
        }

        /// <summary>
        /// Get all stored waveforms
        /// </summary>
        public List<WaveformData> GetStoredWaveforms()
        {
            return memoryManager.GetStoredWaveforms();
        }

        /// <summary>
        /// Clear all stored waveforms
        /// </summary>
        public void ClearMemory()
        {
            memoryManager.ClearMemory();
            UpdateWaveformList();
        }

        #endregion

        #region Logging

        /// <summary>
        /// Log a message through the event system
        /// </summary>
        private void Log(string message)
        {
            LogEvent?.Invoke(this, $"[UI Controller] {message}");
        }

        #endregion
    }
}