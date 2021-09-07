using System;
using System.Collections.Generic;
using System.Text;

namespace SocketServer.tool
{
    public class Singletion<T> where T : class
    {
        private static T _instance = null;
        private static object _locker = new object();

        public static T Instance()
        {
            if(null == _instance)
            {
                lock (_locker)
                {
                    if(null == _instance)
                    {
                        _instance = (T)Activator.CreateInstance(typeof(T), true);
                    }
                }
            }
            return _instance;
        }
    }
}
