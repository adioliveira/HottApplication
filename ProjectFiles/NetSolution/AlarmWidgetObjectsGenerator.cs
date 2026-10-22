#region Using directives
using FTOptix.Core;
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
using OpcUa = UAManagedCore.OpcUa;
#endregion

public class AlarmWidgetObjectsGenerator : BaseNetLogic
{
    /// <summary>
    /// This method adds filter chips to the specified layout, based on the provided alarm filter data.
    /// If no filter chips are found, it creates a new one with specified gaps and alignment.
    /// It then populates the filter chips with active filters and adds a clear all button if applicable.
    /// </summary>
    /// <param name="alarmFilterData">The alarm filter data containing the filters to be displayed.</param>
    /// <param name="filtersLayout">The layout container where the filter chips will be added.</param>
    /// <param name="callingObject">The object that initiated the operation.</param>
    public static void GenerateFilterChips(AlarmFilterDataLogic alarmFilterData, RowLayout filtersLayout, IUAObject callingObject)
    {
        if (alarmFilterData == null || callingObject == null || filtersLayout == null)
            return;

        var filterChips = filtersLayout.Get<RowLayout>("FilterChips");

        if (filterChips == null)
        {
            filterChips = InformationModel.Make<RowLayout>("FilterChips");
            filterChips.HorizontalGap = HorizontalGap;
            filterChips.VerticalGap = VerticalGap;
            filterChips.VerticalAlignment = VerticalAlignment.Center;
            filterChips.HorizontalAlignment = HorizontalAlignment.Stretch;
            filterChips.Wrap = true;
            filtersLayout.Add(filterChips);
        }
        else
            filterChips.Children.Clear();

        var activeFilters = alarmFilterData.Filters.FindAll(x => x.IsChecked);
        foreach (var filter in activeFilters)
        {
            GenerateFilterChip(filterChips, alarmFilterData, callingObject, filter);
        }

        var clearAllButton = filtersLayout.Get<Button>("ClearAll");

        if (clearAllButton == null)
            clearAllButton = GenerateClearAllButton(filtersLayout, callingObject);

        clearAllButton.Visible = (activeFilters.Count != 0);
    }

    /// <summary>
    /// This method creates a filter chip UI element based on the provided filter data and layout.
    /// It constructs a rectangular chip with a label and a close button, centered in the layout.
    /// </summary>
    /// <param name="layout">The layout container where the filter chip will be added.</param>
    /// <param name="alarmFilterData">Data logic for filtering the chip.</param>
    /// <param name="callingObject">The object that initiated the filter operation.</param>
    /// <param name="filter">The filter data to use for creating the chip.</param>
    /// <remarks>
    /// The chip is centered within the layout, with a rounded corner of 16 pixels.
    /// A label is added to the chip, displaying filter attributes and names.
    /// A close button is also added to the chip for user interaction.
    /// </remarks>
    public static void GenerateFilterChip(IUANode layout, AlarmFilterDataLogic alarmFilterData, IUAObject callingObject, Filter filter)
    {
        const int cornerRadius = 16;

        var chipRectangle = InformationModel.Make<Rectangle>(filter.Attribute + "." + filter.Name);
        chipRectangle.VerticalAlignment = VerticalAlignment.Center;
        chipRectangle.CornerRadius = cornerRadius;

        var rowLayout = InformationModel.Make<RowLayout>(defaultEditModelBrowseName);
        rowLayout.VerticalAlignment = VerticalAlignment.Center;


        GenerateFilterChipCloseButton(rowLayout, filter.Name, callingObject, alarmFilterData, filter);
        chipRectangle.Add(rowLayout);
        layout.Add(chipRectangle);
    }

