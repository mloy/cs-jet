// <copyright file="SocketPeerIo.cs" company="Hottinger Baldwin Messtechnik GmbH">
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Hbm.Devices.Jet
{
    public class SocketPeerIo : PeerIo
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
            if (!isConnected())
            {
                // TODO: throw better excewption
                throw new Exception();
            }

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
