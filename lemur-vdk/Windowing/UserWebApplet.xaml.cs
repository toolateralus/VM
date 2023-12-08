using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Lemur;
using Lemur.FS;

namespace Lemur.GUI
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

        public string Path;
        public void LateInit(Computer computer)
        {

        }

        public void Navigate(string appName)
        {
            if (webViewInitialized)
            {
                var html = FileSystem.GetResourcePath(appName + ".index.html");
                
                if (File.Exists(html)) 
                    chromiumBrowser.NavigateToString(File.ReadAllText(html));

            }
        }
    }
}
