﻿using System;
using System.Windows;
using VM.GUI;
using System.Threading.Tasks;
using System.Text;
using Button = System.Windows.Controls.Button;
using System.Windows.Input;

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
        public void InvokeMouse(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            var Args = new object?[]
            {
                 e.LeftButton == MouseButtonState.Pressed,
                 e.RightButton == MouseButtonState.Pressed,
                 e.MiddleButton == MouseButtonState.Pressed,
                 null!, // for pos array
            };

            if (element != null && e.GetPosition(element) is Point pos)
            {
                Args[3] = new object[] { pos.X, pos.Y };
                InvokeEvent(Args);
                return;
            }

            InvokeEvent(Args);
        }

        private void InvokeEvent(params object?[] args)
        {
            try
            {
                jsEngine.ENGINE_JS.CallFunction(FUNCTION_HANDLE, args);
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
            }
        }

        public void InvokeKeyboard(object? sender, System.Windows.Input.KeyEventArgs e)
        {
            InvokeEvent(new object?[] { e.Key, e.IsDown, e.SystemKey });
        }
        public void InvokeGeneric(object? sender, object? arguments)
        {
            InvokeEvent();
        }
    }
}
