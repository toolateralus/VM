using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Lemur.GUI
{
    public class WindowManager : Canvas
    {
        ResizableWindow? targetWindow;
        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);
            if (targetWindow != null)
            {
                targetWindow = null;
                e.Handled = true;
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (targetWindow == null)
                return;
            var newSize = e.GetPosition(targetWindow);
            newSize.X = Math.Clamp(newSize.X, MinWidth, MaxWidth);
            newSize.Y = Math.Clamp(newSize.Y, MinHeight, MaxHeight);

            targetWindow.Width = newSize.X;
            targetWindow.Height = newSize.Y;
            e.Handled = true;
        }
        internal void BeginResize(ResizableWindow window)
        {
            targetWindow = window;
        }
    }
}
