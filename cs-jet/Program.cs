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

            Matcher matcher = new Matcher();
            matcher.endsWith = "state";
            matcher.caseInsensitive = true;
            FetchId fetchId = peer.fetch(matcher, FetchCallback, ResponceCallback);
            Thread.Sleep(5000);
            peer.unfetch(fetchId, ResponceCallback);
            Thread.Sleep(5000);
            peer.info(ResponceCallback);

            Console.ReadKey();
        }

        public static void HandleConnect(object obj, int args)
        {
            waitHandle.Set();
        }

        public static void FetchCallback(JToken fetchEvent)
        {
            Console.WriteLine("fetch fired");
            Console.WriteLine(fetchEvent);
        }

        public static void ResponceCallback(JToken response)
        {
            Console.WriteLine("Got response!");
            Console.WriteLine(response);
        }
    }
}
