using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using SPAD.neXt.Interfaces;
using SPAD.neXt.Interfaces.Configuration;
using SPAD.neXt.Interfaces.Events;
using SPAD.neXt.Interfaces.Scripting;
using SPAD.neXt.Interfaces.Scripting.Stubs;
public class UDPSender : ScriptStub, IScriptAction2
{
    protected override string ScriptDataPrefix => "SCRIPT_SPDUDP";

    public int NumberOfParameters => 1;

    private UdpClient udpClient = null;
    private string lastError = string.Empty; // To prevent too many error warnings popping up
    protected override void InitializeScript()
    {
        SetScriptDataValue("REMOTEIP", "127.0.0.1"); // Creates LOCAL:SCRIPT_SPDUDP_REMOTEIP
        SetScriptDataValue("REMOTEPORT", 15090);// Creates LOCAL:SCRIPT_SPDUDP_REMOTEPORT
        SetScriptDataValue("DEBUG", 0);// Creates LOCAL:SCRIPT_SPDUDP_DEBUG (Set to 1 to log outgoing data)
    }

    protected override void DeinitializeScript()
    {
        udpClient?.Close();
        udpClient = null;
    }

    public void Execute(IApplication app, List<IEventActionParameter> actionParameters)
    {
        try
        {
            var dbg = GetScriptDataValue("DEBUG", 0) == 1;
            if (udpClient == null)
            {
                var remoteIp = GetScriptDataValue("REMOTEIP", "127.0.0.1");
                var remotePort = GetScriptDataValue("REMOTEPORT", 15090);

                udpClient = new UdpClient(remoteIp, remotePort);
                if (dbg) ScriptLogger.Info($"Endpoint: {remoteIp}:{remotePort}");
            }

            var buffer = new List<byte>();
            if (actionParameters != null && actionParameters.Count == 1)
            {
                // Parse the parameters
                // Expected format: 0xval1,0xval2,0xval3 ... 0xd1,0xd2,0x00,0x32
                var firstParameter = actionParameters.First().GetParameterValue("");
                var values = firstParameter.Split(',');
                var warnings = new StringBuilder();
                if (dbg) ScriptLogger.Info($"Execute '{firstParameter}'");
                for (int i = 0; i < values.Length; i++)
                {
                    var value = values[i];
                    byte b = 0;
                    if (Byte.TryParse(value.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
                        buffer.Add(b);
                    else
                    {
                        warnings.AppendLine($"Value {i + 1} '{value}' could not be parsed as hexadecimal. Format: '0xNN' where NN is a hexadecimal representation");
                    }
                }
                if (warnings.Length > 0)
                {
                    ScriptLogger.WarnWithNotification(warnings.ToString());
                }
                if (buffer.Count > 0)
                {
                    if (dbg) ScriptLogger.Info($"Sending {buffer.ToArray().HexDump()}");
                    udpClient.Send(buffer.ToArray(), buffer.Count);
                }
            }
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

    // we only use the new flow
    public void Execute(IApplication app, ISPADEventArgs eventArgs)
    {
        throw new NotImplementedException();
    }
}
