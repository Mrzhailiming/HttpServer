using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            string imgPath = "";
            byte[] bArr;
            //读源jpg
            using (FileStream oldFileStream = new FileStream(imgPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                bArr = new byte[oldFileStream.Length];
                oldFileStream.Read(bArr, 0, bArr.Length);
                oldFileStream.Close();
            }
                
            //写入新的文件
            using (FileStream newFileStre = new FileStream($"{imgPath}_new.jpg", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                newFileStre.Write(bArr, 0, bArr.Length);
            }
        }
    }
}
