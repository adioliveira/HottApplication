#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using static AlarmFilterDataLogic;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using FTOptix.Alarm;
using FTOptix.SerialPort;
using FTOptix.SQLiteStore;
using FTOptix.MQTTBroker;
using FTOptix.OPCUAServer;
#endregion

public class AlarmFilterEditModelLogic : BaseNetLogic
{
    /// <summary>
    /// This method creates and edits a model based on the provided parent node and filters configuration.
    /// </summary>
    /// <param name="parentNode">The parent node for the model.</param>
    /// <param name="filtersConfiguration">The configuration for filters used in the model.</param>
    /// <param name="editModelBrowseName">The browse name for the model, defaults to 'defaultEditModelBrowseName'</param>
    /// <returns>
    /// The method does not return a value.
    /// </returns>
    public static void CreateEditModel(IUANode parentNode, IUANode filtersConfiguration, string editModelBrowseName = defaultEditModelBrowseName)
    {
        FilterEditModel.Create(parentNode, filtersConfiguration, editModelBrowseName);
    }

    /// <summary>
    /// This method retrieves an edit model based on the provided parent node and browse name.
    /// If the model is not found, it throws a CoreConfigurationException.
    /// </summary>
    /// <param name="parentNode">The parent node to search for the edit model.</param>
    /// <param name="editModelBrowseName">The browse name of the edit model to retrieve.</param>
    /// <returns>
    /// An instance of IUAObject representing the edit model.
    /// </returns>
    public static IUAObject GetEditModel(IUANode parentNode, string editModelBrowseName = defaultEditModelBrowseName)
    {
        var filterEditModel = parentNode.GetObject(editModelBrowseName);
        return filterEditModel ?? throw new CoreConfigurationException($"Edit model {editModelBrowseName} filters not found");
    }

    public static void DeleteEditModel(IUANode parentNode, string editModelBrowseName = defaultEditModelBrowseName)
    {
        FilterEditModel.Delete(parentNode, editModelBrowseName);
    }

    /// <summary>
    /// This method updates the type and instances of the provided nodes and edit models.
    /// It calls the <see cref="FilterEditModel.UpdateTypeAndInstances"/> method to perform the update.
    /// </summary>
    /// <param name="parentNode">The parent node to update.</param>
    /// <param name="filtersConfiguration">The configuration for filters to apply.</param>
    /// <param name="editModels">A collection of edit models to update.</param>
    /// <returns>
    /// No return value is returned; the method modifies the nodes and models in place.
    /// </returns>
    public static void UpdateTypeAndEditModels(IUANode parentNode, IUANode filtersConfiguration, IEnumerable<IUAObject> editModels)
    {
        FilterEditModel.UpdateTypeAndInstances(parentNode, filtersConfiguration, editModels);
    }

    private static class FilterEditModel
    {
        /// <summary>
        /// Creates an instance of a type based on the given parent node and filters configuration,
        /// and adds it to the parent node's collection. If the specified edit model browse name
        /// is not found, a new object is created and added to the parent node.
        /// </summary>
        /// <param name="parentNode">The parent node in the information model.</param>
        /// <param name="filtersConfiguration">The configuration for type updates.</param>
        /// <param name="editModelBrowseName">The browse name of the edit model to create or find.</param>
        /// <returns>
        /// No return value (void).
        /// </returns>
        public static void Create(IUANode parentNode, IUANode filtersConfiguration, string editModelBrowseName)
        {
            var typeObject = CreateType(parentNode);
            UpdateType(typeObject, filtersConfiguration);

            var editModelFilters = parentNode.Find(editModelBrowseName);
            if (editModelFilters == null)
            {
                editModelFilters = InformationModel.MakeObject(editModelBrowseName, typeObject.NodeId);
                parentNode.Add(editModelFilters);
            }

            UpdateInstance(editModelFilters, typeObject);
        }

