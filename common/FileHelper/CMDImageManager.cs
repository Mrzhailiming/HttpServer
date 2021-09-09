using DataStruct;
using Helper;
using System;
using System.IO;
using System.Text;

namespace Helper
{
    public class CMDImageManager
    {
        public CMDImageManager()
        {
            Init();
        }

        void Init()
        {
            CMDDispatcher.Instance().RegisterCMD(TCPCMDS.UPLOAD, MyAction);
        }

        void MyAction(TCPTask task)
        {
            byte[] buff = task.buffer;

            int cmdID = CMDHelper.GetCmdID(buff);
            int fileNameLength = CMDHelper.GetCmdFileNameLength(buff);
            string fileNmae = CMDHelper.GetCmdFileName(buff, fileNameLength);

            string filepath = string.Format(@"{0}\Socket", Environment.CurrentDirectory);
            using (FileStream fileStream = FileHelper.CreateFile(filepath, fileNmae))
            {
                BinaryWriter binaryWriter = new BinaryWriter(fileStream);
                binaryWriter.Write(buff, 4 + 4 + 4 + fileNameLength, buff.Length - 4 - 4 - 4 - fileNameLength);
                binaryWriter.Flush();
                binaryWriter.Close();
                binaryWriter.Dispose();
            }
        }
    }
}
