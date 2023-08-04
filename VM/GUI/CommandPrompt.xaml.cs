using System;
using System.Windows.Controls;
using VM.OPSYS.JS;
using JavaScriptEngineSwitcher;
using JavaScriptEngineSwitcher.Core.Extensions;
using System.Linq;

namespace VM.GUI
{
    public partial class CommandPrompt : UserControl
    {
        private JSInterop interop;

        public CommandPrompt()
        {
            InitializeComponent();
            interop = new JSInterop();
            KeyDown += Input_KeyDown;
        }

        private void ExecuteJavaScript()
        {
            string code = input.Text.SplitToLines().LastOrDefault("");
            try
            {
                object result = interop.Execute(code);
                string? output = result?.ToString();

                if (!string.IsNullOrEmpty(output))
                {
                    input.AppendText(Environment.NewLine + output);
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
