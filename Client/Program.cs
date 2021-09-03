using System;

namespace New_MyHttpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            string exePath = string.Format(@"{0}\images\test.jpg", Environment.CurrentDirectory);
            ClientHelper client = new ClientHelper();

            string uploadUrl = "http://127.0.0.1:8080";
            string imgPath = exePath;
            string fileparameter = "file";

            while(true)
            {
                if(Console.ReadLine() == "s")
                {
                    client.UploadImage(uploadUrl, imgPath, fileparameter);
                    Console.WriteLine("SendSuccess");
                }
            }
        }
    }
}