    /// <summary>
    /// This method filters text based on the provided attribute and filter name.
    /// It returns a formatted string representing the filtered text.
    /// </summary>
    /// <param name="attribute">The attribute to use for filtering (EventTime or Severity).</param>
    /// <param name="filterName">The name of the filter being applied.</param>
    /// <param name="alarmFilterData">Data containing the event time and severity values.</param>
    /// <returns>
    /// A string representing the filtered text, formatted according to the attribute and filter name.
    /// </returns>
    private static string FilterChipText(FilterAttribute attribute, string filterName, AlarmFilterDataLogic alarmFilterData)
    {
        if (attribute == FilterAttribute.EventTime && filterName == fromEventTimeBrowseName)
            return TranslateFilterName(filterName) + " " + alarmFilterData.Data.FromEventTime;
        if (attribute == FilterAttribute.EventTime && filterName == toEventTimeBrowseName)
            return TranslateFilterName(filterName) + " " + alarmFilterData.Data.ToEventTime;
        if (attribute == FilterAttribute.Severity && filterName == severityBrowseName)
            return TranslateFilterName(filterName) + ": " + alarmFilterData.Data.FromSeverity + " - " + alarmFilterData.Data.ToSeverity;
        return TranslateFilterName(attribute.ToString()) + " - " + TranslateFilterName(filterName);
    }

    /// <summary>
    /// This method creates a filter chip close button with the specified filter name and adds it to the layout.
    /// The button is initialized with the given filter name and is associated with a mouse click event that
    /// triggers a action based on the 'Filter' event.
    /// </summary>
    /// <param name="layout">The layout container where the button will be added.</param>
    /// <param name="filterName">The name of the filter for which the button is created.</param>
    /// <param name="callingObject">The object that will handle the event when the button is clicked.</param>
    private static void GenerateFilterChipCloseButton(IUANode layout, string filterName, IUAObject callingObject, AlarmFilterDataLogic alarmFilterData, Filter filter)
    {
        var button = InformationModel.Make<ChipCloseButton>(filterName);

        button.Text = FilterChipText(filter.Attribute, filter.Name, alarmFilterData);

        MakeEventHandler(button, FTOptix.UI.ObjectTypes.MouseClickEvent, callingObject, "Filter",
            [new("filterBrowseName", OpcUa.DataTypes.String, filterName)]);

        layout.Add(button);
    }

    /// <summary>
    /// This method generates a "Clear All" button and adds it to the specified layout.
    /// The button is configured with a transparent background, right-aligned,
    /// and has a right margin.
    /// It also attaches a mouse click event handler to the calling object,
    /// which will trigger when the button is clicked.
    /// </summary>
    /// <param name="layout">The layout container to which the button is added.</param>
    /// <param name="callingObject">The object that will handle the mouse click event.</param>
    /// <returns>
    /// A Button object representing the "Clear All" button.
    /// </returns>
    private static Button GenerateClearAllButton(IUANode layout, IUAObject callingObject)
    {
        string clearAll = "ClearAll";
        var button = InformationModel.Make<Button>(clearAll);
        button.Text = TranslateFilterName(clearAll);
        button.RightMargin = Margin;
        button.Elide = Elide.Right;
        button.BackgroundColor = new Color(0, 0, 0, 0); //Transparent
        button.Visible = false;

        button.VerticalAlignment = VerticalAlignment.Center;
        button.HorizontalAlignment = HorizontalAlignment.Right;

        MakeEventHandler(button, FTOptix.UI.ObjectTypes.MouseClickEvent, callingObject, clearAll);

        layout.Add(button);
        return button;
    }

