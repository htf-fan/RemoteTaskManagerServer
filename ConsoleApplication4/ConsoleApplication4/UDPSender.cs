using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication4
{
    class UDPSender
    {
        private UdpClient client = new UdpClient();
        IPEndPoint ip = new IPEndPoint(IPAddress.Parse("10.1.255.255"), Server.Config.UDPSenderPort);
        private byte[] message = Encoding.ASCII.GetBytes(Server.Configuration.MagicPacket);

        public void Send()
        {
            Console.WriteLine("SENDING UDP MESSAGE");
            client.Send(message, message.Length, ip);
            //client.Close();
        }
    }
}
