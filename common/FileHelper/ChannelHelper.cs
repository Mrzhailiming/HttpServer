using DataStruct;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Helper
{
    /// <summary>
    /// connect成功后调用的函数
    /// </summary>
    public delegate void ConnectSuccessCallBack(object sender, SocketAsyncEventArgs e);
    /// <summary>
    /// accept一个连接后调用的函数
    /// </summary>
    public delegate void AfterAcceptCallBack(SocketAsyncEventArgs newSocketEventArgs, ChannelHelper channelHelper);
    /// <summary>
    /// 接收完命令包调用的函数
    /// </summary>
    public delegate void ProcessCmd(TCPTask task);
    public class ChannelHelper
    {
        /// <summary>
        /// 侦听socket的事件， 只有在侦听的时候才会赋值
        /// </summary>
        SocketAsyncEventArgs _acceptEventArg = null;
        /// <summary>
        /// connect赋值
        /// </summary>
        SocketAsyncEventArgs _socketEventArg = null;
        /// <summary>
        /// accept一个连接后,0.啥也不干 1.执行recv 2.执行send
        /// </summary>
        //int _acceptedTodo = 0;
        /// <summary>
        /// accept一个连接后调用的函数
        /// </summary>
        AfterAcceptCallBack afterAcceptCallBack;
        /// <summary>
        /// 接收完命令包调用的函数
        /// </summary>
        ProcessCmd processCmd;

        /// <summary>
        /// 完成一个命令包接收后，是否继续接收
        /// </summary>
        bool completeRecv_Recv = false;
        /// <summary>
        /// 总共接收的字节数
        /// </summary>
        private int m_totalBytesRead = 0;
        private Socket _socket { get; }
        private IPEndPoint _iPEndPoint;
        private SocketAsyncEventArgsPool _socketAsyncEventArgsPool = new SocketAsyncEventArgsPool();
        private BufferManager _bufferManager;
        private int _receiveBufferSize = 0;
        private int _maxConnections = 0;
        private object _server;
        ChannelHelper() { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="iPEndPoint"></param>
        /// <param name="socketType"></param>
        /// <param name="protocolType"></param>
        /// <param name="AcceptCallBack"></param>
        /// <param name="processcmd"></param>
        /// <param name="isCompleteRecv_Recv"></param>
        /// <param name="receiveBufferSize"></param>
        /// <param name="maxConnections">最大客户端连接数，服务器需要赋值</param>
        public ChannelHelper(IPEndPoint iPEndPoint, SocketType socketType, ProtocolType protocolType,
            AfterAcceptCallBack AcceptCallBack, ProcessCmd processcmd, bool isCompleteRecv_Recv,
            int receiveBufferSize, int maxConnections = 0, object server = null)
        {
            _socket = new Socket(iPEndPoint.AddressFamily, socketType, protocolType);
            _iPEndPoint = iPEndPoint;

            //_acceptedTodo = acceptedTodo;
            afterAcceptCallBack = AcceptCallBack;
            processCmd = processcmd;
            completeRecv_Recv = isCompleteRecv_Recv;

            _receiveBufferSize = receiveBufferSize;
            _maxConnections = maxConnections;
            if (_maxConnections > 0)
            {
                Init();//初始化buffermanager
            }

            _server = server;
        }
        private void Init()
        {
            _bufferManager = new BufferManager(_receiveBufferSize * _maxConnections * 2, _receiveBufferSize);
            _bufferManager.InitBuffer();
        }
        public void Bind()
        {
            _socket.Bind(_iPEndPoint);
        }
        public void Listen(int backlog)
        {
            _acceptEventArg = new SocketAsyncEventArgs();
            _acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            _socket.Listen(backlog);
        }
        public void Connect(ConnectSuccessCallBack connectSuccessCallBack)
        {
            _socketEventArg = new SocketAsyncEventArgs();
            _socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            //channel是不需要这两个bool值的
            _socketEventArg.UserToken = new AsyncUserToken(_receiveBufferSize, _socketEventArg, false, false, _socket);
            //设置缓冲区
            _socketEventArg.SetBuffer(new byte[_receiveBufferSize], 0, _receiveBufferSize);

            SocketAsyncEventArgs connectEventArg = new SocketAsyncEventArgs();
            connectEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(connectSuccessCallBack);
            connectEventArg.UserToken = new AsyncUserToken(_receiveBufferSize, _socketEventArg, false, false, _socket);
            connectEventArg.RemoteEndPoint = _iPEndPoint;//设置需要连接的ip
            AsyncUserToken token = connectEventArg.UserToken as AsyncUserToken;
            token._channelHelper = this;//connectSuccessCallBack需要使用

            _socket.ConnectAsync(connectEventArg);
        }
        public void Accept()
        {
            _acceptEventArg.AcceptSocket = null;
            bool willRaiseEvent = _socket.AcceptAsync(_acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(_acceptEventArg);
            }
        }

        /// <summary>
        /// 设置发送缓冲, 客户端使用
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="beginIndex"></param>
        /// <param name="count"></param>
        public void SetSendBuffer(byte[] buffer, int beginIndex, int count)
        {
            AsyncUserToken token = (AsyncUserToken)_socketEventArg.UserToken;
            token.asyncUserTokenSend.SetTotalSendBuff(buffer);
            token.asyncUserTokenSend.SetBuffer(buffer.Length);
        }

        /// <summary>
        /// 开始发送，异步（客户端使用）
        /// </summary>
        /// <param name="sendBuf"></param>
        public void BeginSend()
        {
            AsyncUserToken token = (AsyncUserToken)_socketEventArg.UserToken;
            bool willRaiseEvent = token.asyncUserTokenSend.SendAsync();
            if (!willRaiseEvent)
            {
                ProcessSend(_socketEventArg);
            }
        }
        /// <summary>
        /// 设置发送缓冲, 服务器使用
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="beginIndex"></param>
        /// <param name="count"></param>
        public void SetSendBuffer(SocketAsyncEventArgs ClientSocketEventArg, byte[] buffer, int beginIndex, int count)
        {
            AsyncUserToken token = (AsyncUserToken)ClientSocketEventArg.UserToken;
            token.asyncUserTokenSend.SetTotalSendBuff(buffer);
            token.asyncUserTokenSend.SetBuffer(buffer.Length);
        }
        /// <summary>
        /// 开始发送，异步（服务器使用）
        /// </summary>
        /// <param name="sendBuf"></param>
        public void BeginSend(SocketAsyncEventArgs ClientSocketEventArg)
        {
            AsyncUserToken token = (AsyncUserToken)ClientSocketEventArg.UserToken;
            bool willRaiseEvent = token.asyncUserTokenSend.SendAsync();
            if (!willRaiseEvent)
            {
                ProcessSend(ClientSocketEventArg);
            }
        }
        /// <summary>
        /// 关闭
        /// </summary>
        /// <param name="socketAsyncEventArgs"></param>
        public void Close(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            AsyncUserToken token = socketAsyncEventArgs.UserToken as AsyncUserToken;

            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                LogHelper.Log(LogType.Exception, ex.ToString());
            }
            token.Socket.Close();

            _socketAsyncEventArgsPool.Push(socketAsyncEventArgs);
        }


        #region protect

        void IO_Completed(object sender, SocketAsyncEventArgs e)
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
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            //ReadEventArg object user token
            SocketAsyncEventArgs newSocketEventArgs = _socketAsyncEventArgsPool.Pop();//取出一个socket事件
            newSocketEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);//绑定回调

            _bufferManager.SetBuffer(newSocketEventArgs);//为socket设置缓冲区
            //给socket事件设置token
            newSocketEventArgs.UserToken = new AsyncUserToken(_receiveBufferSize, newSocketEventArgs, false, false, _socket);

            AsyncUserToken token = (AsyncUserToken)newSocketEventArgs.UserToken;
            token.SetSocket(e.AcceptSocket);

            if (null != afterAcceptCallBack)
            {
                afterAcceptCallBack(newSocketEventArgs, this);
            }

            //Accept下一个连接请求
            Accept();
        }

        /// <summary>
        /// 接收完新连接后需要做的事
        /// </summary>
        /// <param name="newSocketEventArgs"></param>
        //public void AfterAcceptCallBack(SocketAsyncEventArgs newSocketEventArgs, ChannelHelper channelHelper)
        //{
        //    AsyncUserToken token = (AsyncUserToken)newSocketEventArgs.UserToken;
        //    if(1 == _acceptedTodo)//执行recv
        //    {
        //        bool willRaiseEvent = token.asyncUserTokenRecv.ReceiveAsync();
        //        if (!willRaiseEvent)
        //        {
        //            ProcessReceive(newSocketEventArgs);
        //        }
        //    }
        //    else if(2 == _acceptedTodo)//执行send
        //    {
        //        bool willRaiseEvent = token.asyncUserTokenSend.SendAsync();
        //        if (!willRaiseEvent)
        //        {
        //            ProcessSend(newSocketEventArgs);
        //        }
        //    }
        //}

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
                Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
                Console.WriteLine("The server has read a total of {0} bytes", m_totalBytesRead);

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
                    TCPTask task = new TCPTask(token.Socket, token.asyncUserTokenRecv.recvBuff, e, _server);
                    token.asyncUserTokenRecv.Reset();

                    if(null != processCmd)
                    {
                        processCmd(task);//做成异步的
                    }

                    if (completeRecv_Recv)
                    {
                        token.asyncUserTokenRecv.ReceiveAsync();//继续接收
                    }

                    LogHelper.Log(LogType.SUCCESS, "ProcessReceive complete");
                }
            }
            else
            {
                Close(e);
                LogHelper.Log(LogType.Error_ConnectionReset, "ProcessReceive()");
            }
        }

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

                    LogHelper.Log(LogType.SUCCESS, "ProcessSend complete");
                }

            }
            else
            {
                Close(e);
                LogHelper.Log(LogType.Error_ConnectionReset, "ProcessSend()");
            }
        }
        #endregion protect

    }
}
