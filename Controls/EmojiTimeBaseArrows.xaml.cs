// File: Controls/MediaPlayerArrows.xaml.cs
// UNIFIED: Media player style control for ALL sections (TimeBase, Ch1, Ch2, Trigger)

using System;
using System.Windows;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Controls
{
    public enum MediaPlayerOrientation
    {
        Horizontal,  // For TimeBase (⏪ ◀ ⏹ ▶ ⏩)
        Vertical     // For Ch1, Ch2, Trigger (rotated 90°)
    }

    /// <summary>
    /// Unified media player style arrows for all controls
    /// Can be horizontal (TimeBase) or vertical (Ch1, Ch2, Trigger)
    /// </summary>
    public partial class MediaPlayerArrows : UserControl
    {
        // Events for movement (compatible with existing interfaces)
        public event EventHandler<GraticuleMovementEventArgs> GraticuleMovement;

        #region Dependency Properties

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(MediaPlayerOrientation),
                typeof(MediaPlayerArrows), new PropertyMetadata(MediaPlayerOrientation.Horizontal, OnOrientationChanged));

        public static readonly DependencyProperty CurrentValueProperty =
            DependencyProperty.Register("CurrentValue", typeof(double),
                typeof(MediaPlayerArrows), new PropertyMetadata(0.0, OnCurrentValueChanged));

        public static readonly DependencyProperty GraticuleSizeProperty =
            DependencyProperty.Register("GraticuleSize", typeof(double),
                typeof(MediaPlayerArrows), new PropertyMetadata(1.0));

        public static readonly DependencyProperty UnitsProperty =
            DependencyProperty.Register("Units", typeof(string),
                typeof(MediaPlayerArrows), new PropertyMetadata("V"));

        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(double),
                typeof(MediaPlayerArrows), new PropertyMetadata(-10.0));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(double),
                typeof(MediaPlayerArrows), new PropertyMetadata(10.0));

        #endregion

        #region Properties

        public MediaPlayerOrientation Orientation
        {
            get { return (MediaPlayerOrientation)GetValue(OrientationProperty); }
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

        #endregion

        #region Constructor and Initialization

        public MediaPlayerArrows()
        {
            InitializeComponent();
            SetupEventHandlers();
            UpdateDisplay();
            UpdateOrientation();
        }

        private void SetupEventHandlers()
        {
            // Wire up button clicks with appropriate movement types
            LargeBackButton.Click += (s, e) => MoveByGraticule(-1.0, GetBackwardMovementType(true));
            SmallBackButton.Click += (s, e) => MoveByGraticule(-0.1, GetBackwardMovementType(false));
            ZeroButton.Click += (s, e) => MoveToZero();
            SmallForwardButton.Click += (s, e) => MoveByGraticule(0.1, GetForwardMovementType(false));
            LargeForwardButton.Click += (s, e) => MoveByGraticule(1.0, GetForwardMovementType(true));
        }

        #endregion

        #region Orientation Handling

        /// <summary>
        /// Handle orientation changes (Horizontal vs Vertical)
        /// </summary>
        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MediaPlayerArrows control)
            {
                control.UpdateOrientation();
                control.UpdateTooltips();
            }
        }

        /// <summary>
        /// Update the rotation based on orientation
        /// </summary>
        private void UpdateOrientation()
        {
            if (ButtonRotation != null)
            {
                // Rotate the entire button panel 90° for vertical controls
                ButtonRotation.Angle = Orientation == MediaPlayerOrientation.Vertical ? 90 : 0;
            }
        }

        /// <summary>
        /// Update tooltips based on orientation
        /// </summary>
        private void UpdateTooltips()
        {
            if (Orientation == MediaPlayerOrientation.Horizontal)
            {
                // TimeBase - horizontal movement
                if (LargeBackButton != null) LargeBackButton.ToolTip = "Large step backward (1 time scale)";
                if (SmallBackButton != null) SmallBackButton.ToolTip = "Small step backward (0.1 time scale)";
                if (ZeroButton != null) ZeroButton.ToolTip = "Reset to zero offset";
                if (SmallForwardButton != null) SmallForwardButton.ToolTip = "Small step forward (0.1 time scale)";
                if (LargeForwardButton != null) LargeForwardButton.ToolTip = "Large step forward (1 time scale)";
            }
            else
            {
                // Ch1, Ch2, Trigger - vertical movement
                if (LargeBackButton != null) LargeBackButton.ToolTip = "Large step down (1 scale)";
                if (SmallBackButton != null) SmallBackButton.ToolTip = "Small step down (0.1 scale)";
                if (ZeroButton != null) ZeroButton.ToolTip = "Reset to zero";
                if (SmallForwardButton != null) SmallForwardButton.ToolTip = "Small step up (0.1 scale)";
                if (LargeForwardButton != null) LargeForwardButton.ToolTip = "Large step up (1 scale)";
            }
        }

        #endregion

        #region Movement Methods

        /// <summary>
        /// Get appropriate movement type for backward direction
        /// </summary>
        private GraticuleMovementType GetBackwardMovementType(bool isLarge)
        {
            if (Orientation == MediaPlayerOrientation.Horizontal)
            {
                return GraticuleMovementType.HorizontalLeft;
            }
            else
            {
                return isLarge ? GraticuleMovementType.LargeDown : GraticuleMovementType.SmallDown;
            }
        }

        /// <summary>
        /// Get appropriate movement type for forward direction
        /// </summary>
        private GraticuleMovementType GetForwardMovementType(bool isLarge)
        {
            if (Orientation == MediaPlayerOrientation.Horizontal)
            {
                return GraticuleMovementType.HorizontalRight;
            }
            else
            {
                return isLarge ? GraticuleMovementType.LargeUp : GraticuleMovementType.SmallUp;
            }
        }

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

        #region Public API (Compatible with existing controls)

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
            if (d is MediaPlayerArrows control)
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
                ValueDisplay.Text = FormatValue(CurrentValue);
            }

            if (RangeDisplay != null)
            {
                if (Math.Abs(MinValue) == Math.Abs(MaxValue))
                {
                    RangeDisplay.Text = $"Range: ±{FormatValue(Math.Abs(MaxValue))}";
                }
                else
                {
                    RangeDisplay.Text = $"Range: {FormatValue(MinValue)} to {FormatValue(MaxValue)}";
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
        /// Format values with appropriate units (voltage or time)
        /// </summary>
        private string FormatValue(double value)
        {
            if (Units == "s")
            {
                return FormatTime(value);
            }
            else
            {
                return FormatVoltage(value);
            }
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

        /// <summary>
        /// Format voltage values with appropriate units
        /// </summary>
        private string FormatVoltage(double voltage)
        {
            if (Math.Abs(voltage) >= 1.0)
                return $"{voltage:F3}V";
            else if (Math.Abs(voltage) >= 0.001)
                return $"{voltage * 1000:F1}mV";
            else
                return $"{voltage * 1000000:F1}μV";
        }

        #endregion
    }
}