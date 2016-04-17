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

namespace SearchAThing.Net
{

    public static partial class Extensions
    {

        /// <summary>
        /// https://tools.ietf.org/html/rfc1071 #4.1
        /// </summary>
        public static UInt16 Checksum(this byte[] data)
        {
            UInt32 sum = 0;
            var i = 0;
            var count = data.Length;

            while (count > 1)
            {
                sum += (UInt32)data[i] << 8 | data[i + 1];
                count -= 2;
                i += 2;
            }

            if (count > 0)
            {
                sum += (UInt16)((UInt16)data[i] << 8);
            }

            while (sum >> 16 != 0)
            {
                sum = (sum & 0xffff) + (sum >> 16);
            }

            return (UInt16)(~sum & 0xffff);
        }

    }

}
