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
using System.Linq;
using System.Text;

namespace SearchAThing.Net.SRUDP
{

    public enum PacketType
    {
        Connect = (1 << 1),
        Ack = (1 << 2),
        Data = (1 << 3),
        Disconnect = (1 << 5)
    };

    /// <summary>
    /// https://searchathing.com/index.php/2016/02/16/simple-reliable-udp/
    /// </summary>
    public class Packet
    {
        byte opCode;

        const int DisconnectBitPos = 3;
        const int DataBitPos = 2;
        const int AckBitPos = 1;
        const int ConnectBitPos = 0;

        public PacketType Type { get { return ParseType(opCode); } }
        public UInt16 ID { get; private set; }
        public UInt16 DataLen { get; private set; }
        public UInt16 DataLenLeft { get; private set; }

        public byte[] Msg { get; private set; }

        /// <summary>
        /// Gets the SRUDP byte representation.
        /// </summary>        
        public byte[] ToBytes()
        {
            var bytes = new byte[7 + (Msg != null ? Msg.Length : 0)];

            bytes[0] = opCode;

            bytes[1] = (byte)(ID >> 8);
            bytes[2] = (byte)(ID & 0xff);

            bytes[3] = (byte)(DataLen >> 8);
            bytes[4] = (byte)(DataLen & 0xff);

            bytes[5] = (byte)(DataLenLeft >> 8);
            bytes[6] = (byte)(DataLenLeft & 0xff);

            if (Msg?.Length > 0) Msg.CopyTo(bytes, 7);

            return bytes;
        }

        /// <summary>
        /// Construct an SRUDP packet from given protocol specification data
        /// </summary>        
        public Packet(PacketType type, UInt16 id, UInt16 dataLen, UInt16 dataLenLeft, byte[] msg = null)
        {
            opCode = 0;
            ID = id;
            DataLen = dataLen;
            DataLenLeft = dataLenLeft;

            switch (type)
            {
                case PacketType.Connect:
                    opCode |= (1 << ConnectBitPos);
                    if (msg != null || id != 0) throw new ArgumentException("invalid packet on Connect");
                    break;

                case PacketType.Disconnect:
                    opCode |= (1 << DisconnectBitPos);
                    if (msg != null) throw new ArgumentException("invalid packet on Disconnect");
                    break;

                case PacketType.Ack:
                    opCode |= (1 << AckBitPos);
                    if (msg != null) throw new ArgumentException("invalid packet on Ack");
                    break;

                case PacketType.Data:
                    opCode |= (1 << DataBitPos);
                    if (msg == null) throw new ArgumentException("invalid packet on Data");
                    break;
            }

            Msg = msg;
        }

        internal static PacketType ParseType(byte opCode)
        {
            PacketType type;
            if ((opCode & (1 << ConnectBitPos)) != 0) type = PacketType.Connect;
            else if ((opCode & (1 << AckBitPos)) != 0) type = PacketType.Ack;
            else if ((opCode & (1 << DataBitPos)) != 0) type = PacketType.Data;
            else if ((opCode & (1 << DisconnectBitPos)) != 0) type = PacketType.Disconnect;
            else throw new ArgumentException("invalid packet opCode");

            return type;
        }

        /// <summary>
        /// Construct an SRUDP object from a UDP datagram bytes.
        /// </summary>        
        public static Packet Parse(byte[] receivedUDPDatagram)
        {
            if (receivedUDPDatagram.Length < 1) throw new ArgumentException("invalid SRUDP length");

            var opCode = receivedUDPDatagram[0];
            var id = (UInt16)((UInt16)receivedUDPDatagram[1] << 8 | receivedUDPDatagram[2]);
            var dataLen = (UInt16)((UInt16)receivedUDPDatagram[3] << 8 | receivedUDPDatagram[4]);
            var dataLenLeft = (UInt16)((UInt16)receivedUDPDatagram[5] << 8 | receivedUDPDatagram[6]);
            PacketType type = ParseType(opCode);

            byte[] bytes = (type == PacketType.Data) ? receivedUDPDatagram.Skip(7).ToArray() : null;

            return new Packet(type, id, dataLen, dataLenLeft, bytes);
        }

        public override string ToString()
        {
            var msgAscii = Msg != null ? Encoding.ASCII.GetString(Msg) : "";

            return $"PKT [ Type:{Type} ID:{ID} DataLen:{DataLen} DataLenLeft:{DataLenLeft} ] -> Msg.len={Msg?.Length} [{msgAscii}]";
        }

    }

}
