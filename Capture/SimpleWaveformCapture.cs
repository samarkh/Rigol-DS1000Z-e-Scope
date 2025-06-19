using Rigol_DS1000Z_E_Control;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace DS1000Z_E_USB_Control
{
    /// <summary>
    /// Simple waveform capture and memory system for the oscilloscope.
    /// This single class handles everything - no complex architecture needed.
    /// </summary>
    public class SimpleWaveformCapture
    {
        #region Private Fields

        private readonly RigolDS1000ZE oscilloscope;
        private readonly List<CapturedWaveform> storedWaveforms;
        private int maxStoredWaveforms = 100;

        #endregion

        #region Events

        /// <summary>Event for logging messages</summary>
        public event EventHandler<string> LogEvent;

        /// <summary>Event when a waveform is captured</summary>
        public event EventHandler<CapturedWaveform> WaveformCaptured;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize the simple capture system
        /// </summary>
        /// <param name="oscilloscope">Your existing oscilloscope instance</param>
        public SimpleWaveformCapture(RigolDS1000ZE oscilloscope)
        {
            this.oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));
            this.storedWaveforms = new List<CapturedWaveform>();

            Log("✅ Simple capture system initialized");
        }

        #endregion

        #region Main Methods

        /// <summary>
        /// Capture waveform from specified channel
        /// </summary>
        /// <param name="channelNumber">Channel number (1 or 2)</param>
        /// <returns>Captured waveform data, or null if failed</returns>
        public CapturedWaveform CaptureWaveform(int channelNumber = 1)
        {
            if (!oscilloscope.IsConnected)
            {
                Log("❌ Cannot capture: Oscilloscope not connected");
                return null;
            }

            try
            {
                Log($"🎯 Capturing waveform from Channel {channelNumber}...");

                // Stop acquisition for stable capture
                oscilloscope.SendCommand(":STOP");
                System.Threading.Thread.Sleep(100);

                // Set waveform source
                oscilloscope.SendCommand($":WAVeform:SOURce CHANnel{channelNumber}");
                oscilloscope.SendCommand(":WAVeform:MODE NORMal");
                oscilloscope.SendCommand(":WAVeform:FORMat BYTE");

                // Get waveform parameters
                string preambleResponse = oscilloscope.SendQuery(":WAVeform:PREamble?");
                double[] preamble = ParsePreamble(preambleResponse);

                // Get waveform data
                string dataResponse = oscilloscope.SendQuery(":WAVeform:DATA?");
                byte[] rawData = ParseWaveformData(dataResponse);

                if (rawData == null || rawData.Length == 0)
                {
                    Log("❌ No waveform data received");
                    return null;
                }

                // Convert to voltages
                double[] voltages = ConvertToVoltages(rawData, preamble);
                double[] timeValues = GenerateTimeValues(voltages.Length, preamble);

                // Create waveform object
                var waveform = new CapturedWaveform
                {
                    ChannelNumber = channelNumber,
                    CaptureTime = DateTime.Now,
                    VoltageData = voltages.ToList(),
                    TimeData = timeValues.ToList(),
                    SampleCount = voltages.Length,
                    Description = $"CH{channelNumber} - {DateTime.Now:HH:mm:ss}"
                };

                // Store it
                StoreWaveform(waveform);

                Log($"✅ Captured {voltages.Length} points from Channel {channelNumber}");
                WaveformCaptured?.Invoke(this, waveform);

                return waveform;
            }
            catch (Exception ex)
            {
                Log($"❌ Capture error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get all stored waveforms
        /// </summary>
        public List<CapturedWaveform> GetStoredWaveforms()
        {
            return new List<CapturedWaveform>(storedWaveforms);
        }

        /// <summary>
        /// Clear all stored waveforms
        /// </summary>
        public void ClearMemory()
        {
            int count = storedWaveforms.Count;
            storedWaveforms.Clear();
            Log($"🗑️ Memory cleared: {count} waveforms removed");
        }

        /// <summary>
        /// Export waveform to CSV file
        /// </summary>
        /// <param name="waveform">Waveform to export</param>
        /// <param name="filePath">Output file path</param>
        /// <returns>True if successful</returns>
        public bool ExportToCSV(CapturedWaveform waveform, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    // Write header
                    writer.WriteLine($"# Waveform Export - {waveform.Description}");
                    writer.WriteLine($"# Captured: {waveform.CaptureTime}");
                    writer.WriteLine($"# Points: {waveform.SampleCount}");
                    writer.WriteLine("Time (s),Voltage (V)");

                    // Write data
                    for (int i = 0; i < waveform.VoltageData.Count; i++)
                    {
                        double time = i < waveform.TimeData.Count ? waveform.TimeData[i] : i * 1e-6;
                        writer.WriteLine($"{time:E6},{waveform.VoltageData[i]:F6}");
                    }
                }

                Log($"📁 Exported to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"❌ Export error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get memory status information
        /// </summary>
        public string GetMemoryStatus()
        {
            var ch1Count = storedWaveforms.Count(w => w.ChannelNumber == 1);
            var ch2Count = storedWaveforms.Count(w => w.ChannelNumber == 2);
            var totalPoints = storedWaveforms.Sum(w => w.SampleCount);

            return $"Stored: {storedWaveforms.Count} waveforms (CH1: {ch1Count}, CH2: {ch2Count}) - {totalPoints:N0} total points";
        }

        #endregion

        #region Helper Methods

        private void StoreWaveform(CapturedWaveform waveform)
        {
            storedWaveforms.Add(waveform);

            // Remove oldest if over limit
            while (storedWaveforms.Count > maxStoredWaveforms)
            {
                var oldest = storedWaveforms.OrderBy(w => w.CaptureTime).First();
                storedWaveforms.Remove(oldest);
                Log($"📦 Memory limit reached: removed oldest waveform");
            }
        }

        private double[] ParsePreamble(string response)
        {
            try
            {
                if (string.IsNullOrEmpty(response)) return new double[10];

                var parts = response.Split(',');
                var preamble = new double[10];

                for (int i = 0; i < Math.Min(parts.Length, 10); i++)
                {
                    if (double.TryParse(parts[i], out double value))
                        preamble[i] = value;
                }

                return preamble;
            }
            catch
            {
                return new double[10]; // Default values
            }
        }

        private byte[] ParseWaveformData(string response)
        {
            try
            {
                if (string.IsNullOrEmpty(response) || response.Length < 10) return new byte[0];

                // Skip TMC header (e.g., "#9000001200")
                int headerLength = 2 + int.Parse(response.Substring(1, 1));

                if (response.Length <= headerLength) return new byte[0];

                // Convert remaining string to bytes
                var dataString = response.Substring(headerLength);
                return Encoding.ASCII.GetBytes(dataString);
            }
            catch
            {
                return new byte[0];
            }
        }

        private double[] ConvertToVoltages(byte[] rawData, double[] preamble)
        {
            var voltages = new double[rawData.Length];

            // Use preamble values if available, otherwise use defaults
            double yIncrement = preamble.Length > 7 ? preamble[7] : 0.001;
            double yOrigin = preamble.Length > 8 ? preamble[8] : 0.0;
            double yReference = preamble.Length > 9 ? preamble[9] : 0.0;

            for (int i = 0; i < rawData.Length; i++)
            {
                voltages[i] = (rawData[i] - yReference) * yIncrement + yOrigin;
            }

            return voltages;
        }

        private double[] GenerateTimeValues(int pointCount, double[] preamble)
        {
            var timeValues = new double[pointCount];

            // Use preamble values if available, otherwise use defaults
            double xIncrement = preamble.Length > 4 ? preamble[4] : 1e-6;
            double xOrigin = preamble.Length > 5 ? preamble[5] : 0.0;

            for (int i = 0; i < pointCount; i++)
            {
                timeValues[i] = xOrigin + i * xIncrement;
            }

            return timeValues;
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }

        #endregion

        #region Properties

        /// <summary>Number of stored waveforms</summary>
        public int StoredWaveformCount => storedWaveforms.Count;

        /// <summary>Memory limit</summary>
        public int MemoryLimit
        {
            get => maxStoredWaveforms;
            set => maxStoredWaveforms = Math.Max(1, value);
        }

        #endregion
    }

    /// <summary>
    /// Simple class to hold captured waveform data
    /// </summary>
    public class CapturedWaveform
    {
        public int ChannelNumber { get; set; }
        public DateTime CaptureTime { get; set; }
        public List<double> VoltageData { get; set; } = new List<double>();
        public List<double> TimeData { get; set; } = new List<double>();
        public int SampleCount { get; set; }
        public string Description { get; set; } = "";

        public override string ToString()
        {
            return $"CH{ChannelNumber} - {CaptureTime:HH:mm:ss} - {SampleCount:N0} samples";
        }
    }
}