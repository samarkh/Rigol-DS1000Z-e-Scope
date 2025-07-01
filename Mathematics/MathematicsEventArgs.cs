using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// File: Mathematics/MathematicsEventArgs.cs
// Create this new file to contain the missing EventArgs classes

namespace DS1000Z_E_USB_Control.Mathematics
{
    #region SCPICommandEventArgs

    /// <summary>
    /// Event arguments for SCPI command generation events
    /// Contains the SCPI command that was generated
    /// </summary>
    public class SCPICommandEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// The SCPI command string that was generated
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Timestamp when the command was generated
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Optional source/origin of the command
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Command type or category (e.g., "MATH", "MEASUREMENT", "TRIGGER")
        /// </summary>
        public string CommandType { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public SCPICommandEventArgs()
        {
            Command = string.Empty;
            Timestamp = DateTime.Now;
            Source = string.Empty;
            CommandType = string.Empty;
        }

        /// <summary>
        /// Constructor with command
        /// </summary>
        /// <param name="command">The SCPI command</param>
        public SCPICommandEventArgs(string command)
        {
            Command = command ?? string.Empty;
            Timestamp = DateTime.Now;
            Source = string.Empty;
            CommandType = string.Empty;
        }

        /// <summary>
        /// Constructor with command and source
        /// </summary>
        /// <param name="command">The SCPI command</param>
        /// <param name="source">The source/origin of the command</param>
        public SCPICommandEventArgs(string command, string source)
        {
            Command = command ?? string.Empty;
            Timestamp = DateTime.Now;
            Source = source ?? string.Empty;
            CommandType = string.Empty;
        }

        /// <summary>
        /// Full constructor
        /// </summary>
        /// <param name="command">The SCPI command</param>
        /// <param name="source">The source/origin of the command</param>
        /// <param name="commandType">The type/category of command</param>
        public SCPICommandEventArgs(string command, string source, string commandType)
        {
            Command = command ?? string.Empty;
            Timestamp = DateTime.Now;
            Source = source ?? string.Empty;
            CommandType = commandType ?? string.Empty;
        }

        #endregion

        #region Methods

        /// <summary>
        /// String representation of the event args
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return string.IsNullOrEmpty(Source)
                ? $"SCPI Command: {Command}"
                : $"SCPI Command from {Source}: {Command}";
        }

        /// <summary>
        /// Get detailed string representation for debugging
        /// </summary>
        /// <returns>Detailed string representation</returns>
        public string ToDetailedString()
        {
            return $"SCPICommandEventArgs: Command='{Command}', Source='{Source}', " +
                   $"Type='{CommandType}', Time={Timestamp:HH:mm:ss.fff}";
        }

