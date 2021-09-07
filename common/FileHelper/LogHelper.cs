﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Helper
{
    public enum LogType
    {
        Exception,
        PostNoBody,
        Error_FileNameNULL,
        Error_BuffFull,
        Error_CMDRepeat,
        Error_CMDIsNull,
        Msg_ProcessCmd,
        Exception_ProcessCmd
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