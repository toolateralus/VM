using System;
using System.Windows;
using VM.GUI;
using System.Threading.Tasks;
using System.Text;
using Button = System.Windows.Controls.Button;

namespace VM.OS.JS
{
    public class JSEventHandler
    {
        StringBuilder LastCode = new("");
        readonly string TemplateCode;
        JavaScriptEngine js;
        public XAML_EVENTS Event = XAML_EVENTS.RENDER;
        public Action OnUnhook;
        FrameworkElement element;
        public JSEventHandler(FrameworkElement control, XAML_EVENTS @event, JavaScriptEngine js, string id, string method)
        {
            Event = @event;
            this.js = js;
            element = control;
            SetCode(id, method);

            TemplateCode ??= LastCode.ToString();

            switch (@event)
            {
                case XAML_EVENTS.MOUSE_DOWN:
                    if (control is Button button)
                    {
                        button.Click += InvokeGeneric;
                        OnUnhook = () => button.Click -= InvokeGeneric;
                        break;
                    }
                    control.MouseDown += InvokeMouse;
                    OnUnhook = () => control.MouseDown -= InvokeMouse;
                    break;
                case XAML_EVENTS.MOUSE_UP:
                    control.MouseUp += InvokeMouse;
                    OnUnhook = () => control.MouseUp -= InvokeMouse;
                    break;
                case XAML_EVENTS.MOUSE_MOVE:
                    control.MouseMove += InvokeMouse;
                    OnUnhook = () => control.MouseMove -= InvokeMouse;
                    break;
                case XAML_EVENTS.KEY_DOWN:
                    control.KeyDown += InvokeKeyboard;
                    OnUnhook = () => control.KeyDown -= InvokeKeyboard;
                    break;
                case XAML_EVENTS.KEY_UP:
                    control.KeyUp += InvokeKeyboard;
                    OnUnhook = () => control.KeyUp -= InvokeKeyboard;
                    break;
                case XAML_EVENTS.LOADED:
                    control.Loaded += InvokeGeneric;
                    OnUnhook = () => control.Loaded -= InvokeGeneric;
                    break;
                case XAML_EVENTS.WINDOW_CLOSE:
                    control.Unloaded += InvokeGeneric;
                    OnUnhook = () => control.Unloaded -= InvokeGeneric;
                    break;

                case XAML_EVENTS.RENDER:
                default:
                    break;
            }

        }


        public string GetCode()
        {
            return LastCode.ToString();
        }
        public void SetCode(string identifier, string methodName)
        {
            LastCode.Clear();
            LastCode.Append($"{identifier}.{methodName}({arguments_placeholder})");
        }
        const string arguments_placeholder = $"{{!arguments}}";

        public bool Disposed { get; internal set; }

        public void InstantiateCode(object? sender, object? args)
        {
            LastCode.Clear();

            StringBuilder argsBldr = new("\"");
            var arg0 = sender?.ToString() + '\"';

            if (!string.IsNullOrEmpty(arg0))
                argsBldr.Append(arg0);

            var arg1 = args?.ToString();

            if (!string.IsNullOrEmpty(arg1))
                argsBldr.Append(" ," + arg1);

            LastCode.Append(TemplateCode.Replace(arguments_placeholder, $"{argsBldr.ToString()}"));
        }

        public void InvokeMouse(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            if (element != null && e.GetPosition(element) is Point pos)
            {
                InstantiateCode(null, $"[{(e.LeftButton == System.Windows.Input.MouseButtonState.Pressed ? 1 : 0)},{(e.RightButton == System.Windows.Input.MouseButtonState.Pressed ? 1 : 0)}, [{pos.X},{pos.Y}] ,{(e.MiddleButton == System.Windows.Input.MouseButtonState.Pressed ? 1 : 0)}]");
                InvokeEvent();
                return;
            }
            InstantiateCode(null, $"[{(e.LeftButton == System.Windows.Input.MouseButtonState.Pressed ? 1 : 0)},{(e.RightButton == System.Windows.Input.MouseButtonState.Pressed ? 1 : 0)},{(e.MiddleButton == System.Windows.Input.MouseButtonState.Pressed ? 1 : 0)}]");
            InvokeEvent();
        }

        private void InvokeEvent()
        {
            if (LastCode.ToString() is string Code)
            {
                js.DIRECT_EXECUTE(Code);
                LastCode.Clear();
            }
        }

        public void InvokeKeyboard(object? sender, System.Windows.Input.KeyEventArgs e)
        {
            InstantiateCode(null, $"[{e.Key},{e.IsDown},{e.SystemKey}]");
            InvokeEvent();
        }
        public void InvokeGeneric(object? sender, object? arguments)
        {
            InstantiateCode(null, null);
            InvokeEvent();
        }
    }
}
