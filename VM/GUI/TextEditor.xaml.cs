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
using System.Windows.Input;
using System.Linq;
using VM.JS;

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
            Runtime.LoadCustomSyntaxHighlighting();
            LoadFile(path);
            // change the highlighting based on file extension that's opened

            
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            var ctrl = Keyboard.IsKeyDown(Key.LeftCtrl);

            if (ctrl && e.Key == Key.S)
                Save();
            else if (ctrl && e.Key == Key.OemPlus)
                input.FontSize += 1;
            else if (ctrl && e.Key == Key.OemMinus && input.FontSize > 0)
                input.FontSize -= 1;
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
            mdViewer = new MarkdownViewer();
            mdViewer.RenderMarkdown(Contents);
            Computer.OpenApp(mdViewer, "Markdown Renderer");
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }


        private void Save()
        {
            if (!File.Exists(LoadedFile))
            {
                var dialog = new SaveFileDialog();
                dialog.InitialDirectory = Computer.FS_ROOT;

                dialog.FileName = "untitled";
                dialog.DefaultExt = ".js";

                bool? dlg = dialog.ShowDialog();

                if (!dlg.HasValue || !dlg.Value)
                {
                    Notifications.Now("Must pick valid file name");
                    return;
                }
                LoadedFile = dialog.FileName;
            }

            if (string.IsNullOrEmpty(LoadedFile))
            {
                Notifications.Now("Error: invalid file name.");
                return;
            }
            try
            {
                File.WriteAllText(LoadedFile, input.Text);
            } catch (Exception e)
            {
                Notifications.Exception(e);
            }

            Notifications.Now($"Saved {input.LineCount} lines and {input.Text.Length} characters to ...\\{LoadedFile.Split('\\').LastOrDefault()}");
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            var cmd = new CommandPrompt();
            cmd.LateInit(Computer.Current);

            var jsEngine = new JavaScriptEngine(Computer.Current);

            Computer.Current.OpenApp(cmd, engine: jsEngine);

            await jsEngine.Execute(string.IsNullOrEmpty(input.Text) ? "print('You must provide some javascript to execute...')" : input.Text);
        }
    }
}
