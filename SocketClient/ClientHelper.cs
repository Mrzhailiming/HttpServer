using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketClient
{
    class AsyncUserToken
    {
        public Socket Socket;
    }
    class ClientHelper
    {
        Socket _socket = null;
        SocketAsyncEventArgs _SocketAsyncEventArgs = null;
        byte[] _buff = null;
        ClientHelper() { }
        public ClientHelper(IPEndPoint iPEndPoint, int buffSize) 
        {
            try
            {
                Init(buffSize);

                _socket = new Socket(iPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(iPEndPoint.Address, iPEndPoint.Port);

                _SocketAsyncEventArgs.SetBuffer(_buff, 0, buffSize);
                _SocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                _SocketAsyncEventArgs.UserToken = new AsyncUserToken() { Socket = _socket };
            }
            catch (Exception ex)
            {
                LogHelper.Log(LogType.Exception, ex.ToString());
            }
        }

        private void Init(int buffSize)
        {
            _SocketAsyncEventArgs = new SocketAsyncEventArgs();
            _buff = new byte[buffSize];
        }
        public void Send(string msg)
        {
            byte[] sendBuf = System.Text.Encoding.Default.GetBytes(msg);

            Buffer.BlockCopy(sendBuf, 0, _buff, 0, sendBuf.Length);

            _SocketAsyncEventArgs.SetBuffer(0, sendBuf.Length);

            bool willRaiseEvent = _socket.SendAsync(_SocketAsyncEventArgs);


            if (!willRaiseEvent)
            {
                ProcessSend(_SocketAsyncEventArgs);
            }
        }
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
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

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                //先不处理服务器回的
                //e.SetBuffer(e.Offset, e.BytesTransferred);
                //bool willRaiseEvent = token.Socket.SendAsync(e);
                //if (!willRaiseEvent)
                //{
                //    ProcessSend(e);
                //}
            }
            else
            {
                //CloseClientSocket(e);
            }
        }

        // This method is invoked when an asynchronous send operation completes.
        // The method issues another receive on the socket to read any additional
        // data sent from the client
        //
        // <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // done echoing data back to the client
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                // read the next block of data send from the client
                bool willRaiseEvent = token.Socket.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                //CloseClientSocket(e);
            }
        }
    }
}
