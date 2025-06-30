// File: Controls/EmojiTimeBaseArrows.xaml.cs
// Enhanced with Orientation Support - can be Horizontal or Vertical
using System;
using System.Windows;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Controls
{
    /// <summary>
    /// Multimedia style emoji arrows with orientation support
    /// Supports both Horizontal (time base) and Vertical (voltage offset) orientations
    /// Uses rotated emoji symbols (⏮⏪⏹⏩⏭) for media player aesthetics
    /// </summary>
    public partial class EmojiTimeBaseArrows : UserControl
    {
        // Events for movement (compatible with existing GraticuleArrowControl interface)
        public event EventHandler<GraticuleMovementEventArgs> GraticuleMovement;

        #region Enums

        public enum ArrowOrientation
        {
            Horizontal,
            Vertical
        }

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty CurrentValueProperty =
            DependencyProperty.Register("CurrentValue", typeof(double),
                typeof(EmojiTimeBaseArrows), new PropertyMetadata(0.0, OnCurrentValueChanged));

        public static readonly DependencyProperty GraticuleSizeProperty =
            DependencyProperty.Register("GraticuleSize", typeof(double),
                typeof(EmojiTimeBaseArrows), new PropertyMetadata(1.0));

        public static readonly DependencyProperty UnitsProperty =
            DependencyProperty.Register("Units", typeof(string),
                typeof(EmojiTimeBaseArrows), new PropertyMetadata("s", OnUnitsChanged));

        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(double),
                typeof(EmojiTimeBaseArrows), new PropertyMetadata(-10.0));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(double),
                typeof(EmojiTimeBaseArrows), new PropertyMetadata(10.0));

        // NEW: Orientation support
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(ArrowOrientation),
                typeof(EmojiTimeBaseArrows), new PropertyMetadata(ArrowOrientation.Horizontal, OnOrientationChanged));

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

        public ArrowOrientation Orientation
        {
            get { return (ArrowOrientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        #endregion

        #region Constructor and Initialization

        public EmojiTimeBaseArrows()
        {
            InitializeComponent();
            SetupEventHandlers();
            UpdateDisplay();
            UpdateOrientation();
        }

        private void SetupEventHandlers()
        {
            // Wire up emoji button clicks
            LargeBackButton.Click += (s, e) => MoveByGraticule(-1.0, GetMovementType(true));
            SmallBackButton.Click += (s, e) => MoveByGraticule(-0.1, GetMovementType(true));
            ZeroButton.Click += (s, e) => MoveToZero();
            SmallForwardButton.Click += (s, e) => MoveByGraticule(0.1, GetMovementType(false));
            LargeForwardButton.Click += (s, e) => MoveByGraticule(1.0, GetMovementType(false));
        }

        #endregion

        #region Orientation Support

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EmojiTimeBaseArrows control)
            {
                control.UpdateOrientation();
                control.UpdateTooltips();
            }
        }

        private void UpdateOrientation()
        {
            if (OrientationTransform == null) return;

            switch (Orientation)
            {
                case ArrowOrientation.Horizontal:
                    OrientationTransform.Angle = 0;
                    break;
                case ArrowOrientation.Vertical:
                    OrientationTransform.Angle = 270; // Fixed: 270° instead of 90° for correct direction
                    break;
            }
        }

        private void UpdateTooltips()
        {
            if (Orientation == ArrowOrientation.Horizontal)
            {
                // Horizontal tooltips (time-based)
                LargeBackButton.ToolTip = "Large step backward (1 time scale)";
                SmallBackButton.ToolTip = "Small step backward (0.1 time scale)";
                ZeroButton.ToolTip = "Reset to zero offset";
                SmallForwardButton.ToolTip = "Small step forward (0.1 time scale)";
                LargeForwardButton.ToolTip = "Large step forward (1 time scale)";
            }
            else
            {
                // Vertical tooltips (voltage-based)
                LargeBackButton.ToolTip = "Large step down (1 voltage scale)";
                SmallBackButton.ToolTip = "Small step down (0.1 voltage scale)";
                ZeroButton.ToolTip = "Reset to zero voltage";
                SmallForwardButton.ToolTip = "Small step up (0.1 voltage scale)";
                LargeForwardButton.ToolTip = "Large step up (1 voltage scale)";
            }
        }

        private GraticuleMovementType GetMovementType(bool isBackward)
        {
            if (Orientation == ArrowOrientation.Horizontal)
            {
                return isBackward ? GraticuleMovementType.HorizontalLeft : GraticuleMovementType.HorizontalRight;
            }
            else
            {
                return isBackward ? GraticuleMovementType.VerticalDown : GraticuleMovementType.VerticalUp;
            }
        }

        #endregion

        #region Property Change Handlers

        private static void OnCurrentValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EmojiTimeBaseArrows control)
            {
                control.UpdateDisplay();
                control.UpdateButtonStates();
            }
        }

        private static void OnUnitsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EmojiTimeBaseArrows control)
            {
                control.UpdateDisplay();
            }
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
        /// Reset to zero
        /// </summary>
        private void MoveToZero()
        {
            if (Math.Abs(CurrentValue) > 1e-12) // Only if not already zero
            {
                double oldValue = CurrentValue;
                CurrentValue = 0.0;
                UpdateDisplay();
                UpdateButtonStates();

                // Fire event
                GraticuleMovement?.Invoke(this, new GraticuleMovementEventArgs
                {
                    NewValue = 0.0,
                    Increment = -oldValue,
                    GraticuleMultiplier = 0,
                    MovementType = GraticuleMovementType.Zero
                });
            }
        }

        #endregion

        #region Display Updates

        /// <summary>
        /// Update the value display
        /// </summary>
        private void UpdateDisplay()
        {
            if (ValueDisplay != null)
            {
                string formattedValue = FormatValue(CurrentValue);
                ValueDisplay.Text = formattedValue;
            }

            if (RangeDisplay != null)
            {
                string minFormatted = FormatValue(MinValue);
                string maxFormatted = FormatValue(MaxValue);

                if (Math.Abs(MinValue) == Math.Abs(MaxValue))
                {
                    RangeDisplay.Text = $"Range: ±{maxFormatted.Replace("+", "")}";
                }
                else
                {
                    RangeDisplay.Text = $"Range: {minFormatted} to {maxFormatted}";
                }
            }
        }

        /// <summary>
        /// Format value with appropriate precision and units
        /// </summary>
        private string FormatValue(double value)
        {
            string sign = value >= 0 ? "+" : "";

            if (Units == "V" || Units == "v")
            {
                // Voltage formatting
                if (Math.Abs(value) >= 1000)
                    return $"{sign}{value / 1000:F2}kV";
                else if (Math.Abs(value) >= 1)
                    return $"{sign}{value:F3}V";
                else if (Math.Abs(value) >= 0.001)
                    return $"{sign}{value * 1000:F1}mV";
                else
                    return $"{sign}{value * 1000000:F0}µV";
            }
            else if (Units == "s" || Units == "S")
            {
                // Time formatting
                if (Math.Abs(value) >= 1)
                    return $"{sign}{value:F3}s";
                else if (Math.Abs(value) >= 0.001)
                    return $"{sign}{value * 1000:F1}ms";
                else if (Math.Abs(value) >= 0.000001)
                    return $"{sign}{value * 1000000:F1}µs";
                else
                    return $"{sign}{value * 1000000000:F0}ns";
            }
            else
            {
                // Generic formatting
                return $"{sign}{value:F3}{Units}";
            }
        }

        /// <summary>
        /// Update button enabled states based on current value and limits
        /// </summary>
        private void UpdateButtonStates()
        {
            if (LargeBackButton != null)
                LargeBackButton.IsEnabled = CurrentValue > MinValue;

            if (SmallBackButton != null)
                SmallBackButton.IsEnabled = CurrentValue > MinValue;

            if (LargeForwardButton != null)
                LargeForwardButton.IsEnabled = CurrentValue < MaxValue;

            if (SmallForwardButton != null)
                SmallForwardButton.IsEnabled = CurrentValue < MaxValue;
        }

        #endregion

        #region Public API Methods (for compatibility with existing code)

        /// <summary>
        /// Update the range of the control (compatibility method)
        /// </summary>
        public void UpdateRange(double min, double max)
        {
            MinValue = min;
            MaxValue = max;
            // Clamp current value to new range
            if (CurrentValue < min) CurrentValue = min;
            if (CurrentValue > max) CurrentValue = max;
            UpdateDisplay();
            UpdateButtonStates();
        }

        /// <summary>
        /// Set the current value (compatibility method)
        /// </summary>
        public void SetValue(double value)
        {
            CurrentValue = Math.Max(MinValue, Math.Min(MaxValue, value));
            UpdateDisplay();
            UpdateButtonStates();
        }

        #endregion
    }

    // Note: Using existing GraticuleMovementEventArgs and GraticuleMovementType 
    // from Controls/GraticuleMovementEventArgs.cs
}