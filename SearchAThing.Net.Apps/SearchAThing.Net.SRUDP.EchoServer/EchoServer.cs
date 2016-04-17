#region SearchAThing.Net, Copyright(C) 2016 Lorenzo Delana, License under MIT
/*
* The MIT License(MIT)
* Copyright(c) 2016 Lorenzo Delana, https://searchathing.com
*
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files
* (the "Software"), to deal in the Software without restriction,
* including without limitation the rights to use, copy, modify, merge,
* publish, distribute, sublicense, and/or sell copies of the Software,
* and to permit persons to whom the Software is furnished to do so,
* subject to the following conditions:
*
* The above copyright notice and this permission notice shall be
* included in all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

using SearchAThing.Net.SRUDP;
using System;
using System.Net;
using System.Text;
using System.Threading;

namespace SearchAThing.Net.Tests.SRUDP
{
    class Program
    {
        static void Main(string[] args)
        {
            var protocol = new Protocol(500, 3000);

            var srvEndPoint = new IPEndPoint(new IPAddress(new byte[] { 192, 168, 0, 80 }), 50000);

            Listener listener = new Listener(srvEndPoint, protocol);

            listener.ClientConnected += (client) =>
            {
                Console.WriteLine($"client connected: {client.RemoteEndPoint}");

                while (client.State == ClientStateEnum.Connected)
                {
                    byte[] bytes = null;
                    if (client.Read(out bytes) == TransactionResultEnum.Successful)
                    {
                        Console.WriteLine($"Received [{Encoding.ASCII.GetString(bytes)}]");

                        while (client.Write(bytes) != TransactionResultEnum.Successful)
                        {
                            Console.WriteLine("write failed");
                        }
                    }
                }

                Console.WriteLine($"client received a disconnect");
            };

            listener.Start();
        }
    }
}
