using System;
using System.Windows;
using System.Windows.Controls;
using VM.OS;
using VM.OS.JS;

namespace VM.GUI
{
    /// <summary>
    /// Interaction logic for UserWindow.xaml
    /// </summary>
    public partial class UserWindow : UserControl
    {
        public ResizableWindow Owner;
        internal Action? OnClosed;

        public UserWindow()
        {
            InitializeComponent();
        }
        public JavaScriptEngine JavaScriptEngine;
        internal void Init(ResizableWindow frame, UserControl actualUserContent, JavaScriptEngine engine)
        {
            Owner = frame;
            ContentsFrame.Content = actualUserContent;
            this.JavaScriptEngine = engine;
            
            if (engine != null) 
                OnClosed += engine.Dispose;
        }


        internal void Destroy()
        {

            if (Owner is not null && Owner.Parent is Canvas canvas)
            {
                Owner.OnClosed?.Invoke();
                canvas.Children.Remove(Owner);
            }
            OnClosed?.Invoke();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Destroy();
        }

        public void ToggleVisibility(object sender, RoutedEventArgs e)
        {
            Visibility ^= Visibility.Collapsed;
            Owner.Visibility = Visibility;
           
            if (Visibility == Visibility.Visible)
                Owner.BringToTopOfDesktop();

        }

        public double lastW = 0, lastH = 0;
        public Thickness lastMargin = new();
        public bool Maximized = false;
        public void ToggleMaximize(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Visible;
            if (!Maximized)
            {
                Maximized = true;
                lastW = Owner.Width;
                lastH = Owner.Height;
                lastMargin = Owner.Margin;

                Owner.Height = 1080;
                Owner.Width = 1920;
                Owner.Margin = new(0, 0, 0, 0);

                return;
            }

            Maximized = false;

            Owner.Height = lastH;
            Owner.Width = lastW;
            Owner.Margin = lastMargin;
            Owner.BringToTopOfDesktop();
        }
    }

   
}
