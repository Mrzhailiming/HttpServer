using Helper;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Helper
{
    public class BufferManager
    {
        byte[] _buffer = null;
        int _perBufferSize = 0;
        int _totalByteCount = 0;

        int _usedByteCount = 0;
        BufferManager() { }
        public BufferManager(int byteCount, int perBufferSize)
        {
            _totalByteCount = byteCount;
            _perBufferSize = perBufferSize;
        }

        public void InitBuffer()
        {
            _buffer = new byte[_totalByteCount];
        }

        /// <summary>
        /// 设置读写缓冲区
        /// </summary>
        /// <param name="socketAsyncEventArgs"></param>
        public void SetBuffer(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            if((_usedByteCount + _perBufferSize * 2) <= _totalByteCount)
            {
                socketAsyncEventArgs.SetBuffer(_buffer, _usedByteCount, _perBufferSize * 2);
                _usedByteCount = _usedByteCount + _perBufferSize * 2;
            }
            else
            {
                LogHelper.Log(LogType.Error_BuffFull, "buff用完了");
            }
        }
    }
}
