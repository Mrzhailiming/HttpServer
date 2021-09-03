﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SocketServer
{
    public enum LogType
    {
        Exception,
        PostNoBody,
        Error_FileNameNULL,
        Error_BuffFull,
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