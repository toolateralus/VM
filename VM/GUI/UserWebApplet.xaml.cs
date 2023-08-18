using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using VM;

namespace VM.GUI
{
    public partial class UserWebApplet : UserControl
    {
        private bool webViewInitialized = false;

        public UserWebApplet()
        {
            InitializeComponent();
            chromiumBrowser.Loaded += ChromiumBrowser_Loaded;
        }

        private async void ChromiumBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebViewAsync();
            Navigate(Path);
        }

        private async Task InitializeWebViewAsync()
        {
            await chromiumBrowser.EnsureCoreWebView2Async();
            webViewInitialized = true;
        }

        Computer computer;
        public string Path;
        public void LateInit(Computer computer)
        {
            this.computer = computer;
        }

        public void Navigate(string appName)
        {
            if (webViewInitialized)
            {
                var html = Runtime.GetResourcePath(appName + ".index.html");
                
                if (File.Exists(html)) 
                    chromiumBrowser.NavigateToString(File.ReadAllText(html));

            }
        }
    }
}