    /// <summary>
    /// This method generates custom filters toggle UI elements based on the provided layout and filter configuration.
    /// It creates an accordion with a header and content area, where the content area is populated with
    /// accordions for each filter configuration node.
    /// The method also adds an "Apply" button to the content area.
    /// </summary>
    /// <param name="layout">The layout container where the custom filters toggle will be added.</param>
    /// <param name="filterConfiguration">The filter configuration node containing the filter settings.</param>
    /// <param name="filterData">The filter data object containing the filter settings.</param>
    /// <param name="callingObject">The object that will handle the event when the button is clicked.</param>
    /// <remarks>
    /// The accordion is configured with horizontal and vertical alignment, margins, and a header label.
    /// The content area is populated with accordions for each filter configuration node,
    /// and an "Apply" button is added to trigger the filter application.
    /// </remarks>
    public static void GenerateCustomFiltersToggle(IUANode layout, IUANode filterConfiguration, CheckBoxFilterData filterData, IUAObject callingObject)
    {
        var accordion = InformationModel.Make<Accordion>(defaultEditModelBrowseName);
        accordion.HorizontalAlignment = HorizontalAlignment.Stretch;
        accordion.VerticalAlignment = VerticalAlignment.Center;
        accordion.RightMargin = Margin;

        //Header
        var label = InformationModel.Make<Label>("Label");
        label.Text = TranslateFilterName(defaultEditModelBrowseName);
        label.VerticalAlignment = VerticalAlignment.Center;
        label.TopMargin = Margin;
        label.BottomMargin = Margin;
        label.LeftMargin = Margin;
        label.Elide = Elide.Right;
        accordion.Header.Add(label);

        //Content
        var columnLayout = InformationModel.Make<ColumnLayout>(defaultEditModelBrowseName);
        columnLayout.LeftMargin = Margin;
        columnLayout.TopMargin = Margin;
        columnLayout.BottomMargin = Margin;
        columnLayout.VerticalGap = LargeVerticalGap;
        columnLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        columnLayout.VerticalAlignment = VerticalAlignment.Center;
        GenerateAccordions(columnLayout, filterConfiguration, filterData, callingObject);
        GenerateApplyButton(columnLayout, callingObject);

        accordion.Content.Add(columnLayout);

        layout.Add(accordion);
    }

    /// <summary>
    /// This method creates an "Apply" button and adds it to the given layout.
    /// The button is configured with the text "Apply", right-aligned, and
    /// has a right margin. It also attaches a mouse click event handler to the
    /// calling object, which will trigger when the button is clicked.
    /// </summary>
    /// <param name="layout">The layout container to which the button is added.</param>
    /// <param name="callingObject">The object that will handle the mouse click event.</param>
    /// <remarks>
    /// The button is created using the <see cref="InformationModel.Make<Button>"/> method,
    /// and its properties are set as specified. The event handler is attached
    /// using <see cref="MakeEventHandler"/>.
    /// </remarks>
    private static void GenerateApplyButton(IUANode layout, IUAObject callingObject)
    {
        const string apply = "Apply";
        var applyButton = InformationModel.Make<FilterApplyButton>(apply);
        MakeEventHandler(applyButton, FTOptix.UI.ObjectTypes.MouseClickEvent, callingObject, apply);

        layout.Add(applyButton);
    }

    /// <summary>
    /// This method generates accordions for each child node in the filter configuration.
    /// It checks if the child node is enabled and creates an accordion for it.
    /// The accordions are added to the specified layout.
    /// </summary>
    /// <param name="layout">The layout container where the accordions will be added.</param>
    /// <param name="filterConfiguration">The filter configuration node containing the filter settings.</param>
    /// <param name="filterData">The filter data object containing the filter settings.</param>
    /// <param name="callingObject">The object that will handle the event when the button is clicked.</param>
    private static void GenerateAccordions(IUANode layout, IUANode filterConfiguration, CheckBoxFilterData filterData, IUAObject callingObject)
    {
        foreach (var child in filterConfiguration.Children)
        {
            //Accordions should not be generated if they are not visible
            if (!IsAttributeEnabled(child.BrowseName, filterConfiguration))
                continue;

            var accordion = GenerateAccordion(child, filterConfiguration, filterData, callingObject);
            layout.Add(accordion);
        }
    }

