// File: Storage/StorageDataClasses.cs
using DS1000Z_E_USB_Control.Channels.Ch1;
using DS1000Z_E_USB_Control.Channels.Ch2;
using DS1000Z_E_USB_Control.TimeBase;
using DS1000Z_E_USB_Control.Trigger;
using Microsoft.Win32;
using Rigol_DS1000Z_E_Control;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

// File: Storage/SystemSetupStorageManager.cs

using System.IO;

using System.Text.Json;

// File: Storage/EnhancedUSBStorageManager.cs










namespace DS1000Z_E_USB_Control.Storage
{
    /// <summary>
    /// Complete setup data structure for storing all oscilloscope configurations
    /// </summary>
    public class SetupData
    {
        public string Version { get; set; } = "1.0";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public DeviceInfo DeviceInfo { get; set; } = new DeviceInfo();
        public Ch1Settings Channel1Settings { get; set; }
        public Ch2Settings Channel2Settings { get; set; }
        public TriggerSettings TriggerSettings { get; set; }
        public TimeBaseSettings TimeBaseSettings { get; set; }
    }

    /// <summary>
    /// Device information for setup files
    /// </summary>
    public class DeviceInfo
    {
        public string ModelNumber { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string FirmwareVersion { get; set; } = "";
        public string ConnectionInfo { get; set; } = "";
    }

    /// <summary>
    /// USB drive status information
    /// </summary>
    public class USBStatus
    {
        public bool IsConnected { get; set; }
        public int FileCount { get; set; }
        public List<string> Files { get; set; } = new List<string>();
        public string CapacityInfo { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public override string ToString()
        {
            if (!IsConnected)
            {
                return $"USB Status: Disconnected\n{ErrorMessage}";
            }

            return $"USB Status: Connected\n" +
                   $"Files: {FileCount}\n" +
                   $"Capacity: {(string.IsNullOrEmpty(CapacityInfo) ? "Unknown" : CapacityInfo)}";
        }
    }

    /// <summary>
    /// Supported file formats for different operations
    /// </summary>
    public enum USBWaveformFormat
    {
        CSV,    // Comma-separated values
        BIN,    // Binary format 
        TXT     // Text format (alternative to CSV)
    }

    /// <summary>
    /// Supported image formats for screen captures
    /// </summary>
    public enum USBImageFormat
    {
        BMP24,  // 24-bit color bitmap
        BMP8,   // 8-bit grayscale bitmap
        PNG,    // Portable Network Graphics
        JPEG,   // JPEG compressed
        TIFF    // Tagged Image File Format
    }

    /// <summary>
    /// Setup file format options
    /// </summary>
    public enum SetupFileFormat
    {
        NativeOscilloscope,  // .SET format saved directly to oscilloscope
        JSON,                // JSON format for cross-platform compatibility
        Text,                // Human-readable text format
        XML,                 // XML format for structured data
        INI                  // INI format for simple key-value pairs
    }
}



namespace DS1000Z_E_USB_Control.Storage
{
    /// <summary>
    /// Complete system setup storage manager for saving/loading oscilloscope configurations
    /// Works with RigolDS1000ZE and OscilloscopeSettingsManager
    /// </summary>
    public class SystemSetupStorageManager
    {
        private readonly RigolDS1000ZE oscilloscope;
        private readonly OscilloscopeSettingsManager settingsManager;
        private readonly Action<string> logger;

        public SystemSetupStorageManager(RigolDS1000ZE oscilloscope,
                                       OscilloscopeSettingsManager settingsManager,
                                       Action<string> logger)
        {
            this.oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Save Operations

        /// <summary>
        /// Save complete setup with user file dialog
        /// </summary>
        public bool SaveSetupWithDialog(SetupFileFormat format = SetupFileFormat.JSON)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Save Oscilloscope Setup",
                    FileName = $"RigolDS1000ZE_Setup_{DateTime.Now:yyyyMMdd_HHmmss}",
                    Filter = GetFileFilter(format),
                    DefaultExt = GetFileExtension(format)
                };

