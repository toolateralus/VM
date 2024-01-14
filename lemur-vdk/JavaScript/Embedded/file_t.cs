using Lemur.FS;
using Lemur.Windowing;
using System;
using System.IO;

namespace Lemur.JS.Embedded
{
    public class file_t
    {
        public object? read(string path)
        {
            ArgumentNullException.ThrowIfNull(path, nameof(path));
            return FileSystem.Read(path);
        }
        public void write(string path, string data)
        {
            ArgumentNullException.ThrowIfNull(path, nameof(path));
            ArgumentNullException.ThrowIfNull(data, nameof(data));
            FileSystem.Write(path, data);
        }
        public bool exists(string path)
        {
            ArgumentNullException.ThrowIfNull(path, nameof(path));
            return FileSystem.FileExists(path);
        }
    }
}

