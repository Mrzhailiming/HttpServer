using DataStruct;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helper
{
    public class SingleServerLoginManager
    {
        public SingleServerLoginManager()
        {
            Init();
        }

        private void Init()
        {
            CMDDispatcher.Instance().RegisterCMD(TCPCMDS.LOGIN, MyAction);
        }

        void MyAction(TCPTask task)
        {
            byte[] buff = task.buffer;

            int fileNameLength = CMDHelper.GetCmdFileNameLength(buff);
            string IPPORT = CMDHelper.GetCmdFileName(buff, fileNameLength);

            SingleGlobal._clientDic[task.clientEndPoint.ToString()] = new Client() { _clientSocketAsyncEventArgs = task.socketAsyncEventArgs};//添加客户端
            //AsyncUserToken token = (AsyncUserToken)task.socketAsyncEventArgs.UserToken;
            //token.asyncUserTokenSend.;
            IChannelServer_Interface channelServer = (IChannelServer_Interface)task._server;
            CmdBufferHelper cmdBufferHelper = new CmdBufferHelper();

            string loginResult = "0";//暂时用0表示成功
            byte[] sendBuf = cmdBufferHelper.GetSendBuff((int)TCPCMDS.LOGIN, loginResult, 0);

            channelServer.SetSendBuffer(task.clientEndPoint, sendBuf);//向客户端发送登录结果
        }
    }
}
