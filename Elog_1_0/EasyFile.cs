using System;
using System.IO;
using System.Text;

namespace Elog_1_0
{
    /// <summary>
    /// Recovered singleton helper class found inside Elog_1_0.dll metadata.
    /// </summary>
    public sealed class EasyFile
    {
        private static readonly Lazy<EasyFile> _instance = new Lazy<EasyFile>(() => new EasyFile());
        private EasyFile() { }
        public static EasyFile GetInstance() { return _instance.Value; }

        public string ReadFile(string paramFilePath)
        {
            if (string.IsNullOrWhiteSpace(paramFilePath) || !File.Exists(paramFilePath))
                return string.Empty;
            return File.ReadAllText(paramFilePath, Encoding.UTF8);
        }

        public bool WriteFile(string paramFilePath, string paramWriteContent, bool isSave)
        {
            if (!isSave) return true;
            return WriteFile(paramFilePath, paramWriteContent);
        }

        public bool WriteFile(string paramFilePath, string paramWriteContent)
        {
            try
            {
                var dir = Path.GetDirectoryName(paramFilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(paramFilePath, paramWriteContent ?? string.Empty, Encoding.UTF8);
                return true;
            }
            catch { return false; }
        }

        public bool AppendFile(string paramFilePath, string paramWriteContent)
        {
            try
            {
                var dir = Path.GetDirectoryName(paramFilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(paramFilePath, paramWriteContent ?? string.Empty, Encoding.UTF8);
                return true;
            }
            catch { return false; }
        }

        public bool MakeDirectory(string paramPath)
        {
            try { Directory.CreateDirectory(paramPath); return true; }
            catch { return false; }
        }

        public bool DropDirectoryAllFile(string paramFolderPath)
        {
            try
            {
                if (!Directory.Exists(paramFolderPath)) return true;
                foreach (var file in Directory.GetFiles(paramFolderPath)) File.Delete(file);
                return true;
            }
            catch { return false; }
        }
    }
}
