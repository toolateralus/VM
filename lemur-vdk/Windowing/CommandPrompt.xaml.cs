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

namespace Lemur.GUI
{

    public partial class CommandPrompt : UserControl
    {
        internal Engine? Engine;
        private List<string> commandHistory = [];
        private int historyIndex = -1;
        private string tempInput = "";
        public static string? DesktopIcon => FileSystem.GetResourcePath("commandprompt.png");

        public Action<string> OnSend { get; internal set; }
        public ResizableWindow Window { get; private set; }

        public static string? LastSentInput;
        string LastSentBuffer = "";
        public CommandPrompt()
        {
            InitializeComponent();

            PreviewKeyDown += CommandPrompt_PreviewKeyDown;

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


        private async void CommandPrompt_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.C)
            {
                (Window.Content as UserWindow)?.Close();
            }
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.T)
            {
                var text = input.Text;
                var path = FileSystem.Root + "/home/ide/temp.js";
                File.WriteAllText(path, text + "\n this file can be found at 'computer/home/ide/temp.js'");

                var textEditor = new Texed(path);

                Computer.Current.OpenApp(textEditor, "temp.js", Computer.GetNextProcessID());
            }

            if (e.Key == Key.Enter || e.Key == Key.F5)
            {
                await Send(e);
            }
            ManageCommandHistoryKeys(e);
        }

        private async Task Send(KeyEventArgs? e)
        {
            OnSend?.Invoke(input.Text);

            if (commandHistory.Count > 100)
                commandHistory.RemoveAt(0);

            if (e != null && e.RoutedEvent != null)
                e.Handled = true;

            var text = output.Text;

            if (string.IsNullOrEmpty(input.Text))
            {
                Notifications.Now("Invalid input");
                return;
            }

            commandHistory.Add(input.Text);

            await ExecuteJavaScript(code: input.Text, timeout: 50_000);

            if (output.Text == text)
                output.AppendText("\n done.");

            input.Clear();

            LastSentInput = LastSentBuffer;
            LastSentBuffer = output.Text;
        }

        private void ManageCommandHistoryKeys(KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Up)
            {
                if (historyIndex == -1)
                    tempInput = input.Text;

                if (historyIndex < commandHistory.Count - 1)
                {
                    historyIndex++;
                    input.Text = commandHistory[commandHistory.Count - 1 - historyIndex];
                }
            }

            if (e.Key == System.Windows.Input.Key.Down)
            {
                if (historyIndex == -1)
                    tempInput = input.Text;

                if (historyIndex > 0)
                {
                    historyIndex--;
                    input.Text = commandHistory[commandHistory.Count - 1 - historyIndex];
                }
            }


        }

        private async Task ExecuteJavaScript(string code, int timeout = int.MaxValue)
        {
            if (Computer.Current.CmdLine.TryCommand(code))
                return;

            input.Clear();

            try
            {
                using var cts = new CancellationTokenSource();
                var executionTask = Engine?.Execute(code, cts.Token);

                var timeoutTask = Task.Delay(timeout);

                if (executionTask == null)
                    throw new NullReferenceException("No execution task was found, the engine probably was disposed while in use.");

                var completedTask = await Task.WhenAny(executionTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    cts.Cancel(); // Cancel the execution task
                    this.output.AppendText("\nExecution timed out.");
                }
                else
                {
                    var result = await executionTask;
                    string? output = result?.ToString();

                    if (!string.IsNullOrEmpty(output))
                    {
                        this.output.AppendText("\n" + output);
                    }
                }
            }
            catch (Exception ex)
            {
                this.output.Text += ex.Message + Environment.NewLine;
            }
        }



        private void Grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            input.Focus();
        }
    }
}
