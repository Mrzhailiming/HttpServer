using System;
using System.Net.Sockets;

namespace DataStruct
{
    public class AsyncUserToken
    {
        public string exeName;
        public Socket Socket = null;
        int socketBuffLength = 0;
        public SocketAsyncEventArgs AsyncEventArgs = null;
        public AsyncUserTokenSend asyncUserTokenSend = null;
        public AsyncUserTokenRecv asyncUserTokenRecv = null;
        AsyncUserToken() { }
        
        /// <summary>
        /// token
        /// </summary>
        /// <param name="socketBuffLen"></param>
        /// <param name="asyncEventArgs"></param>
        /// <param name="completeSendTORecv">发送完毕是否转换为接收</param>
        /// <param name="ContinueRecv">接收完毕是否继续接收</param>
        /// <param name="socket"></param>
        public AsyncUserToken(int socketBuffLen, SocketAsyncEventArgs asyncEventArgs, bool completeSendTORecv, bool ContinueRecv, Socket socket = null) 
        {
            Socket = socket;
            socketBuffLength = socketBuffLen;
            AsyncEventArgs = asyncEventArgs;
            asyncUserTokenSend = new AsyncUserTokenSend(socket, socketBuffLen, asyncEventArgs, completeSendTORecv);
            asyncUserTokenRecv = new AsyncUserTokenRecv(socketBuffLen, asyncEventArgs, ContinueRecv, socket);
        }
        public bool IsContinueRecv()
        {
            return asyncUserTokenRecv.isContinueRecv;
        }
        public bool IsCompleteSendTORecv()
        {
            return asyncUserTokenSend.isCompleteSendTORecv;
        }

        public void SetSocket(Socket socket)
        {
            this.Socket = socket;
            this.asyncUserTokenSend.Socket = socket;
            this.asyncUserTokenRecv.Socket = socket;
        }
    }


    public class AsyncUserTokenSend
    {
        //new的时候赋值
        public Socket Socket;
        public int socketBuffLength = 0;
        public SocketAsyncEventArgs AsyncEventArgs = null;
        public bool isCompleteSendTORecv = false;//send完毕是否转为recv, 这两个标志位同时只能有一个为true

        //设置sendbuff的时候赋值， 发送完成后需要reset
        public byte[] sendBuff = null;
        public int hadSendNum = 0;
        public int needSendNum = 0;

        //
        public bool isCompleteSend = false;

        AsyncUserTokenSend() { }
        public AsyncUserTokenSend(Socket socket, int socketBuffLen, SocketAsyncEventArgs asyncEventArgs, bool completeSendTORecv)
        {
            Socket = socket;
            socketBuffLength = socketBuffLen;
            AsyncEventArgs = asyncEventArgs;
            isCompleteSendTORecv = completeSendTORecv;
        }
        /// <summary>
        /// 设置总共需要发送的buffer
        /// </summary>
        /// <param name="buf"></param>
        public void SetTotalSendBuff(byte[] buf)
        {
            sendBuff = buf;
            hadSendNum = 0;
            needSendNum = buf.Length;
            isCompleteSend = false;
        }

        /// <summary>
        /// 设置本次发送的buffer
        /// </summary>
        /// <param name="sendLength"></param>
        public void SetBuffer(int sendLength)
        {
            int realSendNum = sendLength <= socketBuffLength ? sendLength : socketBuffLength;
            Buffer.BlockCopy(sendBuff, hadSendNum, AsyncEventArgs.Buffer, AsyncEventArgs.Offset, realSendNum);
            hadSendNum += realSendNum;

            AsyncEventArgs.SetBuffer(AsyncEventArgs.Offset, realSendNum);
        }

        /// <summary>
        /// 调用socket的异步发送接口
        /// </summary>
        /// <returns></returns>
        public bool SendAsync()
        {
            return Socket.SendAsync(AsyncEventArgs);
        }
        public bool IsSendComplete()
        {
            if (hadSendNum == needSendNum)
            {
                isCompleteSend = true;
                return true;//发送完毕
            }
            else
            {
                return false;
            }
        }

