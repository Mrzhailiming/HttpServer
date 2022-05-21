using DataStruct;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helper
{
    public class SingleClientLoginManager
    {
        public SingleClientLoginManager()
        {
            Init();
        }

        private void Init()
        {
            CMDDispatcher.Instance().RegisterCMD(TCPCMDS.LOGIN, MyAction);
        }

        /// <summary>
        /// 接收login结果
        /// </summary>
        /// <param name="task"></param>
        void MyAction(TCPTask task)
        {
            byte[] buff = task.buffer;

            int fileNameLength = CMDHelper.GetCmdFileNameLength(buff);
            string result = CMDHelper.GetCmdFileName(buff, fileNameLength);

            if("0" == result)
            {
                LogHelper.Log(LogType.SUCCESS, "__LoginSuccess__");
            }
        }
    }
}
