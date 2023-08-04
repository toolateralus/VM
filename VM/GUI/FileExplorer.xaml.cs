﻿using System;
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
using VM.OPSYS;

namespace VM.GUI
{
    /// <summary>
    /// Interaction logic for FileExplorer.xaml
    /// </summary>
    public partial class FileExplorer : UserControl
    {
        ObservableCollection<string> FileViewerData = new();
        Dictionary<string, string> OriginalPaths = new();
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
                SearchBar.Text = OS.Current.FileSystem.CurrentDirectory;
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

            var fileNames = OS.Current.FileSystem.DirectoryListing();
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
            OS.Current.FileSystem.NewFile(SearchBar.Text);
            UpdateView();

        }

        private void AddDirectory_Click(object sender, RoutedEventArgs e)
        {
            OS.Current.FileSystem.NewFile(SearchBar.Text, true);
            UpdateView();

        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            OS.Current.FileSystem.DeleteFile(SearchBar.Text);
            UpdateView();

        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            Notifications.Now(OS.Current.FileSystem.CurrentDirectory);
        }

        private void BackPressed(object sender, RoutedEventArgs e)
        {

            if (OS.Current.FileSystem.History.Count == 0)
            {
                Notifications.Now("No file or directory to go back to.");
            }

            OS.Current.FileSystem.ChangeDirectory(OS.Current.FileSystem.History.Pop());
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
            UpdateView();
        }

        private void ForwardPressed(object sender, RoutedEventArgs e)
        {
            UpdateView();
        }
    }
}