        /// <summary>
        /// Updates the type and instances based on the provided parent node, filters configuration, and edit models.
        /// </summary>
        /// <param name="parentNode">The parent node to use for type creation.</param>
        /// <param name="filtersConfiguration">The filters configuration to use for type update.</param>
        /// <param name="editModels">A collection of edit models to update.</param>
        /// <returns>
        /// No return value.
        /// </returns>
        public static void UpdateTypeAndInstances(IUANode parentNode, IUANode filtersConfiguration, IEnumerable<IUAObject> editModels)
        {
            var typeObject = CreateType(parentNode);
            UpdateType(typeObject, filtersConfiguration);

            foreach (var editModel in editModels)
            {
                if (editModel.BrowseName != filtersConfigurationBrowseName && editModel.BrowseName != netLogicFileBrowseName)
                    UpdateInstance(editModel, typeObject);
            }
        }

        /// <summary>
        /// This method removes an edit model filter from the specified parent node.
        /// </summary>
        /// <param name="parentNode">The parent node from which to remove the edit model filter.</param>
        /// <param name="editModelBrowseName">The browse name of the edit model filter to remove.</param>
        /// <returns>
        /// No return value (void).
        /// </returns>
        public static void Delete(IUANode parentNode, string editModelBrowseName)
        {
            var editModelFilters = parentNode.GetObject(editModelBrowseName);
            if (editModelFilters != null)
                parentNode.Remove(editModelFilters);
        }

        /// <summary>
        /// This method creates a new instance of the IUANode type based on the provided parent node.
        /// It retrieves the components ID from the parent node, looks up the components folder,
        /// and finds the appropriate edit model filters. If not found, it creates a new
        /// edit model filters type and returns it; otherwise, it returns the existing one.
        /// </summary>
        /// <param name="parentNode">The parent node from which to retrieve the components ID and browse name.</param>
        /// <returns>
        /// An IUANode instance representing the edit model filters, either newly created or existing.
        /// </returns>
        /// <remarks>
        /// This method assumes that <see cref="GetVariable"/> and <see cref="Find"/> methods are properly implemented
        /// to retrieve and search for the required data in the information model.
        /// </remarks>
        private static IUANode CreateType(IUANode parentNode)
        {
            var componentsId = parentNode.GetVariable(alarmWidgetComponentsBrowseName).Value;
            var componentsFolder = InformationModel.Get(componentsId);
            var editModelFilters = componentsFolder.Find(parentNode.BrowseName);

            if (editModelFilters == null)
            {
                var newEditModelFiltersType = InformationModel.MakeObjectType(parentNode.BrowseName);
                componentsFolder.Add(newEditModelFiltersType);
                return newEditModelFiltersType;
            }
            return editModelFilters;
        }

        /// <summary>
        /// This method updates the type of the provided edit model based on the filters configuration.
        /// It iterates through the children of the filters configuration,
        /// checking if each child is visible.
        /// If a child is not visible, it removes the corresponding variable from the edit model.
        /// If a child is visible and the variable does not exist, it creates a new variable and adds it to the edit model.
        /// If the child is visible, it updates the attribute of the edit model based on the filters configuration.
        /// </summary>
        /// <param name="editModel">The edit model to update.</param>
        /// <param name="filtersConfiguration">The filters configuration to use for the update.</param>
        private static void UpdateType(IUANode editModel, IUANode filtersConfiguration)
        {
            foreach (var attribute in filtersConfiguration.Children)
            {
                var setting = editModel.GetVariable(attribute.BrowseName);

                if (!IsVisible(attribute))
                {
                    if (setting != null)
                        editModel.Remove(setting);
                }
                else
                {
                    if (setting == null)
                    {
                        setting = InformationModel.MakeVariable(attribute.BrowseName, OpcUa.DataTypes.BaseDataType);
                        editModel.Add(setting);
                    }
                    UpdateAttribute(setting, attribute);
                }
            }
        }

