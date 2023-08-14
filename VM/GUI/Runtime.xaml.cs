using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using VM.OS;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using System.Linq;
using System.Windows.Controls;
using System.Collections.Concurrent;

namespace VM.GUI
{

    public partial class Runtime : Window
    {

        public static Dictionary<Computer, ComputerWindow> Computers = new();

        public Runtime()
        {
            InitializeComponent();
            IDBox.KeyDown += IDBox_KeyDown;
            
            onWindowStateChanged += (ws) => WindowState = ws;

            using (XmlReader reader = XmlReader.Create(new StringReader(JAVASCRIPT_SYNTAX_HIGHLIGHTING.HIGHLIGHTING)))
            {
                IHighlightingDefinition jsHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                HighlightingManager.Instance.RegisterHighlighting("JavaScriptCustom", new[] { ".js" }, jsHighlighting);
            }

            IDBox.Focus();
            IDBox.Text = "0";

            StartPerpetualColorAnimation();

            const string COMPUTER = "computer";
            const string FAST_BOOT_FILENAME = "this.ins";

            if (GetResourcePath(FAST_BOOT_FILENAME) is string path && path.Contains(COMPUTER))
            {
                int startIndex = path.IndexOf(COMPUTER) + COMPUTER.Length;

                if (startIndex < path.Length)
                {
                    string computerNumber = path.Substring(startIndex);
                    int endIndex = computerNumber.IndexOfAny(new char[] { '/', '\\' });

                    if (endIndex != -1)
                    {
                        computerNumber = computerNumber.Substring(0, endIndex);
                    }

                    if (uint.TryParse(computerNumber, out uint number))
                    {
                        InstantiateComputer(number);
                        Close();
                    }
                }
            }


        }
        public static Action<WindowState>? onWindowStateChanged;
        public static void Restart(uint id)
        {

        }

        #region Color Animation
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
        #endregion

        private void IDBox_KeyDown(object sender, KeyEventArgs e)
        {
            
        }
        public static ComputerWindow GetWindow(Computer pc)
        {
            // TODO : fix this;
            try
            {
                return Computers[pc];
            }
            catch
            {

            }
            return null;
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

                InstantiateComputer(cpu_id);
            }
        }
        private static void InstantiateComputer(uint cpu_id)
        {
            OS.Computer pc = new(cpu_id);
            ComputerWindow wnd = new(pc);
            Computers[pc] = wnd;
            pc.FinishInit(wnd);
            onWindowStateChanged?.Invoke(WindowState.Minimized);
        }

        public static ConcurrentDictionary<int, (object? val, int replyCh)> NetworkEvents = new();
        public static (object? value, int reply) PullEvent(int channel, Computer computer)
        {
            (object? val, int replyCh) val;
            while (!NetworkEvents.Remove(channel, out val) && !computer.Disposing)
            {
                Thread.SpinWait(1);
            }
            return val;
        }
        internal static void Broadcast(int outCh, int inCh, object? msg)
        {
            NetworkEvents[outCh] = (msg, inCh);
        }
        internal static string GetResourcePath(string name)
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VM";

            VerifyOrCreateAppdataDir(path);

            string[] entries = Directory.GetFileSystemEntries(path, name, SearchOption.AllDirectories);

            return entries.FirstOrDefault();
        }

        internal static void VerifyOrCreateAppdataDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        internal static (string XAML, string JS) GetAppDefinition(Computer pc, string dir)
        {
            const string xamlExt = ".xaml";
            const string xamlJsExt = ".xaml.js";

            var absPath = GetResourcePath(dir);

            if (Directory.Exists(absPath))
            {
                string name = dir.Split('.')[0];
                string xamlFile = Path.Combine(absPath, name + xamlExt);
                string jsFile = Path.Combine(absPath, dir.Split('.')[0]  + xamlJsExt);

                if (File.Exists(xamlFile) && File.Exists(jsFile))
                {
                    return (File.ReadAllText(xamlFile), File.ReadAllText(jsFile));
                }
                else
                {
                    Notifications.Now("Matching XAML and XAML.js files not found.");
                }
            }

            return ("Not found!", "Not Found!");
        }
        public static T SearchForOpenWindowType<T>(Computer Computer)
        {
            var wnd = Runtime.GetWindow(Computer);

            foreach (var window in wnd.USER_WINDOW_INSTANCES)
                    if (window.Value is UserWindow userWindow && userWindow.Content is Grid g)
                    {
                        foreach (var item in g.Children)
                        {
                            if (item is Frame frame)
                            {
                                if (frame.Content is T ActualApplication)
                                {
                                    return ActualApplication;
                                }
                            }
                        }

                    }
            return default;

        }

    }
}
