using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Windows.Controls;
using VM.GUI;
using VM.OS.FS;
using VM.OS.JS;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;
using System.Windows.Media.Imaging;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Microsoft.VisualBasic.Devices;
using System.Threading;
using System.Windows.Threading;
using System.Reflection.Metadata;

namespace VM.OS
{
    public class OS
    {
        // we should re-think the references we have to the computer everywhere, maybe just combine the OS and pc or fix the strange references.
        public FileSystem FS;
        public JavaScriptEngine JavaScriptEngine;
        public CommandLine CommandLine;
        public Theme Theme = new();

        public readonly uint ID;
        public readonly string FS_ROOT;
        public readonly string WORKING_DIR;

        public JObject Config;
        public Dictionary<string, Type> Applets = new();
        public void InstallApplication(string exePath, Type type) 
        {
            // do we need this collection? it helps us identify already existing apps but it's almost unneccesary,
            // we may be relying on our UI scripts to do too much behavior.
            if (Applets.TryGetValue(exePath, out _))
            {
                Notifications.Now("Tried to install an app that already exists on the computer, try renaming it if this was intended");
                return;
            }

            Applets[exePath] = type;
            Notifications.Now($"{exePath} installed!");

            ComputerWindow window = Runtime.GetWindow(FS.Computer);
            window.RegisterApp(exePath, type);
        }

        public OS(uint id, Computer computer)
        {
            CommandLine = new(computer);

            var WORKING_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VM";

            this.WORKING_DIR = Path.GetFullPath(WORKING_DIR);
            // prepare the root dir for the FileSystem, since we add a dir to contain that itself.
            FS_ROOT = $"{this.WORKING_DIR}\\computer{id}";

            FS = new(FS_ROOT, computer);

            Config = OSConfigLoader.Load();

        }

        public void InitializeEngine(uint id, Computer computer)
        {
            JavaScriptEngine = new(this.WORKING_DIR, computer);

            string jsDirectory = Computer.SearchForParentRecursive("VM");

            JavaScriptEngine.LoadModules(jsDirectory + "\\OS-JS");

            _ = JavaScriptEngine.Execute($"os.id = {id}");

            JavaScriptEngine.InteropModule.OnComputerExit += computer.Exit;

            if (Runtime.GetResourcePath("startup.js") is string AbsPath)
            {
                JavaScriptEngine.ExecuteScript(AbsPath);
            }
        }

        public void SaveConfig()
        {
            string configFilePath = Runtime.GetResourcePath("config.json");

            if (!string.IsNullOrEmpty(configFilePath))
            {
                try
                {
                    File.WriteAllText(configFilePath, Config.ToString());
                }
                catch (Exception ex)
                {
                    Notifications.Now($"Error saving JSON config: {ex.Message}");
                }
            }
        }
    }
}
