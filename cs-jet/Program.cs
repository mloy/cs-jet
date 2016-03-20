using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Threading;

namespace cs_jet
{
    class Program
    {
        private static readonly EventWaitHandle waitHandle = new AutoResetEvent(false);

        static void Main(string[] args)
        {
            IPAddress[] ips;
            ips = Dns.GetHostAddresses("172.19.1.1");
            SocketPeerIo io = new SocketPeerIo(ips[0]);

            Peer peer = new Peer(io);
            peer.HandlePeerConnect += HandleConnect;
            peer.connect();
            waitHandle.WaitOne();
            waitHandle.Reset();

            peer.info(ResponceCallback);
            Thread.Sleep(10000);
            peer.info(ResponceCallback);

            Console.ReadKey();
        }

        public static void HandleConnect(object obj, int args)
        {
            waitHandle.Set();
        }

        public static void ResponceCallback(JObject response)
        {
            Console.WriteLine(response);
        }
    }
}
