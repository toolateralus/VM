using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit;
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
        public static string? DesktopIcon => Runtime.GetResourcePath("texteditor.png");

        public MarkdownViewer? mdViewer;

        public TextEditor(Computer pc, string path)
        {
            InitializeComponent();
            computer = pc;
            LoadedFile = path;

            if (File.Exists(path))
            {
                string extension = System.IO.Path.GetExtension(path)?.ToLower();

                switch (extension)
                {
                    case ".cs":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                        break;
                    case ".js":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScriptCustom");
                        break;
                    case ".htm":
                    case ".html":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("HTML");
                        break;
                    case ".asp":
                    case ".aspx":
                    case ".asax":
                    case ".asmx":
                    case ".ascx":
                    case ".master":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("ASP/XHTML");
                        break;
                    case ".boo":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Boo");
                        break;
                    case ".atg":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Coco");
                        break;
                    case ".css":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("CSS");
                        break;
                    case ".c":
                    case ".h":
                    case ".cc":
                    case ".cpp":
                    case ".hpp":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C++");
                        break;
                    case ".java":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Java");
                        break;
                    case ".patch":
                    case ".diff":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Patch");
                        break;
                    case ".ps1":
                    case ".psm1":
                    case ".psd1":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("PowerShell");
                        break;
                    case ".php":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("PHP");
                        break;
                    case ".py":
                    case ".pyw":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Python");
                        break;
                    case ".tex":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("TeX");
                        break;
                    case ".sql":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("TSQL");
                        break;
                    case ".vb":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("VB");
                        break;
                    case ".xml":
                    case ".xsl":
                    case ".xslt":
                    case ".xsd":
                    case ".manifest":
                    case ".config":
                    case ".addin":
                        // ... add more XML-related extensions ...
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
                        break;
                    case ".md":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("MarkDown");
                        RunMarkdownViewer(path);
                        break;
                    case ".json":
                        input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Json");
                        break;
                    // Add more cases for other supported file types
                    default:
                        // If none of the above extensions match, use a default syntax highlighting (if applicable)
                        // For example: input.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
                        break;
                }

                Contents = File.ReadAllText(path);
                input.Text = Contents;
            }
            // change the highlighting based on file extension that's opened
        }

        private void RunMarkdownViewer(string path)
        {
            var wnd = Runtime.GetWindow(computer);
            mdViewer = new MarkdownViewer();
            Contents = File.ReadAllText(path);
            mdViewer.RenderMarkdown(Contents);
            wnd.OpenApp(mdViewer, "Markdown Renderer");
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
            Runtime.GetWindow(computer).OpenApp(fileExplorer);
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
