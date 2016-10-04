using ConsoleApplication4.DataClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace ConsoleApplication4
{
    class Program
    {

        public static int[] cpuVerlaufarr = new int[100];

        public static Dictionary<string, PerformanceData> perfWatchlist = new Dictionary<string, PerformanceData>();
        public static Dictionary<int, Process> processWatchlist = new Dictionary<int, Process>();

        private static BackgroundWorker bgwCPU;
        private static BackgroundWorker bgwProcesses;
        private static BackgroundWorker bgwCPUDraw;
        private static BackgroundWorker bgwUDPSender;


        public static void Main(string[] args)
        {
            LoadServerConfiguration();
            initializeComponents();
            new Thread(() =>
            {
                MonitorMaster mm = new MonitorMaster();
                mm.StartListening();
            }).Start();
            

            //while (true)
            //{
            //    Thread.Sleep(10);
            //}

            MachineData md = new MachineData();
            string command = string.Empty;

            while (true)
            {
                command = Console.ReadLine();
                if(!HandleConsoleCommand(command))
                {
                    break;
                }
            }
        }

        private static void LoadServerConfiguration()
        {
            //load config file ....


            Server.Config = new Server.Configuration();

            while (Functions.FindIPAdress() == "")
            {
                Thread.Sleep(5000);
            }

            Server.Config.UDPSenderPort = 15000;
            Server.Config.MonitorMasterPort = 16000;
            Server.Config.CommandMasterPort = 17000;
        }

        private static void initializeComponents()
        {
            bgwCPU = new BackgroundWorker();
            bgwCPU.WorkerReportsProgress = true;
            bgwCPU.WorkerSupportsCancellation = true;

            bgwCPU.DoWork += BgwCPU_DoWork;
            bgwCPU.ProgressChanged += BgwCPU_ProgressChanged;
            bgwCPU.RunWorkerCompleted += BgwCPU_RunWorkerCompleted;

            bgwProcesses = new BackgroundWorker();
            bgwProcesses.WorkerReportsProgress = true;
            bgwProcesses.WorkerSupportsCancellation = true;

            bgwProcesses.DoWork += BgwProcesses_DoWork;
            bgwProcesses.ProgressChanged += BgwProcesses_ProgressChanged;
            bgwProcesses.RunWorkerCompleted += BgwProcesses_RunWorkerCompleted;

            bgwCPU.RunWorkerAsync();
            bgwProcesses.RunWorkerAsync();
        }

        private static void BgwProcesses_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            processWatchlist.Clear();
        }

        private static void BgwProcesses_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Process[] processes = (Process[])e.UserState;
            foreach (Process p in processes)
            {
                if (processWatchlist.ContainsKey(p.Id))
                {
                    //update
                    processWatchlist[p.Id] = p;
                }
                else
                {
                    //insert
                    processWatchlist.Add(p.Id, p);
                }
            }

            processes = null;
        }

        private static void BgwProcesses_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker me = (BackgroundWorker)sender;

            while (!e.Cancel)
            {
                Thread.Sleep(1000);
                Process[] processes = Process.GetProcesses();
                me.ReportProgress(1, processes);
                //List<Process> orderedProcessess = processes.OrderByDescending(memory => memory.PrivateMemorySize64).ToList();
            }
        }
        
        private static void BgwCPU_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            perfWatchlist.Clear();
        }

        private static void BgwCPU_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ffff");

            if (perfWatchlist.ContainsKey(timeStamp))
            {
                perfWatchlist[timeStamp] = (PerformanceData)e.UserState; ;
            }
            else
            {
                if (perfWatchlist.Count == 50)
                {
                    Dictionary<string, PerformanceData> tmpCPUWatchlist = new Dictionary<string, PerformanceData>();
                    string firstKey = perfWatchlist.ElementAt(0).Key;
                    foreach (KeyValuePair<string, PerformanceData> keyPair in perfWatchlist)
	                {
                        if (keyPair.Key != firstKey)
                        {
                            tmpCPUWatchlist.Add(keyPair.Key, keyPair.Value);
                        }
	                }
                    perfWatchlist= new Dictionary<string, PerformanceData>();
                    foreach (KeyValuePair<string, PerformanceData> keyPair in tmpCPUWatchlist)
                    {
                        if (keyPair.Key != firstKey)
                        {
                            perfWatchlist.Add(keyPair.Key, keyPair.Value);
                        }
                    }
                }
                perfWatchlist.Add(timeStamp, (PerformanceData)e.UserState);
            }

            timeStamp = null;
        }

        private static void BgwCPU_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker me = (BackgroundWorker)sender;
            PerformanceCounter cpuTotalCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            PerformanceCounter cpuOSCounter = new PerformanceCounter("Processor", "% Privileged Time", "_Total");
            PerformanceCounter memCacheCounter = new PerformanceCounter("Memory", "Cache Bytes" ,null);
            PerformanceCounter memAvailCounter = new PerformanceCounter("Memory", "Available MBytes", null);
            PerformanceCounter diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            PerformanceCounter diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");

            cpuTotalCounter.NextValue();
            cpuOSCounter.NextValue();
            memCacheCounter.NextValue();
            memAvailCounter.NextValue();
            diskReadCounter.NextValue();
            diskWriteCounter.NextValue();
            while (!e.Cancel)
            {
                Thread.Sleep(1000);
                PerformanceData per = new PerformanceData();
                per.cpuTotal = Convert.ToInt32(cpuTotalCounter.NextValue());
                per.cpuOS= Convert.ToInt32(cpuOSCounter.NextValue()); //float
                per.memCached= Convert.ToInt32(Functions.ByteToMegabyte(memCacheCounter.NextValue())); //float
                per.memFree =Convert.ToInt32(memAvailCounter.NextValue()); //float in MB
                per.diskRead= Convert.ToInt32(Functions.ByteToMegabyte(diskReadCounter.NextValue())); //float in Bytes/sec
                per.diskWrite= Convert.ToInt32(Functions.ByteToMegabyte(diskWriteCounter.NextValue())); //float in Bytes/sec
                me.ReportProgress(1,per);
            }
        }

        public static bool HandleConsoleCommand(string command)
        {
            bool result = true;
            Console.WriteLine();
            switch (command)
            {
                case "exit":
                    result = false;
                    break;

                case "prozesse":
                    processes();
                    break;

                case "cpu":
                    cpu();
                    break;

                case "cpustart":
                    bgwCPUDraw = new BackgroundWorker();
                    bgwCPUDraw.WorkerReportsProgress = true;
                    bgwCPUDraw.WorkerSupportsCancellation = true;

                    bgwCPUDraw.DoWork += bgwCPUDraw_DoWork;
                    bgwCPUDraw.ProgressChanged += bgwCPUDraw_ProgressChanged;
                    bgwCPUDraw.RunWorkerCompleted += bgwCPUDraw_RunWorkerCompleted;

                    bgwCPUDraw.RunWorkerAsync();
                    break;
                case "cpustop":
                    bgwCPUDraw.CancelAsync();
                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("");
                    break;
                case "udpstart":
                    bgwUDPSender = new BackgroundWorker();
                    bgwUDPSender.WorkerReportsProgress = true;
                    bgwUDPSender.WorkerSupportsCancellation = true;

                    bgwUDPSender.DoWork += bgwUDPSender_DoWork;
                    bgwUDPSender.ProgressChanged += bgwUDPSender_ProgressChanged;
                    bgwUDPSender.RunWorkerCompleted += bgwUDPSender_RunWorkerCompleted;

                    bgwUDPSender.RunWorkerAsync();
                    break;
                case "udpstop":
                    bgwUDPSender.CancelAsync();
                    break;
                default:
                    Console.WriteLine("Funktion \"{0}\" existiert nicht!", command);
                    break;

            }

            return result;
        }

        private static void bgwUDPSender_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
        }

        private static void bgwUDPSender_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
        }

        private static void bgwUDPSender_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker me = (BackgroundWorker)sender;
            UDPSender us = new UDPSender();
            while (!e.Cancel)
            {
                e.Cancel = me.CancellationPending;
                Thread.Sleep(7000);
                us.Send();
            }
        }

        private static void bgwCPUDraw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
        }

        private static void bgwCPUDraw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private static void bgwCPUDraw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker me = (BackgroundWorker)sender;
            int progress = 1;
            
            int cursorLeft = Console.CursorLeft;
            int cursorTop = Console.CursorTop;
            byte[] cpuData = new byte[50];
            int count = 0;
            while (!e.Cancel)
            {
                count = 0;
                e.Cancel = me.CancellationPending;
                Thread.Sleep(1000);
                
                //Buffer.BlockCopy(cpuVerlaufarr, 0, result, 0, result.Length);
                Dictionary<string, Socket> tmpSocketPool = new Dictionary<string, Socket>();
                tmpSocketPool = MonitorMaster.SocketPool.ToDictionary(entry => entry.Key, entry => entry.Value);

                Dictionary<string, PerformanceData> tmpCPUWatchlist = new Dictionary<string, PerformanceData>();
                tmpCPUWatchlist = perfWatchlist.ToDictionary(entry => entry.Key, entry => entry.Value);
                
                foreach (KeyValuePair<string, PerformanceData> keyPair in tmpCPUWatchlist)
                {
                    cpuData[count] = Convert.ToByte(keyPair.Value.cpuTotal);
                    count++;
                }

                foreach (KeyValuePair<string,Socket> keyPair in tmpSocketPool)
                {   
                    MonitorMaster.Send(keyPair.Value, cpuData);
                }

                continue;
                bool[,] data = new bool[50, 10];
                //Dictionary<string, PerformanceData> tmpCPUWatchlist = new Dictionary<string, PerformanceData>();
                //tmpCPUWatchlist = perfWatchlist.ToDictionary(entry => entry.Key, entry => entry.Value);
                //tmpCPUWatchlist sollte man noch ordern
                int counter = 49;
                int percentage=0;
                int drawPercentage=0;
                cursorLeft = Console.CursorLeft;
                cursorTop = Console.CursorTop;
                foreach (KeyValuePair<string, PerformanceData> keyPair in tmpCPUWatchlist)
                {
                    percentage = keyPair.Value.cpuTotal;
                    if (percentage <= 100 && percentage >= 90)
                    {
                        drawPercentage = 9;
                    }
                    if (percentage < 90 && percentage >= 80)
                    {
                        drawPercentage = 8;
                    }
                    if (percentage < 80 && percentage >= 70)
                    {
                        drawPercentage = 7;
                    }
                    if (percentage < 70 && percentage >= 60)
                    {
                        drawPercentage = 6;
                    }
                    if (percentage < 60 && percentage >= 50)
                    {
                        drawPercentage = 5;
                    }
                    if (percentage < 50 && percentage >= 40)
                    {
                        drawPercentage = 4;
                    }
                    if (percentage < 40 && percentage >= 30)
                    {
                        drawPercentage = 3;
                    }
                    if (percentage < 30 && percentage >= 20)
                    {
                        drawPercentage = 2;
                    }
                    if (percentage < 20 && percentage >= 10)
                    {
                        drawPercentage = 1;
                    }
                    if (percentage < 10 && percentage >= 0)
                    {
                        drawPercentage = 0;
                    }
                    data[counter, drawPercentage] = true;
                    counter--;
                }
                //if (counter > 0)
                //{
                //    for (int i = 0; i <= counter; i++)
                //    {
                //        for (int j = 0; j < 10; j++)
                //        {
                //            data[i, j] = false;
                //        }
                //    }   
                //}
                tmpCPUWatchlist = null;

                if (progress == 2)
                {
                    Console.CursorLeft = 0;
                    Console.CursorTop = cursorTop - 10;
                }
                else
                {
                    Console.WriteLine("");
                }

                //DO SOMETHING!!

                for (int i = 9; i >= 0; i--)
                {
                    if (i < 9)
                    {
                        Console.Write(" ");
                        Console.Write((i + 1) * 10);
                    }
                    else
                    {
                        Console.Write((i + 1) * 10);
                    }
                    
                    for (int j = 0; j < data.GetLength(0); j++)
                    {
                        if (data[j, i])
                        {
                            Console.Write(".");
                        }
                        else
                        {
                            Console.Write(" ");
                        }
                    }
                    Console.WriteLine(" ");
                }

                if (progress == 2)
                {
                    Console.CursorLeft = cursorLeft;
                    Console.CursorTop = cursorTop;
                }
                me.ReportProgress(progress,data);
                progress = 2;
            }
        }

        private static void drawVerlauf()
        {
            string[,] a = new string[10, 50];

            //double-for loop
            //..
            

            Console.WriteLine("__________");
            for (int i = 0; i < 10; i++)
            {
                String line = "";
                for (int j = 9; j >= 0; j--)
                {
                    if (cpuVerlaufarr[j] == 10 - i)
                    {
                        line = String.Concat(line, ".");
                    }
                    else
                    {
                        line = String.Concat(line, " ");
                    }
                }
                Console.WriteLine(line);
            }
            Console.WriteLine("__________");
        }

        private static void cpuVerlauf()
        {
            PerformanceCounter perfCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            for (int i = 0; i < 10; i++)
            {
                perfCounter.NextValue();
                Thread.Sleep(200);
                Program.cpuVerlaufarr[i] = Convert.ToInt32(Math.Round(perfCounter.NextValue(),1));
            }
        }

        private static void cpu()
        {
            PerformanceCounter perfCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            perfCounter.NextValue();
            Thread.Sleep(1000);
            double usage = Math.Round(perfCounter.NextValue(), 2);
            Console.WriteLine("Derzeitige CPU-Auslastung (letzte 1000ms) {0} %", usage);
        }

        private static void processes()
        {
            Process[] processes = Process.GetProcesses();

            List<Process> orderedProcessess = processes.OrderByDescending(memory => memory.PrivateMemorySize64).ToList();

            foreach (Process p in orderedProcessess)
            {
                long memory = (p.PrivateMemorySize64 / 1024);
                Console.WriteLine("{0,-40} {1,10} Kb", p.ProcessName, memory);
            }
        }
    }
}