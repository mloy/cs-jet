// <copyright file="Peer.cs" company="Hottinger Baldwin Messtechnik GmbH">
//
// CS Jet, a library to communicate with Jet IPC.
//
// The MIT License (MIT)
//
// Copyright (C) Hottinger Baldwin Messtechnik GmbH
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Hbm.Devices.Jet
{
    public class Peer
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

        public FetchId fetch(Matcher matcher, Action<JToken> fetchCallback, Action<JToken> responseCallback)
        {
            int fetchId = Interlocked.Increment(ref fetchIdCounter);
            JetFetcher fetcher = new JetFetcher(fetchCallback);
            registerFetcher(fetchId, fetcher);

            JObject parameters = new JObject();
            parameters["path"] = fillPath(matcher);
            parameters["caseInsensitive"] = matcher.caseInsensitive;
            parameters["id"] = fetchId;
            int requestId = Interlocked.Increment(ref requestIdCounter);
            JetMethod fetch = new JetMethod(JetMethod.FETCH, parameters, requestId, responseCallback);
            executeMethod(fetch, requestId);
            return new FetchId(fetchId);
        }

        public void set(string path, JToken value, Action<JToken> responseCallback)
        {
            JObject parameters = new JObject();
            parameters["path"] = path;
            parameters["value"] = value;
            int requestId = Interlocked.Increment(ref requestIdCounter);
            JetMethod set = new JetMethod(JetMethod.SET, parameters, requestId, responseCallback);
            executeMethod(set, requestId);
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
            // TODO: handle batch responses
            JToken json = JToken.Parse(arg);
            if (json == null)
            {
                return;
            }

            if (json.Type == JTokenType.Object)
            {
                handleMessage((JObject)json);
                return;
            }

            if (json.Type == JTokenType.Array)
            {
                foreach (var item in json.Children())
                {
                    if (item.Type == JTokenType.Object)
                    {
                        handleMessage((JObject)item);
                    }
                }
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

        void handleMessage(JObject json)
        {
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

        private JObject fillPath(Matcher matcher)
        {
            JObject path = new JObject();
            if (!String.IsNullOrEmpty(matcher.contains))
            {
                path["contains"] = matcher.contains;
            }
            if (!String.IsNullOrEmpty(matcher.startsWith))
            {
                path["startsWith"] = matcher.startsWith;
            }
            if (!String.IsNullOrEmpty(matcher.endsWith))
            {
                path["endsWith"] = matcher.endsWith;
            }
            if (!String.IsNullOrEmpty(matcher.equals))
            {
                path["equals"] = matcher.equals;
            }
            if (!String.IsNullOrEmpty(matcher.equalsNot))
            {
                path["equalsNot"] = matcher.equalsNot;
            }
            return path;
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
