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
using System.Text;

namespace SearchAThing.Net.SRUDP
{

    public enum ClientStateEnum
    {
        Connecting,
        Connected,
        Disconnecting,
        Disconnected
    };

    public enum TransactionResultEnum
    {
        Failed,
        Successful
    }

    /// <summary>
    /// SRUDP Client
    /// ref. doc https://searchathing.com/?p=59
    /// </summary>
    public class Client : IDisposable
    {
        ushort txId;
        internal ushort rxId;
        internal bool managed;

        byte[] rxDataNotConsumed;

        Queue<Packet> packets;
        AutoResetEvent evtPacketAvailable;

        internal UdpClient udp;

        void ManageAcks(Packet packet, UInt16 id)
        {
            if (packet.Type == PacketType.Disconnect && id == rxId)
            {
                Console.WriteLine("ManageAxks: Disconnect");
                State = ClientStateEnum.Disconnecting;
                Send(PacketType.Ack, id);
                State = ClientStateEnum.Disconnected;
                if (!managed) udp.Close();
            }
            else if (id == rxId - 1)
            {
                Console.WriteLine("ManageAxks: Ack previously received packet");
                Send(PacketType.Ack, id);
            }
            else
                Console.WriteLine($"UnManagedAcks type:{packet.Type} id:{id}");
        }

        int yy = 0;

        internal TransactionResultEnum Send(PacketType opCodeType, UInt16 ackId, byte[] data = null)
        {
            if (data == null || data.Length <= Protocol.MsgSizeMax)
            {
                return SendChunk(opCodeType, ackId, data, (ushort)((data != null) ? data.Length : 0), 0);
            }
            else
            {
                var dataLen = (UInt16)Protocol.MsgSizeMax;
                var dataLenLeft = (UInt16)data.Length;
                var i = 0;

                while (dataLenLeft != 0)
                {
                    dataLenLeft -= dataLen;

                    if (SendChunk(opCodeType, 0, data, dataLen, dataLenLeft, i) == TransactionResultEnum.Failed) return TransactionResultEnum.Failed;
                    i += dataLen;

                    if (dataLenLeft > 0 && dataLenLeft < Protocol.MsgSizeMax) dataLen = dataLenLeft;                    
                }
                return TransactionResultEnum.Successful;
            }
        }

        TransactionResultEnum SendChunk(PacketType opCodeType, UInt16 ackId, byte[] data = null, UInt16 dataLen = 0, UInt16 dataLenLeft = 0, int dataOff = 0)
        {
            if (State == ClientStateEnum.Disconnected) return TransactionResultEnum.Failed;

            var beginTime = DateTime.Now;
            Packet pkt = new Packet(opCodeType,
                (opCodeType == PacketType.Ack) ? ackId : txId,
                dataLen,
                dataLenLeft,
                (data == null) ? null : (data.Skip(dataOff).Take(dataLen).ToArray()));

            var ackReceived = false;
            while (!ackReceived && State != ClientStateEnum.Disconnected && (DateTime.Now - beginTime).TotalMilliseconds <= Protocol.ConnectionTimeoutMs)
            {
                var pktBytes = pkt.ToBytes();
                udp.Send(pktBytes, pktBytes.Length, RemoteEndPoint);

                if (opCodeType == PacketType.Ack)
                {
                    return TransactionResultEnum.Successful;
                }

                var ackBegin = DateTime.Now;

                Func<Packet, bool> ackCheck = (rxPkt) =>
                {
                    var id = rxPkt.ID;

                    if (rxPkt.Type == PacketType.Ack && id == txId)
                    {
                        ackReceived = true;
                        ++txId;

                        return true;
                    }
                    else
                    {
                        ManageAcks(rxPkt, id);
                    }
                    return false;
                };

                while (!ackReceived && State != ClientStateEnum.Disconnected && (DateTime.Now - ackBegin).TotalMilliseconds <= Protocol.AckTimeoutMs)
                {
                    while (managed || (udp.Client != null && udp.Available > 0))
                    {
                        if (managed)
                        {
                            var rxPkt = DequeuePacket();
                            if (rxPkt != null && ackCheck(rxPkt)) return TransactionResultEnum.Successful;
                        }
                        else
                        {
                            var task = udp.ReceiveAsync();
                            if (task.Wait(Protocol.AckTimeoutMs))
                            {
                                if (task.Result.RemoteEndPoint.Equals(RemoteEndPoint))
                                {
                                    var rxPkt = Packet.Parse(task.Result.Buffer);

                                    if (ackCheck(rxPkt)) return TransactionResultEnum.Successful;
                                }
                            }
                        }
                    }
                }
            }

            return TransactionResultEnum.Failed;
        }

