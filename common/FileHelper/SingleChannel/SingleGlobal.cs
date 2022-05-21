using DataStruct;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helper
{
    /// <summary>
    /// 双channel通信
    /// </summary>
    public class SingleGlobal
    {
        /// <summary>
        /// ip:port 作为download通道的寻找client的key，但是task存储的是upload通道的ip:port
        /// </summary>
        public static Dictionary<string, Client> _clientDic = new Dictionary<string, Client>();

        public static Client FindClient(string ClientIP)
        {
            Client client;

            if(!_clientDic.TryGetValue(ClientIP, out client))
            {
                return null;
            }
            return client;
        }
    }
}
