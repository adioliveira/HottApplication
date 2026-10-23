#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using System;
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

public class AlarmFilterLogic : BaseNetLogic
{
    public override void Start()
    {
        alarmFilter = new AlarmFilter(Owner);
    }

    /// <summary>
    /// This method applies a filter to the alarm system using the provided browse name.
    /// It first validates the browse name and then refreshes the alarm filter.
    /// </summary>
    /// <param name="filterBrowseName">The name of the filter to apply.</param>
    /// <returns>
    /// No return value.
    /// </returns>
    [ExportMethod]
    public void Filter(string filterBrowseName)
    {
        alarmFilter.IsValidFilterBrowseName(filterBrowseName);
        alarmFilter.Refresh();
    }

    /// <summary>
    /// This method loads a preset with the given name, saves all data, and refreshes the display.
    /// </summary>
    /// <param name="presetName">The name of the preset to load.</param>
    /// <returns>
    /// No return value.
    /// </returns>
    [ExportMethod]
    public void LoadPreset(string presetName)
    {
        alarmFilter.LoadPreset(presetName);
        alarmFilter.SaveAll();
        alarmFilter.Refresh();
    }

    /// <summary>
    /// This method triggers the refresh operation, calling the <see cref="alarmFilter.Refresh()"/> method.
    /// </summary>
    /// <remarks>
    /// This method is typically used to update or reinitialize the state of the object being refreshed.
    /// </remarks>
    [ExportMethod]
    public void Refresh()
    {
        alarmFilter.Refresh();
    }

    /// <summary>
    /// This method triggers the saving and refreshing of alarm data.
    /// </summary>
    /// <remarks>
    /// The method calls <see cref="alarmFilter.SaveAll()"/> and <see cref="alarmFilter.Refresh()"/>
    /// to perform the necessary operations.
    /// </remarks>
    [ExportMethod]
    public void Apply()
    {
        alarmFilter.SaveAll();
        alarmFilter.Refresh();
    }

    /// <summary>
    /// This method clears all alarms and updates the filter state.
    /// </summary>
    /// <remarks>
    /// The method performs the following actions:
    /// 1. Clears all filters using <see cref="alarmFilter.ClearFilters()"/>.
    /// 2. Saves all filters using <see cref="alarmFilter.SaveAll()"/>.
    /// 3. Refreshes the alarm list using <see cref="alarmFilter.Refresh()"/>.
    /// </remarks>
    [ExportMethod]
    public void ClearAll()
    {
        alarmFilter.ClearFilters();
        alarmFilter.SaveAll();
        alarmFilter.Refresh();
    }

    /// <summary>
    /// This method closes the alarm filter and performs a refresh operation.
    /// </summary>
    /// <remarks>
    /// The method calls <see cref="alarmFilter.LoadPreset(defaultEditModelBrowseName)"/> and <see cref="alarmFilter.Refresh()"/>.
    /// </remarks>
    [ExportMethod]
    public void Close()
    {
        alarmFilter.LoadPreset(defaultEditModelBrowseName);
        alarmFilter.Refresh();
    }

    private sealed class AlarmFilter
    {
        /// <summary>
        /// Initializes an AlarmFilter with the provided owner and sets up the filter configuration.
        /// </summary>
        /// <param name="owner">The owner of the alarm filter.</param>
        /// <returns>
        /// An object representing the initialized AlarmFilter.
        /// </returns>
        public AlarmFilter(IUANode owner)
        {
            Owner = owner;

            try
            {
                filterConfiguration = AlarmFilterEditModelLogic.GetEditModel(AlarmWidgetConfiguration, filtersConfigurationBrowseName);
            }
            catch (CoreConfigurationException ex)
            {
                Log.Warning($"{filtersConfigurationBrowseName} in AlarmWidgetEditModel not found: " + ex.Message);
            }

            InitializeAlarmFilterData();
            queryBuilder = new()
            {
                Query = AlarmWidget.Get("Layout/AlarmsDataGrid").GetVariable("Query")
            };
            AlarmFilterEditModelLogic.CreateEditModel(AlarmWidgetConfiguration, filterConfiguration);

            InitializeCheckBoxes(defaultEditModelBrowseName);
            InitializeDateTimePickers(defaultEditModelBrowseName);
            InitializeTextBoxes(defaultEditModelBrowseName);
            ExpandAccordions();
        }

