using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace DS1000Z_E_USB_Control.Measurements
{
    /// <summary>
    /// Utility class for exporting measurement data and statistics
    /// </summary>
    public static class MeasurementExportUtility
    {
        #region Export Methods

        /// <summary>
        /// Export measurement statistics to CSV file
        /// </summary>
        /// <param name="controller">The measurement controller containing statistics</param>
        /// <param name="parentWindow">Parent window for dialogs</param>
        /// <returns>True if export was successful</returns>
        public static bool ExportStatisticsToCSV(MeasurementController controller, Window parentWindow = null)
        {
            if (controller?.Statistics == null || !controller.Statistics.Any())
            {
                MessageBox.Show("No statistics data to export.", "Export Statistics",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"MeasurementStatistics_{DateTime.Now:yyyyMMdd_HHmmss}",
                Title = "Export Measurement Statistics"
            };

            if (parentWindow != null)
            {
                saveDialog.Owner = parentWindow;
            }

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var csvContent = GenerateStatisticsCSV(controller);
                    File.WriteAllText(saveDialog.FileName, csvContent);

                    MessageBox.Show($"Statistics exported successfully to:\n{saveDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting statistics:\n{ex.Message}", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Export current measurement values to CSV file
        /// </summary>
        /// <param name="controller">The measurement controller containing current values</param>
        /// <param name="parentWindow">Parent window for dialogs</param>
        /// <returns>True if export was successful</returns>
        public static bool ExportCurrentValuesToCSV(MeasurementController controller, Window parentWindow = null)
        {
            if (controller?.Settings?.EnabledMeasurements == null || !controller.Settings.EnabledMeasurements.Any())
            {
                MessageBox.Show("No measurement data to export.", "Export Data",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"MeasurementValues_{DateTime.Now:yyyyMMdd_HHmmss}",
                Title = "Export Current Measurement Values"
            };

            if (parentWindow != null)
            {
                saveDialog.Owner = parentWindow;
            }

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var csvContent = GenerateCurrentValuesCSV(controller);
                    File.WriteAllText(saveDialog.FileName, csvContent);

                    MessageBox.Show($"Current values exported successfully to:\n{saveDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting current values:\n{ex.Message}", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Export measurement configuration to JSON file
        /// </summary>
        /// <param name="controller">The measurement controller containing settings</param>
        /// <param name="parentWindow">Parent window for dialogs</param>
        /// <returns>True if export was successful</returns>
        public static bool ExportConfigurationToJSON(MeasurementController controller, Window parentWindow = null)
        {
            if (controller?.Settings == null)
            {
                MessageBox.Show("No configuration data to export.", "Export Configuration",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"MeasurementConfig_{DateTime.Now:yyyyMMdd_HHmmss}",
                Title = "Export Measurement Configuration"
            };

            if (parentWindow != null)
            {
                saveDialog.Owner = parentWindow;
            }

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var jsonContent = GenerateConfigurationJSON(controller);
                    File.WriteAllText(saveDialog.FileName, jsonContent);

                    MessageBox.Show($"Configuration exported successfully to:\n{saveDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting configuration:\n{ex.Message}", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return false;
        }

        #endregion

        #region CSV Generation Methods

        /// <summary>
        /// Generate CSV content for measurement statistics
        /// </summary>
        /// <param name="controller">The measurement controller</param>
        /// <returns>CSV formatted string</returns>
        private static string GenerateStatisticsCSV(MeasurementController controller)
        {
            var sb = new StringBuilder();

            // Header information
            sb.AppendLine("Measurement Statistics Export");
            sb.AppendLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Source: {controller.Settings.AutoMeasureSource}");
            sb.AppendLine($"Update Rate: {controller.Settings.AutoUpdateIntervalMs} ms");
            sb.AppendLine();

            // CSV headers
            sb.AppendLine("Measurement,Current,Average,Minimum,Maximum,Std Deviation,Sample Count,Unit");

            // Data rows
            var parameters = MeasurementSettings.GetAvailableParameters();
            foreach (var kvp in controller.Statistics)
            {
                var stats = kvp.Value;
                var parameter = parameters.ContainsKey(kvp.Key) ? parameters[kvp.Key] : null;
                var unit = parameter?.Unit ?? "Unknown";

                sb.AppendLine($"{kvp.Key},{stats.Current:E6},{stats.Average:E6}," +
                            $"{stats.Minimum:E6},{stats.Maximum:E6}," +
                            $"{stats.StandardDeviation:E6},{stats.Count},{unit}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate CSV content for current measurement values
        /// </summary>
        /// <param name="controller">The measurement controller</param>
        /// <returns>CSV formatted string</returns>
        private static string GenerateCurrentValuesCSV(MeasurementController controller)
        {
            var sb = new StringBuilder();

            // Header information
            sb.AppendLine("Current Measurement Values Export");
            sb.AppendLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Source: {controller.Settings.AutoMeasureSource}");
            sb.AppendLine();

            // CSV headers
            sb.AppendLine("Measurement,Value,Unit,Status");

            // Data rows
            var parameters = MeasurementSettings.GetAvailableParameters();
            foreach (var measurement in controller.Settings.EnabledMeasurements)
            {
                var parameter = parameters.ContainsKey(measurement) ? parameters[measurement] : null;
                var unit = parameter?.Unit ?? "Unknown";

                // Get current value from statistics if available
                var currentValue = controller.Statistics.ContainsKey(measurement)
                    ? controller.Statistics[measurement].Current
                    : double.NaN;

                var status = double.IsNaN(currentValue) ? "No Data" : "Valid";
                var valueStr = double.IsNaN(currentValue) ? "N/A" : $"{currentValue:E6}";

                sb.AppendLine($"{measurement},{valueStr},{unit},{status}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate JSON content for measurement configuration
        /// </summary>
        /// <param name="controller">The measurement controller</param>
        /// <returns>JSON formatted string</returns>
        private static string GenerateConfigurationJSON(MeasurementController controller)
        {
            var config = new
            {
                ExportInfo = new
                {
                    ExportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Version = "1.0"
                },
                MeasurementSettings = new
                {
                    AutoMeasureSource = controller.Settings.AutoMeasureSource,
                    AutoUpdateEnabled = controller.Settings.AutoUpdateEnabled,
                    AutoUpdateIntervalMs = controller.Settings.AutoUpdateIntervalMs,
                    EnabledMeasurements = controller.Settings.EnabledMeasurements?.ToList() ?? new List<string>(),
                    StatisticsEnabled = controller.Settings.StatisticsEnabled
                }
            };

            return System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get export file name suggestion based on export type
        /// </summary>
        /// <param name="exportType">Type of export (Statistics, Values, Config)</param>
        /// <returns>Suggested file name</returns>
        public static string GetSuggestedFileName(string exportType)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return exportType switch
            {
                "Statistics" => $"MeasurementStatistics_{timestamp}",
                "Values" => $"MeasurementValues_{timestamp}",
                "Config" => $"MeasurementConfig_{timestamp}",
                _ => $"MeasurementExport_{timestamp}"
            };
        }

        /// <summary>
        /// Validate controller state for export operations
        /// </summary>
        /// <param name="controller">The measurement controller to validate</param>
        /// <param name="requireData">Whether data is required for the operation</param>
        /// <returns>True if controller is valid for export</returns>
        public static bool ValidateControllerForExport(MeasurementController controller, bool requireData = true)
        {
            if (controller == null)
                return false;

            if (controller.Settings == null)
                return false;

            if (requireData && (controller.Statistics == null || !controller.Statistics.Any()))
                return false;

            return true;
        }

        #endregion
    }
}