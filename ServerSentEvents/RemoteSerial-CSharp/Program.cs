using ServiceStack;
using ServiceStack.Web;
using SPAD.neXt.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSERemoteSerial
{
    internal class Program
    {
       

        public static string DeviceID = "{DD7E3826-E439-4484-B186-A1443F3BC531}";
        public static string AuthorID = "3870169da23f7cc86823120e6063521a";
        public static string Version = "1.0";
        public static int InstanceID = 1;

        public static IServiceClient Client { get; private set; }
        public static string SessionId { get; private set; }
        static void Main(string[] args)
        {
            var baseUri = "http://127.0.0.1:28002/"; // Start SSEProxy first! connect to Proxy. (Once SPAD 0.9.12.97 get's released change the port to 28001)
            var apiKey = String.Empty;

            if (String.IsNullOrEmpty(apiKey))
            {
                // If we do not have an apikey yet we
                // create a temporary client and challenge SPAD to get the apiKey or have user enter it directly
                var authClient = new JsonServiceClient(baseUri);

                // Challenge SPAD. SPAD will display a 4 digit key now
                authClient.Post(new Challenge());

                // Ask user to enter it (Proxy displays it in its window)
                Console.Write("Please enter challengetoken :");
                var challToken = Console.ReadLine();

                var authReply = authClient.Post(new Challenge { Token = challToken });
                if (!authReply.Success)
                {
                    Console.WriteLine("Challenge failed");
                    return;
                }

                // It's safe to store the apikey. It will only change if user actively changes it
                apiKey = authReply.Result.ApiKey;

                authClient = null; // not needed anymore
            }

            // Create Server-Side-Events EventReceiver 
            var client = new ServerEventsClient(baseUri)
            {
                // Add apikey to the stream
                EventStreamRequestFilter = req => req.AddBearerToken(apiKey),

                OnException = (e) => Console.WriteLine("Exception: " + e.ToString()),
                OnConnect = (serverEvent) => Console.WriteLine("Our Session is '" + serverEvent.Id + "'"),

            };

            // Register a named reveiver for SPAD events
            client.RegisterNamedReceiver<SpadNamedReceiver>("spad");


            // Tell our CallBack client about the apiKey
            client.ServiceClient.BearerToken = apiKey;

            // So we can easier use it later on
            Client = client.ServiceClient;
            
            // Connect to SSE to SPAD / Proxy
            client.Connect().Wait();
            Console.WriteLine("Connected");

            SessionId = client.SubscriptionId;

            // Inform SPAD that we are a Serial-Remote-Device
            Client.Post(new SerialConnect { Session = SessionId, DeviceID = DeviceID, InstanceID = InstanceID }); // Sync Call

            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }


        public static void SPAD_SendSerialEvent(string serialEvent)
        {
            Client.Post(new SerialEvent { Session = SessionId, Message = serialEvent }); // This is a SyncCall. Waits for server to ackknowledge
        }
    }

    // The receiver for spad messages

    public class SpadNamedReceiver : ServerEventReceiver
    {
        public void debug(string msg) => Console.WriteLine("DEBUG " + msg);
        public void error(string msg) => Console.WriteLine("ERROR " + msg);

        public void ChannelJoin(ChannelJoin data) => Console.WriteLine("Joined channel '" + data.Channel + "'");
        public void NetworkEvent(NetworkEvent eventData)
        {
            Console.WriteLine($"Networkevent {eventData.EventTrigger} in {Request.Channel}");
        }
        public override void NoSuchMethod(string selector, object message)
        {
            var msg = (ServerEventMessage)message;
            Console.WriteLine("Unknown data in " + selector);
            Console.WriteLine(msg.Json);
        }

        // One or More Serial Message(s) have been received
        public void msg(string incommingMsg)
        {
            var commands = UnescapeAndSplit(incommingMsg);

            foreach (var currentCommand in commands)
            {
                Console.WriteLine("IN: " + String.Join(" , ", currentCommand.Select(x => "'" + x + "'")));

                if (currentCommand[0] == "0") // SPAD Command Channel
                {
                    switch (currentCommand[1])
                    {
                        case "INIT":
                            {
                                Program.SPAD_SendSerialEvent("0,SPAD," + Program.DeviceID + ",Remote Api Demo,2," + Program.Version + ",AUTHOR=" + Program.AuthorID + ",PID=Demo,ALLOWLOCAL=2;");
                                break;
                            }
                        case "CONFIG":
                            {
                                Program.SPAD_SendSerialEvent("0,OPTION,ISGENERIC=1,PAGESUPPORT=0,CMD_COOLDOWN=50,DATA_COOLDOWN=100,VPSUPPORT=0,NOECHO=1;");
                                Program.SPAD_SendSerialEvent("1,ADD,20,DataOut,S32,RW,Data Output;");
                                Program.SPAD_SendSerialEvent("1,SUBSCRIBE,21,SIMCONNECT:AIRSPEED INDICATED,Knots,1.1;");
                                Program.SPAD_SendSerialEvent("0,OUTPUT,1,L_ONE,LED,SPAD_LED,UI_FACE=3,IMG_ON=_PanelImages//LED_green.png,IMG_OFF=_PanelImages//LED_off.png,COL_0=Green,COL_1=Red,BEHAVIOR=ONOFF;");
                                Program.SPAD_SendSerialEvent("0,INPUT,2000,E_TUNER,ENCODER,SPAD_DOUBLE_ENCODER;");
                                Program.SPAD_SendSerialEvent("0,INPUT,5,I_APPR,PUSHBUTTON,SPAD_SIMPLEBUTTON,ROUTETO=E_TUNER,HIDDEN=1,_ROUTETO.PRESS=PUSH;");
                                Program.SPAD_SendSerialEvent("0,CONFIG;");
                                break;
                            }
                        case "SCANSTATE": Program.SPAD_SendSerialEvent("0,SCANSTATE;"); break;
                        default: break;
                    }
                }
            }

        }

        private List<List<string>> UnescapeAndSplit(string msg)
        {
            var result = new List<List<string>>();
            StringBuilder sb = new StringBuilder(1024);
            var current = new List<string>();
            for (int i = 0; i < msg.Length; i++)
            {
                var c = msg[i];
                if (c == '/')
                {
                    i++;
                    sb.Append(msg[i]);
                    continue;
                }
                if (c == ',') // next Arg
                {
                    current.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }
                if (c == ';') // next command
                {
                    current.Add(sb.ToString());
                    sb.Clear();
                    result.Add(current);
                    current = new List<string>();
                    continue;
                }
                sb.Append(c);
            }

            return result;
        }
    }

}
