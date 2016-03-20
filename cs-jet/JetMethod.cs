// <copyright file="JetMethod.cs" company="Hottinger Baldwin Messtechnik GmbH">
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

namespace Hbm.Devices.Jet
{
    internal class JetMethod
    {
        internal static readonly string INFO = "info";
        internal static readonly string FETCH = "fetch";
        internal static readonly string UNFETCH = "unfetch";

        private JObject data;
        private Action<JToken> responseCallback;

        internal JetMethod(string method, JObject parameters, int requestId, Action<JToken> responseCallback)
        {
            this.responseCallback = responseCallback;
            JObject json = new JObject();
            json["jsonrpc"] = "2.0";
            json["method"] = method;
            json["id"] = requestId;
            if (parameters != null)
            {
                json["params"] = parameters;
            }
            data = json;
        }

        internal void callResponseCallback(JToken response)
        {
            if (responseCallback != null)
            {
                responseCallback(response);
            }
        }

        internal string getJson()
        {
            return JsonConvert.SerializeObject(data);
        }
    }
}
