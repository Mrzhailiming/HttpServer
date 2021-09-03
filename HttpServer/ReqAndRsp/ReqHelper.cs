using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

namespace server
{
    public class RequestHelper
    {
        private HttpListenerRequest request = null;
        private Stream inputStream = null;
        /// <summary>
        /// 文件输出路径
        /// </summary>
        string filepath = string.Format(@"{0}\post\", Environment.CurrentDirectory);
        public RequestHelper() { }
        public RequestHelper(HttpListenerRequest request)
        {
            this.request = request;

            inputStream = request.InputStream;
        }
        public Stream RequestStream { get; set; }

        public void ExtracHeader()
        {
            RequestStream = request.InputStream;//这个目前不知道干啥的
        }

        public delegate void ExecutingDispatch(FileStream fs);

        public bool DispatchResources(ExecutingDispatch action)
        {
            if ("GET" == request.HttpMethod)
            {
                //GET
                HTTP_Get(action);
                return true;
            }
            else
            {
                //Post
                //HTTP_Post(action);

                //HTTP_Post_GZip(action);

                HTTP_Post_TWO(action);
                return false;
            }
        }
        private void HTTP_Post_GZip(ExecutingDispatch action)
        {
            try
            {
                GZipStream gZipStream = new GZipStream(inputStream, CompressionMode.Decompress);//解压
                byte[] buf = new byte[request.ContentLength64];
                int readCount = gZipStream.Read(buf, 0, (int)request.ContentLength64);

                using (FileStream newFileS = CreateFileStream(filepath, "tmpFileName.txt"))
                {
                    newFileS.Write(buf, 0, readCount);
                }
            }
            catch(Exception ex)
            {
                LogHelper.Log(LogType.Exception, ex.ToString());
            }

        }
        /// <summary>
        /// GET
        /// </summary>
        /// <param name="action"></param>
        private void HTTP_Get(ExecutingDispatch action)
        {
            var rawUrl = request.RawUrl;//资源默认放在执行程序的root文件下，默认文档为index.html
            string filePath;
            //默认访问文件
            if ("/" == rawUrl)
            {
                filePath = string.Format(@"{0}\root\index.html", Environment.CurrentDirectory);
            }
            //访问其他文件
            else
            {
                string[] parts = DealRawUrl(request.RawUrl);
                filePath = BuildFullPath(parts);
            }

            try
            {
                if (File.Exists(filePath))
                {
                    FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);

                    action?.Invoke(fs);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(LogType.Exception, ex.ToString());
            }
        }
        /// <summary>
        /// POST 
        /// </summary>
        /// <param name="action"></param>
        private void HTTP_Post_TWO(ExecutingDispatch action)
        {
            if (!request.HasEntityBody)
            {
                IPEndPoint remote = request.RemoteEndPoint;
                LogHelper.Log(LogType.PostNoBody, $"{remote.Address.ToString()}:{remote.Port.ToString()}");
            }
            try
            {
                StreamReader read = new StreamReader(request.InputStream, Encoding.UTF8);
                string ret = read.ReadToEnd();//made ret的长度和流的长度不一样
                WriteToFile(ret);

                //byte[] buff = new byte[request.ContentLength64];
                //request.InputStream.Read(buff, 0, (int)request.ContentLength64);
                //using (FileStream newFileS = CreateFileStream(filepath, "nice.jpg"))
                //{
                //    newFileS.Write(buff, 0, buff.Length);
                //}

            }
            catch (Exception ex)
            {
                LogHelper.Log(LogType.Exception, ex.ToString());
            }
        }
        /// <summary>
        /// POST
        /// </summary>
        /// <param name="action"></param>
        private void HTTP_Post(ExecutingDispatch action)
        {
            if (!request.HasEntityBody)
            {
                IPEndPoint remote = request.RemoteEndPoint;
                LogHelper.Log(LogType.PostNoBody, $"{remote.Address.ToString()}:{remote.Port.ToString()}");
            }

            inputStream = request.InputStream;

            //string fileName = $"HTTP_Post_{DateTime.Now.Ticks.ToString()}.{request.ContentType}";

            StreamReader reader = new StreamReader(inputStream, Encoding.UTF8);
            string msg = reader.ReadToEnd();
            //1.找到文件名字
            string fileName = FindFileName(msg);
            if (null == fileName || "" == fileName)
            {
                LogHelper.Log(LogType.Exception, "HTTP_Post()_filename == \"\"");
                return;
            }

            byte[] buff = new byte[1024];
            FileStream writeStream = CreateFileStream(filepath, fileName);
            FileObject fileObject = new FileObject() { fs = writeStream, buffer = buff };

            inputStream.BeginRead(buff, 0, buff.Length, new AsyncCallback(EndRead), fileObject);
        }

        /// <summary>
        /// 异步读取的回调
        /// </summary>
        /// <param name="ar"></param>
        private void EndRead(IAsyncResult ar)
        {
            FileObject obj = ar.AsyncState as FileObject;
            int num = inputStream.EndRead(ar);

            //写入文件
            obj.fs.Write(obj.buffer, 0, num);
            if (num < 1)
            {
                obj.fs.Close(); //关闭文件流　　　　　　　　　　
                inputStream.Close();//关闭输入流　　　　
                return;
            }
            //循环读取
            inputStream.BeginRead(obj.buffer, 0, obj.buffer.Length, new AsyncCallback(EndRead), obj);
        }

        #region querys

        public void ResponseQuerys()
        {
            var querys = request.QueryString;
            foreach (string key in querys.AllKeys)
            {
                VarityQuerys(key, querys[key]);
            }
        }

        private void VarityQuerys(string key, string value)
        {
            switch (key)
            {
                case "pic": Pictures(value); break;
                case "text": Texts(value); break;
                default: Defaults(value); break;
            }
        }

        private void Pictures(string id)
        {

        }

        private void Texts(string id)
        {

        }

        private void Defaults(string id)
        {

        }
        #endregion querys

        /// <summary>
        /// 处理rawURL
        /// </summary>
        /// <param name="rawUrl"></param>
        /// <returns></returns>
        private string[] DealRawUrl(string rawUrl)
        {
            string[] ret = rawUrl.Split('/');
            return ret;
        }

        /// <summary>
        /// 根据处理后的url创建路径
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        private string BuildFullPath(string[] parts)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Environment.CurrentDirectory).Append("\\root");
            foreach (string part in parts)
            {
                if ("" == part)
                {
                    continue;
                }
                builder.Append('\\').Append(part);
            }
            return builder.ToString();
        }

