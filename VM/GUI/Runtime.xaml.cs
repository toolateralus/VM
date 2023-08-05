using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VM;
using VM.OPSYS;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Xml;
using System.Windows.Forms;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using System.Threading;
using System.Net.NetworkInformation;
using System.Threading.Channels;

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

        private void IDBox_KeyDown(object sender, KeyEventArgs e)
         {
            var regex = new Regex("[^0-9]+");
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
                System.Windows.MessageBox.Show("The inputted computer id was invalid. It must be a non-negative integer.");
                return;
            }

            Computer pc = new(cpu_id);
            ComputerWindow wnd = new(pc);

            Computers[pc] = wnd;

            wnd.Show();
            wnd.Closed += (o, e) => Computers.Remove(pc);
        }

        public static Dictionary<int, (object? val, int replyCh)> NetworkEvents = new();
        public static object? PullEvent(int channel)
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
            NetworkEvents[outCh] = (msg , inCh);
        }
    }
}
