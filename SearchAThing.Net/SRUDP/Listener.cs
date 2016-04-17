#region SearchAThing.Net, Copyright(C) 2016 Lorenzo Delana, License under MIT
/*
* The MIT License(MIT)
* Copyright(c) 2016 Lorenzo Delana, https://searchathing.com
*
* Permission is hereby granted, free of charge, to any person obtaining a
* copy of this software and associated documentation files (the "Software"),
* to deal in the Software without restriction, including without limitation
* the rights to use, copy, modify, merge, publish, distribute, sublicense,
* and/or sell copies of the Software, and to permit persons to whom the
* Software is furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
* FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
* DEALINGS IN THE SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;

namespace SearchAThing.Net.SRUDP
{

    public class Listener : IDisposable
    {

        Dictionary<string, Client> remoteEndPointClients;

        public delegate void ClientConnectedDelegate(Client client);

        public event ClientConnectedDelegate ClientConnected;

        public IPEndPoint SrvEndPoint { get; private set; }
        public Protocol Protocol { get; private set; }

        public Listener(IPEndPoint srvEndPoint, Protocol protocol = null)
        {
            if (protocol == null) protocol = new Protocol();
            Protocol = protocol;
            SrvEndPoint = srvEndPoint;
            remoteEndPointClients = new Dictionary<string, Client>();
        }

        public void Start()
        {
            var udp = new UdpClient(SrvEndPoint);
            udp.DontFragment = true;

            while (true)
            {
                IPEndPoint remoteEndPoint = null;
                var bytes = udp.Receive(ref remoteEndPoint);

                Client client = null;
                if (!remoteEndPointClients.TryGetValue(remoteEndPoint.ToString(), out client) &&
                    ClientConnected != null)
                {
                    var pkt = Packet.Parse(bytes);

                    if (pkt.Type == PacketType.Connect && pkt.ID == 0)
                    {
                        client = new Client(remoteEndPoint, Protocol);

                        client.managed = true;
                        client.udp = udp;
                        client.LocalEndPoint = SrvEndPoint;
                        client.State = ClientStateEnum.Connecting;
                        client.Send(PacketType.Ack, client.rxId++);
                        client.State = ClientStateEnum.Connected;

                        remoteEndPointClients.Add(remoteEndPoint.ToString(), client);

                        var thClient = new Thread(() =>
                        {
                            ClientConnected(client);
                            remoteEndPointClients.Remove(client.RemoteEndPoint.ToString());
                        });
                        thClient.Name = "thClient";
                        thClient.Start();
                    }
                }
                else
                {
                    client.PushPacket(Packet.Parse(bytes));
                }
            }
        }

        public void Dispose()
        {

        }
    }

}