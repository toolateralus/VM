using Lemur.FS;
using Lemur.GUI;
using Lemur.Windowing;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lemur.JS.Embedded {
    /// <summary>
    /// An embedded JavaScript Type :
    /// 
    /// Provides interaction with the terminal and OS 'terminal'
    /// </summary>
    public class Terminal_t : embedable
    {
    
        /// <summary>
        /// Tries to invoke a terminal command, in the background.
        /// </summary>
        /// <param name="command"></param>
        [ApiDoc("call a terminal command. much like C's system() function")]
        public void call(string command)
        {
            Task.Run(() =>
            {
                if (!Computer.Current.CLI.TryCommand(command))
                    Notifications.Now($"Couldn't 'call' {command}");
            });
        }
        [ApiDoc("send a global notification to the desktop. Useful for throwing warnings or errors that the user of an app needs to see. just use notify() not Term.notify()")]
        public void notify(params object[] message)
        {
            try
            {
                var msg = '\n' + string.Join('\n', message);
                Notifications.Now(msg);
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
            }
        }
        /// <summary>
        /// Prints to the terminal. there is a global function 'print' that wraps this.
        /// </summary>
        /// <param name="message"></param>
        [ApiDoc("print a line to every terminal instance open. Useful for throwing warnings or errors that the user of an app needs to see. just use print() not Term.print()")]
        public void print(params object[] message)
        {
            try
            {
                var msg = '\n' + string.Join('\n', message);
                Computer.Current.Window?.Dispatcher.Invoke(() =>
                {
                    foreach (var cmd in GetComputer().ProcessManager.TryGetAllProcessesOfTypeUnsafe<GUI.Terminal>())
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
        [ApiDoc("Read a line of input from the terminal. Useful for CLI apps.")]
        public string? read()
        {
            GUI.Terminal cmd = null;
            cmd = GetComputer().ProcessManager.TryGetProcessOfType<GUI.Terminal>();

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

            while (string.IsNullOrEmpty(result))
            {
                Thread.Sleep(5);
            }

            cmd.IsReading = false;
            cmd.OnTerminalSend -= end;

            return result;
        }
        [ApiDoc("Create a command alias, that when the alias is called in the terminal, the javascript file at the provided path is executed.")]
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

            Computer.Current.CLI.Aliases[alias] = FileSystem.GetResourcePath(path) ?? "not found";
        }
        [ApiDoc("Set the default directory for the command aliases to be searched for")]
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
                if (!string.IsNullOrEmpty(regex))
                {
                    name = Regex.Match(file, regex).Value;
                }
                else
                {
                    name = Path.GetFileName(file).Replace(Path.GetExtension(file), "");
                }

                Computer.Current.CLI.Aliases[name] = file;
            };
            Action<string, string> procDir = delegate { };
            FileSystem.ProcessDirectoriesAndFilesRecursively(path, /*UNUSED*/ procDir, procFile);
        }
    }
}

