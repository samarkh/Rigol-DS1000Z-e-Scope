// File: Controls/EmojiTimeBaseArrows.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Controls
{
    /// <summary>
    /// Multimedia style emoji arrows for TimeBase horizontal offset control
    /// Uses rotated emoji symbols (⏮⏪⏹⏩⏭) for media player aesthetics
    /// </summary>
    public partial class EmojiTimeBaseArrows : UserControl
    {
        // Events for movement (compatible with existing GraticuleArrowControl interface)
        public event EventHandler<GraticuleMovementEventArgs> GraticuleMovement;

        #region Dependency Properties

        public static readonly DependencyProperty CurrentValueProperty =
            DependencyProperty.Register("CurrentValue", typeof(double),
                typeof(EmojiTimeBaseArrows), new PropertyMetadata(0.0, OnCurrentValueChanged));

        public static readonly DependencyProperty GraticuleSizeProperty =
            DependencyProperty.Register("GraticuleSize", typeof(double),
                typeof(EmojiTimeBaseArrows), new PropertyMetadata(1.0));

        public static readonly DependencyProperty UnitsProperty =
            DependencyProperty.Register("Units", typeof(string),
                typeof(EmojiTimeBaseArrows), new PropertyMetadata("s"));

        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(double),
                typeof(EmojiTimeBaseArrows), new PropertyMetadata(-10.0));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(double),
                typeof(EmojiTimeBaseArrows), new PropertyMetadata(10.0));

        #endregion

        #region Properties

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

        #endregion

        #region Constructor and Initialization

        public EmojiTimeBaseArrows()
        {
            InitializeComponent();
            SetupEventHandlers();
            UpdateDisplay();
        }

        private void SetupEventHandlers()
        {
            // Wire up emoji button clicks
            LargeBackButton.Click += (s, e) => MoveByGraticule(-1.0, GraticuleMovementType.HorizontalLeft);
            SmallBackButton.Click += (s, e) => MoveByGraticule(-0.1, GraticuleMovementType.HorizontalLeft);
            ZeroButton.Click += (s, e) => MoveToZero();
            SmallForwardButton.Click += (s, e) => MoveByGraticule(0.1, GraticuleMovementType.HorizontalRight);
            LargeForwardButton.Click += (s, e) => MoveByGraticule(1.0, GraticuleMovementType.HorizontalRight);
        }

        #endregion

        #region Movement Methods

        /// <summary>
        /// Move by specified graticule multiplier
        /// </summary>
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
                UpdateButtonStates();

                // Fire event with compatible interface
                GraticuleMovement?.Invoke(this, new GraticuleMovementEventArgs
                {
                    NewValue = newValue,
                    Increment = increment,
                    GraticuleMultiplier = graticuleMultiplier,
                    MovementType = movementType
                });
            }
        }

        /// <summary>
        /// Move to zero position (stop button)
        /// </summary>
        private void MoveToZero()
        {
            double newValue = 0.0;

            // Clamp to valid range (in case zero is outside range)
            newValue = Math.Max(MinValue, Math.Min(MaxValue, newValue));

            if (Math.Abs(CurrentValue - newValue) > 1e-12)
            {
                double increment = newValue - CurrentValue;
                CurrentValue = newValue;
                UpdateDisplay();
                UpdateButtonStates();

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

        #endregion

        #region Public API (Compatible with GraticuleArrowControl)

        /// <summary>
        /// Set value programmatically
        /// </summary>
        public void SetValue(double value)
        {
            CurrentValue = Math.Max(MinValue, Math.Min(MaxValue, value));
            UpdateDisplay();
            UpdateButtonStates();
        }

        /// <summary>
        /// Update range programmatically
        /// </summary>
        public void UpdateRange(double min, double max)
        {
            MinValue = min;
            MaxValue = max;
            CurrentValue = Math.Max(min, Math.Min(max, CurrentValue));
            UpdateDisplay();
            UpdateButtonStates();
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// Update the display when current value changes
        /// </summary>
        private static void OnCurrentValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EmojiTimeBaseArrows control)
            {
                control.UpdateDisplay();
                control.UpdateButtonStates();
            }
        }

        /// <summary>
        /// Update the value display and range text
        /// </summary>
        private void UpdateDisplay()
        {
            if (ValueDisplay != null)
            {
                ValueDisplay.Text = FormatTime(CurrentValue);
            }

            if (RangeDisplay != null)
            {
                if (Math.Abs(MinValue) == Math.Abs(MaxValue))
                {
                    RangeDisplay.Text = $"Range: ±{FormatTime(Math.Abs(MaxValue))}";
                }
                else
                {
                    RangeDisplay.Text = $"Range: {FormatTime(MinValue)} to {FormatTime(MaxValue)}";
                }
            }
        }

        /// <summary>
        /// Update button enabled states based on current value and range
        /// </summary>
        private void UpdateButtonStates()
        {
            if (LargeBackButton != null)
                LargeBackButton.IsEnabled = (CurrentValue - GraticuleSize) >= MinValue;

            if (SmallBackButton != null)
                SmallBackButton.IsEnabled = (CurrentValue - GraticuleSize * 0.1) >= MinValue;

            if (SmallForwardButton != null)
                SmallForwardButton.IsEnabled = (CurrentValue + GraticuleSize * 0.1) <= MaxValue;

            if (LargeForwardButton != null)
                LargeForwardButton.IsEnabled = (CurrentValue + GraticuleSize) <= MaxValue;

            // Zero button is always enabled if zero is within range
            if (ZeroButton != null)
                ZeroButton.IsEnabled = (0.0 >= MinValue && 0.0 <= MaxValue);
        }

        /// <summary>
        /// Format time values with appropriate units
        /// </summary>
        private string FormatTime(double timeValue)
        {
            if (timeValue == 0) return "0s";

            double absTime = Math.Abs(timeValue);

            if (absTime >= 1.0)
                return $"{timeValue:F3}s";
            else if (absTime >= 1e-3)
                return $"{timeValue * 1000:F3}ms";
            else if (absTime >= 1e-6)
                return $"{timeValue * 1000000:F3}μs";
            else if (absTime >= 1e-9)
                return $"{timeValue * 1000000000:F3}ns";
            else
                return $"{timeValue:E2}s";
        }

        #endregion
    }
}