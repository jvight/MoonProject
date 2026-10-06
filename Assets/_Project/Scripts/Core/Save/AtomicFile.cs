using System.IO;
using System.Text;

namespace MoonProject.Core.Save
{
    /// <summary>
    /// Crash-safe file replacement: the new content is written and flushed to a temporary file next to the target,
    /// then swapped in with one rename (File.Replace), which also moves the previous file to the backup path. At
    /// every instant the target is either the complete old file or the complete new one.
    /// </summary>
    internal static class AtomicFile
    {
        public const string TempSuffix = ".tmp";

        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static void Write(string path, string backupPath, string content)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporary = path + TempSuffix;
            byte[] bytes = Utf8.GetBytes(content);
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            if (File.Exists(path))
            {
                File.Replace(temporary, path, backupPath, true);
            }
            else
            {
                File.Move(temporary, path);
            }
        }
    }
}
