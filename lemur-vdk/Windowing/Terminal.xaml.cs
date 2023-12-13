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
using ICSharpCode.AvalonEdit.Search;
using ICSharpCode.AvalonEdit;

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

        public Action<string> OnTerminalSend { get; internal set; }
        public ResizableWindow Window { get; private set; }
        public bool IsReading { get; internal set; }

        public static string? LastSentInput;
        string LastSentBuffer = "";
        public Terminal()
        {
            InitializeComponent();

            PreviewKeyDown += terminal_PreviewKeyDown;

            output.AppendText("Interpreter Controls:\r\n----------------------\r\n\r\nCommon Shortcuts:\r\n- [Shift + Tab]: Toggle between interpreters.\r\n- [Ctrl + T]: Open a temporary JavaScript file with the input's contents.\r\n- [Ctrl + Shift + C]: End any process, including this one, and close the window.\r\n\r\nJavaScript Interpreter:\r\n------------------------\r\n\r\nShortcuts:\r\n- [Left Shift + Enter] or [F5]: Run the input code.\r\n\r\nTerminal Interpreter:\r\n---------------------\r\nShortcuts:\r\n-'help' command : type help into the input bar below and press enter. \n you will get a list of all possible commands and how to use many of them \r\n[Up Arrow / Down Arrow]: Navigate forward and backward in command history.\r\n  (Note: This feature may have occasional quirks but is generally reliable and saves history to a file on session begin and end.)\r\n");
            input.Focus();

            output.TextChanged += Output_TextChanged;



            if (FileSystem.GetResourcePath("history.txt") is string path && path != "")
            {
                var jArray = JsonConvert.DeserializeObject<List<string>>(FileSystem.Read(path));
                commandHistory = jArray ?? [];
            }

            SearchPanel.Install(output);
        }

        private void Output_TextChanged(object? sender, EventArgs e)
        {
            output.ScrollToLine(output.Text.Length);
        }

        public void LateInit(Computer _, ResizableWindow rsz)
        {
            Engine ??= new("Terminal");
            Window = rsz;

            rsz.OnApplicationClose += () =>
            {
                var json = JsonConvert.SerializeObject(commandHistory, Formatting.Indented);
                FileSystem.Write("system/history.txt", json);
            };
        }

        Dictionary<Interpreter, string> cachedInput = new (){
            { Interpreter.Terminal, ""},
            { Interpreter.JavaScript, ""},
        };
        private async void terminal_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            
            switch (e.Key)
            {
                case System.Windows.Input.Key.Tab:
                    // switching interpreters.
                    if (Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift))
                    {
                        interpreterBox.SelectedIndex = 1 - interpreterBox.SelectedIndex;
                        var i = (Interpreter)interpreterBox.SelectedIndex;
                        output.AppendText($"\nusing interpreter::{i}");

                        // switching to javascript;
                        if (i == Interpreter.JavaScript)
                        {
                            interpreterLabel.Content = "JavaScript";
                            cachedInput[Interpreter.Terminal] = input.Text;
                            input.Text = cachedInput[Interpreter.JavaScript];
                            input.MinHeight = 100;
                            input.Focus();
                        // switching to terminal
                        } else if (i == Interpreter.Terminal)
                        {
                            interpreterLabel.Content = "Terminal";
                            cachedInput[Interpreter.JavaScript] = input.Text;
                            input.Text = cachedInput[Interpreter.Terminal];
                            input.MinHeight = 0;
                            input.Focus();
                        }

                    }
                    break;

                case System.Windows.Input.Key.T:
                    if (!Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
                        return;

                    // todo: fix up temp file.
                    var text = input.Text;
                    var path = FileSystem.Root + "/home/ide/temp.js";
                    File.WriteAllText(path, text + "\n this file can be found at 'computer/home/ide/temp.js'");
                    var textEditor = new Texed(path);
                    Computer.Current.OpenApp(textEditor, "temp.js", Computer.GetNextProcessID());
                    break;


                case System.Windows.Input.Key.Enter:
                case System.Windows.Input.Key.F5:
                    await Send(e).ConfigureAwait(true);
                    break;

                case System.Windows.Input.Key.Up:
                    if ((Interpreter)interpreterBox.SelectedIndex == Interpreter.JavaScript)
                        return;

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
                case System.Windows.Input.Key.Down:
                    if ((Interpreter)interpreterBox.SelectedIndex == Interpreter.JavaScript)
                        return;

                    if (historyIndex == -1)
                        tempInput = input.Text;
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


            string inputText = input.Text;

            var outputText = output.Text;


            switch ((Interpreter)interpreterBox.SelectedIndex)
            {
                // terminal
                case Interpreter.Terminal:

                    HandleEvent(e);
                    if (string.IsNullOrEmpty(inputText))
                    {
                        Notifications.Now("Invalid input");
                        return;
                    }

                    OnTerminalSend?.Invoke(inputText);

                    // for Terminalread
                    if (IsReading)
                        return;

                    var success = Computer.Current.CmdLine.TryCommand(inputText);

                    if (!success)
                    {
                        Notifications.Now($"terminal::failure\n\t{inputText}\n\t\t: command not found.");
                        return;
                    }
                    PushHistory(inputText);
                    input.Clear();
                    break;

                case Interpreter.JavaScript:

                    // for newlines
                    if (!Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) && !Keyboard.IsKeyDown(System.Windows.Input.Key.F5))
                        return;

                    if (string.IsNullOrEmpty(inputText))
                    {
                        Notifications.Now("Invalid input");
                        return;
                    }

                    await ExecuteJavaScript(code: inputText, timeout: 50_000).ConfigureAwait(true);
                    break;
            }

            // clean up, show stuff.
            if (output.Text == outputText)
                output.AppendText("\n done.");

            

            LastSentInput = LastSentBuffer;
            LastSentBuffer = output.Text;
            HandleEvent(e);
        }

        private static void HandleEvent(KeyEventArgs? e)
        {
            if (e != null && e.RoutedEvent != null)
                e.Handled = true;
        }

        private void PushHistory(string inputText)
        {
            if (commandHistory.Count > 100)
                commandHistory.RemoveAt(0);

            if (commandHistory.Contains(inputText))
                commandHistory.RemoveAll(i => i == inputText);

            commandHistory.Add(inputText);
        }

        private async Task ExecuteJavaScript(string code, int timeout = int.MaxValue)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(code);

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
