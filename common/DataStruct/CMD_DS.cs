using System;
using System.Collections.Generic;
using System.Text;

namespace DataStruct
{
    public class CMD_DS
    {
        public CMDHeader _header = new CMDHeader();

        public CMDBody _body = new CMDBody();

        public byte[] GetSendBuff(int cmd, string fileName, int fileLength)
        {
            _header.CMD_ID = cmd;

            byte[] fileBuf = Encoding.Default.GetBytes(fileName);
            int fileNameLength = fileBuf.Length;

           _body.buffer = new byte[fileLength + 4 + 4 + fileNameLength];//

            SetCMD();
            SetFileName(fileBuf);
            SetFileNameLength(fileNameLength);

            return _body.buffer;
        }

        /// <summary>
        /// 设置cmd
        /// </summary>
        void SetCMD()
        {
            byte[] src = BitConverter.GetBytes(_header.CMD_ID);
            Array.Copy(src, 0, _body.buffer, 0, 4);
        }
        /// <summary>
        /// 设置文件名的长度
        /// </summary>
        void SetFileNameLength(int fileNameLength)
        {
            byte[] src = BitConverter.GetBytes(fileNameLength);
            Array.Copy(src, 0, _body.buffer, 4, 4);
        }
        /// <summary>
        /// 设置文件名
        /// </summary>
        void SetFileName(byte[] fileNameBuf)
        {
            Array.Copy(fileNameBuf, 0, _body.buffer, 8, fileNameBuf.Length);
        }
    }

    public class CMDHeader
    {
        public int CMD_ID { get; set; }//4b
    }

    public class CMDBody
    {
        public byte[] buffer = null;//
    }
}
