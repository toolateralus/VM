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
            CompositionTarget.Rendering += Updater;
            FileBox.MouseDoubleClick += Navigate;
            KeyDown += FileExplorer_KeyDown;
        }

        private void FileExplorer_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                #region Special
                case Key.LWin:
                    break;
                case Key.RWin:
                    break;
                case Key.Apps:
                    break;
                case Key.Sleep:
                    break;
                case Key.NumPad0:
                    break;
                case Key.NumPad1:
                    break;
                case Key.NumPad2:
                    break;
                case Key.NumPad3:
                    break;
                case Key.NumPad4:
                    break;
                case Key.NumPad5:
                    break;
                case Key.NumPad6:
                    break;
                case Key.NumPad7:
                    break;
                case Key.NumPad8:
                    break;
                case Key.NumPad9:
                    break;
                case Key.Multiply:
                    break;
                case Key.Add:
                    break;
                case Key.Separator:
                    break;
                case Key.Subtract:
                    break;
                case Key.Decimal:
                    break;
                case Key.Divide:
                    break;
                case Key.F1:
                    break;
                case Key.F2:
                    break;
                case Key.F3:
                    break;
                case Key.F4:
                    break;
                case Key.F5:
                    break;
                case Key.F6:
                    break;
                case Key.F7:
                    break;
                case Key.F8:
                    break;
                case Key.F9:
                    break;
                case Key.F10:
                    break;
                case Key.F11:
                    break;
                case Key.F12:
                    break;
                case Key.NumLock:
                    break;
                case Key.Scroll:
                    break;
                case Key.LeftShift:
                    break;
                case Key.RightShift:
                    break;
                case Key.LeftCtrl:
                    break;
                case Key.RightCtrl:
                    break;
                case Key.LeftAlt:
                    break;
                case Key.RightAlt:
                    break;
                case Key.BrowserHome:
                    break;
                case Key.VolumeMute:
                    break;
                case Key.VolumeDown:
                    break;
                case Key.VolumeUp:
                    break;
                case Key.MediaNextTrack:
                    break;
                case Key.MediaPreviousTrack:
                    break;
                case Key.MediaStop:
                    break;
                case Key.MediaPlayPause:
                    break;
                case Key.Oem1:
                    break;
                case Key.OemPlus:
                    break;
                case Key.OemComma:
                    break;
                case Key.OemMinus:
                    break;
                case Key.OemPeriod:
                    break;
                case Key.None:
                    break;
                case Key.Cancel:
                    break;
                case Key.Back:
                    break;
                case Key.Tab:
                    break;
                case Key.LineFeed:
                    break;
                case Key.Clear:
                    break;
                case Key.Enter:
                    break;
                case Key.Pause:
                    break;
                case Key.Capital:
                    break;
                case Key.Space:
                    break;
                case Key.PageUp:
                    break;
                case Key.Next:
                    break;
                case Key.End:
                    break;
                case Key.Home:
                    break;
                case Key.Left:
                    break;
                case Key.Up:
                    break;
                case Key.Right:
                    break;
                case Key.Down:
                    break;
                case Key.Select:
                    break;
                case Key.Print:
                    break;
                case Key.Escape:
                    Keyboard.ClearFocus();
                    SearchBar.Text = OS.Current.FileSystem.CurrentDirectory;
                    break;
                case Key.PrintScreen:
                    break;
                case Key.Insert:
                    break;
                case Key.Delete:
                    break;
                case Key.Help:
                    break;
#endregion
                #region Numbers
                case Key.D0:
                    break;
                case Key.D1:
                    break;
                case Key.D2:
                    break;
                case Key.D3:
                    break;
                case Key.D4:
                    break;
                case Key.D5:
                    break;
                case Key.D6:
                    break;
                case Key.D7:
                    break;
                case Key.D8:
                    break;
                case Key.D9:
                    break;
                #endregion
                #region Alphabetical
                case Key.A:
                    break;
                case Key.B:
                    break;
                case Key.C:
                    break;
                case Key.D:
                    break;
                case Key.E:
                    break;
                case Key.F:
                    break;
                case Key.G:
                    break;
                case Key.H:
                    break;
                case Key.I:
                    break;
                case Key.J:
                    break;
                case Key.K:
                    break;
                case Key.L:
                    break;
                case Key.M:
                    break;
                case Key.N:
                    break;
                case Key.O:
                    break;
                case Key.P:
                    break;
                case Key.Q:
                    break;
                case Key.R:
                    break;
                case Key.S:
                    break;
                case Key.T:
                    break;
                case Key.U:
                    break;
                case Key.V:
                    break;
                case Key.W:
                    break;
                case Key.X:
                    break;
                case Key.Y:
                    break;
                case Key.Z:
                    break;
                #endregion
            }
        }

        private void Navigate(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            string path = SearchBar.Text;
            OS.Current.FileSystem.ChangeDirectory(path);
        }

        private void Updater(object? sender, EventArgs e)
        {
            FileViewerData.Clear();

            var fileNames = OS.Current.FileSystem.SerializeCurrentDirectory();

            foreach (var file in fileNames)
            {
                if (Directory.Exists(file) && !File.Exists(file))
                {
                    FileViewerData.Add("📁 " + file.Split('\\').LastOrDefault("???"));
                } else
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
            
        }

        private void BackPressed(object sender, RoutedEventArgs e)
        {

        }

        private void SearchPressed(object sender, RoutedEventArgs e)
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
                } else
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
