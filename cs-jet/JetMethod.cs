using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace cs_jet
{
    internal class JetMethod
    {
        internal static readonly string INFO = "info";

        private JObject data;
        private Action<JObject> responseCallback;

        internal JetMethod(string method, JObject parameters, int requestId, Action<JObject> responseCallback)
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

        internal void callResponseCallback(JObject response)
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
