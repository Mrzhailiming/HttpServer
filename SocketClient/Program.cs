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
            //UploadFilemanager imageManager = new UploadFilemanager();
            SingleClientDownloadFilemanager downloadFilemanager = new SingleClientDownloadFilemanager();
            SingleClientLoginManager loginManager = new SingleClientLoginManager();//单通道


            int bufferSize = 1024 * 1024;

            //103.46.128.49:18635 //为什么只有这个端口可以连接到server，花生壳？
            //103.46.128.49 11279
            //IPEndPoint upiPEndPoint = new IPEndPoint(IPAddress.Parse("103.46.128.49"), 18635);
            //IPEndPoint downiPEndPoint = new IPEndPoint(IPAddress.Parse("103.46.128.49"), 11279);

            IPEndPoint upiPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            IPEndPoint downiPEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.10"), 9000);

            Dictionary<int, IClientHelper_Interface> clientDic = new Dictionary<int, IClientHelper_Interface>();

            for(int i = 0; i < 1; ++i)
            {
                IPEndPoint loaclendPoint = new IPEndPoint(IPAddress.Parse("192.168.1.10"), 8088 + i);
                IClientHelper_Interface client = new SingleChannelClientHelper(upiPEndPoint, downiPEndPoint, loaclendPoint, bufferSize);

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
                        foreach(IClientHelper_Interface client in clientDic.Values)
                        {
                            client.Send(fileFullPath);
                            Console.WriteLine("上传成功!");
                        }
                    }
                    else if ("g" == ch.ToLower())
                    {
                        string fileFullPath = Console.ReadLine();
                        foreach (IClientHelper_Interface client in clientDic.Values)
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
