// ============================================================================
// File: Mathematics/MathematicsEventArgs.cs - NO CHANGES
// ============================================================================
using System;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Event arguments for SCPI commands
    /// </summary>
    public class SCPICommandEventArgs : EventArgs
    {
        public string Command { get; }
        public string Source { get; }
        public string Category { get; }
        public DateTime Timestamp { get; }

        public SCPICommandEventArgs(string command, string source = "", string category = "")
        {
            Command = command ?? string.Empty;
            Source = source ?? string.Empty;
            Category = category ?? string.Empty;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Event arguments for errors
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public string Error { get; }
        public string Source { get; set; }
        public string Category { get; set; }
        public ErrorSeverity Severity { get; set; }

        public ErrorEventArgs(string error)
        {
            Error = error ?? string.Empty;
            Severity = ErrorSeverity.Error;
        }
    }

    /// <summary>
    /// Event arguments for status updates
    /// </summary>
    public class StatusEventArgs : EventArgs
    {
        public string Message { get; }
        public StatusLevel Level { get; }
        public string Source { get; set; }
        public string Category { get; set; }

        public StatusEventArgs(string message, StatusLevel level = StatusLevel.Info, string source = "", string category = "")
        {
            Message = message ?? string.Empty;
            Level = level;
            Source = source ?? string.Empty;
            Category = category ?? string.Empty;
        }
    }

    /// <summary>
    /// Error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Status levels
    /// </summary>
    public enum StatusLevel
    {
        Info,
        Warning,
        Success,
        Error
    }
}
