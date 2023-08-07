using System;
using System.Diagnostics;
using System.IO;

namespace VM.OS.JS
{
    public class JSInterop
    {
        public Action<string, object?>? OnModuleExported;
        public Func<string, object?>? OnModuleImported;
        public Action<int>? OnComputerExit;

        #region System
        public void print(object message)
        {
            Debug.WriteLine(message);
        }
        public void export(string id, object? obj)
        {
            OnModuleExported?.Invoke(id, obj);
        }
        public void exit(int code)
        {
            OnComputerExit?.Invoke(code);
        }
        #endregion
        #region IO
        public object? require(string path)
        {
            return OnModuleImported?.Invoke(path);
        }
        public object? read_file(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            return File.ReadAllText(path);
        }
        public void write_file(string path, string data)
        {
           File.WriteAllText(path, data);
        }
        public bool file_exists(string path)
        {
            return File.Exists(path);
        }
        #endregion
    }
}
