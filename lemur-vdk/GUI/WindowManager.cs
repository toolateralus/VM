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
        private static Vector resizeMargin = new(10, 10);
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
                ResizableWindow window = targetWindow;
                MoveWindow(window, left, top);
            }
        }

        private void MoveWindow(ResizableWindow window, double left, double top)
        {
            SetLeft(window, Math.Clamp(left, -resizeMargin.X, MaxWidth));
            SetTop(window, Math.Clamp(top, -resizeMargin.Y, MaxHeight));
        }

        private void PerformResize(ResizableWindow window, ResizeEdge edge, Point relPos)
        {
            switch (edge)
            {
                case ResizeEdge.None:
                    break;
                case ResizeEdge.TopLeft:
                    relPos -= resizeMargin;
                    Vector winPos = new(GetLeft(window) + relPos.X, GetTop(window) + relPos.Y);
                    Point winSize = new(window.Width - relPos.X, window.Height - relPos.Y);
                    window.Resize(winSize);
                    MoveWindow(window, winPos.X, winPos.Y);
                    break;
                case ResizeEdge.TopCenter:
                    break;
                case ResizeEdge.TopRight:
                    break;
                case ResizeEdge.CenterLeft:
                    break;
                case ResizeEdge.CenterRight:
                    relPos.X += resizeMargin.X;
                    relPos.Y = window.Height;
                    window.Resize(relPos);
                    break;
                case ResizeEdge.BottomLeft:
                    break;
                case ResizeEdge.BottomCenter:
                    relPos.Y += resizeMargin.Y;
                    relPos.X = window.Width;
                    window.Resize(relPos);
                    break;
                case ResizeEdge.BottomRight:
                    window.Resize(relPos + resizeMargin);
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

        internal void BeginResize(ResizableWindow window, ResizeEdge edge, Point relPos)
        {
            if (!isResizing)
            {
                window.BringToTopOfDesktop();
                PerformResize(window, edge, relPos);
                resizingEdge = edge;
                targetWindow = window;
                isResizing = true;
            }
        }
    }
}
