//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;

//namespace Server
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            string imgPath = "F:\\test.jpg";
//            byte[] bArr;
//            //读源jpg
//            using (FileStream oldFileStream = new FileStream(imgPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
//            {

//                BinaryReader binaryReader = new BinaryReader(oldFileStream);

//                bArr = new byte[oldFileStream.Length];
//                binaryReader.Read(bArr, 0, bArr.Length);
//                binaryReader.Close();
//                //写入新的文件
//                using (FileStream newFileStre = new FileStream($"{imgPath}_binary.jpg", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
//                {
//                    BinaryWriter binaryWriter = new BinaryWriter(newFileStre);
//                    binaryWriter.Write(bArr, 0, bArr.Length);
//                }
//            }
//        }
//    }
//}
