using System;
using System.Net;

namespace SocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int numConnections = 100;
            int receiveBufferSize = 1024 * 1024;

            //指令处理
            CMDImageManager imageManager = new CMDImageManager();
            DownloadFilemanager downloadFilemanager = new DownloadFilemanager();

            //servermanager.cs
            //IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
            //ServerManager server = new ServerManager(numConnections, receiveBufferSize);
            //server.Init();
            //server.Start(iPEndPoint);

            //testserver.cs
            //Server server1 = new Server(numConnections, receiveBufferSize);
            //server1.Init();
            //server1.Start(iPEndPoint);

            IPEndPoint upiPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
            IPEndPoint downiPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8081);
            ChannelServer channelServer = new ChannelServer(upiPEndPoint, downiPEndPoint, receiveBufferSize, numConnections);
            channelServer.Start();

            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }


        static void Test()
        {

        }
    }
}
