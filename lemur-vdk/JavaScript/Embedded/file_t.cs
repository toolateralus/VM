using System;
using System.IO;
using lemur.Windowing;
using Lemur.FS;

namespace lemur.JS.Embedded
{
    public class file_t
    {
        public object? read(string path)
        {
            if (!File.Exists(path))
            {
                if (FileSystem.GetResourcePath(path) is string AbsPath && !string.IsNullOrEmpty(AbsPath))
                {
                    return File.ReadAllText(AbsPath);
                }

                return null;
            }
            return File.ReadAllText(path);
        }
        public void write(string path, object? data)
        {
            if (string.IsNullOrEmpty(path))
            {
                Notifications.Exception(e: new ArgumentNullException("Tried to write a file with a null or empty path, this is not allowed."));
                return;
            }

            if (!Path.IsPathFullyQualified(path))
                path = Path.Combine(FileSystem.Root, path);

            string? dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, data?.ToString() ?? "");
        }
        public bool exists(string path)
        {
            return FileSystem.GetResourcePath(path) is string AbsPath && !string.IsNullOrEmpty(AbsPath) ? File.Exists(AbsPath) : false;
        }
        public object get_entries(string path)
        {
            if (File.Exists(path))
                return path;

            if (!Directory.Exists(path))
                return "";

            return Directory.GetFileSystemEntries(path);
        }
    }
}

