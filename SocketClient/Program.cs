using Helper;
using System;
using System.Net;

namespace SocketClient
{
    class Program
    {
        static void Main(string[] args)
        {
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
            ClientHelper client = new ClientHelper(iPEndPoint, 1024 * 1024 * 2);
            //TestClient client = new TestClient(iPEndPoint, 1024 * 1024 * 2);

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
