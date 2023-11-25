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
    public class InteropEvent : InteropFunction
    {
        public XAML_EVENTS Event = XAML_EVENTS.RENDER;
        FrameworkElement element;

        public InteropEvent(FrameworkElement control, XAML_EVENTS @event, Engine js, string id, string method)
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

                case XAML_EVENTS.RENDER:
                case XAML_EVENTS.PHYSICS: /// deprecated, use RENDER instead.
                    executionThread = new(HeavyWorkerLoop);
                    executionThread.Start();
                    break;
                case XAML_EVENTS.MOUSE_LEAVE:
                    {
                        control.MouseLeave += InvokeGeneric;
                        onDispose += () => control.MouseLeave -= InvokeGeneric;
                    }
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