        /// <summary>
        /// This method saves all controls on the form: check boxes, date time pickers, and text boxes.
        /// </summary>
        /// <remarks>
        /// The method calls three separate methods: SaveCheckBoxes(), SaveDateTimePickers(), and SaveTextBoxes() to perform the saving operation for each type of control.
        /// </remarks>
        public void SaveAll()
        {
            SaveCheckBoxes();
            SaveDateTimePickers();
            SaveTextBoxes();
        }

        /// <summary>
        /// This method checks if the specified filter browse name exists in the alarm filter data.
        /// If it does not exist, a warning is logged indicating that the browse name was not found.
        /// </summary>
        /// <param name="filterBrowseName">The browse name of the filter to check.</param>
        /// <remarks>
        /// The method checks the existence of the filter browse name in the collection using the
        /// <see cref="Any"/> method. If the name is not found, a warning is logged with the name
        /// of the filter and the message that the browse name was not found.
        /// </remarks>
        public void IsValidFilterBrowseName(string filterBrowseName)
        {
            if (!alarmFilterData.Filters.Any(x => x.Name == filterBrowseName))
                Log.Warning($"Filter {filterBrowseName} browse name not found");
        }

        /// <summary>
        /// This method clears all filters by setting the IsChecked property of each filter to false.
        /// </summary>
        /// <param name="filter">The filter to clear.</param>
        /// <returns>
        /// No return value.
        /// </returns>
        public void ClearFilters()
        {
            foreach (var filter in alarmFilterData.Filters)
                filter.IsChecked = false;
                
            InitializeDateTimePickers(defaultEditModelBrowseName);
            InitializeTextBoxes(defaultEditModelBrowseName);
        }

        /// <summary>
        /// This method refreshes the alarm filter by building a new query and generating filter chips.
        /// </summary>
        public void Refresh()
        {
            queryBuilder.BuildQuery(alarmFilterData, filterConfiguration);
            queryBuilder.RefreshQuery();

            var alarmWidgetLogic = AlarmWidget.GetObject("AlarmWidgetLogic");
            var filterLayout = AlarmWidget.Get<RowLayout>("Layout/FilterHorizontalLayout");

            AlarmWidgetObjectsGenerator.GenerateFilterChips(alarmFilterData, filterLayout, alarmWidgetLogic);
        }

        /// <summary>
        /// This method loads a preset by initializing checkboxes, date-time pickers, text boxes, and expanding accordions based on the provided preset name.
        /// </summary>
        /// <param name="presetName">The name of the preset to load.</param>
        /// <returns>None</returns>
        public void LoadPreset(string presetName)
        {
            InitializeCheckBoxes(presetName);
            InitializeDateTimePickers(presetName);
            InitializeTextBoxes(presetName);
            ExpandAccordions();
        }

        public IUANode AlarmWidget
        {
            get
            {
                var aliasNodeId = Owner.GetVariable("ModelAlias").Value;
                var alarmWidget = InformationModel.Get(aliasNodeId);
                return alarmWidget ?? throw new CoreConfigurationException("ModelAlias node id not found");
            }
        }

        public IUANode AlarmWidgetConfiguration
        {
            get
            {
                var nodePointer = AlarmWidget.GetVariable("ConfigurationPointer").Value;
                var alarmWidgetConfiguration = InformationModel.Get(nodePointer);
                return alarmWidgetConfiguration ?? throw new CoreConfigurationException("AlarmWidgetConfiguration not found");
            }
        }

        /// <summary>
        /// This method initializes the alarm filter data by retrieving layout and logic objects,
        /// generating preset buttons, and configuring custom filters based on configuration settings.
        /// </summary>
        /// <remarks>
        /// The method uses the Owner object to access layout and logic components, and processes
        /// custom filters based on configuration variables.
        /// </remarks>
        private void InitializeAlarmFilterData()
        {
            var baseLayout = Owner.Get("Filters/ScrollView/Layout");
            var alarmFilterLogic = Owner.GetObject("AlarmFilterLogic");
            AlarmWidgetObjectsGenerator.GeneratePresetButtons(baseLayout, AlarmWidgetConfiguration, alarmFilterLogic);

            AlarmWidgetObjectsGenerator.GenerateCustomFiltersToggle(baseLayout, filterConfiguration, filterData, alarmFilterLogic);
            var customLayout = baseLayout.Get("CustomFilters/Content/CustomFilters");
            ProcessAttribute([.. customLayout.Children]);

            var customFiltersAvailableOnRuntime = AlarmWidgetConfiguration.GetVariable("CustomFiltersAvailableOnRuntime");
            var customFiltersExpandedByDefault = AlarmWidgetConfiguration.GetVariable("CustomFiltersExpandedByDefault");

            if (customFiltersAvailableOnRuntime == null || !customFiltersAvailableOnRuntime.Value)
                baseLayout.Get<Accordion>(defaultEditModelBrowseName).Visible = false;
            else if (customFiltersExpandedByDefault != null)
                baseLayout.Get<Accordion>(defaultEditModelBrowseName).Expanded = customFiltersExpandedByDefault.Value;
        }

