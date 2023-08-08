using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VM.OS;

namespace VM.GUI
{
    /// <summary>
    /// Interaction logic for UserWebApplet.xaml
    /// </summary>
    public partial class UserWebApplet : UserControl
    {
        public UserWebApplet()
        {
            InitializeComponent();
            
        }
        Computer computer;
        public void LateInit(Computer computer)
        {
            this.computer = computer;
        }

        public void Navigate(string appName)
        {
            Uri uri = new Uri(Runtime.GetResourcePath(appName , ".index.html"), UriKind.RelativeOrAbsolute);
            webBrowser.Navigate(uri);
        }

    }
}
