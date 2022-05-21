using System;
using System.Collections.Generic;
using System.Text;

namespace Helper
{
    public interface IClientHelper_Interface
    {
        void Start();
        void Get(string fileFullPath);
        void Send(string fileFullPath);
    }
}
