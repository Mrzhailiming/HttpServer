using System;
using System.Net.Sockets;

namespace DataStruct
{
    public class AsyncUserToken
    {
        //new的时候赋值
        public Socket Socket;
        public int socketBuffLength = 0;
        public SocketAsyncEventArgs AsyncEventArgs = null;

        //设置sendbuff的时候赋值， 发送完成后需要reset
        public byte[] sendBuff = null;
        public int hadSendNum = 0;
        public int needSendNum = 0;

        //
        public bool isCompleteSend = false;

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
            Buffer.BlockCopy(sendBuff, hadSendNum, AsyncEventArgs.Buffer, 0, realSendNum);
            hadSendNum += realSendNum;

            AsyncEventArgs.SetBuffer(0, realSendNum);
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
        }
    }
    public class AsyncUserTokenRecv
    {
        //new的时候赋值
        public int socketBuffLength = 0;
        public SocketAsyncEventArgs AsyncEventArgs = null;

        public Socket Socket;//accept之后赋值

        //接收数据时赋值，一条指令接收完后需要reset
        public byte[] recvBuff = null;
        public byte[] headerBuff = null;
        public int hadRecvNum = 0;
        public int needRecvNum = 0;

        //
        public bool isRecvComplete = false;

        /// <summary>
        /// 调用Socket.ReceiveAsync
        /// </summary>
        /// <returns></returns>
        public bool ReceiveAsync()
        {
            return Socket.ReceiveAsync(AsyncEventArgs);
        }

        /// <summary>
        /// 将接收到数据的复制到recvBuff
        /// </summary>
        public void BuffCopy()
        {
            if(needRecvNum <= 0)
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
            else if(hadRecvNum < needRecvNum)
            {
                //AsyncEventArgs.BytesTransferred 大于剩余需要拷贝的字节数，说明下一条数据来了
                int realCopy = AsyncEventArgs.BytesTransferred <= (needRecvNum - hadRecvNum) ? AsyncEventArgs.BytesTransferred : (needRecvNum - hadRecvNum);
                Buffer.BlockCopy(AsyncEventArgs.Buffer, AsyncEventArgs.Offset, recvBuff, hadRecvNum, realCopy);

                hadRecvNum += realCopy;
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

}
