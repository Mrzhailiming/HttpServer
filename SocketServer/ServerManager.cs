using DataStruct;
using Helper;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketServer
{
    // Implements the connection logic for the socket server.
    // After accepting a connection, all data read from the client
    // is sent back to the client. The read and echo back to the client pattern
    // is continued until the client disconnects.

    class ServerManager
    {
        private int m_numConnections;   // the maximum number of connections the sample is designed to handle simultaneously
        private int m_receiveBufferSize;// buffer size to use for each socket I/O operation
        BufferManager m_bufferManager;  // represents a large reusable set of buffers for all socket operations
        const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
        Socket listenSocket;            // the socket used to listen for incoming connection requests
                                        // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
        SocketAsyncEventArgsPool m_readWritePool;
        int m_totalBytesRead;           // counter of the total # bytes received by the server
        int m_numConnectedSockets;      // the total number of clients connected to the server
        Semaphore m_maxNumberAcceptedClients;

        ServerManager() { }

        public ServerManager(int numConnections, int receiveBufferSize)
        {
            m_totalBytesRead = 0;
            m_numConnectedSockets = 0;
            m_numConnections = numConnections;
            m_receiveBufferSize = receiveBufferSize;
            // allocate buffers such that the maximum number of sockets can have one outstanding read and
            //write posted to the socket simultaneously
            m_bufferManager = new BufferManager(receiveBufferSize * numConnections * opsToPreAlloc,
                receiveBufferSize);

            m_readWritePool = new SocketAsyncEventArgsPool(numConnections);
            m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);
        }

        public void Init()
        {
            // Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds
            // against memory fragmentation
            m_bufferManager.InitBuffer();

            // preallocate pool of SocketAsyncEventArgs objects
            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < m_numConnections; i++)
            {
                //Pre-allocate a set of reusable SocketAsyncEventArgs
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(Socket_IOHelper.IO_Completed);
                readWriteEventArg.UserToken = new AsyncUserToken(m_receiveBufferSize * 2, readWriteEventArg);

                // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
                m_bufferManager.SetBuffer(readWriteEventArg);

                // add SocketAsyncEventArg to the pool
                m_readWritePool.Push(readWriteEventArg);
            }
        }

        public void Start(IPEndPoint localEndPoint)
        {
            // create the socket which listens for incoming connections
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            // start the server with a listen backlog of 100 connections
            listenSocket.Listen(100);

            // post accepts on the listening socket
            StartAccept(null);

            //Console.WriteLine("{0} connected sockets with one outstanding receive posted to each....press any key", m_outstandingReadCount);
            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
        }

        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            m_maxNumberAcceptedClients.WaitOne();
            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Interlocked.Increment(ref m_numConnectedSockets);
            Console.WriteLine("Client connection accepted. There are {0} clients connected to the server",
                m_numConnectedSockets);

            // Get the socket for the accepted client connection and put it into the
            //ReadEventArg object user token
            SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();

            AsyncUserToken token = (AsyncUserToken)readEventArgs.UserToken;
            token.Socket = e.AcceptSocket;

            bool willRaiseEvent = token.asyncUserTokenRecv.ReceiveAsync();//接收连接后的第一件事就是receive
            if (!willRaiseEvent)
            {
                Socket_IOHelper.ProcessReceive(readEventArgs);
            }

            //Accept下一个连接请求
            StartAccept(e);
        }

        //// This method is called whenever a receive or send operation is completed on a socket
        ////
        //// <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
        //void IO_Completed(object sender, SocketAsyncEventArgs e)
        //{
        //    // determine which type of operation just completed and call the associated handler
        //    switch (e.LastOperation)
        //    {
        //        case SocketAsyncOperation.Receive:
        //            ProcessReceive(e);
        //            break;
        //        case SocketAsyncOperation.Send:
        //            ProcessSend(e);
        //            break;
        //        default:
        //            throw new ArgumentException("The last operation completed on the socket was not a receive or send");
        //    }
        //}
        //
        //// This method is invoked when an asynchronous receive operation completes.
        //// If the remote host closed the connection, then the socket is closed.
        //// If data was received then the data is echoed back to the client.
        ////
        //private void ProcessReceive(SocketAsyncEventArgs e)
        //{
        //    // check if the remote host closed the connection
        //    AsyncUserTokenRecv token = (AsyncUserTokenRecv)e.UserToken;
        //    if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
        //    {
        //        //increment the count of the total bytes receive by the server
        //        Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
        //        Console.WriteLine("The server has read a total of {0} bytes", m_totalBytesRead);

        //        token.BuffCopy();
        //        if (!token.Check())
        //        {
        //            bool willRaiseEvent = token.ReceiveAsync();
        //            if (!willRaiseEvent)
        //            {
        //                ProcessReceive(e);
        //            }
        //        }
        //        else
        //        {
        //            TCPTask task = new TCPTask(token.Socket, token.recvBuff);
        //            ProcessCmd(task);
        //            token.Reset();
        //            token.ReceiveAsync();//继续接收
        //        }
                

        //        ////echo the data received back to the client
        //        //e.SetBuffer(e.Offset, e.BytesTransferred);
        //        //bool willRaiseEvent = token.Socket.SendAsync(e);
        //        //if (!willRaiseEvent)
        //        //{
        //        //    ProcessSend(e);
        //        //}
        //    }
        //    else
        //    {
        //        CloseClientSocket(e);
        //    }
        //}

        //// This method is invoked when an asynchronous send operation completes.
        //// The method issues another receive on the socket to read any additional
        //// data sent from the client
        ////
        //// <param name="e"></param>
        //private void ProcessSend(SocketAsyncEventArgs e)
        //{
        //    if (e.SocketError == SocketError.Success)
        //    {
        //        // done echoing data back to the client
        //        AsyncUserToken token = (AsyncUserToken)e.UserToken;
        //        // read the next block of data send from the client
        //        bool willRaiseEvent = token.Socket.ReceiveAsync(e);
        //        if (!willRaiseEvent)
        //        {
        //            ProcessReceive(e);
        //        }
        //    }
        //    else
        //    {
        //        CloseClientSocket(e);
        //    }
        //}

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserTokenRecv token = e.UserToken as AsyncUserTokenRecv;

            // close the socket associated with the client
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception) { }
            token.Socket.Close();

            // decrement the counter keeping track of the total number of clients connected to the server
            Interlocked.Decrement(ref m_numConnectedSockets);

            // Free the SocketAsyncEventArg so they can be reused by another client
            m_readWritePool.Push(e);

            m_maxNumberAcceptedClients.Release();
            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", m_numConnectedSockets);
        }

        private static void ProcessCmd(TCPTask task)
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
