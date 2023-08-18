using System;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VM.GUI
{
    public partial class ResizableWindow : Frame
    {
        private const int ResizeMargin = 30;
        private bool isDragging = false;
        private bool isResizing = false;
        private Point dragOffset;
        private double originalWidth;
        private double originalHeight;
        private enum ResizeDirection
        {
            None,
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }
        private ResizeDirection resizeDirection;
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
           MinWidth = 50;
           MinHeight = 50;
           MaxWidth = 1920;
           MaxHeight = 1080 - 25;

        }
        private void onMouseLeave(object sender, MouseEventArgs e)
        {
            isDragging = false;
            isResizing = false;
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
            var altDown = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
            if (altDown && e.RightButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(App.Current.MainWindow as Runtime);
                var delta = pos - dragOffset;

                double maxDelta = ResizeSpeed;
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

                if (ActualWidth + delta.X >= MinWidth && ActualWidth + delta.X <= MaxWidth)
                    Width += delta.X;

                if (ActualHeight + delta.Y >= MinHeight && ActualHeight + delta.Y <= MaxHeight)
                    Height += delta.Y;
            }
            else if (altDown && isDragging)
            {
                Point newPosition = e.GetPosition(App.Current.MainWindow as Runtime);
                double left = newPosition.X - dragOffset.X;
                double top = newPosition.Y - dragOffset.Y;
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
    }
}
