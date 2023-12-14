using ICSharpCode.AvalonEdit.Search;
using Lemur.FS;
using Lemur.Windowing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Lemur.GUI
{
    /// <summary>
    /// The included file explorer app. 
    /// browse, create, open, move, and delete files & directories.
    /// Rough around the edges, not the most refined tool.
    /// </summary>
    public partial class Explorer : UserControl
    {
        public static string? DesktopIcon => FileSystem.GetResourcePath("folder.png");
        internal Action<string>? OnNavigated;
        private Computer computer;
        private readonly ObservableCollection<FileSystemEntry> FileViewerData = new();
        private readonly Dictionary<string, string> OriginalPaths = new();
        private ContextMenu CreateMenu(string extension)
        {
            var menu = new ContextMenu();
            switch (extension)
            {
                case ".js":
                case ".md":
                case ".txt":
                case ".html":
                case ".xaml":
                case ".json":
                    var editItem = new MenuItem { Header = "Edit" };
                    editItem.Click += OnEditClicked;
                    //menu.Items.Add(editItem);
                    break;
                default:
                    break;
            }
            var deleteItem = new MenuItem() { Header = "Delete" };
            deleteItem.Click += Delete_Click;
            var propertiesItem = new MenuItem() { Header = "Properties" };
            propertiesItem.Click += Properties_Click;
            var renameItem = new MenuItem() { Header = "Rename" };

            renameItem.Click += OnRenameClicked;
            //menu.Items.Add(renameItem);
            menu.Items.Add(deleteItem);
            menu.Items.Add(propertiesItem);
            return menu;
        }
        public Explorer()
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
        private void OnRenameClicked(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnEditClicked(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        private void PreviewPath(object sender, SelectionChangedEventArgs e)
        {
            if (FileBox.SelectedItem is FileSystemEntry entry && OriginalPaths.TryGetValue(entry.Name, out var AbsolutePath))
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

        public void LateInit(Computer c)
        {
            this.computer = c;
        }
        private void UpdateView()
        {
            SearchBar.Text = FileSystem.CurrentDirectory.Replace(FileSystem.Root + "\\", "");

            FileViewerData.Clear();

            var fileNames = Computer.Current.FileSystem.DirectoryListing();

            const string FolderIcon = "📁";
            const string FileIcon = "📄";

            if (SearchBar.Text != FileSystem.Root)
            {
                var parentAddr = ".. back";
                var entry = new FileSystemEntry("", parentAddr, new());
                FileViewerData.Add(entry);
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
                ContextMenu menu = CreateMenu(Path.GetExtension(file));
                string name = visualPath.ToString();
                var entry = new FileSystemEntry(isDir ? FolderIcon : FileIcon, name, menu);
                FileViewerData.Add(entry);

                OriginalPaths[name] = file;
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
        
        private void SearchPressed(object sender, RoutedEventArgs e)
        {
            Navigate();
            UpdateView();
        }
        private void Navigate()
        {
            var path = SearchBar.Text;

            if (Computer.Current.CLI.TryCommand(path))
            {
                Notifications.Now($"Command {path} succeeded.");
                return;
            }

            var exists = FileSystem.FileExists(path) || FileSystem.DirectoryExists(path);

            if (exists)
            {
                if (FileSystem.FileExists(path))
                {
                    Computer.Current.OpenApp(new Texed(path), "texed.app", computer.ProcessManager.GetNextProcessID());
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

        private void OnSearchBarkeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                Navigate();
                UpdateView();
            }
        }
    }
}
public record class FileSystemEntry(string Icon, string Name, ContextMenu Menu);
