#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using System;
using System.Collections.Generic;
using UAManagedCore;
using FTOptix.Alarm;
using FTOptix.SerialPort;
using FTOptix.SQLiteStore;
using FTOptix.MQTTBroker;
#endregion

public class AlarmFilterDataLogic : BaseNetLogic
{
    public enum FilterAttribute
    {
        AlarmState,
        Name,
        Class,
        EventTime,
        Group,
        Inhibit,
        Message,
        Priority,
        Severity,
        AlarmStatus
    }

    public abstract class Filter
    {
        public abstract bool IsChecked { get; set; }
        public string Name { get; }
        public FilterAttribute Attribute { get; }
        public string SqlCondition { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class with the provided attribute and name.
        /// The <see cref="SqlCondition"/> is initialized based on the provided name and attribute.
        /// </summary>
        /// <param name="attribute">The attribute associated with the filter.</param>
        /// <param name="name">The name of the filter.</param>
        /// <returns>
        /// A <see cref="Filter"/> instance with the specified attribute and name, and the <see cref="SqlCondition"/> initialized accordingly.
        /// </returns>
        protected Filter(FilterAttribute attribute, string name)
        {
            Attribute = attribute;
            Name = name;

            SqlCondition = presetSqlConditions.GetValueOrDefault(name) ??
                   GenerateSqlCondition(attribute, name);
        }

        /// <summary>
        /// This method generates a SQL condition based on the provided filter attribute and browse name.
        /// Depending on the attribute, it constructs a SQL LIKE condition with the translated browse name.
        /// </summary>
        /// <param name="attribute">The filter attribute (Inhibit, Class, Group, Name, or AlarmState).</param>
        /// <param name="checkboxBrowseName">The browse name to be used in the SQL condition.</param>
        /// <returns>
        /// A string representing the SQL condition.
        /// </returns>
        private static string GenerateSqlCondition(FilterAttribute attribute, string checkboxBrowseName)
        {
            if (attribute == FilterAttribute.Inhibit)
                return $"ShelvingState.CurrentState = '{TranslateFilterName(checkboxBrowseName)}'";
            if (attribute == FilterAttribute.Class)
                return $"RAAlarmData.AlarmClass LIKE '%{TranslateFilterName(checkboxBrowseName)}%'";
            if (attribute == FilterAttribute.Group)
                return $"RAAlarmData.AlarmGroup LIKE '%{TranslateFilterName(checkboxBrowseName)}%'";
            if (attribute == FilterAttribute.Name)
                return $"BrowseName LIKE '%{TranslateFilterName(checkboxBrowseName)}%'";
            if (attribute == FilterAttribute.AlarmState)
                return GenerateSqlConditionAlarmState(checkboxBrowseName);

            return $"{attribute} LIKE '%{TranslateFilterName(checkboxBrowseName)}%'";
        }

        /// <summary>
        /// This method generates a SQL condition string based on the provided checkbox browse name.
        /// It translates the filter name to its corresponding SQL string and constructs a condition
        /// that checks if the `CurrentState` matches one of the specified values.
        /// </summary>
        /// <param name="checkboxBrowseName">The name of the checkbox used to determine the condition.</param>
        /// <returns>
        /// A string representing the SQL condition, or an empty string if no match is found.
        /// </returns>
        private static string GenerateSqlConditionAlarmState(string checkboxBrowseName)
        {
            var highHigh = TranslateFilterName("HighHighState");
            var high = TranslateFilterName("HighState");
            var lowLow = TranslateFilterName("LowLowState");
            var low = TranslateFilterName("LowState");
            var active = TranslateFilterName("ActiveState");
            var inactive = TranslateFilterName("InactiveState");

            if (checkboxBrowseName == "HighHighState")
                return $"CurrentState IN ('{highHigh}','{highHigh} {high}')";
            if (checkboxBrowseName == "HighState")
                return $"CurrentState IN ('{high}','{highHigh} {high}')";
            if (checkboxBrowseName == "LowLowState")
                return $"CurrentState IN ('{lowLow}','{low} {lowLow}')";
            if (checkboxBrowseName == "LowState")
                return $"CurrentState IN ('{low}','{low} {lowLow}')";
            if (checkboxBrowseName == "ActiveStateDigital")
                return $"CurrentState IN ('{active}')";
            if (checkboxBrowseName == "InactiveState")
                return $"CurrentState IN ('{inactive}')";

            return "";
        }

        /// <summary>
        /// This method translates a given text ID to its corresponding translation.
        /// If a translation exists, it returns the translated text; otherwise, it returns the original text ID.
        /// </summary>
        /// <param name="textId">The ID of the text to be translated.</param>
        /// <returns>
        /// A string representing the translated text if a translation exists, otherwise the original text ID.
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

        private static readonly Dictionary<string, string> presetSqlConditions = new()
        {
            { "Urgent", "(Severity >= 751 AND Severity <= 1000)" },
            { "High", "(Severity >= 501 AND Severity <= 750)" },
            { "Medium", "(Severity >= 251 AND Severity <= 500)" },
            { "Low", "(Severity >= 1 AND Severity <= 250)" },
            { "NormalUnacked", "(ActiveState.Id = 0 AND AckedState.Id = 0)" },
            { "InAlarm", "ActiveState.Id = 1" },
            { "InAlarmAcked", "(ActiveState.Id = 1 AND AckedState.Id = 1)" },
            { "InAlarmUnacked", "(ActiveState.Id = 1 AND AckedState.Id = 0)" },
            { "InAlarmConfirmed", "(ActiveState.Id = 1 AND ConfirmedState.Id = 1)" },
            { "InAlarmUnconfirmed", "(ActiveState.Id = 1 AND ConfirmedState.Id = 0)" },
            { "Enabled", "EnabledState.Id = 1" },
            { "Disabled", "EnabledState.Id = 0" },
            { "Suppressed", "SuppressedState.Id = 1" },
            { "Unsuppressed", "SuppressedState.Id = 0" },
            { "Severity", ""},
            { "FromEventTime", ""},
            { "ToEventTime", ""}
        };
    }

