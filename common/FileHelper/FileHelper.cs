using System;
using System.IO;

namespace Helper
{
    public class FileHelper
    {
        /// <summary>
        /// 获取文件名
        /// </summary>
        /// <param name="fileFullPath"></param>
        /// <returns></returns>
        public static string GetFileName(string fileFullPath)
        {
            int beginIndex = fileFullPath.LastIndexOf('\\');
            return fileFullPath.Substring(beginIndex + 1);
        }
        /// <summary>
        /// 创建文件流
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static FileStream CreateFileStream(string filepath, string name)
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
    }
}
