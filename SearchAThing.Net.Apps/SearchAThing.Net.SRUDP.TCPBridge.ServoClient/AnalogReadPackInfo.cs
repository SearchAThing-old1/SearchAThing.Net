using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SearchAThing.Core;
using System.Globalization;

namespace SearchAThing.Net.SRUDP.TCPBridge.ServoClient
{

    public class AnalogReadPackInfo
    {

        /// <summary>
        /// samples/sec rate
        /// </summary>
        public double SamplesFreq = 0.0;

        /// <summary>
        /// total time during sampling (ms).
        /// </summary>
        public UInt32 SampleTotalMs = 0;

        public UInt16[] Samples;

    }

}
