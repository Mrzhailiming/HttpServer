using System;
using System.Collections.Generic;
using System.Text;

namespace SocketServer.Data
{
    public class CMD_DS
    {
        public CMDHeader _header = null;

        public CMDBody _body = null;
    }

    public class CMDHeader
    {
        public int CMD_ID { get; set; }//4b
    }

    public class CMDBody
    {
        public byte[] buffer = null;//
    }
}
