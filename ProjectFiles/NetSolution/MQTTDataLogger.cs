#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.DataLogger;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.WebUI;
using FTOptix.NativeUI;
using FTOptix.Store;
using FTOptix.InfluxDBStoreRemote;
using FTOptix.OPCUAClient;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.InfluxDBStore;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.MQTTClient;
using System.Collections.Generic;
using System.Text.Json;
using FTOptix.OPCUAServer;
using FTOptix.Alarm;
using FTOptix.SerialPort;
using FTOptix.SQLiteStore;
using FTOptix.MQTTBroker;
#endregion

public class MQTTDataLogger : BaseNetLogic
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
    public void InsertPayloadIntoTable(string payload, NodeId tableNodeId)
    {

        var table = InformationModel.Get<Table>(tableNodeId);
        if(table == null) return;

        var data = JsonSerializer.Deserialize<Rootobject>(payload);

        var columnNamesList = new List<string>();
        object[,] values = new object[1, data.Records.Length + 2];

        for(int i = 0; i < data.Records.Length; i++)
        {
            columnNamesList.Add(data.Records[i].TagName);
            values[0, i] = ToClrObject(data.Records[i].Value);
        }

        var utc = DateTime.SpecifyKind(data.Timestamp, DateTimeKind.Utc);

        columnNamesList.Add("LocalTimestamp");
        values[0, data.Records.Length] = utc.ToLocalTime(); ;

        columnNamesList.Add("Timestamp");
        values[0, data.Records.Length + 1] = utc;

        var columnNames = columnNamesList.ToArray();

        var store = (Store)table.Owner.Owner;
        store.Insert(table.BrowseName, columnNames, values);
    }


    static object? ToClrObject(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.Number => el.TryGetInt64(out var l) ? (object)l :
                                   el.TryGetDouble(out var d) ? d : null,

            JsonValueKind.String => el.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => el.ToString()
        };
    }


}


public class Rootobject
{
    public Record[] Records { get; set; } = Array.Empty<Record>();
    public DateTime Timestamp { get; set; }
}

public class Record
{
    public string TagName { get; set; } = string.Empty;
    public JsonElement Value { get; set; }   // <-- klucz: nie object
}
