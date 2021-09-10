using DataStruct;
using Helper;
using SocketServer.tool;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Helper
{
    public class CMDHandler
    {
        public Action<TCPTask> _action = null;

    }
    public class CMDDispatcher : Singletion<CMDDispatcher>
    {
        static int threadNum = 4;
        static int semaphoreMaximumCount = 100;//100个不够吧
        Semaphore semaphore = new Semaphore(0, semaphoreMaximumCount);
        static int count = 0;

        Dictionary<TCPCMDS, CMDHandler> _CMD2Action = new Dictionary<TCPCMDS, CMDHandler>();
        ConcurrentQueue<TCPTask> _taskQueue = new ConcurrentQueue<TCPTask>();
        CMDDispatcher()
        {
            for(int i = 0; i < threadNum; ++i)
            {
                Thread thread = new Thread(Execute);
                thread.Name = i.ToString();
                thread.Start();
            }
        }

        public void Test()
        {
            //for(int i = 0; i < 101; ++i)
            //{
            //    TCPTask task = new TCPTask();
            //    Dispatcher(task);
            //}
        }
        private void Execute()
        {
            TCPTask task = null;
            CMDHandler outHandler;
            TCPCMDS cmdID;
            //使用生产者消费者模型?
            while (true)
            {
                try
                {
                    semaphore.WaitOne();
                    //
                    if (_taskQueue.TryDequeue(out task))
                    {
                        cmdID = (TCPCMDS)Byte4Int(task.buffer);

                        if (!_CMD2Action.TryGetValue(cmdID, out outHandler))
                        {
                            LogHelper.Log(LogType.Error_CMDIsNull, cmdID.ToString());
                        }
                        else
                        {
                            LogHelper.Log(LogType.Msg_ProcessCmd, cmdID.ToString());

                            outHandler._action(task);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log(LogType.Exception, ex.ToString());
                }
                finally
                {
                    task = null;
                    outHandler = null;
                    Interlocked.Increment(ref count);
                    Console.WriteLine("threadName:{0}__taskID:{1}", Thread.CurrentThread.Name, count);
                }
            }
        }
        public void RegisterCMD(TCPCMDS cmdID, Action<TCPTask> action)
        {
            CMDHandler newHandler = new CMDHandler()
            {
                _action = action
            };
            CMDHandler outHandler;
            if(_CMD2Action.TryGetValue(cmdID, out outHandler))
            {
                LogHelper.Log(LogType.Error_CMDRepeat, cmdID.ToString());
                throw new Exception("cmd重复");
            }
            else
            {
                _CMD2Action[cmdID] = newHandler;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        public void Dispatcher(TCPTask task)
        {
            _taskQueue.Enqueue(task);

            semaphore.Release();
        }


        //4位byte转为int
        private static int Byte4Int(byte[] buf)
        {
            return ((buf[3] & 0xff) << 24) | ((buf[2] & 0xff) << 16) | ((buf[1] & 0xff) << 8) | (buf[0] & 0xff);
        }
    }
}
