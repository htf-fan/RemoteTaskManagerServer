using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication4
{
    class Functions
    {
        //test
        public static int ByteToMegabyte(double bytes)
        {
            int result = 0;

            result = Convert.ToInt32(bytes / Math.Pow(1024, 2));
            return result;
        }

        public static string FindIPAdress()
        {
            NetworkInterface[] ni = NetworkInterface.GetAllNetworkInterfaces();
            List<string> ips = new List<string>();
            foreach (NetworkInterface netif in ni)
            {
                if (netif.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                    && netif.OperationalStatus == OperationalStatus.Up)
                {
                    //Console.WriteLine("Network Interface: {0}", netif.Name);
                    IPInterfaceProperties properties = netif.GetIPProperties();
                    //foreach (IPAddress dns in properties.DnsAddresses)
                    //    Console.WriteLine("\tDNS: {0}", dns);
                    //foreach (IPAddressInformation anycast in properties.AnycastAddresses)
                    //    Console.WriteLine("\tAnyCast: {0}", anycast.Address);
                    //foreach (IPAddressInformation multicast in properties.MulticastAddresses)
                    //    Console.WriteLine("\tMultiCast: {0}", multicast.Address);
                    foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                    {
                        if (unicast.Address.ToString().StartsWith("10."))
                        {
                            ips.Add(unicast.Address.ToString());
                        }
                    }

                }
            }

            if (ips.Count > 0)
            {
                Server.Config.Address = System.Net.IPAddress.Parse(ips[0]);
                return ips[0];
            }

            return "";

        }
    }
}
