using Helper;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace SocketServer
{
    public class SocketAsyncEventArgsPool
    {
        Stack<SocketAsyncEventArgs> _listEvent = new Stack<SocketAsyncEventArgs>();
        public SocketAsyncEventArgsPool(int maxNum)
        {
            for(int i = 0; i < maxNum; ++i)
            {
                _listEvent.Push(new SocketAsyncEventArgs());
            }
        }
        public bool Push(SocketAsyncEventArgs sae)
        {
            _listEvent.Push(sae);
            return true;
        }
        public SocketAsyncEventArgs Pop()
        {
            try
            {
                return _listEvent.Pop();
            }
            catch(Exception ex)
            {
                LogHelper.Log(LogType.Exception, ex.ToString());
            }

            return new SocketAsyncEventArgs();
        }
    }
}
