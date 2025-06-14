using System;
using System.Windows;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Controls
{
    public partial class GraticuleArrowControl : UserControl
    {
        // Events for movement
        public event EventHandler<GraticuleMovementEventArgs> GraticuleMovement;

        // Properties
        public double CurrentValue { get; set; }
        public double GraticuleSize { get; set; } = 1.0;
        public string Units { get; set; } = "V";
        public double MinValue { get; set; } = -10.0;
        public double MaxValue { get; set; } = 10.0;

        public GraticuleArrowControl()
        {
            InitializeComponent();
            SetupEventHandlers();
            UpdateDisplay();
        }

        private void SetupEventHandlers()
        {
            DoubleUpButton.Click += (s, e) => MoveByGraticule(1.0);
            SingleUpButton.Click += (s, e) => MoveByGraticule(0.1);
            SingleDownButton.Click += (s, e) => MoveByGraticule(-0.1);
            DoubleDownButton.Click += (s, e) => MoveByGraticule(-1.0);
        }

        private void MoveByGraticule(double graticuleMultiplier)
        {
            double increment = GraticuleSize * graticuleMultiplier;
            double newValue = CurrentValue + increment;

            // Clamp to valid range
            newValue = Math.Max(MinValue, Math.Min(MaxValue, newValue));

            if (Math.Abs(newValue - CurrentValue) > 1e-12) // Only if actually changed
            {
                CurrentValue = newValue;
                UpdateDisplay();

                // Fire event
                GraticuleMovement?.Invoke(this, new GraticuleMovementEventArgs
                {
                    NewValue = newValue,
                    Increment = increment,
                    GraticuleMultiplier = graticuleMultiplier
                });
            }
        }

        public void SetValue(double value)
        {
            CurrentValue = Math.Max(MinValue, Math.Min(MaxValue, value));
            UpdateDisplay();
        }

        public void UpdateRange(double min, double max)
        {
            MinValue = min;
            MaxValue = max;
            CurrentValue = Math.Max(min, Math.Min(max, CurrentValue));
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (ValueDisplay != null)
            {
                string formattedValue = FormatValue(CurrentValue);
                ValueDisplay.Text = formattedValue;

                // Update button states based on range
                if (DoubleUpButton != null)
                    DoubleUpButton.IsEnabled = (CurrentValue + GraticuleSize) <= MaxValue;
                if (SingleUpButton != null)
                    SingleUpButton.IsEnabled = (CurrentValue + GraticuleSize * 0.1) <= MaxValue;
                if (SingleDownButton != null)
                    SingleDownButton.IsEnabled = (CurrentValue - GraticuleSize * 0.1) >= MinValue;
                if (DoubleDownButton != null)
                    DoubleDownButton.IsEnabled = (CurrentValue - GraticuleSize) >= MinValue;
            }
        }

        private string FormatValue(double value)
        {
            if (Units == "V")
            {
                if (Math.Abs(value) >= 1.0)
                    return $"{value:F3}V";
                else if (Math.Abs(value) >= 0.001)
                    return $"{value * 1000:F1}mV";
                else
                    return $"{value * 1000000:F1}μV";
            }
            else if (Units == "s")
            {
                if (Math.Abs(value) >= 1.0)
                    return $"{value:F3}s";
                else if (Math.Abs(value) >= 1e-3)
                    return $"{value * 1000:F3}ms";
                else if (Math.Abs(value) >= 1e-6)
                    return $"{value * 1000000:F3}μs";
                else if (Math.Abs(value) >= 1e-9)
                    return $"{value * 1000000000:F3}ns";
                else
                    return $"{value:E2}s";
            }
            else
            {
                return $"{value:F3}{Units}";
            }
        }
    }

    // Event args for graticule movement
    public class GraticuleMovementEventArgs : EventArgs
    {
        public double NewValue { get; set; }
        public double Increment { get; set; }
        public double GraticuleMultiplier { get; set; }
    }
}