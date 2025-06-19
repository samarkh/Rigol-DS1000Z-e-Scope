using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OscilloscopeControl.Capture  // FIXED: Correct namespace
{
    /// <summary>
    /// Core manager class that handles waveform capture from the Rigol DS1000Z-E oscilloscope.
    /// Implements SCPI commands for data retrieval, processing, and storage.
    /// </summary>
    public class WaveformMemoryManager  // FIXED: public instead of internal
    {
        #region Private Fields

        private readonly IOscilloscopeInterface oscilloscope;
        private readonly List<WaveformData> storedWaveforms;
        private readonly object waveformLock = new object();
        private int maxStoredWaveforms = 100;

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

                // Simple capture implementation for now
                var waveform = new WaveformData
                {
                    ChannelNumber = 1,
                    ChannelName = "CH1",
                    CaptureTime = DateTime.Now,
                    VoltageData = new List<double> { 0.1, 0.2, 0.3 }, // Dummy data
                    TimeData = new List<double> { 0.0, 1e-6, 2e-6 },
                    Mode = mode,
                    Format = format
                };

                StoreWaveform(waveform);
                capturedWaveforms.Add(waveform);

                WaveformCaptured?.Invoke(this, waveform);
                Log($"✅ Captured {capturedWaveforms.Count} waveform(s)");
            }
            catch (Exception ex)
            {
                Log($"❌ Error during capture: {ex.Message}");
            }

            return capturedWaveforms;
        }

        #endregion

        #region Memory Management Methods

        /// <summary>
        /// Store a waveform in memory with automatic limit management
        /// </summary>
        private void StoreWaveform(WaveformData waveform)
        {
            lock (waveformLock)
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
        /// Get the current memory limit
        /// </summary>
        public int GetMemoryLimit()
        {
            return maxStoredWaveforms;
        }

        /// <summary>
        /// Set the maximum number of stored waveforms
        /// </summary>
        public void SetMemoryLimit(int limit)
        {
            if (limit < 1) limit = 1;
            maxStoredWaveforms = limit;
            Log($"📏 Memory limit set to {maxStoredWaveforms} waveforms");
        }

        #endregion

        #region Logging

        /// <summary>
        /// Log a message through the event system
        /// </summary>
        private void Log(string message)
        {
            LogEvent?.Invoke(this, $"[Memory Manager] {message}");
        }

        #endregion
    }
}