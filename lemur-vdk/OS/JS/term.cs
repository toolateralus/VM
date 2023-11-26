using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Lemur.GUI;
using Lemur.FS;
using System.Text.RegularExpressions;

namespace Lemur.JS
{
    public class term
    {
        public void print(object message)
        {
            try
            {
                Computer.Current.Window?.Dispatcher.Invoke(() =>
                {
                    Debug.WriteLine(message);
                    Notifications.Now(message?.ToString() ?? "null");
                });
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
            }
        }

        public string? read()
        {
            CommandPrompt cmd = null;
            cmd = Computer.TryGetProcessOfType<CommandPrompt>();

            var waiting = true;
            string result = "";
            if (cmd is null)
            {
                Notifications.Now("No console was open, so reading is impossible");
                return null;
            }
            cmd.OnSend += end;

            void end(string obj)
            {
                result = obj;
            }

            while (result == "")
            {
                Thread.Sleep(5);
            }

            return result;
        }
        public void alias(string alias, string path)
        {

            if (path.Split('.') is string[] arr)
            {
                if (arr.Length > 1)
                {
                    if (arr[1] != "js")
                    {
                        Notifications.Now("invalid file extension for alias");
                        return;
                    }
                    // valid .js extension
                }
                else
                {
                    // needs appended .js
                    path = path += ".js";
                }
            }

            Computer.Current.CmdLine.Aliases[alias] = FileSystem.GetResourcePath(path) ?? "not found";
        }
        public void setAliasDirectory(string path, string regex = "")
        {
            if (FileSystem.GetResourcePath(path) is string AbsPath && !string.IsNullOrEmpty(AbsPath))
            {
                // validated path.
                path = AbsPath;
                Computer.Current.Config["ALIAS_PATH"] = path;
            }
            else
            {
                Notifications.Now("Attempted to set command directory to an emtpy or null string");
                return;
            }
            if (File.Exists(path) && !Directory.Exists(path))
            {
                Notifications.Now("Attempted to set command directory to an existing file or a nonexistent directory");
                return;
            }
            Action<string, string> procFile = (rootDir, file) =>
            {
                string name = "";
                if (regex != "")
                {
                    name = Regex.Match(file, regex).Value;
                }
                else
                {
                    name = Path.GetFileName(file).Replace(Path.GetExtension(file), "");
                }

                Computer.Current.CmdLine.Aliases[name] = file;
            };
            Action<string, string> procDir = delegate { };
            FileSystem.ProcessDirectoriesAndFilesRecursively(path, /*UNUSED*/ procDir, procFile);
        }
    }
}

