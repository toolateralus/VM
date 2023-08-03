using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VM
{
    public partial class ResizableWindow : Frame
    {
        private bool isDragging = false;
        private bool isResizing = false;
        private Point dragOffset;
        private double originalWidth;
        private double originalHeight;

        public ResizableWindow()
        {
           
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;

            MinWidth = 100;
            MinHeight = 100;
            MaxWidth = 500;
            MaxHeight = 500;
        }

        protected void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.ClickCount == 2)
                { 

                }
                else
                {
                    isDragging = true;
                    dragOffset = e.GetPosition(this.Parent as UIElement);
                    this.CaptureMouse();
                }
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                isResizing = true;
                dragOffset = e.GetPosition(this);
                originalWidth = this.ActualWidth;
                originalHeight = this.ActualHeight;
            }
        }

        protected void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newPosition = e.GetPosition(this.Parent as UIElement);

                double left = newPosition.X - dragOffset.X;
                double top = newPosition.Y - dragOffset.Y;

                Margin = new Thickness(left, top, 0, 0);
            }
            else if (isResizing)
            {
                Point newPosition = e.GetPosition(this);

                double newWidth = originalWidth + newPosition.X - dragOffset.X;
                double newHeight = originalHeight + newPosition.Y - dragOffset.Y;

                newWidth = Math.Max(MinWidth, Math.Min(MaxWidth, newWidth));
                newHeight = Math.Max(MinHeight, Math.Min(MaxHeight, newHeight));

                this.Width = newWidth;
                this.Height = newHeight;
            }
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
