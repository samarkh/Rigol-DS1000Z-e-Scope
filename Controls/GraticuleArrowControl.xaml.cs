// Enhanced single control that handles both orientations
// File: Controls/GraticuleArrowControl.xaml.cs

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

        // UI Elements (will be created dynamically)
        private Button button1, button2, button3, button4;
        private TextBlock valueDisplay;
        private Panel mainPanel;

        public GraticuleArrowControl()
        {
            InitializeComponent();
            CreateUI();
            SetupEventHandlers();
            UpdateDisplay();
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (GraticuleArrowControl)d;
            control.CreateUI();
            control.SetupEventHandlers();
            control.UpdateDisplay();
        }

        private void CreateUI()
        {
            // Clear existing content
            this.Content = null;

            if (Orientation == ArrowControlOrientation.Vertical)
            {
                CreateVerticalUI();
            }
            else
            {
                CreateHorizontalUI();
            }
        }

        private void CreateVerticalUI()
        {
            var stackPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical };

            // Double Up Arrow (1 graticule)
            button1 = new Button
            {
                Width = 40,
                Height = 25,
                Margin = new Thickness(2),
                Content = "⇈",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                ToolTip = "Move up 1 graticule"
            };

            // Single Up Arrow (0.1 graticule)
            button2 = new Button
            {
                Width = 40,
                Height = 20,
                Margin = new Thickness(2),
                Content = "↑",
                FontSize = 12,
                ToolTip = "Move up 0.1 graticule"
            };

            // Value Display
            valueDisplay = new TextBlock
            {
                Width = 40,
                Height = 20,
                Margin = new Thickness(2),
                Text = "0.000V",
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = System.Windows.Media.Brushes.LightGray
            };

            // Single Down Arrow (0.1 graticule)
            button3 = new Button
            {
                Width = 40,
                Height = 20,
                Margin = new Thickness(2),
                Content = "↓",
                FontSize = 12,
                ToolTip = "Move down 0.1 graticule"
            };

            // Double Down Arrow (1 graticule)
            button4 = new Button
            {
                Width = 40,
                Height = 25,
                Margin = new Thickness(2),
                Content = "⇊",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                ToolTip = "Move down 1 graticule"
            };

            stackPanel.Children.Add(button1);
            stackPanel.Children.Add(button2);
            stackPanel.Children.Add(valueDisplay);
            stackPanel.Children.Add(button3);
            stackPanel.Children.Add(button4);

            mainPanel = stackPanel;
            this.Content = stackPanel;
        }

        private void CreateHorizontalUI()
        {
            var stackPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

            // Rewind Button (1 graticule backward)
            button1 = new Button
            {
                Width = 30,
                Height = 40,
                Margin = new Thickness(2),
                Content = "⏪",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                ToolTip = "Rewind 1 graticule in time"
            };

            // Previous Button (0.1 graticule backward)
            button2 = new Button
            {
                Width = 25,
                Height = 40,
                Margin = new Thickness(2),
                Content = "◀️",
                FontSize = 14,
                ToolTip = "Step backward 0.1 graticule in time"
            };

            // Value Display
            valueDisplay = new TextBlock
            {
                Width = 60,
                Height = 40,
                Margin = new Thickness(2),
                Text = "0.000s",
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = System.Windows.Media.Brushes.LightGray
            };

            // Next Button (0.1 graticule forward)
            button3 = new Button
            {
                Width = 25,
                Height = 40,
                Margin = new Thickness(2),
                Content = "▶️",
                FontSize = 14,
                ToolTip = "Step forward 0.1 graticule in time"
            };

            // Fast Forward Button (1 graticule forward)
            button4 = new Button
            {
                Width = 30,
                Height = 40,
                Margin = new Thickness(2),
                Content = "⏩",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                ToolTip = "Fast forward 1 graticule in time"
            };

            stackPanel.Children.Add(button1);
            stackPanel.Children.Add(button2);
            stackPanel.Children.Add(valueDisplay);
            stackPanel.Children.Add(button3);
            stackPanel.Children.Add(button4);

            mainPanel = stackPanel;
            this.Content = stackPanel;
        }

        private void SetupEventHandlers()
        {
            if (button1 != null) button1.Click -= Button_Click;
            if (button2 != null) button2.Click -= Button_Click;
            if (button3 != null) button3.Click -= Button_Click;
            if (button4 != null) button4.Click -= Button_Click;

            if (Orientation == ArrowControlOrientation.Vertical)
            {
                // Vertical: button1=up1, button2=up0.1, button3=down0.1, button4=down1
                if (button1 != null) button1.Click += (s, e) => MoveByGraticule(1.0);
                if (button2 != null) button2.Click += (s, e) => MoveByGraticule(0.1);
                if (button3 != null) button3.Click += (s, e) => MoveByGraticule(-0.1);
                if (button4 != null) button4.Click += (s, e) => MoveByGraticule(-1.0);
            }
            else
            {
                // Horizontal: button1=back1, button2=back0.1, button3=forward0.1, button4=forward1
                if (button1 != null) button1.Click += (s, e) => MoveByGraticule(-1.0);
                if (button2 != null) button2.Click += (s, e) => MoveByGraticule(-0.1);
                if (button3 != null) button3.Click += (s, e) => MoveByGraticule(0.1);
                if (button4 != null) button4.Click += (s, e) => MoveByGraticule(1.0);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // This is just to remove old handlers
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
            if (valueDisplay != null)
            {
                string formattedValue = FormatValue(CurrentValue);
                valueDisplay.Text = formattedValue;

                // Update button states based on range and orientation
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

    // Event args for graticule movement (same as before)
    public class GraticuleMovementEventArgs : EventArgs
    {
        public double NewValue { get; set; }
        public double Increment { get; set; }
        public double GraticuleMultiplier { get; set; }
    }
}