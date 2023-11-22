using Newtonsoft.Json.Linq;
using System;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Lemur.GUI
{
    public partial class ResizableWindow : Frame
    {
        private bool isDragging = false;
        private bool isResizing = false;
        private Point leftClickPos;

        public Action? OnClosed { get; internal set; }
        public float ResizeSpeed => Computer.Current?.config?.Value<float>("WINDOW_RESIZE_SPEED") ?? 1f;
        public ResizableWindow(ComputerWindow owner)
        {
           MouseDown += OnMouseDown;
           MouseMove += OnMouseMove;
           MouseUp += OnMouseUp;
           MouseLeave += onMouseLeave;

           MinWidth = Computer.Current.Window?.Computer?.config?.Value<float?>("MIN_WIN_WIDTH") ?? 50;
           MinHeight = Computer.Current.Window?.Computer?.config?.Value<float?>("MIN_WIN_HEIGHT") ?? 50;
           MaxWidth = Computer.Current.Window?.Computer?.config?.Value<float?>("MAX_WIN_WIDTH") ?? 1920;
           MaxHeight = Computer.Current.Window?.Computer?.config?.Value<float?>("MAX_WIN_HEIGHT") ?? 1080 - 25;
        }
        private void onMouseLeave(object sender, MouseEventArgs e)
        {
            isDragging = false;
            isResizing = false;
        }
        private protected void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift)))
                return;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                leftClickPos = e.GetPosition(Computer.Current.Window);
                BringToTopOfDesktop();
                isDragging = true;
                e.Handled = true;
            }
            else
            if (e.RightButton == MouseButtonState.Pressed &&
                Parent is WindowManager windowManager)
            {
                windowManager.BeginResize(this);
                e.Handled = true;
            }
        }
        public void BringToTopOfDesktop()
        {
            if (Parent is Canvas grid && grid.Children.Contains(this))
            {
                grid.Children.Remove(this);
                grid.Children.Add(this);
                Canvas.SetZIndex(this, Computer.Current.Window.TopMostZIndex);
            }
        }
        protected void OnMouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(Computer.Current.Window);
            var windowControlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift);

            if (windowControlPressed && isDragging)
            {
                double left = pos.X - leftClickPos.X;
                double top = pos.Y - leftClickPos.Y;
                Canvas.SetLeft(this, left);
                Canvas.SetTop(this, top);
            }
        }

        protected void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging || isResizing)
            {
                isDragging = false;
                isResizing = false;
            }
        }

        internal void Dispose()
        {
            MouseDown -= OnMouseDown;
            MouseMove -= OnMouseMove;
            MouseUp -= OnMouseUp;
        }
    }
}
