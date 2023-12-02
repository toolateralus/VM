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

namespace Lemur.GUI
{
    /// <summary>
    /// Interaction logic for TextEditor.xaml
    /// </summary>
    public partial class TextEditor : UserControl
    {
        public string LoadedFile;
        internal string Contents;
        public static string? DesktopIcon => FileSystem.GetResourcePath("texteditor.png");

        public MarkdownViewer? mdViewer;

        

        public TextEditor(string path) : this()
        {
            LoadFile(path);
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
                
                textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(extension);

                Contents = File.ReadAllText(path);
                textEditor.Text = Contents;
            }
        }

        private void RunMarkdownViewer(string path)
        {
            var wnd = Computer.Current.Window;
            mdViewer = new MarkdownViewer();
            Contents = File.ReadAllText(path);
            mdViewer.RenderMarkdown(Contents);
            Computer.Current?.OpenApp(mdViewer, "Markdown Renderer");
        }


        public TextEditor()
        {
            InitializeComponent();
            SearchPanel.Install(textEditor);
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
        private void RenderMD_Click(object sender, RoutedEventArgs e)
        {
            mdViewer = new MarkdownViewer();
            mdViewer.RenderMarkdown(Contents);
            Computer.Current.OpenApp(mdViewer, "Markdown Renderer");
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
            var cmd = new CommandPrompt();

            cmd.LateInit(Computer.Current);

            var jsEngine = new Engine(Computer.Current);

            Computer.Current.OpenApp(cmd, engine: jsEngine);

            await jsEngine.Execute(string.IsNullOrEmpty(textEditor.Text) ? "print('You must provide some javascript to execute...')" : textEditor.Text);
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            textEditor.Visibility ^= Visibility.Hidden;
            prefsWindow.Visibility ^= Visibility.Hidden;
        }

        private void DocTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