        void ForceDisconnect()
        {
            State = ClientStateEnum.Disconnected;
            txId = rxId = 0;
            LocalEndPoint = null;
        }

        internal void PushPacket(Packet packet)
        {
            packets.Enqueue(packet);

            evtPacketAvailable.Set();
        }

        internal void WaitPacketAvail(int milliseconds = -1)
        {
            if (milliseconds == -1)
                evtPacketAvailable.WaitOne();
            else
                evtPacketAvailable.WaitOne(milliseconds);
        }

        internal Packet DequeuePacket()
        {
            Packet packet = null;

            if (evtPacketAvailable.WaitOne(Protocol.ConnectionTimeoutMs))
            {
                packet = packets.Dequeue();
            }

            return packet;
        }

        public ClientStateEnum State { get; internal set; }
        public IPEndPoint LocalEndPoint { get; internal set; }
        public IPEndPoint RemoteEndPoint { get; private set; }
        public Protocol Protocol { get; private set; }

        public Client(IPEndPoint remoteEndPoint, Protocol protocol = null)
        {
            if (protocol == null) protocol = new Protocol();
            Protocol = protocol;
            RemoteEndPoint = remoteEndPoint;

            State = ClientStateEnum.Disconnected;
            rxId = txId = 0;

            packets = new Queue<Packet>();
            evtPacketAvailable = new AutoResetEvent(false);
        }

        public TransactionResultEnum Connect()
        {
            if (managed) return TransactionResultEnum.Failed;

            if (State != ClientStateEnum.Disconnected) return TransactionResultEnum.Failed;

            udp = new UdpClient(0);
            udp.DontFragment = true;

            LocalEndPoint = (IPEndPoint)udp.Client.LocalEndPoint;

            State = ClientStateEnum.Connecting;

            var res = Send(PacketType.Connect, 0);
            if (res == TransactionResultEnum.Successful) State = ClientStateEnum.Connected;

            return res;
        }

        public TransactionResultEnum Write(byte[] data)
        {
            return Send(PacketType.Data, 0, data);
        }