        /// <summary>
        /// This method processes a collection of IUANode objects, checking if they are of type Accordion.
        /// If they are, it attempts to parse the browse name of the accordion into a FilterAttribute enum value.
        /// If successful, it processes the content of the accordion by calling the ProcessContent method.
        /// If the browse name is not valid, a warning is logged.
        /// </summary>
        /// <param name="nodes">A collection of IUANode objects to process.</param>
        /// <remarks>
        /// The method iterates through the collection of nodes, checking if each node is an Accordion.
        /// If it is, it attempts to parse the browse name into a FilterAttribute enum value.
        /// If successful, it processes the content of the accordion.
        /// If the browse name is not valid, a warning is logged.
        /// </remarks>
        private void ProcessAttribute(IEnumerable<IUANode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node == null)
                    return;

                if (node is Accordion accordion)
                {
                    if (Enum.TryParse(accordion.BrowseName, out FilterAttribute attribute))
                        ProcessContent([.. accordion.Get("Content").Children], attribute, accordion);
                    else
                        Log.Warning($"Accordion {accordion.BrowseName} browse name is not a valid FilterAttribute.");
                }
            }
        }

        /// <summary>
        /// This method processes a collection of IUANode objects, applying the given FilterAttribute and Accordion to each node.
        /// It recursively processes child nodes of ColumnLayout and RowLayout elements, and handles CheckBox elements by adding them to the alarmFilterData.Filters collection.
        /// </summary>
        /// <param name="nodes">A collection of IUANode objects to process.</param>
        /// <param name="attribute">The FilterAttribute to apply to all processed nodes.</param>
        /// <param name="accordion">The Accordion object to manage the processing of nested layouts.</param>
        private void ProcessContent(IEnumerable<IUANode> nodes, FilterAttribute attribute, Accordion accordion)
        {
            foreach (var node in nodes)
            {
                if (node == null)
                    return;

                if (node is ColumnLayout columnLayout)
                    ProcessContent([.. columnLayout.Children], attribute, accordion);
                if (node is RowLayout rowLayout)
                    ProcessContent([.. rowLayout.Children], attribute, accordion);
                if (node is CheckBox checkbox)
                    alarmFilterData.Filters.Add(new CheckBoxFilter(checkbox, attribute, accordion));
            }
        }

        /// <summary>
        /// This method initializes date-time pickers based on the provided model browse name and configuration.
        /// It retrieves variables from the filter configuration and initializes date-time pickers for the 'from' and 'to' event times
        /// if the corresponding variables are set to true.
        /// </summary>
        /// <param name="editModelBrowseName">The model browse name used to look up the edit model.</param>
        /// <remarks>
        /// The method checks if the 'eventTimeVariable' and its child variables ('fromEventTimeBrowseName' and 'toEventTimeBrowseName')
        /// are set to true before initializing the date-time pickers.
        /// </remarks>
        private void InitializeDateTimePickers(string editModelBrowseName)
        {
            var eventTimeVariable = filterConfiguration.GetVariable(eventTimeBrowseName);
            if (eventTimeVariable.Value)
            {
                if (eventTimeVariable.GetVariable(fromEventTimeBrowseName).Value)
                    InitializeDateTimePicker(fromEventTimeBrowseName, editModelBrowseName);
                if (eventTimeVariable.GetVariable(toEventTimeBrowseName).Value)
                    InitializeDateTimePicker(toEventTimeBrowseName, editModelBrowseName);
            }
        }

        /// <summary>
        /// This method initializes a DateTimePicker based on a filter. If the filter is checked, it sets the DateTimePicker value using a derived variable; otherwise, it sets it to the current date and time.
        /// </summary>
        /// <param name="name">The name of the filter to use for the DateTimePicker.</param>
        /// <param name="editModelBrowseName">The browse name for the model edit.</param>
        /// <returns>
        /// No return value; the method modifies the DateTimePicker's value based on the filter or current time.
        /// </returns>
        private void InitializeDateTimePicker(string name, string editModelBrowseName)
        {
            var filter = alarmFilterData.Filters.First(x => x.Name == name &&
                                             x.Attribute == FilterAttribute.EventTime);

            if (filter.IsChecked)
            {
                var variable = GetFiltersModelVariable(name + dateTimeBrowseName, FilterAttribute.EventTime, editModelBrowseName);
                if (variable != null)
                    filterData.EventTimePickers.GetValueOrDefault(name).Value = variable.Value;
            }

            else
                filterData.EventTimePickers.GetValueOrDefault(name).Value = DateTime.Now;
        }

        /// <summary>
        /// This method initializes text boxes based on configuration values and filter settings.
        /// It retrieves variables from the configuration, checks if they are set, and updates
        /// the text boxes with the appropriate values.
        /// </summary>
        /// <param name="editModelBrowseName">The name of the edit model being used.</param>
        /// <remarks>
        /// The method checks if the severity attributes are set. If both are set and the severity filter is checked,
        /// it updates the text boxes with values from the severity model. If not, it defaults to "1" and "1000".
        /// </remarks>
        private void InitializeTextBoxes(string editModelBrowseName)
        {
            var severityAttributeVariable = filterConfiguration.GetVariable(severityBrowseName);
            var severityChildVariable = severityAttributeVariable.GetVariable(severityBrowseName);

            if (severityAttributeVariable.Value && severityChildVariable.Value)
            {
                var severityFilter = alarmFilterData.Filters.First(x => x.Name == severityBrowseName &&
                                                    x.Attribute == FilterAttribute.Severity);

                if (severityFilter.IsChecked)
                {
                    var variableFromSeverity = GetFiltersModelVariable(fromSeverityBrowseName, FilterAttribute.Severity, editModelBrowseName);
                    if (variableFromSeverity != null)
                        filterData.TextBoxes.GetValueOrDefault(fromSeverityBrowseName).Text = variableFromSeverity.Value;

                    var variableToSeverity = GetFiltersModelVariable(toSeverityBrowseName, FilterAttribute.Severity, editModelBrowseName);
                    if (variableToSeverity != null)
                        filterData.TextBoxes.GetValueOrDefault(toSeverityBrowseName).Text = variableToSeverity.Value;
                }
                else
                {
                    filterData.TextBoxes.GetValueOrDefault(fromSeverityBrowseName).Text = "1";
                    filterData.TextBoxes.GetValueOrDefault(toSeverityBrowseName).Text = "1000";
                }
            }
        }

        /// <summary>
        /// This method initializes checkboxes based on the provided model browse name.
        /// It iterates through a list of filters and sets the checked status of each checkbox
        /// according to the value retrieved from the model.
        /// </summary>
        /// <param name="editModelBrowseName">The name of the model used to retrieve the filter values.</param>
        /// <returns>
        /// No return value; the method modifies the checkboxes in place.
        /// </returns>
        private void InitializeCheckBoxes(string editModelBrowseName)
        {
            foreach (var (filter, isChecked) in from filter in alarmFilterData.Filters
                                                let isChecked = GetFiltersModelVariable(filter.Name, filter.Attribute, editModelBrowseName).Value
                                                select (filter, isChecked))
                filter.IsChecked = isChecked;
        }

        /// <summary>
        /// This method expands the accordions based on the checked status of the filters.
        /// It retrieves a list of distinct attributes from the filters and checks if any filter with that attribute is checked.
        /// If so, it expands the corresponding accordion.
        /// </summary>
        private void ExpandAccordions()
        {
            var attributes = alarmFilterData.Filters
                      .Select(x => x.Attribute)
                      .Distinct()
                      .ToList();

            foreach (var attribute in attributes)
            {
                var isChecked = alarmFilterData.Filters.FindAll(x => x.Attribute == attribute)
                                       .Any(x => x.IsChecked);

                var filter = (CheckBoxFilter)alarmFilterData.Filters.First(x => x.Attribute == attribute);
                ExpandAccordion(filter, isChecked);
            }
        }

        /// <summary>
        /// This method sets the expanded state of the Accordion control associated with the provided filter.
        /// </summary>
        /// <param name="filter">The filter object containing the Accordion control.</param>
        /// <param name="value">A boolean value indicating whether the Accordion should be expanded.</param>
        /// <returns>
        /// The method does not return a value.
        /// </returns>
        private static void ExpandAccordion(CheckBoxFilter filter, bool value)
        {
            filter.Accordion.Expanded = value;
        }

        /// <summary>
        /// This method saves the checked status of filters into the corresponding model variables.
        /// </summary>
        /// <param name="filter">The filter to process.</param>
        /// <returns>
        /// No return value, as the method modifies state directly.
        /// </returns>
        private void SaveCheckBoxes()
        {
            foreach (var filter in alarmFilterData.Filters)
            {
                var variable = GetFiltersModelVariable(filter.Name, filter.Attribute, defaultEditModelBrowseName);
                if (variable != null)
                    variable.Value = filter.IsChecked;
            }
        }

        /// <summary>
        /// This method sets the values of date-time pickers based on configuration variables.
        /// It checks if the event time variable is set and then retrieves the corresponding
        /// values from the event time browse names, updating the filters model variable
        /// with the retrieved values.
        /// </summary>
        /// <remarks>
        /// The method uses <see cref="filterConfiguration.GetVariable"/> to retrieve
        /// variables from the configuration, and <see cref="GetFiltersModelVariable"/> to
        /// update the filters model variable with the event time values.
        /// </remarks>
        private void SaveDateTimePickers()
        {
            var eventTimeVariable = filterConfiguration.GetVariable(eventTimeBrowseName);
            if (eventTimeVariable.Value)
            {
                if (eventTimeVariable.GetVariable(fromEventTimeBrowseName).Value)
                {
                    var variable = GetFiltersModelVariable(fromEventTimeDateTimeBrowseName, FilterAttribute.EventTime, defaultEditModelBrowseName);
                    if (variable != null)
                        variable.Value = filterData.EventTimePickers.GetValueOrDefault(fromEventTimeBrowseName).Value;
                }
                if (eventTimeVariable.GetVariable(toEventTimeBrowseName).Value)
                {
                    var variable = GetFiltersModelVariable(toEventTimeDateTimeBrowseName, FilterAttribute.EventTime, defaultEditModelBrowseName);
                    if (variable != null)
                        variable.Value = filterData.EventTimePickers.GetValueOrDefault(toEventTimeBrowseName).Value;
                }
            }
        }

        /// <summary>
        /// This method saves the values of text boxes into the corresponding model variables.
        /// It checks if the severity attribute variable is set and retrieves the values from the text boxes,
        /// updating the filters model variable with the retrieved values.
        /// </summary>
        private void SaveTextBoxes()
        {
            var severityAttributeVariable = filterConfiguration.GetVariable(severityBrowseName);
            var severityChildVariable = severityAttributeVariable.GetVariable(severityBrowseName);
            if (severityAttributeVariable.Value && severityChildVariable.Value)
            {
                var variableFromSeverity = GetFiltersModelVariable(fromSeverityBrowseName, FilterAttribute.Severity, defaultEditModelBrowseName);
                if (variableFromSeverity != null)
                    variableFromSeverity.Value = filterData.TextBoxes.GetValueOrDefault(fromSeverityBrowseName).Text;

                var variableToSeverity = GetFiltersModelVariable(toSeverityBrowseName, FilterAttribute.Severity, defaultEditModelBrowseName);
                if (variableToSeverity != null)
                    variableToSeverity.Value = filterData.TextBoxes.GetValueOrDefault(toSeverityBrowseName).Text;
            }
        }

        /// <summary>
        /// Retrieves a variable from a filter model based on a browse name, attribute, and edit model browse name.
        /// </summary>
        /// <param name="browseName">The browse name of the variable to retrieve.</param>
        /// <param name="attribute">The filter attribute to use for lookup.</param>
        /// <param name="editModelBrowseName">The browse name of the edit model to use.</param>
        /// <returns>
        /// The variable from the filter model, or null if the attribute or variable is not found.
        /// </returns>
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

        private CheckBoxFilterData filterData { get => (CheckBoxFilterData)alarmFilterData.Data; }
        private readonly AlarmFilterDataLogic alarmFilterData = new()
        {
            Data = new CheckBoxFilterData()
        };
        private readonly AlarmFilterQueryBuilderLogic queryBuilder;
        private readonly IUANode Owner;
        private readonly IUANode filterConfiguration;
    }

    private AlarmFilter alarmFilter;
}
