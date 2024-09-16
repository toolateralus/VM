using Lemur.FS;
using Lemur.GUI;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Lemur.Windowing {
    /// <summary>
    /// Interaction logic for Browser.xaml
    /// </summary>
    public partial class Browser : UserControl
    {
        public static string? DesktopIcon => FileSystem.GetResourcePath("ms-edge-logo.jpg");
        public Browser()
        {
            InitializeComponent();
        }
        public async void LateInit(string _, Computer pc, ResizableWindow win)
        {
            await WebView.EnsureCoreWebView2Async(null).ConfigureAwait(true);
            WebView.CoreWebView2.Navigate("https://www.github.com/toolateralus/lemur-vdk/issues");
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (WebView.CanGoBack)
            {
                WebView.GoBack();
            }
        }
        private void WebView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.XButton1 == MouseButtonState.Pressed && WebView.CanGoBack)
            {
                WebView.GoBack();
            }
            else if (e.XButton2 == MouseButtonState.Pressed && WebView.CanGoForward)
            {
                WebView.GoForward();
            }
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Uri.TryCreate(AddressBar.Text, UriKind.Absolute, out var uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                {
                    WebView.CoreWebView2.Navigate(AddressBar.Text);
                    Notifications.Now($"Navigating to {AddressBar.Text}");
                }
                else
                {
                    Notifications.Now($"Invalid URL: {AddressBar.Text}");
                }
            }
        }
        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (WebView.CanGoForward)
            {
                WebView.GoForward();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            WebView.Reload();
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            WebView.CoreWebView2.Navigate(AddressBar.Text);
            Notifications.Now($"Navigating to {AddressBar.Text}");
        }
    }
}