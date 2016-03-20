using Newtonsoft.Json.Linq;
using System;

namespace cs_jet
{
    internal class JetFetcher
    {
        private Action<JToken> fetchCallback;

        internal JetFetcher(Action<JToken> callback)
        {
            fetchCallback = callback;
        }

        internal void callFetchCallback(JToken parameters)
        {
            if (fetchCallback != null)
            {
                fetchCallback(parameters);
            }
        }
    }
}
