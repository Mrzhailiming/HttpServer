using DataStruct;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helper
{
    public class CMDHelper
    {
        public static int GetCmdID(byte[] buff)
        {
            byte[] destBuf = new byte[4];
            Array.Copy(buff, Offset.cmdIDOffset, destBuf, 0, 4);
            return BitConverter.ToInt32(destBuf, 0);
        }
        public static int GetCmdFileNameLength(byte[] buff)
        {
            byte[] destBuf = new byte[4];
            Array.Copy(buff, Offset.fileNameLengthOffset, destBuf, 0, 4);
            return BitConverter.ToInt32(destBuf, 0);
        }
        public static string GetCmdFileName(byte[] buff, int fileNameLength)
        {
            byte[] destBuf = new byte[fileNameLength];
            Array.Copy(buff, Offset.fileNameOffset, destBuf, 0, fileNameLength);
            return Encoding.Default.GetString(destBuf);
        }
    }
}
