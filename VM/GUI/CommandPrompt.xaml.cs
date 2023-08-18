using System;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Collections.Generic;
using VM.JS;
using VM;
using System.Windows;
using System.Linq;
using System.Windows.Input;
using System.Threading;

namespace VM.GUI
{
    
    public partial class CommandPrompt : UserControl
    {
        private JavaScriptEngine? Engine;
        private List<string> commandHistory = new List<string>();
        private int historyIndex = -1; 
        private string tempInput = ""; 
        public Computer computer;
        public static string? DesktopIcon => Runtime.GetResourcePath("commandprompt.png");

        public Action<string> OnSend { get; internal set; }
        public static string? LastUploaded;
        string uploaded_buffer = "";
        public CommandPrompt()
        {
            InitializeComponent();
            PreviewKeyDown += CommandPrompt_PreviewKeyDown;
            DrawTextBox("type 'help' for commands, \nor enter any valid single-line java script to interact with the environment. \nby default, results of expressions get printed to this console.");
            input.Focus();

            output.TextChanged += Output_TextChanged;
            
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

        public void LateInit(Computer computer)
        {
            this.computer = computer;
            Engine = computer.JavaScriptEngine;

            output.FontFamily = new(computer.Config.Value<string>("FONT") ?? "Consolas");
            input.FontFamily = new(computer.Config.Value<string>("FONT") ?? "Consolas");
        }
        private async void CommandPrompt_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.F5)
            {
                await Send(e);
            }
            ManageCommandHistoryKeys(e);
        }

        private async Task Send(KeyEventArgs? e)
        {
            OnSend?.Invoke(input.Text);

            await ExecuteJavaScript(input.Text, computer.Config.Value<int?>("CMD_LINE_TMOUT") ?? 10_000);
            
            if (e != null && e.RoutedEvent != null)
                e.Handled = true;

            input.Clear();


            LastUploaded = uploaded_buffer;
            uploaded_buffer = output.Text;
        }

        private void ManageCommandHistoryKeys(KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Up)
            {
                if (historyIndex == -1)
                {
                    tempInput = input.Text;
                }

                if (historyIndex < commandHistory.Count - 1)
                {
                    historyIndex++;
                    input.Text = commandHistory[commandHistory.Count - 1 - historyIndex];
                }
            }

            if (e.Key == System.Windows.Input.Key.Down)
            {
                if (historyIndex >= 0)
                {
                    historyIndex--;
                    if (historyIndex == -1)
                    {
                        input.Text = tempInput;
                    }
                    else
                    {
                        input.Text = commandHistory[commandHistory.Count - 1 - historyIndex];
                    }
                }
            }

         
        }

        private async Task ExecuteJavaScript(string code, int timeout = int.MaxValue)
        {
            if (computer.CommandLine.TryCommand(code))
            {
                return;
            }

            if (commandHistory.Count > 50)
            {
                commandHistory.RemoveAt(0);
            }

            commandHistory.Add(code);

            input.Clear();

            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    var executionTask = Engine.Execute(code, cts.Token);
                    var timeoutTask = Task.Delay(timeout);

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
