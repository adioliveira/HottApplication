#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using System;
using System.Linq;
using System.Linq.Expressions;
using UAManagedCore;
using FTOptix.Alarm;
using FTOptix.SerialPort;
using FTOptix.SQLiteStore;
using static AlarmFilterDataLogic;
#endregion

public class AlarmWidgetLogic : BaseNetLogic
{
    public override void Start()
    {
        alarmsDataGridModel = Owner.GetVariable("Layout/AlarmsDataGrid/Model");

        var currentSession = LogicObject.Context.Sessions.CurrentSessionInfo;
        actualLanguageVariable = currentSession.SessionObject.Get<IUAVariable>("ActualLanguage");
        actualLanguageVariable.VariableChange += OnSessionActualLanguageChange;

        filterConfiguration = AlarmFilterEditModelLogic.GetEditModel(AlarmWidgetConfiguration, filtersConfigurationBrowseName);
        LoadFiltersData();
        PrepareQuery();
        RefreshFilterChips();
    }

    public override void Stop()
    {
        actualLanguageVariable.VariableChange -= OnSessionActualLanguageChange;
    }

    /// <summary>
    /// This method loads filter data, sets the filter based on the provided name, prepares the query, and refreshes the filter chips.
    /// </summary>
    /// <param name="filterBrowseName">The name of the filter to use for setting the filter.</param>
    [ExportMethod]
    public void Filter(string filterBrowseName)
    {
        LoadFiltersData();
        SetFilter(filterBrowseName);
        PrepareQuery();
        RefreshFilterChips();
    }

    /// <summary>
    /// This method clears all filters and performs a series of operations to prepare the application for a new state.
    /// </summary>
    /// <remarks>
    /// The method ensures that all filters are cleared and the application is ready for the next operation.
    /// </remarks>
    [ExportMethod]
    public void ClearAll()
    {
        LoadFiltersData();
        ClearFilters();
        SaveFilters();
        PrepareQuery();
        RefreshFilterChips();
    }

    /// <summary>
    /// This method is triggered when the actual language of the session changes.
    /// It restarts the data binding on the data grid model variable to refresh the displayed data.
    /// </summary>
    /// <param name="sender"> The sender of the event.</param>
    /// <param name="e"> The event arguments containing information about the change.</param>
    public void OnSessionActualLanguageChange(object sender, VariableChangeEventArgs e)
    {
        var dynamicLink = alarmsDataGridModel.GetVariable("DynamicLink");
        if (dynamicLink == null)
            return;

        // Restart the data bind on the data grid model variable to refresh data
        dynamicLink.Stop();
        dynamicLink.Start();
    }

    /// <summary>
    /// This method loads filter data from the configuration and sets the filter data based on the loaded values.
    /// It retrieves the filter configuration and iterates through each attribute node,
    /// setting the corresponding filter data based on the browse name.
    /// </summary>
    private void LoadFiltersData()
    {
        AlarmFilterEditModelLogic.CreateEditModel(AlarmWidgetConfiguration, filterConfiguration);
        var configuration = AlarmFilterEditModelLogic.GetEditModel(AlarmWidgetConfiguration);

        foreach (var attributeNode in configuration.Children)
        {
            if (Enum.TryParse(attributeNode.BrowseName, out FilterAttribute attribute))
            {
                foreach (var child in attributeNode.Children)
                {
                    var value = InformationModel.GetVariable(child.NodeId).Value;

                    switch (child.BrowseName)
                    {
                        case fromEventTimeDateTimeBrowseName:
                            filterData.SetFromEventTime((DateTime)value);
                            break;
                        case toEventTimeDateTimeBrowseName:
                            filterData.SetToEventTime((DateTime)value);
                            break;
                        case fromSeverityBrowseName:
                            filterData.SetFromSeverity(value);
                            break;
                        case toSeverityBrowseName:
                            filterData.SetToSeverity(value);
                            break;
                        default:
                            if (!alarmFilterData.Filters.Any(filter => filter.Name == child.BrowseName))
                                alarmFilterData.Filters.Add(new ToggleFilter(child.BrowseName, value, attribute));
                            else
                                alarmFilterData.Filters.First(filter => filter.Name == child.BrowseName).IsChecked = value;
                            break;
                    }
                }
            }
            else
                Log.Warning($"Accordion {attributeNode.BrowseName} browse name is not a valid FilterAttribute.");
        }
    }

    /// <summary>
    /// This method sets the filter based on the provided browse name.
    /// If the filter is not found, it logs a warning and returns.
    /// If the filter is found, it sets the check state to false and
    /// updates the value of the corresponding filter variable to false.
    /// </summary>
    /// <param name="filterBrowseName">The name of the filter to set.</param>
    /// <remarks>
    /// If the filter is not found, a warning is logged and the method returns immediately.
    /// </remarks>
    private void SetFilter(string filterBrowseName)
    {
        var filter = alarmFilterData.Filters.FirstOrDefault(x => x.Name == filterBrowseName);
        if (filter == null)
        {
            Log.Warning($"FilterBrowseName '{filterBrowseName}' not found in filters list.");
            return;
        }

        filter.IsChecked = false;
        var variable = GetFiltersModelVariable(filter.Name, filter.Attribute, defaultEditModelBrowseName);
        if (variable != null)
            variable.Value = false;
    }

