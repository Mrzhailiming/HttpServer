using DataStruct;
using System;
using System.Net;
using System.Net.Sockets;

namespace Helper
{
    public class SingleChannelServer : IChannelServer_Interface
    {
        /// <summary>
        /// 负责上传
        /// </summary>
        ChannelHelper channelUpload;

        private int _listenBackLog = 100;

        public SingleChannelServer(IPEndPoint UploadiPEndPoint, IPEndPoint DownloadiPEndPoint, int bufferSize, int maxConnections)
        {
            channelUpload = new ChannelHelper(UploadiPEndPoint, SocketType.Stream, ProtocolType.Tcp, 
                UploadAfterAcceptCallBack, UploadProcessCmd, true, bufferSize, maxConnections,
                this, CompleteSendCallBack);
        }

        public void Start()
        {
            channelUpload.Bind();
            channelUpload.Listen(_listenBackLog);
            channelUpload.Accept();

        }
        /// <summary>
        /// send完成的回调，继续接收
        /// </summary>
        /// <param name="e"></param>
        void CompleteSendCallBack(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            // send 的时候设置了缓冲区的大小
            // 恢复缓冲大小 !!!!!
            // 不然会对接收的时候产生剧烈影响
            socketAsyncEventArgs.SetBuffer(0, 1024 * 1024); 
            channelUpload.BeginRecv(socketAsyncEventArgs);
        }
        /// <summary>
        /// 给客户端发送信息
        /// </summary>
        /// <param name="clientEndPoint"></param>
        /// <param name="buff"></param>
        public void SetSendBuffer(EndPoint clientEndPoint, byte[] buff)
        {
            Client client = SingleGlobal.FindClient(clientEndPoint.ToString());
            if (null != client)
            {
                channelUpload.SetSendBuffer(client._clientSocketAsyncEventArgs, buff, 0, buff.Length);
                channelUpload.BeginSend(client._clientSocketAsyncEventArgs);
            }
            else
            {
                LogHelper.Log(LogType.Error_ClientNotFound, "DownloadFilemanager.MyAction()");
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
    }
}