                if (dialog.ShowDialog() == true)
                {
                    return SaveSetupToFile(dialog.FileName, format);
                }

                return false;
            }
            catch (Exception ex)
            {
                logger($"❌ Save setup dialog error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save setup to specific file
        /// </summary>
        public bool SaveSetupToFile(string filePath, SetupFileFormat format)
        {
            try
            {
                logger($"💾 Saving setup to: {filePath} (Format: {format})");

                // Read current settings from oscilloscope
                if (!settingsManager.ReadAllCurrentSettings())
                {
                    logger("❌ Failed to read current settings from oscilloscope");
                    return false;
                }

                // Create setup data structure
                var setupData = CreateSetupData();

                // Save in requested format
                bool success = format switch
                {
                    SetupFileFormat.JSON => SaveAsJSON(filePath, setupData),
                    SetupFileFormat.Text => SaveAsText(filePath, setupData),
                    SetupFileFormat.XML => SaveAsXML(filePath, setupData),
                    SetupFileFormat.INI => SaveAsINI(filePath, setupData),
                    SetupFileFormat.NativeOscilloscope => SaveAsNativeFormat(filePath),
                    _ => SaveAsJSON(filePath, setupData) // Default to JSON
                };

                if (success)
                {
                    logger($"✅ Setup saved successfully: {Path.GetFileName(filePath)}");
                }

                return success;
            }
            catch (Exception ex)
            {
                logger($"❌ Save setup error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save to oscilloscope's native .SET format
        /// </summary>
        public bool SaveAsNativeFormat(string filename)
        {
            try
            {
                if (!oscilloscope.IsConnected)
                {
                    logger("❌ Oscilloscope not connected");
                    return false;
                }

                // Use oscilloscope's built-in setup save
                string baseName = Path.GetFileNameWithoutExtension(filename);

                // Save to oscilloscope internal memory first
                bool success = oscilloscope.SendCommand($":SYSTem:SETup \"{baseName}\"");

                if (success)
                {
                    logger($"✅ Setup saved to oscilloscope internal memory: {baseName}.SET");

                    // If USB is available, try to copy to USB drive
                    try
                    {
                        oscilloscope.SendCommand($":STORage:SETup:FORMat SET");
                        oscilloscope.SendCommand($":STORage:SETup:FNAMe \"{baseName}\"");
                        oscilloscope.SendCommand(":STORage:SETup:SAVE");
                        logger($"✅ Setup also copied to USB drive");
                    }
                    catch
                    {
                        logger("⚠️ Could not copy to USB drive (USB may not be connected)");
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                logger($"❌ Native format save error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Load Operations

        /// <summary>
        /// Load setup with user file dialog
        /// </summary>
        public bool LoadSetupWithDialog()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Load Oscilloscope Setup",
                    Filter = "All Setup Files|*.json;*.txt;*.xml;*.ini;*.set|" +
                           "JSON Files (*.json)|*.json|" +
                           "Text Files (*.txt)|*.txt|" +
                           "XML Files (*.xml)|*.xml|" +
                           "INI Files (*.ini)|*.ini|" +
                           "Oscilloscope Files (*.set)|*.set|" +
                           "All Files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    return LoadSetupFromFile(dialog.FileName);
                }

                return false;
            }
            catch (Exception ex)
            {
                logger($"❌ Load setup dialog error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load setup from file (auto-detect format)
        /// </summary>
        public bool LoadSetupFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    logger($"❌ Setup file not found: {filePath}");
                    return false;
                }

                logger($"📂 Loading setup from: {Path.GetFileName(filePath)}");

                // Auto-detect format based on extension
                string extension = Path.GetExtension(filePath).ToLower();
                SetupFileFormat format = extension switch
                {
                    ".json" => SetupFileFormat.JSON,
                    ".xml" => SetupFileFormat.XML,
                    ".ini" => SetupFileFormat.INI,
                    ".set" => SetupFileFormat.NativeOscilloscope,
                    _ => SetupFileFormat.Text
                };

                // Load based on format
                bool success = format switch
                {
                    SetupFileFormat.JSON => LoadFromJSON(filePath),
                    SetupFileFormat.XML => LoadFromXML(filePath),
                    SetupFileFormat.INI => LoadFromINI(filePath),
                    SetupFileFormat.NativeOscilloscope => LoadFromNativeFormat(filePath),
                    _ => LoadFromText(filePath)
                };

                if (success)
                {
                    logger($"✅ Setup loaded successfully from: {Path.GetFileName(filePath)}");
                }

                return success;
            }
            catch (Exception ex)
            {
                logger($"❌ Load setup error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Format-Specific Save Methods

        private bool SaveAsJSON(string filePath, SetupData setupData)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string jsonString = JsonSerializer.Serialize(setupData, options);
                File.WriteAllText(filePath, jsonString, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                logger($"❌ JSON save error: {ex.Message}");
                return false;
            }
        }

        private bool SaveAsText(string filePath, SetupData setupData)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# Rigol DS1000Z-E Oscilloscope Setup File");
                sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"# Device: {setupData.DeviceInfo.ModelNumber} ({setupData.DeviceInfo.SerialNumber})");
                sb.AppendLine();

                // Device Information
                sb.AppendLine("[Device Information]");
                sb.AppendLine($"Model = {setupData.DeviceInfo.ModelNumber}");
                sb.AppendLine($"Serial = {setupData.DeviceInfo.SerialNumber}");
                sb.AppendLine($"Firmware = {setupData.DeviceInfo.FirmwareVersion}");
                sb.AppendLine();

                // Channel Settings
                if (setupData.Channel1Settings != null)
                {
                    sb.AppendLine("[Channel 1]");
                    sb.AppendLine(setupData.Channel1Settings.ToString());
                    sb.AppendLine();
                }

                if (setupData.Channel2Settings != null)
                {
                    sb.AppendLine("[Channel 2]");
                    sb.AppendLine(setupData.Channel2Settings.ToString());
                    sb.AppendLine();
                }

                // Trigger Settings
                if (setupData.TriggerSettings != null)
                {
                    sb.AppendLine("[Trigger]");
                    sb.AppendLine(setupData.TriggerSettings.ToString());
                    sb.AppendLine();
                }

                // TimeBase Settings
                if (setupData.TimeBaseSettings != null)
                {
                    sb.AppendLine("[TimeBase]");
                    sb.AppendLine(setupData.TimeBaseSettings.ToString());
                    sb.AppendLine();
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                logger($"❌ Text save error: {ex.Message}");
                return false;
            }
        }

        private bool SaveAsXML(string filePath, SetupData setupData)
        {
            try
            {
                // For now, save as JSON with .xml extension
                logger("⚠️ XML format not yet implemented, saving as JSON instead");
                return SaveAsJSON(Path.ChangeExtension(filePath, ".json"), setupData);
            }
            catch (Exception ex)
            {
                logger($"❌ XML save error: {ex.Message}");
                return false;
            }
        }

        private bool SaveAsINI(string filePath, SetupData setupData)
        {
            try
            {
                // For now, save as Text with .ini extension
                logger("⚠️ INI format not yet implemented, saving as Text instead");
                return SaveAsText(Path.ChangeExtension(filePath, ".txt"), setupData);
            }
            catch (Exception ex)
            {
                logger($"❌ INI save error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Format-Specific Load Methods

        private bool LoadFromJSON(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath, Encoding.UTF8);
                var setupData = JsonSerializer.Deserialize<SetupData>(jsonString, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return ApplySetupData(setupData);
            }
            catch (Exception ex)
            {
                logger($"❌ JSON load error: {ex.Message}");
                return false;
            }
        }

        private bool LoadFromText(string filePath)
        {
            try
            {
                logger("⚠️ Text format loading not yet implemented");
                return false;
            }
            catch (Exception ex)
            {
                logger($"❌ Text load error: {ex.Message}");
                return false;
            }
        }

        private bool LoadFromXML(string filePath)
        {
            try
            {
                logger("⚠️ XML format loading not yet implemented");
                return false;
            }
            catch (Exception ex)
            {
                logger($"❌ XML load error: {ex.Message}");
                return false;
            }
        }

        private bool LoadFromINI(string filePath)
        {
            try
            {
                logger("⚠️ INI format loading not yet implemented");
                return false;
            }
            catch (Exception ex)
            {
                logger($"❌ INI load error: {ex.Message}");
                return false;
            }
        }

        private bool LoadFromNativeFormat(string filePath)
        {
            try
            {
                if (!oscilloscope.IsConnected)
                {
                    logger("❌ Oscilloscope not connected");
                    return false;
                }

                string baseName = Path.GetFileNameWithoutExtension(filePath);

                // Try to recall from oscilloscope internal memory
                bool success = oscilloscope.SendCommand($":SYSTem:SETup:RECall \"{baseName}\"");

                if (success)
                {
                    logger($"✅ Setup recalled from oscilloscope: {baseName}");
                    // Refresh our local settings
                    settingsManager.ReadAllCurrentSettings();
                }

                return success;
            }
            catch (Exception ex)
            {
                logger($"❌ Native format load error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private SetupData CreateSetupData()
        {
            return new SetupData
            {
                Version = "1.0",
                Timestamp = DateTime.Now,
                DeviceInfo = new DeviceInfo
                {
                    ModelNumber = settingsManager.GetDeviceID(),
                    SerialNumber = "Unknown", // TODO: Get from oscilloscope if available
                    FirmwareVersion = "Unknown", // TODO: Get from oscilloscope if available
                    ConnectionInfo = "USB"
                },
                Channel1Settings = settingsManager.Channel1Settings,
                Channel2Settings = settingsManager.Channel2Settings,
                TriggerSettings = settingsManager.TriggerSettings,
                TimeBaseSettings = settingsManager.TimeBaseSettings
            };
        }

        private bool ApplySetupData(SetupData setupData)
        {
            try
            {
                logger("🔄 Applying setup data to oscilloscope...");

                bool success = true;

                // Apply Channel 1 settings
                if (setupData.Channel1Settings != null)
                {
                    if (!settingsManager.ApplyChannel1Settings(setupData.Channel1Settings))
                        success = false;
                }

                // Apply Channel 2 settings
                if (setupData.Channel2Settings != null)
                {
                    if (!settingsManager.ApplyChannel2Settings(setupData.Channel2Settings))
                        success = false;
                }

                // Apply Trigger settings
                if (setupData.TriggerSettings != null)
                {
                    if (!settingsManager.ApplyTriggerSettings(setupData.TriggerSettings))
                        success = false;
                }

                // Apply TimeBase settings
                if (setupData.TimeBaseSettings != null)
                {
                    if (!settingsManager.ApplyTimeBaseSettings(setupData.TimeBaseSettings))
                        success = false;
                }

                return success;
            }
            catch (Exception ex)
            {
                logger($"❌ Apply setup error: {ex.Message}");
                return false;
            }
        }

        private string GetFileFilter(SetupFileFormat format)
        {
            return format switch
            {
                SetupFileFormat.JSON => "JSON Setup Files (*.json)|*.json|All Files (*.*)|*.*",
                SetupFileFormat.Text => "Text Setup Files (*.txt)|*.txt|All Files (*.*)|*.*",
                SetupFileFormat.XML => "XML Setup Files (*.xml)|*.xml|All Files (*.*)|*.*",
                SetupFileFormat.INI => "INI Setup Files (*.ini)|*.ini|All Files (*.*)|*.*",
                SetupFileFormat.NativeOscilloscope => "Oscilloscope Setup Files (*.set)|*.set|All Files (*.*)|*.*",
                _ => "All Files (*.*)|*.*"
            };
        }

        private string GetFileExtension(SetupFileFormat format)
        {
            return format switch
            {
                SetupFileFormat.JSON => ".json",
                SetupFileFormat.Text => ".txt",
                SetupFileFormat.XML => ".xml",
                SetupFileFormat.INI => ".ini",
                SetupFileFormat.NativeOscilloscope => ".set",
                _ => ".txt"
            };
        }

        #endregion
    }
}



namespace DS1000Z_E_USB_Control.Storage
{
    /// <summary>
    /// Enhanced USB storage manager with full format support
    /// Works with RigolDS1000ZE oscilloscope connection
    /// </summary>
    public class EnhancedUSBStorageManager
    {
        private readonly RigolDS1000ZE oscilloscope;
        private readonly Action<string> logger;

        public EnhancedUSBStorageManager(RigolDS1000ZE oscilloscope, Action<string> logger)
        {
            this.oscilloscope = oscilloscope ?? throw new ArgumentNullException(nameof(oscilloscope));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Waveform Storage with Multiple Formats

        /// <summary>
        /// Save waveform with specified format to USB drive
        /// </summary>
        public bool SaveWaveformToUSB(int channelNumber, string filename, USBWaveformFormat format)
        {
            try
            {
                if (!oscilloscope.IsConnected)
                {
                    logger("❌ Oscilloscope not connected");
                    return false;
                }

                // Stop acquisition for stable save
                oscilloscope.SendCommand(":STOP");
                Thread.Sleep(100);

                // Set the file format
                string formatCommand = format switch
                {
                    USBWaveformFormat.CSV => ":STORage:WAVeform:FORMat CSV",
                    USBWaveformFormat.BIN => ":STORage:WAVeform:FORMat BIN",
                    USBWaveformFormat.TXT => ":STORage:WAVeform:FORMat TXT",
                    _ => ":STORage:WAVeform:FORMat CSV"
                };

                oscilloscope.SendCommand(formatCommand);

                // Set the source channel
                oscilloscope.SendCommand($":STORage:WAVeform:SOURce CHANnel{channelNumber}");

                // Set the filename (without extension - oscilloscope adds it)
                oscilloscope.SendCommand($":STORage:WAVeform:FNAMe \"{filename}\"");

                // Execute the save command
                bool success = oscilloscope.SendCommand(":STORage:WAVeform:SAVE");

                if (success)
                {
                    string extension = GetWaveformExtension(format);
                    logger($"✅ Waveform saved to oscilloscope USB: {filename}{extension} ({format})");
                }
                else
                {
                    logger($"❌ Failed to save waveform to USB in {format} format");
                }

                return success;
            }
            catch (Exception ex)
            {
                logger($"❌ USB waveform save error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save multiple channels simultaneously
        /// </summary>
        public bool SaveMultipleWaveformsToUSB(int[] channelNumbers, string baseFilename, USBWaveformFormat format)
        {
            bool allSuccess = true;

            foreach (int channel in channelNumbers)
            {
                string filename = $"{baseFilename}_CH{channel}";
                if (!SaveWaveformToUSB(channel, filename, format))
                {
                    allSuccess = false;
                }
                Thread.Sleep(200); // Small delay between saves
            }

            return allSuccess;
        }

        private string GetWaveformExtension(USBWaveformFormat format)
        {
            return format switch
            {
                USBWaveformFormat.CSV => ".csv",
                USBWaveformFormat.BIN => ".bin",
                USBWaveformFormat.TXT => ".txt",
                _ => ".csv"
            };
        }

        #endregion

        #region Enhanced Image Storage

        /// <summary>
        /// Save screen image with enhanced options
        /// </summary>
        public bool SaveScreenImageToUSB(string filename, USBImageFormat format, bool colorMode = true, bool invertColors = false)
        {
            try
            {
                if (!oscilloscope.IsConnected)
                {
                    logger("❌ Oscilloscope not connected");
                    return false;
                }

                // Set image format
                oscilloscope.SendCommand($":STORage:IMAGe:TYPE {format}");

                // Set color mode (color vs grayscale)
                string colorSetting = colorMode ? "ON" : "OFF";
                oscilloscope.SendCommand($":STORage:IMAGe:COLor {colorSetting}");

                // Set invert setting
                string invertSetting = invertColors ? "ON" : "OFF";
                oscilloscope.SendCommand($":STORage:IMAGe:INVERT {invertSetting}");

                // Set filename
                oscilloscope.SendCommand($":STORage:IMAGe:FNAMe \"{filename}\"");

                // Save image to USB
                bool success = oscilloscope.SendCommand(":STORage:IMAGe:SAVE");

                if (success)
                {
                    string extension = GetImageExtension(format);
                    string colorDesc = colorMode ? "color" : "grayscale";
                    string invertDesc = invertColors ? ", inverted" : "";
                    logger($"✅ Screen image saved: {filename}{extension} ({format}, {colorDesc}{invertDesc})");
                }
                else
                {
                    logger($"❌ Failed to save image to USB");
                }

                return success;
            }
            catch (Exception ex)
            {
                logger($"❌ USB image save error: {ex.Message}");
                return false;
            }
        }

        private string GetImageExtension(USBImageFormat format)
        {
            return format switch
            {
                USBImageFormat.BMP24 => ".bmp",
                USBImageFormat.BMP8 => ".bmp",
                USBImageFormat.PNG => ".png",
                USBImageFormat.JPEG => ".jpg",
                USBImageFormat.TIFF => ".tiff",
                _ => ".png"
            };
        }

        #endregion

        #region USB File Management

        /// <summary>
        /// Get list of files on USB drive
        /// </summary>
        public List<string> GetUSBFileList(string filePattern = "*.*")
        {
            try
            {
                if (!oscilloscope.IsConnected)
                {
                    logger("❌ Oscilloscope not connected");
                    return new List<string>();
                }

                // Query file catalog
                string response = oscilloscope.SendQuery($":STORage:CATalog? \"{filePattern}\"");

                if (string.IsNullOrEmpty(response))
                {
                    logger("📁 No files found or USB drive not connected");
                    return new List<string>();
                }

                // Parse the response (format may vary)
                var files = new List<string>();
                string[] parts = response.Split(',', ';', '\n');

                foreach (string part in parts)
                {
                    string trimmed = part.Trim().Trim('"');
                    if (!string.IsNullOrEmpty(trimmed) && trimmed != "0")
                    {
                        files.Add(trimmed);
                    }
                }

                logger($"📁 Found {files.Count} files on USB drive");
                return files;
            }
            catch (Exception ex)
            {
                logger($"❌ USB file list error: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Delete file from USB drive
        /// </summary>
        public bool DeleteUSBFile(string filename)
        {
            try
            {
                if (!oscilloscope.IsConnected)
                {
                    logger("❌ Oscilloscope not connected");
                    return false;
                }

                bool success = oscilloscope.SendCommand($":STORage:DELete \"{filename}\"");

                if (success)
                {
                    logger($"🗑️ File deleted from USB: {filename}");
                }
                else
                {
                    logger($"❌ Failed to delete file: {filename}");
                }

                return success;
            }
            catch (Exception ex)
            {
                logger($"❌ USB file delete error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check USB drive status and capacity
        /// </summary>
        public USBStatus GetUSBStatus()
        {
            try
            {
                if (!oscilloscope.IsConnected)
                {
                    return new USBStatus { IsConnected = false, ErrorMessage = "Oscilloscope not connected" };
                }

                // Try to get USB information
                var files = GetUSBFileList();
                bool isConnected = files.Count >= 0; // Even empty drives return empty list

                // Try to get capacity info (if supported)
                string capacityResponse = "";
                try
                {
                    capacityResponse = oscilloscope.SendQuery(":STORage:CAPacity?");
                }
                catch
                {
                    // Capacity query might not be supported on all models
                }

                return new USBStatus
                {
                    IsConnected = isConnected,
                    FileCount = files.Count,
                    Files = files,
                    CapacityInfo = capacityResponse
                };
            }
            catch (Exception ex)
            {
                return new USBStatus
                {
                    IsConnected = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion
    }
}