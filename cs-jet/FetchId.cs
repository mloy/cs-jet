using System.Threading;

namespace cs_jet
{
    public class FetchId
    {
        private int id;

        internal FetchId(int id)
        {
            this.id = id;
        }

        internal int getId()
        {
            return id;
        }
    }
}