    /// <summary>
    /// This method generates an accordion UI element based on the provided filter configuration node.
    /// It creates a header with a label and populates the content area with
    /// accordions for event time and severity filters.
    /// </summary>
    /// <param name="filterConfigurationNode">The filter configuration node containing the filter settings.</param>
    /// <param name="filterConfiguration">The filter configuration node containing the filter settings.</param>
    /// <param name="filterData">The filter data object containing the filter settings.</param>
    /// <param name="callingObject">The object that will handle the event when the button is clicked.</param>
    /// <returns>
    /// An Accordion object representing the filter configuration node.
    /// </returns>
    private static Accordion GenerateAccordion(IUANode filterConfigurationNode, IUANode filterConfiguration, CheckBoxFilterData filterData, IUAObject callingObject)
    {
        var accordion = InformationModel.Make<Accordion>(filterConfigurationNode.BrowseName);
        accordion.HorizontalAlignment = HorizontalAlignment.Stretch;
        accordion.VerticalAlignment = VerticalAlignment.Center;
        accordion.RightMargin = Margin;
        accordion.Expanded = false;

        //Header
        var label = InformationModel.Make<Label>("Label");
        label.Text = TranslateFilterName(filterConfigurationNode.BrowseName);
        label.VerticalAlignment = VerticalAlignment.Center;
        label.TopMargin = Margin;
        label.BottomMargin = Margin;
        label.LeftMargin = Margin;
        label.Elide = Elide.Right;
        accordion.Header.Add(label);

        //Content
        if (filterConfigurationNode.BrowseName == eventTimeBrowseName)
            accordion.Content.Add(GenerateLayoutForEventTime(filterConfigurationNode, filterConfiguration, filterData, callingObject));
        else if (filterConfigurationNode.BrowseName == severityBrowseName)
            accordion.Content.Add(GenerateLayoutForSeverity(filterConfigurationNode, filterConfiguration, filterData, callingObject));
        else
            accordion.Content.Add(GenerateColumnLayout(filterConfigurationNode, filterConfiguration, callingObject));

        return accordion;
    }

    /// <summary>
    /// Generates a column layout based on the provided node and filter configuration.
    /// The layout includes checkboxes for each child browse name that is enabled,
    /// and sets the margin values for the layout.
    /// </summary>
    /// <param name="node">The IUANode representing the node to generate the layout for.</param>
    /// <param name="filterConfiguration">The filter configuration to determine which child browse names are enabled.</param>
    /// <param name="callingObject">The calling object used to generate the checkbox.</param>
    /// <returns>
    /// A ColumnLayout object containing the configured column layout with enabled checkboxes and margin settings.
    /// </returns>
    private static ColumnLayout GenerateColumnLayout(IUANode node, IUANode filterConfiguration, IUAObject callingObject)
    {
        var columnLayout = InformationModel.Make<ColumnLayout>(node.BrowseName);

        foreach (var childBrowseName in node.Children.Select(child => child.BrowseName))
        {
            if (!IsCheckboxEnabled(childBrowseName, node.BrowseName, filterConfiguration))
                continue;
            var checkbox = GenerateCheckbox(childBrowseName, callingObject);
            columnLayout.Children.Add(checkbox);
        }
        columnLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        columnLayout.LeftMargin = Margin;
        columnLayout.TopMargin = Margin;
        columnLayout.RightMargin = Margin;

        return columnLayout;
    }

    /// <summary>
    /// This method generates a column layout for event time filtering based on a given node and filter configuration.
    /// It creates a layout with margins, horizontal and vertical alignment, and adds checkboxes and date pickers for filtering.
    /// </summary>
    /// <param name="node">The node to base the layout on.</param>
    /// <param name="filterConfiguration">The configuration for filtering rules.</param>
    /// <param name="filterData">The data containing filter information.</param>
    /// <param name="callingObject">The object that handles the event.</param>
    /// <returns>
    /// A <see cref="ColumnLayout"/> object representing the filtered layout.
    /// </returns>
    private static ColumnLayout GenerateLayoutForEventTime(IUANode node, IUANode filterConfiguration, CheckBoxFilterData filterData, IUAObject callingObject)
    {
        var columnLayout = InformationModel.Make<ColumnLayout>(node.BrowseName);
        columnLayout.LeftMargin = Margin;
        columnLayout.TopMargin = Margin;
        columnLayout.RightMargin = Margin;
        columnLayout.BottomMargin = Margin;
        columnLayout.VerticalGap = VerticalGap;
        columnLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        columnLayout.VerticalAlignment = VerticalAlignment.Center;

        foreach (var childBrowseName in node.Children.Select(child => child.BrowseName))
        {
            if (!IsCheckboxEnabled(childBrowseName, node.BrowseName, filterConfiguration))
                continue;

            var rowLayout = InformationModel.Make<RowLayout>("RowLayout");
            rowLayout.HorizontalAlignment = HorizontalAlignment.Stretch;

            var checkBox = InformationModel.Make<CheckBox>(childBrowseName);
            checkBox.VerticalAlignment = VerticalAlignment.Center;
            checkBox.Height = CheckBoxSize;
            checkBox.Width = CheckBoxSize;
            MakeEventHandler(checkBox, FTOptix.UI.ObjectTypes.UserValueChangedEvent, callingObject, "Filter",
                [new("filterBrowseName", OpcUa.DataTypes.String, checkBox.BrowseName)]);

            rowLayout.Add(checkBox);

            var columnLayout2 = InformationModel.Make<ColumnLayout>("ColumnLayout");
            columnLayout2.HorizontalAlignment = HorizontalAlignment.Stretch;
            columnLayout2.VerticalGap = SmallVerticalGap;
            columnLayout2.LeftMargin = Margin;
            rowLayout.Add(columnLayout2);

            var label = InformationModel.Make<Label>("Label");
            label.Text = TranslateFilterName(childBrowseName);
            label.Elide = Elide.Right;
            columnLayout2.Add(label);

            var dateTimePicker = InformationModel.Make<DateTimePicker>(childBrowseName);
            dateTimePicker.HorizontalAlignment = HorizontalAlignment.Stretch;

            filterData.EventTimePickers.Add(childBrowseName, dateTimePicker);

            columnLayout2.Add(dateTimePicker);

            columnLayout.Children.Add(rowLayout);
        }

        return columnLayout;
    }

