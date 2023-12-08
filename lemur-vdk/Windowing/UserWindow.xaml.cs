﻿using System;
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

            maximizeBtn.Click += (_,_) => Owner?.ToggleMaximize();

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
        }
        internal void InitializeUserContent(ResizableWindow frame, UserControl actualUserContent, Engine? engine)
        {
            Owner = frame;

            ContentsFrame.Content = actualUserContent;
            
            if (engine != null)
                JavaScriptEngine = engine;
        }
        internal void Close()
        {
            JavaScriptEngine?.Dispose();
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