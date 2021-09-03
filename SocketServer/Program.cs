using System;
using System.Net;

namespace SocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int numConnections = 1;
            int receiveBufferSize = 100;
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);

            ServerHelper server = new ServerHelper(numConnections, receiveBufferSize);
            server.Init();
            server.Start(iPEndPoint);

            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }
}
