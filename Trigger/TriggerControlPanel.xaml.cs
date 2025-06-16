// Updated sections for TriggerControlPanel.xaml.cs to support multimedia arrow controls
// Replace the existing WireUpAdditionalControls() method with this version:

/// <summary>
/// Wire up additional controls not handled by the base controller
/// </summary>
private void WireUpAdditionalControls()
{
    if (ForceTriggerButton != null)
    {
        ForceTriggerButton.Click += ForceTrigger_Click;
    }

    if (QuickZeroLevelButton != null)
    {
        QuickZeroLevelButton.Click += QuickZeroLevel_Click;
    }

    // REPLACED: Remove slider event handler and replace with arrow control handler
    if (TriggerLevelArrows != null)
    {
        TriggerLevelArrows.GraticuleMovement += TriggerLevelArrows_GraticuleMovement;
    }

    if (HoldoffTextBox != null)
    {
        HoldoffTextBox.TextChanged += HoldoffTextBox_TextChanged;
        HoldoffTextBox.LostFocus += HoldoffTextBox_LostFocus;
    }

    // Subscribe to settings changes to update arrow control and range displays
    if (controller != null)
    {
        controller.SettingsChanged += (sender, e) =>
        {
            UpdateRangeDisplays();
            UpdateTriggerLevelArrowControl();
        };
    }
}

// ADD these new methods to TriggerControlPanel.xaml.cs:

/// <summary>
/// Handle trigger level arrow movement (replaces slider handler)
/// </summary>
private void TriggerLevelArrows_GraticuleMovement(object sender, GraticuleMovementEventArgs e)
{
    if (controller == null) return;

    controller.HandleTriggerLevelChanged(e.NewValue);
    UpdateLevelValueDisplay();
    LogEvent?.Invoke(this, $"Trigger level moved {e.GraticuleMultiplier:F1} graticule to {FormatVoltage(e.NewValue)}");
}

/// <summary>
/// Update the trigger level arrow control (replaces slider updates)
/// </summary>
public void UpdateTriggerLevelArrowControl()
{
    if (TriggerLevelArrows == null || controller == null) return;

    var settings = controller.GetSettings();
    var (minLevel, maxLevel) = GetTriggerLevelRange();

    // Update arrow control properties
    TriggerLevelArrows.GraticuleSize = 0.1; // 0.1V per graticule step
    TriggerLevelArrows.Units = "V";
    TriggerLevelArrows.UpdateRange(minLevel, maxLevel);
    TriggerLevelArrows.SetValue(settings.EdgeLevel);

    // Update range displays
    if (MinLevelDisplay != null)
        MinLevelDisplay.Text = FormatVoltage(minLevel);
    if (MaxLevelDisplay != null)
        MaxLevelDisplay.Text = FormatVoltage(maxLevel);
    if (LevelRangeText != null)
    {
        string rangeText;
        if (Math.Abs(minLevel) == Math.Abs(maxLevel))
        {
            rangeText = $"Range: ±{FormatVoltage(Math.Abs(maxLevel))}";
        }
        else
        {
            rangeText = $"Range: {FormatVoltage(minLevel)} to {FormatVoltage(maxLevel)}";
        }
        LevelRangeText.Text = rangeText;
    }
}

/// <summary>
/// Get trigger level range based on source channel settings
/// </summary>
private (double min, double max) GetTriggerLevelRange()
{
    // You'll need to get channel settings from the main application
    // For now, return reasonable default range
    return (-5.0, 5.0);

    // TODO: Replace with actual channel-based range calculation:
    // var ch1Settings = GetChannel1Settings(); // Get from main app
    // var ch2Settings = GetChannel2Settings(); // Get from main app
    // var settings = controller.GetSettings();
    // return settings.GetTriggerLevelRange(ch1Settings, ch2Settings);
}

/// <summary>
/// Update the trigger level value display (replaces slider display update)
/// </summary>
private void UpdateLevelValueDisplay()
{
    if (LevelValueText == null || TriggerLevelArrows == null) return;

    double value = TriggerLevelArrows.CurrentValue;
    LevelValueText.Text = FormatVoltage(value);
}

/// <summary>
/// Format voltage for display
/// </summary>
private string FormatVoltage(double voltage)
{
    if (Math.Abs(voltage) >= 1.0)
        return $"{voltage:F3}V";
    else if (Math.Abs(voltage) >= 0.001)
        return $"{voltage * 1000:F1}mV";
    else
        return $"{voltage * 1000000:F1}µV";
}

// UPDATE the SetupEnhancedUI method:

/// <summary>
/// Set up enhanced UI elements
/// </summary>
private void SetupEnhancedUI()
{
    UpdateRangeDisplays();
    UpdateHoldoffDisplay();
    UpdateTriggerLevelArrowControl(); // Added this line
}

// UPDATE the UpdateRangeDisplays method to work with the new layout:

/// <summary>
/// Update the min/max range displays
/// </summary>
public void UpdateRangeDisplays()
{
    var (minLevel, maxLevel) = GetTriggerLevelRange();

    if (MaxLevelDisplay != null)
        MaxLevelDisplay.Text = FormatVoltage(maxLevel);
    if (MinLevelDisplay != null)
        MinLevelDisplay.Text = FormatVoltage(minLevel);
    if (LevelRangeText != null)
        LevelRangeText.Text = $"Range: {FormatVoltage(minLevel)} to {FormatVoltage(maxLevel)}";
}