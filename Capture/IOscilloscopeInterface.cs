using System;

namespace OscilloscopeControl.Capture
{
    /// <summary>
    /// Interface that defines the contract for oscilloscope communication.
    /// This allows the capture system to work with different oscilloscope implementations.
    /// </summary>
    public interface IOscilloscopeInterface
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether the oscilloscope is currently connected
        /// </summary>
        bool IsConnected { get; }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when a log message is generated
        /// </summary>
        event EventHandler<string> LogEvent;

        #endregion

        #region Methods

        /// <summary>
        /// Send a SCPI command to the oscilloscope
        /// </summary>
        /// <param name="command">The SCPI command to send</param>
        /// <returns>True if successful, false otherwise</returns>
        bool SendCommand(string command);

        /// <summary>
        /// Send a SCPI query to the oscilloscope and return the response
        /// </summary>
        /// <param name="query">The SCPI query to send</param>
        /// <returns>The response string, or null/empty if failed</returns>
        string SendQuery(string query);

        #endregion
    }
}