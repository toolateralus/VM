using System;
using System.Windows.Controls;
using VM.OPSYS.JS;
using JavaScriptEngineSwitcher;
using JavaScriptEngineSwitcher.Core.Extensions;
using System.Linq;
using VM.OPSYS;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace VM.GUI
{
    public partial class CommandPrompt : UserControl
    {
        private JavaScriptEngine Engine;

        public CommandPrompt(Computer computer)
        {
            InitializeComponent();
            Engine = computer.OS.JavaScriptEngine;
            KeyDown += Input_KeyDown;
           
        }

        private async Task ExecuteJavaScript()
        {
            string code = input.Text.SplitToLines().LastOrDefault("");
            try
            {
                var task = Engine.Execute(code);
                await task;
                var result = task.Result;

                string? output = result?.ToString();

                if (!string.IsNullOrEmpty(output))
                {
                    input.Text = input.Text.Insert(0, output + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                input.Text = input.Text.Insert(0, ex.Message + Environment.NewLine);
            }
        }
      
        private async void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Shift)
                {
                    await ExecuteJavaScript();
                    return;
                }
               
            }

            if (e.Key == System.Windows.Input.Key.F5)
            {
                await ExecuteJavaScript();
            }



        }

    }
}
