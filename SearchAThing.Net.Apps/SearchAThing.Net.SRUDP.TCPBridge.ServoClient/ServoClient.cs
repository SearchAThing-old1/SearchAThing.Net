using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SearchAThing.Core;
using System.Globalization;

namespace SearchAThing.Net.SRUDP.TCPBridge.ServoClient
{

    public enum FreeMemType { FragmentedSum = 0, MaxContiguousBlock = 1 };
    public enum DigitalMode { Input, Output };

    public class ServoClient
    {

        SRUDP.TCPBridge.Client client;

        public ServoClient(string bridgeIp, int bridgePort,
            string srudpDeviceHost, int srudpDevicePort)
        {
            client = new Client(bridgeIp, bridgePort);
            client.Connect(srudpDeviceHost, srudpDevicePort);
        }

        // https://searchathing.com/?page_id=886#uptime
        public TimeSpan Uptime()
        {
            client.WriteLine("uptime");
            // OK
            client.ReadLine();

            // <days>.<milliseconds>
            var s = client.ReadLine();
            var ss = s.Split('.');

            return TimeSpan.FromDays(int.Parse(ss[0])).Add(TimeSpan.FromMilliseconds(int.Parse(ss[1])));
        }

        // https://searchathing.com/?page_id=886#free
        public int Free(FreeMemType type)
        {
            client.WriteLine($"free {(int)type}");
            // OK
            client.ReadLine();

            // <free-ram-bytes>
            var s = client.ReadLine();

            return int.Parse(s);
        }

        // https://searchathing.com/?page_id=886#ds18b20_setup
        public void DS18B20Setup(int port)
        {
            client.WriteLine($"ds18b20_setup {port}");
            // OK
            client.ReadLine();
        }

        // https://searchathing.com/?page_id=886#ds18b20_count
        public int DS18B20GetDeviceCount()
        {
            client.WriteLine($"ds18b20_count");

            var x = client.ReadLine();
            if (x == "OK")
            {
                return int.Parse(client.ReadLine());
            }
            else if (x.StartsWith("ERR "))
            {
                throw new Exception($"ERR {x.StripBegin("ERR ")}");
            }
            else throw new Exception($"unexpected protocol msg {x}");
        }

        // https://searchathing.com/?page_id=886#ds18b20_get
        public float DS18B20GetTemperatureC(int idx)
        {
            client.WriteLine($"ds18b20_get {idx}");

            var x = client.ReadLine();
            if (x == "OK")
            {
                var s = client.ReadLine();
                return float.Parse(s, CultureInfo.InvariantCulture);
            }
            else if (x.StartsWith("ERR "))
            {
                throw new Exception($"ERR {x.StripBegin("ERR ")}");
            }
            else throw new Exception($"unexpected protocol msg {x}");
        }

        // https://searchathing.com/?page_id=886#digital_setup
        public void DigitalSetup(int port, DigitalMode mode)
        {
            client.WriteLine($"digital_setup {port} {((mode == DigitalMode.Input) ? "IN" : "OUT")}");

            var x = client.ReadLine();
            if (x == "OK")
            {
                return;
            }
            else if (x.StartsWith("ERR "))
            {
                throw new Exception($"ERR {x.StripBegin("ERR ")}");
            }
            else throw new Exception($"unexpected protocol msg {x}");
        }

        // https://searchathing.com/?page_id=886#digital_write
        public void DigitalWrite(int port, int value)
        {
            client.WriteLine($"digital_write {port} {value}");

            var x = client.ReadLine();
            if (x == "OK")
            {
                return;
            }
            else if (x.StartsWith("ERR "))
            {
                throw new Exception($"ERR {x.StripBegin("ERR ")}");
            }
            else throw new Exception($"unexpected protocol msg {x}");
        }

        // https://searchathing.com/?page_id=886#digital_read
        public int DigitalRead(int port)
        {
            client.WriteLine($"digital_read {port}");

            var x = client.ReadLine();
            if (x == "OK")
            {                
                return int.Parse(client.ReadLine());
            }
            else if (x.StartsWith("ERR "))
            {
                throw new Exception($"ERR {x.StripBegin("ERR ")}");
            }
            else throw new Exception($"unexpected protocol msg {x}");
        }

        // https://searchathing.com/?page_id=886#analog_read
        public int AnalogRead(int port)
        {
            client.WriteLine($"analog_read {port}");

            var x = client.ReadLine();
            if (x == "OK")
            {
                return int.Parse(client.ReadLine());
            }
            else if (x.StartsWith("ERR "))
            {
                throw new Exception($"ERR {x.StripBegin("ERR ")}");
            }
            else throw new Exception($"unexpected protocol msg {x}");
        }

        public AnalogReadPackInfo AnalogReadInterval(int port, uint intervalMs)
        {
            var res = new AnalogReadPackInfo();
            var dtBegin = DateTime.Now;

            while ((DateTime.Now - dtBegin).TotalMilliseconds < intervalMs)
            {
                // with 600bytes packet excluding headers data bytes aprox
                // 480 enough for max 240 samples
                var r = AnalogReadPack(port, 100); // 200bytes ram

                if (res.Samples != null)
                {
                    var newArr = new ushort[res.Samples.Length + r.Samples.Length];
                    res.Samples.CopyTo(newArr, 0);
                    r.Samples.CopyTo(newArr, res.Samples.Length);
                    res.Samples = newArr;
                }
                else
                    res = r;
            }

            res.SampleTotalMs = intervalMs;
            res.SamplesFreq = (double)res.Samples.Length / (intervalMs / 1000);

            return res;
        }

        // https://searchathing.com/?page_id=886#analog_read_pack
        public AnalogReadPackInfo AnalogReadPack(int port, int samplesCount)
        {
            var dtBegin = DateTime.Now;

            var res = new AnalogReadPackInfo();

            client.WriteLine($"analog_read_pack {port} {samplesCount}");

            var x = client.ReadLine();
            if (x == "OK")
            {                
                var totalTimeMs = UInt32.Parse(client.ReadLine());

                var data = client.ReadBytes();

                res.Samples = new UInt16[samplesCount];
                res.SampleTotalMs = totalTimeMs;
                for (int i = 0; i < samplesCount; ++i)
                    res.Samples[i] = (UInt16)(((UInt16)data[i * 2]) << 8 | ((UInt16)data[i * 2 + 1]));

                res.SamplesFreq = (double)samplesCount / (DateTime.Now - dtBegin).TotalSeconds;

                return res;
            }
            else if (x.StartsWith("ERR "))
            {
                throw new Exception($"ERR {x.StripBegin("ERR ")}");
            }
            else throw new Exception($"unexpected protocol msg {x}");
        }

    }

}
