using server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace New_MyHttpServer
{
    class ClientHelper
    {
        HttpClient httpClient = new HttpClient();

        public void UploadImage(string uploadUrl, string imgPath, string fileparameter = "file")
        {
            try
            {
                //创建连接
                HttpWebRequest request = WebRequest.Create(uploadUrl) as HttpWebRequest;
                request.AllowAutoRedirect = true;
                request.Method = "POST";

                string boundary = DateTime.Now.Ticks.ToString("X"); // 随机分隔线
                request.ContentType = "multipart/form-data;charset=utf-8;boundary=" + boundary;
                byte[] itemBoundaryBytes = Encoding.UTF8.GetBytes("\r\n--" + boundary + "\r\n");
                byte[] endBoundaryBytes = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");

                int pos = imgPath.LastIndexOf('\\');
                string fileName = imgPath.Substring(pos + 1);

                //请求头部信息
                StringBuilder sbHeader = new StringBuilder(string.Format("Content-Disposition:form-data;name=\"" + fileparameter + "\";filename=\"{0}\"\r\nContent-Type:application/octet-stream\r\n\r\n", fileName));
                byte[] postHeaderBytes = Encoding.UTF8.GetBytes(sbHeader.ToString());

                FileStream fs = new FileStream(imgPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                byte[] bArr = new byte[fs.Length];
                fs.Read(bArr, 0, bArr.Length);
                fs.Close();

                using (FileStream newFileStre = new FileStream($"{imgPath}_tick_{DateTime.Now.Ticks}.jpg", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    newFileStre.Write(bArr, 0, bArr.Length);
                }


                //获取post的流
                Stream postStream = request.GetRequestStream();
                //postStream.Write(itemBoundaryBytes, 0, itemBoundaryBytes.Length);
                //postStream.Write(postHeaderBytes, 0, postHeaderBytes.Length);
                postStream.Write(bArr, 0, bArr.Length);//写文件内容
                //postStream.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);
                postStream.Close();

                //获取rsp的流
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;//使用异步
                Stream instream = response.GetResponseStream();
                StreamReader sr = new StreamReader(instream, Encoding.UTF8);
                string content = sr.ReadToEnd();

            }
            catch (Exception ex)
            {
                //跨项目引用loghelper
                LogHelper.Log(LogType.Exception, ex.ToString());
            }
            
        }
    }
}
