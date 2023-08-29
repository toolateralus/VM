using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using VM;
using ICSharpCode.AvalonEdit.Search;
using System;
using VM.FS;

namespace VM.GUI
{
    /// <summary>
    /// Interaction logic for TextEditor.xaml
    /// </summary>
    public partial class TextEditor : UserControl
    {
        Computer Computer;
        public string LoadedFile;
        public string Contents;
        public static string? DesktopIcon => FileSystem.GetResourcePath("texteditor.png");

        public MarkdownViewer? mdViewer;

        public TextEditor(Computer pc, string path)
        {
            InitializeComponent();
            Computer = pc;
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
            var wnd = Computer.Window;
            mdViewer = new MarkdownViewer();
            Contents = File.ReadAllText(path);
            mdViewer.RenderMarkdown(Contents);
            Computer?.OpenApp(mdViewer, "Markdown Renderer");
        }

        public void LateInit(Computer pc)
        {
            Computer = pc;
        }

        public TextEditor()
        {
            InitializeComponent();
            SearchPanel.Install(input);
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            FileExplorer fileExplorer = new FileExplorer();
            fileExplorer.LateInit(Computer);

            Computer.OpenApp(fileExplorer);

            fileExplorer.OnNavigated += (file) =>
            {
                LoadFile(file);
            };
        }
        private void RenderMD_Click(object sender, RoutedEventArgs e)
        {
            var wnd = Computer.Window;
            mdViewer = new MarkdownViewer();
            mdViewer.RenderMarkdown(Contents);
            Computer.OpenApp(mdViewer, "Markdown Renderer");
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(LoadedFile))
            {
                var dialog = new SaveFileDialog();
                dialog.InitialDirectory = Computer.FS_ROOT;

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
            await Computer.JavaScriptEngine.Execute(input.Text);
        }
    }
}
