using DataStruct;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Helper
{
    /// <summary>
    /// 暂时不用
    /// </summary>
    public class Socket_IOHelper_____
    {
        public static void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public static void ProcessReceive(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                token.asyncUserTokenRecv.BuffCopy();
                if (!token.asyncUserTokenRecv.Check())
                {
                    bool willRaiseEvent = token.asyncUserTokenRecv.ReceiveAsync();
                    if (!willRaiseEvent)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    TCPTask task = new TCPTask(token.Socket, token.asyncUserTokenRecv.recvBuff, e);
                    token.asyncUserTokenRecv.Reset();

                    ProcessCmd(task);

                    if (token.IsContinueRecv())
                    {
                        token.asyncUserTokenRecv.ReceiveAsync();//继续接收
                    }
                }
            }
            else
            {
                LogHelper.Log(LogType.Error_ConnectionReset, "ProcessReceive()");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public static void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // done echoing data back to the client
                AsyncUserToken token = (AsyncUserToken)e.UserToken;

                if (!token.asyncUserTokenSend.IsSendComplete())//没发送完
                {
                    token.asyncUserTokenSend.SetBuffer(token.asyncUserTokenSend.needSendNum - token.asyncUserTokenSend.hadSendNum);
                    bool send = token.asyncUserTokenSend.SendAsync();
                    if (!send)
                    {
                        ProcessSend(e);//继续发送
                    }
                }
                else//发送完了
                {
                    token.asyncUserTokenSend.Reset();

                    if (token.IsCompleteSendTORecv())
                    {
                        token.asyncUserTokenRecv.ReceiveAsync();//转为接收
                    }
                    LogHelper.Log(LogType.SUCCESS, "");
                }

            }
            else
            {
                LogHelper.Log(LogType.Error_ConnectionReset, "ProcessSend()");
            }
        }

        public static void ProcessCmd(TCPTask task)
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
