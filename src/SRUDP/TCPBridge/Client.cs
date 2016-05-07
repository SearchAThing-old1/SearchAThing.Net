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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SearchAThing.Core;
using System.Reflection;

namespace SearchAThing.Net.SRUDP.TCPBridge
{

    public class Client
    {

        TcpClient client;        
        StreamWriter sw;
        StreamReader sr;

        void CheckBridgeExitcode()
        {
            var s = sr.ReadLine();

            if (s == "OK")
            {
                return;
            }
            else if (s.StartsWith("ERR"))
            {
                var msg = s.StripBegin("ERR").Trim();
                switch (msg)
                {
                    case "AlreadyConnected": throw new SRUDPAlreadyConnected();
                    case "TransactionFailed": throw new SRUDPTransactionFailed();
                    default: throw new SRUDPUnspecifiedError(msg);
                }
            }
            else
                throw new SRUDPProtocolMalformed();
        }

        public string SrvHost { get; private set; }
        public int SrvPort { get; private set; }

        public Client(string srvHost, int srvPort)
        {
            SrvHost = srvHost;
            SrvPort = srvPort;

            client = new TcpClient();
            client.Connect(srvHost, srvPort);

            var ns = client.GetStream();
            sw = new StreamWriter(ns);
            sr = new StreamReader(ns);
        }

        ~Client()
        {
            client.Close();
        }

        /// <summary>
        /// Connects to a remote SRUDP server.
        /// https://searchathing.com/?page_id=829#conn
        /// </summary>        
        public void Connect(string srudpIp, int srudpPort)
        {
            sw.WriteLine($"conn {srudpIp} {srudpPort}");
            sw.Flush();

            CheckBridgeExitcode();
        }

        /// <summary>
        /// Write a text line to the connected SRUDP server.
        /// https://searchathing.com/?page_id=829#waln
        /// </summary>        
        public void WriteLine(string line)
        {
            sw.WriteLine($"waln {line}");
            sw.Flush();

            CheckBridgeExitcode();
        }

        /// <summary>
        /// Write bytes data.
        /// Note: data array must not exceed max capacity of the SRUDP server
        /// packet device (generally about 400-500 bytes).
        /// https://searchathing.com/?page_id=829#wbin
        /// </summary>        
        public void WriteBytes(byte[] data)
        {
            sw.WriteLine($"wbin {data.Length}");    
            sw.Flush();       
            sw.BaseStream.Write(data, 0, data.Length);
            sw.Flush();

            CheckBridgeExitcode();
        }

        /// <summary>
        /// Read a line of data from the SRUDP server endpoint.
        /// https://searchathing.com/?page_id=829#raln
        /// </summary>
        /// <returns></returns>
        public string ReadLine()
        {
            sw.WriteLine($"raln");
            sw.Flush();

            CheckBridgeExitcode();

            return sr.ReadLine();
        }

        /// <summary>
        /// Read expected data from the SRUDP server endpoint.        
        /// https://searchathing.com/?page_id=829#rbin
        /// </summary>        
        public byte[] ReadBytes()
        {
            sw.WriteLine($"rbin");
            sw.Flush();

            CheckBridgeExitcode();

            var s = sr.ReadLine();
            var len = int.Parse(s);
            var res = new byte[len];

            int i = 0;
            while (i != len)
            {
                var cnt = sr.BaseStream.Read(res, i, len - i);                
                i += cnt;
            }

            return res;
        }

        /// <summary>
        /// Returns the current SRUDP TCP Bridge protocol version.
        /// https://searchathing.com/?page_id=829#vers
        /// </summary>        
        public string Version()
        {
            sw.WriteLine($"vers");
            sw.Flush();

            CheckBridgeExitcode();

            return sr.ReadLine();
        }

        /// <summary>
        /// Disconnect from the SRUDP server endpoint.
        /// https://searchathing.com/?page_id=829#disc
        /// </summary>
        public void Disconect()
        {
            sw.WriteLine($"disc");
            sw.Flush();

            CheckBridgeExitcode();
        }

    }

}
