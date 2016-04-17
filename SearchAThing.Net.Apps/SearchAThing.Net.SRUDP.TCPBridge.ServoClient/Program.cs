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

using System;
using static System.Math;

namespace SearchAThing.Net.SRUDP.TCPBridge.ServoClient
{

    class Program
    {

        static void Main(string[] args)
        {
            var client = new ServoClient(
                "192.168.0.80", 51000,
                "192.168.0.40", 50000);

            Console.WriteLine($"uptime={client.Uptime()}");

            client.DS18B20Setup(2);
            var thermometerCnt = client.DS18B20GetDeviceCount();
            Console.WriteLine($"thermometer count={thermometerCnt}");

            for (int i = 0; i < thermometerCnt; ++i)
            {
                Console.WriteLine($"thermometer n.{i} = {client.DS18B20GetTemperatureC(i)}");
            }

            client.DigitalSetup(6, DigitalMode.Output);

            Console.WriteLine($"free={client.Free(FreeMemType.MaxContiguousBlock)}");

            var vRef = 4.34;

            {
                var acLineV = 234;
                var V_to_mv = 1e3;
                var W_to_KW = 1e-3;
                var mvASensitivity = 185; // ACS712 5A model                 

                var kWH = 0.0;

                while (true)
                {
                    var begin = DateTime.Now;

                    var res = client.AnalogReadInterval(1, 1000);

                    //    Console.WriteLine($"samples cnt = {res.Samples.Length}");
                    //Console.WriteLine($"samples freq = {res.SamplesFreq}");
                    //Console.WriteLine($"sampling time (ms) = {res.SampleTotalMs}");*/

                    var instPowerSum = 0.0;
                    var sumSquaredVoltage = 0.0;
                    var sumSquaredCurrent = 0.0;

                    var cnt = res.Samples.Length;

                    foreach (var a in res.Samples)
                    {
                        var ar = a - 512;
                        var v = ar * vRef / 1024.0;

                        var instCurrent = Sqrt(Pow(v, 2)) * V_to_mv / mvASensitivity;
                        var instantPower = acLineV * instCurrent;
                        instPowerSum += instantPower;

                        var squaredVoltage = Pow(acLineV, 2);
                        sumSquaredVoltage += squaredVoltage;

                        var squaredCurrent = Pow(instCurrent, 2);
                        sumSquaredCurrent += squaredCurrent;
                    }

                    var realPower = instPowerSum / cnt;

                    var meanSquaredVoltage = sumSquaredVoltage / cnt;
                    var rmsV = Sqrt(meanSquaredVoltage);

                    var meanSquaredCurrent = sumSquaredCurrent / cnt;
                    var rmsI = Sqrt(meanSquaredCurrent);

                    var apparentPower = rmsV * rmsI;

                    var powerFactor = realPower / apparentPower;

                    var realTime = (DateTime.Now - begin).TotalMilliseconds;

                    kWH += (apparentPower * W_to_KW * realTime / 1e3 / 3600.0);

                    Console.Write(string.Format("RealPower = {0:0.00}", realPower));
                    Console.Write(string.Format("\trmsV = {0:0.00}V", rmsV));
                    Console.Write(string.Format("\trmsI = {0:0.00}A", rmsI));
                    Console.Write(string.Format("\tApparentPower = {0:0.00}", apparentPower));
                    Console.Write(string.Format("\tPowerFactor = {0:0.00}", powerFactor));
                    Console.Write(string.Format("\tCost = {0:0.0000}", kWH));
                    Console.WriteLine();
                }
            }
            /*
                        {
                            while (true)
                            {
                                // 4
                                var res = client.AnalogRead(4);

                                Console.WriteLine(string.Format("V = {0:0.00}", res/1023.0 * vRef));

                            }
                        }*/

            Console.WriteLine("press a key to terminate");
            Console.ReadKey();
        }

    }

}
