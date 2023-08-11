using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using VM.OS;
using VM.Types;

namespace VM.OS.FS
{
    public class FileSystem
    {
        private string currentDirectory;
        public Computer Computer;

        class Installer
        {
            const string PATH = "computer.utils";

            public Installer(string root)
            {
                var dir = Computer.GetParentDir("VM");
                string fullPath = Path.Combine(dir, PATH);

                if (Directory.Exists(fullPath))
                {
                    CopyDirectory(fullPath, root);
                }
            }

            private void CopyDirectory(string sourceDir, string destDir)
            {
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                foreach (string file in Directory.GetFiles(sourceDir))
                {
                    string destFile = Path.Combine(destDir, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                }

                foreach (string subDir in Directory.GetDirectories(sourceDir))
                {
                    string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                    CopyDirectory(subDir, destSubDir);
                }
            }
        }


        public FileSystem(string root, Computer computer)
        {
            Computer = computer;
            if (string.IsNullOrEmpty(root))
            {
                throw new ArgumentException("Invalid root directory path.");
            }

            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
                Installer installer = new(root);
            }

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
                string currentDirectory = Computer.OS.FS.CurrentDirectory;

                string[] components = currentDirectory.Split('\\');

                if (components.Length > 1)
                {
                    string[] parentComponents = components.Take(components.Length - 1).ToArray();

                    string parentDirectory = string.Join("\\", parentComponents);

                    Computer.OS.FS.ChangeDirectory(parentDirectory);
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

        internal void Copy(string path, string destination)
        {
            File.Copy(path, destination);
        }
    }
}
