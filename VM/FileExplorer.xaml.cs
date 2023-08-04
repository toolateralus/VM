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

namespace VM
{
    /// <summary>
    /// Interaction logic for FileExplorer.xaml
    /// </summary>
    public partial class FileExplorer : UserControl
    {
        ObservableCollection<string> FileViewerData = new();
        public FileExplorer()
        {
            InitializeComponent();
            FileBox.ItemsSource = FileViewerData;
            FileBox.MouseDoubleClick += (sender, @event) => Navigate();
            KeyDown += FileExplorer_KeyDown;
        }

        private void FileExplorer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Keyboard.ClearFocus();
                SearchBar.Text = OS.Current.FileSystem.CurrentDirectory;
                UpdateView();
            }
            if (e.Key == Key.Enter)
            {
                Navigate();
            }
        }

        

        private void UpdateView()
        {
            FileViewerData.Clear();

            var fileNames = OS.Current.FileSystem.DirectoryListing();

            foreach (var file in fileNames)
            {
                if (Directory.Exists(file) && !File.Exists(file))
                {
                    FileViewerData.Add("📁 " + file.Split('\\').LastOrDefault("???"));
                } 
                else
                {
                    FileViewerData.Add("📄 " + file.Split('\\').LastOrDefault("???"));
                }
            }
        }
        
        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            OS.Current.FileSystem.NewFile(SearchBar.Text);
        }

        private void AddDirectory_Click(object sender, RoutedEventArgs e)
        {
            OS.Current.FileSystem.NewFile(SearchBar.Text, true);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            OS.Current.FileSystem.DeleteFile(SearchBar.Text);
        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            Notifications.Now(OS.Current.FileSystem.CurrentDirectory);
        }

        private void BackPressed(object sender, RoutedEventArgs e)
        {
            OS.Current.FileSystem.ChangeDirectory("..");
        }

        private void SearchPressed(object sender, RoutedEventArgs e)
        {
            Navigate();

        }

        private void Navigate()
        {
            var path = SearchBar.Text;

            if (Command.TryCommand(path))
            {
                return;
            }

            var exists = OS.Current.FileSystem.FileExists(path) || OS.Current.FileSystem.DirectoryExists(path);

            if (exists)
            {
                string dir = "";
                if (OS.Current.FileSystem.FileExists(path))
                {
                    if (System.IO.Path.GetDirectoryName(path) is string _dir)
                    {
                        dir = _dir;
                    }
                }
                else
                {
                    dir = path;
                }

                OS.Current.FileSystem.ChangeDirectory(dir);
            }
        }

        private void UpPressed(object sender, RoutedEventArgs e)
        {
            OS.Current.FileSystem.ChangeDirectory("..");
        }

        private void ForwardPressed(object sender, RoutedEventArgs e)
        {

        }
    }
}
