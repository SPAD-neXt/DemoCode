using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using SPAD.neXt.Interfaces;
using SPAD.neXt.Interfaces.Configuration;
using SPAD.neXt.Interfaces.Events;
using SPAD.neXt.Interfaces.Scripting;
using SPAD.neXt.Interfaces.Scripting.Stubs;

public class FileWriter : ScriptStub, IScriptAction2
{
    protected override string ScriptDataPrefix => "SCRIPT_SPDFILEWRITER";

    public int NumberOfParameters => 2;

    private string lastError = string.Empty; // To prevent too many error warnings popping up
    protected override void InitializeScript()
    {

    }

    protected override void DeinitializeScript()
    {
        var airraftName = EventSystem.GetExistingDataDefinition("LOCAL:SPAD_AIRCRAFT")?.GetValueAs<string>();
    }

    public void Execute(IApplication app, List<IEventActionParameter> actionParameters)
    {

        if (actionParameters != null && actionParameters.Count == 2)
        {
            var fileName = actionParameters[0].GetParameterValue("filewriter.txt");
            var data = actionParameters[1].GetParameterValue("No Data received");
            try
            {
                File.WriteAllText(fileName, data);
                // Change above line to 
                // File.AppendAllText(fileName, data);
                // to keep existing data instead of overwriting
            }
            catch (Exception ex)
            {
                // log to logfile
                ScriptLogger.Warn($"Execute failed: {ex}");
                // show ui warning if message is diffrent
                ScriptLogger.WarnWithNotificationIf(ex.Message != lastError, "Execute failed: " + ex.Message);
                lastError = ex.Message;
            }
        }
    }

    // we only use the new flow
    public void Execute(IApplication app, ISPADEventArgs eventArgs)
    {
        throw new NotImplementedException();
    }
}
