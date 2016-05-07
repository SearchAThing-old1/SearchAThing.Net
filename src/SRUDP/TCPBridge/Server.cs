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

    public class Server
    {

        TcpListener tcpListener;
        Thread serverTh;
        List<Thread> clients;

        void ServerThFn()
        {
            while (true)
            {
                var client = tcpListener.AcceptTcpClient();

                var thClient = new Thread(ClientThFn);
                thClient.Start(client);
            }
        }

        static string CurrentVersion
        {
            get
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;

                return $"{v.Major}.{v.Minor}.{v.Build}";
            }
        }

        void ClientThFn(object _tcpClient)
        {
            var tcpClient = (TcpClient)_tcpClient;
            var buf = new byte[SRUDPRxBufferSize];

            while (true)
            {
                //#if !DEBUG
                try
                //#endif
                {
                    var ns = tcpClient.GetStream();
                    var br = new BinaryReader(ns);
                    var sr = new StreamReader(ns);
                    var sw = new StreamWriter(ns);                    

                    SRUDP.Client srudp = null;

                    while (tcpClient.Connected)
                    {
                        //#if !DEBUG
                        try
                        //#endif
                        {
                            var cmd = sr.ReadLine();

                            if (cmd == null) { tcpClient.Close(); break; }
                            if (string.IsNullOrEmpty(cmd)) continue;

                            Action<string> WriteError = (errCode) =>
                            {
                                sw.WriteLine($"ERR {errCode}");
                                sw.Flush();
                            };

                            Action SyntaxError = () =>
                            {
                                WriteError("BadSyntax (? or help to show syntax)");
                            };                            

                            //---------------------------------------------------

                            #region Connect

                            // https://searchathing.com/?p=829#conn

                            if (cmd.StartsWith("conn "))
                            {
                                if (srudp != null)
                                {
                                    WriteError("AlreadyConnected");
                                }
                                else
                                {
                                    var remoteIp = "";
                                    var remotePort = 0;

                                    var r = cmd.StripBegin("conn ");
                                    var ss = r.Split(' ');
                                    if (ss.Length != 2) SyntaxError();
                                    else
                                    {
                                        var ips = ss[0].Split('.');
                                        if (ips.Length != 4 ||
                                            ips.Any(s => s.Any(c => !char.IsDigit(c))) ||
                                            ss[1].Any(c => !char.IsDigit(c)))
                                            SyntaxError();
                                        else
                                        {
                                            remoteIp = ss[0];
                                            remotePort = int.Parse(ss[1]);

                                            srudp = new SRUDP.Client(new IPEndPoint(IPAddress.Parse(remoteIp), remotePort));
                                            if (srudp.Connect() == SRUDP.TransactionResultEnum.Successful)
                                            {
                                                sw.WriteLine("OK"); sw.Flush();
                                            }
                                            else
                                            {
                                                WriteError("TransactionFailed");
                                                srudp = null;
                                            }
                                        }
                                    }
                                }
                            }

                            #endregion

                            //---------------------------------------------------

                            #region Write ASCII Line

                            // https://searchathing.com/?p=829#waln

                            else if (cmd.StartsWith("waln "))
                            {
                                if (srudp == null || srudp.State != SRUDP.ClientStateEnum.Connected)
                                    WriteError("NotConnected");
                                else
                                {
                                    if (srudp.Write(Encoding.ASCII.GetBytes(cmd.StripBegin("waln "))) == SRUDP.TransactionResultEnum.Successful)
                                    {
                                        sw.WriteLine("OK"); sw.Flush();
                                    }
                                    else
                                        WriteError("TransactionFailed");
                                }
                            }

                            #endregion

                            //---------------------------------------------------

                            #region Read bytes

                            // https://searchathing.com/?p=829#raln

                            else if (cmd.Equals("raln"))
                            {
                                if (srudp == null || srudp.State != SRUDP.ClientStateEnum.Connected)
                                    WriteError("NotConnected");
                                else
                                {
                                    string res = null;

                                    if (srudp.ReadLine(out res) == SRUDP.TransactionResultEnum.Successful)
                                    {
                                        sw.WriteLine("OK");
                                        sw.WriteLine(res);
                                        sw.Flush();
                                    }
                                    else
                                        WriteError("TransactionFailed");
                                }
                            }

                            #endregion

                            //---------------------------------------------------

                            #region Read bytes

                            // https://searchathing.com/?p=829#rbin

                            else if (cmd.Equals("rbin"))
                            {
                                if (srudp == null || srudp.State != SRUDP.ClientStateEnum.Connected)
                                    WriteError("NotConnected");
                                else
                                {
                                    byte[] res = null;

                                    if (srudp.Read(out res) == SRUDP.TransactionResultEnum.Successful)
                                    {
                                        sw.WriteLine("OK");
                                        sw.WriteLine(res.Length);
                                        sw.Flush();
                                        sw.BaseStream.Write(res, 0, res.Length);
                                        sw.Flush();
                                        
                                        Console.WriteLine($"rbin flushed {res.Length} bytes");
                                        Console.WriteLine($"send buffer size = {tcpClient.SendBufferSize}");
                                    }
                                    else
                                        WriteError("TransactionFailed");
                                }
                            }

                            #endregion

                            //---------------------------------------------------

                            #region Write Binary

                            // https://searchathing.com/?p=829#wbin

                            else if (cmd.StartsWith("wbin "))
                            {
                                var ss = cmd.Split(' ');
                                if (ss.Length != 2 || ss[1].Any(c => !char.IsDigit(c))) SyntaxError();
                                else
                                {
                                    var binDataLen = int.Parse(ss[1]);

                                    var rbuf = new byte[binDataLen];
                                    var i = 0;
                                    while (true)
                                    {
                                        var cnt = ns.Read(rbuf, i, rbuf.Length - i);
                                        if (cnt == rbuf.Length - i)
                                        {
                                            if (srudp.Write(rbuf) == SRUDP.TransactionResultEnum.Failed)
                                                WriteError("TransactionFailed");
                                            else
                                            {
                                                sw.WriteLine("OK");
                                                sw.Flush();
                                            }
                                            break;
                                        }
                                        else
                                            i += cnt;
                                    }
                                }
                            }

                            #endregion

                            //---------------------------------------------------

                            #region Disconnect

                            // https://searchathing.com/?p=829#disc

                            else if (cmd == "disc")
                            {
                                if (srudp == null)
                                    WriteError("NotConnected");
                                else
                                {
                                    if (srudp.Disconnect() == SRUDP.TransactionResultEnum.Successful)
                                    {
                                        sw.WriteLine("OK"); sw.Flush();
                                        srudp = null;
                                    }
                                    else
                                        WriteError("TransactionFailed");
                                }
                            }

                            #endregion

                            //---------------------------------------------------

                            #region Version

                            // https://searchathing.com/?p=829#vers

                            else if (cmd == "vers")
                            {
                                if (srudp != null) srudp.Disconnect();

                                sw.WriteLine("OK");
                                sw.WriteLine(CurrentVersion);
                                sw.Flush();
                            }

                            #endregion

                            //---------------------------------------------------

                            #region Quit

                            // https://searchathing.com/?p=829#quit

                            else if (cmd == "quit")
                            {
                                if (srudp != null) srudp.Disconnect();

                                sw.WriteLine("OK"); sw.Flush();
                                tcpClient.Close();
                                break;
                            }

                            #endregion

                            //---------------------------------------------------

                            #region Help

                            // https://searchathing.com/?p=829#help

                            else if (cmd == "help" || cmd == "?")
                            {
                                sw.WriteLine($"SRUDP TCP Wrapper v.{CurrentVersion} info available at https://searchathing.com/?p=829");

                                sw.Flush();
                            }

                            #endregion

                            else
                                SyntaxError();
                        }
                        //#if !DEBUG
                        catch (Exception ex)
                        {
                            Console.WriteLine($"ex: {ex.Message}");
                        }
                        //#endif
                    }

                    if (srudp != null) srudp.Disconnect();
                }
                //#if !DEBUG
                catch
                {
                    break;
                }
                //#endif
            }
        }

        public string SrvHost { get; private set; }
        public int SrvPort { get; private set; }

        public int SRUDPRxBufferSize { get; private set; }

        public Server(string srvHost, int srvPort, int srudpRxBufferSize = 600)
        {
            clients = new List<Thread>();

            SrvHost = srvHost;
            SrvPort = srvPort;

            SRUDPRxBufferSize = srudpRxBufferSize;
        }

        public void Start()
        {
            if (tcpListener != null) throw new BridgeAlreadyStarted();

            var bridgeAddress = IPAddress.Parse(SrvHost);
            tcpListener = new TcpListener(bridgeAddress, SrvPort);

            tcpListener.Start();

            serverTh = new Thread(ServerThFn);
            serverTh.Start();
        }

        public void Stop()
        {
            if (tcpListener == null) throw new BridgeNotActive();

            foreach (var client in clients) client.Abort();
            serverTh.Abort();
            tcpListener.Stop();
            tcpListener = null;
        }

    }

}
