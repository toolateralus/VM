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

        public ResizableWindow()
        {
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
            this.ReleaseMouseCapture();
        }
        protected void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.ClickCount == 2)
                {
                    BringToTopOfDesktop();
                }
                else
                {
                    isDragging = true;
                    this.CaptureMouse();
                }
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                isResizing = true;
                originalWidth = this.ActualWidth;
                originalHeight = this.ActualHeight;
            }
            dragOffset = e.GetPosition(this);
        }
        public void BringToTopOfDesktop()
        {
            if (Parent is Grid grid && grid.Children.Contains(this))
            {
                grid.Children.Remove(this);
                grid.Children.Add(this);
            }
        }
        protected void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging && !isResizing)
            {
                resizeDirection = DetectResizeDirection(e.GetPosition(this));
                ChangeCursor();
            }
            else if (isDragging)
            {
                Point newPosition = e.GetPosition(App.Current.MainWindow as Runtime);

                double left = newPosition.X - dragOffset.X;
                double top = newPosition.Y - dragOffset.Y;

                Canvas.SetLeft(this, left);
                Canvas.SetTop(this, top);
            }
            else if (isResizing)
            {
                PerformResizing(e);
            }
        }
        private ResizeDirection DetectResizeDirection(Point mousePosition)
        {
            bool leftEdge = mousePosition.X < ResizeMargin;
            bool rightEdge = mousePosition.X > this.ActualWidth - ResizeMargin;
            bool topEdge = mousePosition.Y < ResizeMargin;
            bool bottomEdge = mousePosition.Y > this.ActualHeight - ResizeMargin;

            if (leftEdge && topEdge) return ResizeDirection.TopLeft;
            if (rightEdge && topEdge) return ResizeDirection.TopRight;
            if (leftEdge && bottomEdge) return ResizeDirection.BottomLeft;
            if (rightEdge && bottomEdge) return ResizeDirection.BottomRight;
            if (leftEdge) return ResizeDirection.Left;
            if (rightEdge) return ResizeDirection.Right;
            if (topEdge) return ResizeDirection.Top;
            if (bottomEdge) return ResizeDirection.Bottom;

            return ResizeDirection.None;
        }
        private void ChangeCursor()
        {
            switch (resizeDirection)
            {
                case ResizeDirection.Left:
                case ResizeDirection.Right:
                    this.Cursor = Cursors.SizeWE;
                    break;
                case ResizeDirection.Top:
                case ResizeDirection.Bottom:
                    this.Cursor = Cursors.SizeNS;
                    break;
                case ResizeDirection.TopLeft:
                case ResizeDirection.BottomRight:
                    this.Cursor = Cursors.SizeNWSE;
                    break;
                case ResizeDirection.TopRight:
                case ResizeDirection.BottomLeft:
                    this.Cursor = Cursors.SizeNESW;
                    break;
                default:
                    this.Cursor = Cursors.Arrow;
                    break;
            }
        }
        private void PerformResizing(MouseEventArgs e)
        {
            Point currentPosition = e.GetPosition(App.Current.MainWindow as Runtime);

            double deltaX = currentPosition.X - dragOffset.X;
            double deltaY = currentPosition.Y - dragOffset.Y;

            double newWidth = this.ActualWidth;
            double newHeight = this.ActualHeight;

            switch (resizeDirection)
            {
                case ResizeDirection.Left:
                    newWidth = originalWidth - deltaX;
                    Canvas.SetLeft(this, Canvas.GetLeft(this) + deltaX);
                    break;
                case ResizeDirection.Right:
                    newWidth = originalWidth + deltaX;
                    break;
                case ResizeDirection.Top:
                    newHeight = originalHeight - deltaY;
                    Canvas.SetTop(this, Canvas.GetTop(this) + deltaY);
                    break;
                case ResizeDirection.Bottom:
                    newHeight = originalHeight + deltaY;
                    break;
                case ResizeDirection.TopLeft:
                    newWidth = originalWidth - deltaX;
                    newHeight = originalHeight - deltaY;
                    Canvas.SetLeft(this, Canvas.GetLeft(this) + deltaX);
                    Canvas.SetTop(this, Canvas.GetTop(this) + deltaY);
                    break;
                case ResizeDirection.TopRight:
                    newWidth = originalWidth + deltaX;
                    newHeight = originalHeight - deltaY;
                    Canvas.SetTop(this, Canvas.GetTop(this) + deltaY);
                    break;
                case ResizeDirection.BottomLeft:
                    newWidth = originalWidth - deltaX;
                    newHeight = originalHeight + deltaY;
                    Canvas.SetLeft(this, Canvas.GetLeft(this) + deltaX);
                    break;
                case ResizeDirection.BottomRight:
                    newWidth = originalWidth + deltaX;
                    newHeight = originalHeight + deltaY;
                    break;
            }

            this.Width = Math.Max(MinWidth, Math.Min(MaxWidth, newWidth));
            this.Height = Math.Max(MinHeight, Math.Min(MaxHeight, newHeight));
        }
        protected void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging || isResizing)
            {
                isDragging = false;
                isResizing = false;
                this.ReleaseMouseCapture();
            }
        }
    }
}
