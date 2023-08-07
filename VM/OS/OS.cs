using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VM.GUI;
using VM.OS.FS;
using VM.OS.JS;
using VM.OS.Network;

namespace VM.OS
{
    public class Computer
    {
        // This connects every computer to the lan server
        public NetworkConfiguration Network = new();

        public Computer(uint id)
        {
            OS = new(id, this);

            
            
            
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

        internal void InstallCustomApplication(string id, UserControl type)
        {
            
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
            
            _ = JavaScriptEngine.Execute($"OS.id = {id}");

            JavaScriptEngine.InteropModule.OnComputerExit += computer.Exit;
        }
    }
}
