using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Lemur.FS;
using Lemur.Windowing;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Lemur.GUI {
    /// <summary>
    /// The MainWindow of the app, responsible for launching & initializing a 'Computer',
    /// and some helpers.
    /// </summary>
    public partial class Runtime : Window
    {
        public Runtime()
        {
            InitializeComponent();
            IDBox.KeyDown += IDBox_KeyDown;

            OnWindowStateChanged += (ws) => WindowState = ws;

            IDBox.Text = "0";

            BootButton.Focus();
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

                // instantiate singleton.
                Computer pc = new(cpu_id);

                LoadCustomSyntaxHighlighting();
            }
        }
        internal static BitmapImage? GetAppIcon(string type)
        {
            var absPath = FileSystem.GetResourcePath(type + ".app") + $"\\{type}.ico";

            if (string.IsNullOrEmpty(absPath) || !File.Exists(absPath))
                return null;

            return Computer.LoadImage(absPath);
        }

    }
}
