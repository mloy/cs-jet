using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace cs_jet
{
    internal class JetMethod
    {
        internal static readonly string INFO = "info";

        private JObject data;

        internal JetMethod(string method, JObject parameters, int requestId)
        {
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

        internal string getJson()
        {
            return JsonConvert.SerializeObject(data);
        }
    }
}
