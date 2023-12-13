using Lemur.FS;
using Lemur.GUI;
using Lemur.Windowing;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lemur.JS.Embedded
{
    /// <summary>
    /// An embedded JavaScript Type :
    /// 
    /// Provides interaction with the terminal and OS 'terminal'
    /// </summary>
    public class term_t
    {
        /// <summary>
        /// Tries to invoke a terminal command, in the background.
        /// </summary>
        /// <param name="command"></param>
        public void call(string command)
        {
            Task.Run(() =>
            {
                if (!Computer.Current.CmdLine.TryCommand(command))
                    Notifications.Now($"Couldn't 'call' {command}");
            });
        }
        /// <summary>
        /// Prints to the terminal. there is a global function 'print' that wraps this.
        /// </summary>
        /// <param name="message"></param>
        public void print(params object[] message)
        {
            try
            {
                var msg = '\n' + string.Join('\n', message);
                Computer.Current.Window?.Dispatcher.Invoke(() =>
                {
                    foreach (var cmd in Computer.TryGetAllProcessesOfTypeUnsafe<Terminal>())
                        cmd.output.AppendText(msg);
                });
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
            }
        }
        /// <summary>
        /// Read from the terminal.
        /// </summary>
        /// <returns></returns>
        public string? read()
        {
            Terminal cmd = null;
            cmd = Computer.TryGetProcessOfType<Terminal>();

            var waiting = true;
            string result = "";
            if (cmd is null)
            {
                Notifications.Now("No console was open, so reading is impossible");
                return null;
            }
            cmd.IsReading = true;
            cmd.OnTerminalSend += end;

            void end(string obj)
            {
                result = obj;
            }

            while (result == "")
            {
                Thread.Sleep(5);
            }

            cmd.IsReading = false;
            cmd.OnTerminalSend -= end;

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

