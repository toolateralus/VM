using Lemur.FS;
using Lemur.GUI;
using Lemur.Types;
using Lemur.Windowing;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Shapes;
using Path = System.IO.Path;
namespace Lemur.FS
{
    // ### Terminal Commands

    // Actual implementation
    public partial class FileSystem
    {
        public static string Root { get; private set; }
        public FileSystem(string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                throw new ArgumentException("Invalid root directory path.");
            }

            try
            {
                if (!Directory.Exists(root))
                {
                    Directory.CreateDirectory(root);
                    Installer.Install(root);
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
                return;
            }

            Root = currentDirectory = root;
        }

        private static string currentDirectory;
        public static string CurrentDirectory
        {
            get { return currentDirectory; }
            set
            {
                try
                {
                    if (Directory.Exists(value) &&
                        WithinFileSystemBounds(value))
                    {
                        currentDirectory = value;
                    }
                    else
                        throw new DirectoryNotFoundException($"Directory '{value}' not found.");
                }
                catch (Exception e)
                {
                    Notifications.Exception(e);
                    return;
                }
            }
        }

        public static string GetResourcePath(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    Notifications.Now($"Failed to get file {name}");
                    return "";
                }

                if (System.IO.Path.IsPathFullyQualified(name))
                    if (File.Exists(name) || Directory.Exists(name))
                    {
                        if (WithinFileSystemBounds(name))
                            return name;
                        else Notifications.Now($"File {name} is inaccessible due to it being outside of the restricted file system");
                    }

                VerifyOrCreateAppdataDir(Root);

                if (Directory.Exists(name) || File.Exists(name))
                {
                    if (WithinFileSystemBounds(name))
                        return name;
                    else Notifications.Now($"File {name} is inaccessible due to it being outside of the restricted file system");
                }

