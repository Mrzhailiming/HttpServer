using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Helper
{
    public interface IChannelServer_Interface
    {
        void SetSendBuffer(EndPoint clientEndPoint, byte[] buff);
    }
}
