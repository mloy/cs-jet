using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace cs_jet
{
    class SocketPeerIo : PeerIo
    {
        static readonly int receiveBufferSize = 20000;
        private byte[] receiveBuffer = new byte[receiveBufferSize];
        private int currentReadIndex = 0;
        private int currentWriteIndex = 0;

        PeerOperation operation = PeerOperation.READ_LENGTH;

        private IPAddress address;
        private Socket client;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;

        private bool enoughDataInBuffer = true;
        private int messageLength;

        private bool connected = false;

        public void setConnected(bool connected)
        {
            this.connected = connected;
        }
        public bool isConnected()
        {
            return connected;
        }

        public event EventHandler<int> HandleConnect;
        public event EventHandler<string> HandleIncomingMessage;

        public SocketPeerIo(IPAddress address) {
            this.address = address;
        }

        public void connect()
        {
            IPEndPoint remoteEP = new IPEndPoint(address, 11122);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.BeginConnect(remoteEP, new AsyncCallback(this.ConnectCallback), null);
        }

        public void sendMessage(byte[] buffer)
        {
            int length = IPAddress.HostToNetworkOrder(buffer.Length);
            SocketAsyncEventArgs buf = new SocketAsyncEventArgs();
            var list = new List<ArraySegment<byte>>();
            list.Add(new ArraySegment<byte>(BitConverter.GetBytes(length)));
            list.Add(new ArraySegment<byte>(buffer));
            buf.BufferList = list;
            client.SendAsync(buf);
        }

        private int DataInBuffer()
        {
            return currentWriteIndex - currentReadIndex;
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                client.EndConnect(ar);
                stream = new NetworkStream(client);
                writer = new StreamWriter(stream);
                reader = new StreamReader(stream);
                setConnected(true);
                if (HandleConnect != null)
                {
                    HandleConnect(this, 0);
                }
            }
            catch (SocketException e)
            {
                HandleConnect(this, e.ErrorCode);
            }

            client.BeginReceive(receiveBuffer, 0, receiveBufferSize, 0, new AsyncCallback(this.ReceiveLength), null);
        }

        private void ReceiveLength(IAsyncResult ar)
        {
            enoughDataInBuffer = true;
            int bytesRead = client.EndReceive(ar);
            if (bytesRead == 0)
            {
                // close
            }
            else if (bytesRead < 0)
            {
                // error
            }
            else
            {
                currentWriteIndex += bytesRead;
            }

            while (enoughDataInBuffer)
            {
                switch (operation)
                {
                    case PeerOperation.READ_LENGTH:
                        if (DataInBuffer() >= 4)
                        {
                            int length = BitConverter.ToInt32(receiveBuffer, currentReadIndex);
                            currentReadIndex += 4;
                            messageLength = IPAddress.NetworkToHostOrder(length);
                            if (messageLength + 4 > receiveBufferSize)
                            {
                                // handle error: log, close socket etc.
                            }
                            operation = PeerOperation.READ_MESSAGE;
                        }
                        else
                        {
                            enoughDataInBuffer = false;
                        }
                        break;

                    case PeerOperation.READ_MESSAGE:
                        if (DataInBuffer() >= messageLength)
                        {
                            string json = Encoding.UTF8.GetString(receiveBuffer, currentReadIndex, messageLength);
                            currentReadIndex += messageLength;
                            int rest = DataInBuffer();
                            if (rest > 0)
                            {
                                Buffer.BlockCopy(receiveBuffer, currentReadIndex, receiveBuffer, 0, rest);
                                currentWriteIndex = rest;
                            }
                            else
                            {
                                currentWriteIndex = 0;
                            }
                            currentReadIndex = 0;
                            operation = PeerOperation.READ_LENGTH;

                            if (HandleIncomingMessage != null)
                            {
                                HandleIncomingMessage(this, json);
                            }
                        }
                        else
                        {
                            enoughDataInBuffer = false;
                        }
                        break;
                }
            }
            client.BeginReceive(receiveBuffer, currentWriteIndex, receiveBufferSize - DataInBuffer(), 0, new AsyncCallback(ReceiveLength), null);
        }
    }
    enum PeerOperation { READ_LENGTH, READ_MESSAGE};

}
