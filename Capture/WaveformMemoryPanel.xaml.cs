using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace OscilloscopeControl.Capture
{
    /// <summary>
    /// Interaction logic for WaveformMemoryPanel.xaml
    /// Code-behind for the waveform capture and memory management user interface.
    /// </summary>
    public partial class WaveformMemoryPanel : UserControl
    {
        #region Constructor

        /// <summary>
        /// Initialize the waveform memory panel user control
        /// </summary>
        public WaveformMemoryPanel()
        {
            InitializeComponent();
            this.Loaded += WaveformMemoryPanel_Loaded;
        }

        #endregion

        #region UI Properties (Must match what WaveformMemoryController expects)

        /// <summary>Channel selection combo box</summary>
        public ComboBox ChannelSelectionComboBox { get; private set; }

        /// <summary>Waveform mode combo box</summary>
        public ComboBox WaveformModeComboBox { get; private set; }

        /// <summary>Waveform format combo box</summary>
        public ComboBox WaveformFormatComboBox { get; private set; }

        /// <summary>Capture waveform button</summary>
        public Button CaptureWaveformButton { get; private set; }

        /// <summary>Clear memory button</summary>
        public Button ClearMemoryButton { get; private set; }

        /// <summary>Export selected button</summary>
        public Button ExportSelectedButton { get; private set; }

        /// <summary>Stored waveforms list box</summary>
        public ListBox StoredWaveformsListBox { get; private set; }

        /// <summary>Memory status text block</summary>
        public TextBlock MemoryStatusTextBlock { get; private set; }

        /// <summary>Waveform details text block</summary>
        public TextBlock WaveformDetailsTextBlock { get; private set; }

        /// <summary>Capture progress bar</summary>
        public ProgressBar CaptureProgressBar { get; private set; }

        /// <summary>Memory limit slider</summary>
        public Slider MemoryLimitSlider { get; private set; }

        /// <summary>Memory limit text block</summary>
        public TextBlock MemoryLimitTextBlock { get; private set; }

        /// <summary>Auto capture checkbox</summary>
        public CheckBox AutoCaptureCheckBox { get; private set; }

        /// <summary>Filter channel combo box</summary>
        public ComboBox FilterChannelComboBox { get; private set; }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when the user control is fully loaded
        /// </summary>
        private void WaveformMemoryPanel_Loaded(object sender, RoutedEventArgs e)
        {
            // Find and assign UI controls by name (these must exist in your XAML)
            try
            {
                ChannelSelectionComboBox = FindName("ChannelSelectionComboBox") as ComboBox;
                WaveformModeComboBox = FindName("WaveformModeComboBox") as ComboBox;
                WaveformFormatComboBox = FindName("WaveformFormatComboBox") as ComboBox;
                CaptureWaveformButton = FindName("CaptureWaveformButton") as Button;
                ClearMemoryButton = FindName("ClearMemoryButton") as Button;
                ExportSelectedButton = FindName("ExportSelectedButton") as Button;
                StoredWaveformsListBox = FindName("StoredWaveformsListBox") as ListBox;
                MemoryStatusTextBlock = FindName("MemoryStatusTextBlock") as TextBlock;
                WaveformDetailsTextBlock = FindName("WaveformDetailsTextBlock") as TextBlock;
                CaptureProgressBar = FindName("CaptureProgressBar") as ProgressBar;
                MemoryLimitSlider = FindName("MemoryLimitSlider") as Slider;
                MemoryLimitTextBlock = FindName("MemoryLimitTextBlock") as TextBlock;
                AutoCaptureCheckBox = FindName("AutoCaptureCheckBox") as CheckBox;
                FilterChannelComboBox = FindName("FilterChannelComboBox") as ComboBox;

                // Initialize default values
                InitializeDefaults();
            }
            catch (Exception ex)
            {
                // Log error if needed
                MessageBox.Show($"Error initializing UI controls: {ex.Message}", "UI Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize default values for UI controls
        /// </summary>
        private void InitializeDefaults()
        {
            try
            {
                // Initialize channel selection
                if (ChannelSelectionComboBox != null)
                {
                    ChannelSelectionComboBox.Items.Clear();
                    ChannelSelectionComboBox.Items.Add("Channel 1");
                    ChannelSelectionComboBox.Items.Add("Channel 2");
                    ChannelSelectionComboBox.Items.Add("Both Channels");
                    ChannelSelectionComboBox.SelectedIndex = 0;
                }

                // Initialize waveform mode
                if (WaveformModeComboBox != null)
                {
                    WaveformModeComboBox.Items.Clear();
                    WaveformModeComboBox.Items.Add("Normal");
                    WaveformModeComboBox.Items.Add("Raw");
                    WaveformModeComboBox.Items.Add("Maximum");
                    WaveformModeComboBox.SelectedIndex = 0;
                }

                // Initialize waveform format
                if (WaveformFormatComboBox != null)
                {
                    WaveformFormatComboBox.Items.Clear();
                    WaveformFormatComboBox.Items.Add("Byte");
                    WaveformFormatComboBox.Items.Add("Word");
                    WaveformFormatComboBox.Items.Add("ASCII");
                    WaveformFormatComboBox.SelectedIndex = 0;
                }

                // Initialize filter channel
                if (FilterChannelComboBox != null)
                {
                    FilterChannelComboBox.Items.Clear();
                    FilterChannelComboBox.Items.Add("All Channels");
                    FilterChannelComboBox.Items.Add("Channel 1 Only");
                    FilterChannelComboBox.Items.Add("Channel 2 Only");
                    FilterChannelComboBox.SelectedIndex = 0;
                }

                // Initialize memory limit slider
                if (MemoryLimitSlider != null)
                {
                    MemoryLimitSlider.Minimum = 10;
                    MemoryLimitSlider.Maximum = 1000;
                    MemoryLimitSlider.Value = 100;
                }

                // Initialize status text
                if (MemoryStatusTextBlock != null)
                {
                    MemoryStatusTextBlock.Text = "Ready";
                }

                if (MemoryLimitTextBlock != null)
                {
                    MemoryLimitTextBlock.Text = "100";
                }
            }
            catch (Exception ex)
            {
                // Ignore initialization errors for missing controls
            }
        }

        #endregion

        #region Public Properties for Compatibility

        /// <summary>
        /// Indicates whether the panel is ready for capture operations
        /// </summary>
        public bool IsReadyForCapture => CaptureWaveformButton?.IsEnabled == true;

        /// <summary>
        /// Gets the number of items currently in the waveform list
        /// </summary>
        public int WaveformCount => StoredWaveformsListBox?.Items.Count ?? 0;

        #endregion
    }
}