    /// <summary>
    /// This method clears all the filters by setting their IsChecked property to false.
    /// </summary>
    /// <param name="filter">The filter to clear.</param>
    /// <note>
    /// This method modifies the state of the filters in-place.
    /// </note>
    private void ClearFilters()
    {
        foreach (var filter in alarmFilterData.Filters)
            filter.IsChecked = false;
    }

    /// <summary>
    /// This method iterates through each filter in the alarm filter data and sets the value of a corresponding variable based on the checked state of the filter.
    /// </summary>
    /// <param name="filter">The filter to process.</param>
    private void SaveFilters()
    {
        foreach (var filter in alarmFilterData.Filters)
        {
            var variable = GetFiltersModelVariable(filter.Name, filter.Attribute, defaultEditModelBrowseName);
            if (variable != null)
                variable.Value = filter.IsChecked;
        }
    }

    /// <summary>
    /// Retrieves the filter model variable based on the browse name, attribute, and edit model browse name.
    /// </summary>
    /// <param name="browseName">The browse name of the variable to retrieve.</param>
    /// <param name="attribute">The filter attribute to use for lookup.</param>
    /// <param name="editModelBrowseName">The browse name of the edit model to use.</param>
    /// <returns>
    /// The variable from the filter model, or null if the attribute or variable is not found.
    /// </returns>
    /// <note>
    /// If the attribute is not found, a warning is logged and null is returned.
    /// If the variable is not found, another warning is logged and null is returned.
    /// </note>
    private IUAVariable GetFiltersModelVariable(string browseName, FilterAttribute attribute, string editModelBrowseName)
    {
        var filtersModel = AlarmFilterEditModelLogic.GetEditModel(AlarmWidgetConfiguration, editModelBrowseName);
        var attributeVariable = filtersModel.GetVariable(attribute.ToString());
        if (attributeVariable == null)
        {
            Log.Warning($"FilterModel attribute: {attribute} not found.");
            return null;
        }

        var variable = attributeVariable.GetVariable(browseName);
        if (variable == null)
            Log.Warning($"FilterModel variable: {browseName} not found.");
        return variable;
    }

    /// <summary>
    /// This method refreshes the filter chips by retrieving the alarm widget logic and filter layout,
    /// then generating filter chips based on the provided alarm filter data and layout.
    /// </summary>
    /// <remarks>
    /// The method assumes that <see cref="Owner.GetObject"/> and <see cref="Owner.Get"/> are
    /// available to retrieve the necessary objects. It uses <see cref="AlarmWidgetObjectsGenerator"/>
    /// to generate the filter chips.
    /// </remarks>
    private void RefreshFilterChips()
    {
        var alarmWidgetLogic = Owner.GetObject("AlarmWidgetLogic");
        var filterLayout = Owner.Get<RowLayout>("Layout/FilterHorizontalLayout");

        AlarmWidgetObjectsGenerator.GenerateFilterChips(alarmFilterData, filterLayout, alarmWidgetLogic);
    }

    /// <summary>
    /// This method prepares a query by building it using a query builder logic.
    /// It retrieves a query from a specified data grid and builds the query
    /// based on provided alarm filter data and configuration.
    /// </summary>
    /// <remarks>
    /// The method uses a query builder to construct the query, which is then
    /// refreshed to ensure the latest query is used.
    /// </remarks>
    private void PrepareQuery()
    {
        AlarmFilterQueryBuilderLogic queryBuilder = new()
        {
            Query = Owner.Get("Layout/AlarmsDataGrid").GetVariable("Query")
        };
        queryBuilder.BuildQuery(alarmFilterData, filterConfiguration);
        queryBuilder.RefreshQuery();
    }

    /// <summary>
    /// This property retrieves the alarm widget configuration from the owner object.
    /// It uses the "ConfigurationPointer" variable to get the pointed node ID,
    /// and then retrieves the corresponding IUANode from the information model.
    /// </summary>
    /// <returns>
    /// The IUANode representing the alarm widget configuration.
    /// </returns>
    private IUANode AlarmWidgetConfiguration
    {
        get
        {
            var pointedNodeId = Owner.GetVariable("ConfigurationPointer").Value;
            var alarmWidgetConfiguration = InformationModel.Get(pointedNodeId);
            return alarmWidgetConfiguration ?? throw new CoreConfigurationException("AlarmWidgetConfiguration not found");
        }
    }

    private IUAVariable alarmsDataGridModel;
    private IUAVariable actualLanguageVariable;
    private IUAObject filterConfiguration;
    private ToggleFilterData filterData { get => (ToggleFilterData)alarmFilterData.Data; }
    private readonly AlarmFilterDataLogic alarmFilterData = new()
    {
        Data = new ToggleFilterData()
    };
}
