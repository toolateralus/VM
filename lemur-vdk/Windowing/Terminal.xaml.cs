using Lemur.JS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Lemur;
using Lemur.FS;
using Lemur.Windowing;
using System.IO;
using Newtonsoft.Json;
using System.Security.Cryptography.Xml;
using Lemur.JS.Embedded;

namespace Lemur.GUI
{
    public partial class Terminal : UserControl
    {
        enum Interpreter
        {
            Terminal,
            JavaScript,
        }
        internal Engine? Engine;
        private List<string> commandHistory = [];
        private int historyIndex = -1;
        private string tempInput = "";
        public static string? DesktopIcon => FileSystem.GetResourcePath("terminal.png");

        public Action<string> OnSend { get; internal set; }
        public ResizableWindow Window { get; private set; }
        public bool ProcessReading { get; internal set; }

        public static string? LastSentInput;
        string LastSentBuffer = "";
        public Terminal()
        {
            InitializeComponent();

            PreviewKeyDown += terminal_PreviewKeyDown;

            DrawTextBox("type 'help' for commands, \nor enter any valid single-line java script to interact with the environment. \n");
            input.Focus();

            output.TextChanged += Output_TextChanged;



            if (FileSystem.GetResourcePath("history.txt") is string path && path != "")
            {
                var jArray = JsonConvert.DeserializeObject<List<string>>(FileSystem.Read(path));
                commandHistory = jArray ?? [];
            }

        }

        private void Output_TextChanged(object? sender, EventArgs e)
        {
            output.ScrollToLine(output.Text.Length);
        }

        public void DrawTextBox(string content)
        {
            List<string> contentLines = content.Split('\n').ToList();
            int maxContentWidth = GetMaxContentWidth(contentLines);
            int boxWidth = maxContentWidth + 6; // Account for box characters

            void DrawBoxTop()
            {
                output.AppendText("\n╔");
                for (int i = 0; i < boxWidth; ++i)
                {
                    output.AppendText("═");
                }
                output.AppendText("╗\n");
            }

            void DrawBoxBottom()
            {
                output.AppendText("╚");
                for (int i = 0; i < boxWidth; ++i)
                {
                    output.AppendText("═");
                }
                output.AppendText("╝");
            }

            DrawBoxTop();

            foreach (string line in contentLines)
            {
                output.AppendText("║" + PadCenter(line, boxWidth) + "║\n");
            }

            DrawBoxBottom();
        }

        private int GetMaxContentWidth(List<string> contentLines)
        {
            int maxWidth = 0;
            foreach (string line in contentLines)
            {
                maxWidth = Math.Max(maxWidth, line.Length);
            }
            return maxWidth;
        }

        private string PadCenter(string text, int width)
        {
            int padding = (width - text.Length) / 2;
            return text.PadLeft(padding + text.Length).PadRight(width);
        }

        public void LateInit(Computer computer, ResizableWindow rsz)
        {
            Engine ??= new();
            Window = rsz;

            rsz.OnAppClosed += () =>
            {
                var json = JsonConvert.SerializeObject(commandHistory, Formatting.Indented);
                FileSystem.Write("system/history.txt", json);
            };
        }


        private async void terminal_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            
            switch (e.Key)
            {
                

                case Key.T:
                    if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                        return;

                    // todo: fix up temp file.
                    var text = input.Text;
                    var path = FileSystem.Root + "/home/ide/temp.js";
                    File.WriteAllText(path, text + "\n this file can be found at 'computer/home/ide/temp.js'");
                    var textEditor = new Texed(path);
                    Computer.Current.OpenApp(textEditor, "temp.js", Computer.GetNextProcessID());
                    break;


                case Key.Enter:
                case Key.F5:
                    await Send(e).ConfigureAwait(true);
                    break;

                case Key.Up:
                    if (historyIndex == -1)
                    {
                        tempInput = input.Text;
                    }
                    if (historyIndex < commandHistory.Count - 1)
                    {
                        historyIndex++;
                        input.Text = commandHistory[commandHistory.Count - 1 - historyIndex];
                    }
                    break;
                case Key.Down:
                    if (historyIndex == -1)
                    {
                        tempInput = input.Text;
                    }
                    if (historyIndex > 0)
                    {
                        historyIndex--;
                        input.Text = commandHistory[commandHistory.Count - 1 - historyIndex];
                    }
                    break;
            }
        }

        private async Task Send(KeyEventArgs? e)
        {
            if (e != null && e.RoutedEvent != null)
                e.Handled = true;

            string inputText = input.Text;

            OnSend?.Invoke(inputText);

            // for term.read
            if (ProcessReading)
            {
                input.Text = inputText;
                return;
            }

            if (commandHistory.Count > 100)
                commandHistory.RemoveAt(0);
            

            var outputText = output.Text;

            if (string.IsNullOrEmpty(inputText))
            {
                Notifications.Now("Invalid input");
                return;
            }

            if (commandHistory.Contains(inputText))
                commandHistory.RemoveAll(i => i == inputText);

            commandHistory.Add(inputText);

            
            
            switch ((Interpreter)interpreterBox.SelectedIndex)
            {
                // terminal
                case Interpreter.Terminal:
                    var success = Computer.Current.CmdLine.TryCommand(inputText);

                    if (!success)
                    {
                        Notifications.Now($"terminal::failure {inputText} : command not found.");
                        return;
                    }

                    break;

                case Interpreter.JavaScript:
                    await ExecuteJavaScript(code: inputText, timeout: 50_000).ConfigureAwait(true);
                    break;
            }
            

            if (output.Text == outputText)
                output.AppendText("\n done.");

            input.Clear();

            LastSentInput = LastSentBuffer;
            LastSentBuffer = output.Text;
        }


        private async Task ExecuteJavaScript(string code, int timeout = int.MaxValue)
        {
            try
            {
                using var cts = new CancellationTokenSource();

                var executionTask = Engine?.Execute(code, cts.Token) ?? throw new InvalidOperationException("Couldn't get an execution task from the engine.");

                var timeoutTask = Task.Delay(timeout);

                var completedTask = await Task.WhenAny(executionTask, timeoutTask).ConfigureAwait(true);

                if (completedTask == timeoutTask)
                {
                    await cts.CancelAsync().ConfigureAwait(true);
                    output.AppendText("\nExecution timed out.");
                }
                else
                {
                    var result = await executionTask.ConfigureAwait(true);
                    string? output = result?.ToString();

                    if (!string.IsNullOrEmpty(output))
                        this.output.AppendText("\n" + output);
                }
            }
            catch (Exception ex)
            {
                output.Text += ex.Message + Environment.NewLine;
            }
        }


        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            input.Focus();
        }

        private void ClearButtonClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            Computer.Current.CmdLine.TryCommand("clear");
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {

        }
    }
}
