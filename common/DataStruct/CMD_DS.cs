using System;
using System.Collections.Generic;
using System.Text;

namespace DataStruct
{
    public class Offset
    {
        public const int cmdIDOffset = 0;
        public const int cmdLengthOffset = 4;
        public const int fileNameLengthOffset = 8;
        public const int fileNameOffset = 12;
        /// <summary>
        /// 文件名的偏移
        /// </summary>
        public const int sendOffset = 12;
    }
    public class CMD_DS
    {
        public CMDHeader _header = new CMDHeader();

        public CMDBody _body = new CMDBody();

        

        /// <summary>
        /// 设置命令头
        /// </summary>
        /// <param name="cmd">CmdID</param>
        /// <param name="fileName">filename</param>
        /// <param name="fileLength">filelength</param>
        /// <returns></returns>
        public byte[] GetSendBuff(int cmd, string fileName, int fileLength)
        {
            _header.CMD_ID = cmd;

            byte[] fileBuf = Encoding.Default.GetBytes(fileName);
            int fileNameLength = fileBuf.Length;

            _header.cmdLength = fileLength + 4 + 4 + 4 + fileNameLength;//cmdID length fileNameLength body

            _body.buffer = new byte[_header.cmdLength];//最大开1339031595字节 = 1.3G

            SetCMD();
            SetCMDLength();
            SetFileNameLength(fileNameLength);
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
