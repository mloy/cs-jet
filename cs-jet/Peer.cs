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
            if (!io.isConnected())
            {
                // TODO: throw better excewption
                throw new Exception();
            }

            JObject infoJson = new JObject();
            infoJson["jsonrpc"] = "2.0";
            infoJson["id"] = Interlocked.Increment(ref requestID);
            infoJson["method"] = "info";
            string json = JsonConvert.SerializeObject(infoJson);
            io.sendMessage(Encoding.UTF8.GetBytes(json));
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
