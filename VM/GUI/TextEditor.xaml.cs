using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using VM;

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
        public static string? DesktopIcon => Runtime.GetResourcePath("texteditor.png");

        public MarkdownViewer? mdViewer;

        public TextEditor(Computer pc, string path)
        {
            InitializeComponent();
            computer = pc;
            LoadedFile = path;

            LoadFile(path);
            // change the highlighting based on file extension that's opened
        }

        private void LoadFile(string path)
        {
            if (File.Exists(path))
            {
                string? extension = System.IO.Path.GetExtension(path)?.ToLower();

                if (extension == null)
                    return;
                input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(extension);
                Contents = File.ReadAllText(path);
                input.Text = Contents;
            }
        }

        private void RunMarkdownViewer(string path)
        {
            var wnd = computer.Window;
            mdViewer = new MarkdownViewer();
            Contents = File.ReadAllText(path);
            mdViewer.RenderMarkdown(Contents);
            wnd?.OpenApp(mdViewer, "Markdown Renderer");
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

            computer.Window?.OpenApp(fileExplorer);

            fileExplorer.OnNavigated += (file) =>
            {
                LoadFile(file);
            };
        }
        private void RenderMD_Click(object sender, RoutedEventArgs e)
        {
            mdViewer?.RenderMarkdown(Contents ?? "## no markdown found");
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(LoadedFile))
            {
                var dialog = new SaveFileDialog();
                dialog.InitialDirectory = computer.FS_ROOT;

                dialog.FileName = "New";
                dialog.DefaultExt = ".js";

                bool? dlg = dialog.ShowDialog();

                if (dlg.HasValue && dlg.Value)
                {
                    LoadedFile = dialog.FileName;

                    File.WriteAllText(LoadedFile, input.Text);
                }
            }

            if (string.IsNullOrEmpty(LoadedFile))
                return;

            File.WriteAllText(LoadedFile, input.Text);
        }
        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            await computer.JavaScriptEngine.Execute(input.Text);
        }
    }
}
