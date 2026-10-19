#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.WebUI;
using FTOptix.DataLogger;
using FTOptix.Store;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.System;
using FTOptix.Modbus;
using FTOptix.OPCUAServer;
using FTOptix.MQTTBroker;
using FTOptix.OPCUAClient;
using FTOptix.S7TCP;
using FTOptix.Report;
using FTOptix.NativeUI;
using FTOptix.ODBCStore;
using FTOptix.Alarm;
#endregion

public class Area_Rectangle : BaseNetLogic
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
			     
    public void AreaCalc()
    {
        // Initialize and read inputs
        float AreaValue;
        float LengthValue = LogicObject.GetVariable("Inp_Length").Value;
        float WidthValue = LogicObject.GetVariable("Inp_Width").Value;
                
        AreaValue = LengthValue * WidthValue; 
                
        // Set outputs
        LogicObject.GetVariable("Out_Area").Value = AreaValue;
                
        Log.Info("AreaCalc", "Area calculation complete!");
    }
}
