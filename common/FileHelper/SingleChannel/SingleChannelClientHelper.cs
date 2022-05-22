using DataStruct;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Helper
{
    /// <summary>
    /// 单channel通信
    /// </summary>
    public class SingleChannelClientHelper : IClientHelper_Interface
    {
        /// <summary>
        /// 单通
        /// </summary>
        ChannelHelper channelUpload;

        IPEndPoint localDownloadiPEndPoint;

        byte[] sendBuf;

        public SingleChannelClientHelper(IPEndPoint RemoteUploadiPEndPoint, IPEndPoint RemoteDownloadiPEndPoint, IPEndPoint LocalDownloadiPEndPoint, int buffSize)
        {
            channelUpload = new ChannelHelper(RemoteUploadiPEndPoint, SocketType.Stream, ProtocolType.Tcp, null, ProcessCmd, false, buffSize, 0, this, CompleteSendCallBack);
            localDownloadiPEndPoint = LocalDownloadiPEndPoint;
        }

        public void Start()
        {
            channelUpload.Connect(UploadconnectSuccessCallBack);
        }
        public void Send(string fileFullPath)
        {
            try
            {


                CmdBufferHelper cmdBufferHelper = new CmdBufferHelper();
                long key = DateTime.Now.Ticks;
                using (FileStream fs = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    string fileName = FileHelper.GetFileName(fileFullPath);

                    byte[] fileBuf = Encoding.Default.GetBytes(fileName);
                    int fileNameLength = fileBuf.Length;
                    int sendOffset = Offset.sendOffset;//命令头的偏移

                    BinaryReader binaryReader = new BinaryReader(fs);//用二进制流
                    int perSendFileLen = 1024 * 1024; // 每次发送500个字节
                    int curOffset = 0;
                    while (curOffset < (int)fs.Length)
                    {
                        int leave = (int)fs.Length - curOffset;
                        perSendFileLen = perSendFileLen < leave ? perSendFileLen : leave;
                        binaryReader.BaseStream.Seek(curOffset, SeekOrigin.Begin);

                        sendBuf = cmdBufferHelper.GetSendBuff((int)TCPCMDS.UPLOAD, fileName, perSendFileLen, (int)fs.Length, key);

                        binaryReader.Read(sendBuf, sendOffset + fileNameLength, sendBuf.Length - sendOffset - fileNameLength);
                        channelUpload.SetSendBuffer(sendBuf, 0, sendBuf.Length);
                        channelUpload.Send(sendBuf);

                        curOffset += perSendFileLen;

                        Thread.Sleep(1000 * 2);
                        Console.WriteLine("正在上传......");
                    }

                    binaryReader.Close();
                    binaryReader.Dispose();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}");
            }
        }

        private void ResetSendBuff()
        {
            sendBuf = null;
        }
        public void Get(string fileFullPath)
        {
            byte[] sendBuf;
            CmdBufferHelper cmdBufferHelper = new CmdBufferHelper();

            string fileName = FileHelper.GetFileName(fileFullPath);
            sendBuf = cmdBufferHelper.GetSendBuff((int)TCPCMDS.DOWNLOAD, fileName, 0);

            channelUpload.SetSendBuffer(sendBuf, 0, sendBuf.Length);
            channelUpload.BeginSend();
        }



        public void CompleteSendCallBack(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            //channelUpload.SetSendBuffer(sendBuf, 0, sendBuf.Length);
            //channelUpload.BeginSend();
            //异步接收，几十个字节的接收，递归会栈溢出，试一下循环接收
            //channelUpload.BeginRecv_new(socketAsyncEventArgs);//开始接收结果 一个socket不能同事sendasync和recvsaync
            socketAsyncEventArgs.SetBuffer(0, 1024 * 1024); // 恢复缓冲区大小
        }

        public void UploadconnectSuccessCallBack(object sender, SocketAsyncEventArgs connectAsyncEventArgs)
        {
            //do login, tell server my downloadEndPoit
            //string myEndPoint = localDownloadiPEndPoint.ToString();
            //获取channelDownload的endpoint
            string endPoint = ((AsyncUserToken)connectAsyncEventArgs.UserToken).Socket.LocalEndPoint.ToString();

            CmdBufferHelper cmdBufferHelper = new CmdBufferHelper();

            //sendBuf = cmdBufferHelper.GetSendBuff((int)TCPCMDS.LOGIN, endPoint, 0);
            //channelUpload.SetSendBuffer(sendBuf, 0, sendBuf.Length);
            //channelUpload.BeginSend();
        }

        void ProcessCmd(TCPTask task)
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
