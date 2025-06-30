using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Rigol_DS1000Z_E_Control
{
    public class VisaManager : IDisposable
    {
        private IntPtr resourceManagerHandle = IntPtr.Zero;
        private IntPtr instrumentHandle = IntPtr.Zero;
        private bool isConnected = false;

        // VISA Constants
        private const int VI_SUCCESS = 0;
        private const int VI_TMO_IMMEDIATE = 0;
        private const int VI_GPIB_REN_DEASSERT_GTL = 2;

        #region VISA P/Invoke Declarations

        [DllImport("visa32.dll")]
        private static extern int viOpenDefaultRM(out IntPtr sesn);

        [DllImport("visa32.dll")]
        private static extern int viOpen(IntPtr sesn, string rsrcName, int accessMode, int openTimeout, out IntPtr vi);

        [DllImport("visa32.dll")]
        private static extern int viClose(IntPtr vi);

        [DllImport("visa32.dll")]
        private static extern int viWrite(IntPtr vi, byte[] buf, int count, out int retCount);

        [DllImport("visa32.dll")]
        private static extern int viRead(IntPtr vi, byte[] buf, int count, out int retCount);

        [DllImport("visa32.dll")]
        private static extern int viGpibControlREN(IntPtr vi, int mode);

        [DllImport("visa32.dll")]
        private static extern int viFindRsrc(IntPtr sesn, string expr, out IntPtr findList, out int retcnt, StringBuilder desc);

        [DllImport("visa32.dll")]
        private static extern int viFindNext(IntPtr findList, StringBuilder desc);

        #endregion

        public event EventHandler<string> LogEvent;

        public bool IsConnected => isConnected;

        public bool Connect(string resourceName)
        {
            try
            {
                // Open the resource manager
                int status = viOpenDefaultRM(out resourceManagerHandle);
                if (status != VI_SUCCESS)
                {
                    Log($"Failed to open VISA resource manager. Error code: {status}");
                    Log("Make sure VISA runtime is installed (NI-VISA or Keysight IO Libraries)");
                    return false;
                }

                // Open the instrument
                status = viOpen(resourceManagerHandle, resourceName, 0, VI_TMO_IMMEDIATE, out instrumentHandle);
                if (status != VI_SUCCESS)
                {
                    Log($"Failed to open instrument at {resourceName}. Error code: {status}");
                    viClose(resourceManagerHandle);
                    resourceManagerHandle = IntPtr.Zero;
                    return false;
                }

                isConnected = true;
                Log($"Successfully connected to {resourceName}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Connection error: {ex.Message}");
                return false;
            }
        }

        public bool Disconnect()
        {
            try
            {
                if (instrumentHandle != IntPtr.Zero)
                {
                    // Return to local mode
                    viGpibControlREN(instrumentHandle, VI_GPIB_REN_DEASSERT_GTL);

                    // Close instrument
                    int status = viClose(instrumentHandle);
                    instrumentHandle = IntPtr.Zero;

                    if (status != VI_SUCCESS)
                    {
                        Log($"Warning: Instrument close returned error code: {status}");
                    }
                }

                if (resourceManagerHandle != IntPtr.Zero)
                {
                    viClose(resourceManagerHandle);
                    resourceManagerHandle = IntPtr.Zero;
                }

                isConnected = false;
                Log("Disconnected successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Disconnection error: {ex.Message}");
                return false;
            }
        }

        public bool SendCommand(string command)
        {
            if (!isConnected || instrumentHandle == IntPtr.Zero)
            {
                Log("Cannot send command - not connected");
                return false;
            }

            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(command + "\n");
                int retCount = 0;
                int status = viWrite(instrumentHandle, buffer, buffer.Length, out retCount);

                if (status != VI_SUCCESS)
                {
                    Log($"Write failed. Error code: {status}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log($"Send command error: {ex.Message}");
                return false;
            }
        }

        public string SendQuery(string query)
        {
            if (!SendCommand(query))
            {
                return string.Empty;
            }

            try
            {
                byte[] readBuffer = new byte[1024];
                int retReadCount = 0;
                int status = viRead(instrumentHandle, readBuffer, readBuffer.Length, out retReadCount);

                if (status != VI_SUCCESS)
                {
                    Log($"Read failed. Error code: {status}");
                    return string.Empty;
                }

                string response = Encoding.ASCII.GetString(readBuffer, 0, retReadCount).Trim();
                return response;
            }
            catch (Exception ex)
            {
                Log($"Query error: {ex.Message}");
                return string.Empty;
            }
        }

        public List<string> FindResources()
        {
            List<string> resources = new List<string>();
            IntPtr findList = IntPtr.Zero;

            try
            {
                if (resourceManagerHandle == IntPtr.Zero)
                {
                    int rmStatus = viOpenDefaultRM(out resourceManagerHandle);
                    if (rmStatus != VI_SUCCESS)
                    {
                        return resources;
                    }
                }

                StringBuilder desc = new StringBuilder(256);
                int retcnt = 0;
                int findStatus = viFindRsrc(resourceManagerHandle, "USB?*", out findList, out retcnt, desc);

                if (findStatus == VI_SUCCESS && retcnt > 0)
                {
                    resources.Add(desc.ToString());

                    for (int i = 1; i < retcnt; i++)
                    {
                        if (viFindNext(findList, desc) == VI_SUCCESS)
                        {
                            resources.Add(desc.ToString());
                        }
                    }
                }

                if (findList != IntPtr.Zero)
                {
                    viClose(findList);
                }

                return resources;
            }
            catch
            {
                return resources;
            }
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }

        public void Dispose()
        {
            Disconnect();
        }


        // Add this method to your VisaManager.cs class

        public byte[] SendBinaryQuery(string query, int maxBufferSize = 100000)
        {
            if (!SendCommand(query))
            {
                return new byte[0];
            }

            try
            {
                // Use larger buffer for waveform data
                byte[] readBuffer = new byte[maxBufferSize];
                int retReadCount = 0;
                int status = viRead(instrumentHandle, readBuffer, readBuffer.Length, out retReadCount);

                if (status != VI_SUCCESS)
                {
                    Log($"Binary read failed. Error code: {status}");
                    return new byte[0];
                }

                // Return only the actual data received
                byte[] actualData = new byte[retReadCount];
                Array.Copy(readBuffer, actualData, retReadCount);

                Log($"Received {retReadCount} bytes of binary data");
                return actualData;
            }
            catch (Exception ex)
            {
                Log($"Binary query error: {ex.Message}");
                return new byte[0];
            }
        }

    }
}