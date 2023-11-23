using Lemur.GUI;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Lemur.GUI
{
    public class WindowManager : Canvas
    {
        private ResizableWindow? targetWindow;
        private ResizeEdge resizingEdge;
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

            if (targetWindow == null || (!isResizing && !isDragging))
                return;


            if (isResizing)
            {
                PerformResize(targetWindow, resizingEdge, e.GetPosition(targetWindow));
            }
            else if (isDragging)
            {
                // Todo: fix window going to (0,0) every second time you click on it;
                var pos = e.GetPosition(this);
                var left = pos.X - startDragPosition.X;
                var top = pos.Y - startDragPosition.Y;
                SetLeft(targetWindow, Math.Clamp(left, 0, MaxWidth));
                SetTop(targetWindow, Math.Clamp(top, 0, MaxHeight));
            }
        }

        private static void PerformResize(ResizableWindow window, ResizeEdge edge, Point relPos)
        {
            switch (edge)
            {
                case ResizeEdge.None:
                    break;
                case ResizeEdge.TopLeft:
                    break;
                case ResizeEdge.TopCenter:
                    break;
                case ResizeEdge.TopRight:
                    break;
                case ResizeEdge.CenterLeft:
                    break;
                case ResizeEdge.CenterRight:
                    break;
                case ResizeEdge.BottomLeft:
                    break;
                case ResizeEdge.BottomCenter:
                    break;
                case ResizeEdge.BottomRight:
                    window.Resize(relPos);
                    break;
                default:
                    break;
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

        internal void BeginResize(ResizableWindow window, ResizeEdge edge)
        {
            if (!isResizing)
            {
                resizingEdge = edge;
                targetWindow = window;
                isResizing = true;
            }
        }
    }
}
