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
    //public class CMD_DS
    //{
    //    public CMDHeader _header = new CMDHeader();

    //    public CMDBody _body = new CMDBody();
    //}
    //public class CMDHeader
    //{
    //    public int CMD_ID { get; set; }//4b
    //}

    //public class CMDBody
    //{
    //    public byte[] buffer = null;//
    //}
    class AsyncUserToken
    {
        public Socket Socket;
    }
    class ClientHelper
    {
        Socket _socket = null;
        SocketAsyncEventArgs _SocketAsyncEventArgs = null;
        byte[] _buff = null;
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
        
        private void Connect()
        {
            
            _socket.Connect(_IPEndPoint.Address, _IPEndPoint.Port);

            _SocketAsyncEventArgs.SetBuffer(_buff, 0, _buff.Length);
            _SocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            _SocketAsyncEventArgs.UserToken = new AsyncUserToken() { Socket = _socket };
        }
        private void Init(IPEndPoint iPEndPoint, int buffSize)
        {
            _SocketAsyncEventArgs = new SocketAsyncEventArgs();
            _buff = new byte[buffSize];

            _IPEndPoint = iPEndPoint;
            _socket = new Socket(iPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }
        public void Send(string fileFullPath)
        {
            byte[] sendBuf;

            CMD_DS cMD_DS = new CMD_DS();

            using (FileStream fs = new FileStream(fileFullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                string fileName = FileHelper.GetFileName(fileFullPath);

                byte[] fileBuf = Encoding.Default.GetBytes(fileName);
                int fileNameLength = fileBuf.Length;

                //sendBuf = cMD_DS._body.buffer = new byte[fs.Length + 4 + 4 + fileNameLength];//
                //SetCMD(sendBuf, 0);
                //SetFileNameLength(sendBuf, fileNameLength);
                //SetFileName(sendBuf, fileBuf);

                sendBuf = cMD_DS.GetSendBuff(0, fileName, (int)fs.Length);

                BinaryReader binaryReader = new BinaryReader(fs);//用二进制流
                binaryReader.Read(sendBuf, 8 + fileNameLength, sendBuf.Length - 8 - fileNameLength);
                binaryReader.Close();
                binaryReader.Dispose();
            }

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


        /// <summary>
        /// 设置cmd
        /// </summary>
        static void SetCMD(byte[] sendBuf, int cmdID)
        {
            byte[] src = BitConverter.GetBytes(cmdID);
            Array.Copy(src, 0, sendBuf, 0, 4);
        }
        /// <summary>
        /// 设置文件名的长度
        /// </summary>
        static void SetFileNameLength(byte[] sendBuf, int fileNameLength)
        {
            byte[] src = BitConverter.GetBytes(fileNameLength);
            Array.Copy(src, 0, sendBuf, 4, 4);
        }
        /// <summary>
        /// 设置文件名
        /// </summary>
        static void SetFileName(byte[] sendBuf, byte[] fileNameBuf)
        {
            Array.Copy(fileNameBuf, 0, sendBuf, 8, fileNameBuf.Length);
        }
       
    }
}
