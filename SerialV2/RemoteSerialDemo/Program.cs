using CommandMessenger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace RemoteDemo
{
    public delegate void EventHandler<TSenderArg, TEventArgs>(TSenderArg sender, TEventArgs e);

    /*
     * 
     * Connection to SPAD.neXt using Serial V2.1 Protocol (CmdMessenger)
     * Show Case: Using WebSocket instead of Serial Communication
     * 
     * SPAD.neXt >= 0.9.12.2 required (Interface not exposed else)
     */
    internal class Program
    {
        static Guid myDeviceId = new Guid("{DD7E3826-E439-4484-B186-A1443F3BC521}"); // Change This!
        static string myAuthorid = "229057729962967050"; // Change This!
        static Version myVersion = new Version(1, 1); // Device Version. This is used to decide if a cached layout is loaded or not

        static Dictionary<int, Action<WsClient, ReceivedCommand>> commandHandler = new Dictionary<int, Action<WsClient, ReceivedCommand>>();
        static long isStarted = 0;
        static void Main(string[] args)
        {
            Console.WriteLine("Connecting ...");


            commandHandler[0] = OnSpadCommandReceived;
            // commandHandler[1] = OnSpadCommandReceived; Not needed. Spad never sends on channel 1
            commandHandler[2] = OnSpadEventReceived;

            commandHandler[6] = OnSpadLedEventReceived;
            commandHandler[7] = OnSpadDisplayEventReceived;

            var demoSocket = new WsClient();
            demoSocket.TextMessageReceived += MsgReceived;
            demoSocket.BinaryMessageReceived += (sender, inStream) => { Console.WriteLine($"Received Binary {inStream.Length}"); inStream.Dispose(); };
            demoSocket.ConnectAsync("ws://127.0.0.1:28001/serialapi/" + myDeviceId).Wait();

            SpinWait.SpinUntil(() => Interlocked.Read(ref isStarted) == 1);
            Console.WriteLine("\r\n\r\nPress Key to Send a device input event");
            Console.WriteLine("1 - OUTER TUNER COUNTERCLOCKWISE");
            Console.WriteLine("2 - INNER TUNER COUNTERCLOCKWISE");
            Console.WriteLine("3 - INNER TUNER CLOCKWISE");
            Console.WriteLine("4 - OUTER TUNER CLOCKWISE");
            Console.WriteLine("5 - Activate Button");
            Console.WriteLine("0 - EXIT");
            bool exitLoop = false;
            do
            {
                var c = Console.ReadKey();
                switch (c.KeyChar)
                {
                    case '1': demoSocket.SendMessage(new SendCommand(8, 2000, -100).ToString()); break; // 2000 Our Encoder ID , -100 = Outer Counter Clockwise
                    case '2': demoSocket.SendMessage(new SendCommand(8, 2000, -1).ToString()); break; // 2000 Our Encoder ID , -1 = Inner Counter Clockwise
                    case '3': demoSocket.SendMessage(new SendCommand(8, 2000, 1).ToString()); break;// 2000 Our Encoder ID , 1 = Inner Clockwise
                    case '4': demoSocket.SendMessage(new SendCommand(8, 2000, 100).ToString()); break;// 2000 Our Encoder ID , 100 = Outer Clockwise
                    case '5':
                        demoSocket.SendMessage(new SendCommand(8, 0, 1).ToString()); // 0 = Our Button ID , 1 = PRESS
                        demoSocket.SendMessage(new SendCommand(8, 0, 0).ToString()); // 0 = Our Button ID , 0 = RELEASE
                        break;
                    case '0': exitLoop = true; break;

                }
            }
            while (!exitLoop);


            demoSocket.DisconnectAsync().Wait();
        }

        private static void OnSpadLedEventReceived(WsClient sender, ReceivedCommand cmd)
        {
            var ledIndex = cmd.ReadInt32Arg();
            var ledVal = cmd.ReadInt32Arg(); // 0 = Off, 1 = On
            var ledTag = cmd.ReadStringArg();
            Console.WriteLine($"Turn Led {ledIndex} " + (ledVal == 0 ? "OFF" : "ON"));

        }

        private static void OnSpadDisplayEventReceived(WsClient sender, ReceivedCommand cmd)
        {
            var displayIndex = cmd.ReadInt32Arg();
            var rowIndex = cmd.ReadInt32Arg();
            var displayCommand = cmd.ReadInt32Arg(); // 0 = off , 1 = on , 2 = Display Value
            var displayValue = cmd.ReadStringArg();
            if (displayCommand < 2)
                Console.WriteLine($"Turn Display {displayIndex} " + (displayCommand == 0 ? "OFF" : "ON"));
            else
                Console.WriteLine($"Display '{displayValue}' on Display {displayIndex} row {rowIndex}");
        }

        private static void OnSpadCommandReceived(WsClient sender, ReceivedCommand obj)
        {
            var subCmd = obj.ReadStringArg();
            switch (subCmd)
            {
                case "INIT":
                    {
                        var apiVersion = obj.ReadInt32Arg();
                        var spadVersion = obj.ReadStringArg();
                        var spadAuthToken = obj.ReadStringArg();
                        Console.WriteLine($"Connection API v{apiVersion} SPAD.neXt {spadVersion}");
                        sender.SendMessage(new SendCommand(0, "SPAD", myDeviceId, "SPAD Remote Demo", 2, myVersion, "A=" + myAuthorid).ToString());
                        break;
                    }
                
                case "PING":
                    {
                        sender.SendMessage(new SendCommand(0, "PONG", obj.ReadStringArg()).ToString());
                        break;
                    }
                case "CONFIG":
                    {
                        Console.WriteLine("Sending Configuration");

                        sender.SendMessage(new SendCommand(1, "OPTION", "ISGENERIC=1", "PAGESUPPORT=1", "CMD_COOLDOWN=50", "DATA_COOLDOWN=100").ToString());

                        sender.SendMessage(new SendCommand(1, "INPUT", 0, "BUTTON_1", "PUSHBUTTON", "SPAD_PUSHBUTTON").ToString()); // A push button with PRESS/PRESS SHORT/PRESS LONG/RELEASE Events

                        sender.SendMessage(CreateLcdDisplay(1, "D_COMFRQ", 6, 1).ToString()); // A 6 Column 1 Row Display (Display ID 1)

                        sender.SendMessage(new SendCommand(1, "INPUT", 2000, "E_TUNER", "ENCODER", "SPAD_DOUBLE_ENCODER").ToString()); // Our Encoder (ID 2000) for changing frequency ACCELLERATED!

                        //sender.SendMessage(new SendCommand(1, "OUTPUT", 0, "L_HDG_LOCKED", "LED", "SPAD_LED").ToString()); // A Simple on/off LED
                        //sender.SendMessage(new SendCommand(1, "OUTPUT", 1, "L_GEAR_LEFT", "LED", "SPAD_LED_3COL").ToString()); // A Led with 3 Colors Red/Green/Yellow
                       // sender.SendMessage(new SendCommand(1, "OUTPUT", 2, "L_AP1", "LED", "SPAD_LED", "HIDDEN=1").ToString()); // An Led integraded into a button. Do not expose in UI (see linked button!) 

                        // sender.SendMessage(new SendCommand(1, "INPUT", 1, "BUTTON_2_WITH_LED", "PUSHBUTTON", "SPAD_PUSHBUTTON", "LED=L_AP1").ToString()); // Button with integrated LED (L_AP1)


                        sender.SendMessage("0,CONFIG;"); // We are done with config
                        break;
                    }
                default:
                    break;
            }
        }

        private static SendCommand CreateLcdDisplay(int commandIndex, string tag, int length, int rows, int width = 133, int height = 40, int fontsize = 36, string font = "LCDFONT", string foreground = "#FFFF0017")
        {
            return new SendCommand(1, "OUTPUT", commandIndex, tag, "DISPLAY", "SPAD_DISPLAY", "LENGTH=" + length, "ROWS=" + rows, "WIDTH=" + width,
                "HEIGHT=" + height, "FONTSIZE=" + fontsize, "FONT=" + font, "FOREGROUND=" + foreground);
        }

        private static void OnSpadEventReceived(WsClient sender, ReceivedCommand obj)
        {
            var subCmd = obj.ReadStringArg();
            switch (subCmd)
            {
                case "ERROR":
                    {
                        Console.WriteLine("Error received :" + obj.ReadStringArg());
                        return;
                    }
                case "START":
                    {
                        Console.WriteLine("Ready to rock! Start command received");
                        Interlocked.Increment(ref isStarted);
                        return;
                    }
                case "END":
                    {
                        Console.WriteLine("SPAD.neXt exiting.");
                        return;
                    }
                default:
                    break;
            }
            Console.WriteLine("SPAD Event: " + obj.RawString);
        }

        #region CmdMessenger Stuff
        private static ReceivedCommand ParseMessage(string line)
        {
            // Trim and clean line
            var cleanedLine = line.Trim('\r', '\n');
            cleanedLine = Escaping.Remove(cleanedLine, ';', '/');

            return new ReceivedCommand(Escaping.Split(cleanedLine, ',', '/', StringSplitOptions.RemoveEmptyEntries));
        }

        static string currentBuffer = "";
        static object lockObject = new object();
        private static void MsgReceived(WsClient sender, string e)
        {
            //Console.WriteLine(e);
            lock (lockObject)
            {
                var cmds = new List<ReceivedCommand>();
                currentBuffer += e;

                if (currentBuffer.Length == 0)
                    return;

                int pos = 0;
                while (pos < currentBuffer.Length)
                {
                    var c = currentBuffer[pos];
                    if (c == '/') // Skip Escaped char
                    {
                        pos += 2;
                        continue;
                    }
                    if (c == ';') // EndOfCommand
                    {
                        cmds.Add(ParseMessage(currentBuffer.Substring(0, pos)));
                        pos++;
                        if (pos < currentBuffer.Length)
                        {
                            currentBuffer = currentBuffer.Substring(pos);
                            pos = 0;
                            continue;
                        }
                        else
                        {
                            currentBuffer = String.Empty;
                            break;
                        }
                    }
                    pos++;
                }
                cmds.ForEach(cmd =>
                {
                    if (commandHandler.TryGetValue(cmd.CmdId, out var handler))
                    {
                        handler(sender, cmd);
                    }
                    else
                    {
                        Console.WriteLine($"unknown CmdID {cmd}");
                    }
                });
            }


        }
        #endregion
    }
}

namespace System
{
    public static class SystemExtensions
    {
        public static string ToJson(this object obj, JsonSerializerSettings settings = null)
        {
            if (settings == null)
                return JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.None);
            else
                return JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.None, settings);
        }
    }
}
