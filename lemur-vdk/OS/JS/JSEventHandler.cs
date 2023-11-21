using System;
using System.Windows;
using Lemur.GUI;
using System.Threading.Tasks;
using System.Text;
using Button = System.Windows.Controls.Button;
using System.Windows.Input;
using System.Reflection;
using System.Threading;

namespace Lemur.JS
{
    public class XAMLJSEventHandler : JavaScriptWpfHook, IDisposable
    {
        public XAML_EVENTS Event = XAML_EVENTS.RENDER;
        FrameworkElement element;

        public XAMLJSEventHandler(FrameworkElement control, XAML_EVENTS @event, JavaScriptEngine js, string id, string method)
        {
            Event = @event;
            this.javaScriptEngine = js;
            element = control;
            base.functionHandle = CreateFunction(id, method);
            CreateHook(control, @event);
        }

        private void CreateHook(FrameworkElement control, XAML_EVENTS @event)
        {
            // todo: fix these potential / likely memory leaks
            switch (@event)
            {
                case XAML_EVENTS.MOUSE_DOWN:
                    {
                        if (control is Button button)
                        {
                            button.Click += InvokeGeneric;
                            onDispose += () => button.Click -= InvokeGeneric;
                            break;
                        }
                        control.MouseDown += InvokeGeneric;
                        onDispose += () => control.MouseDown -= InvokeGeneric;
                    }
                    break;
                case XAML_EVENTS.MOUSE_UP:
                    {
                        control.MouseUp += InvokeGeneric;
                        onDispose += () => control.MouseUp -= InvokeGeneric;
                    }
                    break;
                case XAML_EVENTS.MOUSE_MOVE:
                    {
                        control.MouseMove += InvokeMouse;
                        onDispose += () => control.MouseMove -= InvokeMouse;
                    }
                    break;
                case XAML_EVENTS.KEY_DOWN:
                    {
                        OnKeyDown += InvokeKeyboard;
                        onDispose += () => OnKeyDown -= InvokeKeyboard;
                    }
                    break;
                case XAML_EVENTS.KEY_UP:
                    {
                        OnKeyUp += InvokeKeyboard;
                        onDispose += () => OnKeyUp -= InvokeKeyboard;
                    }
                    break;
                case XAML_EVENTS.LOADED:
                    {
                        control.Loaded += InvokeGeneric;
                        onDispose += () => control.Loaded -= InvokeGeneric;
                    }
                    break;
                case XAML_EVENTS.WINDOW_CLOSE:
                    {
                        control.Unloaded += InvokeGeneric;
                        onDispose += () => control.Unloaded -= InvokeGeneric;
                    }
                    break;

                /// the lower the delay, the faster the calls back to js, however this can have
                /// a counterintuitive effect when too short or too many running, since
                /// a low delay introduces large cpu overhead.
                /// also, DELAY_BETWEEN_WORK_ITERATIONS + 1 == like 3ms,
                /// so we use much smaller values to get a more appropriate speed.
                case XAML_EVENTS.RENDER:
                    executionThread = new(HeavyWorkerLoop);
                    executionThread.Start();
                    break;
                case XAML_EVENTS.PHYSICS:
                    executionThread = new(HeavyWorkerLoop);
                    executionThread.Start();
                    break;
                default:
                    break;
            }
        }

        public Action<object?, KeyEventArgs> OnKeyUp;

        public Action<object?, KeyEventArgs> OnKeyDown;
        public override void InvokeGeneric(object? sender, object? arguments)
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

        public void InvokeKeyboard(object? sender, System.Windows.Input.KeyEventArgs e)
        {
            InvokeEvent(e.Key.ToString(), e.IsDown);
        }
    }
}
