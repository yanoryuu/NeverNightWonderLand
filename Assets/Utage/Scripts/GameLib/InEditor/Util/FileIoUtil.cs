using System.IO;

namespace Utage
{
    public static class FileIoUtil
    {
        public static void CreateFilePathDirectoryIfNotExists(string path)
        {
            CreateDirectoryIfNotExists(FilePathUtil.GetDirectoryPath(path));
        }
        public static void CreateDirectoryIfNotExists(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
