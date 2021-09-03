using System;
using System.Net;

namespace server
{
    public class ServerHelper
    {
        HttpListener httpListener = new HttpListener();
        
        public void Setup(int port = 8080)
        {
            httpListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            httpListener.Prefixes.Add(string.Format("http://*:{0}/", port));//如果发送到8080 端口没有被处理，则这里全部受理，+是全部接收
            httpListener.Start();//开启服务

            Receive();//异步接收请求
        }

        private void Receive()
        {
            httpListener.BeginGetContext(new AsyncCallback(EndReceive), null);
        }

        void EndReceive(IAsyncResult ar)
        {
            HttpListenerContext context = httpListener.EndGetContext(ar);
            Dispather(context);//解析请求
            Receive();
        }

        RequestHelper RequestHelper;
        ResponseHelper ResponseHelper;
        void Dispather(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            RequestHelper = new RequestHelper(request);
            ResponseHelper = new ResponseHelper(response);

            bool isGetReq = RequestHelper.DispatchResources(fs => {
                ResponseHelper.WriteToClient(fs);// 对相应的请求做出回应
            });

            if (!isGetReq)
            {
                response.StatusCode = 201;
                //只有关闭OutputStream, 浏览器才不会一直等待??
                response.OutputStream.Close();
            }
        }
    }
}
