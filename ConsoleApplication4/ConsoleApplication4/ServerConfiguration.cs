using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication4
{
    public static class Server
    {
        public static Configuration Config = null;

        public class Configuration
        {
            public int MonitorMasterPort { get; set; }
            public int CommandMasterPort { get; set; }
            public int UDPSenderPort { get; set; }
            public const string MagicPacket = "DEINE_MAMA";
            public IPAddress Address { get; set; }
        }
    }
}
