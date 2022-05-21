using System;
using System.Collections.Generic;
using System.Text;

namespace DataStruct
{
    
    /// <summary>
    /// 设置命令头部
    /// </summary>
    public class CmdBufferHelper
    {
        public CMDHeader _header = new CMDHeader();

        public CMDBody _body = new CMDBody();

        

        /// <summary>
        /// 设置命令头
        /// </summary>
        /// <param name="cmd">命令ID</param>
        /// <param name="fileName">文件名</param>
        /// <param name="CurfileLength">本次发送的文件长度</param>
        /// <returns></returns>
        public byte[] GetSendBuff(int cmd, string fileName, int CurfileLength, int fileTotalLen = 0, long filekey = 0)
        {
            _header.CMD_ID = cmd;

            byte[] fileBuf = Encoding.Default.GetBytes(fileName);
            int fileNameLength = fileBuf.Length;

            _header.cmdLength = CurfileLength + Offset.sendOffset + fileNameLength;//cmdID length fileNameLength body filetotallenght

            _body.buffer = new byte[_header.cmdLength];//最大开1339031595字节 = 1.3G

            SetCMD();
            SetCMDLength();
            SetFileNameLength(fileNameLength);
            SetFileTotalLength(fileTotalLen);
            SetFileKey(filekey);
            SetFileName(fileBuf);

            return _body.buffer;
        }

        /// <summary>
        /// 设置cmd
        /// </summary>
        void SetCMD()
        {
            byte[] src = BitConverter.GetBytes(_header.CMD_ID);
            Array.Copy(src, 0, _body.buffer, Offset.cmdIDOffset, 4);
        }
        /// <summary>
        /// 设置命令包长度
        /// </summary>
        void SetCMDLength()
        {
            byte[] src = BitConverter.GetBytes(_header.cmdLength);
            Array.Copy(src, 0, _body.buffer, Offset.cmdLengthOffset, 4);
        }
        /// <summary>
        /// 设置文件名的长度
        /// </summary>
        void SetFileNameLength(int fileNameLength)
        {
            byte[] src = BitConverter.GetBytes(fileNameLength);
            Array.Copy(src, 0, _body.buffer, Offset.fileNameLengthOffset, 4);
        }
        /// <summary>
        /// 设置文件长度
        /// </summary>
        void SetFileTotalLength(int fileLength)
        {
            byte[] src = BitConverter.GetBytes(fileLength);
            Array.Copy(src, 0, _body.buffer, Offset.fileTotalLengthOffset, 4);
        }
        /// <summary>
        /// 设置文件唯一标识
        /// </summary>
        void SetFileKey(long filekey)
        {
            byte[] src = BitConverter.GetBytes(filekey);
            Array.Copy(src, 0, _body.buffer, Offset.fileKeyOffset, 8);
        }
        /// <summary>
        /// 设置文件名
        /// </summary>
        void SetFileName(byte[] fileNameBuf)
        {
            Array.Copy(fileNameBuf, 0, _body.buffer, Offset.fileNameOffset, fileNameBuf.Length);
        }
    }

    public class CMDHeader
    {
        public int CMD_ID { get; set; }//4b
        public int cmdLength { get; set; }//4b
    }

    public class CMDBody
    {
        public byte[] buffer = null;//
    }
}
