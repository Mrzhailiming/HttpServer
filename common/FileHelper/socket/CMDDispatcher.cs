using DataStruct;
using Helper;
using SocketServer.tool;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helper
{
    public class CMDHandler
    {
        public Action<TCPTask> _action = null;

    }
    public class CMDDispatcher : Singletion<CMDDispatcher>
    {
        Dictionary<TCPCMDS, CMDHandler> _CMD2Action = new Dictionary<TCPCMDS, CMDHandler>();
        public void RegisterCMD(TCPCMDS cmdID, Action<TCPTask> action)
        {
            CMDHandler newHandler = new CMDHandler()
            {
                _action = action
            };
            CMDHandler outHandler;
            if(_CMD2Action.TryGetValue(cmdID, out outHandler))
            {
                LogHelper.Log(LogType.Error_CMDRepeat, cmdID.ToString());
                throw new Exception("cmd重复");
            }
            else
            {
                _CMD2Action[cmdID] = newHandler;
            }
        }

        public void Dispatcher(TCPTask task)
        {
            TCPCMDS cmdID = (TCPCMDS)Byte4Int(task.buffer);
            CMDHandler outHandler;
            if (!_CMD2Action.TryGetValue(cmdID, out outHandler))
            {
                LogHelper.Log(LogType.Error_CMDIsNull, cmdID.ToString());
            }
            else
            {
                LogHelper.Log(LogType.Msg_ProcessCmd, cmdID.ToString());
                
                outHandler._action(task);
            }
        }


        //4位byte转为int
        private static int Byte4Int(byte[] buf)
        {
            return ((buf[0] & 0xff) << 24) | ((buf[1] & 0xff) << 16) | ((buf[2] & 0xff) << 8) | (buf[3] & 0xff);
        }
    }
}
