using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Lemur.FS;

namespace Lemur.GUI
{
    /// <summary>
    /// Interaction logic for FileExplorer.xaml
    /// </summary>
    public partial class FileExplorer : UserControl
    {
        public static string? DesktopIcon => FileSystem.GetResourcePath("folder.png");
        internal Action<string>? OnNavigated;

        private readonly ObservableCollection<string> FileViewerData = new();
        private readonly Dictionary<string, string> OriginalPaths = new();
        public FileExplorer()
        {
            InitializeComponent();
            FileBox.FontSize = 16;

            FileBox.ItemsSource = FileViewerData;
            FileBox.SelectionChanged += PreviewPath;
            FileBox.MouseDoubleClick += (sender, @event) =>
            {
                Navigate();
                UpdateView();

            }; 

            Computer.Current.Window.KeyDown += FileExplorer_KeyDown;

            UpdateView();

        }

        public void LateInit(Computer c)
        {
            // neccesary
        }
        private void PreviewPath(object sender, SelectionChangedEventArgs e)
        {
            if (FileBox.SelectedItem is string Path && OriginalPaths.TryGetValue(Path, out var AbsolutePath))
            {
                var x = FileSystem.Root + "\\";
                if (AbsolutePath == FileSystem.Root)
                    AbsolutePath = "";
                SearchBar.Text = AbsolutePath.Replace(x, "");
            }
        }
        private void FileExplorer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Keyboard.ClearFocus();
                UpdateView();
            }
            if (e.Key == Key.Enter)
            {
                Navigate();
                UpdateView();
            }

            
        }

        private void UpdateView()
        {
            SearchBar.Text = FileSystem.CurrentDirectory.Replace(FileSystem.Root + "\\", "");

            FileViewerData.Clear();
            
            var fileNames = Computer.Current.FileSystem.DirectoryListing();

            const string FolderIcon = "📁 ";
            const string FileIcon =   "📄 ";

            if (SearchBar.Text != FileSystem.Root)
            {
                var parentAddr = ".. back";
                FileViewerData.Add(parentAddr);
                OriginalPaths[parentAddr] = Directory.GetParent(FileSystem.CurrentDirectory)?.FullName ?? throw new InvalidOperationException("Invalid file structure");
            }
            else
            {
                SearchBar.Text = "";
            }

            foreach (var file in fileNames)
            {
                StringBuilder visualPath = new(file.Split('\\').LastOrDefault("???"));

                var isDir = Directory.Exists(file) && !File.Exists(file);

                visualPath.Insert(0, isDir ? FolderIcon : FileIcon);

                var finalVisualPath = visualPath.ToString();

                FileViewerData.Add(finalVisualPath);

                OriginalPaths[finalVisualPath] = file;
            }

        }

        private static string GetUniquePath(string dir, string name, string extension)
        {
            string path = $"{dir}{name}{extension}";
            if (FileSystem.FileExists(path) ||
                FileSystem.DirectoryExists(path))
            {
                int fileCount = 1;
                path = $"{dir}{name}{fileCount}{extension}";
                while (FileSystem.FileExists(path) ||
                    FileSystem.DirectoryExists(path))
                {
                    fileCount++;
                    path = $"{dir}{name}{fileCount}{extension}";
                }
            }

            return path;
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            var dir = FileSystem.CurrentDirectory.Replace(FileSystem.Root, "");
            if (dir.Length != 0)
                dir += "\\";
            string path = GetUniquePath(dir, "newfile", ".txt");
            FileSystem.NewFile(path);
            UpdateView();
        }

        private void AddDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dir = FileSystem.CurrentDirectory.Replace(FileSystem.Root, "");
            if (dir.Length != 0)
                dir += "\\";
            string path = GetUniquePath(dir, "newfolder", "");
            FileSystem.NewFile(path, true);
            UpdateView();
        }
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            FileSystem.Delete(SearchBar.Text);
            UpdateView();

        }
        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            Notifications.Now(FileSystem.CurrentDirectory);
        }
        private void BackPressed(object sender, RoutedEventArgs e)
        {
            if (FileSystem.History.Count == 0)
            {
                Notifications.Now("No file or directory to go back to.");
            }

            FileSystem.ChangeDirectory(FileSystem.History.Pop());

            UpdateView();
        }
        private void SearchPressed(object sender, RoutedEventArgs e)
        {
            Navigate();
            UpdateView();
        }
        private void Navigate()
        {
            var path = SearchBar.Text;

            if (Computer.Current.CmdLine.TryCommand(path))
                return;

            var exists = FileSystem.FileExists(path) || FileSystem.DirectoryExists(path);

            if (exists)
            {
                if (FileSystem.FileExists(path))
                {
                    Computer.Current.OpenApp(new TextEditor(path));
                    OnNavigated?.Invoke(path);
                }


                FileSystem.ChangeDirectory(path);
            }

        }
        private void UpPressed(object sender, RoutedEventArgs e)
        {
            FileSystem.ChangeDirectory("..");
            UpdateView();
        }
        private void ForwardPressed(object sender, RoutedEventArgs e)
        {
            UpdateView();
        }
    }
}
