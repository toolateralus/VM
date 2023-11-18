using Newtonsoft.Json.Linq;
using System;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VM.GUI
{
    public partial class ResizableWindow : Frame
    {
        private bool isDragging = false;
        private bool isResizing = false;
        private Point dragOffset;
        public Action OnClosed { get; internal set; }
        ComputerWindow Owner;
        public float ResizeSpeed => Owner?.Computer?.Config.Value<float?>("WINDOW_RESIZE_SPEED") ?? 4f;
        public ResizableWindow(ComputerWindow owner)
        {
           Owner = owner;
           MouseDown += OnMouseDown;
           MouseMove += OnMouseMove;
           MouseUp += OnMouseUp;
           MouseLeave += onMouseLeave;

           MinWidth = Owner?.Computer?.Config.Value<float?>("MIN_WIN_WIDTH") ?? 50;
           MinHeight = Owner?.Computer?.Config.Value<float?>("MIN_WIN_HEIGHT") ?? 50;
           MaxWidth = Owner?.Computer?.Config.Value<float?>("MAX_WIN_WIDTH") ?? 1920;
           MaxHeight = Owner?.Computer?.Config.Value<float?>("MAX_WIN_HEIGHT") ?? 1080 - 25;
        }
        private void onMouseLeave(object sender, MouseEventArgs e)
        {
            isDragging = false;
            isResizing = false;
            dragOffset = new();
        }
        protected void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            dragOffset = e.GetPosition(this);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                BringToTopOfDesktop();
                isDragging = true;
            }
        }
        public void BringToTopOfDesktop()
        {
            if (Parent is Canvas grid && grid.Children.Contains(this))
            {
                grid.Children.Remove(this);
                grid.Children.Add(this);
                Canvas.SetZIndex(this, Owner.TopMostZIndex);
            }
        }
        protected void OnMouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(App.Current.MainWindow as Runtime);
            var altDown = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
            
            // Todo: refactor this entire drag/move/resize system. It's been nothing but trouble, and surprisingly
            // This is the best state it has ever been in.

            if (altDown && e.RightButton == MouseButtonState.Pressed)
            {
                var delta = pos - dragOffset;
                double maxDelta = ResizeSpeed;
                delta = NormalizeMouseDelta(delta, maxDelta);
                PerformResize(delta);

            }
            else if (altDown && isDragging)
            {
                double left = pos.X - dragOffset.X;
                double top = pos.Y - dragOffset.Y;
                Canvas.SetLeft(this, left);
                Canvas.SetTop(this, top);
            }
        }

        private void PerformResize(Vector delta)
        {
            if (ActualWidth + delta.X >= MinWidth && ActualWidth + delta.X <= MaxWidth)
                Width += delta.X;
            else if (ActualWidth + delta.X < MinWidth)
                Width = MinWidth;
            else if (ActualWidth + delta.X > MaxWidth)
                Width = MaxWidth;

            if (ActualHeight + delta.Y >= MinHeight && ActualHeight + delta.Y <= MaxHeight)
                Height += delta.Y;
            else if (ActualHeight + delta.Y < MinHeight)
                Height = MinHeight;
            else if (ActualHeight + delta.Y > MaxHeight)
                Height = MaxHeight;
        }

        private static Vector NormalizeMouseDelta(Vector delta, double maxDelta)
        {
            if (Math.Abs(delta.X) > Math.Abs(delta.Y))
            {
                delta.X = Math.Clamp(delta.X, -maxDelta, maxDelta);
                delta.Y = 0;
            }
            else
            {
                delta.Y = Math.Clamp(delta.Y, -maxDelta, maxDelta);
                delta.X = 0;
            }

            return delta;
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
