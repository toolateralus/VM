using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

namespace VM
{
    public class OS
    {
        public FileSystem FileSystem = new(ROOT);

        public static string ROOT => root;
        static string root = $"{System.IO.Directory.GetCurrentDirectory()}\\root";
        
        private static OS current = null!;
        public static OS Current => current;

        public FontFamily SystemFont { get; internal set; } = new FontFamily("Consolas");

        public OS()
        {
            if (current != null)
            {
                throw new InvalidOperationException("Cannot instantiate several instances of the operating system");
            }
            else current = this;
        }

       
    }
    public static class Command
    {
        public static Dictionary<string, Action<object[]?>> Commands = new(){
            { "-root", RootCmd },
        };
        static internal void RootCmd(object[]? args)
        {
            OS.Current.FileSystem.ChangeDirectory(OS.ROOT);
        }

        internal static bool TryCommand(string path, params object[]? args)
        {
            if (Commands.TryGetValue(path, out var cmd))
            {
                cmd.Invoke(args);
                return true;
            }

            return false;

        }
    }

    public class FileSystem
    {
        private string currentDirectory;
        public FileSystem(string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                throw new ArgumentException("Invalid root directory path.");
            }

            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            currentDirectory = root;
        }
        public string CurrentDirectory
        {
            get { return currentDirectory; }
            set
            {
                if (Directory.Exists(value))
                {
                    currentDirectory = value;
                }
                else
                {
                    throw new DirectoryNotFoundException($"Directory '{value}' not found.");
                }
            }
        }

        public Deque<string> History = new();
        public void ChangeDirectory(string path)
        {
            if (path == "..")
            {
                string currentDirectory = OS.Current.FileSystem.CurrentDirectory;

                string[] components = currentDirectory.Split('\\');

                if (components.Length > 1)
                {
                    string[] parentComponents = components.Take(components.Length - 1).ToArray();

                    string parentDirectory = string.Join("\\", parentComponents);

                    OS.Current.FileSystem.ChangeDirectory(parentDirectory);
                }
                return;
            }

            string newPath = Path.Combine(currentDirectory, path);

            if (Directory.Exists(newPath))
            {
                History.Push(currentDirectory);
                currentDirectory = newPath;
            }
            else
            {
                Notifications.Now($"Directory '{path}' not found in current path.");
            }
        }
        public void NewFile(string fileName, bool isDirectory = false)
        {
            string newPath = Path.Combine(currentDirectory, fileName);
            if (isDirectory && !File.Exists(newPath) && !Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            else
            {
                if (!File.Exists(newPath) && !Directory.Exists(newPath))
                {
                    File.Create(newPath).Close();
                }
                else
                {
                    Notifications.Now($"File '{fileName}' already exists.");
                }
            }
        }
        public void DeleteFile(string fileName, bool isDirectory = false)
        {
            string targetPath = Path.Combine(currentDirectory, fileName);
            if (isDirectory)
            {
                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                }
                else
                {
                    Notifications.Now($"Directory '{fileName}' not found in current path.");
                }
            }
            else
            {
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }
                else
                {
                    Notifications.Now($"File '{fileName}' not found in current path.");
                }
            }
        }
        public void Write(string fileName, string content)
        {
            string filePath = Path.Combine(currentDirectory, fileName);
            File.WriteAllText(filePath, content);
        }
        public string Read(string fileName)
        {
            string filePath = Path.Combine(currentDirectory, fileName);
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            else
            {
                Notifications.Now($"File '{fileName}' not found in current path.");
                return "";
            }
        }
        public bool FileExists(string fileName)
        {
            string filePath = Path.Combine(currentDirectory, fileName);
            return File.Exists(filePath);
        }
        public bool DirectoryExists(string directoryName)
        {
            string directoryPath = Path.Combine(currentDirectory, directoryName);
            return Directory.Exists(directoryPath);
        }
        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }
        public string[] DirectoryListing()
        {
            string[] content = Directory.GetFileSystemEntries(currentDirectory);
            return content;
        }
    }
}
