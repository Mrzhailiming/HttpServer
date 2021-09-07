using System;
using System.Net;

namespace SocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            CMDImageManager imageManager = new CMDImageManager();
            DownloadFilemanager downloadFilemanager = new DownloadFilemanager();

            int numConnections = 100;
            int receiveBufferSize = 1024 * 1024;
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);

            ServerManager server = new ServerManager(numConnections, receiveBufferSize);
            server.Init();
            server.Start(iPEndPoint);

            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }


        static void Test()
        {

        }
    }
}
