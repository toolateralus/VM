﻿using System;
using System.Windows;
using Lemur.GUI;
using System.Threading.Tasks;
using System.Text;
using Button = System.Windows.Controls.Button;
using System.Windows.Input;
using System.Reflection;
using System.Threading;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Lemur.Windowing;
using Lemur;
using Lemur.JS;

namespace Lemur.JavaScript.Api
{
    public class InteropEvent : InteropFunction
    {
        public XAML_EVENTS Event = XAML_EVENTS.RENDER;
        FrameworkElement element;

        public InteropEvent(FrameworkElement control, XAML_EVENTS @event, Engine js, string id, string method)
        {
            Event = @event;
            javaScriptEngine = js;
            element = control;

            Computer.Current.Window.Dispatcher.Invoke(async delegate
            {
                functionHandle = await CreateFunction(id, method);
                CreateHook(control, @event);
            });
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
                            OnEventDisposed += () => button.Click -= InvokeGeneric;
                            break;
                        }
                        control.MouseDown += InvokeGeneric;
                        OnEventDisposed += () => control.MouseDown -= InvokeGeneric;
                    }
                    break;
                case XAML_EVENTS.MOUSE_UP:
                    {
                        control.MouseUp += InvokeGeneric;
                        OnEventDisposed += () => control.MouseUp -= InvokeGeneric;
                    }
                    break;
                case XAML_EVENTS.MOUSE_MOVE:
                    {
                        control.MouseMove += InvokeMouse;
                        OnEventDisposed += () => control.MouseMove -= InvokeMouse;
                    }
                    break;
                case XAML_EVENTS.KEY_DOWN:
                    {
                    }
                    break;
                case XAML_EVENTS.KEY_UP:
                    {
                    }
                    break;
                case XAML_EVENTS.LOADED:
                    {
                        control.Loaded += InvokeGeneric;
                        OnEventDisposed += () => control.Loaded -= InvokeGeneric;
                    }
                    break;
                case XAML_EVENTS.WINDOW_CLOSE:
                    {
                        control.Unloaded += InvokeGeneric;
                        OnEventDisposed += () => control.Unloaded -= InvokeGeneric;
                    }
                    break;
                case XAML_EVENTS.SELECTION_CHANGED:
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
                            Notifications.Now($"Invalid hook: {control.Name} did not have the 'SELECTION_CHANGED' event to hook into");
                        }
                    }
                    break;
                case XAML_EVENTS.MOUSE_LEAVE:
                    {
                        control.MouseLeave += InvokeGeneric;
                        OnEventDisposed += () => control.MouseLeave -= InvokeGeneric;
                    }
                    break;

                case XAML_EVENTS.PHYSICS: /// deprecated, use RENDER instead.
                case XAML_EVENTS.RENDER:
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
