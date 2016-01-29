using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenCL.Net;

// Big thanks to Yevano, this is based on his Java miner. I just simply translated it to C# so I can build on it.
// Version 0.11 (1.28.16) (OpenCL GPU stuff)
// WARNING: This version is approximately only 20% the speed of Yevano's miner.
// Update Speed is every 5 seconds, to prevent network spam.
// By Tokuruu

namespace CSharp_Krist_Miner
{
    class Program
    {
        public static string Address;
        public static int Threads;
        public static string Prefix;

        public static Context _context;
        public static Device _device;

        public static int BlocksDone = 0;
        public static int Hashes = 0;
        public static int updateMS = 5000;

        public static int Balance = 0;
        public static string CurrentBlock = "a";
        public static int CurrentWork = 0;

        public static Dictionary<int, Miner> Miners = new Dictionary<int, Miner>();
        public static Dictionary<int, Thread> _Threads = new Dictionary<int, Thread>();

        static void Main(string[] args)
        {
            if (args.Count() != 3)
            {
                Console.WriteLine("Arguments: <address> <threads> [prefix]");
                Console.ReadLine();
                throw (new Exception("Invalid parameters"));
            }
            try {
                Address = args[0];
                Threads = int.Parse(args[1]);
                Prefix = args[2];
            } catch (Exception ex)
            {
                Console.WriteLine("Arguments: <address> <threads> [prefix]");
                Console.ReadLine();
                throw (new Exception("Invalid parameters"));
            }
            int NWork = Helper.getWork();
            CurrentWork = NWork;

            Console.WriteLine("Setting up GPU...");
            Setup();

            Thread DisplayThread = new Thread(Display);
            DisplayThread.Start();

            string Block = "";
            string oldBlock = "";
            int work = 0;
            int balance = 0;
            try
            {
                Block = Helper.getLastBlock();
                oldBlock = Block;
                CurrentBlock = Block;

                Console.WriteLine("Beginning on block: " + Block);
                work = Helper.getWork();
                balance = Helper.getBalance(Address);
                for (int v=0; v < Threads; v++)
                {
                    Console.WriteLine("Adding miner: " + v);
                    Miners.Add(v, new Miner(v.ToString()));
                    _Threads.Add(v, new Thread(Miners[v].Run));
                    _Threads[v].Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Program: " + ex.ToString());
                Console.ReadLine();
            }
        }

        private static void CheckErr(ErrorCode err, string name)
        {
            if (err != ErrorCode.Success)
            {
                Console.WriteLine("ERROR: " + name + " (" + err.ToString() + ")");
            }
        }

        private static void Setup ()
        {
            ErrorCode error;
            Platform[] platforms = Cl.GetPlatformIDs(out error);
            List<Device> deviceList = new List<Device>();

            CheckErr(error, "Cl.GetPlatformIDs");
            foreach (Platform platform in platforms)
            {
                string platformName = Cl.GetPlatformInfo(platform, PlatformInfo.Name, out error).ToString();
                Console.WriteLine("Platform: " + platformName);
                CheckErr(error, "Cl.GetPlatformInfo");
                //We will be looking only for GPU devices
                foreach (Device device in Cl.GetDeviceIDs(platform, DeviceType.Gpu, out error))
                {
                    CheckErr(error, "Cl.GetDeviceIDs");
                    Console.WriteLine("Device: " + device.ToString());
                    deviceList.Add(device);
                }
            }

            if (deviceList.Count <= 0)
            {
                Console.WriteLine("No devices found.");
                return;
            }

            _device = deviceList[0];

            if (Cl.GetDeviceInfo(_device, DeviceInfo.ImageSupport,
                      out error).CastTo<OpenCL.Net.Bool>() == OpenCL.Net.Bool.False)
            {
                Console.WriteLine("No image support.");
                return;
            }
            _context
         = Cl.CreateContext(null, 1, new[] { _device }, null,
        IntPtr.Zero, out error);    //Second parameter is amount of devices
            CheckErr(error, "Cl.CreateContext");
        }

        static void Display ()
        {
            while (true)
            {
                Thread.Sleep(updateMS);
                Console.WriteLine(CurrentBlock + " " + CurrentWork.ToString() + " " + Balance.ToString() + "KST @ " + (Math.Floor((decimal) Hashes/1000)/5000).ToString() + "MH/s Blocks Done: " + BlocksDone.ToString());
                Hashes = 0;
                int NWork = Helper.getWork();
                if (CurrentWork != NWork)
                {
                    Console.WriteLine("New Work!");
                    CurrentBlock = Helper.getLastBlock();
                    CurrentWork = NWork;
                    Balance = Helper.getBalance(Address);
                }
            }
        }
    }
}
