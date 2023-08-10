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
        private double originalWidth;
        private double originalHeight;

        public ResizableWindow()
        {
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseLeave += onMouseLeave;
            MinWidth = 50;
            MinHeight = 50;
            MaxWidth = 1920;
            MaxHeight = 1080;
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
            dragOffset = e.GetPosition(App.Current.MainWindow as Runtime);
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
            if (isDragging)
            {
                Point newPosition = e.GetPosition(App.Current.MainWindow as Runtime);

                double left = newPosition.X - dragOffset.X;
                double top = newPosition.Y - dragOffset.Y;

                Canvas.SetLeft(this, left);
                Canvas.SetTop(this, top);
            }
            else if (isResizing)
            {
                Point newPosition = e.GetPosition(App.Current.MainWindow as Runtime);

                double newWidth = originalWidth + newPosition.X - dragOffset.X;
                double newHeight = originalHeight + newPosition.Y - dragOffset.Y;

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
