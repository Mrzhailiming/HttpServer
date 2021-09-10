using DataStruct;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helper
{
    public class LoginManager
    {
        public LoginManager()
        {
            Init();
        }

        private void Init()
        {
            CMDDispatcher.Instance().RegisterCMD(TCPCMDS.LOGIN, MyAction);
        }

        void MyAction(TCPTask task)
        {
            byte[] buff = task.buffer;

            int fileNameLength = CMDHelper.GetCmdFileNameLength(buff);
            string IPPORT = CMDHelper.GetCmdFileName(buff, fileNameLength);

            Global._upIP2dowmIP[task.clientEndPoint.ToString()] = IPPORT;//添加客户端downloadport
        }
    }
}
