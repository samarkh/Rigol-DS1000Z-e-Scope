using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Static class containing mathematics calculation commands and utilities for Rigol DS1000Z-E oscilloscope
    /// This class provides frequency calculations based on oscilloscope timebase settings and validation methods
    /// </summary>
    public static class MathematicsCommands
    {
        #region Constants
        /// <summary>
        /// Base multiplier for screen sample rate calculation (100 / timebase)
        /// </summary>
        private const double SCREEN_SAMPLE_RATE_BASE = 100.0;

        /// <summary>
        /// Step size multiplier (0.005 * screen sample rate)
        /// </summary>
        private const double STEP_SIZE_MULTIPLIER = 0.005;

        /// <summary>
        /// Minimum frequency multiplier (0.005 * screen sample rate)
        /// </summary>
        private const double MIN_FREQ_MULTIPLIER = 0.005;

        /// <summary>
        /// Maximum frequency multiplier for standard filters (0.1 * screen sample rate)
        /// </summary>
        private const double MAX_FREQ_MULTIPLIER = 0.1;

        /// <summary>
        /// Maximum frequency multiplier for W1 in band filters (0.095 * screen sample rate)
        /// </summary>
        private const double BAND_FILTER_W1_MAX_MULTIPLIER = 0.095;

        /// <summary>
        /// Minimum frequency multiplier for W2 in band filters (0.01 * screen sample rate)
        /// </summary>
        private const double W2_MIN_FREQ_MULTIPLIER = 0.01;
        #endregion

        #region Frequency Range Calculations
        /// <summary>
        /// Calculate valid frequency range based on timebase and filter type
        /// Formula: Screen Sample Rate = 100 / Horizontal Timebase
        /// </summary>
        /// <param name="timebaseSeconds">Current timebase in seconds</param>
        /// <param name="filterType">Filter type (LPASs, HPASs, BPASs, BSTop)</param>
        /// <returns>Tuple containing (minFreq, maxFreq, stepSize)</returns>
        /// <exception cref="ArgumentException">Thrown when filterType is invalid</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timebaseSeconds is invalid</exception>
        public static (double minFreq, double maxFreq, double stepSize) CalculateFilterFrequencyRange(double timebaseSeconds, string filterType)
        {
            if (timebaseSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(timebaseSeconds), "Timebase must be greater than zero");

            if (string.IsNullOrEmpty(filterType))
                throw new ArgumentException("Filter type cannot be null or empty", nameof(filterType));

            double screenSampleRate = SCREEN_SAMPLE_RATE_BASE / timebaseSeconds;
            double stepSize = STEP_SIZE_MULTIPLIER * screenSampleRate;
            double minFreq = MIN_FREQ_MULTIPLIER * screenSampleRate;
            double maxFreq;

            switch (filterType.ToUpperInvariant())
            {
                case "LPASS": // Low Pass
                case "HPASS": // High Pass
                    maxFreq = MAX_FREQ_MULTIPLIER * screenSampleRate;
                    break;
                case "BPASS": // Band Pass  
                case "BSTOP": // Band Stop
                    maxFreq = BAND_FILTER_W1_MAX_MULTIPLIER * screenSampleRate; // For W1 in band filters
                    break;
                default:
                    throw new ArgumentException($"Invalid filter type: {filterType}. Valid types are: LPASs, HPASs, BPASs, BSTop");
            }

            return (minFreq, maxFreq, stepSize);
        }

        /// <summary>
        /// Calculate valid W2 frequency range for band filters (Band Pass and Band Stop)
        /// W2 frequency must be higher than W1 frequency in band filters
        /// </summary>
        /// <param name="timebaseSeconds">Current timebase in seconds</param>
        /// <returns>Tuple containing (minFreq, maxFreq, stepSize) for W2</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timebaseSeconds is invalid</exception>
        public static (double minFreq, double maxFreq, double stepSize) CalculateW2FrequencyRange(double timebaseSeconds)
        {
            if (timebaseSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(timebaseSeconds), "Timebase must be greater than zero");

            double screenSampleRate = SCREEN_SAMPLE_RATE_BASE / timebaseSeconds;
            double minFreq = W2_MIN_FREQ_MULTIPLIER * screenSampleRate;   // W2 minimum is higher than W1
            double maxFreq = MAX_FREQ_MULTIPLIER * screenSampleRate;
            double stepSize = STEP_SIZE_MULTIPLIER * screenSampleRate;

            return (minFreq, maxFreq, stepSize);
        }

        /// <summary>
        /// Calculate screen sample rate based on timebase
        /// </summary>
        /// <param name="timebaseSeconds">Current timebase in seconds</param>
        /// <returns>Screen sample rate in Hz</returns>
        public static double CalculateScreenSampleRate(double timebaseSeconds)
        {
            if (timebaseSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(timebaseSeconds), "Timebase must be greater than zero");

            return SCREEN_SAMPLE_RATE_BASE / timebaseSeconds;
        }
        #endregion

        #region Validation Methods
        /// <summary>
        /// Validate frequency against timebase constraints
        /// </summary>
        /// <param name="frequency">Frequency to validate</param>
        /// <param name="timebaseSeconds">Current timebase in seconds</param>
        /// <param name="filterType">Filter type</param>
        /// <param name="isW2">Whether this is for W2 frequency (band filters)</param>
        /// <returns>True if frequency is valid</returns>
        public static bool IsValidFilterFrequency(double frequency, double timebaseSeconds, string filterType, bool isW2 = false)
        {
            try
            {
                if (frequency <= 0)
                    return false;

                if (isW2)
                {
                    var (minFreq, maxFreq, stepSize) = CalculateW2FrequencyRange(timebaseSeconds);
                    return frequency >= minFreq && frequency <= maxFreq;
                }
                else
                {
                    var (minFreq, maxFreq, stepSize) = CalculateFilterFrequencyRange(timebaseSeconds, filterType);
                    return frequency >= minFreq && frequency <= maxFreq;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate that W1 and W2 frequencies are compatible for band filters
        /// </summary>
        /// <param name="w1Frequency">W1 frequency</param>
        /// <param name="w2Frequency">W2 frequency</param>
        /// <returns>True if W1 < W2</returns>
        public static bool ValidateBandFilterFrequencies(double w1Frequency, double w2Frequency)
        {
            return w1Frequency > 0 && w2Frequency > 0 && w1Frequency < w2Frequency;
        }

        /// <summary>
        /// Check if a filter type is a band filter (requires W2)
        /// </summary>
        /// <param name="filterType">Filter type to check</param>
        /// <returns>True if band filter</returns>
        public static bool IsBandFilter(string filterType)
        {
            if (string.IsNullOrEmpty(filterType))
                return false;

            string upperType = filterType.ToUpperInvariant();
            return upperType == "BPASS" || upperType == "BSTOP";
        }

        /// <summary>
        /// Check if a source channel is valid
        /// </summary>
        /// <param name="source">Source to validate</param>
        /// <returns>True if valid source</returns>
        public static bool IsValidSource(string source)
        {
            if (string.IsNullOrEmpty(source))
                return false;

            string upperSource = source.ToUpperInvariant();
            return upperSource == "CHANNEL1" || upperSource == "CHANNEL2" ||
                   upperSource == "CHANNEL3" || upperSource == "CHANNEL4";
        }

        /// <summary>
        /// Check if a math operation is valid
        /// </summary>
        /// <param name="operation">Operation to validate</param>
        /// <returns>True if valid operation</returns>
        public static bool IsValidOperation(string operation)
        {
            if (string.IsNullOrEmpty(operation))
                return false;

            string upperOp = operation.ToUpperInvariant();
            return upperOp == "ADD" || upperOp == "SUBTRACT" ||
                   upperOp == "MULTIPLY" || upperOp == "DIVISION";
        }

        /// <summary>
        /// Check if an FFT window function is valid
        /// </summary>
        /// <param name="window">Window function to validate</param>
        /// <returns>True if valid window function</returns>
        public static bool IsValidFFTWindow(string window)
        {
            if (string.IsNullOrEmpty(window))
                return false;

            string upperWindow = window.ToUpperInvariant();
            return upperWindow == "HANNING" || upperWindow == "HAMMING" ||
                   upperWindow == "BLACKMAN" || upperWindow == "RECTANGLE";
        }
        #endregion

        #region Utility Methods




        /// <summary>
        /// Format frequency value for display with appropriate units
        /// </summary>
        /// <param name="frequency">Frequency in Hz</param>
        /// <returns>Formatted frequency string with appropriate units</returns>
        public static string FormatFrequency(double frequency)
        {
            if (frequency >= 1e9)
                return $"{frequency / 1e9:F2} GHz";
            else if (frequency >= 1e6)
                return $"{frequency / 1e6:F2} MHz";
            else if (frequency >= 1e3)
                return $"{frequency / 1e3:F2} kHz";
            else
                return $"{frequency:F2} Hz";
        }

        /// <summary>
        /// Format time value for display with appropriate units
        /// </summary>
        /// <param name="timeSeconds">Time in seconds</param>
        /// <returns>Formatted time string with appropriate units</returns>
        public static string FormatTime(double timeSeconds)
        {
            if (timeSeconds >= 1)
                return $"{timeSeconds:F3} s";
            else if (timeSeconds >= 1e-3)
                return $"{timeSeconds * 1e3:F3} ms";
            else if (timeSeconds >= 1e-6)
                return $"{timeSeconds * 1e6:F3} μs";
            else
                return $"{timeSeconds * 1e9:F3} ns";
        }

        /// <summary>
        /// Generate tooltip text for W1 frequency input
        /// </summary>
        /// <param name="timebase">Current timebase in seconds</param>
        /// <param name="filterType">Filter type (LPASs, HPASs, BPASs, BSTop)</param>
        /// <returns>Formatted tooltip string</returns>
        public static string GenerateW1TooltipText(double timebase, string filterType)
        {
            try
            {
                var (minFreq, maxFreq, stepSize) = CalculateFilterFrequencyRange(timebase, filterType);

                string filterTypeDisplay = GetFilterTypeDisplayName(filterType);
                double screenSampleRate = 100.0 / timebase;

                return $"W1 Frequency Limits ({filterTypeDisplay}):\n" +
                       $"• Min: {FormatFrequency(minFreq)}\n" +
                       $"• Max: {FormatFrequency(maxFreq)}\n" +
                       $"• Step: {FormatFrequency(stepSize)}\n" +
                       $"• Timebase: {FormatTime(timebase)}\n" +
                       $"• Sample Rate: {FormatFrequency(screenSampleRate)}";
            }
            catch (Exception ex)
            {
                return $"Error calculating W1 limits: {ex.Message}";
            }
        }

        /// <summary>
        /// Generate tooltip text for W2 frequency input
        /// </summary>
        /// <param name="timebase">Current timebase in seconds</param>
        /// <returns>Formatted tooltip string</returns>
        public static string GenerateW2TooltipText(double timebase)
        {
            try
            {
                var (minFreq, maxFreq, stepSize) = CalculateW2FrequencyRange(timebase);

                double screenSampleRate = 100.0 / timebase;

                return $"W2 Frequency Limits (Band Filters):\n" +
                       $"• Min: {FormatFrequency(minFreq)}\n" +
                       $"• Max: {FormatFrequency(maxFreq)}\n" +
                       $"• Step: {FormatFrequency(stepSize)}\n" +
                       $"• Timebase: {FormatTime(timebase)}\n" +
                       $"• Sample Rate: {FormatFrequency(screenSampleRate)}\n" +
                       $"• Note: W2 must be greater than W1";
            }
            catch (Exception ex)
            {
                return $"Error calculating W2 limits: {ex.Message}";
            }
        }












        /// <summary>
        /// Round frequency to nearest valid step size
        /// </summary>
        /// <param name="frequency">Input frequency</param>
        /// <param name="timebaseSeconds">Current timebase in seconds</param>
        /// <returns>Rounded frequency</returns>
        public static double RoundToValidFrequencyStep(double frequency, double timebaseSeconds)
        {
            if (frequency <= 0 || timebaseSeconds <= 0)
                return 0;

            double screenSampleRate = SCREEN_SAMPLE_RATE_BASE / timebaseSeconds;
            double stepSize = STEP_SIZE_MULTIPLIER * screenSampleRate;
            double minFreq = MIN_FREQ_MULTIPLIER * screenSampleRate;

            // Round to nearest step
            double steps = Math.Round((frequency - minFreq) / stepSize);
            return Math.Max(minFreq, minFreq + (steps * stepSize));
        }

        /// <summary>
        /// Constrain frequency to valid range
        /// </summary>
        /// <param name="frequency">Input frequency</param>
        /// <param name="timebaseSeconds">Current timebase in seconds</param>
        /// <param name="filterType">Filter type</param>
        /// <param name="isW2">Whether this is for W2 frequency</param>
        /// <returns>Constrained frequency within valid range</returns>
        public static double ConstrainFrequencyToValidRange(double frequency, double timebaseSeconds, string filterType, bool isW2 = false)
        {
            try
            {
                var (minFreq, maxFreq, stepSize) = isW2
                    ? CalculateW2FrequencyRange(timebaseSeconds)
                    : CalculateFilterFrequencyRange(timebaseSeconds, filterType);

                // Constrain to valid range
                double constrainedFreq = Math.Max(minFreq, Math.Min(maxFreq, frequency));

                // Round to valid step
                return RoundToValidFrequencyStep(constrainedFreq, timebaseSeconds);
            }
            catch
            {
                return 1000000; // Default 1MHz fallback
            }
        }

        /// <summary>
        /// Get filter type display name
        /// </summary>
        /// <param name="filterType">Filter type code</param>
        /// <returns>Human-readable filter type name</returns>
        public static string GetFilterTypeDisplayName(string filterType)
        {
            if (string.IsNullOrEmpty(filterType))
                return "Unknown";

            return filterType.ToUpperInvariant() switch
            {
                "LPASS" => "Low Pass",
                "HPASS" => "High Pass",
                "BPASS" => "Band Pass",
                "BSTOP" => "Band Stop",
                _ => filterType
            };
        }
        #endregion
    }
}