        /// <summary>
        /// This method updates the attribute based on the browse name of the filters configuration attribute.
        /// If the browse name matches <see cref="eventTimeBrowseName"}, it updates the event time and returns.
        /// If the browse name matches <see cref="severityBrowseName"}, it updates the severity and returns.
        /// If the browse name does not match either, it recursively updates all child nodes.
        /// </summary>
        /// <param name="editModel">The model to update.</param>
        /// <param name="filtersConfigurationAttribute">The filters configuration attribute to process.</param>
        /// <returns>
        /// The method does not return a value.
        /// </returns>
        private static void UpdateAttribute(IUANode editModel, IUANode filtersConfigurationAttribute)
        {
            if (filtersConfigurationAttribute.BrowseName == eventTimeBrowseName)
            {
                UpdateEventTime(editModel, filtersConfigurationAttribute);
                return;
            }

            if (filtersConfigurationAttribute.BrowseName == severityBrowseName)
            {
                UpdateSeverity(editModel, filtersConfigurationAttribute);
                return;
            }

            foreach (var child in filtersConfigurationAttribute.Children)
                Update(editModel, child);
        }

        /// <summary>
        /// This method updates the event time based on the filters configuration attribute.
        /// It iterates through the children of the filters configuration attribute,
        /// checking if each child is visible.
        /// If a child is not visible, it removes the corresponding variable from the edit model.
        /// If a child is visible and the variable does not exist, it creates a new variable and adds it to the edit model.
        /// If the child is visible, it updates the event time variable based on the filters configuration.
        /// </summary>
        /// <param name="editModel">The model to update.</param>
        /// <param name="filtersConfigurationAttribute">The filters configuration attribute to process.</param>
        private static void UpdateEventTime(IUANode editModel, IUANode filtersConfigurationAttribute)
        {
            foreach (var child in filtersConfigurationAttribute.Children)
            {
                Update(editModel, child);
                var childVisible = IsVisible(child);
                if (child.BrowseName == fromEventTimeBrowseName)
                {
                    var dateTime = editModel.GetVariable(fromEventTimeDateTimeBrowseName);

                    if (!childVisible)
                    {
                        if (dateTime != null)
                            editModel.Remove(dateTime);
                    }
                    else if (dateTime == null)
                    {
                        dateTime = InformationModel.MakeVariable(fromEventTimeDateTimeBrowseName, OpcUa.DataTypes.DateTime);
                        dateTime.Value = DateTime.Now;
                        editModel.Add(dateTime);
                    }
                }

                if (child.BrowseName == toEventTimeBrowseName)
                {
                    var dateTime = editModel.GetVariable(toEventTimeDateTimeBrowseName);

                    if (!childVisible)
                    {
                        if (dateTime != null)
                            editModel.Remove(dateTime);
                    }
                    else if (dateTime == null)
                    {
                        dateTime = InformationModel.MakeVariable(toEventTimeDateTimeBrowseName, OpcUa.DataTypes.DateTime);
                        dateTime.Value = DateTime.Now;
                        editModel.Add(dateTime);
                    }
                }
            }
        }

