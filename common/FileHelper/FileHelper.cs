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
        /// 创建文件流(如果文件存在，创建一个新文件)
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static FileStream CreateFile(string filepath, string name)
        {
            FileStream ret = null;
            try
            {
                string extName = "";
                name = GetFileExtension(name, out extName);
                if (!Directory.Exists(filepath))
                {
                    Directory.CreateDirectory(filepath);
                }
                string fileFullPath = $"{filepath}\\{name}.{extName}";
                if (File.Exists(fileFullPath))
                {
                    fileFullPath = $"{filepath}\\{name}_{DateTime.Now.Ticks}_{extName}";
                }
                ret = new FileStream(fileFullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            }
            catch (Exception ex)
            {
                LogHelper.Log(LogType.Exception, ex.ToString());
            }
            return ret;
        }
        public static string GetFileExtension(string name, out string extName)
        {
            int index = name.LastIndexOf('.');
            extName = name.Substring(index);
            return name.Substring(0, index);
        }
        //
        public static FileStream OpenFile(string filepath, string name)
        {
            FileStream ret = null;
            try
            {
                string fileFullPath = $"{filepath}\\{name}";
                if (Directory.Exists(filepath) && File.Exists(fileFullPath))
                {
                    ret = new FileStream(fileFullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(LogType.Exception, ex.ToString());
            }
            return ret;
        }
    }
}
