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
        private int requestIdCounter;
        private int fetchIdCounter;
        private Dictionary<int, JetMethod> openRequests;
        private Dictionary<int, JetFetcher> openFetches;

        public event EventHandler<int> HandlePeerConnect;

        public Peer(PeerIo io)
        {
            openRequests = new Dictionary<int, JetMethod>();
            openFetches = new Dictionary<int, JetFetcher>();
            this.io = io;
            io.HandleIncomingMessage += HandleIncomingMessage;
        }

        public void connect()
        {
            io.HandleConnect += HandleConnect;
            io.connect();
        }

        public void info(Action<JToken> responseCallback)
        {
            int id = Interlocked.Increment(ref requestIdCounter);
            JetMethod info = new JetMethod(JetMethod.INFO, null, id, responseCallback);
            executeMethod(info, id);
        }

        public FetchId fetch(Action<JToken> fetchCallback, Action<JToken> responseCallback)
        {
            int fetchId = Interlocked.Increment(ref fetchIdCounter);
            JetFetcher fetcher = new JetFetcher(fetchCallback);
            registerFetcher(fetchId, fetcher);

            JObject parameters = new JObject();
            parameters["path"] = new JObject();
            parameters["id"] = fetchId;
            int requestId = Interlocked.Increment(ref requestIdCounter);
            JetMethod fetch = new JetMethod(JetMethod.FETCH, parameters, requestId, responseCallback);
            executeMethod(fetch, requestId);
            return new FetchId(fetchId);
        }

        public void unfetch(FetchId fetchId, Action<JToken> responseCallback)
        {
            unregisterFetcher(fetchId.getId());

            JObject parameters = new JObject();
            parameters["id"] = fetchId.getId();
            int requestId = Interlocked.Increment(ref requestIdCounter);
            JetMethod unfetch = new JetMethod(JetMethod.UNFETCH, parameters, requestId, responseCallback);
            executeMethod(unfetch, requestId);
        }

        public void HandleIncomingMessage(object obj, string arg)
        {
            JObject json = JObject.Parse(arg);
            if (json == null)
            {
                return;
            }

            JToken fetchIdToken = getFetchId(json);
            if (fetchIdToken != null)
            {
                handleFetch(fetchIdToken.ToObject<int>(), json);
                return;
            }

            if (isResponse(json))
            {
                handleResponse(json);
                return;
            }
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

        private void registerFetcher(int fetchId, JetFetcher fetcher)
        {
            lock(openFetches)
            {
                openFetches.Add(fetchId, fetcher);
            }
        }

        private void unregisterFetcher(int fetchId)
        {
            lock(openFetches)
            {
                openFetches.Remove(fetchId);
            }
        }

        private JToken getFetchId(JObject json)
        {
            JToken methodToken = json["method"];
            if ((methodToken != null) && (methodToken.Type == JTokenType.Integer))
            {
                return methodToken;
            }
            return null;
        }

        private void handleFetch(int fetchId, JObject json)
        {
            JetFetcher fetcher = null;
            lock (openFetches)
            {
                if (openFetches.ContainsKey(fetchId))
                {
                    fetcher = openFetches[fetchId];
                }
            }
            if (fetcher != null)
            {
                JToken parameters = json["params"];
                if (parameters.Type != JTokenType.Null)
                {
                    fetcher.callFetchCallback(parameters);
                }
            }
        }

        private bool isResponse(JObject json)
        {
            JToken methodToken = json["method"];
            if ((methodToken == null) || (methodToken.ToObject<String>() == null))
            {
                return true;
            }
            return false;
        }

        private void handleResponse(JObject json)
        {
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
        }
    }
}
