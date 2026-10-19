#region Using directives
using System;
using CoreBase = FTOptix.CoreBase;
using FTOptix.HMIProject;
using UAManagedCore;
using FTOptix.UI;
using FTOptix.NetLogic;
using FTOptix.OPCUAClient;
using FTOptix.MQTTClient;
using FTOptix.OPCUAServer;
using FTOptix.Alarm;
using FTOptix.SerialPort;
using FTOptix.SQLiteStore;
#endregion

public class ClockLogic : BaseNetLogic
{
	public override void Start()
	{
		periodicTask = new PeriodicTask(UpdateTime, 200, LogicObject);
		periodicTask.Start();
	}

	public override void Stop()
	{
		periodicTask.Dispose();
		periodicTask = null;
	}

	private void UpdateTime()
	{
		DateTime localTime = DateTime.Now;
		DateTime utcTime = DateTime.UtcNow;
		LogicObject.GetVariable("Time").Value = localTime;
		LogicObject.GetVariable("Time/Year").Value = localTime.Year;
		LogicObject.GetVariable("Time/Month").Value = localTime.Month;
		LogicObject.GetVariable("Time/Day").Value = localTime.Day;
		LogicObject.GetVariable("Time/Hour").Value = localTime.Hour;
		LogicObject.GetVariable("Time/Minute").Value = localTime.Minute;
		LogicObject.GetVariable("Time/Second").Value = localTime.Second;
		LogicObject.GetVariable("UTCTime").Value = utcTime;
		LogicObject.GetVariable("UTCTime/Year").Value = utcTime.Year;
		LogicObject.GetVariable("UTCTime/Month").Value = utcTime.Month;
		LogicObject.GetVariable("UTCTime/Day").Value = utcTime.Day;
		LogicObject.GetVariable("UTCTime/Hour").Value = utcTime.Hour;
		LogicObject.GetVariable("UTCTime/Minute").Value = utcTime.Minute;
		LogicObject.GetVariable("UTCTime/Second").Value = utcTime.Second;
	}

	private PeriodicTask periodicTask;
}
