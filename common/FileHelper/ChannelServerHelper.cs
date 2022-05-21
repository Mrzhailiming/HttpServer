using DataStruct;
using Helper;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Helper
{
    public class ChannelServer : IChannelServer_Interface
    {
        /// <summary>
        /// 负责上传
        /// </summary>
        ChannelHelper channelUpload;
        /// <summary>
        /// 负责下载
        /// </summary>
        ChannelHelper channelDownload;

        private int _listenBackLog = 100;

        public ChannelServer(IPEndPoint UploadiPEndPoint, IPEndPoint DownloadiPEndPoint, int bufferSize, int maxConnections)
        {
            channelUpload = new ChannelHelper(UploadiPEndPoint, SocketType.Stream, ProtocolType.Tcp, UploadAfterAcceptCallBack, UploadProcessCmd, true, bufferSize, maxConnections, this);
            channelDownload = new ChannelHelper(DownloadiPEndPoint, SocketType.Stream, ProtocolType.Tcp, DownloadAfterAcceptCallBack, DownloadProcessCmd, false, bufferSize, maxConnections, this);
        }

        public void Start()
        {
            channelUpload.Bind();
            channelUpload.Listen(_listenBackLog);
            channelUpload.Accept();

            channelDownload.Bind();
            channelDownload.Listen(_listenBackLog);
            channelDownload.Accept();
        }
        public void SetSendBuffer(EndPoint clientEndPoint, byte[] buff)
        {
            Client client = Global.FindClient(clientEndPoint.ToString());
            if (null != client)
            {
                channelDownload.SetSendBuffer(client._clientSocketAsyncEventArgs, buff, 0, buff.Length);
                channelDownload.BeginSend(client._clientSocketAsyncEventArgs);
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

        void DownloadAfterAcceptCallBack(SocketAsyncEventArgs newSocketEventArgs, ChannelHelper channelHelper)
        {
            AsyncUserToken token = newSocketEventArgs.UserToken as AsyncUserToken;
            EndPoint remoteEndPoint = token.Socket.RemoteEndPoint;
            token.exeName = $"server_downloadchannel_{remoteEndPoint}";

            //记录一个新链接
            Global._clientDic[remoteEndPoint.ToString()] = new Client() { _clientSocketAsyncEventArgs = newSocketEventArgs };
        }

        private string GetIP(EndPoint remoteEndPoint)
        {
            string ret = remoteEndPoint.ToString();
            int endIndex = ret.IndexOf(':');
            return ret.Substring(0, endIndex);
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
