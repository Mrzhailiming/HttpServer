using Helper;
using SocketServer.Data;
using System;
using System.IO;
using System.Text;

namespace SocketServer
{
    class CMDImageManager
    {
        public CMDImageManager()
        {
            Init();
        }

        void Init()
        {
            CMDDispatcher.Instance().RegisterCMD(TCPCMDS.IMAGE, MyAction);
        }

        void MyAction(CMDHandlerActionParam param)
        {
            byte[] buff = param._buffer;

            int cmdID = GetCmdID(buff);
            int fileNameLength = GetFileNameLength(buff);
            string fileNmae = GetFileName(buff, fileNameLength);

            string filepath = string.Format(@"{0}\Socket", Environment.CurrentDirectory);
            using (FileStream fileStream = FileHelper.CreateFileStream(filepath, fileNmae))
            {
                BinaryWriter binaryWriter = new BinaryWriter(fileStream);
                binaryWriter.Write(buff, 4, buff.Length - 4);
                binaryWriter.Flush();
                binaryWriter.Close();
                binaryWriter.Dispose();
            }
        }
        

        public int GetCmdID(byte[] buff)
        {
            byte[] destBuf = new byte[4];
            Array.Copy(buff, 0, destBuf, 0, 4);
            return BitConverter.ToInt32(destBuf, 0);
        }
        public int GetFileNameLength(byte[] buff)
        {
            byte[] destBuf = new byte[4];
            Array.Copy(buff, 4, destBuf, 0, 4);
            return BitConverter.ToInt32(destBuf, 0);
        }
        public string GetFileName(byte[] buff, int fileNameLength)
        {
            byte[] destBuf = new byte[fileNameLength];
            Array.Copy(buff, 8, destBuf, 0, fileNameLength);
            return Encoding.Default.GetString(destBuf);
        }
    }
}
