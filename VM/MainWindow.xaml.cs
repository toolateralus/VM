using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VM;

namespace VM
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetBackground(@"Background.png");
        }

        // singleton
        public OS os = new();

        private void SetBackground(string path)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bitmapImage.EndInit();
            desktopBackground.Source = bitmapImage;
        }

        public Dictionary<string, ResizableWindow> Windows = new();
       
        private void Open<T>(string title = "window", int width = 200, int height = 100, Brush? background = null, Brush? foreground = null) where T : UserWindow, new()
        {
            background ??= Brushes.LightGray;
            foreground ??= Brushes.Black;

            var window = new T();
            window.Title = title;
            

            if (Windows.TryGetValue(title, out var value))
            {
                Windows.Remove(title);
                Desktop.Children.Remove(value);
            }

            var frame = new ResizableWindow
            {
                Content = window,
                Width = width,
                Height = height,
                Background = background,
                Foreground = foreground,
            };
            window.Init(frame);

            Windows.Add(title, frame);

            Desktop.Children.Add(frame);
        }


        private void Taskbar_Click(object sender, RoutedEventArgs e)
        {
            Open<UserWindow>("MyWindow");
        }
    }
}
