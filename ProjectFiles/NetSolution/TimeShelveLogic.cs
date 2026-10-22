#region Using directives
using UAManagedCore;
using FTOptix.UI;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.SerialPort;
using FTOptix.SQLiteStore;
using FTOptix.MQTTBroker;
using FTOptix.OPCUAServer;
#endregion

public class TimeShelveLogic : BaseNetLogic
{
    /// <summary>
    /// This method initializes and sets up the duration buttons for the timed shelf.
    /// It retrieves the preset and custom time buttons from the owner's UI and calls the
    /// PresetShelveDurationButtonPressed method to initialize the preset duration.
    /// </summary>
    /// <remarks>
    /// The method assumes that the owner has a method <see cref="Get<ToggleButton>"/> that
    /// can retrieve a ToggleButton by its ID. It uses the IDs "TimedShelve/Layout/DurationButtonsLayout/PresetShelveDurationButton"
    /// and "TimedShelve/Layout/DurationButtonsLayout/CustomTime/CustomTimeShelveButton" to find the buttons.
    /// </remarks>
    public override void Start()
    {
        presetTimeButton = Owner.Get<ToggleButton>("TimedShelve/Layout/DurationButtonsLayout/PresetShelveDurationButton");
        customTimeButton = Owner.Get<ToggleButton>("TimedShelve/Layout/DurationButtonsLayout/CustomTime/CustomTimeShelveButton");

        PresetShelveDurationButtonPressed();
    }

    /// <summary>
    /// This method sets the active state of the 'Preset Shelve Duration' button to true and the 'Custom Shelve Duration' button to false.
    /// </summary>
    /// <remarks>
    /// This method is typically used to configure the UI state based on user selection.
    /// </remarks>
    [ExportMethod]
    public void PresetShelveDurationButtonPressed()
    {
        presetTimeButton.Active = true;
        customTimeButton.Active = false;
    }

    /// <summary>
    /// This method disables the preset time button and enables the custom time button.
    /// </summary>
    /// <remarks>
    /// This method is used to switch between preset and custom time settings on the shelf button.
    /// </remarks>
    [ExportMethod]
    public void CustomTimeShelveButtonPressed()
    {
        presetTimeButton.Active = false;
        customTimeButton.Active = true;
    }

    private ToggleButton presetTimeButton;
    private ToggleButton customTimeButton;
}
