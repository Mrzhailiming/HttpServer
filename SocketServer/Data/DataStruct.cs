using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace SocketServer.Data
{
    public class AsyncUserToken
    {
        public Socket Socket 
        { 
            get 
            {
                return Socket; 
            } 
            set { Socket = value; } 
        }
    }
}
