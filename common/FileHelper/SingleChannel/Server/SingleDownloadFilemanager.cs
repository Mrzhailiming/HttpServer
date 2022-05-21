using DataStruct;
using Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Helper
{
    public class SingleDownloadFilemanager
    {
        public SingleDownloadFilemanager()
        {
            Init();
        }

        private void Init()
        {
            CMDDispatcher.Instance().RegisterCMD(TCPCMDS.DOWNLOAD, MyAction);
        }

        void MyAction(TCPTask task)
        {
            Socket clientSocket = task.clientSocket;
            byte[] buff = task.buffer;
            byte[] sendBuf;
            CmdBufferHelper cmdBufferHelper = new CmdBufferHelper();

            int cmdID = CMDHelper.GetCmdID(buff);
            int fileNameLength = CMDHelper.GetCmdFileNameLength(buff);
            string fileName = CMDHelper.GetCmdFileName(buff, fileNameLength);

            string filepath = string.Format(@"{0}\Socket", Environment.CurrentDirectory);
            using (FileStream fileStream = FileHelper.OpenFile(filepath, fileName))
            {
                sendBuf = cmdBufferHelper.GetSendBuff((int)TCPCMDS.UPLOAD, fileName, (int)fileStream.Length);
                //sendBuf = new byte[fileStream.Length + Offset.sendOffset + fileNameLength];

                BinaryReader binaryReader = new BinaryReader(fileStream);
                binaryReader.Read(sendBuf, Offset.sendOffset + fileNameLength, sendBuf.Length - Offset.sendOffset - fileNameLength);
                binaryReader.Close();
                binaryReader.Dispose();
            }

            //设置发送数据
            IChannelServer_Interface channelServer = (IChannelServer_Interface)task._server;
            channelServer.SetSendBuffer(task.clientEndPoint, sendBuf);
        }
    }
}
