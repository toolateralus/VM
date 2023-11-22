using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Lemur;
using Lemur.FS;

namespace Lemur.GUI
{
    /// <summary>
    /// Interaction logic for FileExplorer.xaml
    /// </summary>
    public partial class FileExplorer : UserControl
    {
        public static string? DesktopIcon => FileSystem.GetResourcePath("fileexplorer.png");
        internal Action<string>? OnNavigated;

        private readonly ObservableCollection<string> FileViewerData = new();
        private readonly Dictionary<string, string> OriginalPaths = new();
        public FileExplorer()
        {
            InitializeComponent();
            FileBox.ItemsSource = FileViewerData;
            FileBox.SelectionChanged += PreviewPath;
            FileBox.MouseDoubleClick += (sender, @event) =>
            {
                Navigate();
                UpdateView();

            }; 
            KeyDown += FileExplorer_KeyDown;
            UpdateView();

        }

        public void LateInit(Computer c)
        {
            // neccesary
        }
        private void PreviewPath(object sender, SelectionChangedEventArgs e)
        {
            if (FileBox.SelectedItem is string Path && OriginalPaths.TryGetValue(Path, out var AbsolutePath))
                SearchBar.Text = AbsolutePath;
        }
        private void FileExplorer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Keyboard.ClearFocus();
                SearchBar.Text = Computer.Current.fileSystem.CurrentDirectory;
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
            
            FileViewerData.Clear();
            
            var fileNames = Computer.Current.fileSystem.DirectoryListing();

            const string FolderIcon = "📁 ";
            const string FileIcon =   "📄 ";

            foreach (var file in fileNames)
            {
                StringBuilder visualPath = new(file.Split('\\').LastOrDefault("???"));

                var isDir = Directory.Exists(file) && !File.Exists(file);

                visualPath.Insert(0, isDir ? FolderIcon : FileIcon);

                var finalVisualPath = visualPath.ToString();

                FileViewerData.Add(finalVisualPath);

                OriginalPaths[finalVisualPath] = file;
            }

            FileViewerData.Add("..");

            OriginalPaths[".."] = Directory.GetParent(Computer.Current.fileSystem.CurrentDirectory)?.FullName ?? throw new InvalidOperationException("Invalid file structure");
        }
        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            Computer.Current.fileSystem.NewFile(SearchBar.Text);
            UpdateView();

        }
        private void AddDirectory_Click(object sender, RoutedEventArgs e)
        {
            Computer.Current.fileSystem.NewFile(SearchBar.Text, true);
            UpdateView();

        }
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            Computer.Current.fileSystem.Delete(SearchBar.Text);
            UpdateView();

        }
        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            Notifications.Now(Computer.Current.fileSystem.CurrentDirectory);
        }
        private void BackPressed(object sender, RoutedEventArgs e)
        {
            if (Computer.Current.fileSystem.History.Count == 0)
            {
                Notifications.Now("No file or directory to go back to.");
            }

            Computer.Current.fileSystem.ChangeDirectory(Computer.Current.fileSystem.History.Pop());

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

            // this is a very hacky solution to the way they were originally designed as static, todo fix the entire command system, 
            // and make a common fs
            if (Computer.Current.cmdLine.TryCommand(path))
            {
                return;
            }

            var exists = Computer.Current.fileSystem.FileExists(path) || Computer.Current.fileSystem.DirectoryExists(path);

            if (exists)
            {
                if (Computer.Current.fileSystem.FileExists(path))
                {
                    Computer.Current.OpenApp(new TextEditor(path));
                    OnNavigated?.Invoke(path);
                }


                Computer.Current.fileSystem.ChangeDirectory(path);
            }

        }
        private void UpPressed(object sender, RoutedEventArgs e)
        {
            Computer.Current.fileSystem.ChangeDirectory("..");
            UpdateView();
        }
        private void ForwardPressed(object sender, RoutedEventArgs e)
        {
            UpdateView();
        }
    }
}
