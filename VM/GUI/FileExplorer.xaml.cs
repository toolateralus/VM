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
using VM.OS;

namespace VM.GUI
{
    /// <summary>
    /// Interaction logic for FileExplorer.xaml
    /// </summary>
    public partial class FileExplorer : UserControl
    {
        public static string? DesktopIcon => Runtime.GetResourcePath("fileexplorer.png");
        ObservableCollection<string> FileViewerData = new();
        Dictionary<string, string> OriginalPaths = new();
        public Computer computer;
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
        public void LateInit(Computer computer)
        {
            this.computer = computer;
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
                SearchBar.Text = computer.OS.FS.CurrentDirectory;
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
            if (computer is null)
                return;
            
            FileViewerData.Clear();
            var fileNames = computer.OS.FS.DirectoryListing();
            const string FolderIcon = "📁 ";
            const string FileIcon = "📄 ";

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
        
        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            computer.OS.FS.NewFile(SearchBar.Text);
            UpdateView();

        }

        private void AddDirectory_Click(object sender, RoutedEventArgs e)
        {
            computer.OS.FS.NewFile(SearchBar.Text, true);
            UpdateView();

        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            computer.OS.FS.Delete(SearchBar.Text);
            UpdateView();

        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            Notifications.Now(computer.OS.FS.CurrentDirectory);
        }

        private void BackPressed(object sender, RoutedEventArgs e)
        {

            if (computer.OS.FS.History.Count == 0)
            {
                Notifications.Now("No file or directory to go back to.");
            }

            computer.OS.FS.ChangeDirectory(computer.OS.FS.History.Pop());

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
            if (computer.OS.CommandLine.TryCommand(path))
            {
                return;
            }

            var exists = computer.OS.FS.FileExists(path) || computer.OS.FS.DirectoryExists(path);

            if (exists)
            {
                string dir = "";
                if (computer.OS.FS.FileExists(path))
                {
                    Runtime.GetWindow(computer).OpenApp(new TextEditor(computer, path));
                }
                else
                {
                    dir = path;
                }

                computer.OS.FS.ChangeDirectory(dir);
            }
        }

        private void UpPressed(object sender, RoutedEventArgs e)
        {
            computer.OS.FS.ChangeDirectory("..");
            UpdateView();
        }

        private void ForwardPressed(object sender, RoutedEventArgs e)
        {
            UpdateView();
        }
    }
}
