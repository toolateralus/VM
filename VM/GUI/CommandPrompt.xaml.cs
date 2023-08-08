using System;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Collections.Generic;
using VM.OS.JS;
using VM.OS;

namespace VM.GUI
{
    public partial class CommandPrompt : UserControl
    {
        private JavaScriptEngine Engine;
        private List<string> commandHistory = new List<string>();
        private int historyIndex = -1; 
        private string tempInput = ""; 
        public Computer computer;
        public static string? DesktopIcon => Runtime.GetResourcePath("commandprompt", ".png");
        public CommandPrompt()
        {
            InitializeComponent();
            KeyDown += Input_KeyDown;
            PreviewKeyDown += CommandPrompt_PreviewKeyDown;
        }
        public void LateInit(Computer computer)
        {
            this.computer = computer;
            Engine = computer.OS.JavaScriptEngine;
        }
        private void CommandPrompt_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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

        private async Task ExecuteJavaScript()
        {
            string code = input.Text;

            if (commandHistory.Count > 50)
            {
                commandHistory.RemoveAt(0);
            }

            commandHistory.Add(code);


            input.Clear();
            try
            {
                var task = Engine.Execute(code);
                await task;
                var result = task.Result;

                string? output = result?.ToString();

                if (!string.IsNullOrEmpty(output))
                {
                    this.output.Text += output + Environment.NewLine;
                }
            }
            catch (Exception ex)
            {
                this.output.Text += ex.Message + Environment.NewLine;
            }
        }

        private async void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((e.Key == System.Windows.Input.Key.Enter && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Shift) || e.Key == System.Windows.Input.Key.F5)
            {
                await ExecuteJavaScript();
            }
        }
    }
}
