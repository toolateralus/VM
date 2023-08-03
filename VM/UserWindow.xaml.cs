﻿using System;
using System.Windows;
using System.Windows.Controls;

namespace VM
{
    /// <summary>
    /// Interaction logic for UserWindow.xaml
    /// </summary>
    public partial class UserWindow : UserControl
    {
        private ResizableWindow? owner;
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
            if (owner is null || owner.Parent is not Grid grid)
                throw new InvalidOperationException("Window was destroyed but it had no parent");

            grid.Children.Remove(owner);
            OnClosed?.Invoke();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Destroy();
        }

        public void Minimize(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Collapsed;

            if (owner != null)
                owner.Visibility = Visibility;
        }

        public void ToggleMaximize(object sender, RoutedEventArgs e)
        {
            Visibility ^= Visibility.Collapsed;

            if (owner != null)
                owner.Visibility = Visibility;
        }
    }

   
}