    public class CheckBoxFilter : Filter
    {
        public override bool IsChecked { get => checkbox.Checked; set => checkbox.Checked = value; }
        public Accordion Accordion { get => accordion; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckBoxFilter"/> class with the specified checkbox, attribute, and accordion.
        /// The constructor initializes the <see cref="checkbox"/> property with the browse name from the checkbox and the <see cref="accordion"/> property.
        /// </summary>
        /// <param name="checkbox">The checkbox control to be used.</param>
        /// <param name="attribute">The filter attribute to be applied.</param>
        /// <param name="accordion">The accordion control to be used.</param>
        /// <remarks>
        /// The constructor sets up the <see cref="checkbox"/> property using the <see cref="BrowseName"/> property of the checkbox.
        /// </remarks>
        public CheckBoxFilter(CheckBox checkbox, FilterAttribute attribute, Accordion accordion) : base(attribute, checkbox.BrowseName)
        {
            this.checkbox = checkbox;
            this.accordion = accordion;
        }

        private readonly Accordion accordion;
        private readonly CheckBox checkbox;
    }

    public class ToggleFilter : Filter
    {
        public override bool IsChecked { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleFilter"/> class with the specified name, check state, and attribute.
        /// </summary>
        /// <param name="name">The name of the filter.</param>
        /// <param name="isChecked">A boolean value indicating whether the filter is checked.</param>
        /// <param name="attribute">The attribute associated with the filter.</param>
        /// <remarks>
        /// The constructor calls the base constructor with the attribute and name.
        /// </remarks>
        public ToggleFilter(string name, bool isChecked, FilterAttribute attribute) : base(attribute, name)
        {
            IsChecked = isChecked;
        }
    }

    public interface IFilterData
    {
        DateTime FromEventTime { get; }
        DateTime ToEventTime { get; }
        int FromSeverity { get; }
        int ToSeverity { get; }
    }

    public class CheckBoxFilterData : IFilterData
    {
        public DateTime FromEventTime => eventTimePickers.GetValueOrDefault(fromEventTimeBrowseName).Value;

