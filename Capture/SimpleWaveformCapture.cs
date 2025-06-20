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
    /// Export format options for waveform data
    /// </summary>
    public enum ExportFormat
    {
        CSV,
        JSON,
        MATLAB,
        RawBinary,
        WithPreamble
    }

    /// <summary>
    /// Simple waveform capture and memory system for the oscilloscope.
    /// Supports multiple export formats and handles binary data properly.
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

            Log("✅ Simple capture system initialized with multi-format export support");
        }

        #endregion

        #region Main Capture Methods

        /// <summary>
        /// Capture waveform from specified channel with binary data support
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

                // Set waveform source and format
                oscilloscope.SendCommand($":WAVeform:SOURce CHANnel{channelNumber}");
                oscilloscope.SendCommand(":WAVeform:MODE NORMal");
                oscilloscope.SendCommand(":WAVeform:FORMat BYTE");

                // Get waveform parameters (text data)
                string preambleResponse = oscilloscope.SendQuery(":WAVeform:PREamble?");
                Log($"📊 Preamble: {preambleResponse}");
                double[] preamble = ParsePreamble(preambleResponse);

                // Get waveform data (binary data) - use binary query method
                byte[] binaryData = oscilloscope.SendBinaryQuery(":WAVeform:DATA?");

                if (binaryData == null || binaryData.Length == 0)
                {
                    Log("❌ No binary data received");
                    return null;
                }

                Log($"📈 Received {binaryData.Length} bytes of raw data");

                // Parse the binary data correctly
                byte[] rawData = ParseBinaryWaveformData(binaryData);

                if (rawData == null || rawData.Length == 0)
                {
                    Log("❌ Failed to parse waveform data");
                    return null;
                }

                Log($"✅ Parsed {rawData.Length} data points");

                // Convert to voltages and time values
                double[] voltages = ConvertToVoltages(rawData, preamble);
                double[] timeValues = GenerateTimeValues(voltages.Length, preamble);

                // Create waveform object with all data
                var waveform = new CapturedWaveform
                {
                    ChannelNumber = channelNumber,
                    CaptureTime = DateTime.Now,
                    VoltageData = voltages.ToList(),
                    TimeData = timeValues.ToList(),
                    SampleCount = voltages.Length,
                    Description = $"CH{channelNumber} - {DateTime.Now:HH:mm:ss}",
                    Preamble = preamble,  // Store preamble for complete exports
                    RawData = rawData     // Store raw data for binary exports
                };

                // Store it in memory
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

        #endregion

        #region Export Methods

        /// <summary>
        /// Export waveform in the specified format
        /// </summary>
        public bool ExportWaveform(CapturedWaveform waveform, string filePath, ExportFormat format)
        {
            if (waveform == null)
            {
                Log("❌ Cannot export: No waveform data");
                return false;
            }

            switch (format)
            {
                case ExportFormat.CSV:
                    return ExportToCSV(waveform, filePath);
                case ExportFormat.JSON:
                    return ExportToJSON(waveform, filePath);
                case ExportFormat.MATLAB:
                    return ExportToMAT(waveform, filePath);
                case ExportFormat.RawBinary:
                    return ExportRawBinary(waveform, filePath);
                case ExportFormat.WithPreamble:
                    return ExportWithPreamble(waveform, filePath);
                default:
                    Log($"❌ Unknown export format: {format}");
                    return false;
            }
        }

        /// <summary>
        /// Export waveform to CSV file (original format)
        /// </summary>
        public bool ExportToCSV(CapturedWaveform waveform, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    // Write header with metadata
                    writer.WriteLine($"# Waveform Export - {waveform.Description}");
                    writer.WriteLine($"# Captured: {waveform.CaptureTime}");
                    writer.WriteLine($"# Channel: {waveform.ChannelNumber}");
                    writer.WriteLine($"# Sample Count: {waveform.SampleCount}");
                    writer.WriteLine($"# Export Format: CSV");
                    writer.WriteLine($"# Export Time: {DateTime.Now}");
                    writer.WriteLine("Time (s),Voltage (V)");

                    // Write data
                    for (int i = 0; i < waveform.VoltageData.Count; i++)
                    {
                        double time = i < waveform.TimeData.Count ?
                            waveform.TimeData[i] : i * 1e-6;
                        writer.WriteLine($"{time:E6},{waveform.VoltageData[i]:F6}");
                    }
                }

                Log($"📁 Exported CSV to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"❌ CSV export error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Export as JSON for web applications (manual serialization - no dependencies)
        /// </summary>
        public bool ExportToJSON(CapturedWaveform waveform, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("{");
                    writer.WriteLine("  \"metadata\": {");
                    writer.WriteLine($"    \"channel\": {waveform.ChannelNumber},");
                    writer.WriteLine($"    \"captureTime\": \"{waveform.CaptureTime:yyyy-MM-ddTHH:mm:ss.fffZ}\",");
                    writer.WriteLine($"    \"sampleCount\": {waveform.SampleCount},");
                    writer.WriteLine($"    \"description\": \"{EscapeJsonString(waveform.Description)}\",");
                    writer.WriteLine($"    \"exportFormat\": \"JSON\",");
                    writer.WriteLine($"    \"exportTime\": \"{DateTime.Now:yyyy-MM-ddTHH:mm:ss.fffZ}\",");
                    writer.WriteLine($"    \"software\": \"Rigol DS1000Z-E Control\"");
                    writer.WriteLine("  },");
                    writer.WriteLine("  \"waveform\": {");
                    writer.WriteLine("    \"timeUnit\": \"seconds\",");
                    writer.WriteLine("    \"voltageUnit\": \"volts\",");
                    writer.WriteLine("    \"data\": [");

                    // Write data points
                    for (int i = 0; i < waveform.VoltageData.Count; i++)
                    {
                        double time = i < waveform.TimeData.Count ?
                            waveform.TimeData[i] : i * 1e-6;
                        double voltage = waveform.VoltageData[i];

                        writer.Write($"      {{\"time\": {time:E6}, \"voltage\": {voltage:F6}}}");

                        if (i < waveform.VoltageData.Count - 1)
                        {
                            writer.WriteLine(",");
                        }
                        else
                        {
                            writer.WriteLine();
                        }
                    }

                    writer.WriteLine("    ]");
                    writer.WriteLine("  }");
                    writer.WriteLine("}");
                }

                Log($"📁 Exported JSON to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"❌ JSON export error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Export as MATLAB-compatible .m file
        /// </summary>
        public bool ExportToMAT(CapturedWaveform waveform, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("% MATLAB compatible waveform data");
                    writer.WriteLine($"% Generated by: Rigol DS1000Z-E Control Software");
                    writer.WriteLine($"% Channel: {waveform.ChannelNumber}");
                    writer.WriteLine($"% Captured: {waveform.CaptureTime}");
                    writer.WriteLine($"% Sample Count: {waveform.SampleCount}");
                    writer.WriteLine($"% Export Time: {DateTime.Now}");
                    writer.WriteLine();

                    // Time vector
                    writer.WriteLine("% Time data (seconds)");
                    writer.Write("time = [");
                    for (int i = 0; i < waveform.TimeData.Count; i++)
                    {
                        if (i > 0) writer.Write(", ");
                        if (i % 8 == 0 && i > 0)
                        {
                            writer.WriteLine(" ...");
                            writer.Write("        ");
                        }
                        writer.Write($"{waveform.TimeData[i]:E6}");
                    }
                    writer.WriteLine("];");
                    writer.WriteLine();

                    // Voltage vector
                    writer.WriteLine("% Voltage data (volts)");
                    writer.Write("voltage = [");
                    for (int i = 0; i < waveform.VoltageData.Count; i++)
                    {
                        if (i > 0) writer.Write(", ");
                        if (i % 8 == 0 && i > 0)
                        {
                            writer.WriteLine(" ...");
                            writer.Write("          ");
                        }
                        writer.Write($"{waveform.VoltageData[i]:F6}");
                    }
                    writer.WriteLine("];");
                    writer.WriteLine();

                    // Metadata structure
                    writer.WriteLine("% Metadata structure");
                    writer.WriteLine("metadata = struct();");
                    writer.WriteLine($"metadata.channel = {waveform.ChannelNumber};");
                    writer.WriteLine($"metadata.sampleCount = {waveform.SampleCount};");
                    writer.WriteLine($"metadata.captureTime = '{waveform.CaptureTime:yyyy-MM-dd HH:mm:ss}';");
                    writer.WriteLine($"metadata.description = '{waveform.Description}';");
                    writer.WriteLine();

                    // Usage examples
                    writer.WriteLine("% Usage examples:");
                    writer.WriteLine("% figure;");
                    writer.WriteLine("% plot(time, voltage);");
                    writer.WriteLine("% xlabel('Time (s)');");
                    writer.WriteLine("% ylabel('Voltage (V)');");
                    writer.WriteLine($"% title('Channel {waveform.ChannelNumber} Waveform - {waveform.CaptureTime:HH:mm:ss}');");
                    writer.WriteLine("% grid on;");
                }

                Log($"📁 Exported MATLAB format to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"❌ MATLAB export error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Export raw binary data with metadata header
        /// </summary>
        public bool ExportRawBinary(CapturedWaveform waveform, string filePath)
        {
            try
            {
                using (var writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
                {
                    // Write file signature and version
                    writer.Write("RIGOL_RAW_V1".ToCharArray());

                    // Write metadata
                    writer.Write(waveform.ChannelNumber);
                    writer.Write(waveform.CaptureTime.ToBinary());
                    writer.Write(waveform.SampleCount);

                    // Write description length and description
                    string desc = waveform.Description ?? "";
                    writer.Write(desc.Length);
                    if (desc.Length > 0)
                        writer.Write(desc.ToCharArray());

                    // Write preamble if available
                    if (waveform.Preamble != null)
                    {
                        writer.Write(waveform.Preamble.Length);
                        foreach (double value in waveform.Preamble)
                        {
                            writer.Write(value);
                        }
                    }
                    else
                    {
                        writer.Write(0); // No preamble
                    }

                    // Write raw data if available, otherwise convert voltages
                    if (waveform.RawData != null)
                    {
                        writer.Write(waveform.RawData.Length);
                        writer.Write(waveform.RawData);
                    }
                    else
                    {
                        // Convert voltages back to approximate raw values
                        writer.Write(waveform.VoltageData.Count);
                        foreach (double voltage in waveform.VoltageData)
                        {
                            byte approximateRaw = (byte)Math.Round((voltage + 2.0) * 127.0 / 4.0);
                            writer.Write(approximateRaw);
                        }
                    }
                }

                Log($"📁 Exported raw binary to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"❌ Raw binary export error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Export with complete preamble information for full reconstruction
        /// </summary>
        public bool ExportWithPreamble(CapturedWaveform waveform, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("# Complete Rigol DS1000Z-E Waveform Export");
                    writer.WriteLine($"# Generated by: Rigol DS1000Z-E Control Software");
                    writer.WriteLine($"# Export time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"# Channel: {waveform.ChannelNumber}");
                    writer.WriteLine($"# Captured: {waveform.CaptureTime}");
                    writer.WriteLine($"# Sample count: {waveform.SampleCount}");
                    writer.WriteLine($"# Description: {waveform.Description}");
                    writer.WriteLine("#");
                    writer.WriteLine("# SCPI Preamble Parameters (for complete data reconstruction):");
                    writer.WriteLine("# [0] Format (0=BYTE, 1=WORD, 2=ASC)");
                    writer.WriteLine("# [1] Type (0=NORMal, 1=MAXimum, 2=RAW)");
                    writer.WriteLine("# [2] Points (number of data points)");
                    writer.WriteLine("# [3] Count (always 1 in NORMal mode)");
                    writer.WriteLine("# [4] XIncrement (time between points)");
                    writer.WriteLine("# [5] XOrigin (time of first point)");
                    writer.WriteLine("# [6] XReference (reference point)");
                    writer.WriteLine("# [7] YIncrement (voltage per ADC count)");
                    writer.WriteLine("# [8] YOrigin (voltage at ADC=0)");
                    writer.WriteLine("# [9] YReference (ADC reference point)");
                    writer.WriteLine("#");

                    if (waveform.Preamble != null && waveform.Preamble.Length > 0)
                    {
                        for (int i = 0; i < Math.Min(waveform.Preamble.Length, 10); i++)
                        {
                            writer.WriteLine($"# Preamble[{i}]: {waveform.Preamble[i]:E6}");
                        }
                    }
                    else
                    {
                        writer.WriteLine("# No preamble data available");
                    }

                    writer.WriteLine("#");
                    writer.WriteLine("# Voltage calculation: V = (ADC - YReference) * YIncrement + YOrigin");
                    writer.WriteLine("# Time calculation: T = XOrigin + (Index * XIncrement)");
                    writer.WriteLine("#");
                    writer.WriteLine("# Data format: Index, Time(s), Voltage(V), RawADC");
                    writer.WriteLine("Index,Time,Voltage,RawADC");

                    for (int i = 0; i < waveform.VoltageData.Count; i++)
                    {
                        double time = i < waveform.TimeData.Count ?
                            waveform.TimeData[i] : i * 1e-6;

                        // Get raw ADC value if available, otherwise approximate
                        int rawADC = 128; // Default
                        if (waveform.RawData != null && i < waveform.RawData.Length)
                        {
                            rawADC = waveform.RawData[i];
                        }
                        else
                        {
                            // Approximate from voltage if preamble is available
                            if (waveform.Preamble != null && waveform.Preamble.Length > 9)
                            {
                                double yIncrement = waveform.Preamble[7];
                                double yOrigin = waveform.Preamble[8];
                                double yReference = waveform.Preamble[9];

                                if (Math.Abs(yIncrement) > 1e-12) // Avoid division by zero
                                {
                                    rawADC = (int)Math.Round((waveform.VoltageData[i] - yOrigin) / yIncrement + yReference);
                                }
                            }
                        }

                        writer.WriteLine($"{i},{time:E6},{waveform.VoltageData[i]:F6},{rawADC}");
                    }
                }

                Log($"📁 Exported with preamble to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"❌ Preamble export error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Static Helper Methods for Export Formats

        /// <summary>
        /// Get file extension for export format
        /// </summary>
        public static string GetFileExtension(ExportFormat format)
        {
            switch (format)
            {
                case ExportFormat.CSV:
                case ExportFormat.WithPreamble:
                    return ".csv";
                case ExportFormat.JSON:
                    return ".json";
                case ExportFormat.MATLAB:
                    return ".m";
                case ExportFormat.RawBinary:
                    return ".bin";
                default:
                    return ".txt";
            }
        }

        /// <summary>
        /// Get file filter for save dialog
        /// </summary>
        public static string GetFileFilter(ExportFormat format)
        {
            switch (format)
            {
                case ExportFormat.CSV:
                case ExportFormat.WithPreamble:
                    return "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                case ExportFormat.JSON:
                    return "JSON files (*.json)|*.json|All files (*.*)|*.*";
                case ExportFormat.MATLAB:
                    return "MATLAB files (*.m)|*.m|All files (*.*)|*.*";
                case ExportFormat.RawBinary:
                    return "Binary files (*.bin)|*.bin|All files (*.*)|*.*";
                default:
                    return "All files (*.*)|*.*";
            }
        }

        /// <summary>
        /// Get format description for UI display
        /// </summary>
        public static string GetFormatDescription(ExportFormat format)
        {
            switch (format)
            {
                case ExportFormat.CSV:
                    return "Comma-separated values (Excel compatible)";
                case ExportFormat.JSON:
                    return "JavaScript Object Notation (web compatible)";
                case ExportFormat.MATLAB:
                    return "MATLAB script file (ready to plot)";
                case ExportFormat.RawBinary:
                    return "Raw binary data (compact format)";
                case ExportFormat.WithPreamble:
                    return "CSV with complete oscilloscope parameters";
                default:
                    return "Unknown format";
            }
        }

        #endregion

        #region USB Storage Methods (Direct to Oscilloscope)

        /// <summary>
        /// Save waveform directly to USB drive connected to oscilloscope
        /// </summary>
        public bool SaveWaveformToUSB(int channelNumber, string filename)
        {
            try
            {
                if (!oscilloscope.IsConnected)
                {
                    Log("❌ Oscilloscope not connected");
                    return false;
                }

                // Stop acquisition for stable save
                oscilloscope.SendCommand(":STOP");
                System.Threading.Thread.Sleep(100);

                // Set the file format (CSV, BIN, etc.) - you can modify this
                oscilloscope.SendCommand(":STORage:WAVeform:FORMat CSV");

                // Set the source channel
                oscilloscope.SendCommand($":STORage:WAVeform:SOURce CHANnel{channelNumber}");

                // Set the filename (without extension)
                oscilloscope.SendCommand($":STORage:WAVeform:FNAMe \"{filename}\"");

                // Save to USB
                oscilloscope.SendCommand(":STORage:WAVeform:SAVE");

                Log($"✅ Saved CH{channelNumber} waveform to USB: {filename}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"❌ USB save error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check USB drive status
        /// </summary>
        public bool CheckUSBStatus()
        {
            try
            {
                if (!oscilloscope.IsConnected)
                {
                    Log("❌ Oscilloscope not connected");
                    return false;
                }

                // Query storage system status
                string status = oscilloscope.SendQuery(":STORage:STATus?");
                Log($"System status: {status}");

                // Try to list files (if USB is connected, this should work)
                // Note: Exact command may vary - check oscilloscope manual
                string files = oscilloscope.SendQuery(":STORage:CATalog?");

                if (!string.IsNullOrEmpty(files))
                {
                    Log($"📁 Files on USB: {files}");
                    return true;
                }
                else
                {
                    Log("📁 No USB drive detected or empty");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"❌ USB status check error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Memory Management Methods

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
        /// Get memory status information
        /// </summary>
        public string GetMemoryStatus()
        {
            var ch1Count = storedWaveforms.Count(w => w.ChannelNumber == 1);
            var ch2Count = storedWaveforms.Count(w => w.ChannelNumber == 2);
            var totalPoints = storedWaveforms.Sum(w => w.SampleCount);
            var totalMemoryMB = totalPoints * 16 / (1024 * 1024); // Rough estimate in MB

            return $"Stored: {storedWaveforms.Count} waveforms (CH1: {ch1Count}, CH2: {ch2Count})\n" +
                   $"Total Points: {totalPoints:N0}\n" +
                   $"Est. Memory: ~{totalMemoryMB:F1} MB\n" +
                   $"Limit: {maxStoredWaveforms} waveforms";
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
                Log($"📦 Memory limit reached: removed oldest waveform from {oldest.CaptureTime:HH:mm:ss}");
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

                Log($"📋 Parsed preamble: {preamble.Length} parameters");
                return preamble;
            }
            catch (Exception ex)
            {
                Log($"⚠️ Preamble parsing error: {ex.Message}");
                return new double[10]; // Default values
            }
        }

        private byte[] ParseBinaryWaveformData(byte[] binaryResponse)
        {
            try
            {
                if (binaryResponse == null || binaryResponse.Length < 10)
                {
                    Log("❌ Binary data too short");
                    return new byte[0];
                }

                // Check for TMC header starting with '#'
                if (binaryResponse[0] != (byte)'#')
                {
                    Log("❌ Invalid TMC header - doesn't start with #");
                    return new byte[0];
                }

                // Get the length of the length field
                int lengthDigits = binaryResponse[1] - (byte)'0';
                if (lengthDigits < 1 || lengthDigits > 9)
                {
                    Log($"❌ Invalid TMC header length digits: {lengthDigits}");
                    return new byte[0];
                }

                // Calculate total header length
                int headerLength = 2 + lengthDigits;

                if (binaryResponse.Length <= headerLength)
                {
                    Log($"❌ Binary data too short for header length {headerLength}");
                    return new byte[0];
                }

                // Extract the actual data (skip TMC header)
                int dataLength = binaryResponse.Length - headerLength;
                byte[] actualData = new byte[dataLength];
                Array.Copy(binaryResponse, headerLength, actualData, 0, dataLength);

                Log($"📋 TMC Header: {headerLength} bytes, Data: {dataLength} points");
                return actualData;
            }
            catch (Exception ex)
            {
                Log($"❌ Binary parsing error: {ex.Message}");
                return new byte[0];
            }
        }

        private double[] ConvertToVoltages(byte[] rawData, double[] preamble)
        {
            var voltages = new double[rawData.Length];

            // Use preamble values if available, otherwise use defaults
            double yIncrement = preamble.Length > 7 ? preamble[7] : 0.001;
            double yOrigin = preamble.Length > 8 ? preamble[8] : 0.0;
            double yReference = preamble.Length > 9 ? preamble[9] : 127.0;

            for (int i = 0; i < rawData.Length; i++)
            {
                voltages[i] = (rawData[i] - yReference) * yIncrement + yOrigin;
            }

            Log($"⚡ Converted {rawData.Length} points to voltages (range: {voltages.Min():F3}V to {voltages.Max():F3}V)");
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

            Log($"⏱️ Generated time values: {xIncrement:E3}s/point, span: {timeValues.Last() - timeValues.First():E3}s");
            return timeValues;
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }

        /// <summary>
        /// Helper method to escape strings for JSON
        /// </summary>
        private string EscapeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            return input.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t");
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
    /// Enhanced class to hold captured waveform data with export support
    /// </summary>
    public class CapturedWaveform
    {
        public int ChannelNumber { get; set; }
        public DateTime CaptureTime { get; set; }
        public List<double> VoltageData { get; set; } = new List<double>();
        public List<double> TimeData { get; set; } = new List<double>();
        public int SampleCount { get; set; }
        public string Description { get; set; } = "";

        // New properties for enhanced export capabilities
        public double[] Preamble { get; set; }  // SCPI preamble parameters
        public byte[] RawData { get; set; }     // Original ADC values

        /// <summary>
        /// Alias for CaptureTime to maintain compatibility
        /// </summary>
        public DateTime Timestamp
        {
            get => CaptureTime;
            set => CaptureTime = value;
        }

        /// <summary>
        /// Sample rate calculated from preamble data
        /// </summary>
        public double SampleRate
        {
            get
            {
                if (Preamble != null && Preamble.Length > 4 && Math.Abs(Preamble[4]) > 1e-12)
                {
                    return 1.0 / Preamble[4]; // Sample rate = 1/time increment
                }
                return 1e6; // Default 1 MSa/s if not available
            }
        }

        public override string ToString()
        {
            return $"CH{ChannelNumber} - {CaptureTime:HH:mm:ss} - {SampleCount:N0} samples";
        }

        /// <summary>
        /// Get detailed information about this waveform
        /// </summary>
        public string GetDetailedInfo()
        {
            var info = new StringBuilder();
            info.AppendLine($"Channel: {ChannelNumber}");
            info.AppendLine($"Captured: {CaptureTime:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine($"Sample Count: {SampleCount:N0}");
            info.AppendLine($"Sample Rate: {SampleRate / 1e6:F1} MSa/s");
            info.AppendLine($"Description: {Description}");

            if (VoltageData.Count > 0)
            {
                info.AppendLine($"Voltage Range: {VoltageData.Min():F3}V to {VoltageData.Max():F3}V");
            }

            if (TimeData.Count > 0)
            {
                info.AppendLine($"Time Span: {(TimeData.Last() - TimeData.First()) * 1000:F3}ms");
            }

            return info.ToString();
        }
    }
}