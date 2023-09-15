using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VM.Network;
using VM.JS;
using VM.FS;
using Microsoft.ClearScript.JavaScript;

namespace VM.Lang
{
    public class CommandLine : IDisposable
    {
        public Computer Computer;
        public Dictionary<string, List<Command>> Commands = new();
        public Dictionary<string, string> Aliases = new();
        private bool Disposing;

        public CommandLine(Computer computer)
        {
            Computer = computer;
            RegisterNativeCommands();
        }
        private void RegisterNativeCommands()
        {
            LoadCommandSet("generic commands",
                new("help", Help, "shows a list of all available commands and aliases to the currently open command prompt."),
                new("config" ,Config, "config .. {<set or get>} ..  {<property name>} .. {(new value is for set only) <new value>}"),
                new("js", RunJavaScriptSourceFile, "runs a js file of name provided, such as myCodeFile to run myCodeFile.js in any directory under ../Appdata/VM")
            );
        }
        #region  DEFAULT COMMANDS
        // These should get moved to their respective locations, it's unneccesary to make all the commands statically in this class when it presents complexity,
        // you should be making commands in their context. it's much simpler anyway.
        
        
        private void Config(object[]? obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string getset)
            {
                string toLower = getset.ToLower();

                if (obj.Length > 1 && obj[1] is string propname)
                {
                    propname = propname.ToUpper();

                    if (toLower == "get")
                    {
                        if (!Computer.Config.TryGetValue(propname, out var propValue))
                        {
                            IO.WriteLine($"Property '{propname}' not found in configuration.");
                            return;
                        }
                        IO.WriteLine($"propname : {propValue}");
                    }
                    else if (toLower == "set" && obj.Length > 2)
                    {
                        string arg = "";

                        // join last args
                        foreach (var item in obj[2..])
                        {
                            arg += $" {item}";
                        }

                        arg = arg.Trim();

                        if (arg.First() == '[' && arg.Last() == ']')
                        {
                            var clean = arg.Replace('[', ' ');
                            clean = clean.Replace(']', ' ');

                            var contents = clean.Split(',');

                            List<object> objectArgs = new();

                            foreach (var item in contents)
                            {
                                if (float.TryParse(item, out var floatingpt))
                                    objectArgs.Add(floatingpt);
                                else if (int.TryParse(item, out var integer))
                                    objectArgs.Add(integer);
                                else if (bool.TryParse(item, out var boolean))
                                    objectArgs.Add(boolean);
                            }

                            var jObject =JObject.FromObject(objectArgs);

                            Computer.Config[propname] = jObject;
                        }

                        Computer.Config[propname] = arg;
                    }
                }
                else if (toLower == "all")
                {
                    foreach (var kvp in Computer.Config)
                    {
                        IO.WriteLine($"\n {{{kvp.Key} : {kvp.Value}}}");
                    }
                }
                else
                {
                    IO.WriteLine("Invalid operation specified.");
                }
            }
            else
            {
                IO.WriteLine("Invalid input parameters.");
            }
        }
        private void Help(params object[]? obj)
        {
            if (obj?.Length > 0)
            {
                GetSpecificHelp(obj);
                return;
            }

            IO.WriteLine(" ### Native Commands ### ");
            IO.WriteLine("");
            // Determine the maximum width of item.Max(
            int maxTagWidth = Commands.Max(item => item.Value.Max(item => item.id.Length));

            foreach (var dir in Commands)
            {
                foreach(var item in dir.Value)
                {
                    string paddedTag = item.id.PadRight(maxTagWidth);
                    IO.WriteLine($"{paddedTag} '{string.Join(",", item.infos).ToUpper()}'");
                }
            }

            foreach (var item in Aliases)
            {
                // Split the alias to get the last part for padding
                string alias = item.Value.Split('/').Last();
                string paddedAlias = item.Key.PadRight(maxTagWidth);
                IO.WriteLine($"{paddedAlias} -> {alias}");
            }

            IO.WriteLine("");
            IO.WriteLine("### JAVASCRIPT GLOBAL FUNCTIONS ####");
            IO.WriteLine("");

            // we use any function starting with a lowercase letter, since it's against C# typical syntactical norms
            foreach(var item in typeof(JSInterop).GetMethods().Where(M=>M.Name.StartsWith(M.Name.ToLower()[0])))
            {
                Computer.JavaScriptEngine.InteropModule.print(item.Name + $"()");
            }
        }
        private void GetSpecificHelp(params object[] parameters)
        {
            var name = parameters[0] as string;
            List<string> args = new();
            for (int i = 0; i < parameters.Length; ++i)
            {
                if (parameters[i] is string arg)
                {
                    args.Add(arg);
                }
            }
        }
        private async void RunJavaScriptSourceFile(object[]? obj)
        {
            if (obj.Length > 0 && obj[0] is string path && FileSystem.GetResourcePath(path + ".js") is string AbsPath &&  File.Exists(AbsPath))
            {
                await Computer.JavaScriptEngine.Execute(File.ReadAllText(AbsPath));
                IO.WriteLine($"running {AbsPath}...");
            }
        }
        #endregion
        public bool TryCommand(string input)
        {
            if (Find(input) is Command _cmd && _cmd.id != null && _cmd.id != "NULL" && _cmd.Method != null)
            {
                _cmd.Method.Invoke(null);
                return true;
            }

            string[] split = input.Split(' ');

            if (split.Length == 0)
                return false;

            string cmdName = split.First();
            var str_args = split[1..];

            if (Aliases.TryGetValue(cmdName, out var alias) && File.Exists(alias))
            {
                var jsCode = File.ReadAllText(alias);

                const string ArgsArrayReplacement = "[/***/]";
                var index = jsCode.IndexOf(ArgsArrayReplacement);

                if (index != -1)
                {
                    var args = jsCode.Substring(index, ArgsArrayReplacement.Length);

                    var newArgs = $"[{string.Join(",", str_args)}]";

                    jsCode = jsCode.Replace(args, newArgs);
                }

                Task.Run(async delegate { await Computer.JavaScriptEngine.Execute(jsCode); });
                return true;
            }

            return TryInvoke(cmdName, str_args);
        }
        public Command Find(string name)
        {   
            foreach(var dir in Commands)
            {
                foreach(var cmd in dir.Value)
                    if (cmd.id==name)
                        return cmd;
                
            }

            return default;
        }

        internal bool TryInvoke(string name, string[] args)
        {
            if (Find(name) is Command command && command.id != null && command.id != "NULL" && command.Method != null)
            {
                command.Method.Invoke(args);
                return true;
            }
            return false;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposing)
            {
                if (disposing)
                {
                    Computer = null!;
                    Commands.Clear();
                    Aliases.Clear();
                }
                Disposing = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Some of the pre existing directories : "generic commands", "network commands", "file system commands",
        /// </summary>
        /// <param name="Directory"></param>
        /// <param name="commands"></param>
        public void LoadCommandSet(string Directory = "generic commands", params Command[] commands)
        {
            Commands ??= new();

            if (!Commands.TryGetValue(Directory, out var cmds) || cmds == null)
                Commands[Directory] = new();

                
            Commands[Directory].AddRange(commands);
        }
    }
}

