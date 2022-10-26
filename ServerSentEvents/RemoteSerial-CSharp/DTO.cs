using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPAD.neXt.DTO
{
    /*
    * 
    * Routes etc just here for easier reference. Can be grabbed actively from spad http://localhost:28002/ui/DeviceManagePush?tab=code 
    * or SPAD.neXt.DTO Assembly (without routes) when done
    */

   
    public enum ChannelType
    {
        Unknown = 0,
        Text = 1,
        Device = 2,
        ManagedDevice = 3,
        CustomDevice = 4,
    }

    public enum EventPriority
    {
        First = 0,
        High = 1000,
        Low = 10000,
        Last = 100000,
        All = 999999,
    }

    public enum EventSeverity
    {
        None = 0,
        Verbose = 1,
        Normal = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }

    [Route("/serialapi/{Session}/event")]
    public class SerialEvent : IReturnVoid
    {
        public string Session { get; set; }
        public string Message { get; set; }
    }
    [Route("/serialapi/{Session}/connect")]
    public class SerialConnect : IReturnVoid
    {
        public string Session { get; set; }
        public string DeviceID { get; set; }
        public int InstanceID { get; set; }
    }

    public class ChannelJoin
    {
        public Guid ChannelId { get; set; } = Guid.Empty;
        public string Channel { get; set; }
        public string DisplayName { get; set; }
        public ChannelType ChannelType { get; set; } = ChannelType.Unknown;
    }

    

   
    public class NetworkEvent
    {
        public bool Handled { get; private set; } = false;

        public string EventKey { get; set; } = String.Empty;

        public string EventName { get; set; } = String.Empty;

        public string EventTrigger { get; set; } = String.Empty;
        public string EventType { get; set; } = String.Empty;

        public EventPriority EventPriority { get; set; } = EventPriority.Low;

        public EventSeverity EventSeverity { get; set; } = EventSeverity.Normal;

        public Dictionary<string, object> EventData { get; set; } = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

    }



    [Route("/challenge")]
    public class Challenge : IReturn<ServiceReply<ChallengeResponse>>
    {
        public string Token { get; set; }
    }

    public class ChallengeResponse
    {
        public string ApiKey { get; set; }
    }

    public class ServiceReply
    {
        public bool Success { get; set; }
    }

    public class ServiceReply<T> : ServiceReply
    {
        public T Result { get; set; }

    }

   
}
