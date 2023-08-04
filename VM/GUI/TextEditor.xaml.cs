using System.IO;
using System.Windows;
using System.Windows.Controls;
using VM.OPSYS;

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
        public bool open;
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

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.GetPCWindow(computer).Open(new FileExplorer(computer));
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(LoadedFile, input.Text);
        }
    }
}
