using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            peer.info();
            Thread.Sleep(10000);
            peer.info();

            Console.ReadKey();
        }

        public static void HandleConnect(object obj, int args)
        {
            waitHandle.Set();
        }
    }
}
