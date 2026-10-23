#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System.Collections.Generic;
using System.Linq;
using UAManagedCore;
using FTOptix.Alarm;
using FTOptix.SerialPort;
using FTOptix.SQLiteStore;
using FTOptix.MQTTBroker;
using FTOptix.OPCUAServer;
using static AlarmFilterDataLogic;
#endregion

public class AlarmWidgetGenerateDefaultFiltersToggle : BaseNetLogic
{
    /// <summary>
    /// This method retrieves an edit model configuration, deletes the existing one, and creates a new one with the provided configuration.
    /// </summary>
    [ExportMethod]
    public void GenerateCustomFilters()
    {
        var filterConfiguration = AlarmFilterEditModelLogic.GetEditModel(AlarmWidgetConfiguration, filtersConfigurationBrowseName);

        AlarmFilterEditModelLogic.DeleteEditModel(AlarmWidgetConfiguration);
        AlarmFilterEditModelLogic.CreateEditModel(AlarmWidgetConfiguration, filterConfiguration);
    }

    /// <summary>
    /// This method generates a preset filter configuration based on the provided alarm widget configuration and a given browse name.
    /// It retrieves the appropriate filter configuration, finds an available preset name, and creates the edit model with the preset.
    /// </summary>
    /// <param name="AlarmWidgetConfiguration">The alarm widget configuration to use for generating the preset filters.</param>
    /// <param name="filtersConfigurationBrowseName">The browse name used to look up the filter configuration.</param>
    [ExportMethod]
    public void GeneratePresetFilters()
    {
        var filterConfiguration = AlarmFilterEditModelLogic.GetEditModel(AlarmWidgetConfiguration, filtersConfigurationBrowseName);

        var nodes = AlarmWidgetConfiguration.GetNodesByType<IUAObject>().Select(n => n.BrowseName);
        var presetName = FindNextAvailablePresetName(nodes, "PresetFilters");
        AlarmFilterEditModelLogic.CreateEditModel(AlarmWidgetConfiguration, filterConfiguration, presetName);
    }

    /// <summary>
    /// Updates the custom and preset filters based on the provided configuration.
    /// </summary>
    [ExportMethod]
    public void UpdateCustomAndPresetsFilters()
    {
        var filterConfiguration = AlarmFilterEditModelLogic.GetEditModel(AlarmWidgetConfiguration, filtersConfigurationBrowseName);

        var nodes = AlarmWidgetConfiguration.GetNodesByType<IUAObject>();
        AlarmFilterEditModelLogic.UpdateTypeAndEditModels(AlarmWidgetConfiguration, filterConfiguration, nodes);
    }

    public IUANode AlarmWidgetConfiguration
    {
        get
        {
            var alarmWidgetConfiguration = InformationModel.Get(Owner.NodeId);
            return alarmWidgetConfiguration ?? throw new CoreConfigurationException($"{alarmWidgetConfigurationBrowseName} not found");
        }
    }

    /// <summary>
    /// Finds the next available preset name by checking existing presets and appending a number to the base name.
    /// </summary>
    /// <param name="existingPresets"> The collection of existing preset names.</param>
    /// <param name="name"> The base name to use for generating the new preset name.</param>
    /// <returns>
    /// The next available preset name, which is a combination of the base name and a number.
    /// </returns>
    public static string FindNextAvailablePresetName(IEnumerable<string> existingPresets, string name)
    {
        if (existingPresets == null)
            existingPresets = Enumerable.Empty<string>(); // Treat null as an empty collection

        var existingPresetNumbers = new HashSet<int>();
        foreach (var presetName in existingPresets.Where(presetName => presetName.StartsWith(name)))
        {
            if (presetName == name)
                existingPresetNumbers.Add(0); // Treat "PresetFilters" as PresetFilters0
            else
            {
                string numberPart = presetName.Substring(name.Length);
                if (int.TryParse(numberPart, out int number))
                    existingPresetNumbers.Add(number);
            }
        }

        int counter = 1;
        while (existingPresetNumbers.Contains(counter))
            counter++;
        return $"{name}{counter}";
    }

    private readonly string alarmWidgetConfigurationBrowseName = "AlarmWidgetConfiguration";
}
