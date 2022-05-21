using Helper;
using System;
using System.Net;

namespace SocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int numConnections = 101;
            int receiveBufferSize = 1024 * 1024 * 10;

            //双通指令处理
            //UploadFilemanager imageManager = new UploadFilemanager();
            //DownloadFilemanager downloadFilemanager = new DownloadFilemanager();
            //LoginManager loginManager = new LoginManager();//双通道login

            //单通
            SingleDownloadFilemanager singledownloadFilemanager = new SingleDownloadFilemanager();
            SingleServerLoginManager singleServerLoginManager = new SingleServerLoginManager();//单通道
            SingleUploadFileManager singleUploadManager = new SingleUploadFileManager();


            IPEndPoint upiPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8000);
            IPEndPoint downiPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000);

            //双通道
            //ChannelServer channelServer = new ChannelServer(upiPEndPoint, downiPEndPoint, receiveBufferSize, numConnections);
            //channelServer.Start();

            SingleChannelServer singleChannel = new SingleChannelServer(upiPEndPoint, downiPEndPoint, receiveBufferSize, numConnections);
            singleChannel.Start();

            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }


        static void Test()
        {

        }
    }
}
