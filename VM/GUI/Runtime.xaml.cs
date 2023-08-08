﻿using ICSharpCode.AvalonEdit.Highlighting.Xshd;
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

namespace VM.GUI
{

    public partial class Runtime : Window
    {

        public static Dictionary<Computer, ComputerWindow> Computers = new();

        public Runtime()
        {
            InitializeComponent();
            IDBox.KeyDown += IDBox_KeyDown;

            using (XmlReader reader = XmlReader.Create(new StringReader(JAVASCRIPT_SYNTAX_HIGHLIGHTING.HIGHLIGHTING)))
            {
                IHighlightingDefinition jsHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                HighlightingManager.Instance.RegisterHighlighting("JavaScriptCustom", new[] { ".js" }, jsHighlighting);
            }

            IDBox.Focus();
            IDBox.Text = "0";

            StartPerpetualColorAnimation();

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

        Regex regex = new Regex("[^0-9]+");
        private void IDBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = regex.IsMatch(IDBox.Text);
        }
        public static ComputerWindow GetWindow(Computer pc)
        {
            return Computers[pc];
        }
        private void NewComputerButton(object sender, RoutedEventArgs e)
        {
            var id = IDBox.Text;

            if (!uint.TryParse(id, out var cpu_id))
            {
                System.Windows.MessageBox.Show($"The computer id \"{id}\" was invalid. It must be a non-negative integer.");
                IDBox.Text = "0";
                return;
            }

            OS.Computer pc = new(cpu_id);
            ComputerWindow wnd = new(pc);
            Computers[pc] = wnd;

            pc.OS.InstallApplication("CommandPrompt.app", typeof(CommandPrompt));
            pc.OS.InstallApplication("FileExplorer.app", typeof(FileExplorer));
            pc.OS.InstallApplication("TextEditor.app", typeof(TextEditor));
            pc.OS.InstallApplication("Browser.app", typeof(UserWebApplet));

            wnd.Show();
            wnd.Closed += (o, e) =>
            {
                Computers.Remove(pc);
                pc.Shutdown();
            };
        }

        public static Dictionary<int, (object? val, int replyCh)> NetworkEvents = new();
        public static (object? value, int reply) PullEvent(int channel)
        {
            while (!NetworkEvents.TryGetValue(channel, out _))
            {
                Thread.SpinWait(1);
            }
            var value = NetworkEvents[channel];
            NetworkEvents.Remove(channel);
            return value;

        }

        internal static void Broadcast(int outCh, int inCh, object? msg)
        {
            NetworkEvents[outCh] = (msg, inCh);
        }

        internal static string? GetResourcePath(string name, string ext)
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VM";

            VerifyOrCreateAppdataDir(path);

            string[] files = Directory.GetFiles(path, name + ext, SearchOption.AllDirectories);

            if (files.Length > 0)
            {
                return files[0];
            }

            return null;
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

            var path = pc.OS.FS_ROOT;

            var absDir = Path.Combine(path, dir);

            if (Directory.Exists(absDir))
            {
                string name = dir.Split('.')[0];
                string xamlFile = Path.Combine(absDir, name + xamlExt);
                string jsFile = Path.Combine(absDir, dir.Split('.')[0]  + xamlJsExt);

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
    }
}