    /// <summary>
    /// This method generates a layout for a severity level based on the provided node and configuration.
    /// It creates a row layout with specified margins and alignment, and optionally adds a checkbox
    /// based on filter conditions.
    /// </summary>
    /// <param name="node">The IUANode representing the severity node.</param>
    /// <param name="filterConfiguration">The filter configuration node.</param>
    /// <param name="filterData">The filter data for the severity level.</param>
    /// <param name="callingObject">The calling object for event handling.</param>
    /// <returns>
    /// A RowLayout object with the configured layout, including margins, alignment, and optional checkbox.
    /// </returns>
    private static RowLayout GenerateLayoutForSeverity(IUANode node, IUANode filterConfiguration, CheckBoxFilterData filterData, IUAObject callingObject)
    {
        var child = node.Get(severityBrowseName);

        var rowLayout = InformationModel.Make<RowLayout>(child.BrowseName);
        rowLayout.LeftMargin = Margin;
        rowLayout.TopMargin = Margin;
        rowLayout.RightMargin = Margin;
        rowLayout.BottomMargin = Margin;
        rowLayout.HorizontalGap = HorizontalGap;
        rowLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        rowLayout.VerticalAlignment = VerticalAlignment.Center;

        if (!IsCheckboxEnabled(child.BrowseName, node.BrowseName, filterConfiguration))
            return rowLayout;

        var checkBox = InformationModel.Make<CheckBox>(child.BrowseName);
        checkBox.VerticalAlignment = VerticalAlignment.Center;
        checkBox.Height = CheckBoxSize;
        checkBox.Width = CheckBoxSize;
        checkBox.Elide = Elide.Right;
        MakeEventHandler(checkBox, FTOptix.UI.ObjectTypes.UserValueChangedEvent, callingObject, "Filter",
            [new("filterBrowseName", OpcUa.DataTypes.String, checkBox.BrowseName)]);


        rowLayout.Add(checkBox);

        rowLayout.Add(GenerateColumnLayoutForSeverity(fromSeverityBrowseName, filterData));
        rowLayout.Add(GenerateColumnLayoutForSeverity(toSeverityBrowseName, filterData));

        return rowLayout;
    }

    /// <summary>
    /// This method creates a ColumnLayout for a given severity name and filter data.
    /// It sets up the layout with a label and a text box, both stretched horizontally,
    /// and adds the text box to the filter data collection.
    /// </summary>
    /// <param name="name">The name of the severity to be displayed.</param>
    /// <param name="filterData">The filter data containing the text box to be added.</param>
    /// <returns>
    /// A ColumnLayout object configured with a label and a text box, both stretched horizontally.
    /// </returns>
    /// <remarks>
    /// The layout uses a horizontal stretch for both elements and a small vertical gap.
    /// The text box is added to the filter data collection under the specified name.
    /// </remarks>
    private static ColumnLayout GenerateColumnLayoutForSeverity(string name, CheckBoxFilterData filterData)
    {
        var columnLayout = InformationModel.Make<ColumnLayout>(name);
        columnLayout.HorizontalAlignment = HorizontalAlignment.Stretch;
        columnLayout.VerticalGap = SmallVerticalGap;

        var label = InformationModel.Make<Label>("Label");
        label.Text = TranslateFilterName(name);
        label.Elide = Elide.Right;
        columnLayout.Add(label);

        var textBox = InformationModel.Make<TextBox>(name);
        textBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        columnLayout.Add(textBox);
        filterData.TextBoxes.Add(name, textBox);

        return columnLayout;
    }

