using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VM
{
    public class OS
    {
        public FileSystem FileSystem = new(ROOT);

        public static string ROOT => root;
        static string root = $"{System.IO.Directory.GetCurrentDirectory()}\\root";
        
        public static string CurrentDirectory => currentDirectory;
        static string currentDirectory = root;

        private static OS current = null!;
        public static OS Current => current;
        public OS()
        {
            if (current != null)
            {
                throw new InvalidOperationException("Cannot instantiate several instances of the operating system");
            }
            else current = this;
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

        public void ChangeDirectory(string path)
        {
            string newPath = Path.Combine(currentDirectory, path);
            if (Directory.Exists(newPath))
            {
                currentDirectory = newPath;
            }
            else
            {
                throw new DirectoryNotFoundException($"Directory '{path}' not found in current path.");
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
                    throw new IOException($"File '{fileName}' already exists.");
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
                    throw new DirectoryNotFoundException($"Directory '{fileName}' not found in current path.");
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
                    throw new FileNotFoundException($"File '{fileName}' not found in current path.");
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
                throw new FileNotFoundException($"File '{fileName}' not found in current path.");
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

        public string[] SerializeCurrentDirectory()
        {
            string[] content = Directory.GetFileSystemEntries(currentDirectory);
            return content;
        }
    }
}
