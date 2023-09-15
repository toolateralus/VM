using System;
using VM.JS;
using VM;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace VM.Avalonia
{
    /// <summary>
    /// Interaction logic for UserWindow.xaml
    /// </summary>
    public partial class UserWindow : UserControl
    {
        public ResizableWindow Owner;
        public Action? OnClosed;
        public double lastW = 0, lastH = 0;
        public Point lastPos = new();
        public bool Maximized = false;
        public JavaScriptEngine JavaScriptEngine;
        public UserWindow()
        {
            InitializeComponent();
        }
        public void InitializeUserContent(ResizableWindow frame, UserControl actualUserContent, JavaScriptEngine engine)
        {
            Owner = frame;
            ContentsContentControl.Content = actualUserContent;
            this.JavaScriptEngine = engine;
            
            if (engine != null) 
                OnClosed += engine.Dispose;
        }
        public void Close()
        {
            JavaScriptEngine?.Dispose();
            if (Owner is not null && Owner.Parent is Canvas canvas)
            {
                Owner.OnClosed?.Invoke();
                canvas.Children.Remove(Owner);
            }
            OnClosed?.Invoke();
        }
        /// <summary>
        /// Wrapper for the button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Todo : Fix this strange problem where resizing is inconsistent when min/maximized
        public void ToggleVisibility(object sender, RoutedEventArgs e)
        {
            
            IsVisible = !IsVisible;
            
            if (IsVisible)
                Owner.BringToTopOfDesktop();

        }
        // Todo : Fix this strange problem where resizing is inconsistent when min/maximized
        public void ToggleMaximize(object sender, RoutedEventArgs e)
        {
            IsVisible = true;
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
        public void UnMaximize()
        {
            Maximized = false;
            Owner.Height = lastH;
            Owner.Width = lastW;
            Canvas.SetLeft(Owner, lastPos.X);
            Canvas.SetTop(Owner, lastPos.Y);
        }
        public void Maximize()
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
