#region Using directives
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.Core;
using System;
using System.Linq;
using UAManagedCore;
using FTOptix.Alarm;
using FTOptix.SerialPort;
using FTOptix.SQLiteStore;
using FTOptix.MQTTBroker;
#endregion

public class AlarmWidgetsPaginationLogic : BaseNetLogic
{
    private enum Move { First, Previous, Next, Last };

    public override void Start()
    {
        affinityId = LogicObject.Context.AssignAffinityId();

        currentDisplayedAlarm = LogicObject.GetVariable("CurrentDisplayedAlarm");
        currentDisplayedAlarm.Value = NodeId.Empty;

        currentDisplayedAlarmIndex = LogicObject.GetVariable("CurrentDisplayedAlarmIndex");
        currentDisplayedAlarmIndex.Value = 0;

        RegisterObserverOnLocalizedAlarmsContainer();
        RegisterObserverOnSessionActualLanguageChange(LogicObject.Context);
        RegisterObserverOnLocalizedAlarmsObject();

        alarmsCount = LogicObject.GetVariable("AlarmCount");
        alarmsCount.Value = localizedAlarmsContainer?.Children.Count ?? 0;

        var alarms = localizedAlarmsContainer?.Children.ToList();
        var element = alarms?.FirstOrDefault(x => x.NodeId == dataGrid.SelectedItem);
        var index = alarms?.IndexOf(element) ?? -1;
        LockAndMoveAlarm(index);
    }

    public override void Stop()
    {
        alarmEventRegistration?.Dispose();
        alarmEventRegistration2?.Dispose();
        sessionActualLanguageRegistration?.Dispose();

        alarmEventRegistration = null;
        alarmEventRegistration2 = null;
        sessionActualLanguageRegistration = null;
    }

    #region Exported user methods
    [ExportMethod]
    public void NextAlarm()
    {
        LockAndMoveAlarm(Move.Next);
        dataGrid.SelectedItem = currentDisplayedAlarm.Value;
    }

    [ExportMethod]
    public void PreviousAlarm()
    {
        LockAndMoveAlarm(Move.Previous);
        dataGrid.SelectedItem = currentDisplayedAlarm.Value;
    }

    [ExportMethod]
    public void FirstAlarm()
    {
        LockAndMoveAlarm(Move.First);
        dataGrid.SelectedItem = currentDisplayedAlarm.Value;
    }

    [ExportMethod]
    public void LastAlarm()
    {
        LockAndMoveAlarm(Move.Last);
        dataGrid.SelectedItem = currentDisplayedAlarm.Value;
    }
    #endregion

    #region Alarms specific events
    private void OnAlarmAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
    {
        alarmsCount.Value = localizedAlarmsContainer?.Children.Count ?? 0;
    }

    private void OnAlarmRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
    {
        alarmsCount.Value = localizedAlarmsContainer?.Children.Count ?? 0;
        if ((int)alarmsCount.Value == 0 || targetNode.NodeId == (NodeId)currentDisplayedAlarm.Value)
            LockAndMoveAlarm(Move.Next);
    }
    #endregion

    #region AlarmPagination iterates alarms list
    private void MoveAlarm(Move moveDirection)
    {
        switch (moveDirection)
        {
            case Move.Next:
                IncrementAlarmIndex();
                break;
            case Move.Previous:
                DecrementAlarmIndex();
                break;
            case Move.First:
                alarmIndex = 0;
                break;
            case Move.Last:
                alarmIndex = (localizedAlarmsContainer?.Children.Count ?? 0) - 1;
                break;
            default:
                break;
        }

        GoToCurrentAlarm();
    }

    private void MoveAlarm(int index)
    {
        alarmIndex = index;
        GoToCurrentAlarm();
    }

    private void IncrementAlarmIndex()
    {
        alarmIndex++;
        int count = localizedAlarmsContainer?.Children.Count ?? 0;
        if (count == 0)
            alarmIndex = -1;
        else if (alarmIndex >= count)
            alarmIndex = 0; // ensure endless loop over alarms
    }

    private void DecrementAlarmIndex()
    {
        alarmIndex--;
        int count = localizedAlarmsContainer?.Children.Count ?? 0;
        if (count == 0)
        {
            alarmIndex = -1;
            return;
        }
        if (alarmIndex < 0)
            alarmIndex = count > 0 ? count - 1 : 0;
    }

