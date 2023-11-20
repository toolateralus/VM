using System;
using System.Windows;
using System.Windows.Controls;
using Lemur;
using Lemur.JS;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Lemur.GUI
{
    /// <summary>
    /// Interaction logic for UserWindow.xaml needs work big time.
    /// </summary>
    public partial class UserWindow : UserControl
    {
        public ResizableWindow Owner;
        internal Action? OnClosed;
        public double lastW = 0, lastH = 0;
        public Point lastPos = new();
        public bool Maximized = false;
        public JavaScriptEngine JavaScriptEngine;
        public UserWindow()
        {
            InitializeComponent();
        }
        internal void InitializeUserContent(ResizableWindow frame, UserControl actualUserContent, JavaScriptEngine engine)
        {
            Owner = frame;
            ContentsFrame.Content = actualUserContent;
            JavaScriptEngine = engine;
            
            if (engine != null) 
                OnClosed += engine.Dispose;

            object? unused_param = null;

            // these args are just from the wpf event handler and don't get used.
            ToggleMaximize((object)unused_param, (RoutedEventArgs)unused_param); 
        }
        internal void Close()
        {
            JavaScriptEngine?.Dispose();
            if (Owner is not null && Owner.Parent is Canvas canvas)
            {
                canvas.Children.Remove(Owner);
            }
            OnClosed?.Invoke();
        }
        /// <summary>
        /// Wrapper for the button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Todo : Fix this strange problem where resizing is inconsistent when min/maximized
        public void ToggleVisibility(object sender, RoutedEventArgs e)
        {

            Visibility ^= Visibility.Collapsed;
            Owner.Visibility = Visibility;
           
            if (Visibility == Visibility.Visible)
            {
                Owner.BringToTopOfDesktop();
            }

            if (Visibility == Visibility.Hidden)
                Visibility = Visibility.Visible;
        }
        // Todo : Fix this strange problem where resizing is inconsistent when min/maximized
        public void ToggleMaximize(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Visible;
            if (!Maximized)
            {
                Maximize();
                return;
            }
            UnMaximize();
        }
        /// <summary>
        /// Not quite a minimize, just a maximization cancel.
        /// </summary>
        private void UnMaximize()
        {
            Maximized = false;
            Owner.Height = lastH;
            Owner.Width = lastW;
            Canvas.SetLeft(Owner, lastPos.X);
            Canvas.SetTop(Owner, lastPos.Y);
        }
        private void Maximize()
        {
            Maximized = true;
            lastW = Owner.Width;
            lastH = Owner.Height;
            lastPos = new(Canvas.GetLeft(Owner), Canvas.GetTop(Owner));
            Canvas.SetTop(Owner, 0);
            Canvas.SetLeft(Owner, 0);
            Owner.Width = 1920;
            Owner.Height = 1080;
            Owner.BringToTopOfDesktop();

        }
    }
}
