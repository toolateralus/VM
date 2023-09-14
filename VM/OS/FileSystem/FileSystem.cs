using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            var filesystem_commands = new Command[] 
            {
                new Command("clear", Clear, "makes directory at provided path if permitted"),
                new Command("root", RootCmd, "navigates the open file explorer to the root directory of the computer."),
                new Command("ls", ListDir, "lists all dirs in current directory"),
                new Command("cd", ChangeDir, "navigates to provided path if permitted"),
                new Command("mkdir", MkDir, "makes directory at provided path if permitted"),
                new Command("delete", Delete, "deletes a file or directory"),
                new Command("copy", Copy, "copy arg1 to any number of provided paths,\'\n\t\' example: { copy source destination1 destination2 destination3... }"),
                new Command("move", Move, "moves a file/changes its name"),
            };

            Computer.CommandLine.LoadCommandSet(filesystem_commands);

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

        #region FILE_SYSTEM_COMMANDS
            private void Delete(object[]? obj)
            {
                if (obj != null && obj.Length > 0 && obj[0] is string target)
                {
                    Delete(target);
                }
                else
                {
                    Notifications.Now("Invalid input parameters.");
                }
            }
            private void ListDir(object[]? obj)
            {
                // cache dir in case the user passes a path in to list that's not their working dir
                var origin_dir = CurrentDirectory;

                if (obj != null && obj.Length > 0 && obj[0] is string path){
                    ChangeDir(obj);
                }

                var list = DirectoryListing();

                var textList = string.Join(",\n", list);

                var text = $"LISTING : {CurrentDirectory}";
                
                System.Console.WriteLine(text);
                System.Console.WriteLine();
                System.Console.WriteLine(textList);
                
                ChangeDir(new[]{origin_dir});
            }
            private void ChangeDir(object[]? obj)
            {
                if (obj != null && obj.Length > 0 && obj[0] is string Path)
                {
                    ChangeDirectory(Path);
                }
            }
            private void Clear(object[]? obj)
            {
                Console.Clear();
            }
            private void Copy(object[]? obj)
            {
                if (obj != null && obj.Length > 1 && obj[0] is string Path)
                {
                    for (int i = 1; i < obj.Length; i++)
                    {
                        string Destination = obj[i] as string;
                        
                        if (Destination is null || string.IsNullOrEmpty(Destination))
                        {
                            Notifications.Now($"Invalid path {Destination} in Copy");
                            continue;
                        }
                        Copy(Path, Destination);
                        Notifications.Now($"Copied from {Path}->{Destination}");
                    }
                }
            }
            private void MkDir(object[]? obj)
            {
                if (obj != null && obj.Length > 0 && obj[0] is string Path)
                {
                    NewFile(Path); 
                    Notifications.Now($"Created directory {Path}");
                }
            }
            private void Move(object[]? obj)
            {
                string? a = obj[0] as string;
                string? b = obj[1] as string;
                Move(a, b);
                Notifications.Now($"Moved {a}->{b}");
            }
            public void RootCmd(object[]? args)
        {
            ChangeDirectory(Computer.FS_ROOT);
        }

        #endregion

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
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/VM";

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
                string currentDirectory = CurrentDirectory;

                string[] components = currentDirectory.Split('/');

                if (components.Length > 1)
                {
                    string[] parentComponents = components.Take(components.Length - 1).ToArray();

                    string parentDirectory = string.Join("/", parentComponents);

                    ChangeDirectory(parentDirectory);
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
       
        public void NewFile(string fileName)
        {
            string path = GetRelativeOrAbsolute(fileName);

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
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