        public TransactionResultEnum ReadLine(out string res)
        {
            // if a line already available from rxDataNotConsumed
            if (rxDataNotConsumed != null && rxDataNotConsumed.Length > 0)
            {
                var s = Encoding.ASCII.GetString(rxDataNotConsumed);
                if (s.Contains("\r\n"))
                {
                    var sb = new StringBuilder();
                    int i = 0;
                    while (i < rxDataNotConsumed.Length)
                    {
                        if (rxDataNotConsumed[i] == '\r' && (i + 1) < rxDataNotConsumed.Length &&
                            rxDataNotConsumed[i + 1] == '\n')
                        {
                            i += 2;
                            break;
                        }
                        else
                            sb.Append((char)rxDataNotConsumed[i++]);
                    }
                    res = sb.ToString();

                    if (i == rxDataNotConsumed.Length)
                        rxDataNotConsumed = null;
                    else
                        rxDataNotConsumed = rxDataNotConsumed.Skip(i).ToArray();

                    return TransactionResultEnum.Successful;
                }
            }

            byte[] r = null;
            var readExitcode = Read(out r);
            if (readExitcode == TransactionResultEnum.Successful)
            {
                // integrate not consumed data
                if (rxDataNotConsumed != null && rxDataNotConsumed.Length > 0)
                {
                    var r2 = new byte[rxDataNotConsumed.Length + r.Length];
                    rxDataNotConsumed.CopyTo(r2, 0);
                    r.CopyTo(r2, rxDataNotConsumed.Length);
                    r = r2;
                    rxDataNotConsumed = null;
                }

                // recurse read until newline
                if (!Encoding.ASCII.GetString(r).Contains("\r\n"))
                {
                    rxDataNotConsumed = r;
                    return ReadLine(out res);
                }

                var sb = new StringBuilder();
                int i = 0;
                while (i < r.Length)
                {
                    if (r[i] == '\r' && (i + 1) < r.Length && r[i + 1] == '\n')
                    {
                        i += 2;
                        break;
                    }
                    else
                        sb.Append((char)r[i++]);
                }
                res = sb.ToString();

                // compose not consumed data if any
                if (i != r.Length)
                {
                    rxDataNotConsumed = r.Skip(i).ToArray();
                }

                return TransactionResultEnum.Successful;
            }
            else
            {
                res = null;
                return readExitcode;
            }
        }

        public TransactionResultEnum Read(out byte[] res)
        {
            res = null;
            UInt16 dataLenReaded = 0;
            var rxBegin = DateTime.Now;

            byte[] rxRes = null;

            Action<Packet> ReadPkt = (rxPkt) =>
            {
                var id = rxPkt.ID;

                if (rxPkt.Type == PacketType.Data && id == rxId)
                {
                    rxBegin = DateTime.Now;

                    var data = rxPkt.Msg;
                    var dataLen = rxPkt.DataLen;
                    var dataLenLeft = rxPkt.DataLenLeft;

                    if (dataLenReaded == 0)
                        rxRes = new byte[dataLen + dataLenLeft];

                    for (int j = 0; j < dataLen; ++j) rxRes[dataLenReaded + j] = data[j];
                    dataLenReaded += dataLen;

                    Send(PacketType.Ack, rxId);
                    ++rxId;
                }
                else
                {
                    Console.WriteLine($"Managing unsequenced packet during Read");
                    ManageAcks(rxPkt, id);
                }
            };

            while (State == ClientStateEnum.Connected)
            {
                if ((DateTime.Now - rxBegin).TotalMilliseconds > Protocol.ConnectionTimeoutMs)
                {
                    Disconnect();
                    return TransactionResultEnum.Failed;
                }

                if (managed)
                {
                    var rxPkt = DequeuePacket();
                    if (rxPkt != null) ReadPkt(rxPkt);
                }
                else
                {
                    IPEndPoint remoteEndPoint = null;
                    var rxUdp = udp.Receive(ref remoteEndPoint);
                    if (remoteEndPoint.Equals(RemoteEndPoint))
                    {
                        var rxPkt = Packet.Parse(rxUdp);
                        ReadPkt(rxPkt);
                    }
                }

                if (rxRes != null && dataLenReaded == rxRes.Length)
                {
                    // manages previous not consumed data (caused by the ReadLine)
                    if (rxDataNotConsumed != null && rxDataNotConsumed.Length > 0)
                    {
                        res = new byte[rxDataNotConsumed.Length + rxRes.Length];
                        rxDataNotConsumed.CopyTo(res, 0);
                        rxRes.CopyTo(res, rxDataNotConsumed.Length);
                        rxDataNotConsumed = null;
                    }
                    else
                        res = rxRes;
                    return TransactionResultEnum.Successful;
                }
            }

            return TransactionResultEnum.Failed;
        }

        public TransactionResultEnum Disconnect()
        {
            State = ClientStateEnum.Disconnecting;
            var res = Send(PacketType.Disconnect, 0);
            ForceDisconnect();

            return res;
        }

        public void Dispose()
        {
            //      if (State == ClientStateEnum.Connected) Disconnect();
        }

    }

}