        #endregion
    }

    #endregion

    #region StatusEventArgs

    /// <summary>
    /// Event arguments for status update events
    /// Contains status messages and related information
    /// </summary>
    public class StatusEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// The status message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Timestamp when the status was updated
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Status level or priority
        /// </summary>
        public StatusLevel Level { get; set; }

        /// <summary>
        /// Optional source/origin of the status
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Optional status category
        /// </summary>
        public string Category { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public StatusEventArgs()
        {
            Message = string.Empty;
            Timestamp = DateTime.Now;
            Level = StatusLevel.Info;
            Source = string.Empty;
            Category = string.Empty;
        }

        /// <summary>
        /// Constructor with message
        /// </summary>
        /// <param name="message">The status message</param>
        public StatusEventArgs(string message)
        {
            Message = message ?? string.Empty;
            Timestamp = DateTime.Now;
            Level = StatusLevel.Info;
            Source = string.Empty;
            Category = string.Empty;
        }

        /// <summary>
        /// Constructor with message and level
        /// </summary>
        /// <param name="message">The status message</param>
        /// <param name="level">The status level</param>
        public StatusEventArgs(string message, StatusLevel level)
        {
            Message = message ?? string.Empty;
            Timestamp = DateTime.Now;
            Level = level;
            Source = string.Empty;
            Category = string.Empty;
        }

        /// <summary>
        /// Constructor with message, level, and source
        /// </summary>
        /// <param name="message">The status message</param>
        /// <param name="level">The status level</param>
        /// <param name="source">The source/origin of the status</param>
        public StatusEventArgs(string message, StatusLevel level, string source)
        {
            Message = message ?? string.Empty;
            Timestamp = DateTime.Now;
            Level = level;
            Source = source ?? string.Empty;
            Category = string.Empty;
        }

        /// <summary>
        /// Full constructor
        /// </summary>
        /// <param name="message">The status message</param>
        /// <param name="level">The status level</param>
        /// <param name="source">The source/origin of the status</param>
        /// <param name="category">The status category</param>
        public StatusEventArgs(string message, StatusLevel level, string source, string category)
        {
            Message = message ?? string.Empty;
            Timestamp = DateTime.Now;
            Level = level;
            Source = source ?? string.Empty;
            Category = category ?? string.Empty;
        }

        #endregion

        #region Methods

        /// <summary>
        /// String representation of the event args
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return string.IsNullOrEmpty(Source)
                ? $"Status: {Message}"
                : $"Status from {Source}: {Message}";
        }

        /// <summary>
        /// Get detailed string representation for debugging
        /// </summary>
        /// <returns>Detailed string representation</returns>
        public string ToDetailedString()
        {
            return $"StatusEventArgs: Message='{Message}', Level={Level}, " +
                   $"Source='{Source}', Category='{Category}', Time={Timestamp:HH:mm:ss.fff}";
        }

        #endregion
    }

    #endregion

    #region Supporting Enumerations

    /// <summary>
    /// Status levels for status events
    /// </summary>
    public enum StatusLevel
    {
        /// <summary>
        /// Debug level - detailed diagnostic information
        /// </summary>
        Debug,

        /// <summary>
        /// Information level - general information
        /// </summary>
        Info,

        /// <summary>
        /// Warning level - potential problems
        /// </summary>
        Warning,

        /// <summary>
        /// Error level - error conditions
        /// </summary>
        Error,

        /// <summary>
        /// Critical level - critical errors
        /// </summary>
        Critical
    }

    #endregion


    #region ErrorEventArgs

    /// <summary>
    /// Event arguments for error events
    /// Contains error information and related details
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// The error message - matches existing code expectations
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Alternative property name for compatibility
        /// </summary>
        public string Message => Error;

        /// <summary>
        /// Timestamp when the error occurred
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Optional exception that caused the error
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Optional source/origin of the error
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Error severity level
        /// </summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>
        /// Error category
        /// </summary>
        public string Category { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public ErrorEventArgs()
        {
            Error = string.Empty;
            Timestamp = DateTime.Now;
            Exception = null;
            Source = string.Empty;
            Severity = ErrorSeverity.Error;
            Category = string.Empty;
        }

        /// <summary>
        /// Constructor with error message
        /// </summary>
        /// <param name="error">The error message</param>
        public ErrorEventArgs(string error)
        {
            Error = error ?? string.Empty;
            Timestamp = DateTime.Now;
            Exception = null;
            Source = string.Empty;
            Severity = ErrorSeverity.Error;
            Category = string.Empty;
        }

        /// <summary>
        /// Constructor with error message and exception
        /// </summary>
        /// <param name="error">The error message</param>
        /// <param name="exception">The exception that caused the error</param>
        public ErrorEventArgs(string error, Exception exception)
        {
            Error = error ?? string.Empty;
            Timestamp = DateTime.Now;
            Exception = exception;
            Source = string.Empty;
            Severity = ErrorSeverity.Error;
            Category = string.Empty;
        }

        /// <summary>
        /// Full constructor
        /// </summary>
        /// <param name="error">The error message</param>
        /// <param name="exception">The exception that caused the error</param>
        /// <param name="source">The source/origin of the error</param>
        /// <param name="severity">The error severity</param>
        /// <param name="category">The error category</param>
        public ErrorEventArgs(string error, Exception exception, string source,
                            ErrorSeverity severity, string category)
        {
            Error = error ?? string.Empty;
            Timestamp = DateTime.Now;
            Exception = exception;
            Source = source ?? string.Empty;
            Severity = severity;
            Category = category ?? string.Empty;
        }

        #endregion

        #region Methods

        /// <summary>
        /// String representation of the error event args
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return string.IsNullOrEmpty(Source)
                ? $"Error: {Error}"
                : $"Error from {Source}: {Error}";
        }

        /// <summary>
        /// Get detailed string representation for debugging
        /// </summary>
        /// <returns>Detailed string representation</returns>
        public string ToDetailedString()
        {
            return $"ErrorEventArgs: Error='{Error}', Severity={Severity}, " +
                   $"Source='{Source}', Category='{Category}', Time={Timestamp:HH:mm:ss.fff}, " +
                   $"Exception={Exception?.GetType().Name ?? "null"}";
        }

        #endregion
    }

    #endregion

    #region Supporting Enumerations

    /// <summary>
    /// Error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// Low severity - minor issues
        /// </summary>
        Low,

        /// <summary>
        /// Medium severity - moderate issues
        /// </summary>
        Medium,

        /// <summary>
        /// High severity - serious issues
        /// </summary>
        High,

        /// <summary>
        /// Error severity - standard errors
        /// </summary>
        Error,

        /// <summary>
        /// Critical severity - system-critical errors
        /// </summary>
        Critical
    }



    #endregion


    
       
}
