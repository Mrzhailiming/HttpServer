using DataStruct;
using Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketClient
{
    /// <summary>
    /// 单socket，目前不用了
    /// </summary>
    class ClientHelper
    {
        Socket _socket = null;
        SocketAsyncEventArgs _SocketAsyncEventArgs = null;
        byte[] _buff = null;//发送缓冲区
        IPEndPoint _IPEndPoint = null;
        ClientHelper() { }
        public ClientHelper(IPEndPoint iPEndPoint, int buffSize) 
        {
            try
            {
                Init(iPEndPoint, buffSize);
                Connect();
            }
            catch (Exception ex)
            {
                LogHelper.Log(LogType.Exception, ex.ToString());
                Thread.Sleep(1000);
                Reconnect();
            }
        }

        /// <summary>
        /// 重连
        /// </summary>
        private void ReConnect()
        {
            _socket = new Socket(_IPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Connect();
        }
        private void Connect()
        {
            
            _socket.Connect(_IPEndPoint.Address, _IPEndPoint.Port);

            _SocketAsyncEventArgs.SetBuffer(_buff, 0, _buff.Length);
            _SocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            _SocketAsyncEventArgs.UserToken = new AsyncUserToken(_buff.Length, _SocketAsyncEventArgs, true, false, _socket);
            ((AsyncUserToken)_SocketAsyncEventArgs.UserToken).exeName = "client";

            //test
            //((AsyncUserToken)_SocketAsyncEventArgs.UserToken).asyncUserTokenRecv.ReceiveAsync();
        }
        private void Init(IPEndPoint iPEndPoint, int buffSize)
        {
            _SocketAsyncEventArgs = new SocketAsyncEventArgs();
            _buff = new byte[buffSize];
            _IPEndPoint = iPEndPoint;

            _socket = new Socket(_IPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
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

            BeginSend(sendBuf);
        }

        public void Get(string fileFullPath)
        {
            byte[] sendBuf;
            CmdBufferHelper cmdBufferHelper = new CmdBufferHelper();

            string fileName = FileHelper.GetFileName(fileFullPath);
            sendBuf = cmdBufferHelper.GetSendBuff((int)TCPCMDS.DOWNLOAD, fileName, 0);
            BeginSend(sendBuf);
        }

        void BeginSend(byte[] sendBuf)
        {
            AsyncUserToken token = (AsyncUserToken)_SocketAsyncEventArgs.UserToken;
            token.asyncUserTokenSend.SetTotalSendBuff(sendBuf);
            token.asyncUserTokenSend.SetBuffer(sendBuf.Length);
            bool willRaiseEvent = token.asyncUserTokenSend.SendAsync();
            if (!willRaiseEvent)
            {
                ProcessSend(_SocketAsyncEventArgs);
            }
        }

        private void Release()
        {
            if (null != _socket)
            {
                _socket.Dispose();
            }
        }
        private void Reconnect()
        {
            Release();
            Init(_IPEndPoint, _buff.Length);
        }

        public void IO_Completed(object sender, SocketAsyncEventArgs e)
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
        public void ProcessReceive(SocketAsyncEventArgs e)
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
                        ProcessReceive(e);//递归
                    }
                }
                else
                {
                    TCPTask task = new TCPTask(token.Socket, token.asyncUserTokenRecv.recvBuff, e);
                    token.asyncUserTokenRecv.Reset();

                    ProcessCmd(task);

                    LogHelper.Log(LogType.SUCCESS, "client ProcessReceive complete");

                    token.asyncUserTokenRecv.ReceiveAsync();//继续接收
                }
            }
            else
            {
                LogHelper.Log(LogType.Error_ConnectionReset, "ProcessReceive()");
            }
        }

        bool isbeginRecv = false;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void ProcessSend(SocketAsyncEventArgs e)
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

                    if (!this.isbeginRecv)
                    {
                        this.isbeginRecv = true;
                        token.asyncUserTokenRecv.ReceiveAsync();//转为接收
                    }
                    LogHelper.Log(LogType.SUCCESS, "client ProcessSend complete");
                }

            }
            else
            {
                LogHelper.Log(LogType.Error_ConnectionReset, "ProcessSend()");
            }
        }

        public void ProcessCmd(TCPTask task)
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
