using DataStruct;
using Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketClient
{
    class ChannelClient
    {
        /// <summary>
        /// 负责上传
        /// </summary>
        ChannelHelper channelUpload;
        /// <summary>
        /// 负责下载
        /// </summary>
        ChannelHelper channelDownload;

        SocketAsyncEventArgs _SocketAsyncEventArgs;
        public ChannelClient(IPEndPoint UploadiPEndPoint, IPEndPoint DownloadiPEndPoint, int buffSize)
        {
            //和服务器正好相反
            channelDownload = new ChannelHelper(DownloadiPEndPoint, SocketType.Stream, ProtocolType.Tcp, null, UploadProcessCmd, true, buffSize);
            channelUpload = new ChannelHelper(UploadiPEndPoint, SocketType.Stream, ProtocolType.Tcp, null, DownloadProcessCmd, false, buffSize);
        }

        public void Start()
        {
            channelUpload.Connect(UploadconnectSuccessCallBack);
            channelDownload.Connect(DownloadconnectSuccessCallBack);//
        }
        public void Send(string fileFullPath)
        {
            byte[] sendBuf;

            CmdBufferHelper cmdBufferHelper = new CmdBufferHelper();

            using (FileStream fs = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                string fileName = FileHelper.GetFileName(fileFullPath);

                byte[] fileBuf = Encoding.Default.GetBytes(fileName);
                int fileNameLength = fileBuf.Length;

                sendBuf = cmdBufferHelper.GetSendBuff((int)TCPCMDS.UPLOAD, fileName, (int)fs.Length);

                BinaryReader binaryReader = new BinaryReader(fs);//用二进制流
                int sendOffset = Offset.sendOffset;//命令头的偏移
                binaryReader.Read(sendBuf, sendOffset + fileNameLength, sendBuf.Length - sendOffset - fileNameLength);
                binaryReader.Close();
                binaryReader.Dispose();
            }

            channelUpload.SetSendBuffer(sendBuf, 0, sendBuf.Length);
            channelUpload.BeginSend();
        }

        public void Get(string fileFullPath)
        {
            byte[] sendBuf;
            CmdBufferHelper cmdBufferHelper = new CmdBufferHelper();

            string fileName = FileHelper.GetFileName(fileFullPath);
            sendBuf = cmdBufferHelper.GetSendBuff((int)TCPCMDS.DOWNLOAD, fileName, 0);

            channelUpload.SetSendBuffer(sendBuf, 0, sendBuf.Length);
            channelUpload.BeginSend();
        }
        public void UploadconnectSuccessCallBack(object sender, SocketAsyncEventArgs connectAsyncEventArgs)
        {
            //do nothing
        }
        public void DownloadconnectSuccessCallBack(object sender, SocketAsyncEventArgs connectAsyncEventArgs)
        {
            AsyncUserToken token = (AsyncUserToken)connectAsyncEventArgs.UserToken;
            ChannelHelper channelHelper = token._channelHelper as ChannelHelper;
            //执行recv
            bool willRaiseEvent = token.asyncUserTokenRecv.ReceiveAsync();
            if (!willRaiseEvent)
            {
                channelHelper.ProcessReceive(connectAsyncEventArgs);
            }
        }
        void UploadAfterAcceptCallBack(SocketAsyncEventArgs newSocketEventArgs, ChannelHelper channelHelper)
        {
            AsyncUserToken token = (AsyncUserToken)newSocketEventArgs.UserToken;
            //执行recv
            bool willRaiseEvent = token.asyncUserTokenRecv.ReceiveAsync();
            if (!willRaiseEvent)
            {
                channelHelper.ProcessReceive(newSocketEventArgs);
            }
        }
        void UploadProcessCmd(TCPTask task)
        {
            try
            {
                CMDDispatcher.Instance().Dispatcher(task);

            }
            catch (Exception ex)
            {
                LogHelper.Log(LogType.Exception_ProcessCmd, ex.ToString());
            }
        }

        void DownloadAfterAcceptCallBack(SocketAsyncEventArgs newSocketEventArgs, ChannelHelper channelHelper)
        {
            //AsyncUserToken token = (AsyncUserToken)newSocketEventArgs.UserToken;
            ////执行recv
            //bool willRaiseEvent = token.asyncUserTokenRecv.ReceiveAsync();
            //if (!willRaiseEvent)
            //{
            //    channelHelper.ProcessReceive(newSocketEventArgs);
            //}
        }
        void DownloadProcessCmd(TCPTask task)
        {
            try
            {
                CMDDispatcher.Instance().Dispatcher(task);

            }
            catch (Exception ex)
            {
                LogHelper.Log(LogType.Exception_ProcessCmd, ex.ToString());
            }
        }
    }
}
