using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SearchAThing.Net.SRUDP.TCPBridge.ServerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                PrintHelp();
                return;
            }

            var srvAddr = args[0];
            var srvPort = int.Parse(args[1]);
            int? srudpRxBufferSize = null;
            if (args.Length == 3) srudpRxBufferSize = int.Parse(args[2]);

            Server srv = null;
            if (srudpRxBufferSize.HasValue)
                srv = new Server(srvAddr, srvPort, srudpRxBufferSize.Value);
            else
                srv = new Server(srvAddr, srvPort);

            srv.Start();
        }

        static void PrintHelp()
        {
            Console.WriteLine("Syntax:");
            Console.WriteLine($"\t{Assembly.GetExecutingAssembly().GetName().Name} <srv-ip> <srv-port> [srudp-rx-buffer-size]");
        }
    }
}
