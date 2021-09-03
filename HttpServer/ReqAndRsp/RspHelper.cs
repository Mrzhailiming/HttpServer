using System;
using System.IO;
using System.Net;

namespace server
{
    public class FileObject
    {
        public FileStream fs;
        public FileStream OldFs = null;
        public byte[] buffer;
    }

    public class ResponseHelper
    {
        private HttpListenerResponse response;
        public ResponseHelper(HttpListenerResponse response)
        {
            this.response = response;
            OutputStream = response.OutputStream;

        }
        public Stream OutputStream { get; set; }

        public void WriteToClient(FileStream fs)
        {
            byte[] buffer = new byte[1024];
            FileObject obj = new FileObject() { fs = fs, buffer = buffer };
            fs.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(EndWrite), obj);
        }
        void EndWrite(IAsyncResult ar)
        {
            var obj = ar.AsyncState as FileObject;
            var num = obj.fs.EndRead(ar);

            //写入rsp
            OutputStream.Write(obj.buffer, 0, num);
            if (num < 1)
            {
                response.StatusCode = 200;//result
                obj.fs.Close(); //关闭文件流　　　　　　　　　　
                OutputStream.Close();//关闭输出流，如果不关闭，浏览器将一直在等待状态 　　　　　　　　　　
                return;
            }
            //循环读取
            obj.fs.BeginRead(obj.buffer, 0, obj.buffer.Length, new AsyncCallback(EndWrite), obj);
        }
    }
}
