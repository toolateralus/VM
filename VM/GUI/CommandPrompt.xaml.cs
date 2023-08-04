using System;
using System.Windows.Controls;
using VM.OPSYS.JS;
using JavaScriptEngineSwitcher;
using JavaScriptEngineSwitcher.Core.Extensions;
using System.Linq;
using VM.OPSYS;

namespace VM.GUI
{
    public partial class CommandPrompt : UserControl
    {
        private JavaScriptEngine Engine;

        public CommandPrompt()
        {
            InitializeComponent();
            Engine = OS.Current.JavaScriptEngine;
            KeyDown += Input_KeyDown;
            Unloaded += (o, e) => Engine?.Dispose();
        }

        private void ExecuteJavaScript()
        {
            string code = input.Text.SplitToLines().LastOrDefault("");
            try
            {
                object result = Engine.Execute(code);
                string? output = result?.ToString();

                if (!string.IsNullOrEmpty(output))
                {
                    input.Text = input.Text.Insert(0, output + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                input.AppendText(Environment.NewLine + "Error: " + ex.Message);
            }
        }

        private void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Shift)
                {
                    ExecuteJavaScript();
                    return;
                }
               
            }
            if (e.Key == System.Windows.Input.Key.F5)
            {
                ExecuteJavaScript();
            }
        }

    }
}
