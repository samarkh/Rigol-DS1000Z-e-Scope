using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace OscilloscopeControl.Capture
{
    /// <summary>
    /// Interaction logic for WaveformMemoryPanel.xaml
    /// Code-behind for the waveform capture and memory management user interface.
    /// 
    /// This file contains minimal code-behind logic since most functionality
    /// is handled by the WaveformMemoryManager class.
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

            // The DataContext and control connections will be managed by
            // the MemorySystemIntegration and WaveformMemoryManager classes

            // Set any default properties if needed
            this.Loaded += WaveformMemoryPanel_Loaded;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when the user control is fully loaded
        /// </summary>
        private void WaveformMemoryPanel_Loaded(object sender, RoutedEventArgs e)
        {
            // Any initialization that needs to happen after the control is loaded
            // Most functionality is handled by the WaveformMemoryManager

            // You could add any additional UI setup here if needed
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Indicates whether the panel is ready for capture operations
        /// </summary>
        public bool IsReadyForCapture => CaptureWaveformButton?.IsEnabled == true;

        /// <summary>
        /// Gets the number of items currently in the waveform list
        /// </summary>
        public int WaveformCount => StoredWaveformsListBox?.Items.Count ?? 0;

        #endregion

        #region Public Methods

        /// <summary>
        /// Programmatically set focus to the capture button
        /// </summary>
        public void FocusCaptureButton()
        {
            CaptureWaveformButton?.Focus();
        }

        /// <summary>
        /// Scroll the waveform list to the top
        /// </summary>
        public void ScrollWaveformListToTop()
        {
            if (StoredWaveformsListBox?.Items.Count > 0)
            {
                StoredWaveformsListBox.ScrollIntoView(StoredWaveformsListBox.Items[0]);
            }
        }

        /// <summary>
        /// Clear the current waveform selection
        /// </summary>
        public void ClearWaveformSelection()
        {
            if (StoredWaveformsListBox != null)
            {
                StoredWaveformsListBox.SelectedItem = null;
            }
        }

        #endregion
    }

    #region Value Converters

    /// <summary>
    /// Converter to display duration values with appropriate time units
    /// Converts seconds to a user-friendly display format (ms, μs, etc.)
    /// </summary>
    public class DurationToMillisecondsConverter : IValueConverter
    {
        /// <summary>
        /// Singleton instance for efficient reuse
        /// </summary>
        public static readonly DurationToMillisecondsConverter Instance = new DurationToMillisecondsConverter();

        /// <summary>
        /// Convert duration in seconds to a formatted string with appropriate units
        /// </summary>
        /// <param name="value">Duration in seconds (double)</param>
        /// <param name="targetType">Target type (not used)</param>
        /// <param name="parameter">Parameter (not used)</param>
        /// <param name="culture">Culture info for formatting</param>
        /// <returns>Formatted duration string with units</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double durationInSeconds)
            {
                double durationInMs = durationInSeconds * 1000.0;

                if (durationInMs >= 10000.0)
                {
                    // Show in seconds if >= 10 seconds
                    return $"{durationInSeconds:F1}s";
                }
                else if (durationInMs >= 1000.0)
                {
                    // Show in seconds with more precision if >= 1 second
                    return $"{durationInSeconds:F2}s";
                }
                else if (durationInMs >= 1.0)
                {
                    // Show in milliseconds if >= 1 ms
                    return $"{durationInMs:F1}ms";
                }
                else if (durationInMs >= 0.001)
                {
                    // Show in microseconds if >= 1 μs
                    return $"{durationInMs * 1000.0:F0}μs";
                }
                else
                {
                    // Show in nanoseconds for very small durations
                    return $"{durationInMs * 1000000.0:F0}ns";
                }
            }

            return "0ms";
        }

        /// <summary>
        /// Convert back is not implemented as it's not needed for display-only binding
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack not implemented for DurationToMillisecondsConverter");
        }
    }

    /// <summary>
    /// Converter to format voltage values with appropriate voltage units
    /// Automatically selects V, mV, μV, or kV based on magnitude
    /// </summary>
    public class VoltageFormatter : IValueConverter
    {
        /// <summary>
        /// Singleton instance for efficient reuse
        /// </summary>
        public static readonly VoltageFormatter Instance = new VoltageFormatter();

        /// <summary>
        /// Convert voltage value to formatted string with appropriate units
        /// </summary>
        /// <param name="value">Voltage in volts (double)</param>
        /// <param name="targetType">Target type (not used)</param>
        /// <param name="parameter">Parameter (not used)</param>
        /// <param name="culture">Culture info for formatting</param>
        /// <returns>Formatted voltage string with units</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double voltage)
            {
                double absVoltage = Math.Abs(voltage);

                if (absVoltage >= 1000.0)
                {
                    // Kilovolts for large voltages
                    return $"{voltage / 1000.0:F2}kV";
                }
                else if (absVoltage >= 1.0)
                {
                    // Volts for normal range
                    return $"{voltage:F3}V";
                }
                else if (absVoltage >= 0.001)
                {
                    // Millivolts for small voltages
                    return $"{voltage * 1000.0:F1}mV";
                }
                else if (absVoltage >= 0.000001)
                {
                    // Microvolts for very small voltages
                    return $"{voltage * 1000000.0:F0}μV";
                }
                else
                {
                    // Nanovolts for extremely small voltages
                    return $"{voltage * 1000000000.0:F0}nV";
                }
            }

            return "0V";
        }

        /// <summary>
        /// Convert back is not implemented as it's not needed for display-only binding
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack not implemented for VoltageFormatter");
        }
    }

    /// <summary>
    /// Converter to format large numbers with appropriate abbreviations
    /// Useful for displaying point counts, sample rates, etc.
    /// </summary>
    public class PointCountFormatter : IValueConverter
    {
        /// <summary>
        /// Singleton instance for efficient reuse
        /// </summary>
        public static readonly PointCountFormatter Instance = new PointCountFormatter();

        /// <summary>
        /// Convert large numbers to abbreviated format (K, M, G)
        /// </summary>
        /// <param name="value">Number to format (int, long, or double)</param>
        /// <param name="targetType">Target type (not used)</param>
        /// <param name="parameter">Parameter (not used)</param>
        /// <param name="culture">Culture info for formatting</param>
        /// <returns>Formatted number string with abbreviation</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return FormatNumber(intValue);
            }
            else if (value is long longValue)
            {
                return FormatNumber(longValue);
            }
            else if (value is double doubleValue)
            {
                return FormatNumber((long)doubleValue);
            }

            return "0";
        }

        /// <summary>
        /// Format a number with appropriate abbreviation
        /// </summary>
        private string FormatNumber(long number)
        {
            if (number >= 1000000000)
            {
                return $"{number / 1000000000.0:F1}G";
            }
            else if (number >= 1000000)
            {
                return $"{number / 1000000.0:F1}M";
            }
            else if (number >= 1000)
            {
                return $"{number / 1000.0:F1}K";
            }
            else
            {
                return number.ToString("N0");
            }
        }

        /// <summary>
        /// Convert back is not implemented as it's not needed for display-only binding
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack not implemented for PointCountFormatter");
        }
    }

    /// <summary>
    /// Converter to format file sizes in human-readable format
    /// Converts bytes to KB, MB, GB as appropriate
    /// </summary>
    public class FileSizeFormatter : IValueConverter
    {
        /// <summary>
        /// Singleton instance for efficient reuse
        /// </summary>
        public static readonly FileSizeFormatter Instance = new FileSizeFormatter();

        /// <summary>
        /// Convert file size in bytes to formatted string
        /// </summary>
        /// <param name="value">File size in bytes (long or double)</param>
        /// <param name="targetType">Target type (not used)</param>
        /// <param name="parameter">Parameter (not used)</param>
        /// <param name="culture">Culture info for formatting</param>
        /// <returns>Formatted file size string</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                return FormatFileSize(bytes);
            }
            else if (value is double doubleBytes)
            {
                return FormatFileSize((long)doubleBytes);
            }

            return "0 B";
        }

        /// <summary>
        /// Format file size with appropriate units
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            if (bytes >= 1073741824) // 1 GB
            {
                return $"{bytes / 1073741824.0:F1} GB";
            }
            else if (bytes >= 1048576) // 1 MB
            {
                return $"{bytes / 1048576.0:F1} MB";
            }
            else if (bytes >= 1024) // 1 KB
            {
                return $"{bytes / 1024.0:F1} KB";
            }
            else
            {
                return $"{bytes} B";
            }
        }

        /// <summary>
        /// Convert back is not implemented as it's not needed for display-only binding
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack not implemented for FileSizeFormatter");
        }
    }

    #endregion
}