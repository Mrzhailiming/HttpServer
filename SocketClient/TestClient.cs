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
    class TestClient
    {
        Socket _socket = null;
        SocketAsyncEventArgs _SocketAsyncEventArgs = null;
        byte[] _buff = null;//发送缓冲区
        IPEndPoint _IPEndPoint = null;

        int m_totalBytesRead = 0;
        TestClient() { }
        public TestClient(IPEndPoint iPEndPoint, int buffSize)
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

            CMD_DS cMD_DS = new CMD_DS();

            using (FileStream fs = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                string fileName = FileHelper.GetFileName(fileFullPath);

                byte[] fileBuf = Encoding.Default.GetBytes(fileName);
                int fileNameLength = fileBuf.Length;

                sendBuf = cMD_DS.GetSendBuff((int)TCPCMDS.UPLOAD, fileName, (int)fs.Length);

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
            CMD_DS cMD_DS = new CMD_DS();

            string fileName = FileHelper.GetFileName(fileFullPath);
            sendBuf = cMD_DS.GetSendBuff((int)TCPCMDS.DOWNLOAD, fileName, 0);
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
        // This method is invoked when an asynchronous receive operation completes.
        // If the remote host closed the connection, then the socket is closed.
        // If data was received then the data is echoed back to the client.
        //
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                //increment the count of the total bytes receive by the server
                Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
                Console.WriteLine("The Client has read a total of {0} bytes", m_totalBytesRead);

                //echo the data received back to the client
                e.SetBuffer(e.Offset, e.BytesTransferred);
                bool willRaiseEvent = token.Socket.SendAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessSend(e);
                }
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
