namespace DS1000Z_E_USB_Control.Measurements
{
    /// <summary>  
    /// Represents statistics for a measurement.  
    /// </summary>  
    public class MeasurementStatistics
    {
        /// <summary>  
        /// The key identifying the measurement.  
        /// </summary>  
        public string MeasurementKey { get; set; }

        /// <summary>  
        /// The current value of the measurement.  
        /// </summary>  
        public double Current { get; set; }

        /// <summary>  
        /// The average value of the measurement.  
        /// </summary>  
        public double Average { get; set; }

        /// <summary>  
        /// The minimum value of the measurement.  
        /// </summary>  
        public double Minimum { get; set; }

        /// <summary>  
        /// The maximum value of the measurement.  
        /// </summary>  
        public double Maximum { get; set; }

        /// <summary>  
        /// The standard deviation of the measurement.  
        /// </summary>  
        public double StandardDeviation { get; set; }

        /// <summary>  
        /// The count of measurement samples.  
        /// </summary>  
        public int Count { get; set; }
    }
}
