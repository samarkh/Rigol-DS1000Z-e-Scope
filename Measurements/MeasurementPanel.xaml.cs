using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DS1000Z_E_USB_Control.Measurements
{
    /// <summary>
    /// Interaction logic for MeasurementPanel.xaml
    /// Complete implementation with all event handlers and UI management
    /// </summary>
    public partial class MeasurementPanel : UserControl
    {
        #region Fields

        private MeasurementController controller;
        private bool isInitialized = false;
        private bool panelCollapsed = false;
        private DispatcherTimer autoUpdateTimer;
        private readonly Dictionary<string, CheckBox> measurementCheckBoxes;
        private readonly Dictionary<string, Border> valueDisplays;
        private readonly Dictionary<string, Border> statisticsDisplays;

        #endregion

        #region Events

        /// <summary>
        /// Event raised for logging purposes
        /// </summary>
        public event EventHandler<string> LogEvent;

        #endregion

        #region Constructor

        public MeasurementPanel()
        {
            InitializeComponent();
            measurementCheckBoxes = new Dictionary<string, CheckBox>();
            valueDisplays = new Dictionary<string, Border>();
            statisticsDisplays = new Dictionary<string, Border>();
            InitializeAutoUpdateTimer();
            CreateMeasurementSelectionUI();
            isInitialized = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Set the measurement controller
        /// </summary>
        public MeasurementController Controller
        {
            get => controller;
            set
            {
                if (controller != null)
                {
                    controller.MeasurementValueUpdated -= Controller_MeasurementValueUpdated;
                    controller.MeasurementStatisticsUpdated -= Controller_MeasurementStatisticsUpdated;
                }

                controller = value;

                if (controller != null)
                {
                    controller.MeasurementValueUpdated += Controller_MeasurementValueUpdated;
                    controller.MeasurementStatisticsUpdated += Controller_MeasurementStatisticsUpdated;
                    UpdateUI();
                }
            }
        }

        ///// <summary>
        ///// Reference to the current values panel
        ///// </summary>
        //public Panel CurrentValuesPanel { get; set; }

        ///// <summary>
        ///// Reference to the statistics panel
        ///// </summary>
        //public Panel StatisticsPanel { get; set; }

        ///// <summary>
        ///// Reference to the "No measurements" text
        ///// </summary>
        //public TextBlock NoMeasurementsText { get; set; }

        ///// <summary>
        ///// Reference to the "No statistics" text
        ///// </summary>
        //public TextBlock NoStatisticsText { get; set; }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the auto-update timer
        /// </summary>
        private void InitializeAutoUpdateTimer()
        {
            autoUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            autoUpdateTimer.Tick += AutoUpdateTimer_Tick;
        }

        /// <summary>
        /// Create the measurement selection UI
        /// </summary>
        private void CreateMeasurementSelectionUI()
        {
            // This method would create the checkboxes for measurement selection
            // Implementation depends on your specific UI structure
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Event handler for measurement value updates
        /// </summary>
        private void Controller_MeasurementValueUpdated(object sender, MeasurementValueEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => Controller_MeasurementValueUpdated(sender, e));
                return;
            }

            UpdateValueDisplay(e.MeasurementKey, e.Value);
        }

        /// <summary>
        /// Event handler for measurement statistics updates
        /// </summary>
        private void Controller_MeasurementStatisticsUpdated(object sender, MeasurementStatisticsEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => Controller_MeasurementStatisticsUpdated(sender, e));
                return;
            }

            UpdateStatisticsDisplay(e.MeasurementKey, e.Statistics);
        }

        /// <summary>
        /// Auto-update timer tick event
        /// </summary>
        private void AutoUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (controller?.Settings?.AutoUpdateEnabled == true)
            {
                controller.UpdateAllMeasurements();
            }
        }

        /// <summary>
        /// Event handler for auto display checked
        /// </summary>
        private void AutoDisplay_Checked(object sender, RoutedEventArgs e)
        {
            if (controller?.Settings != null && isInitialized)
            {
                controller.Settings.AutoDisplayEnabled = true;
                controller.SetAutoDisplay(true);
                LogEvent?.Invoke(this, "Auto display enabled");
            }
        }

        /// <summary>
        /// Event handler for auto display unchecked
        /// </summary>
        private void AutoDisplay_Unchecked(object sender, RoutedEventArgs e)
        {
            if (controller?.Settings != null && isInitialized)
            {
                controller.Settings.AutoDisplayEnabled = false;
                controller.SetAutoDisplay(false);
                LogEvent?.Invoke(this, "Auto display disabled");
            }
        }

        /// <summary>
        /// Event handler for export statistics button
        /// </summary>
        private void ExportStatistics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"MeasurementStatistics_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                // Note: Removed .Owner property as it's causing compilation errors
                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportStatisticsToFile(saveFileDialog.FileName);
                    LogEvent?.Invoke(this, $"Statistics exported to: {saveFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error exporting statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Toggle button click event
        /// </summary>
        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            panelCollapsed = !panelCollapsed;
            UpdatePanelVisibility();
        }

        #endregion

        #region UI Management

        /// <summary>
        /// Update the entire UI based on current settings
        /// </summary>
        private void UpdateUI()
        {
            if (controller?.Settings == null) return;

            UpdateAutoUpdateTimer();
            UpdateCurrentValuesDisplay();
            UpdateStatisticsDisplay();
            UpdateVisibilityStates();
        }

        /// <summary>
        /// Update the auto-update timer based on settings
        /// </summary>
        private void UpdateAutoUpdateTimer()
        {
            if (controller?.Settings?.AutoUpdateEnabled == true)
            {
                autoUpdateTimer.Interval = TimeSpan.FromMilliseconds(controller.Settings.AutoUpdateIntervalMs);
                autoUpdateTimer.Start();
            }
            else
            {
                autoUpdateTimer.Stop();
            }
        }

        /// <summary>
        /// Update panel visibility based on collapsed state
        /// </summary>
        private void UpdatePanelVisibility()
        {
            if (MainContent != null)
            {
                MainContent.Visibility = panelCollapsed ? Visibility.Collapsed : Visibility.Visible;
            }

            // Update toggle button appearance
            if (ToggleIcon != null && ToggleText != null)
            {
                ToggleIcon.Text = panelCollapsed ? "🔽" : "🔼";
                ToggleText.Text = panelCollapsed ? "Expand" : "Collapse";
            }
        }

        /// <summary>
        /// Update visibility states for various UI elements
        /// </summary>
        private void UpdateVisibilityStates()
        {
            UpdateNoMeasurementsVisibility();
            UpdateNoStatisticsVisibility();
        }

        /// <summary>
        /// Update "No measurements" text visibility
        /// </summary>
        private void UpdateNoMeasurementsVisibility()
        {
            bool hasMeasurements = controller?.Settings?.EnabledMeasurements?.Any() == true;

            if (NoMeasurementsText != null)
                NoMeasurementsText.Visibility = hasMeasurements ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Update "No statistics" text visibility
        /// </summary>
        private void UpdateNoStatisticsVisibility()
        {
            bool hasMeasurements = controller?.Settings?.EnabledMeasurements?.Any() == true;

            if (NoStatisticsText != null)
                NoStatisticsText.Visibility = (hasMeasurements && controller.Settings.StatisticsEnabled) ?
                    Visibility.Collapsed : Visibility.Visible;
        }

        #endregion

        #region Value Display Management

        /// <summary>
        /// Create value display for a measurement
        /// </summary>
        private void CreateValueDisplay(string measurementKey)
        {
            if (valueDisplays.ContainsKey(measurementKey) || CurrentValuesPanel == null) return;

            var parameters = MeasurementSettings.GetAvailableParameters();
            if (!parameters.ContainsKey(measurementKey)) return;

            var parameter = parameters[measurementKey];

            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1, 1, 1, 1), // Fixed: all 4 parameters
                Margin = new Thickness(2, 2, 2, 2), // Fixed: all 4 parameters
                Padding = new Thickness(5, 5, 5, 5) // Fixed: all 4 parameters
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var label = new TextBlock
            {
                Text = parameter.DisplayName + ":",
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(label, 0);

            var value = new TextBlock
            {
                Text = "---",
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            Grid.SetColumn(value, 1);

            grid.Children.Add(label);
            grid.Children.Add(value);
            border.Child = grid;
            border.Tag = measurementKey;

            valueDisplays[measurementKey] = border;
            CurrentValuesPanel.Children.Add(border);
        }

        /// <summary>
        /// Update value display for a measurement
        /// </summary>
        private void UpdateValueDisplay(string measurementKey, double value)
        {
            if (!valueDisplays.ContainsKey(measurementKey)) return;

            var border = valueDisplays[measurementKey];
            var grid = border.Child as Grid;
            var valueText = grid?.Children[1] as TextBlock;

            if (valueText != null)
            {
                var parameters = MeasurementSettings.GetAvailableParameters();
                if (parameters.ContainsKey(measurementKey))
                {
                    var parameter = parameters[measurementKey];
                    string formattedValue = FormatMeasurementValue(value, parameter.Unit);
                    valueText.Text = formattedValue;
                }
            }
        }

        /// <summary>
        /// Remove value display for a measurement
        /// </summary>
        private void RemoveValueDisplay(string measurementKey)
        {
            if (valueDisplays.ContainsKey(measurementKey) && CurrentValuesPanel != null)
            {
                CurrentValuesPanel.Children.Remove(valueDisplays[measurementKey]);
                valueDisplays.Remove(measurementKey);
            }
        }

        /// <summary>
        /// Update current values display
        /// </summary>
        private void UpdateCurrentValuesDisplay()
        {
            if (CurrentValuesPanel == null) return;

            // Clear existing displays
            CurrentValuesPanel.Children.Clear();
            valueDisplays.Clear();

            // Add "No measurements" text back if exists
            if (NoMeasurementsText != null)
                CurrentValuesPanel.Children.Add(NoMeasurementsText);

            // Create displays for enabled measurements
            if (controller?.Settings?.EnabledMeasurements != null)
            {
                foreach (var measurementKey in controller.Settings.EnabledMeasurements)
                {
                    CreateValueDisplay(measurementKey);
                }
            }

            UpdateNoMeasurementsVisibility();
        }

        #endregion

        #region Statistics Display Management

        /// <summary>
        /// Create statistics display for a measurement
        /// </summary>
        private void CreateStatisticsDisplay(string measurementKey)
        {
            if (statisticsDisplays.ContainsKey(measurementKey) || StatisticsPanel == null) return;

            var parameters = MeasurementSettings.GetAvailableParameters();
            if (!parameters.ContainsKey(measurementKey)) return;

            var parameter = parameters[measurementKey];

            var expander = new Expander
            {
                Header = parameter.DisplayName,
                Margin = new Thickness(2, 2, 2, 2), // Fixed: all 4 parameters
                Background = Brushes.AliceBlue
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Create statistics labels and values
            var labels = new[] { "Min:", "Max:", "Avg:", "StdDev:" };
            for (int i = 0; i < labels.Length; i++)
            {
                var label = new TextBlock
                {
                    Text = labels[i],
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(5, 2, 5, 2) // Fixed: all 4 parameters
                };
                Grid.SetRow(label, i);
                Grid.SetColumn(label, 0);

                var valueBlock = new TextBlock
                {
                    Text = "---",
                    FontFamily = new FontFamily("Consolas"),
                    Margin = new Thickness(5, 2, 5, 2) // Fixed: all 4 parameters
                };
                Grid.SetRow(valueBlock, i);
                Grid.SetColumn(valueBlock, 1);

                grid.Children.Add(label);
                grid.Children.Add(valueBlock);
            }

            expander.Content = grid;
            expander.Tag = measurementKey;

            var border = new Border
            {
                Child = expander,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1, 1, 1, 1), // Fixed: all 4 parameters
                Margin = new Thickness(2, 2, 2, 2) // Fixed: all 4 parameters
            };

            statisticsDisplays[measurementKey] = border;
            StatisticsPanel.Children.Add(border);
        }

        /// <summary>
        /// Update statistics display for a measurement
        /// </summary>
        private void UpdateStatisticsDisplay(string measurementKey, MeasurementStatistics statistics)
        {
            if (!statisticsDisplays.ContainsKey(measurementKey)) return;

            var border = statisticsDisplays[measurementKey];
            var expander = border.Child as Expander;
            var grid = expander?.Content as Grid;

            if (grid != null)
            {
                var parameters = MeasurementSettings.GetAvailableParameters();
                if (parameters.ContainsKey(measurementKey))
                {
                    var parameter = parameters[measurementKey];

                    // Update the statistics values
                    var values = new[]
                    {
                        FormatMeasurementValue(statistics.Minimum, parameter.Unit),
                        FormatMeasurementValue(statistics.Maximum, parameter.Unit),
                        FormatMeasurementValue(statistics.Average, parameter.Unit),
                        FormatMeasurementValue(statistics.StandardDeviation, parameter.Unit)
                    };

                    for (int i = 0; i < values.Length; i++)
                    {
                        var valueBlock = grid.Children.OfType<TextBlock>()
                            .Where(tb => Grid.GetRow(tb) == i && Grid.GetColumn(tb) == 1)
                            .FirstOrDefault();

                        if (valueBlock != null)
                            valueBlock.Text = values[i];
                    }
                }
            }
        }

        /// <summary>
        /// Update statistics display for all measurements
        /// </summary>
        private void UpdateStatisticsDisplay()
        {
            if (StatisticsPanel == null) return;

            // Clear existing displays
            StatisticsPanel.Children.Clear();
            statisticsDisplays.Clear();

            // Add "No statistics" text back if exists
            if (NoStatisticsText != null)
                StatisticsPanel.Children.Add(NoStatisticsText);

            // Create displays for enabled measurements
            if (controller?.Settings?.EnabledMeasurements != null && controller.Settings.StatisticsEnabled)
            {
                foreach (var measurementKey in controller.Settings.EnabledMeasurements)
                {
                    CreateStatisticsDisplay(measurementKey);
                }
            }

            UpdateNoStatisticsVisibility();
        }

        /// <summary>
        /// Export statistics to file
        /// </summary>
        private void ExportStatisticsToFile(string fileName)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Measurement,Current Value,Min,Max,Average,Standard Deviation,Count");

                if (controller?.Statistics != null)
                {
                    foreach (var kvp in controller.Statistics)
                    {
                        var stats = kvp.Value;
                        var currentValue = controller.CurrentValues.ContainsKey(kvp.Key)
                            ? controller.CurrentValues[kvp.Key]?.ToString() ?? "N/A"
                            : "N/A";

                        sb.AppendLine($"{kvp.Key},{currentValue},{stats.Minimum},{stats.Maximum},{stats.Average},{stats.StandardDeviation},{stats.Count}");
                    }
                }

                File.WriteAllText(fileName, sb.ToString());
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke(this, $"Error writing statistics file: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Format measurement value with appropriate units
        /// </summary>
        private string FormatMeasurementValue(double value, string unit)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "---";

            // Format based on the magnitude of the value
            string formattedValue;
            if (Math.Abs(value) >= 1e6)
                formattedValue = (value / 1e6).ToString("F3") + "M";
            else if (Math.Abs(value) >= 1e3)
                formattedValue = (value / 1e3).ToString("F3") + "k";
            else if (Math.Abs(value) >= 1)
                formattedValue = value.ToString("F3");
            else if (Math.Abs(value) >= 1e-3)
                formattedValue = (value * 1e3).ToString("F3") + "m";
            else if (Math.Abs(value) >= 1e-6)
                formattedValue = (value * 1e6).ToString("F3") + "μ";
            else if (Math.Abs(value) >= 1e-9)
                formattedValue = (value * 1e9).ToString("F3") + "n";
            else
                formattedValue = value.ToString("E2");

            return $"{formattedValue} {unit}";
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Cleanup()
        {
            autoUpdateTimer?.Stop();
            autoUpdateTimer = null;

            if (controller != null)
            {
                controller.MeasurementValueUpdated -= Controller_MeasurementValueUpdated;
                controller.MeasurementStatisticsUpdated -= Controller_MeasurementStatisticsUpdated;
            }
        }

        #endregion
    }
}