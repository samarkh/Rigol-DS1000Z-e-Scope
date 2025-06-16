// Updates for TriggerController.cs to support multimedia arrow controls
// Add these property declarations to the UI Control Properties region:

#region UI Control Properties (Add to existing region)

// REPLACED: Slider property with Arrow control property
using DS1000Z_E_USB_Control.Controls;
using DS1000Z_E_USB_Control.Properties;
using System;
using System.Globalization;

public GraticuleArrowControl TriggerLevelArrows { get; set; }

// Keep existing properties:
// public ComboBox TriggerModeComboBox { get; set; }
// public ComboBox TriggerSweepComboBox { get; set; }
// ... etc

#endregion

// UPDATE the InitializeControls method to wire up arrow control instead of slider:

/// <summary>
/// Initialize UI controls with current settings
/// </summary>
public void InitializeControls()
{
    DisableEventHandlers();
    isUpdating = true;

    try
    {
        // Initialize combo boxes (existing code stays the same)
        InitializeComboBoxes();

        // REPLACED: Initialize arrow control instead of slider
        InitializeTriggerLevelArrowControl();

        // Initialize other controls
        InitializeOtherControls();

        // Update displays
        UpdateCurrentSettingsDisplay();
        UpdateRangeDisplays();
    }
    catch (Exception ex)
    {
        Log($"Error initializing trigger UI: {ex.Message}");
    }
    finally
    {
        EnableEventHandlers();
        isUpdating = false;
    }
}

// ADD this new method:

/// <summary>
/// Initialize the trigger level arrow control (replaces slider initialization)
/// </summary>
private void InitializeTriggerLevelArrowControl()
{
    if (TriggerLevelArrows == null) return;

    // Set up arrow control properties
    TriggerLevelArrows.GraticuleSize = 0.1; // 0.1V per step
    TriggerLevelArrows.Units = "V";
    TriggerLevelArrows.CurrentValue = settings.EdgeLevel;

    // Set reasonable range (this should be updated when source channel changes)
    TriggerLevelArrows.UpdateRange(-5.0, 5.0);

    Log("Trigger level arrow control initialized");
}

// UPDATE the UpdateUIFromSettings method:

/// <summary>
/// Update UI controls from current settings
/// </summary>
public void UpdateUIFromSettings()
{
    if (isUpdating) return;

    DisableEventHandlers();
    isUpdating = true;

    try
    {
        // Update combo boxes (existing code)
        UpdateComboBoxFromSettings();

        // REPLACED: Update arrow control instead of slider
        UpdateArrowControlFromSettings();

        // Update other controls
        UpdateOtherControlsFromSettings();

        // Update displays
        UpdateCurrentSettingsDisplay();
        UpdateRangeDisplays();
    }
    catch (Exception ex)
    {
        Log($"Error updating trigger UI: {ex.Message}");
    }
    finally
    {
        EnableEventHandlers();
        isUpdating = false;
    }
}

// ADD this new method:

/// <summary>
/// Update arrow control from settings (replaces UpdateSliderFromSettings)
/// </summary>
private void UpdateArrowControlFromSettings()
{
    if (TriggerLevelArrows != null && !isUpdating)
    {
        TriggerLevelArrows.SetValue(settings.EdgeLevel);
        UpdateArrowControlValueDisplay();
    }
}

// ADD this new method:

/// <summary>
/// Update arrow control value display
/// </summary>
private void UpdateArrowControlValueDisplay()
{
    if (LevelValueText != null && TriggerLevelArrows != null)
    {
        LevelValueText.Text = FormatVoltage(TriggerLevelArrows.CurrentValue);
    }
}

// UPDATE the HandleTriggerLevelChanged method to work with arrow control:

/// <summary>
/// Handle trigger level changes from arrow control or other sources
/// </summary>
public void HandleTriggerLevelChanged(double level)
{
    if (!oscilloscope.IsConnected || isUpdating) return;

    string command = $":TRIGger:EDGe:LEVel {level.ToString(CultureInfo.InvariantCulture)}";

    if (oscilloscope.SendCommand(command))
    {
        settings.EdgeLevel = level;
        Log($"Trigger level set to {level:F3}V");
        UpdateCurrentSettingsDisplay();
        UpdateSettingsManagerIfAvailable();
        UpdateArrowControlValueDisplay(); // Updated this line
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
    else
    {
        Log("Failed to set trigger level");
        UpdateArrowControlFromSettings(); // Updated this line
    }
}

// ADD this method to support dynamic range updates:

/// <summary>
/// Update trigger level range based on source channel settings
/// </summary>
public void UpdateTriggerLevelRange(double minLevel, double maxLevel)
{
    if (TriggerLevelArrows == null) return;

    // Update arrow control range
    TriggerLevelArrows.UpdateRange(minLevel, maxLevel);

    // Clamp current value to new range if needed
    double currentValue = TriggerLevelArrows.CurrentValue;
    if (currentValue < minLevel || currentValue > maxLevel)
    {
        double clampedValue = Math.Max(minLevel, Math.Min(maxLevel, currentValue));
        HandleTriggerLevelChanged(clampedValue);
    }

    // Update range displays
    UpdateRangeDisplays();

    Log($"Trigger level range updated: {minLevel:F3}V to {maxLevel:F3}V");
}

// REMOVE or comment out these slider-related methods:
/*
private void UpdateSliderRange() { ... }
private void UpdateSliderFromSettings() { ... }
private void UpdateSliderValueDisplay() { ... }
*/

// ADD this helper method for voltage formatting:

/// <summary>
/// Format voltage value for display
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