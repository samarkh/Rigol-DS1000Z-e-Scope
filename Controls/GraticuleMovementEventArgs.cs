
// Add this enum to your Controls namespace or create a new file
// File: Controls/GraticuleMovementEventArgs.cs
using System;

namespace DS1000Z_E_USB_Control.Controls
{
    /// <summary>
    /// FIXED: Complete enum with all required movement types
    /// </summary>
    public enum GraticuleMovementType
    {
        // Basic directional movements
        VerticalUp,
        VerticalDown,
        HorizontalLeft,
        HorizontalRight,

        // ADDED: Missing fine/coarse movement types
        LargeUp,     // Large upward movement (1 graticule)
        SmallUp,     // Small upward movement (0.1 graticule)
        SmallDown,   // Small downward movement (0.1 graticule)
        LargeDown,   // Large downward movement (1 graticule)
        Zero         // Reset to zero
    }

    /// <summary>
    /// FIXED: Event args with MovementType property added
    /// </summary>
    public class GraticuleMovementEventArgs : EventArgs
    {
        public double NewValue { get; set; }
        public double Increment { get; set; }
        public double GraticuleMultiplier { get; set; }

        // ADDED: Missing MovementType property that Ch1ControlPanel expects
        public GraticuleMovementType MovementType { get; set; }
    }
}
