using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading;

namespace cs_jet
{
    class Peer
    {
        private PeerIo io;
        private int requestID = 0;

        public event EventHandler<int> HandlePeerConnect;

        public Peer(PeerIo io)
        {
            this.io = io;
            io.HandleIncomingMessage += HandleIncomingMessage;
        }

        public void connect()
        {
            io.HandleConnect += HandleConnect;
            io.connect();
        }

        public void info(Action<JObject> responseCallback)
        {
            int id = Interlocked.Increment(ref requestID);
            JetMethod info = new JetMethod(JetMethod.INFO, null, id);
            io.sendMessage(Encoding.UTF8.GetBytes(info.getJson()));
        }

        public static void HandleIncomingMessage(object obj, string arg)
        {
            JObject json = JObject.Parse(arg);
            Console.WriteLine(json);
        }

        public void HandleConnect(object obj, int arg)
        {
            if (HandlePeerConnect != null)
            {
                HandlePeerConnect(this, arg);
            }
        }
    }
}
