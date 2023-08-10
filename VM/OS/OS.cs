using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VM.GUI;
using VM.OS.FS;
using VM.OS.JS;
using VM.OS.Network;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;
using System.Windows.Media.Imaging;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Microsoft.VisualBasic.Devices;
using System.Threading.Tasks;

namespace VM.OS
{
    public class Computer
    {
        // This connects every computer to the lan server
        public NetworkConfiguration Network = new();

        public Computer(uint id)
        {
            OS = new(id, this);

            OS.JavaScriptEngine.LoadModules(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VM\\OS-JS"));
            _ = OS.JavaScriptEngine.Execute($"OS.id = {id}");

            if (Runtime.GetResourcePath("startup", ".js") is string AbsPath)
            {
                OS.JavaScriptEngine.ExecuteScript(AbsPath);
            }

        }
        public uint ID() => OS.ID;

        public OS OS;

        internal void Exit(int exitCode)
        {
            Runtime.GetWindow(this).Close();

            if (Runtime.Computers.Count > 0 && exitCode != 0)
            {
                Notifications.Now($"Computer {ID()} has exited, most likely due to an error. code:{exitCode}");
            }
        }

        internal void Shutdown()
        {
            OS.JavaScriptEngine.Dispose();
        }

        internal void FinishInit(Computer pc, ComputerWindow wnd)
        {
            string[] backgroundpath = pc.OS.Config.Value<string>("BACKGROUND").Split('.') ?? new[] { "background", ".png" };

            wnd.desktopBackground.Source = ComputerWindow.LoadImage(Runtime.GetResourcePath(backgroundpath[0], "." + backgroundpath[1]));

            pc.OS.InstallApplication("CommandPrompt.app", typeof(CommandPrompt));
            pc.OS.InstallApplication("FileExplorer.app", typeof(FileExplorer));
            pc.OS.InstallApplication("TextEditor.app", typeof(TextEditor));

            wnd.Show();

            wnd.Closed += (o, e) =>
            {
                Runtime.Computers.Remove(pc);
                pc.Shutdown();
                Task.Run(() => pc.OS.SaveConfig());
            };
        }
    }

    /// <summary>
    /// The default initialization for a parameterless construction of this object represents a fully implemented default theme, and
    /// it's meant to be customized.
    /// </summary>
    public class Theme
    {
        public Brush Background = Brushes.LightGray;
        public Brush Foreground = Brushes.White;
        public Brush Border = Brushes.Transparent;
        public FontFamily Font = new("Consolas");
        public Thickness BorderThickness = new(0, 0, 0, 0);
        public double FontSize = 12;
    }
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
            
            // we get our working root
            var WORKING_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VM";
            
            this.WORKING_DIR = Path.GetFullPath(WORKING_DIR);

            // prepare the root dir for the FileSystem, since we add a dir to contain that itself.
            FS_ROOT = $"{this.WORKING_DIR}\\computer{id}";
            
            FS = new(FS_ROOT, computer);

            // prepare the javascript engine, and assign the computer ID to the var in the OS instance (in the js), and get the on exit event from the js env.
            JavaScriptEngine = new(this.WORKING_DIR, computer);

            JavaScriptEngine.InteropModule.OnComputerExit += computer.Exit;

            Config = OSConfigLoader.Load();

        }

        public void SaveConfig()
        {
            string configFilePath = Runtime.GetResourcePath("config", ".json");

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

    internal class OSConfigLoader
    {
        internal static JObject Load()
        {
            if (Runtime.GetResourcePath("config", ".json") is string AbsPath)
            {
                if (File.Exists(AbsPath))
                {
                    string json = File.ReadAllText(AbsPath);

                    try
                    {
                        return JObject.Parse(json);
                    }
                    catch (Exception ex)
                    {
                        Notifications.Now($"Error loading JSON: {ex.Message}");
                    }
                }
                else
                {
                    Notifications.Now("JSON file not found.");
                }
            }

            return null;
        }
    }
}