        public DateTime ToEventTime
        {
            get { return eventTimePickers.GetValueOrDefault(toEventTimeBrowseName).Value; }
        }

        public int FromSeverity
        {
            get
            {
                if (Int32.TryParse(textBoxes.GetValueOrDefault(fromSeverityBrowseName).Text, out int result))
                    return result;
                else
                {
                    Log.Warning($"TextBox \"FromSeverity\" should contains integer value");
                    return 1;
                }
            }
        }

        public int ToSeverity
        {
            get
            {
                if (Int32.TryParse(textBoxes.GetValueOrDefault(toSeverityBrowseName).Text, out int result))
                    return result;
                else
                {
                    Log.Warning($"TextBox \"ToSeverity\" should contains integer value");
                    return 1000;
                }
            }
        }

        public Dictionary<string, DateTimePicker> EventTimePickers { get => eventTimePickers; }
        public Dictionary<string, TextBox> TextBoxes { get => textBoxes; }

        private readonly Dictionary<string, DateTimePicker> eventTimePickers = [];
        private readonly Dictionary<string, TextBox> textBoxes = [];
    }

    public class ToggleFilterData : IFilterData
    {
        public DateTime FromEventTime { get => fromEventTime; }
        public DateTime ToEventTime { get => toEventTime; }
        public int FromSeverity { get => fromSeverity; }
        public int ToSeverity { get => toSeverity; }

        /// <summary>
        /// This method sets the <see cref="fromEventTime"/> property with the provided <see cref="DateTime"/> value.
        /// </summary>
        /// <param name="fromEventTime">The DateTime value to set as the event time.</param>
        /// <remarks>
        /// The method assigns the provided DateTime value to the <see cref="fromEventTime"/> property.
        /// </remarks>
        public void SetFromEventTime(DateTime fromEventTime)
        {
            this.fromEventTime = fromEventTime;
        }
        /// <summary>
        /// This method sets the event time to the provided DateTime value.
        /// </summary>
        /// <param name="toEventTime">The DateTime value to set as the event time.</param>
        /// <remarks>
        /// This method assigns the provided DateTime to the instance variable <see cref="toEventTime"/>.
        /// </remarks>
        public void SetToEventTime(DateTime toEventTime)
        {
            this.toEventTime = toEventTime;
        }
        /// <summary>
        /// This method sets the value of the 'fromSeverity' property to the provided integer.
        /// </summary>
        /// <param name="fromSeverity">The severity level to set.</param>
        /// <remarks>
        /// The method assigns the input integer to the 'fromSeverity' field, which is used to
        /// determine the severity level in subsequent operations.
        /// </remarks>
        public void SetFromSeverity(int fromSeverity)
        {
            this.fromSeverity = fromSeverity;
        }
        /// <summary>
        /// This method sets the severity level to the provided integer value.
        /// </summary>
        /// <param name="toSeverity">The severity level to set.</param>
        /// <remarks>
        /// The method assigns the provided integer value to the <see cref="toSeverity"/> field.
        /// </remarks>
        public void SetToSeverity(int toSeverity)
        {
            this.toSeverity = toSeverity;
        }
        private DateTime toEventTime, fromEventTime;
        private int fromSeverity, toSeverity;
    }

    public List<Filter> Filters { get; } = [];
    public IFilterData Data { get; set; }

    public const string eventTimeBrowseName = "EventTime";
    public const string fromEventTimeBrowseName = "FromEventTime";
    public const string toEventTimeBrowseName = "ToEventTime";
    public const string fromEventTimeDateTimeBrowseName = "FromEventTimeDateTime";
    public const string toEventTimeDateTimeBrowseName = "ToEventTimeDateTime";
    public const string dateTimeBrowseName = "DateTime";
    public const string severityBrowseName = "Severity";
    public const string fromSeverityBrowseName = "FromSeverity";
    public const string toSeverityBrowseName = "ToSeverity";
    public const string defaultEditModelBrowseName = "CustomFilters";
    public const string filtersConfigurationBrowseName = "FiltersConfiguration";
}

