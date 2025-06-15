// Enhanced single control that handles both orientations
// File: Controls/GraticuleArrowControl.xaml.cs
// File: Controls/GraticuleArrowControl.xaml.cs
// FIXED: Updated to properly set MovementType in events

using System;
using System.Windows;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Controls
{
    public enum ArrowControlOrientation
    {
        Vertical,    // For channels, trigger (⇈ ↑ ↓ ⇊)
        Horizontal   // For timebase (⏪ ◀️ ▶️ ⏩)
    }

    public partial class GraticuleArrowControl : UserControl
    {
        // Events for movement
        public event EventHandler<GraticuleMovementEventArgs> GraticuleMovement;

        // Dependency Properties
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(ArrowControlOrientation),
                typeof(GraticuleArrowControl), new PropertyMetadata(ArrowControlOrientation.Vertical, OnOrientationChanged));

        public static readonly DependencyProperty CurrentValueProperty =
            DependencyProperty.Register("CurrentValue", typeof(double),
                typeof(GraticuleArrowControl), new PropertyMetadata(0.0));

        public static readonly DependencyProperty GraticuleSizeProperty =
            DependencyProperty.Register("GraticuleSize", typeof(double),
                typeof(GraticuleArrowControl), new PropertyMetadata(1.0));

        public static readonly DependencyProperty UnitsProperty =
            DependencyProperty.Register("Units", typeof(string),
                typeof(GraticuleArrowControl), new PropertyMetadata("V"));

        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(double),
                typeof(GraticuleArrowControl), new PropertyMetadata(-10.0));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(double),
                typeof(GraticuleArrowControl), new PropertyMetadata(10.0));

        // Properties
        public ArrowControlOrientation Orientation
        {
            get { return (ArrowControlOrientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public double CurrentValue
        {
            get { return (double)GetValue(CurrentValueProperty); }
            set { SetValue(CurrentValueProperty, value); }
        }

        public double GraticuleSize
        {
            get { return (double)GetValue(GraticuleSizeProperty); }
            set { SetValue(GraticuleSizeProperty, value); }
        }

        public string Units
        {
            get { return (string)GetValue(UnitsProperty); }
            set { SetValue(UnitsProperty, value); }
        }

        public double MinValue
        {
            get { return (double)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        public double MaxValue
        {
            get { return (double)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        // UI elements
        private Button button1, button2, button3, button4;
        private TextBlock valueDisplay;

        public GraticuleArrowControl()
        {
            InitializeComponent();
            LoadedControls();
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as GraticuleArrowControl;
            control?.UpdateButtonLayout();
        }

        private void LoadedControls()
        {
            // Find controls in the template
            button1 = FindName("Button1") as Button;
            button2 = FindName("Button2") as Button;
            button3 = FindName("Button3") as Button;
            button4 = FindName("Button4") as Button;
            valueDisplay = FindName("ValueDisplay") as TextBlock;

            UpdateButtonLayout();
            UpdateDisplay();
        }

        private void UpdateButtonLayout()
        {
            if (Orientation == ArrowControlOrientation.Vertical)
            {
                // FIXED: Wire up buttons with proper MovementType mapping
                // Vertical: button1=LargeUp, button2=SmallUp, button3=SmallDown, button4=LargeDown
                if (button1 != null) button1.Click += (s, e) => MoveByGraticule(1.0, GraticuleMovementType.LargeUp);
                if (button2 != null) button2.Click += (s, e) => MoveByGraticule(0.1, GraticuleMovementType.SmallUp);
                if (button3 != null) button3.Click += (s, e) => MoveByGraticule(-0.1, GraticuleMovementType.SmallDown);
                if (button4 != null) button4.Click += (s, e) => MoveByGraticule(-1.0, GraticuleMovementType.LargeDown);
            }
            else
            {
                // Horizontal: button1=back1, button2=back0.1, button3=forward0.1, button4=forward1
                if (button1 != null) button1.Click += (s, e) => MoveByGraticule(-1.0, GraticuleMovementType.HorizontalLeft);
                if (button2 != null) button2.Click += (s, e) => MoveByGraticule(-0.1, GraticuleMovementType.HorizontalLeft);
                if (button3 != null) button3.Click += (s, e) => MoveByGraticule(0.1, GraticuleMovementType.HorizontalRight);
                if (button4 != null) button4.Click += (s, e) => MoveByGraticule(1.0, GraticuleMovementType.HorizontalRight);
            }
        }

        // FIXED: Updated to include MovementType parameter
        private void MoveByGraticule(double graticuleMultiplier, GraticuleMovementType movementType)
        {
            double increment = GraticuleSize * graticuleMultiplier;
            double newValue = CurrentValue + increment;

            // Clamp to valid range
            newValue = Math.Max(MinValue, Math.Min(MaxValue, newValue));

            if (Math.Abs(newValue - CurrentValue) > 1e-12) // Only if actually changed
            {
                CurrentValue = newValue;
                UpdateDisplay();

                // FIXED: Fire event with MovementType property set
                GraticuleMovement?.Invoke(this, new GraticuleMovementEventArgs
                {
                    NewValue = newValue,
                    Increment = increment,
                    GraticuleMultiplier = graticuleMultiplier,
                    MovementType = movementType  // ADDED: This was missing
                });
            }
        }

        // ADDED: Method to trigger zero movement
        public void TriggerZeroMovement()
        {
            double newValue = 0.0;

            // Clamp to valid range
            newValue = Math.Max(MinValue, Math.Min(MaxValue, newValue));

            if (Math.Abs(CurrentValue - newValue) > 1e-12)
            {
                double increment = newValue - CurrentValue;
                CurrentValue = newValue;
                UpdateDisplay();

                // Fire event with Zero movement type
                GraticuleMovement?.Invoke(this, new GraticuleMovementEventArgs
                {
                    NewValue = newValue,
                    Increment = increment,
                    GraticuleMultiplier = 0.0,
                    MovementType = GraticuleMovementType.Zero
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
            if (valueDisplay != null)
            {
                string formattedValue = FormatValue(CurrentValue);
                valueDisplay.Text = formattedValue;
                UpdateButtonStates();
            }
        }

        private void UpdateButtonStates()
        {
            if (Orientation == ArrowControlOrientation.Vertical)
            {
                // Vertical: button1=up1, button2=up0.1, button3=down0.1, button4=down1
                if (button1 != null) button1.IsEnabled = (CurrentValue + GraticuleSize) <= MaxValue;
                if (button2 != null) button2.IsEnabled = (CurrentValue + GraticuleSize * 0.1) <= MaxValue;
                if (button3 != null) button3.IsEnabled = (CurrentValue - GraticuleSize * 0.1) >= MinValue;
                if (button4 != null) button4.IsEnabled = (CurrentValue - GraticuleSize) >= MinValue;
            }
            else
            {
                // Horizontal: button1=back1, button2=back0.1, button3=forward0.1, button4=forward1
                if (button1 != null) button1.IsEnabled = (CurrentValue - GraticuleSize) >= MinValue;
                if (button2 != null) button2.IsEnabled = (CurrentValue - GraticuleSize * 0.1) >= MinValue;
                if (button3 != null) button3.IsEnabled = (CurrentValue + GraticuleSize * 0.1) <= MaxValue;
                if (button4 != null) button4.IsEnabled = (CurrentValue + GraticuleSize) <= MaxValue;
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
                if (value == 0) return "0s";

                double absTime = Math.Abs(value);
                if (absTime >= 1.0)
                    return $"{value:F3}s";
                else if (absTime >= 1e-3)
                    return $"{value * 1000:F3}ms";
                else if (absTime >= 1e-6)
                    return $"{value * 1000000:F3}μs";
                else if (absTime >= 1e-9)
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
}