using System;
using System.Windows.Controls;
using VM.OPSYS.JS;
using VroomJs;

namespace VM.GUI
{
    public partial class CommandPrompt : UserControl
    {
        private JSInterop interop;

        public CommandPrompt()
        {
            InitializeComponent();
            interop = new JSInterop();
            input.KeyDown += Input_KeyDown;
        }

        private void ExecuteJavaScript()
        {
            string code = input.Text;
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
                ExecuteJavaScript();
            }
        }
    }
}
