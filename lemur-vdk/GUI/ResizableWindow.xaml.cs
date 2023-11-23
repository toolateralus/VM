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
        public Action? OnClosed { get; internal set; }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if (e is null)
                throw new ArgumentNullException(nameof(e));

            if (Parent is not WindowManager windowManager)
                return;

            var position = e.GetPosition(windowManager);

            if (e.LeftButton == MouseButtonState.Pressed)
                windowManager.BeginMove(this, position);
            else if (e.RightButton == MouseButtonState.Pressed)
                windowManager.BeginResize(this);
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
    }
}
