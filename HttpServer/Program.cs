using System;

namespace server
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerHelper server = new ServerHelper();
            server.Setup();

            RequestHelper test = new RequestHelper();
            test.Test();

            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }
}
