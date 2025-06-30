// File: Controls/EmojiArrows.xaml.cs
// Clean emoji arrow control designed for rotation (0° or 270°)
using System;
using System.Windows;
using System.Windows.Controls;

namespace DS1000Z_E_USB_Control.Controls
{
    /// <summary>
    /// Clean emoji arrows control designed for rotation
    /// Supports 0° (horizontal) and 270° (vertical) orientations
    /// </summary>
    public partial class EmojiArrows : UserControl
    {
        // Events for movement
        public event EventHandler<GraticuleMovementEventArgs> GraticuleMovement;

        #region Dependency Properties

        public static readonly DependencyProperty CurrentValueProperty =
            DependencyProperty.Register("CurrentValue", typeof(double),
                typeof(EmojiArrows), new PropertyMetadata(0.0));

        public static readonly DependencyProperty GraticuleSizeProperty =
            DependencyProperty.Register("GraticuleSize", typeof(double),
                typeof(EmojiArrows), new PropertyMetadata(1.0));

        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(double),
                typeof(EmojiArrows), new PropertyMetadata(-10.0));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(double),
                typeof(EmojiArrows), new PropertyMetadata(10.0));

        public static readonly DependencyProperty RotationAngleProperty =
            DependencyProperty.Register("RotationAngle", typeof(double),
                typeof(EmojiArrows), new PropertyMetadata(0.0, OnRotationAngleChanged));

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

        public double RotationAngle
        {
            get { return (double)GetValue(RotationAngleProperty); }
            set { SetValue(RotationAngleProperty, value); }
        }

        #endregion

        #region Constructor and Initialization

        public EmojiArrows()
        {
            InitializeComponent();
            SetupEventHandlers();
            UpdateRotation();
        }

        private void SetupEventHandlers()
        {
            // Wire up button clicks
            LargeBackButton.Click += (s, e) => MoveByGraticule(-1.0, GetMovementType(true, true));
            SmallBackButton.Click += (s, e) => MoveByGraticule(-0.1, GetMovementType(true, false));
            ZeroButton.Click += (s, e) => MoveToZero();
            SmallForwardButton.Click += (s, e) => MoveByGraticule(0.1, GetMovementType(false, false));
            LargeForwardButton.Click += (s, e) => MoveByGraticule(1.0, GetMovementType(false, true));
        }

        #endregion

        #region Rotation Support

        private static void OnRotationAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EmojiArrows control)
            {
                control.UpdateRotation();
                control.UpdateTooltips();
            }
        }

        private void UpdateRotation()
        {
            if (RotationTransform != null)
            {
                RotationTransform.Angle = RotationAngle;
            }
        }

        private void UpdateTooltips()
        {
            bool isVertical = Math.Abs(RotationAngle - 270) < 1; // Check if approximately 270°

            if (isVertical)
            {
                // Vertical orientation (270°) - voltage controls
                LargeBackButton.ToolTip = "Large step up (+1 scale)";
                SmallBackButton.ToolTip = "Small step up (+0.1 scale)";
                ZeroButton.ToolTip = "Reset to zero";
                SmallForwardButton.ToolTip = "Small step down (-0.1 scale)";
                LargeForwardButton.ToolTip = "Large step down (-1 scale)";
            }
            else
            {
                // Horizontal orientation (0°) - time controls
                LargeBackButton.ToolTip = "Large step backward (-1 scale)";
                SmallBackButton.ToolTip = "Small step backward (-0.1 scale)";
                ZeroButton.ToolTip = "Reset to zero";
                SmallForwardButton.ToolTip = "Small step forward (+0.1 scale)";
                LargeForwardButton.ToolTip = "Large step forward (+1 scale)";
            }
        }

        private GraticuleMovementType GetMovementType(bool isBackward, bool isLarge)
        {
            bool isVertical = Math.Abs(RotationAngle - 270) < 1; // Check if approximately 270°

            if (isVertical)
            {
                // For vertical (270°), "backward" means UP, "forward" means DOWN
                if (isBackward)
                {
                    return isLarge ? GraticuleMovementType.LargeUp : GraticuleMovementType.SmallUp;
                }
                else
                {
                    return isLarge ? GraticuleMovementType.LargeDown : GraticuleMovementType.SmallDown;
                }
            }
            else
            {
                // For horizontal (0°), normal left/right movement
                if (isBackward)
                {
                    return GraticuleMovementType.HorizontalLeft;
                }
                else
                {
                    return GraticuleMovementType.HorizontalRight;
                }
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
                UpdateButtonStates();

                // Fire event
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

        #region Public API Methods

        /// <summary>
        /// Update the range of the control
        /// </summary>
        public void UpdateRange(double min, double max)
        {
            MinValue = min;
            MaxValue = max;
            // Clamp current value to new range
            if (CurrentValue < min) CurrentValue = min;
            if (CurrentValue > max) CurrentValue = max;
            UpdateButtonStates();
        }

        /// <summary>
        /// Set the current value
        /// </summary>
        public void SetValue(double value)
        {
            CurrentValue = Math.Max(MinValue, Math.Min(MaxValue, value));
            UpdateButtonStates();
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
    }
}