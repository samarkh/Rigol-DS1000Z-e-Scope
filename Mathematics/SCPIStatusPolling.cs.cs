using Rigol_DS1000Z_E_Control;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// SCPI status polling utilities for reliable mathematics panel switching
    /// Replaces fixed delays with proper operation completion detection for Rigol DS1000Z-E
    /// 
    /// This class implements the exact SCPI sequence discovered by the user:
    /// :MATH:DISPlay OFF; :MATH:RESet (150ms delay) 
    /// :MATH:DISPlay ON; :MATH:OPERator FFT (500ms delay)
    /// :MATH:FFT:SOURce CHANnel1; :MATH:FFT:WINDow HANNing (50ms delay) 
    /// :MATH:FFT:SPLit FULL (50ms delay)
    /// :MATH:FFT:UNIT VRMS
    /// 
    /// But uses IEEE488.2 status polling instead of fixed timing delays
    /// </summary>
    public static class SCPIStatusPolling
    {
        #region Constants and Configuration

        /// <summary>
        /// Default timeout for operation completion polling (5 seconds)
        /// </summary>
        public const int DEFAULT_TIMEOUT_MS = 5000;

        /// <summary>
        /// Default polling interval for status checks (10 milliseconds)
        /// </summary>
        public const int DEFAULT_POLL_INTERVAL_MS = 10;

        /// <summary>
        /// Maximum polling attempts before timeout
        /// </summary>
        public const int MAX_POLL_ATTEMPTS = DEFAULT_TIMEOUT_MS / DEFAULT_POLL_INTERVAL_MS;

        #endregion

        #region IEEE488.2 Standard Commands

        /// <summary>
        /// Operation Complete command - sets bit 0 in Standard Event Status Register when operation completes
        /// </summary>
        public const string OPC_COMMAND = "*OPC";

        /// <summary>
        /// Operation Complete Query - returns "1" when previous operations are complete
        /// This is the primary command used for status polling
        /// </summary>
        public const string OPC_QUERY = "*OPC?";

        /// <summary>
        /// Standard Event Status Register Query - returns status byte for detailed diagnostics
        /// </summary>
        public const string ESR_QUERY = "*ESR?";

        /// <summary>
        /// Service Request Status Byte Query - returns service request status
        /// </summary>
        public const string STB_QUERY = "*STB?";

        /// <summary>
        /// Wait for Operation Complete - alternative to polling
        /// </summary>
        public const string WAI_COMMAND = "*WAI";

        #endregion

        #region Core Status Polling Methods

        /// <summary>
        /// Send command and wait for operation completion using *OPC? polling
        /// This is the recommended method for reliable SCPI operations
        /// </summary>
        /// <param name="visaManager">VISA communication manager</param>
        /// <param name="command">SCPI command to send</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 5000ms)</param>
        /// <returns>True if operation completed successfully within timeout</returns>
        public static async Task<bool> SendCommandAndWaitAsync(VisaManager visaManager, string command, int timeoutMs = DEFAULT_TIMEOUT_MS)
        {
            try
            {
                // Send the command first
                if (!visaManager.SendCommand(command))
                {
                    Console.WriteLine($"❌ Failed to send command: {command}");
                    return false;
                }

                Console.WriteLine($"📤 Sent: {command}");

                // Wait for operation completion using status polling
                return await WaitForOperationCompleteAsync(visaManager, timeoutMs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Command error [{command}]: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send multiple commands in sequence with proper status polling between each
        /// This is perfect for the mathematics panel switching sequences
        /// </summary>
        /// <param name="visaManager">VISA communication manager</param>
        /// <param name="commands">Array of SCPI commands to send sequentially</param>
        /// <param name="timeoutMs">Timeout per command in milliseconds (default: 5000ms)</param>
        /// <returns>True if all commands completed successfully</returns>
        public static async Task<bool> SendCommandSequenceAsync(VisaManager visaManager, string[] commands, int timeoutMs = DEFAULT_TIMEOUT_MS)
        {
            for (int i = 0; i < commands.Length; i++)
            {
                string command = commands[i];
                Console.WriteLine($"📤 Command {i + 1}/{commands.Length}: {command}");

                if (!await SendCommandAndWaitAsync(visaManager, command, timeoutMs))
                {
                    Console.WriteLine($"❌ Sequence failed at command {i + 1}: {command}");
                    return false;
                }

                Console.WriteLine($"✅ Command {i + 1} completed");
            }

            Console.WriteLine($"🎯 All {commands.Length} commands completed successfully");
            return true;
        }

        /// <summary>
        /// Wait for operation completion using *OPC? polling
        /// Polls every 10ms until operation completes or timeout occurs
        /// </summary>
        /// <param name="visaManager">VISA communication manager</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 5000ms)</param>
        /// <returns>True if operation completed within timeout</returns>
        public static async Task<bool> WaitForOperationCompleteAsync(VisaManager visaManager, int timeoutMs = DEFAULT_TIMEOUT_MS)
        {
            int attempts = 0;
            int maxAttempts = timeoutMs / DEFAULT_POLL_INTERVAL_MS;

            while (attempts < maxAttempts)
            {
                try
                {
                    // Query operation complete status
                    string response = visaManager.SendQuery(OPC_QUERY);

                    if (response == "1")
                    {
                        Console.WriteLine($"✅ Operation completed after {attempts * DEFAULT_POLL_INTERVAL_MS}ms");
                        return true;
                    }

                    // Wait before next poll
                    await Task.Delay(DEFAULT_POLL_INTERVAL_MS);
                    attempts++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Status polling error (attempt {attempts}): {ex.Message}");
                    await Task.Delay(DEFAULT_POLL_INTERVAL_MS);
                    attempts++;
                }
            }

            Console.WriteLine($"⏰ Operation timed out after {timeoutMs}ms");
            return false;
        }

        /// <summary>
        /// Alternative method using Event Status Register (ESR) polling
        /// Uses *OPC command to set completion bit, then polls ESR register
        /// </summary>
        /// <param name="visaManager">VISA communication manager</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 5000ms)</param>
        /// <returns>True if operation completed within timeout</returns>
        public static async Task<bool> WaitForESROperationCompleteAsync(VisaManager visaManager, int timeoutMs = DEFAULT_TIMEOUT_MS)
        {
            // Send *OPC to set the operation complete bit when done
            visaManager.SendCommand(OPC_COMMAND);

            int attempts = 0;
            int maxAttempts = timeoutMs / DEFAULT_POLL_INTERVAL_MS;

            while (attempts < maxAttempts)
            {
                try
                {
                    // Query Event Status Register
                    string response = visaManager.SendQuery(ESR_QUERY);

                    if (int.TryParse(response, out int statusByte))
                    {
                        // Check bit 0 (operation complete bit)
                        if ((statusByte & 0x01) != 0)
                        {
                            Console.WriteLine($"✅ ESR Operation completed after {attempts * DEFAULT_POLL_INTERVAL_MS}ms");
                            return true;
                        }
                    }

                    await Task.Delay(DEFAULT_POLL_INTERVAL_MS);
                    attempts++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ ESR polling error: {ex.Message}");
                    await Task.Delay(DEFAULT_POLL_INTERVAL_MS);
                    attempts++;
                }
            }

            Console.WriteLine($"⏰ ESR Operation timed out after {timeoutMs}ms");
            return false;
        }

        #endregion

        #region Mathematics Panel Specific Methods

        /// <summary>
        /// Switch to FFT analysis with proper status polling
        /// 
        /// This implements the user's exact working sequence:
        /// :MATH:DISPlay OFF; :MATH:RESet         (was 150ms delay)
        /// :MATH:DISPlay ON; :MATH:OPERator FFT   (was 500ms delay)
        /// :MATH:FFT:SOURce CHANnel1; :MATH:FFT:WINDow HANNing (was 50ms delay)
        /// :MATH:FFT:SPLit FULL                   (was 50ms delay) 
        /// :MATH:FFT:UNIT VRMS
        /// </summary>
        /// <param name="visaManager">VISA communication manager</param>
        /// <param name="source">FFT source channel (default: CHANnel1)</param>
        /// <param name="window">FFT window function (default: HANNing)</param>
        /// <param name="split">FFT split mode (default: FULL)</param>
        /// <param name="unit">FFT unit (default: VRMS)</param>
        /// <returns>True if switch completed successfully</returns>
        public static async Task<bool> SwitchToFFTAnalysisAsync(
            VisaManager visaManager,
            string source = "CHANnel1",
            string window = "HANNing",
            string split = "FULL",
            string unit = "VRMS")
        {
            Console.WriteLine("🔄 Switching to FFT Analysis with status polling...");

            var commands = new[]
            {
                // Step 1: Disable and reset (replaces original 150ms delay)
                ":MATH:DISPlay OFF",
                ":MATH:RESet",
                
                // Step 2: Enable FFT mode (replaces original 500ms delay)  
                ":MATH:DISPlay ON",
                ":MATH:OPERator FFT",
                
                // Step 3: Configure FFT parameters (replaces original 50ms delays)
                $":MATH:FFT:SOURce {source}",
                $":MATH:FFT:WINDow {window}",
                $":MATH:FFT:SPLit {split}",
                $":MATH:FFT:UNIT {unit}"
            };

            return await SendCommandSequenceAsync(visaManager, commands);
        }

        /// <summary>
        /// Switch to Basic Operations with proper status polling
        /// Implements reliable switching for ADD, SUB, MUL, DIV operations
        /// </summary>
        /// <param name="visaManager">VISA communication manager</param>
        /// <param name="source1">First source channel (default: CHANnel1)</param>
        /// <param name="source2">Second source channel (default: CHANnel2)</param>
        /// <param name="operation">Math operation - ADD, SUB, MUL, DIV (default: ADD)</param>
        /// <returns>True if switch completed successfully</returns>
        public static async Task<bool> SwitchToBasicOperationsAsync(
            VisaManager visaManager,
            string source1 = "CHANnel1",
            string source2 = "CHANnel2",
            string operation = "ADD")
        {
            Console.WriteLine("🔄 Switching to Basic Operations with status polling...");

            var commands = new[]
            {
                ":MATH:DISPlay OFF",
                ":MATH:RESet",
                ":MATH:DISPlay ON",
                $":MATH:OPERator {operation}",
                $":MATH:SOURce1 {source1}",
                $":MATH:SOURce2 {source2}"
            };

            return await SendCommandSequenceAsync(visaManager, commands);
        }

        /// <summary>
        /// Switch to Digital Filter with proper status polling
        /// Implements reliable switching for filter operations
        /// </summary>
        /// <param name="visaManager">VISA communication manager</param>
        /// <param name="filterType">Filter type - LPASs, HPASs, BPASs, BSTop (default: LPASs)</param>
        /// <param name="w1">Lower cutoff frequency in Hz (default: 1000)</param>
        /// <param name="w2">Upper cutoff frequency in Hz (default: 10000)</param>
        /// <returns>True if switch completed successfully</returns>
        public static async Task<bool> SwitchToDigitalFilterAsync(
            VisaManager visaManager,
            string filterType = "LPASs",
            double w1 = 1000,
            double w2 = 10000)
        {
            Console.WriteLine("🔄 Switching to Digital Filter with status polling...");

            var commands = new[]
            {
                ":MATH:DISPlay OFF",
                ":MATH:RESet",
                ":MATH:DISPlay ON",
                ":MATH:OPERator FILTer",
                $":MATH:FILTer:TYPE {filterType}",
                $":MATH:FILTer:W1 {w1}",
                $":MATH:FILTer:W2 {w2}"
            };

            return await SendCommandSequenceAsync(visaManager, commands);
        }

        /// <summary>
        /// Switch to Advanced Math functions with proper status polling
        /// Implements reliable switching for integration, differentiation, etc.
        /// </summary>
        /// <param name="visaManager">VISA communication manager</param>
        /// <param name="function">Advanced function - INTG, DIFF, SQRT, LG, LN, EXP, ABS</param>
        /// <param name="startPoint">Start point for analysis</param>
        /// <param name="endPoint">End point for analysis</param>
        /// <returns>True if switch completed successfully</returns>
        public static async Task<bool> SwitchToAdvancedMathAsync(
            VisaManager visaManager,
            string function,
            double startPoint,
            double endPoint)
        {
            Console.WriteLine("🔄 Switching to Advanced Math with status polling...");

            var commands = new[]
            {
                ":MATH:DISPlay OFF",
                ":MATH:RESet",
                ":MATH:DISPlay ON",
                $":MATH:OPERator {function}",
                $":MATH:OPTion:STARt {startPoint}",
                $":MATH:OPTion:END {endPoint}"
            };

            return await SendCommandSequenceAsync(visaManager, commands);
        }

        /// <summary>
        /// Exit mathematics panel safely
        /// Implements clean shutdown of math subsystem
        /// </summary>
        /// <param name="visaManager">VISA communication manager</param>
        /// <returns>True if exit completed successfully</returns>
        public static async Task<bool> ExitMathematicsPanelAsync(VisaManager visaManager)
        {
            Console.WriteLine("🚪 Exiting Mathematics Panel...");

            var commands = new[]
            {
                ":MATH:DISPlay OFF",
                ":MATH:RESet"
            };

            return await SendCommandSequenceAsync(visaManager, commands);
        }

        #endregion

        #region Utility and Diagnostic Methods

        /// <summary>
        /// Check if instrument supports IEEE488.2 status polling
        /// Verifies that *OPC? command is functional
        /// </summary>
        /// <param name="visaManager">VISA communication manager</param>
        /// <returns>True if status polling is supported and functional</returns>
        public static bool SupportsStatusPolling(VisaManager visaManager)
        {
            try
            {
                Console.WriteLine("🔍 Testing status polling support...");

                // Try a simple *OPC? query
                string response = visaManager.SendQuery(OPC_QUERY);
                bool supported = !string.IsNullOrEmpty(response);

                Console.WriteLine($"Status polling support: {(supported ? "✅ Supported" : "❌ Not supported")}");
                return supported;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Status polling test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get current instrument status for debugging and diagnostics
        /// Queries multiple IEEE488.2 status registers
        /// </summary>
        /// <param name="visaManager">VISA communication manager</param>
        /// <returns>Status information string with STB, ESR, and OPC values</returns>
        public static string GetInstrumentStatus(VisaManager visaManager)
        {
            try
            {
                Console.WriteLine("📊 Querying instrument status...");

                string stb = visaManager.SendQuery(STB_QUERY);
                string esr = visaManager.SendQuery(ESR_QUERY);
                string opc = visaManager.SendQuery(OPC_QUERY);

                string status = $"STB: {stb}, ESR: {esr}, OPC: {opc}";
                Console.WriteLine($"📊 Status: {status}");
                return status;
            }
            catch (Exception ex)
            {
                string errorStatus = $"Status query failed: {ex.Message}";
                Console.WriteLine($"❌ {errorStatus}");
                return errorStatus;
            }
        }

        /// <summary>
        /// Test the complete mathematics switching sequence for validation
        /// Useful for verifying that status polling is working correctly
        /// </summary>
        /// <param name="visaManager">VISA communication manager</param>
        /// <returns>True if test sequence completed successfully</returns>
        public static async Task<bool> TestMathematicsSwitchingAsync(VisaManager visaManager)
        {
            Console.WriteLine("🧪 Testing mathematics panel switching sequence...");

            try
            {
                // Test sequence: Basic → FFT → Filter → Exit

                Console.WriteLine("1️⃣ Testing Basic Operations...");
                if (!await SwitchToBasicOperationsAsync(visaManager, "CHANnel1", "CHANnel2", "ADD"))
                {
                    Console.WriteLine("❌ Basic operations test failed");
                    return false;
                }

                Console.WriteLine("2️⃣ Testing FFT Analysis...");
                if (!await SwitchToFFTAnalysisAsync(visaManager, "CHANnel1", "HANNing", "FULL", "VRMS"))
                {
                    Console.WriteLine("❌ FFT analysis test failed");
                    return false;
                }

                Console.WriteLine("3️⃣ Testing Digital Filter...");
                if (!await SwitchToDigitalFilterAsync(visaManager, "LPASs", 1000, 5000))
                {
                    Console.WriteLine("❌ Digital filter test failed");
                    return false;
                }

                Console.WriteLine("4️⃣ Testing Exit...");
                if (!await ExitMathematicsPanelAsync(visaManager))
                {
                    Console.WriteLine("❌ Exit test failed");
                    return false;
                }

                Console.WriteLine("🎉 All mathematics switching tests passed!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test sequence failed: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}