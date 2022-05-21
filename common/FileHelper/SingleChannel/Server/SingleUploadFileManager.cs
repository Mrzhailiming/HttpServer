using DataStruct;
using Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Helper
{
    public class SingleUploadFileManager
    {
        public SingleUploadFileManager()
        {
            Init();
        }

        void Init()
        {
            CMDDispatcher.Instance().RegisterCMD(TCPCMDS.UPLOAD, MyAction);
        }

        Dictionary<long, int> recvRec = new Dictionary<long, int>();

        void MyAction(TCPTask task)
        {
            byte[] buff = task.buffer;

            int cmdID = CMDHelper.GetCmdID(buff);
            int fileNameLength = CMDHelper.GetCmdFileNameLength(buff);
            int fileTotalLength = CMDHelper.GetCmdFileTotalLength(buff);
            long fileKey = CMDHelper.GetCmdFileKey(buff);
            string fileNmae = CMDHelper.GetCmdFileName(buff, fileNameLength);

            int curRecvlen = buff.Length - Offset.sendOffset - fileNameLength;
            int hadRecvLen;
            int offset = 0;
            bool createNewFile = true;
            // 上传过一次
            if (recvRec.TryGetValue(fileKey, out hadRecvLen))
            {
                // 上传完了
                if (hadRecvLen >= fileTotalLength)
                {
                    recvRec[fileKey] = curRecvlen;
                }
                // 没上传完
                else
                {
                    offset = hadRecvLen;
                    recvRec[fileKey] += curRecvlen;

                    createNewFile = false;
                }
                
            }
            // 没上传过, 记录第一次
            else if(curRecvlen < fileTotalLength)
            {
                recvRec.Add(fileKey, curRecvlen);
            }

            AsyncUserToken asyncUserToken = task.socketAsyncEventArgs.UserToken as AsyncUserToken;
            IPEndPoint remote = task.clientEndPoint as IPEndPoint;
            IPEndPoint local = asyncUserToken.Socket.LocalEndPoint as IPEndPoint;
            fileNmae = $"remote{remote.Address}_{remote.Port}_loacl{local.Address}_{local.Port}_{fileKey}_{fileNmae}";

            string filepath = string.Format(@"{0}\Socket", Environment.CurrentDirectory);
            using (FileStream fileStream = FileHelper.CreateFile(filepath, fileNmae, createNewFile))
            {
                BinaryWriter binaryWriter = new BinaryWriter(fileStream);
                binaryWriter.BaseStream.Seek(offset, SeekOrigin.Begin);

                binaryWriter.Write(buff, Offset.sendOffset + fileNameLength, buff.Length - Offset.sendOffset - fileNameLength);
                binaryWriter.Flush();
                binaryWriter.Close();
                binaryWriter.Dispose();
            }
        }
    }
}
