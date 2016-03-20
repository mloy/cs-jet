using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace cs_jet
{
    class Peer
    {
        private PeerIo io;
        private int requestID;
        private Dictionary<int, JetMethod> openRequests;

        public event EventHandler<int> HandlePeerConnect;

        public Peer(PeerIo io)
        {
            openRequests = new Dictionary<int, JetMethod>();
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
            JetMethod info = new JetMethod(JetMethod.INFO, null, id, responseCallback);
            executeMethod(info, id);
        }

        public void HandleIncomingMessage(object obj, string arg)
        {
            JObject json = JObject.Parse(arg);
            JToken token = json["id"];
            if (token != null)
            {
                int id = token.ToObject<int>();
                JetMethod method = null;
                lock (openRequests)
                {
                    if (openRequests.ContainsKey(id))
                    {
                        method = openRequests[id];
                        openRequests.Remove(id);
                    }
                }
                if (method != null)
                {
                    method.callResponseCallback(json);
                }
            }
            
            Console.WriteLine(json);
        }

        public void HandleConnect(object obj, int arg)
        {
            if (HandlePeerConnect != null)
            {
                HandlePeerConnect(this, arg);
            }
        }

        private void executeMethod(JetMethod method, int id)
        {
            lock (openRequests)
            {
                openRequests.Add(id, method);
            }
            io.sendMessage(Encoding.UTF8.GetBytes(method.getJson()));
        }
    }
}
