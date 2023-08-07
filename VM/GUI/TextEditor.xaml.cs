using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using VM.OS;

namespace VM.GUI
{
    /// <summary>
    /// Interaction logic for TextEditor.xaml
    /// </summary>
    public partial class TextEditor : UserControl
    {
        Computer computer;
        public string LoadedFile;
        public string Contents;
        public TextEditor(Computer pc, string path)
        {
            InitializeComponent();
            computer = pc;
            LoadedFile = path;
            
            if (File.Exists(path))
            {
                Contents = File.ReadAllText(path);
                input.Text = Contents;
            }
        }
        public void LateInit(Computer pc)
        {
            computer = pc;
        }

        public TextEditor()
        {
            InitializeComponent();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            FileExplorer fileExplorer = new FileExplorer();
            fileExplorer.LateInit(computer);
            Runtime.GetWindow(computer).Open(fileExplorer);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(LoadedFile))
            {
                var dialog = new SaveFileDialog();
                dialog.InitialDirectory = computer.OS.FS_ROOT;
                dialog.FileName = "New";
                dialog.DefaultExt = ".js";

                bool? dlg = dialog.ShowDialog();

                if (dlg.HasValue && dlg.Value)
                {
                    LoadedFile = dialog.FileName;

                    File.WriteAllText(LoadedFile, input.Text);
                }
            }
             File.WriteAllText(LoadedFile, input.Text);
        }
        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            await computer.OS.JavaScriptEngine.Execute(input.Text);
        }
    }
}
