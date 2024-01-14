using Lemur.GUI;
using Lemur.JS;
using Lemur.Windowing;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;

namespace Lemur.JavaScript.Api
{
    public class InteropEvent : InteropFunction
    {
        internal Event Event = Event.Rendering;
        FrameworkElement element;

        internal InteropEvent(FrameworkElement control, Event @event, Engine js, string id, string method)
        {
            Event = @event;
            javaScriptEngine = js;
            element = control;

            Computer.Current.Window.Dispatcher.Invoke(async delegate
            {
                functionHandle = await CreateFunction(id, method).ConfigureAwait(false);
                CreateHook(control, @event);
            });
        }

        private void CreateHook(FrameworkElement control, Event @event)
        {
            // todo: fix these potential / likely memory leaks
            switch (@event)
            {
                case Event.MouseDown:
                    {
                        if (control is Button button)
                        {
                            button.Click += InvokeGeneric;
                            OnEventDisposed += () => button.Click -= InvokeGeneric;
                            break;
                        }
                        control.MouseDown += InvokeGeneric;
                        OnEventDisposed += () => control.MouseDown -= InvokeGeneric;
                    }
                    break;
                case Event.MouseUp:
                    {
                        control.MouseUp += InvokeGeneric;
                        OnEventDisposed += () => control.MouseUp -= InvokeGeneric;
                    }
                    break;
                case Event.MouseMove:
                    {
                        control.MouseMove += InvokeMouse;
                        OnEventDisposed += () => control.MouseMove -= InvokeMouse;
                    }
                    break;
                case Event.KeyDown:
                    {
                        control.KeyDown += InvokeKeyboard;
                        OnEventDisposed += () => control.KeyDown -= InvokeKeyboard;
                    }
                    break;
                case Event.KeyUp:
                    {
                        control.KeyUp += InvokeKeyboard;
                        OnEventDisposed += () => control.KeyUp -= InvokeKeyboard;
                    }
                    break;
                case Event.Loaded:
                    {
                        control.Loaded += InvokeGeneric;
                        OnEventDisposed += () => control.Loaded -= InvokeGeneric;
                    }
                    break;
                case Event.WindowClose:
                    {
                        control.Unloaded += InvokeGeneric;
                        OnEventDisposed += () => control.Unloaded -= InvokeGeneric;
                    }
                    break;
                case Event.SelectionChanged:
                    {
                        if (control is Selector lb)
                        {

                            void selectorevent(object? sender, SelectionChangedEventArgs e)
                            {
                                InvokeGeneric(sender, lb.SelectedIndex);
                            }
                            lb.SelectionChanged += selectorevent;
                            OnEventDisposed += () => lb.SelectionChanged -= selectorevent;
                        }
                        else
                        {
                            Notifications.Now($"Invalid hook: {control.Name} did not have the 'SelectionChanged' event to hook into");
                        }
                    }
                    break;
                case Event.MouseLeave:
                    {
                        control.MouseLeave += InvokeGeneric;
                        OnEventDisposed += () => control.MouseLeave -= InvokeGeneric;
                    }
                    break;

                case Event.Rendering:
                    executionThread = new(RenderLoop);
                    executionThread.Start();

                    OnEventDisposed += () =>
                    {
                        Running = false;
                        Task.Run(() => executionThread.Join());
                    };

                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(@event));
            }
        }

        public override void InvokeGeneric(object? sender, object? arguments)
        {
            if (arguments is RoutedEventArgs args)
            {
                var mouseArgs = new object[] {
                    Mouse.LeftButton is MouseButtonState.Pressed,
                    Mouse.RightButton is MouseButtonState.Pressed
                };

                InvokeMouse(sender?.GetType()?.GetProperty("Name")?.GetValue(sender) ?? "unknown", mouseArgs);
            }
            else
            {
                InvokeEventBackground(arguments);
            }
        }
        public void InvokeMouse(object? sender, object? e)
        {
            if (e is MouseEventArgs mvA && mvA.GetPosition(sender as IInputElement ?? element) is Point pos)
            {
                InvokeEventBackground(pos.X, pos.Y);
                return;
            }
            else if (e is object?[] mouse_args)
            {
                InvokeEventBackground(mouse_args[0], mouse_args[1]);
            }

        }

        public void InvokeKeyboard(object? sender, KeyEventArgs e)
        {
            InvokeEventBackground(e.Key.ToString(), e.IsDown);
        }
    }
}