    /// <summary>
    /// This method creates and configures a CheckBox control with specified properties.
    /// It sets the browse name, text, elide style, bottom margin, and attaches an event handler
    /// for the UserValueChangedEvent of the specified object type.
    /// </summary>
    /// <param name="browseName">The browse name for the CheckBox.</param>
    /// <param name="callingObject">The calling object that will handle the event.</param>
    /// <returns>
    /// A CheckBox control configured with the specified properties.
    /// </returns>
    private static CheckBox GenerateCheckbox(string browseName, IUAObject callingObject)
    {
        var checkBox = InformationModel.Make<FilterCheckbox>(browseName);
        checkBox.Text = TranslateFilterName(browseName);
        MakeEventHandler(checkBox, FTOptix.UI.ObjectTypes.UserValueChangedEvent, callingObject, "Filter",
            [new("filterBrowseName", OpcUa.DataTypes.String, checkBox.BrowseName)]);
        return checkBox;
    }

    /// <summary>
    /// This method checks if a specific attribute is enabled based on the provided filter configuration.
    /// </summary>
    /// <param name="browseName">The name of the browse to check.</param>
    /// <param name="filterConfiguration">The filter configuration object containing the attribute value.</param>
    /// <returns>
    /// A boolean value indicating whether the attribute is enabled. If the configuration is found, it returns the value; otherwise, it returns false.</returns>
    private static bool IsAttributeEnabled(string browseName, IUANode filterConfiguration)
    {
        var config = filterConfiguration.GetVariable(browseName);
        if (config != null)
            return config.Value;
        else
        {
            Log.Warning($"FilterConfiguration not contains configuration for accordion: {browseName}.");
            return false;
        }
    }

    /// <summary>
    /// This method checks if a checkbox is enabled based on the provided configuration.
    /// It retrieves the value of the specified attribute for the given browse name from the filter configuration.
    /// If the configuration is found, the value is returned; otherwise, a warning is logged and false is returned.
    /// </summary>
    /// <param name="browseName">The name of the browse to check.</param>
    /// <param name="attribute">The attribute to look up in the configuration.</param>
    /// <param name="filterConfiguration">The filter configuration object to retrieve the value from.</param>
    /// <returns>
    /// A boolean value indicating whether the checkbox is enabled.
    /// If the configuration is not found, returns false with a warning message.
    /// </returns>
    private static bool IsCheckboxEnabled(string browseName, string attribute, IUANode filterConfiguration)
    {
        var config = filterConfiguration.Get(attribute).GetVariable(browseName);
        if (config != null)
            return config.Value;
        else
        {
            Log.Warning($"FilterConfiguration not contains configuration for checkbox: {browseName} for attribute {attribute}.");
            return false;
        }
    }

    /// <summary>
    /// This method translates a given text ID to its corresponding translation.
    /// If a translation exists for the given text ID, it returns the translated text.
    /// If no translation is found, it returns the original text ID as a string.
    /// </summary>
    /// <param name="textId">The unique identifier for the text to be translated.</param>
    /// <returns>
    /// A string representing the translated text or the original text ID if no translation is found.
    /// </returns>
    private static string TranslateFilterName(string textId)
    {
        var translation = InformationModel.LookupTranslation(new LocalizedText(textId));
        if (!translation.IsEmpty())
        {
            return translation.Text;
        }
        return textId;
    }

