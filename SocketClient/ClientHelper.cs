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
            _SocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(Socket_IOHelper.IO_Completed);
            _SocketAsyncEventArgs.UserToken = new AsyncUserToken(_buff.Length, _SocketAsyncEventArgs, _socket);

            ((AsyncUserToken)_SocketAsyncEventArgs.UserToken).asyncUserTokenRecv.ReceiveAsync();//绑定好久接收
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
                Socket_IOHelper.ProcessSend(_SocketAsyncEventArgs);
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
    }
}
