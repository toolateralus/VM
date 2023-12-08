using Lemur.GUI;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Lemur.GUI
{
    public partial class ResizableWindow : Frame
    {
        public double lastW = 0, lastH = 0;
        public Point lastPos = new();
        public bool Maximized = false;
        public Action? OnClosed { get; internal set; }

        internal void BeginResize(ResizeEdge edge, Point relPos)
        {
            if (Parent is not WindowManager windowManager)
                return;
            windowManager.BeginResize(this, edge, relPos);
        }

        internal void BeginMove(Point position)
        {
            if (Parent is not WindowManager windowManager)
                return;
            windowManager.BeginMove(this, position);
        }

        public void BringToTopOfDesktop()
        {
            if (Parent is Canvas grid && grid.Children.Contains(this))
            {
                grid.Children.Remove(this);
                grid.Children.Add(this);
                Panel.SetZIndex(this, Computer.Current.Window.TopMostZIndex);
            }
        }
        internal void ToggleVisibility()
        {

            Visibility ^= Visibility.Collapsed;

            if (Visibility == Visibility.Visible)
                BringToTopOfDesktop();

            if (Visibility == Visibility.Hidden)
                Visibility = Visibility.Visible;
        }
        public void ToggleMaximize()
        {
            Visibility = Visibility.Visible;
            if (!Maximized)
                Maximize();
            else
                UnMaximize();
        }
        /// <summary>
        /// Not quite a minimize, just a maximization cancel.
        /// </summary>
        private void UnMaximize()
        {
            Maximized = false;
            Height = lastH;
            Width = lastW;
            Canvas.SetLeft(this, lastPos.X);
            Canvas.SetTop(this, lastPos.Y);
        }
        private void Maximize()
        {
            Maximized = true;
            lastW = Width;
            lastH = Height;
            lastPos = new(Canvas.GetLeft(this), Canvas.GetTop(this));
            Canvas.SetTop(this, 0);
            Canvas.SetLeft(this, 0);
            Width = SystemParameters.PrimaryScreenWidth - 5;
            Height = SystemParameters.PrimaryScreenHeight - 25;
            BringToTopOfDesktop();

        }

        internal void Resize(double width, double height)
        {
            width = Math.Max(25, width);
            height = Math.Max(25, height);
            Width = width;
            Height = height;
        }
    }
}
