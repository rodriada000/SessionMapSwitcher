using System.IO;

namespace SessionMapSwitcher.Classes
{
    public static class FileInfoExtensionMethods
    {
        public static string NameWithoutExtension(this FileInfo fileInfo)
        {
            int extLength = fileInfo.Extension.Length;
            return fileInfo.Name.Substring(0, fileInfo.Name.Length - extLength);
        }
    }
}
