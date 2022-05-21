using DataStruct;
using Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Helper
{
    public class SingleClientDownloadFilemanager
    {
        public SingleClientDownloadFilemanager()
        {
            Init();
        }

        private void Init()
        {
            CMDDispatcher.Instance().RegisterCMD(TCPCMDS.DOWNLOAD, MyAction);
        }

        /// <summary>
        /// 接收服务器发回的下载数据
        /// </summary>
        /// <param name="task"></param>
        void MyAction(TCPTask task)
        {
            byte[] buff = task.buffer;

            int cmdID = CMDHelper.GetCmdID(buff);
            int fileNameLength = CMDHelper.GetCmdFileNameLength(buff);
            string fileNmae = CMDHelper.GetCmdFileName(buff, fileNameLength);

            AsyncUserToken asyncUserToken = task.socketAsyncEventArgs.UserToken as AsyncUserToken;

            fileNmae = $"remote_{task.clientEndPoint.GetHashCode()}_loacl_{asyncUserToken.Socket.LocalEndPoint.GetHashCode()}_{fileNmae}";

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
