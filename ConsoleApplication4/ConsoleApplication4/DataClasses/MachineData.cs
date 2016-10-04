using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication4.DataClasses
{
    public class MachineData
    {
        private string _MachineName;
        public string MachineName
        {
            get { return this._MachineName; }
        }

        private int _MachineMemory;
        public int MachineMemory
        {
            get { return this._MachineMemory; }
        }

        private string _MachineIP;

        public string MachineIP
        {
            get { return this._MachineIP; }
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            internal uint dwLength;
            internal uint dwMemoryLoad;
            internal ulong ullTotalPhys;
            internal ulong ullAvailPhys;
            internal ulong ullTotalPageFile;
            internal ulong ullAvailPageFile;
            internal ulong ullTotalVirtual;
            internal ulong ullAvailVirtual;
            internal ulong ullAvailExtendedVirtual;
        }

        //[return: MarshalAs(UnmanagedType.Bool)]
        //[DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        //internal static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public MachineData()
        {
            MEMORYSTATUSEX statEX = new MEMORYSTATUSEX();
            statEX.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            GlobalMemoryStatusEx(ref statEX);

            this._MachineMemory = Functions.ByteToMegabyte(statEX.ullTotalPhys);
            this._MachineName = Environment.MachineName;
            this._MachineIP = Server.Config.Address.ToString();
        }
    }
}
