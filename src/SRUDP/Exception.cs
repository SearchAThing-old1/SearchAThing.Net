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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchAThing.Net.SRUDP
{

    public class ExceptionBase : Exception
    {
        string msg;

        public ExceptionBase(string _msg = "") : base(_msg)
        {
            msg = _msg;
        }

        public override string ToString()
        {
            return msg;
        }
    }

    public class SRUDPConnectionTimeoutException : ExceptionBase
    {
        public SRUDPConnectionTimeoutException(string msg = "") : base($"ConnectionTimeout {msg}")
        {
        }
    }    

    public class SRUDPListenerAlreadyStarted : ExceptionBase
    {
        public SRUDPListenerAlreadyStarted(string msg = "") : base($"Listener already started {msg}")
        {
        }
    }

    public class SRUDPListenerNotStarted : ExceptionBase
    {
        public SRUDPListenerNotStarted(string msg = "") : base($"Listener not started {msg}")
        {
        }
    }

    public class SRUDPClientDisconnected : ExceptionBase
    {
        public SRUDPClientDisconnected(string msg = "") : base($"Client disconnected {msg}")
        {

        }
    }

    public class SRUDPClientAlreadyConnected : ExceptionBase
    {
        public SRUDPClientAlreadyConnected(string msg = "") : base($"Client already connected {msg}")
        {

        }
    }

    public class SRUDPClientAlreadyDisconnected : ExceptionBase
    {
        public SRUDPClientAlreadyDisconnected(string msg = "") : base($"Client already disconnected {msg}")
        {

        }
    }

}
