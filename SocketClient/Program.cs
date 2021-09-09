using Helper;
using System;
using System.Net;

namespace SocketClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //指令处理
            CMDImageManager imageManager = new CMDImageManager();
            DownloadFilemanager downloadFilemanager = new DownloadFilemanager();

            int bufferSize = 1024 * 1024;

            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
            //ClientHelper client = new ClientHelper(iPEndPoint, 1024 * 1024 * 2);
            //TestClient client = new TestClient(iPEndPoint, 1024 * 1024 * 2);

            IPEndPoint upiPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
            IPEndPoint downiPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8081);
            ChannelClient client = new ChannelClient(upiPEndPoint, downiPEndPoint, bufferSize);
            client.Start();

            while (true)
            {
                try
                {
                    string ch = Console.ReadLine();
                    if("s" == ch.ToLower())
                    {
                        string fileFullPath = Console.ReadLine();
                        client.Send(fileFullPath);
                    }
                    else if("g" == ch.ToLower())
                    {
                        string fileFullPath = Console.ReadLine();
                        client.Get(fileFullPath);
                    }
                    else
                    {
                        LogHelper.Log(LogType.SUCCESS, "chose s or g");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log(LogType.Exception, ex.ToString());
                }
            }

        }
    }
}
