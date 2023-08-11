using System;
using System.Windows;
using System.Windows.Controls;

namespace VM.GUI
{
    /// <summary>
    /// Interaction logic for UserWindow.xaml
    /// </summary>
    public partial class UserWindow : UserControl
    {
        private ResizableWindow owner;
        internal Action? OnClosed;

        public UserWindow()
        {
            InitializeComponent();
            
        }

        internal void Init(ResizableWindow frame, UserControl contents)
        {
            owner = frame;
            ContentsFrame.Content = contents;
        }
        internal void Destroy()
        {
            if (owner is null || owner.Parent is not Canvas canvas)
                throw new InvalidOperationException("Window was destroyed but it had no parent");

            owner.OnClosed?.Invoke();

            canvas.Children.Remove(owner);
            OnClosed?.Invoke();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Destroy();
        }

        public void ToggleVisibility(object sender, RoutedEventArgs e)
        {
            Visibility ^= Visibility.Collapsed;
            owner.Visibility = Visibility;
           
            if (Visibility == Visibility.Visible)
                owner.BringToTopOfDesktop();

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
                lastW = owner.Width;
                lastH = owner.Height;
                lastMargin = owner.Margin;

                owner.Height = 1080;
                owner.Width = 1920;
                owner.Margin = new(0, 0, 0, 0);

                return;
            }

            Maximized = false;

            owner.Height = lastH;
            owner.Width = lastW;
            owner.Margin = lastMargin;
            owner.BringToTopOfDesktop();
        }
    }

   
}
