// <copyright file="JetExample.cs" company="Hottinger Baldwin Messtechnik GmbH">
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

using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Threading;

using Hbm.Devices.Jet;

namespace JetExample
{
    class JetExample
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
