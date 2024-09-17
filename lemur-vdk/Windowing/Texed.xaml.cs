using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using Lemur.FS;
using Lemur.JS;
using Lemur.Windowing;
using Microsoft.Win32;
using OpenTK.Graphics.ES11;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Lemur.GUI {
    /// <summary>
    /// The in app text editor / IDE. much of the great behavior comes from the use of AvaloniaEdit's TextEditor control.
    /// it does a ton of heavy lifting.
    /// 
    /// Create, Load, Edit, & Save text files.
    /// Syntax Highlighting & a few IDE functionalities.
    /// support for around 20 languages, listing a few : JavaScript, C#, C++, JSON, Markdown, and XML/XAML,
    /// </summary>
    public partial class Texed : UserControl {
        public string LoadedFile;
        internal string Contents;
        public Dictionary<string, string> LanguageOptions = new()
        {
            { "MarkDown", ".md" },
            { "JavaScript", ".js" },
            { "C#", ".cs" },
            { "HTML", ".html" },
            { "ASP/XHTML", ".aspx" },
            { "XmlDoc", ".xml" },
            { "Boo", ".boo" },
            { "Coco", ".coco" },
            { "CSS", ".css" },
            { "C++", ".cpp" },
            { "Java", ".java" },
            { "Patch", ".patch" },
            { "PowerShell", ".ps1" },
            { "PHP", ".php" },
            { "Python", ".py" },
            { "TeX", ".tex" },
            { "TSQL", ".tsql" },
            { "VB", ".vb" },
            { "XML", ".xml" },
            { "Json", ".json" },
        };
        // reflection grabs this later.
        public static string? DesktopIcon => FileSystem.GetResourcePath("texed.png");
        public MarkdownViewer? mdViewer;
        public Terminal? terminal;
        private Computer computer;
        public Texed(string path, bool renderMarkdown) : this(path) {
            if (renderMarkdown) {
                mdViewer = new MarkdownViewer();
                mdViewer.RenderMarkdown(Contents);
                Computer.Current.PresentGUI(mdViewer, "md.app", Computer.Current.ProcessManager.GetNextProcessID());
            }
        }
        /// <summary>
        /// Loads a file from path and opens a new text editor for that file.
        /// </summary>
        /// <param name="path"></param>
        public Texed(string path) : this() {
            LoadFile(path);
        }
        /// <summary>
        /// Base constructor, you probably do not want to use this.
        /// </summary>
        public Texed() {
            Contents = "Load a file";
            LoadedFile = "newfile.txt";
            InitializeComponent();
            SearchPanel.Install(textEditor);

            shTypeBox.ItemsSource = LanguageOptions;
            themeBox.ItemsSource = new List<string>() { "Light", "Dark" };


            textEditor.TextArea.TextEntered += TextEntered;

            var fonts = Fonts.SystemFontFamilies;
            var list = new List<string>();
            foreach (var font in fonts) {
                list.Add(font.Source);
            }

            fontBox.ItemsSource = list;

            var config = Computer.Current.Config;

            // dark is 1, light is 0;
            if (config.ContainsKey("TEXT_EDITOR_THEME"))
                themeBox.SelectedIndex = config.Value<string>("TEXT_EDITOR_THEME") == "Dark" ? 1 : 0;
            else {
                config["TEXT_EDITOR_THEME"] = "Light";
                themeBox.SelectedIndex = 0;
            }

            if (config.ContainsKey("TEXT_EDITOR_FONT"))
                fontBox.SelectedIndex = fontBox.Items.IndexOf(config.Value<string>("TEXT_EDITOR_FONT"));
            else
                fontBox.SelectedIndex = fontBox.Items.IndexOf("Consolas");


            shTypeBox.SelectedIndex = 1;
        }

        Dictionary<string, List<string>> apiDocs = ApiDocParser.Parse();
        private CompletionWindow? GetCompletionWindow(string prefix) {
            var completionWindow = new CompletionWindow(textEditor.TextArea);
            var insertedSomething = false;
            IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
            if (prefix == "::") {
                insertedSomething = true;
                foreach (var (_class, methodList) in apiDocs) {
                    foreach (var methodInfo in methodList) {
                        data.Add(new MyCompletionData(methodInfo));
                    }
                }
            }
            if (prefix.Contains('.')) {
                var first = prefix.Split('.')[0];
                foreach (var (_class, methodList) in apiDocs) {
                    if (first == _class) {
                        insertedSomething = true;
                        foreach (var methodInfo in methodList) {
                            data.Add(new MyCompletionData(methodInfo));
                        }
                    }
                }
            }

            if (!insertedSomething) {
                return null;
            }
            return completionWindow;
        }

        public void LateInit(string _, Computer c, ResizableWindow win) {
            this.computer = c;
        }
        protected override void OnKeyUp(KeyEventArgs e) {
            var ctrl = Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl);

            if (ctrl && e.Key == System.Windows.Input.Key.S)
                Save();
            else if (ctrl && e.Key == System.Windows.Input.Key.OemPlus)
                textEditor.FontSize += 1;
            else if (ctrl && e.Key == System.Windows.Input.Key.OemMinus && textEditor.FontSize > 0)
                textEditor.FontSize -= 1;
            else if (ctrl && e.Key == System.Windows.Input.Key.LeftShift && Keyboard.IsKeyDown(System.Windows.Input.Key.Tab)) {
                // we should have 'tabs' in future, allowing for several open documents at once.
                // this should be relatively easy.
            }
            else if (e.Key == System.Windows.Input.Key.F5) {
                RunButton_Click(null!, null!);
            }
            Contents = textEditor.Text;
        }
        private void LoadFile(string path) {
            path = FileSystem.GetResourcePath(path);
            LoadedFile = path;
            if (File.Exists(path)) {
                string? extension = System.IO.Path.GetExtension(path)?.ToLower();

                if (extension == null)
                    return;

                if (extension == ".xaml.js")
                    extension = ".js";

                SetSyntaxHighlighting(extension);

                Contents = "Loading file.. please wait.";

                Task.Run(async () => {
                    Contents = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                    await Dispatcher.InvokeAsync(() => { textEditor.Text = Contents; });
                });

                textEditor.Text = Contents;
            }
        }
        private void SetSyntaxHighlighting(string? extension) {
            var highlighter = HighlightingManager.Instance.GetDefinitionByExtension(extension);

            // yes. this is a thing.
            if (extension == ".xaml")
                extension = ".xml";

            var index = LanguageOptions.Values.ToList().IndexOf(LanguageOptions.Values.FirstOrDefault(value => value == extension));

            if (index != -1)
                shTypeBox.SelectedIndex = index;

            if (highlighter != null)
                textEditor.SyntaxHighlighting = highlighter;
            else Notifications.Now($"No syntax highlighting found for {extension}");
        }
        private void LoadButton_Click(object sender, RoutedEventArgs e) {
            var fileExplorer = Explorer.LoadFilePrompt();

            var pid = computer.ProcessManager.GetNextProcessID();
            Computer.Current.PresentGUI(fileExplorer, "explorer.app", pid);
            var proc = Computer.Current.ProcessManager.GetProcess(pid);

            fileExplorer.OnNavigated += (file) => {
                LoadFile(file);
                proc.Terminate();
            };
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e) {
            Save();
        }
        internal void Save() {
            Notifications.Now(LoadedFile);
            if (!File.Exists(LoadedFile)) {
                var dialog = new SaveFileDialog();
                dialog.InitialDirectory = FileSystem.Root;

                dialog.FileName = "untitled";
                dialog.DefaultExt = ".js";

                bool? dlg = dialog.ShowDialog();

                if (!dlg.HasValue || !dlg.Value) {
                    Notifications.Now("Must pick valid file name");
                    return;
                }
                LoadedFile = dialog.FileName;
            }

            if (string.IsNullOrEmpty(LoadedFile)) {
                Notifications.Now("Error: invalid file name.");
                return;
            }
            try {
                File.WriteAllText(LoadedFile, textEditor.Text);
            }
            catch (Exception e) {
                Notifications.Exception(e);
            }

            Notifications.Now($"Saved {textEditor.LineCount} lines and {textEditor.Text.Length} characters to ...\\{LoadedFile.Split('\\').LastOrDefault()}");
        }
        private async void RunButton_Click(object sender, RoutedEventArgs e) {
            var fileExt = LanguageOptions.ElementAt(shTypeBox.SelectedIndex).Value;

            if (LoadedFile.Contains(".xaml.js"))
                fileExt = ".xaml.js";

            switch (fileExt) {
                case ".md":
                    mdViewer = new MarkdownViewer();
                    mdViewer.RenderMarkdown(Contents);
                    Computer.Current.PresentGUI(mdViewer, "md.app", computer.ProcessManager.GetNextProcessID());
                    break;

                case ".xaml.js":
                    var split = LoadedFile.Split('\\');
                    var name = "";

                    foreach (var line in split)
                        if (line.Contains(".app")) {
                            name = line;
                            break;
                        }

                    _ = await Computer.Current.JavaScript.Execute($"App.start('{name}')").ConfigureAwait(true);
                    break;

                case ".js":
                    if (terminal == null) {
                        terminal = new Terminal();
                        var jsEngine = new Engine(computer, "Terminal");
                        Computer.Current.PresentGUI(terminal, "cmd.app", computer.ProcessManager.GetNextProcessID(), engine: jsEngine);
                        terminal.Window.OnApplicationClose += delegate {
                            terminal = null;
                        };
                    }
                    var code = string.IsNullOrEmpty(textEditor.Text) ? "print('You must provide some javascript to execute...')" : textEditor.Text;

                    _ = Task.Run(async () => {
                        var task = terminal?.Engine?.Execute(code).ConfigureAwait(false);
                        if (task.HasValue)
                            await task.Value;
                    });

                    break;

                default:
                    Notifications.Now($"Can't run {fileExt}");
                    break;
            }

        }
        private void Preferences_Click(object sender, RoutedEventArgs e) {
            textEditor.Visibility ^= Visibility.Hidden;
            prefsWindow.Visibility ^= Visibility.Hidden;
        }
        private void DocTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sender is ComboBox cB) {

                var selected = cB.SelectedItem.ToString();
                var extension = selected[selected.IndexOf(',')..].Replace(",", "").Replace("]", "").Replace(" ", "");
                SetSyntaxHighlighting(extension);
            }
        }
        private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sender is ComboBox cB) {
                if (cB.SelectedIndex == 0) {
                    textEditor.Background = System.Windows.Media.Brushes.White;
                    textEditor.Foreground = System.Windows.Media.Brushes.Black;
                    Computer.Current.Config["TEXT_EDITOR_THEME"] = "Light";
                    Computer.SaveConfig(Computer.Current.Config.ToString());
                }
                else {
                    textEditor.Background = System.Windows.Media.Brushes.Black;
                    textEditor.Foreground = System.Windows.Media.Brushes.White;
                    Computer.Current.Config["TEXT_EDITOR_THEME"] = "Dark";
                    Computer.SaveConfig(Computer.Current.Config.ToString());
                }
            }
        }
        private void FontBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            textEditor.FontFamily =
                Fonts.SystemFontFamilies.
                    FirstOrDefault((f) => {
                        if (f.Source == fontBox.SelectedItem as string) {
                            Computer.Current.Config["TEXT_EDITOR_FONT"] = fontBox.SelectedItem as string;
                            Computer.SaveConfig(Computer.Current.Config.ToString());
                            return true;
                        }
                        return false;
                    }, new System.Windows.Media.FontFamily("Consolas"));
        }

        string prefix = "";
        private void TextEntered(object sender, TextCompositionEventArgs e) {

            if (e.Text == " ") {
                prefix = "";
            }
            else {
                prefix += e.Text;
            }

            var completionWindow = GetCompletionWindow(prefix);
            if (completionWindow != null) { 
                completionWindow.Show();
                completionWindow.Closed += delegate {
                    completionWindow = null;
                };
                prefix = "";
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == System.Windows.Input.Key.Back || e.Key == System.Windows.Input.Key.Delete || e.Key == System.Windows.Input.Key.Tab) {
                prefix = "";
            }
           
        }
    }
    public class MyCompletionData : ICompletionData {
        public MyCompletionData(string signature) {
            var index = signature.IndexOf('(');
            var first = signature[0..index];
            Text = first.Split(' ')[1];
            Description = signature;
        }
        public ImageSource Image {
            get { return null; }
        }

        public string Text { get; private set; }
        public object Content {
            get { return this.Text; }
        }
        public object Description { get; set; }
        public double Priority { get; set; }
        public void Complete(TextArea textArea, ISegment completionSegment,
            EventArgs insertionRequestEventArgs) {
            textArea.Document.Replace(completionSegment, this.Text);
        }
    }
}
