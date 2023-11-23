using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Lemur.GUI
{
    public class WindowManager : Canvas
    {
        private ResizableWindow? targetWindow;
        private Point startDragPosition;
        private bool isDragging;
        private bool isResizing;

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            if (targetWindow != null)
                targetWindow = null;
            isDragging = false;
            isResizing = false;
        }
        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
             if (e is null)
                throw new ArgumentNullException(nameof(e));

            if ((!isResizing && !isDragging) || targetWindow == null)
                return;

            var pos = e.GetPosition(this);

            if (isResizing)
            {
                pos.X = Math.Clamp(pos.X, MinWidth, MaxWidth);
                pos.Y = Math.Clamp(pos.Y, MinHeight, MaxHeight);

                targetWindow.Width = pos.X;
                targetWindow.Height = pos.Y;
            }
            else if (isDragging)
            {
                var left = pos.X - startDragPosition.X;
                var top = pos.Y - startDragPosition.Y;

                SetLeft(targetWindow, Math.Clamp(left, 0, MaxWidth));
                SetTop(targetWindow, Math.Clamp(top, 0, MaxHeight));
            }
        }

        internal void BeginMove(ResizableWindow window, Point mousePos)
        {
            if (!isDragging)
            {
                window.BringToTopOfDesktop();
                startDragPosition = mousePos;
                targetWindow = window;
                isDragging = true;
            }
        }

        internal void BeginResize(ResizableWindow window)
        {
            if (!isResizing)
            {
                targetWindow = window;
                isResizing = true;
            }
        }
    }
}