        /// <summary>
        /// This method updates the severity based on the filters configuration attribute.
        /// It iterates through the children of the filters configuration attribute,
        /// checking if each child is visible.
        /// If a child is not visible, it removes the corresponding variable from the edit model.
        /// If a child is visible and the variable does not exist, it creates a new variable and adds it to the edit model.
        /// If the child is visible, it updates the severity variable based on the filters configuration.
        /// </summary>
        /// <param name="editModel">The model to update.</param>
        /// <param name="filtersConfigurationAttribute">The filters configuration attribute to process.</param>
        private static void UpdateSeverity(IUANode editModel, IUANode filtersConfigurationAttribute)
        {
            foreach (var child in filtersConfigurationAttribute.Children)
            {
                Update(editModel, child);

                var childVisible = IsVisible(child);
                if (child.BrowseName == severityBrowseName)
                {
                    var fromSeverity = editModel.GetVariable(fromSeverityBrowseName);
                    var toSeverity = editModel.GetVariable(toSeverityBrowseName);

                    if (!childVisible)
                    {
                        if (fromSeverity != null)
                            editModel.Remove(fromSeverity);
                        if (toSeverity != null)
                            editModel.Remove(toSeverity);
                    }
                    else
                    {
                        if (fromSeverity == null)
                        {
                            fromSeverity = InformationModel.MakeVariable(fromSeverityBrowseName, OpcUa.DataTypes.UInt16);
                            fromSeverity.Value = 1;
                            editModel.Add(fromSeverity);
                        }
                        if (toSeverity == null)
                        {
                            toSeverity = InformationModel.MakeVariable(toSeverityBrowseName, OpcUa.DataTypes.UInt16);
                            toSeverity.Value = 1000;
                            editModel.Add(toSeverity);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method updates the specified model by setting or removing a variable based on visibility.
        /// </summary>
        /// <param name="editModel">The model to update.</param>
        /// <param name="filtersConfigurationChild">The node representing the filters configuration.</param>
        /// <remarks>
        /// The method checks if the node is visible. If it is not, it removes the variable if it exists.
        /// If the variable does not exist, it creates a new boolean variable with the specified browse name and adds it to the model.
        /// </remarks>
        private static void Update(IUANode editModel, IUANode filtersConfigurationChild)
        {
            var setting = editModel.GetVariable(filtersConfigurationChild.BrowseName);

            if (!IsVisible(filtersConfigurationChild))
            {
                if (setting != null)
                    editModel.Remove(setting);
            }
            else if (setting == null)
            {
                setting = InformationModel.MakeVariable(filtersConfigurationChild.BrowseName, OpcUa.DataTypes.Boolean);
                editModel.Add(setting);
            }
        }

        /// <summary>
        /// This method checks if a given IUANode is visible by retrieving its value from the InformationModel.
        /// </summary>
        /// <param name="node">The IUANode to check visibility for.</param>
        /// <returns>
        /// A boolean value indicating whether the node is visible. The value is retrieved from the InformationModel.
        /// </returns>
        private static bool IsVisible(IUANode node)
        {
            return InformationModel.GetVariable(node.NodeId).Value;
        }

        /// <summary>
        /// This method updates the instance by removing unnecessary properties and adding missing properties based on the provided type.
        /// </summary>
        /// <param name="instance">The instance to be updated.</param>
        /// <param name="type">The type to determine which properties to remove or add.</param>
        private static void UpdateInstance(IUANode instance, IUANode type)
        {
            RemoveUnnecessaryInstanceProperties(instance, type);
            AddMissingInstanceProperties(instance, type);
        }

        /// <summary>
        /// Recursively removes unnecessary instance properties from a node and its descendants.
        /// </summary>
        /// <param name="instance">The IUANode instance to process.</param>
        /// <param name="type">The IUANode type to look up for child nodes.</param>
        /// <remarks>
        /// The method traverses the children of the instance and checks if each child exists in the type.
        /// If a child does not exist, it is removed. If it does, the method is recursively called on the child.
        /// </remarks>
        private static void RemoveUnnecessaryInstanceProperties(IUANode instance, IUANode type)
        {
            foreach (var instanceChild in instance.Children)
            {
                var typeChild = type.Get(instanceChild.BrowseName);

                if (typeChild == null)
                    instance.Remove(instanceChild);
                else
                    RemoveUnnecessaryInstanceProperties(instanceChild, typeChild);
            }
        }

        /// <summary>
        /// This method adds missing instance properties to a given IUANode by recursively traversing the type hierarchy.
        /// It retrieves child nodes from the instance and creates missing variables if they are not already present.
        /// </summary>
        /// <param name="instance">The instance node to which missing properties are added.</param>
        /// <param name="type">The type node containing the child nodes to be processed.</param>
        private static void AddMissingInstanceProperties(IUANode instance, IUANode type)
        {
            foreach (var typeChild in type.Children)
            {
                var typeVariable = (IUAVariable)typeChild;
                if (typeVariable == null)
                    continue;

                var instanceChild = instance.Get(typeVariable.BrowseName);
                if (instanceChild == null)
                {
                    var instanceVariable = InformationModel.MakeVariable(
                        typeVariable.BrowseName,
                        typeVariable.DataType,
                        typeVariable.VariableType.NodeId,
                        typeVariable.ArrayDimensions);
                    instanceVariable.Value = typeVariable.Value;
                    instanceVariable.Prototype = typeVariable;
                    instance.Add(instanceVariable);
                    instanceChild = instanceVariable;
                }
                AddMissingInstanceProperties(instanceChild, typeChild);
            }
        }
    }

    private const string alarmWidgetComponentsBrowseName = "AlarmWidgetComponents";
    private const string netLogicFileBrowseName = "AlarmWidgetGenerateDefaultFiltersToggle";
}
