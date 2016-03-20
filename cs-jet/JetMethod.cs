using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace cs_jet
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
