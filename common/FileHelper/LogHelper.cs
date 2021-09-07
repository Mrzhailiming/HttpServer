using System;
using System.Collections.Generic;
using System.Text;

namespace Helper
{
    public enum LogType
    {
        SUCCESS,
        Exception,
        PostNoBody,
        Error_FileNameNULL,
        Error_BuffFull,
        Error_CMDRepeat,
        Error_CMDIsNull,
        Error_ConnectionReset,
        Msg_ProcessCmd,
        Exception_ProcessCmd,

        Error_ProcessSend,
    }
    public class LogHelper
    {
        public static LogHelper Instance = new LogHelper();
        public static void Log(LogType logType, string msg)
        {
            Console.WriteLine($"{DateTime.Now.ToString()}_{logType}_{msg}");
        }
    }
}
