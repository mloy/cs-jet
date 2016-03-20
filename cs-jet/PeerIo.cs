using System;

namespace cs_jet
{
    interface PeerIo
    {
        void connect();
        bool isConnected();
        void sendMessage(byte[] buffer);
        event EventHandler<string> HandleIncomingMessage;
        event EventHandler<int> HandleConnect;
    }
}
