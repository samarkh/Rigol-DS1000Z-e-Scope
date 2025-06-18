using System;
using Rigol_DS1000Z_E_Control;

namespace OscilloscopeControl.Capture
{
    /// <summary>
    /// Adapter class that implements IOscilloscopeInterface and wraps the existing RigolDS1000ZE class.
    /// This allows the capture system to work with your existing oscilloscope communication code.
    /// </summary>
    public class OscilloscopeAdapter : IOscilloscopeInterface
    {
        #region Private Fields

        private readonly RigolDS1000ZE rigolOscilloscope;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize the adapter with an existing RigolDS1000ZE instance
        /// </summary>
        /// <param name="rigolOscilloscope">Your existing oscilloscope instance</param>
        public OscilloscopeAdapter(RigolDS1000ZE rigolOscilloscope)
        {
            this.rigolOscilloscope = rigolOscilloscope ?? throw new ArgumentNullException(nameof(rigolOscilloscope));

            // Forward the log events from the wrapped oscilloscope
            this.rigolOscilloscope.LogEvent += (sender, message) => LogEvent?.Invoke(this, message);
        }

        #endregion

        #region IOscilloscopeInterface Implementation

        /// <summary>
        /// Gets a value indicating whether the oscilloscope is currently connected
        /// </summary>
        public bool IsConnected => rigolOscilloscope.IsConnected;

        /// <summary>
        /// Send a SCPI command to the oscilloscope
        /// </summary>
        /// <param name="command">The SCPI command to send</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SendCommand(string command)
        {
            try
            {
                return rigolOscilloscope.SendCommand(command);
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error sending command '{command}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a SCPI query to the oscilloscope and return the response
        /// </summary>
        /// <param name="query">The SCPI query to send</param>
        /// <returns>The response string, or empty if failed</returns>
        public string SendQuery(string query)
        {
            try
            {
                return rigolOscilloscope.SendQuery(query) ?? string.Empty;
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error sending query '{query}': {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Connect to the oscilloscope
        /// </summary>
        /// <returns>True if connection successful</returns>
        public bool Connect()
        {
            try
            {
                return rigolOscilloscope.Connect();
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error connecting: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the oscilloscope
        /// </summary>
        /// <returns>True if disconnection successful</returns>
        public bool Disconnect()
        {
            try
            {
                return rigolOscilloscope.Disconnect();
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error disconnecting: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Event raised when logging messages are generated
        /// </summary>
        public event EventHandler<string> LogEvent;

        #endregion

        #region Additional Properties

        /// <summary>
        /// Get access to the underlying RigolDS1000ZE instance for advanced operations
        /// </summary>
        public RigolDS1000ZE UnderlyingOscilloscope => rigolOscilloscope;

        #endregion
    }
}