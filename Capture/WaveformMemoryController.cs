using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace OscilloscopeControl.Capture
{
    /// <summary>
    /// Core manager class that handles waveform capture from the Rigol DS1000Z-E oscilloscope.
    /// Implements SCPI commands for data retrieval, processing, and storage.
    /// </summary>
    public class WaveformMemoryManager
    {
        #region Private Fields

        private readonly IOscilloscopeInterface oscilloscope;
        private readonly List<WaveformData> storedWaveforms;
        private readonly object waveformLock = new object();
        private int maxStoredWaveforms = 100;

        // SCPI command timeout for data operations (longer than normal commands)
        private const int DataCommandTimeoutMs = 30000;

        #endregion

        #region Events

        /// <summary>Raised when the manager needs to log a message</summary>
        public event EventHandler<string> LogEvent;

        /// <summary>Raised when a waveform is successfully captured</summary>
        public event EventHandler<WaveformData> WaveformCaptured;

        /// <summary>Raised when memory is cleared</summary>
        public event EventHandler MemoryCleared;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize the waveform memory manager with an oscilloscope interface
        /// </summary>
        /// <param name="oscilloscope">The oscilloscope interface for SCPI communication</param>
        public WaveformMemoryManager(IOscilloscopeInterface oscilloscope)
        {
            this.oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));
            this.storedWaveforms = new List<WaveformData>();

            Log("🔧 Waveform memory manager initialized");
        }

        #endregion

        #region Main Capture Methods

        /// <summary>
        /// Capture waveform data from the specified channel(s)
        /// </summary>
        /// <param name="channelSelection">Which channel(s) to capture from</param>
        /// <param name="mode">Waveform capture mode (Normal, Raw, Maximum)</param>
        /// <param name="format">Data format (Byte, Word, ASCII)</param>
        /// <returns>List of captured waveforms</returns>
        public List<WaveformData> CaptureWaveforms(WaveformChannel channelSelection,
                                                    WaveformMode mode = WaveformMode.NORMal,
                                                    WaveformFormat format = WaveformFormat.BYTE)
        {
            if (!oscilloscope.IsConnected)
            {
                Log("❌ Cannot capture: Oscilloscope not connected");
                return new List<WaveformData>();
            }

            var capturedWaveforms = new List<WaveformData>();

            try
            {
                Log($"🎯 Starting waveform capture - Channel: {channelSelection}, Mode: {mode}, Format: {format}");

                // Stop acquisition to ensure stable data during transfer
                SendCommand(":STOP");
                Thread.Sleep(100); // Brief pause to ensure acquisition stops

                // Configure waveform acquisition parameters
                ConfigureWaveformSettings(mode, format);

                // Capture based on channel selection
                switch (channelSelection)
                {
                    case WaveformChannel.Channel1:
                        var ch1Data = CaptureChannelWaveform(1, mode, format);
                        if (ch1Data != null) capturedWaveforms.Add(ch1Data);
                        break;

                    case WaveformChannel.Channel2:
                        var ch2Data = CaptureChannelWaveform(2, mode, format);
                        if (ch2Data != null) capturedWaveforms.Add(ch2Data);
                        break;

                    case WaveformChannel.Both:
                        var ch1BothData = CaptureChannelWaveform(1, mode, format);
                        var ch2BothData = CaptureChannelWaveform(2, mode, format);
                        if (ch1BothData != null) capturedWaveforms.Add(ch1BothData);
                        if (ch2BothData != null) capturedWaveforms.Add(ch2BothData);
                        break;
                }

                // Store captured waveforms in memory
                lock (waveformLock)
                {
                    foreach (var waveform in capturedWaveforms)
                    {
                        StoreWaveform(waveform);
                        WaveformCaptured?.Invoke(this, waveform);
                    }
                }

                Log($"✅ Capture completed: {capturedWaveforms.Count} waveform(s) captured");
            }
            catch (Exception ex)
            {
                Log($"❌ Capture error: {ex.Message}");
            }
            finally
            {
                // Always resume acquisition
                try
                {
                    SendCommand(":RUN");
                }
                catch (Exception ex)
                {
                    Log($"⚠️ Warning: Could not resume acquisition: {ex.Message}");
                }
            }

            return capturedWaveforms;
        }

        /// <summary>
        /// Capture waveform data from a specific channel
        /// </summary>
        /// <param name="channelNumber">Channel number (1 or 2)</param>
        /// <param name="mode">Capture mode</param>
        /// <param name="format">Data format</param>
        /// <returns>Captured waveform data or null if capture failed</returns>
        private WaveformData CaptureChannelWaveform(int channelNumber, WaveformMode mode, WaveformFormat format)
        {
            try
            {
                string channelName = $"Channel {channelNumber}";
                string channelScpi = $"CHANnel{channelNumber}";

                Log($"📊 Capturing data from {channelName}...");

                // Check if channel is enabled
                if (!IsChannelEnabled(channelNumber))
                {
                    Log($"⚠️ {channelName} is disabled, skipping capture");
                    return null;
                }

                // Set waveform source to this channel
                SendCommand($":WAVeform:SOURce {channelScpi}");

                // Get waveform preamble (contains important scaling information)
                var preambleData = GetWaveformPreamble();

                // Get waveform data points
                byte[] rawData = GetWaveformRawData();
                if (rawData.Length == 0)
                {
                    Log($"❌ No data received from {channelName}");
                    return null;
                }

                // Get timing and scaling parameters from oscilloscope
                var timingParams = GetTimingParameters();
                var voltageParams = GetVoltageParameters();
                var channelConfig = GetChannelConfiguration(channelNumber);
                var acquisitionInfo = GetAcquisitionInformation();
                var triggerInfo = GetTriggerInformation();
                var timebaseInfo = GetTimebaseInformation();

                // Convert raw data to voltage and time arrays
                double[] voltageValues = ConvertRawDataToVoltages(rawData, voltageParams, format);
                double[] timeValues = GenerateTimeValues(voltageValues.Length, timingParams);

                // Create waveform data object
                var waveformData = new WaveformData
                {
                    ChannelName = channelName,
                    CaptureTime = DateTime.Now,
                    TimeValues = timeValues,
                    VoltageValues = voltageValues,
                    RawData = rawData,
                    CaptureMode = mode,
                    DataFormat = format,

                    // Timing parameters
                    SampleRate = acquisitionInfo.SampleRate,
                    TimeIncrement = timingParams.XIncrement,
                    TimeOrigin = timingParams.XOrigin,
                    TimeReference = timingParams.XReference,

                    // Voltage parameters  
                    VoltageIncrement = voltageParams.YIncrement,
                    VoltageOrigin = voltageParams.YOrigin,
                    VoltageReference = voltageParams.YReference,
                    VerticalScale = channelConfig.VerticalScale,
                    VerticalOffset = channelConfig.VerticalOffset,

                    // Channel configuration
                    Coupling = channelConfig.Coupling,
                    ProbeRatio = channelConfig.ProbeRatio,
                    BandwidthLimit = channelConfig.BandwidthLimit,
                    Units = channelConfig.Units,

                    // Acquisition information
                    MemoryDepth = acquisitionInfo.MemoryDepth,
                    AcquisitionType = acquisitionInfo.AcquisitionType,
                    AverageCount = acquisitionInfo.AverageCount,

                    // Trigger information
                    TriggerMode = triggerInfo.Mode,
                    TriggerSource = triggerInfo.Source,
                    TriggerLevel = triggerInfo.Level,
                    TriggerSlope = triggerInfo.Slope,
                    TriggerCoupling = triggerInfo.Coupling,

                    // Timebase information
                    TimebaseScale = timebaseInfo.Scale,
                    TimebaseOffset = timebaseInfo.Offset,
                    TimebaseMode = timebaseInfo.Mode
                };

                // Validate the captured data
                if (!waveformData.IsValid())
                {
                    Log($"❌ Invalid waveform data captured from {channelName}");
                    return null;
                }

                Log($"✅ {channelName}: {voltageValues.Length:N0} points, {timeValues.Last() - timeValues.First():F6}s duration, {voltageValues.Min():F3}V to {voltageValues.Max():F3}V");
                return waveformData;
            }
            catch (Exception ex)
            {
                Log($"❌ Error capturing {channelNumber}: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region SCPI Configuration Methods

        /// <summary>
        /// Configure waveform acquisition settings
        /// </summary>
        private void ConfigureWaveformSettings(WaveformMode mode, WaveformFormat format)
        {
            try
            {
                // Set waveform mode (affects data resolution and speed)
                string modeCommand = mode switch
                {
                    WaveformMode.NORMal => "NORM",
                    WaveformMode.RAW => "RAW",
                    WaveformMode.MAXimum => "MAX",
                    _ => "NORM"
                };
                SendCommand($":WAVeform:MODE {modeCommand}");

                // Set data format (affects transfer speed and precision)
                SendCommand($":WAVeform:FORMat {format}");

                Log($"🔧 Waveform configured: Mode={modeCommand}, Format={format}");
            }
            catch (Exception ex)
            {
                Log($"❌ Error configuring waveform settings: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Check if a channel is enabled and ready for capture
        /// </summary>
        private bool IsChannelEnabled(int channelNumber)
        {
            try
            {
                string response = SendQuery($":CHANnel{channelNumber}:DISPlay?");
                return response?.Trim() == "1";
            }
            catch (Exception ex)
            {
                Log($"❌ Error checking channel {channelNumber} status: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Data Retrieval Methods

        /// <summary>
        /// Get waveform preamble containing scaling and format information
        /// </summary>
        private Dictionary<string, string> GetWaveformPreamble()
        {
            try
            {
                string preamble = SendQuery(":WAVeform:PREamble?");
                var result = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(preamble))
                {
                    // Preamble format: <format>,<type>,<points>,<count>,<xincrement>,<xorigin>,<xreference>,<yincrement>,<yorigin>,<yreference>
                    string[] parts = preamble.Split(',');
                    if (parts.Length >= 10)
                    {
                        result["format"] = parts[0].Trim();
                        result["type"] = parts[1].Trim();
                        result["points"] = parts[2].Trim();
                        result["count"] = parts[3].Trim();
                        result["xincrement"] = parts[4].Trim();
                        result["xorigin"] = parts[5].Trim();
                        result["xreference"] = parts[6].Trim();
                        result["yincrement"] = parts[7].Trim();
                        result["yorigin"] = parts[8].Trim();
                        result["yreference"] = parts[9].Trim();
                    }
                }

                Log($"📋 Preamble: {parts?.Length ?? 0} parameters");
                return result;
            }
            catch (Exception ex)
            {
                Log($"❌ Error getting waveform preamble: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Get raw waveform data from the oscilloscope
        /// Handles TMC block data format from :WAVeform:DATA? command
        /// </summary>
        private byte[] GetWaveformRawData()
        {
            try
            {
                Log("📡 Requesting waveform data...");

                // Send data query command and get response
                string response = SendQuery(":WAVeform:DATA?", DataCommandTimeoutMs);

                if (string.IsNullOrEmpty(response))
                {
                    Log("❌ Empty waveform data response");
                    return Array.Empty<byte>();
                }

                // Parse TMC (Test & Measurement Class) block data format
                // Format: #<N><LENGTH><DATA>
                // Example: #9000001200<1200 bytes of data>
                if (!response.StartsWith("#"))
                {
                    Log("❌ Invalid waveform data format - missing TMC header");
                    return Array.Empty<byte>();
                }

                // Get the length field width (N)
                if (response.Length < 2 || !char.IsDigit(response[1]))
                {
                    Log("❌ Invalid TMC header format");
                    return Array.Empty<byte>();
                }

                int lengthFieldWidth = int.Parse(response[1].ToString());
                if (response.Length < 2 + lengthFieldWidth)
                {
                    Log("❌ TMC header truncated");
                    return Array.Empty<byte>();
                }

                // Get the data length
                string lengthStr = response.Substring(2, lengthFieldWidth);
                if (!int.TryParse(lengthStr, out int dataLength))
                {
                    Log($"❌ Invalid TMC data length: {lengthStr}");
                    return Array.Empty<byte>();
                }

                // Extract the actual data
                int dataStartIndex = 2 + lengthFieldWidth;
                if (response.Length < dataStartIndex + dataLength)
                {
                    Log($"❌ Data truncated: expected {dataLength} bytes, got {response.Length - dataStartIndex}");
                    // Use what we have rather than failing completely
                    dataLength = response.Length - dataStartIndex;
                }

                // Convert string data to byte array
                // Note: In a real implementation, you'd handle binary data properly
                // This assumes the response contains the actual byte values
                byte[] data = new byte[dataLength];
                for (int i = 0; i < dataLength && i < response.Length - dataStartIndex; i++)
                {
                    data[i] = (byte)response[dataStartIndex + i];
                }

                Log($"📊 Received {dataLength:N0} bytes of waveform data");
                return data;
            }
            catch (Exception ex)
            {
                Log($"❌ Error getting waveform data: {ex.Message}");
                return Array.Empty<byte>();
            }
        }

        #endregion

        #region Parameter Retrieval Methods

        /// <summary>
        /// Get timing parameters from oscilloscope
        /// </summary>
        private (double XIncrement, double XOrigin, double XReference) GetTimingParameters()
        {
            try
            {
                double xIncrement = GetDoubleQuery(":WAVeform:XINCrement?");
                double xOrigin = GetDoubleQuery(":WAVeform:XORigin?");
                double xReference = GetDoubleQuery(":WAVeform:XREFerence?");

                return (xIncrement, xOrigin, xReference);
            }
            catch (Exception ex)
            {
                Log($"❌ Error getting timing parameters: {ex.Message}");
                return (1e-6, 0.0, 0.0); // Default values
            }
        }

        /// <summary>
        /// Get voltage scaling parameters from oscilloscope
        /// </summary>
        private (double YIncrement, double YOrigin, double YReference) GetVoltageParameters()
        {
            try
            {
                double yIncrement = GetDoubleQuery(":WAVeform:YINCrement?");
                double yOrigin = GetDoubleQuery(":WAVeform:YORigin?");
                double yReference = GetDoubleQuery(":WAVeform:YREFerence?");

                return (yIncrement, yOrigin, yReference);
            }
            catch (Exception ex)
            {
                Log($"❌ Error getting voltage parameters: {ex.Message}");
                return (0.001, 0.0, 0.0); // Default values
            }
        }

        /// <summary>
        /// Get channel configuration from oscilloscope
        /// </summary>
        private (double VerticalScale, double VerticalOffset, string Coupling, double ProbeRatio, string BandwidthLimit, string Units) GetChannelConfiguration(int channelNumber)
        {
            try
            {
                double verticalScale = GetDoubleQuery($":CHANnel{channelNumber}:SCALe?");
                double verticalOffset = GetDoubleQuery($":CHANnel{channelNumber}:OFFSet?");
                string coupling = SendQuery($":CHANnel{channelNumber}:COUPling?")?.Trim() ?? "DC";
                double probeRatio = GetDoubleQuery($":CHANnel{channelNumber}:PROBe?");
                string bandwidthLimit = SendQuery($":CHANnel{channelNumber}:BWLimit?")?.Trim() ?? "OFF";
                string units = SendQuery($":CHANnel{channelNumber}:UNITs?")?.Trim() ?? "V";

                return (verticalScale, verticalOffset, coupling, probeRatio, bandwidthLimit, units);
            }
            catch (Exception ex)
            {
                Log($"❌ Error getting channel {channelNumber} configuration: {ex.Message}");
                return (1.0, 0.0, "DC", 1.0, "OFF", "V"); // Default values
            }
        }

        /// <summary>
        /// Get acquisition information from oscilloscope
        /// </summary>
        private (double SampleRate, string MemoryDepth, string AcquisitionType, int AverageCount) GetAcquisitionInformation()
        {
            try
            {
                double sampleRate = GetDoubleQuery(":ACQuire:SRATe?");
                string memoryDepth = SendQuery(":ACQuire:MDEPth?")?.Trim() ?? "AUTO";
                string acquisitionType = SendQuery(":ACQuire:TYPE?")?.Trim() ?? "NORM";
                int averageCount = 0;

                // Only get average count if in average mode
                if (acquisitionType.Equals("AVER", StringComparison.OrdinalIgnoreCase))
                {
                    averageCount = (int)GetDoubleQuery(":ACQuire:AVERages?");
                }

                return (sampleRate, memoryDepth, acquisitionType, averageCount);
            }
            catch (Exception ex)
            {
                Log($"❌ Error getting acquisition information: {ex.Message}");
                return (1e6, "AUTO", "NORM", 0); // Default values
            }
        }

        /// <summary>
        /// Get trigger information from oscilloscope
        /// </summary>
        private (string Mode, string Source, double Level, string Slope, string Coupling) GetTriggerInformation()
        {
            try
            {
                string mode = SendQuery(":TRIGger:MODE?")?.Trim() ?? "EDGE";
                string source = SendQuery(":TRIGger:EDGe:SOURce?")?.Trim() ?? "CHAN1";
                double level = GetDoubleQuery(":TRIGger:EDGe:LEVel?");
                string slope = SendQuery(":TRIGger:EDGe:SLOPe?")?.Trim() ?? "POS";
                string coupling = SendQuery(":TRIGger:COUPling?")?.Trim() ?? "DC";

                return (mode, source, level, slope, coupling);
            }
            catch (Exception ex)
            {
                Log($"❌ Error getting trigger information: {ex.Message}");
                return ("EDGE", "CHAN1", 0.0, "POS", "DC"); // Default values
            }
        }

        /// <summary>
        /// Get timebase information from oscilloscope
        /// </summary>
        private (double Scale, double Offset, string Mode) GetTimebaseInformation()
        {
            try
            {
                double scale = GetDoubleQuery(":TIMebase:SCALe?");
                double offset = GetDoubleQuery(":TIMebase:OFFSet?");
                string mode = SendQuery(":TIMebase:MODE?")?.Trim() ?? "MAIN";

                return (scale, offset, mode);
            }
            catch (Exception ex)
            {
                Log($"❌ Error getting timebase information: {ex.Message}");
                return (1e-3, 0.0, "MAIN"); // Default values
            }
        }

        #endregion

        #region Data Processing Methods

        /// <summary>
        /// Convert raw data bytes to voltage values using oscilloscope scaling
        /// </summary>
        private double[] ConvertRawDataToVoltages(byte[] rawData, (double YIncrement, double YOrigin, double YReference) voltageParams, WaveformFormat format)
        {
            try
            {
                double[] voltages = new double[rawData.Length];

                for (int i = 0; i < rawData.Length; i++)
                {
                    double rawValue = rawData[i];

                    // Handle different data formats
                    switch (format)
                    {
                        case WaveformFormat.BYTE:
                            // 8-bit signed data: -128 to 127 maps to voltage range
                            rawValue = (sbyte)rawData[i];
                            break;

                        case WaveformFormat.WORD:
                            // 16-bit data (need to combine bytes)
                            if (i + 1 < rawData.Length)
                            {
                                rawValue = (short)((rawData[i + 1] << 8) | rawData[i]);
                                i++; // Skip next byte as we used it
                            }
                            break;

                        case WaveformFormat.ASCii:
                            // ASCII format - raw value is already correct
                            break;
                    }

                    // Apply oscilloscope scaling formula: Voltage = (Data - YReference) * YIncrement + YOrigin
                    voltages[i] = (rawValue - voltageParams.YReference) * voltageParams.YIncrement + voltageParams.YOrigin;
                }

                return voltages;
            }
            catch (Exception ex)
            {
                Log($"❌ Error converting raw data to voltages: {ex.Message}");
                return new double[rawData.Length]; // Return array of zeros
            }
        }

        /// <summary>
        /// Generate time values array based on timing parameters
        /// </summary>
        private double[] GenerateTimeValues(int pointCount, (double XIncrement, double XOrigin, double XReference) timingParams)
        {
            try
            {
                double[] timeValues = new double[pointCount];

                for (int i = 0; i < pointCount; i++)
                {
                    // Time = XOrigin + (index - XReference) * XIncrement
                    timeValues[i] = timingParams.XOrigin + (i - timingParams.XReference) * timingParams.XIncrement;
                }

                return timeValues;
            }
            catch (Exception ex)
            {
                Log($"❌ Error generating time values: {ex.Message}");

                // Return basic time array as fallback
                double[] fallbackTimes = new double[pointCount];
                for (int i = 0; i < pointCount; i++)
                {
                    fallbackTimes[i] = i * 1e-6; // 1μs per point fallback
                }
                return fallbackTimes;
            }
        }

        #endregion

        #region Memory Management Methods

        /// <summary>
        /// Store a waveform in memory with automatic limit management
        /// </summary>
        private void StoreWaveform(WaveformData waveform)
        {
            try
            {
                storedWaveforms.Add(waveform);

                // Manage memory limit by removing oldest waveforms
                while (storedWaveforms.Count > maxStoredWaveforms)
                {
                    var oldest = storedWaveforms.OrderBy(w => w.CaptureTime).First();
                    storedWaveforms.Remove(oldest);
                    Log($"📦 Memory limit reached: removed oldest waveform (limit: {maxStoredWaveforms})");
                }

                Log($"💾 Stored waveform: {waveform.ChannelName} ({storedWaveforms.Count}/{maxStoredWaveforms})");
            }
            catch (Exception ex)
            {
                Log($"❌ Error storing waveform: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all stored waveforms (thread-safe)
        /// </summary>
        public List<WaveformData> GetStoredWaveforms()
        {
            lock (waveformLock)
            {
                return new List<WaveformData>(storedWaveforms);
            }
        }

        /// <summary>
        /// Get stored waveforms for a specific channel
        /// </summary>
        public List<WaveformData> GetStoredWaveforms(string channelName)
        {
            lock (waveformLock)
            {
                return storedWaveforms
                    .Where(w => w.ChannelName.Equals(channelName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        /// <summary>
        /// Clear all stored waveforms
        /// </summary>
        public void ClearMemory()
        {
            lock (waveformLock)
            {
                int count = storedWaveforms.Count;
                storedWaveforms.Clear();
                Log($"🗑️ Memory cleared: {count} waveforms removed");
                MemoryCleared?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Set the maximum number of stored waveforms
        /// </summary>
        public void SetMemoryLimit(int limit)
        {
            if (limit < 1) limit = 1;

            maxStoredWaveforms = limit;

            lock (waveformLock)
            {
                // Remove excess waveforms if new limit is lower
                while (storedWaveforms.Count > maxStoredWaveforms)
                {
                    var oldest = storedWaveforms.OrderBy(w => w.CaptureTime).First();
                    storedWaveforms.Remove(oldest);
                }
            }

            Log($"📏 Memory limit set to {maxStoredWaveforms} waveforms");
        }

        /// <summary>
        /// Get the current memory limit
        /// </summary>
        public int GetMemoryLimit()
        {
            return maxStoredWaveforms;
        }

        #endregion

        #region Export Methods

        /// <summary>
        /// Export a waveform to CSV file with full metadata
        /// </summary>
        public bool ExportToCSV(WaveformData waveform, string filePath)
        {
            try
            {
                Log($"📤 Exporting waveform to: {filePath}");

                var csv = new StringBuilder();

                // CSV Header with metadata
                csv.AppendLine("# Rigol DS1000Z-E Waveform Data Export");
                csv.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                csv.AppendLine("#");
                csv.AppendLine("# === Waveform Information ===");
                csv.AppendLine($"# Channel: {waveform.ChannelName}");
                csv.AppendLine($"# Capture Time: {waveform.CaptureTime:yyyy-MM-dd HH:mm:ss}");
                csv.AppendLine($"# Waveform ID: {waveform.Id}");
                csv.AppendLine($"# Capture Mode: {waveform.CaptureMode}");
                csv.AppendLine($"# Data Format: {waveform.DataFormat}");
                csv.AppendLine("#");
                csv.AppendLine("# === Timing Information ===");
                csv.AppendLine($"# Sample Rate: {waveform.SampleRate:E3} Sa/s");
                csv.AppendLine($"# Time Increment: {waveform.TimeIncrement:E6} s");
                csv.AppendLine($"# Duration: {waveform.Duration:F6} s");
                csv.AppendLine($"# Data Points: {waveform.PointCount:N0}");
                csv.AppendLine("#");
                csv.AppendLine("# === Voltage Information ===");
                csv.AppendLine($"# Vertical Scale: {waveform.VerticalScale:F6} V/div");
                csv.AppendLine($"# Vertical Offset: {waveform.VerticalOffset:F6} V");
                csv.AppendLine($"# Voltage Range: {waveform.MinVoltage:F6} V to {waveform.MaxVoltage:F6} V");
                csv.AppendLine($"# Peak-to-Peak: {waveform.PeakToPeak:F6} V");
                csv.AppendLine($"# RMS: {waveform.RmsVoltage:F6} V");
                csv.AppendLine($"# Average: {waveform.AverageVoltage:F6} V");
                csv.AppendLine("#");
                csv.AppendLine("# === Channel Configuration ===");
                csv.AppendLine($"# Coupling: {waveform.Coupling}");
                csv.AppendLine($"# Probe Ratio: {waveform.ProbeRatio}X");
                csv.AppendLine($"# Bandwidth Limit: {waveform.BandwidthLimit}");
                csv.AppendLine($"# Units: {waveform.Units}");
                csv.AppendLine("#");
                csv.AppendLine("# === Trigger Information ===");
                csv.AppendLine($"# Mode: {waveform.TriggerMode}");
                csv.AppendLine($"# Source: {waveform.TriggerSource}");
                csv.AppendLine($"# Level: {waveform.TriggerLevel:F6} V");
                csv.AppendLine($"# Slope: {waveform.TriggerSlope}");
                csv.AppendLine($"# Coupling: {waveform.TriggerCoupling}");
                csv.AppendLine("#");
                csv.AppendLine("# === Timebase Information ===");
                csv.AppendLine($"# Scale: {waveform.TimebaseScale:E3} s/div");
                csv.AppendLine($"# Offset: {waveform.TimebaseOffset:E6} s");
                csv.AppendLine($"# Mode: {waveform.TimebaseMode}");
                csv.AppendLine("#");
                csv.AppendLine("# === Acquisition Information ===");
                csv.AppendLine($"# Type: {waveform.AcquisitionType}");
                csv.AppendLine($"# Memory Depth: {waveform.MemoryDepth}");
                if (waveform.AverageCount > 0)
                    csv.AppendLine($"# Average Count: {waveform.AverageCount}");
                csv.AppendLine("#");
                csv.AppendLine("# === Data Columns ===");
                csv.AppendLine("# Time (s), Voltage (V)");

                // Data section
                for (int i = 0; i < waveform.TimeValues.Length; i++)
                {
                    csv.AppendLine($"{waveform.TimeValues[i]:E6},{waveform.VoltageValues[i]:E6}");
                }

                // Write to file
                File.WriteAllText(filePath, csv.ToString());

                Log($"✅ Export completed: {waveform.PointCount:N0} points written to {Path.GetFileName(filePath)}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"❌ Export error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Send a command to the oscilloscope
        /// </summary>
        private bool SendCommand(string command)
        {
            try
            {
                return oscilloscope.SendCommand(command);
            }
            catch (Exception ex)
            {
                Log($"❌ Command error '{command}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a query to the oscilloscope and get response
        /// </summary>
        private string SendQuery(string query, int timeoutMs = 5000)
        {
            try
            {
                return oscilloscope.SendQuery(query);
            }
            catch (Exception ex)
            {
                Log($"❌ Query error '{query}': {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Send a query and parse the response as a double value
        /// </summary>
        private double GetDoubleQuery(string query)
        {
            try
            {
                string response = SendQuery(query);
                if (double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    return value;

                Log($"⚠️ Invalid numeric response for '{query}': {response}");
                return 0.0;
            }
            catch (Exception ex)
            {
                Log($"❌ Double query error '{query}': {ex.Message}");
                return 0.0;
            }
        }

        #endregion

        #region Logging

        /// <summary>
        /// Log a message through the event system
        /// </summary>
        private void Log(string message)
        {
            LogEvent?.Invoke(this, $"[Capture] {message}");
        }

        #endregion
    }
}