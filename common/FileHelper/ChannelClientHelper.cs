using DataStruct;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Helper
{
    public class ChannelClientHelper
    {
        /// <summary>
        /// 负责上传
        /// </summary>
        ChannelHelper channelUpload;
        /// <summary>
        /// 负责下载
        /// </summary>
        ChannelHelper channelDownload;

        IPEndPoint localDownloadiPEndPoint;

        byte[] sendBuf;

        Semaphore semaphore = new Semaphore(0, 1);
        public ChannelClientHelper(IPEndPoint RemoteUploadiPEndPoint, IPEndPoint RemoteDownloadiPEndPoint, IPEndPoint LocalDownloadiPEndPoint, int buffSize)
        {
            //和服务器正好相反
            channelDownload = new ChannelHelper(RemoteDownloadiPEndPoint, SocketType.Stream, ProtocolType.Tcp, null, UploadProcessCmd, true, buffSize);
            channelUpload = new ChannelHelper(RemoteUploadiPEndPoint, SocketType.Stream, ProtocolType.Tcp, null, DownloadProcessCmd, false, buffSize, 0, null, CompleteSendCallBack);
            localDownloadiPEndPoint = LocalDownloadiPEndPoint;
        }

        public void Start()
        {
            //channelDownload.Bind(localDownloadiPEndPoint);//使用花生壳不能连接到server， upchannel却可以，是因为手动绑定IPend吗？是的

            channelUpload.Connect(UploadconnectSuccessCallBack);
            channelDownload.Connect(DownloadconnectSuccessCallBack);//
        }
        public void Send(string fileFullPath)
        {
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

        private void ResetSendBuff()
        {
            sendBuf = null;
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



        public void CompleteSendCallBack(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            //channelUpload.SetSendBuffer(sendBuf, 0, sendBuf.Length);
            //channelUpload.BeginSend();
        }



        public void UploadconnectSuccessCallBack(object sender, SocketAsyncEventArgs connectAsyncEventArgs)
        {
            //等待downloadchannel连接成功
            semaphore.WaitOne();
            //do login, tell server my downloadEndPoit
            //string myEndPoint = localDownloadiPEndPoint.ToString();
            //获取channelDownload的endpoint
            string endPoint = ((AsyncUserToken)channelDownload._socketEventArg.UserToken).Socket.LocalEndPoint.ToString();

            CmdBufferHelper cmdBufferHelper = new CmdBufferHelper();

            sendBuf = cmdBufferHelper.GetSendBuff((int)TCPCMDS.LOGIN, endPoint, 0);

            channelUpload.SetSendBuffer(sendBuf, 0, sendBuf.Length);
            channelUpload.BeginSend();
        }
        public void DownloadconnectSuccessCallBack(object sender, SocketAsyncEventArgs connectAsyncEventArgs)
        {
            semaphore.Release();//唤醒UploadconnectSuccessCallBack

            AsyncUserToken token = (AsyncUserToken)connectAsyncEventArgs.UserToken;
            ChannelHelper channelHelper = token._channelHelper as ChannelHelper;
            //token.exeName = $"client_downloadchannel_{token.Socket.RemoteEndPoint}";
            token.exeName = "haushengke";
            //执行recv
            bool willRaiseEvent = token.asyncUserTokenRecv.ReceiveAsync();
            if (!willRaiseEvent)
            {
                channelHelper.ProcessReceive(connectAsyncEventArgs);
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