        /// <summary>
        /// 创建文件流
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private FileStream CreateFileStream(string filepath, string name)
        {
            FileStream ret = null;
            try
            {
                if (!Directory.Exists(filepath))
                {
                    Directory.CreateDirectory(filepath);
                }
                string fileFullPath = $"{filepath}\\{name}";
                if (File.Exists(fileFullPath))
                {
                    fileFullPath = $"{filepath}\\new_{DateTime.Now.Ticks}_{name}";
                }
                ret = new FileStream(fileFullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            }
            catch (Exception ex)
            {
                LogHelper.Log(LogType.Exception, ex.ToString());
            }
            return ret;
        }
        /// <summary>
        /// 写入文件
        /// </summary>
        private void WriteToFile_TWO(string ret)
        {

        }
        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="ret"></param>
        private void WriteToFile(string ret)
        {
            //1.找到文件名字
            string fileName = FindFileName(ret);
            if (null == fileName || "" == fileName)
            {
                LogHelper.Log(LogType.Error_FileNameNULL, "filename == \"\"");
                fileName = $"{DateTime.Now.Ticks}_test.jpg";
            }

            //2./r/n/r/n
            int startIndex = FindStartIndex(ret);
            //length
            int endIndex = FindEndIndex(ret);
            //检查index的合法性
            //string body = ret.Substring(startIndex + 4, endIndex - startIndex - 4);

            FileStream fileStream = CreateFileStream(filepath, fileName);

            //第一种方法
            byte[] byteArray = System.Text.Encoding.Default.GetBytes(ret);//全部输出
            fileStream.Write(byteArray, 0, byteArray.Length);

            ////第二种
            //byte[] buf = new byte[request.ContentLength64];
            //StreamReader streamReader = new StreamReader(inputStream);
            //int result = inputStream.Read(buf, 0, (int)request.ContentLength64);
            //fileStream.Write(buf, 0, result);

            fileStream.Flush();
            fileStream.Close();
        }

        public void Test()
        {

            //string ret = "-----------------------------7e520c38503f6\r\nContent-Disposition: form-data; name=\"image\"; filename=\"test.txt\"\r\nContent-Type: text/plain\r\n\r\n123456\r\n-----------------------------7e520c38503f6--\r\n";
            //WriteToFile(ret);

            //FileStream fileStream = new FileStream(@"F:\Code\test\httpServer\server\bin\Debug\netcoreapp2.1\root\test.jpg", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            //byte[] buff = new byte[1024];
            //FileStream newFs = new FileStream(@"F:\Code\test\httpServer\server\bin\Debug\netcoreapp2.1\root\test_copy.jpg", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            //FileObject obj = new FileObject() { buffer = buff, fs = newFs, OldFs = fileStream };
            //fileStream.BeginRead(buff, 0, buff.Length, new AsyncCallback(TestCall), obj);
        }
        void TestCall(IAsyncResult ar)
        {
            FileObject obj = ar.AsyncState as FileObject;
            FileStream oldFileStream = obj.OldFs;
            int num = oldFileStream.EndRead(ar);

            //写入文件
            obj.fs.Write(obj.buffer, 0, num);
            if (num < 1)
            {
                obj.fs.Close(); //关闭文件流　　　　　　　　　　
                oldFileStream.Close();//关闭输入流　　　　
                return;
            }
            //循环读取
            oldFileStream.BeginRead(obj.buffer, 0, obj.buffer.Length, new AsyncCallback(TestCall), obj);
        }

        private string FindFileName(string ret)
        {
            int index = ret.IndexOf("filename");
            if (index < 0)
            {
                return "";//暂时返回""
            }
            index = ret.IndexOf('\"', index);
            if (index < 0)
            {
                return "";
            }

            StringBuilder builder = new StringBuilder();
            while (ret[++index] != '\"')
            {
                builder.Append(ret[index]);
            }
            return builder.ToString();
        }

        private int FindStartIndex(string ret)
        {
            return ret.IndexOf(TargetStr.FindStartString);
        }

        private int FindEndIndex(string ret)
        {
            return ret.LastIndexOf(TargetStr.FindEndString) - 2;
        }
    }

    public class TargetStr
    {
        public static string FindStartString = "\r\n\r\n";
        public static string FindEndString = "-----------------------------";
    }
}
