// ============================================================================
// File: Mathematics/MathematicsSettings.cs - NO CHANGES NEEDED
// ============================================================================
using System;
using System.Collections.Generic;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Mathematics configuration settings
    /// </summary>
    public class MathematicsSettings
    {
        public string ActiveMode { get; set; } = "BasicOperations";
        public string ConfigurationName { get; set; } = "Default";
        public DateTime LastModified { get; set; } = DateTime.Now;

        // Basic Operations
        public string Operation { get; set; } = "ADD";
        public string Source1 { get; set; } = "CHANnel1";
        public string Source2 { get; set; } = "CHANnel2";

        // FFT Analysis
        public string FFTSource { get; set; } = "CHANnel1";
        public string FFTWindow { get; set; } = "HANNing";
        public string FFTSplit { get; set; } = "FULL";
        public string FFTUnit { get; set; } = "VRMS";

        // Digital Filters
        public string FilterType { get; set; } = "LPASs";
        public string FilterW1 { get; set; } = "1000";
        public string FilterW2 { get; set; } = "10000";

        // Advanced Math
        public string AdvancedFunction { get; set; } = "INTG";
        public string StartPoint { get; set; } = "0";
        public string EndPoint { get; set; } = "100";

        // Display Settings
        public bool DisplayEnabled { get; set; } = true;
        public bool InvertEnabled { get; set; } = false;
        public string Scale { get; set; } = "1.0";
        public string Offset { get; set; } = "0.0";
    }
}