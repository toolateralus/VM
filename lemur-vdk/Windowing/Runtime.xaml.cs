using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Lemur.FS;
using Lemur.Windowing;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Lemur.GUI
{

    public partial class Runtime : Window
    {
        public Runtime()
        {
            InitializeComponent();
            IDBox.KeyDown += IDBox_KeyDown;

            OnWindowStateChanged += (ws) => WindowState = ws;

            IDBox.Text = "0";

            BootButton.Focus();

            StartPerpetualColorAnimation();
        }

        public static void LoadCustomSyntaxHighlighting()
        {
            var path = FileSystem.GetResourcePath("javascript_syntax_highlighting.xhsd");

            if (string.IsNullOrEmpty(path))
            {
                Notifications.Now("An error was encountered while parsing the JavaScript syntax highlighting file, which should be called javascript_syntax_highlighting.xhsd");
                return;
            }

            StreamReader sReader = new(path);

            using XmlReader reader = XmlReader.Create(sReader);

            IHighlightingDefinition jsHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);

            if (HighlightingManager.Instance.GetDefinition("JavaScriptCustom") is IHighlightingDefinition ihd && ihd != default && ihd != null)
                return;

            HighlightingManager.Instance.RegisterHighlighting("JavaScriptCustom", new[] { ".js" }, jsHighlighting);
        }

        public const string COMPUTER = "computer";
        public const string FASTBOOTFILENAME = "this.ins";

        public static Action<WindowState>? OnWindowStateChanged;
        private void IDBox_KeyDown(object sender, KeyEventArgs e)
        {

        }
        private void ReinstallComputerButton(object sender, RoutedEventArgs e)
        {
            var id = IDBox.Text;

            if (!uint.TryParse(id, out var cpu_id))
            {
                System.Windows.MessageBox.Show($"The computer id \"{id}\" was invalid. It must be a non-negative integer.");
                IDBox.Text = "0";
                return;
            }

            var workingDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\Lemur\\computer{cpu_id}";

            if (Directory.Exists(workingDir))
            {
                var result = MessageBox.Show($"Are you sure? this action will delete any existing data in  ..\\Appdata\\Lemur\\computer{cpu_id}", "Installer", MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.Delete(workingDir, true);
                    }
                    catch (Exception ee)
                    {
                        MessageBox.Show(ee.Message, "File/Directory may be in use");
                        return;
                    }
                }
                else return;
            }

            NewComputerButton(null, new());
        }
        private void NewComputerButton(object sender, RoutedEventArgs e)
        {
            var id = IDBox.Text;

            TryOpenComputerAtIDRecursive(id);
            Close();

            void TryOpenComputerAtIDRecursive(string id)
            {
                if (id.Contains(','))
                {
                    var split = id.Split(',');

                    foreach (var item in split)
                    {
                        TryOpenComputerAtIDRecursive(item);
                    }
                    return;
                }
                if (!uint.TryParse(id, out var cpu_id))
                {
                    System.Windows.MessageBox.Show($"The computer id \"{id}\" was invalid. It must be a non-negative integer.");
                    IDBox.Text = "0";
                    return;
                }

                Computer.Boot(cpu_id);
            }
        }
        internal static BitmapImage? GetAppIcon(string type)
        {
            var absPath = FileSystem.GetResourcePath(type) + "\\icon.bmp";

            // silent error, most commonly expected case.
            if (string.IsNullOrEmpty(absPath) || !File.Exists(absPath))
                return null;

            return Computer.LoadImage(absPath);
        }
        /// <summary>
        /// This will validate paths and load the respective js and xaml code from provided 'mydir.app' directory (any .app dir with at least one .xaml and .xaml.js file pair)
        /// Note that loading multiple JS files or even having multiple present in an app is not tested whatsoever, it may cause problems.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        internal static (string XAML, string JS) GetAppDefinition(string dir)
        {
            const string xamlExt = ".xaml";
            const string xamlJsExt = ".xaml.js";
            (string, string) failmsg = ("Not found!", "Not Found!");

            var absPath = FileSystem.GetResourcePath(dir);

            if (Directory.Exists(absPath))
            {
                string name = dir.Split('.').First();

                if (string.IsNullOrEmpty(name))
                {
                    Notifications.Now("");
                    return failmsg;
                }

                string xamlFile = Path.Combine(absPath, name + xamlExt);
                string jsFile = Path.Combine(absPath, dir.Split('.')[0] + xamlJsExt);

                if (File.Exists(xamlFile) && File.Exists(jsFile))
                {
                    var xaml = File.ReadAllText(xamlFile);
                    var js = File.ReadAllText(jsFile);
                    return (xaml, js);
                }
                else
                {
                    Notifications.Now("Matching XAML and XAML.js files not found.");
                }
            }

            return failmsg;
        }

        private readonly Color StartColor = Colors.MediumBlue;
        private readonly Color EndColor = Colors.MediumSlateBlue;
        private readonly TimeSpan AnimationDuration = TimeSpan.FromSeconds(5);
        private async void StartPerpetualColorAnimation()
        {
            while (true)
            {
                await AnimateBackgroundColor(StartColor, EndColor, AnimationDuration);
                await AnimateBackgroundColor(EndColor, StartColor, AnimationDuration);
            }
        }
        private async Task AnimateBackgroundColor(Color fromColor, Color toColor, TimeSpan duration)
        {
            const int steps = 1000;

            for (int step = 0; step <= steps; step++)
            {
                Color currentColor = Lerp(fromColor, toColor, step / (double)steps);
                Background = new SolidColorBrush(currentColor);
                await Task.Delay((int)duration.TotalMilliseconds / steps);
            }
        }
        private Color Lerp(Color from, Color to, double progress)
        {
            byte r = (byte)(from.R + (to.R - from.R) * progress);
            byte g = (byte)(from.G + (to.G - from.G) * progress);
            byte b = (byte)(from.B + (to.B - from.B) * progress);
            return Color.FromRgb(r, g, b);
        }
    }
}
