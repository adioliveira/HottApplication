#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.EventLogger;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.Retentivity;
using FTOptix.Core;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.Recipe;
using System.Collections.Generic;
using System.Linq;
using FTOptix.WebUI;
using FTOptix.System;
using FTOptix.OPCUAServer;
using System.Threading;
using FTOptix.Modbus;
using FTOptix.DataLogger;
using FTOptix.MQTTBroker;
using FTOptix.OPCUAClient;
using FTOptix.S7TCP;
using FTOptix.Report;
using FTOptix.ODBCStore;
using FTOptix.Alarm;
#endregion

public class DB_Confirm : BaseNetLogic
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
    public void GetRowCount()
    {
        /****************************************************************************************/
        /*Declare and initialize variables.                                                     */
        /****************************************************************************************/
        
        Thread.Sleep(250); //Wait for the data log to enter the new data.

        var nodePtr = LogicObject.GetVariable("DataStore");
        var nodeId = (NodeId)nodePtr.Value;
        var myDB = (Store)InformationModel.Get(nodeId);

        var myTable = myDB.Tables.Get<Table>("DB_Confirm");

        object[,] resultSet;
        string[] header;

        myDB.Query("SELECT * FROM Log01_Data", out header, out resultSet);
        LogicObject.GetVariable("RowCount").Value = resultSet != null ? resultSet.GetLength(0) : 0;

    }

}
