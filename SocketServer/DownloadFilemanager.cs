using DataStruct;
using Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace SocketServer
{
    class DownloadFilemanager
    {
        public DownloadFilemanager()
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
            CMD_DS cMD_DS = new CMD_DS();

            int cmdID = CMDHelper.GetCmdID(buff);
            int fileNameLength = CMDHelper.GetCmdFileNameLength(buff);
            string fileName = CMDHelper.GetCmdFileName(buff, fileNameLength);

            string filepath = string.Format(@"{0}\Socket", Environment.CurrentDirectory);
            using (FileStream fileStream = FileHelper.OpenFile(filepath, fileName))
            {
                sendBuf = cMD_DS.GetSendBuff((int)TCPCMDS.UPLOAD, fileName, (int)fileStream.Length);
                //sendBuf = new byte[fileStream.Length + Offset.sendOffset + fileNameLength];

                BinaryReader binaryReader = new BinaryReader(fileStream);
                binaryReader.Read(sendBuf, Offset.sendOffset + fileNameLength, sendBuf.Length - Offset.sendOffset - fileNameLength);
                binaryReader.Close();
                binaryReader.Dispose();
            }

            //设置发送数据
            ServerManager.Instance.SendMsg((AsyncUserToken)task.socketAsyncEventArgs.UserToken, sendBuf);
            //AsyncUserToken token = (AsyncUserToken)task.socketAsyncEventArgs.UserToken;
            //token.asyncUserTokenSend.SetTotalSendBuff(sendBuf);
            //token.asyncUserTokenSend.SetBuffer(sendBuf.Length);
            //bool willRaiseEvent = token.asyncUserTokenSend.SendAsync();
            //if (!willRaiseEvent)
            //{
            //    ProcessSend(task.socketAsyncEventArgs);
            //}
        }

       
    }
}
