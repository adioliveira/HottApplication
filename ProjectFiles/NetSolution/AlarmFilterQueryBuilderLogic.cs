#region Using directives
using UAManagedCore;
using FTOptix.NetLogic;
using System.Linq;
using System.Text;
using FTOptix.Alarm;
using FTOptix.SerialPort;
using FTOptix.SQLiteStore;
using FTOptix.MQTTBroker;
#endregion

public class AlarmFilterQueryBuilderLogic : BaseNetLogic
{
    public IUAVariable Query { get; set; }

    /// <summary>
    /// This method updates the query value to the string representation of the new query.
    /// </summary>
    /// <remarks>
    /// This method is used to set the query value to a string representation of the new query.
    /// </remarks>
    public void RefreshQuery()
    {
        Query.Value = newQuery.ToString();
    }

    /// <summary>
    /// This method builds the SQL query based on the provided filter data and configuration.
    /// </summary>
    /// <param name="alarmFilterData">The filter data containing the selected attributes and their values.</param>
    /// <param name="filterConfiguration">The filter configuration containing the variables for event time and severity.</param>
    /// <remarks>
    /// This method constructs the SQL query by appending the mandatory SQL part and the conditions based on the selected attributes.
    /// It also handles the event time and severity filters if they are enabled in the configuration.
    /// </remarks>
    public void BuildQuery(AlarmFilterDataLogic alarmFilterData, IUANode filterConfiguration)
    {
        newQuery.Clear();
        newQuery.Append(mandatorySQLpart);
        var wasWhereAdded = false;
        string groupStatement;

        var activeAttributes = alarmFilterData.Filters.FindAll(x => x.IsChecked)
                                                      .Select(x => x.Attribute)
                                                      .Distinct()
                                                      .ToList();

        var eventTimeVariable = filterConfiguration.GetVariable(AlarmFilterDataLogic.eventTimeBrowseName);
        if (eventTimeVariable.Value)
        {
            if (eventTimeVariable.GetVariable(AlarmFilterDataLogic.fromEventTimeBrowseName).Value)
                fromEventTime = alarmFilterData.Data.FromEventTime.ToUniversalTime().ToString("o");
            if (eventTimeVariable.GetVariable(AlarmFilterDataLogic.toEventTimeBrowseName).Value)
                toEventTime = alarmFilterData.Data.ToEventTime.ToUniversalTime().AddSeconds(1).ToString("o");
        }

        var severityAttributeVariable = filterConfiguration.GetVariable(AlarmFilterDataLogic.severityBrowseName);
        var severityChildVariable = severityAttributeVariable.GetVariable(AlarmFilterDataLogic.severityBrowseName);
        if (severityAttributeVariable.Value && severityChildVariable.Value)
        {
            fromSeverity = alarmFilterData.Data.FromSeverity;
            toSeverity = alarmFilterData.Data.ToSeverity;
        }

        foreach (var attribute in activeAttributes)
        {
            groupStatement = BuildStatementForGroup(attribute, alarmFilterData);

            if (!string.IsNullOrEmpty(groupStatement))
            {
                if (!wasWhereAdded)
                {
                    newQuery.Append(Where);
                    wasWhereAdded = true;
                }

                newQuery.Append(groupStatement);
                newQuery.Append(And);
            }
        }

        // remove trailing " AND "
        if (wasWhereAdded)
            newQuery.Remove(newQuery.Length - And.Length, And.Length);
    }

    /// <summary>
    /// This method builds the SQL statement for a specific group of filters based on the provided attribute and filter data.
    /// </summary>
    /// <param name="attribute">The attribute for which the SQL statement is being built.</param>
    /// <param name="alarmFilterData">The filter data containing the selected filters for the attribute.</param>
    /// <remarks>
    /// This method constructs the SQL statement for a specific group of filters based on the provided attribute and filter data.
    /// It handles the conditions for event time and severity if they are enabled in the configuration.
    /// </remarks>
    /// <returns>The SQL statement for the specified group of filters.</returns>
    private string BuildStatementForGroup(AlarmFilterDataLogic.FilterAttribute attribute, AlarmFilterDataLogic alarmFilterData)
    {
        StringBuilder result = new();
        var activeGroupFiltersCounter = 0;
        var isFromEventTimeChecked = false;

        var activeFilters = alarmFilterData.Filters.FindAll(x => x.IsChecked && x.Attribute == attribute);

        foreach (var filter in activeFilters)
        {
            if (!string.IsNullOrEmpty(filter.SqlCondition))
            {
                result.Append(filter.SqlCondition);
            }
            else
            {
                if (filter.Name.Equals(AlarmFilterDataLogic.fromEventTimeBrowseName))
                {
                    isFromEventTimeChecked = true;
                    result.Append("(Time >= \"");
                    result.Append(fromEventTime);
                    result.Append("\")");
                }
                else if (filter.Name.Equals(AlarmFilterDataLogic.toEventTimeBrowseName))
                {
                    if (isFromEventTimeChecked)
                    {
                        // replace ") OR " to " AND "
                        result.Remove(result.Length - ClosingBracketOr.Length, ClosingBracketOr.Length);
                        result.Append(And);
                        result.Append("Time < \"");
                        result.Append(toEventTime);
                        result.Append("\")");
                    }
                    else
                    {
                        result.Append("(Time < \"");
                        result.Append(toEventTime);
                        result.Append("\")");
                    }
                }
                else if (filter.Name.Equals(AlarmFilterDataLogic.severityBrowseName))
                {
                    result.Append("(Severity >= ");
                    result.Append(fromSeverity);
                    result.Append(And);
                    result.Append("Severity <= ");
                    result.Append(toSeverity);
                    result.Append(')');
                }
                else
                {
                    throw new CoreConfigurationException("SQL condition cannot be empty.");
                }
            }

            result.Append(Or);
            activeGroupFiltersCounter++;
        }

        // remove trailing " OR "
        if (result.Length > 0)
            result.Remove(result.Length - Or.Length, Or.Length);

        if (activeGroupFiltersCounter >= 2)
        {
            result.Insert(0, "(");
            result.Append(')');
        }

        return result.ToString();
    }

    private readonly StringBuilder newQuery = new StringBuilder(mandatorySQLpart, 1024);
    private string toEventTime, fromEventTime;
    private int fromSeverity, toSeverity;
    private static readonly string mandatorySQLpart = "SELECT * FROM Model";
    private static readonly string Or = " OR ";
    private static readonly string ClosingBracketOr = ") OR ";
    private static readonly string And = " AND ";
    private static readonly string Where = " WHERE ";
}
