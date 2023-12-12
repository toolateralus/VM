﻿using Lemur.JavaScript.Api;
using Lemur.JS;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;

namespace Lemur.GUI
{
    /// <summary>
    /// Interaction logic for UserWindow.xaml needs work big time.
    /// </summary>
    public partial class UserWindow : UserControl
    {
        public ResizableWindow Owner;
        /// <summary>
        /// Called by the UI thread to clean up any resources.
        /// </summary>
        internal event Action? OnAppClosed;
        public Engine JavaScriptEngine;
        public UserWindow()
        {
            InitializeComponent();
            xBtn.Click += CloseWindow;

            // TODO: fix this up
            minimizeBtn.Click += (_, _) => Owner?.ToggleVisibility();

            maximizeBtn.Click += (_, _) => Owner?.ToggleMaximize();

            long lastClickedTime = 0;

            Toolbar.MouseLeftButtonDown += (_, e) =>
            {
                if (DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastClickedTime < 500)
                    Owner?.ToggleMaximize();
                else
                    Owner?.BeginMove(e.GetPosition(this));

                lastClickedTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                e.Handled = true;
            };

            Computer.Current.Window.PreviewKeyDown += UserWindow_PreviewKeyDown;

        }
        private void UserWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && 
                Keyboard.IsKeyDown(Key.LeftCtrl) && 
                Keyboard.IsKeyDown(Key.LeftShift))
            {
                Close();
            }
                
            if (JavaScriptEngine == null)
                return;

            if (JavaScriptEngine.EventHandlers.Count == 0)
                return;

            var events = JavaScriptEngine.EventHandlers.Where(e => e is InteropEvent iE
                                                                && iE.Event == XAML_EVENTS.KEY_DOWN).ToList();

            if (events.Count == 0)
                return;

            var interopEvents = events.Cast<InteropEvent>();

            foreach (var interopEvent in interopEvents)
                interopEvent.InvokeKeyboard(sender, e);
        }
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            Owner?.BringToTopOfDesktop();
        }
        internal void InitializeContent(ResizableWindow frame, UserControl actualUserContent, Engine? engine)
        {
            Owner = frame;

            ContentsFrame.Content = actualUserContent;

            if (engine != null)
                JavaScriptEngine = engine;
        }
        internal void Close()
        {
            JavaScriptEngine?.Dispose();
            OnAppClosed?.Invoke();
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