                if (Directory.Exists(Root))
                {
                    string[] entries = Directory.GetFileSystemEntries(Root, name, new EnumerationOptions
                    {
                        RecurseSubdirectories = true,
                        MaxRecursionDepth = 100,
                    });

                    var foundPath = entries?.FirstOrDefault() ?? "";

                    if (File.Exists(foundPath) || Directory.Exists(foundPath))
                    {
                        if (WithinFileSystemBounds(foundPath))
                            return foundPath;
                        else Notifications.Now($"File {name} is inaccessible due to it being outside of the restricted file system");
                        return foundPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Notifications.Exception(ex);
            }

            return "";
            //throw new NullReferenceException("Failed to get resource " + name);
        }
        private static bool WithinFileSystemBounds(string? name) => name?.StartsWith(Root) is bool b && b;
        internal static void VerifyOrCreateAppdataDir(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
                return;
            }
        }
        public static void ProcessDirectoriesAndFilesRecursively(string directory, Action<string, string> processDirAction, Action<string, string> processFileAction)
        {
            try
            {
                // todo: verify whether we should even validate between iterations. seems pretty costly.
                
                foreach (string file in Directory.EnumerateFiles(directory))
                {
                    if (!ValidateAccess(file, out var path))
                        return;

                    processFileAction(directory, file);
                }

                foreach (string subDir in Directory.EnumerateDirectories(directory))
                {
                    if (!ValidateAccess(subDir, out var path))
                        return;

                    processDirAction(directory, subDir);

                    // Recursively process subdirectories
                    ProcessDirectoriesAndFilesRecursively(subDir, processDirAction, processFileAction);
                }
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
                return;
            }
        }
        public static void ChangeDirectory(string path)
        {


            try
            {
                if (path == "~")
                    path = Root;

                if (path == "..")
                {
                    string currentDirectory = CurrentDirectory;

                    string[] components = currentDirectory.Split('\\');

                    if (components.Length > 1)
                    {
                        string[] parentComponents = components.Take(components.Length - 1).ToArray();

                        string parentDirectory = string.Join("\\", parentComponents);

                        ChangeDirectory(parentDirectory);
                    }
                    return;
                }

                path = GetRelativeOrAbsolute(path);

                if (Directory.Exists(path) && WithinFileSystemBounds(path))
                    CurrentDirectory = path;

                else if (!File.Exists(path))
                {
                    Notifications.Now($"Directory '{path}' not found in current path.");
                }
                else if (!WithinFileSystemBounds(path))
                {
                    Notifications.Now($"Directory '{path}' is inaccessible due to it being outside of the restricted file system");
                }
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
                return;
            }
        }
        public static void NewFile(string fileName, bool isDirectory = false)
        {
            
            try
            {
                if (!ValidateAccess(fileName, out var path))
                    return;

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
            catch (Exception e)
            {
                Notifications.Exception(e);
                return;
            }
        }
        public static void Delete(string fileName)
        {
           

            try
            {

                if (!ValidateAccess(fileName, out var path))
                    return;

                if (Directory.Exists(path) && !File.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                else
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    else
                    {
                        Notifications.Now($"File '{fileName}' not found in current path.");
                    }
                }
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
                return;
            }
        }
        private static string GetRelativeOrAbsolute(string fileName)
        {
            try
            {
                var targetPath = fileName;

                if (!Path.IsPathFullyQualified(targetPath))
                {
                    targetPath = Path.Combine(FileSystem.Root, fileName);
                }

                return targetPath;
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
                return "";
            }
        }
        public static void Write(string fileName, string content)
        {
            try
            {
                if (!ValidateAccess(fileName, out var path))
                    return;

                var parent = Directory.GetParent(path);

                if (parent != null && !Directory.Exists(parent.FullName))
                    parent.Create();

                File.WriteAllText(path, content);
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
                return;
            }
        }
        public static string Read(string fileName)
        {
            try
            {
                if (!ValidateAccess(fileName, out var path))
                    return "";

                if (File.Exists(path))
                {
                    return File.ReadAllText(path);
                }
                else
                {
                    Notifications.Now($"File '{path}' not found in current path.");
                    return "";
                }
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
                return "";
            }
        }
        public static bool FileExists(string fileName)
        {
            try
            {
                if (!ValidateAccess(fileName, out var path))
                    return false;

                return path.Length > 0 && File.Exists(path);
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
                return false;
            }
        }
        public static bool DirectoryExists(string directoryName)
        {
            try
            {
                if (!ValidateAccess(directoryName, out var path))
                    return false;

                return Directory.Exists(path);
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
                return false;
            }
        }

        private static bool ValidateAccess(string directoryName, out string path)
        {
            path = GetRelativeOrAbsolute(directoryName);
            var contents = path.Length == 0 ? "was empty" : path;

            if (!WithinFileSystemBounds(path) || path.Length == 0)
            {
                Notifications.Now($"Unauthorized access or bad path. offending string : {{{contents}}}");
                return false;
            }
            return true;
        }

        public string[] DirectoryListing()
        {
            try
            {
                string[] content = Directory.GetFileSystemEntries(currentDirectory);

                return content;
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
                return [];
            }
        }
        internal static void Copy(string sourcePath, string destinationPath)
        {
            try
            {
                destinationPath = GetRelativeOrAbsolute(destinationPath);

                if (!ValidateAccess(sourcePath, out sourcePath))
                    return;

                if (!ValidateAccess(destinationPath, out destinationPath))
                    return;

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
            catch (Exception e)
            {
                Notifications.Exception(e);
                return;
            }
        }
        internal static void Move(string? path, string? dest)
        {
            try
            {
                if (path != null && dest != null)
                {
                    if (!ValidateAccess(path, out path))
                        return;

                    if (!ValidateAccess(dest, out dest))
                        return;

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
            catch (Exception e)
            {
                Notifications.Exception(e);
                return;
            }
        }
        internal static async void NewDirectory(string path)
        {
            try
            {
                if (!ValidateAccess(path, out path))
                    return;

                if (!File.Exists(path) && !Directory.Exists(path))
                    Directory.CreateDirectory(path);
                else
                {
                    Notifications.Now("that directory or file already exists. do you want to overwrite? [y/n]");

                    var result = await Computer.Current.JavaScript.Execute($"read()");

                    if (result is string answer && answer == "y")
                    {
                        if (File.Exists(path))
                            File.Delete(path);

                        if (Directory.Exists(path))
                            Directory.Delete(path);

                        Directory.CreateDirectory(path);
                    }
                }
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
                return;
            }
        }
    }
}