    /// <summary>
    /// This method generates preset buttons based on the provided configuration nodes.
    /// It iterates through the nodes of the alarm widget configuration, creates a button
    /// for each node that is not a default edit model, filters configuration, or net logic file
    /// browse name, and attaches an event handler for mouse click events.
    /// The button is added to the base layout and displays the translated filter name.
    /// </summary>
    /// <param name="baseLayout">The layout to which the buttons are added.</param>
    /// <param name="alarmWidgetConfiguration">The configuration node containing the buttons to process.</param>
    /// <param name="callingObject">The object that handles the event when the button is clicked.</param>
    public static void GeneratePresetButtons(IUANode baseLayout, IUANode alarmWidgetConfiguration, IUAObject callingObject)
    {
        var nodes = alarmWidgetConfiguration.GetNodesByType<IUAObject>();

        foreach (var node in nodes.Select(n => n.BrowseName))
        {
            if (node == defaultEditModelBrowseName || node == filtersConfigurationBrowseName || node == netLogicFileBrowseName)
                continue;

            var button = InformationModel.Make<PresetFiltersButton>(node);
            button.Text = TranslateFilterName(node);

            MakeEventHandler(button, FTOptix.UI.ObjectTypes.MouseClickEvent, callingObject, "LoadPreset",
                [new("presetName", OpcUa.DataTypes.String, button.BrowseName)]
            );

            baseLayout.Add(button);
        }
    }

    /// <summary>
    /// Creates and adds an event handler to a node, sets its listen event type, and configures method parameters for invocation.
    /// </summary>
    /// <param name="parentNode">The node to which the event handler is added.</param>
    /// <param name="listenEventTypeId">The type ID of the event to listen for.</param>
    /// <param name="callingObject">The object associated with the method call.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="arguments">A list of arguments to pass to the method.</param>
    private static void MakeEventHandler(IUANode parentNode, NodeId listenEventTypeId, IUAObject callingObject, string methodName,
            List<Tuple<string, NodeId, object>> arguments = null)
    {
        var eventHandler = InformationModel.MakeObject<FTOptix.CoreBase.EventHandler>("EventHandler");
        parentNode.Add(eventHandler);

        eventHandler.ListenEventType = listenEventTypeId;

        var methodIndex = eventHandler.MethodsToCall.Any() ? eventHandler.MethodsToCall.Count + 1 : 1;
        var methodContainer = InformationModel.MakeObject($"MethodContainer{methodIndex}");
        eventHandler.MethodsToCall.Add(methodContainer);

        var objectPointerVariable = InformationModel.MakeVariable<NodePointer>("ObjectPointer", OpcUa.DataTypes.NodeId);
        objectPointerVariable.Value = callingObject.NodeId;
        methodContainer.Add(objectPointerVariable);

        var methodNameVariable = InformationModel.MakeVariable("Method", OpcUa.DataTypes.String);
        methodNameVariable.Value = methodName;
        methodContainer.Add(methodNameVariable);

        if (arguments != null)
            CreateInputArguments(methodContainer, arguments);
    }

    /// <summary>
    /// This method creates an InputArguments object and adds it to the provided method container.
    /// It then populates the object with values from a list of tuples, where each tuple contains a
    /// string identifier, a NodeId, and a value to be assigned to the variable.
    /// </summary>
    /// <param name="methodContainer">The container object where the InputArguments object will be added.</param>
    /// <param name="arguments">A list of tuples containing the identifier, NodeId, and value for each input argument.</param>
    private static void CreateInputArguments(
            IUANode methodContainer,
            List<Tuple<string, NodeId, object>> arguments)
    {
        IUAObject inputArguments = InformationModel.MakeObject("InputArguments");
        methodContainer.Add(inputArguments);

        foreach (var arg in arguments)
        {
            var argumentVariable = InformationModel.MakeVariable(
                arg.Item1,
                arg.Item2,
                OpcUa.VariableTypes.BaseDataVariableType);
            argumentVariable.Value = arg.Item3.ToString();
            inputArguments.Add(argumentVariable);
        }
    }

    private const string netLogicFileBrowseName = "AlarmWidgetGenerateDefaultFiltersToggle";
    private const int Margin = 8;
    private const int LargeVerticalGap = 16;
    private const int VerticalGap = 8;
    private const int SmallVerticalGap = 4;
    private const int HorizontalGap = 8;
    private const int CheckBoxSize = 40;
}
