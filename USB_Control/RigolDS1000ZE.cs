using DS1000Z_E_USB_Control;
using System;

namespace Rigol_DS1000Z_E_Control
{
    public class RigolDS1000ZE
    {
        private readonly VisaManager visaManager;

        // Update this with your oscilloscope's actual USB identifier
        // You can find it using NI MAX or Keysight Connection Expert
        // private const string DefaultResourceName = "USB0::0x1AB1::0x04CE::DS1ZE12345678::INSTR";
        private const string DefaultResourceName = "USB0::0x1AB1::0x0517::DS1ZE213800586::INSTR";
        public event EventHandler<string> LogEvent;

        public RigolDS1000ZE()
        {
            visaManager = new VisaManager();
            visaManager.LogEvent += (sender, message) => LogEvent?.Invoke(this, message);
        }

        public bool IsConnected => visaManager.IsConnected;

        public bool Connect()
        {
            // First try the default resource name
            bool result = visaManager.Connect(DefaultResourceName);

            if (!result)
            {
                // If that fails, try to find any Rigol DS1000Z-E device
                Log("Default resource not found. Searching for Rigol oscilloscopes...");
                var resources = visaManager.FindResources();

                foreach (var resource in resources)
                {
                    if (resource.Contains("0x1AB1") && resource.Contains("0x04CE"))
                    {
                        Log($"Found Rigol oscilloscope: {resource}");
                        result = visaManager.Connect(resource);
                        if (result) break;
                    }
                }
            }

            return result;
        }



        public byte[] SendBinaryQuery(string query, int maxBufferSize = 100000)
        {
            if (visaManager == null)
            {
                return new byte[0];
            }

            return visaManager.SendBinaryQuery(query, maxBufferSize);
        }

        public bool Disconnect()
        {
            return visaManager.Disconnect();
        }

        public bool SendCommand(string command)
        {
            return visaManager.SendCommand(command);
        }

        public string SendQuery(string query)
        {
            return visaManager.SendQuery(query);
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}