    private void GoToCurrentAlarm()
    {
        IUANode alarm = localizedAlarmsContainer?.Children.ElementAtOrDefault<IUANode>(alarmIndex);
        if (alarmIndex > 0 && alarm == null)
        {
            alarmIndex = 0; // reset in case moving to currrent alarm is no longer possible
            alarm = localizedAlarmsContainer?.Children.ElementAtOrDefault<IUANode>(alarmIndex);
        }
        if (alarm != null)
            currentDisplayedAlarm.Value = alarm.NodeId;
        else
            currentDisplayedAlarm.Value = NodeId.Empty;
        currentDisplayedAlarmIndex.Value = alarmIndex;
    }
    #endregion

    #region Alarm observers
    private void RegisterObserverOnLocalizedAlarmsObject()
    {
        var alarmsObjectObserver = new AlarmsObjectObserver((ctx) => RegisterObserverOnLocalizedAlarmsContainer());

        alarmEventRegistration2 = Model.RegisterEventObserver(
            alarmsObjectObserver, EventType.ForwardReferenceAdded, affinityId);
    }

    private void RegisterObserverOnLocalizedAlarmsContainer()
    {
        localizedAlarmsContainer = Model;

        if (alarmEventRegistration != null)
        {
            alarmEventRegistration.Dispose();
            alarmEventRegistration = null;
        }

        var alarmsAddRemoveObserver = new AlarmsObserver(this);
        alarmEventRegistration = localizedAlarmsContainer?.RegisterEventObserver(
            alarmsAddRemoveObserver,
            EventType.ForwardReferenceAdded |
            EventType.ForwardReferenceRemoved,
            affinityId);
    }

    private void RegisterObserverOnSessionActualLanguageChange(IContext context)
    {
        var currentSessionActualLanguage = context.Sessions.CurrentSessionInfo.SessionObject.Children["ActualLanguage"];

        var sessionActualLanguageChangeObserver = new CallbackVariableChangeObserver(
            (IUAVariable variable, UAValue newValue, UAValue oldValue, ElementAccess elementAccess, ulong senderId) =>
            {
                RegisterObserverOnLocalizedAlarmsContainer();
            });

        sessionActualLanguageRegistration = currentSessionActualLanguage.RegisterEventObserver(
            sessionActualLanguageChangeObserver, EventType.VariableValueChanged, affinityId);
    }

    private sealed class AlarmsObjectObserver : IReferenceObserver
    {
        public AlarmsObjectObserver(Action<IContext> action)
        {
            registrationCallback = action;
        }

        public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            string localeId = targetNode.Context.Sessions.CurrentSessionHandler.ActualLocaleId;
            if (String.IsNullOrEmpty(localeId))
                localeId = "en-US";

            if (targetNode.BrowseName == localeId)
                registrationCallback(targetNode.Context);
        }

        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
        }

        private readonly Action<IContext> registrationCallback;
    }

    private void LockAndMoveAlarm(Move direction)
    {
        lock (_alarmLock)
        {
            MoveAlarm(direction);
        }
    }

    private void LockAndMoveAlarm(int index)
    {
        lock (_alarmLock)
        {
            MoveAlarm(index);
        }
    }

    private sealed class AlarmsObserver : IReferenceObserver
    {
        public AlarmsObserver(AlarmWidgetsPaginationLogic _alarmsObject)
        {
            alarmsObject = _alarmsObject;
        }

        public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            alarmsObject.OnAlarmAdded(sourceNode, targetNode, referenceTypeId, senderId);
        }

        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            alarmsObject.OnAlarmRemoved(sourceNode, targetNode, referenceTypeId, senderId);
        }

        private readonly AlarmWidgetsPaginationLogic alarmsObject;
    }
    #endregion

    private DataGrid dataGrid
    {
        get
        {
            return (DataGrid)LogicObject.GetAlias("AlarmDataGridAlias");
        }
    }

    private IUANode Model
    {
        get
        {
            var modelNodeId = dataGrid.Model;
            var alarmGridModel = InformationModel.Get(modelNodeId);
            return alarmGridModel ?? throw new CoreConfigurationException("ModelAlias node id not found");
        }
    }

    private uint affinityId;
    private IEventRegistration alarmEventRegistration;
    private IEventRegistration alarmEventRegistration2;
    private IEventRegistration sessionActualLanguageRegistration;
    private IUANode localizedAlarmsContainer = null;
    private int alarmIndex = -1;
    private IUAVariable alarmsCount;
    private IUAVariable currentDisplayedAlarm;
    private IUAVariable currentDisplayedAlarmIndex;
    private readonly object _alarmLock = new object();
}
