using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Lemur.GUI;
using Lemur;
using Lemur.JS;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Button = System.Windows.Controls.Button;

namespace Lemur.GUI
{
    /// <summary>
    /// Interaction logic for UserWindow.xaml needs work big time.
    /// </summary>
    public partial class UserWindow : UserControl
    {
        public ResizableWindow Owner;
        internal Action? OnClosed;
        
        public Engine JavaScriptEngine;
        public UserWindow()
        {
            InitializeComponent();
            xBtn.Click += CloseWindow;

            // TODO: fix this up
            minimizeBtn.Click += (_, _) => Owner?.ToggleVisibility();
            maximizeBtn.Click += (_, _) => Owner?.ToggleMaximize();
        }
        internal void InitializeUserContent(ResizableWindow frame, UserControl actualUserContent, Engine? engine)
        {
            Owner = frame;

            ContentsFrame.Content = actualUserContent;
            
            // for js/wpf apps. otherwise- this could be a C#/WPF user app like the cmd prompt
            if (engine != null)
            {
                JavaScriptEngine = engine;
                OnClosed += engine.Dispose;
            }
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
            e.Handled = true;
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            Owner.BeginMove(e.GetPosition(this));
            e.Handled = true;
        }
        private void OnResizeBorderClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string tag)
                return;
            ResizeEdge edge = (ResizeEdge)Enum.Parse(typeof(ResizeEdge), tag);
            Owner.BeginResize(edge, e.GetPosition(this));
            e.Handled = true;
        }
    }
}
