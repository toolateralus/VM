using System;
using System.Windows;
using VM.GUI;
using System.Threading.Tasks;
using System.Text;
using Button = System.Windows.Controls.Button;
using System.Windows.Input;
using System.Reflection;

namespace VM.JS
{
    public class JSEventHandler
    {
        internal JavaScriptEngine jsEngine;
        public XAML_EVENTS Event = XAML_EVENTS.RENDER;
        public Action OnUnhook;
        FrameworkElement element;

        internal readonly string methodHandle;
        internal readonly string FUNCTION_HANDLE;

        public JSEventHandler(FrameworkElement control, XAML_EVENTS @event, JavaScriptEngine js, string id, string method)
        {
            Event = @event;
            this.jsEngine = js;
            element = control;
            FUNCTION_HANDLE = CreateFunction(id, method);
            CreateHook(control, @event);
        }

        private void CreateHook(FrameworkElement control, XAML_EVENTS @event)
        {
            switch (@event)
            {
                case XAML_EVENTS.MOUSE_DOWN:
                    {
                        if (control is Button button)
                        {
                            button.Click += InvokeGeneric;
                            OnUnhook += () => button.Click -= InvokeGeneric;
                            break;
                        }
                        control.MouseDown += InvokeGeneric;
                        OnUnhook += () => control.MouseDown -= InvokeGeneric;
                    }
                    break;
                case XAML_EVENTS.MOUSE_UP:
                    {
                        control.MouseUp += InvokeGeneric;
                        OnUnhook += () => control.MouseUp -= InvokeGeneric;
                    }
                    break;
                case XAML_EVENTS.MOUSE_MOVE:
                    {
                        control.MouseMove += InvokeMouse;
                        OnUnhook += () => control.MouseMove -= InvokeMouse;
                    }
                    break;
                case XAML_EVENTS.KEY_DOWN:
                    {
                        OnKeyDown += InvokeKeyboard;
                        OnUnhook += () => OnKeyDown -= InvokeKeyboard;
                    }
                    break;
                case XAML_EVENTS.KEY_UP:
                    {
                        OnKeyUp += InvokeKeyboard;
                        OnUnhook += () => OnKeyUp -= InvokeKeyboard;
                    }
                    break;
                case XAML_EVENTS.LOADED:
                    {
                        control.Loaded += InvokeGeneric;
                        OnUnhook += () => control.Loaded -= InvokeGeneric;
                    }
                    break;
                case XAML_EVENTS.WINDOW_CLOSE:
                    {
                        control.Unloaded += InvokeGeneric;
                        OnUnhook += () => control.Unloaded -= InvokeGeneric;
                    }
                    break;

                case XAML_EVENTS.RENDER:
                    // render events are handled elsewhere for performance and threading reasons.
                    // they are properly unhooked on their own.
                default:
                    break;
            }
        }

        public string CreateFunction(string identifier, string methodName)
        {
            var event_call = $"{ identifier }.{ methodName}{ARGS_STRING}";
            var id = $"{ identifier }{ methodName}";
            string func = $"function {id} {ARGS_STRING} {{ {event_call}; }}";
            Task.Run(() => jsEngine?.Execute(func));
            return id;
        }

        const string ARGS_STRING = "(arg1, arg2)";

        public bool Disposing { get; internal set; }
        public Action<object?, KeyEventArgs> OnKeyUp;

        public Action<object?, KeyEventArgs> OnKeyDown; 

        public void InvokeMouse(object? sender, object? e)
        {
            if (e is MouseEventArgs mvA && mvA.GetPosition(sender as IInputElement ?? element) is Point pos)
            {
                InvokeEvent(pos.X, pos.Y);
                return;
            }
            else if (e is object?[] mouse_args)
            {
                InvokeEvent(mouse_args[0], mouse_args[1]);
            }

        }

        private void InvokeEvent(object? arg1 = null, object? arg2 = null)
        {
            Task.Run(() => {
                try
                {
                    jsEngine.ENGINE_JS.CallFunction(FUNCTION_HANDLE, arg1, arg2);
                }
                catch (Exception e)
                {
                    Notifications.Exception(e);
                }
            });
        }

        public void InvokeKeyboard(object? sender, System.Windows.Input.KeyEventArgs e)
        {
            InvokeEvent(e.Key.ToString(), e.IsDown);
        }
        public void InvokeGeneric(object? sender, object? arguments)
        {
            if (arguments is RoutedEventArgs args)
            {
                var mouseArgs = new object[] { Mouse.LeftButton is MouseButtonState.Pressed, Mouse.RightButton is MouseButtonState.Pressed };
                InvokeMouse(sender?.GetType()?.GetProperty("Name")?.GetValue(sender) ?? "unknown", mouseArgs);
            }
            else
            {
                InvokeEvent();
            }
        }
    }
}
