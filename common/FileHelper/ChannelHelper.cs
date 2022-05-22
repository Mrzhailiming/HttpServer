using DataStruct;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Helper
{
    /// <summary>
    /// send完成后调用的函数
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void CompleteSendCallBack(SocketAsyncEventArgs e);
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
        public SocketAsyncEventArgs _socketEventArg = null;
        /// <summary>
        /// accept一个连接后调用的函数, 服务器用
        /// </summary>
        AfterAcceptCallBack afterAcceptCallBack;
        /// <summary>
        /// 接收完命令包调用的函数
        /// </summary>
        ProcessCmd processCmd;
        /// <summary>
        /// send完成后调用的函数
        /// </summary>
        CompleteSendCallBack completeSendCallBack;
        /// <summary>
        /// 完成一个命令包接收后，是否继续接收
        /// </summary>
        bool completeRecv_Recv = false;
        /// <summary>
        /// 总共接收的字节数
        /// </summary>
        private int m_totalBytesRead = 0;
        private Socket _socket { get; }
        /// <summary>
        /// bind时候作为本地endpoint， connect的时候作为remoteendpoint
        /// </summary>
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
            int receiveBufferSize, int maxConnections = 0, object server = null, CompleteSendCallBack completeSendCall = null)
        {
            _socket = new Socket(iPEndPoint.AddressFamily, socketType, protocolType);
            _iPEndPoint = iPEndPoint;

            afterAcceptCallBack = AcceptCallBack;
            processCmd = processcmd;
            completeSendCallBack = completeSendCall;
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="localEndPoint">指定绑定的endpoint，为空绑定默认的</param>
        public void Bind(EndPoint localEndPoint = null)
        {
            if(null != localEndPoint)
            {
                _socket.Bind(localEndPoint);
            }
            else
            {
                _socket.Bind(_iPEndPoint);
            }
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
        /// 开始发送，异步
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

        public void Send(byte[] buf)
        {
            AsyncUserToken token = (AsyncUserToken)_socketEventArg.UserToken;
            token.asyncUserTokenSend.Socket.Send(buf);
        }

        /// <summary>
        /// 开始异步接收
        /// </summary>
        public void BeginRecv(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            AsyncUserToken token = (AsyncUserToken)socketAsyncEventArgs.UserToken;
            bool willRaiseEvent = token.asyncUserTokenRecv.ReceiveAsync();
            if (!willRaiseEvent)
            {
                ProcessReceive(socketAsyncEventArgs);
            }
        }
        /// <summary>
        /// 开始同步接收
        /// </summary>
        public void BeginRecv_new(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            AsyncUserToken token = (AsyncUserToken)socketAsyncEventArgs.UserToken;
            bool willRaiseEvent = token.asyncUserTokenRecv.ReceiveAsync();
            if (!willRaiseEvent)
            {
                ProcessReceive_new(socketAsyncEventArgs);
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

        //public async void RecvAsync()
        //{
        //    await Task.Run();
        //}

        //public void runAsync(object o)
        //{

        //}

        /// <summary>
        /// 异步
        /// </summary>
        /// <param name="e"></param>
        public void ProcessReceive(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                //// 判断所有需接收的数据是否已经完成
                //if (token.Socket.Available != 0)
                //{
                //    bool willRaiseEvent = token.asyncUserTokenRecv.ReceiveAsync();
                //    if (!willRaiseEvent)
                //    {
                //        ProcessReceive(e);
                //    }
                //}

                Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
                Console.WriteLine($"The server has read a total of {m_totalBytesRead} bytes curbytes:{e.BytesTransferred}");

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
                        //token.asyncUserTokenRecv.AsyncEventArgs.SetBuffer(0, 1024 * 1024);
                        token.asyncUserTokenRecv.ReceiveAsync();//继续接收
                    }

                    LogHelper.Log(LogType.SUCCESS, "ProcessReceive complete");
                }
            }
            else
            {
                if(e.SocketError == SocketError.SocketError)//先不关闭连接
                {
                    Close(e);
                    LogHelper.Log(LogType.Error_ConnectionReset, "ProcessReceive()");
                }
                else
                {
                    LogHelper.Log(LogType.Error_BytesTransferred, "ProcessReceive()");
                }
            }
        }

        /// <summary>
        /// t同步
        /// </summary>
        /// <param name="e"></param>
        public void ProcessReceive_new(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
                Console.WriteLine("The server has read a total of {0} bytes", m_totalBytesRead);

                token.asyncUserTokenRecv.BuffCopy();
                while (!token.asyncUserTokenRecv.Check())
                {
                    byte[] recvBuff = new byte[_receiveBufferSize];
                    int recvCount = token.asyncUserTokenRecv.Receive(recvBuff);
                    token.asyncUserTokenRecv.BuffCopy_new(recvBuff, recvCount);
                }
                //else
                {
                    TCPTask task = new TCPTask(token.Socket, token.asyncUserTokenRecv.recvBuff, e, _server);
                    token.asyncUserTokenRecv.Reset();

                    if (null != processCmd)
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
                if (e.SocketError == SocketError.SocketError)//先不关闭连接
                {
                    Close(e);
                    LogHelper.Log(LogType.Error_ConnectionReset, "ProcessReceive()");
                }
                else
                {
                    LogHelper.Log(LogType.Error_BytesTransferred, "ProcessReceive()");
                }
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

                    if(null != completeSendCallBack)
                    {
                        completeSendCallBack(e);
                    }

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