        public void Reset()
        {
            sendBuff = null;
            hadSendNum = 0;
            needSendNum = 0;
            isCompleteSend = false;
        }
    }
    public class AsyncUserTokenRecv
    {
        //new的时候赋值
        public int socketBuffLength = 0;
        public SocketAsyncEventArgs AsyncEventArgs = null;
        public bool isContinueRecv = false;//接收完毕是否继续接收，这两个标志位同时只能有一个为true

        /// <summary>
        /// server端accept之后赋值
        /// </summary>
        public Socket Socket;

        //接收数据时赋值，一条指令接收完后需要reset
        public byte[] recvBuff = null;
        public byte[] headerBuff = null;
        public int hadRecvNum = 0;
        public int needRecvNum = 0;

        //
        public bool isRecvComplete = false;
        AsyncUserTokenRecv() { }
        public AsyncUserTokenRecv(int socketBuffLen, SocketAsyncEventArgs asyncEventArgs, bool ContinueRecv, Socket socket = null)
        {
            socketBuffLength = socketBuffLen;
            AsyncEventArgs = asyncEventArgs;
            Socket = socket;
            isContinueRecv = ContinueRecv;
        }
        /// <summary>
        /// 调用Socket.ReceiveAsync
        /// </summary>
        /// <returns></returns>
        public bool ReceiveAsync()
        {
            try
            {
                return Socket.ReceiveAsync(AsyncEventArgs);
            }
            catch(Exception ex)
            {
                Console.WriteLine("ReceiveAsync异常：{0}", ex.ToString());
            }
            return true;
        }

        /// <summary>
        /// 将接收到数据的复制到recvBuff
        /// </summary>
        public void BuffCopy()
        {
            try
            {
                if (needRecvNum <= 0)
                {
                    
                    headerBuff = new byte[AsyncEventArgs.BytesTransferred];

                    Buffer.BlockCopy(AsyncEventArgs.Buffer, AsyncEventArgs.Offset, headerBuff, 0, AsyncEventArgs.BytesTransferred);
                    hadRecvNum += AsyncEventArgs.BytesTransferred;
                    if (hadRecvNum >= 8)
                    {
                        byte[] needRecvNumBuff = new byte[4];
                        Buffer.BlockCopy(headerBuff, 4, needRecvNumBuff, 0, 4);//
                        needRecvNum = BitConverter.ToInt32(needRecvNumBuff);//获取命令包长度
                        recvBuff = new byte[needRecvNum];
                        Buffer.BlockCopy(headerBuff, 0, recvBuff, 0, headerBuff.Length);
                    }
                }
                else if (hadRecvNum < needRecvNum)
                {
                    //AsyncEventArgs.BytesTransferred 大于剩余需要拷贝的字节数，说明下一条数据来了
                    int realCopy = AsyncEventArgs.BytesTransferred <= (needRecvNum - hadRecvNum) ? AsyncEventArgs.BytesTransferred : (needRecvNum - hadRecvNum);
                    Buffer.BlockCopy(AsyncEventArgs.Buffer, AsyncEventArgs.Offset, recvBuff, hadRecvNum, realCopy);

                    hadRecvNum += realCopy;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("BuffCopy异常{0}：", ex.ToString());
            }
        }
        public bool Check()
        {
            if(hadRecvNum == needRecvNum)
            {
                isRecvComplete = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Reset()
        {
            recvBuff = null;
            headerBuff = null;
            hadRecvNum = 0;
            needRecvNum = 0;
            isRecvComplete = false;
        }
    }



    public class TCPTask
    {
        public Socket clientSocket;
        public byte[] buffer;
        public SocketAsyncEventArgs socketAsyncEventArgs;
        TCPTask() { }

        public TCPTask(Socket socket, byte[] buf, SocketAsyncEventArgs AsyncEventArgs)
        {
            clientSocket = socket;
            buffer = buf;
            socketAsyncEventArgs = AsyncEventArgs;
        }
    }
}
