using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            CompositionTarget.Rendering += Updater;
        }

        private void Updater(object? sender, EventArgs e)
        {
            FileViewerData.Clear();

            var fileNames = OS.Current.FileSystem.SerializeCurrentDirectory();

            foreach (var file in fileNames)
            {
                FileViewerData.Add(file);
            }
        }
        public void Navigate()
        {
            string path = SearchBar.Text;
            OS.Current.FileSystem.ChangeDirectory(path);

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
            
        }

    }
}
