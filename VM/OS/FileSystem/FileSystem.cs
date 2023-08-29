using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using VM;
using VM.Types;

namespace VM.FS
{
    public class FileSystem : IDisposable
    {
        class Installer
        {
            const string PATH = "computer.utils";

            public Installer(string root)
            {
                var dir = Computer.SearchForParentRecursive("VM");
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

        public Computer Computer;
        private string currentDirectory;
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
        private bool Disposing;
        internal static string GetResourcePath(string name)
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VM";

            FileSystem.VerifyOrCreateAppdataDir(path);

            if (Directory.Exists(name) || File.Exists(name))
            {
                return name;
            }

            if (Directory.Exists(path))
            {
                string[] entries = Directory.GetFileSystemEntries(path, name, SearchOption.AllDirectories);

                return entries.FirstOrDefault();
            }

            return "";
        }
        internal static void VerifyOrCreateAppdataDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        public static void ProcessDirectoriesAndFilesRecursively(string directory, Action<string, string> processDirAction, Action<string, string> processFileAction)
        {
            // Process files in the current directory
            foreach (string file in Directory.EnumerateFiles(directory))
            {
                processFileAction(directory, file);
            }

            // Process subdirectories in the current directory
            foreach (string subDir in Directory.EnumerateDirectories(directory))
            {
                processDirAction(directory, subDir);

                // Recursively process subdirectories
                ProcessDirectoriesAndFilesRecursively(subDir, processDirAction, processFileAction);
            }
        }
        public void ChangeDirectory(string path)
        {
            if (path == "..")
            {
                string currentDirectory = Computer.FS.CurrentDirectory;

                string[] components = currentDirectory.Split('\\');

                if (components.Length > 1)
                {
                    string[] parentComponents = components.Take(components.Length - 1).ToArray();

                    string parentDirectory = string.Join("\\", parentComponents);

                    Computer.FS.ChangeDirectory(parentDirectory);
                }
                return;
            }

            path = GetRelativeOrAbsolute(path);

            if (Directory.Exists(path))
            {

                History.Push(currentDirectory);

                currentDirectory = path;
            }
            else if (!File.Exists(path))
            {
                Notifications.Now($"Directory '{path}' not found in current path.");
            }
        }
        public void NewFile(string fileName, bool isDirectory = false)
        {
            string path = GetRelativeOrAbsolute(fileName);

            if (isDirectory && !File.Exists(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    File.Create(path).Close();
                }
                else
                {
                    Notifications.Now($"File '{fileName}' already exists.");
                }
            }
        }
        public void Delete(string fileName, bool isDirectory = false)
        {
            string targetPath = GetRelativeOrAbsolute(fileName);

            if (Directory.Exists(targetPath) && !File.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
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
        private string GetRelativeOrAbsolute(string fileName)
        {
            var targetPath = fileName;

            if (!Path.IsPathFullyQualified(targetPath))
            {
                targetPath = Path.Combine(Computer.FS_ROOT, fileName);
            }

            return targetPath;
        }
        public void Write(string fileName, string content)
        {
            fileName = GetRelativeOrAbsolute(fileName);
            File.WriteAllText(fileName, content);
        }
        public string Read(string fileName)
        {
            fileName = GetRelativeOrAbsolute(fileName);
            if (File.Exists(fileName))
            {
                return File.ReadAllText(fileName);
            }
            else
            {
                Notifications.Now($"File '{fileName}' not found in current path.");
                return "";
            }
        }
        public bool FileExists(string fileName)
        {
            fileName = GetRelativeOrAbsolute(fileName);
            return File.Exists(fileName);
        }
        public bool DirectoryExists(string directoryName)
        {
            directoryName = GetRelativeOrAbsolute(directoryName);
            return Directory.Exists(directoryName);
        }
        public string[] DirectoryListing()
        {
            string[] content = Directory.GetFileSystemEntries(currentDirectory);
            return content;
        }
        internal void Copy(string sourcePath, string destinationPath)
        {
            sourcePath = GetRelativeOrAbsolute(sourcePath);
            destinationPath = GetRelativeOrAbsolute(destinationPath);

            if (Directory.Exists(sourcePath))
            {
                Directory.CreateDirectory(destinationPath);

                string[] files = Directory.GetFiles(sourcePath);
                string[] directories = Directory.GetDirectories(sourcePath);

                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    string destinationFilePath = Path.Combine(destinationPath, fileName);
                    File.Copy(filePath, destinationFilePath, true); // 'true' overwrites if the file already exists
                }

                foreach (string directoryPath in directories)
                {
                    string directoryName = Path.GetFileName(directoryPath);
                    string destinationSubdirectoryPath = Path.Combine(destinationPath, directoryName);
                    Copy(directoryPath, destinationSubdirectoryPath); 
                }
            }
            else if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, destinationPath, true); // 'true' overwrites if the file already exists
            }
            else
            {
                Notifications.Now("Source file or directory not found.. \n" + sourcePath);
            }
        }
        internal void Move(string? path, string? dest)
        {
            if (path != null && dest != null)
            {
                path = GetRelativeOrAbsolute(path);
                dest = GetRelativeOrAbsolute(dest);
                if (File.Exists(path))
                {
                    if (!File.Exists(dest))
                    {
                        File.Move(path, dest);
                    }
                    else
                    {
                        Copy(path, dest);
                        File.Delete(path);
                    }
                }
                else if (Directory.Exists(path))
                {
                    if (!Directory.Exists(dest))
                    {
                        Directory.Move(path, dest);
                    }
                    else
                    {
                        Copy(path, dest);
                        Directory.Delete(path, true);
                    }
                }
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposing)
            {
                if (disposing)
                {
                    Computer = null!;
                }
                Disposing = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
