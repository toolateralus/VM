using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Search;
using System;
using Lemur.FS;
using System.Linq;
using Lemur.JS;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using System.Collections.Generic;
using OpenTK.Platform.Windows;

namespace Lemur.GUI
{
    /// <summary>
    /// Interaction logic for TextEditor.xaml
    /// </summary>
    public partial class TextEditor : UserControl
    {
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
        public static string? DesktopIcon => FileSystem.GetResourcePath("texteditor.png");

        public MarkdownViewer? mdViewer;

        /// <summary>
        /// Loads a file from path and opens a new text editor for that file.
        /// </summary>
        /// <param name="path"></param>
        public TextEditor(string path) : this()
        {
            LoadFile(path);
        }
        /// <summary>
        /// Base constructor, you probably do not want to use this.
        /// </summary>
        public TextEditor()
        {
            Contents = "Load a file";
            LoadedFile = "newfile.txt";
            InitializeComponent();
            SearchPanel.Install(textEditor);


            shTypeBox.ItemsSource = LanguageOptions;
            themeBox.ItemsSource = new List<string>() { "Light", "Dark" };

            var config = Computer.Current.Config;

            // dark is 1, light is 0;
            if (config.ContainsKey("TEXT_EDITOR_THEME"))
                themeBox.SelectedIndex = config.Value<string>("TEXT_EDITOR_THEME") == "Dark" ? 1 : 0;
            else
            {
                config["TEXT_EDITOR_THEME"] = "Light";
                themeBox.SelectedIndex = 0;
            }


            shTypeBox.SelectedIndex = 1;
        }
        public void LateInit(Computer c)
        {
            // neccesary, deprecated
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            var ctrl = Keyboard.IsKeyDown(Key.LeftCtrl);

            if (ctrl && e.Key == Key.S)
                Save();
            else if (ctrl && e.Key == Key.OemPlus)
                textEditor.FontSize += 1;
            else if (ctrl && e.Key == Key.OemMinus && textEditor.FontSize > 0)
                textEditor.FontSize -= 1;
            else if (e.Key == Key.F5)
            {
                RunButton_Click(null!, null!);
            }
            Contents = textEditor.Text;
        }
        private void LoadFile(string path)
        {
            path = FileSystem.GetResourcePath(path);
            LoadedFile = path;
            if (File.Exists(path))
            {
                string? extension = System.IO.Path.GetExtension(path)?.ToLower();

                if (extension == null)
                    return;

                SetSyntaxHighlighting(extension);

                Contents = File.ReadAllText(path);
                textEditor.Text = Contents;
            }
        }

        private void SetSyntaxHighlighting(string? extension)
        {
            var highlighter = HighlightingManager.Instance.GetDefinitionByExtension(extension);

            if (highlighter != null)
                textEditor.SyntaxHighlighting = highlighter;
            else Notifications.Now($"No syntax highlighting found for {extension}");
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            FileExplorer fileExplorer = new FileExplorer();

            Computer.Current.OpenApp(fileExplorer);

            fileExplorer.OnNavigated += (file) =>
            {
                LoadFile(file);
            };
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        internal void Save()
        {
            Notifications.Now(LoadedFile);
            if (!File.Exists(LoadedFile))
            {
                var dialog = new SaveFileDialog();
                dialog.InitialDirectory = FileSystem.Root;

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
                File.WriteAllText(LoadedFile, textEditor.Text);
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
            }

            Notifications.Now($"Saved {textEditor.LineCount} lines and {textEditor.Text.Length} characters to ...\\{LoadedFile.Split('\\').LastOrDefault()}");
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            var element = LanguageOptions.ElementAt(shTypeBox.SelectedIndex);
            if (element.Value == ".md")
            {
                mdViewer = new MarkdownViewer();
                mdViewer.RenderMarkdown(Contents);
                Computer.Current.OpenApp(mdViewer, "Markdown Renderer");
            }
            else if (element.Value == ".js") { 
                var cmd = new CommandPrompt();

                cmd.LateInit(Computer.Current);

                var jsEngine = new Engine(Computer.Current);

                Computer.Current.OpenApp(cmd, engine: jsEngine);

                await jsEngine.Execute(string.IsNullOrEmpty(textEditor.Text) ? "print('You must provide some javascript to execute...')" : textEditor.Text);

            } else
            {
                Notifications.Now($"Can't run {element.Value}");
            }
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            textEditor.Visibility ^= Visibility.Hidden;
            prefsWindow.Visibility ^= Visibility.Hidden;
        }

        private void DocTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cB)
            {

                var selected = cB.SelectedItem.ToString();
                var extension = selected[selected.IndexOf(',')..].Replace(",","").Replace("]","").Replace(" ", "");
                SetSyntaxHighlighting(extension);
            }
        }

        private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cB)
            {
                if (cB.SelectedIndex == 0)
                {
                    textEditor.Background = System.Windows.Media.Brushes.White;
                    textEditor.Foreground = System.Windows.Media.Brushes.Black;
                    Computer.Current.Config["TEXT_EDITOR_THEME"] = "Light";
                }
                else
                {
                    textEditor.Background = System.Windows.Media.Brushes.Black;
                    textEditor.Foreground = System.Windows.Media.Brushes.White;
                    Computer.Current.Config["TEXT_EDITOR_THEME"] = "Dark";
                }
            }
        }
    }
}
