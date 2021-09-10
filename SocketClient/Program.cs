using Helper;
using System;
using System.Collections.Generic;
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

            //103.46.128.49:18635 //为什么只有这个端口可以连接到server
            //103.46.128.49 11279
            IPEndPoint upiPEndPoint = new IPEndPoint(IPAddress.Parse("103.46.128.49"), 18635);
            IPEndPoint downiPEndPoint = new IPEndPoint(IPAddress.Parse("103.46.128.49"), 11279);

            //IPEndPoint upiPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8000);
            //IPEndPoint downiPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000);

            Dictionary<int, ChannelClientHelper> clientDic = new Dictionary<int, ChannelClientHelper>();

            for(int i = 0; i < 10; ++i)
            {
                IPEndPoint loaclendPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8088 + i);
                ChannelClientHelper client = new ChannelClientHelper(upiPEndPoint, downiPEndPoint, loaclendPoint, bufferSize);

                client.Start();

                clientDic[i] = client;
            }
            

            while (true)
            {
                try
                {
                    string ch = Console.ReadLine();
                    if ("s" == ch.ToLower())
                    {
                        string fileFullPath = Console.ReadLine();
                        foreach(ChannelClientHelper client in clientDic.Values)
                        {
                            client.Send(fileFullPath);
                        }
                    }
                    else if ("g" == ch.ToLower())
                    {
                        string fileFullPath = Console.ReadLine();
                        foreach (ChannelClientHelper client in clientDic.Values)
                        {
                            client.Get(fileFullPath);
                        }
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
