// File: Measurements/MeasurementEventArgs.cs
using System;

namespace DS1000Z_E_USB_Control.Measurements
{
    /// <summary>
    /// Event arguments for measurement value updates
    /// </summary>
    public class MeasurementValueEventArgs : EventArgs
    {
        /// <summary>
        /// The measurement key/type (e.g., "VMAX", "FREQ", etc.)
        /// </summary>
        public string MeasurementKey { get; set; }

        /// <summary>
        /// The measurement value
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Optional unit of measurement
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Timestamp when the measurement was taken
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Constructor for MeasurementValueEventArgs
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <param name="value">The measurement value</param>
        /// <param name="unit">Optional unit</param>
        public MeasurementValueEventArgs(string measurementKey, double value, string unit = null)
        {
            MeasurementKey = measurementKey;
            Value = value;
            Unit = unit;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Event arguments for measurement statistics updates
    /// </summary>
    public class MeasurementStatisticsEventArgs : EventArgs
    {
        /// <summary>
        /// The measurement key/type (e.g., "VMAX", "FREQ", etc.)
        /// </summary>
        public string MeasurementKey { get; set; }

        /// <summary>
        /// The measurement statistics
        /// </summary>
        public MeasurementStatistics Statistics { get; set; }

        /// <summary>
        /// Timestamp when the statistics were updated
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Constructor for MeasurementStatisticsEventArgs
        /// </summary>
        /// <param name="measurementKey">The measurement key</param>
        /// <param name="statistics">The measurement statistics</param>
        public MeasurementStatisticsEventArgs(string measurementKey, MeasurementStatistics statistics)
        {
            MeasurementKey = measurementKey;
            Statistics = statistics;
            Timestamp = DateTime.Now;
        }
    }
}