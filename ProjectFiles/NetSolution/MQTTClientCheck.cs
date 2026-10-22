#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.DataLogger;
using FTOptix.HMIProject;
using FTOptix.WebUI;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.Store;
using FTOptix.InfluxDBStoreRemote;
using FTOptix.OPCUAClient;
using FTOptix.MQTTClient;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.InfluxDBStore;
using FTOptix.CommunicationDriver;
using FTOptix.SerialPort;
using FTOptix.Core;
using FTOptix.SQLiteStore;
using FTOptix.MQTTBroker;
using FTOptix.OPCUAServer;
#endregion

public class MQTTClientCheck : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
    [ExportMethod]
    public void CheckMQTTClient()
    {
        // Workaround: both applications run locally and must remain running.
        // Disabling the client through Dynamic Security does not trigger its MQTT
        // Last Will (LWT), so status is determined by a 1200 ms payload timeout.

        var lastPayloadTimestamp = (DateTime)LogicObject.GetVariable("PayloadTimeStamp").Value;
        var statusVariable = InformationModel.GetVariable(
            LogicObject.GetVariable("ClientStatus").Value);

        var elapsed = DateTime.Now - lastPayloadTimestamp;
        statusVariable.Value = elapsed.TotalMilliseconds <= 1200;
    }
}
