using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace SocketServer
{
    public interface SocketServerHelper_Interface
    {
        TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, 13000));
    }
}
