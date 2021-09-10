using DataStruct;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helper
{
    public class Global
    {
        /// <summary>
        /// ip:port 作为download通道的寻找client的key，但是task存储的是upload通道的ip:port
        /// </summary>
        public static Dictionary<string, Client> _clientDic = new Dictionary<string, Client>();

        public static Dictionary<string, string> _upIP2dowmIP = new Dictionary<string, string>();

        public static Client FindClient(string upLoadIP)
        {
            string downLoadIP;
            Client client;
            if (!_upIP2dowmIP.TryGetValue(upLoadIP, out downLoadIP))
            {
                return null;
            }

            if(!_clientDic.TryGetValue(downLoadIP, out client))
            {
                return null;
            }
            return client;
        }
    }
}
