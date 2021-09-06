using System;
using System.Net;

namespace SocketClient
{
    class Program
    {
        static void Main(string[] args)
        {
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
            ClientHelper client = new ClientHelper(iPEndPoint, 1024 * 1024);

            while (true)
            { 
                string fileFullPath = Console.ReadLine();
                client.Send(fileFullPath);
            }

        }
    }
}
