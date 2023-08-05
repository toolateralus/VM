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

namespace VM.GUI
{
    public static class JAVASCRIPT_SYNTAX_HIGHLIGHTING
    {
        public const string HIGHLIGHTING = @"<?xml version=""1.0""?>
            <!-- syntax definition for JavaScript 2.0 by Svante Lidman -->
            <!-- converted to AvalonEdit format by Siegfried Pammer in 2010 -->
            <!-- forked by me in 2023, not for commercial use -->

            <SyntaxDefinition name=""JavaScriptCustom"" extensions="".js"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
                <Color name=""Digits"" foreground=""#6D9C66"" exampleText=""3.14"" />
                <Color name=""Comment"" foreground=""#607D8B"" exampleText=""// comment"" />
                <Color name=""String"" foreground=""#D28258"" exampleText=""var text = &quot;Hello, World!&quot;;"" />
                <Color name=""Character"" foreground=""#D28258"" exampleText=""var char = 'a';"" />
                <Color name=""Regex"" foreground=""#D28258"" exampleText=""/abc/m"" />
                <Color name=""JavaScriptKeyWords"" foreground=""#82B1FF"" exampleText=""return myVariable;"" />
                <Color name=""JavaScriptIntrinsics"" foreground=""#8CDAFF"" exampleText=""Math.random()"" />
                <Color name=""JavaScriptLiterals"" foreground=""#82B1FF"" exampleText=""return false;"" />
                <Color name=""JavaScriptGlobalFunctions"" foreground=""#82B1FF"" exampleText=""escape(myString);"" />
                <RuleSet ignoreCase=""false"">
                    <Keywords color=""JavaScriptKeyWords"">
                        <Word>break</Word>
                        <Word>continue</Word>
                        <Word>delete</Word>
                        <Word>else</Word>
                        <Word>for</Word>
                        <Word>function</Word>
                        <Word>if</Word>
                        <Word>in</Word>
                        <Word>let</Word>
                        <Word>new</Word>
                        <Word>return</Word>
                        <Word>this</Word>
                        <Word>typeof</Word>
                        <Word>var</Word>
                        <Word>void</Word>
                        <Word>while</Word>
                        <Word>with</Word>
                        <Word>abstract</Word>
                        <Word>boolean</Word>
                        <Word>byte</Word>
                        <Word>case</Word>
                        <Word>catch</Word>
                        <Word>char</Word>
                        <Word>class</Word>
                        <Word>const</Word>
                        <Word>debugger</Word>
                        <Word>default</Word>
                        <Word>do</Word>
                        <Word>double</Word>
                        <Word>enum</Word>
                        <Word>export</Word>
                        <Word>extends</Word>
                        <Word>final</Word>
                        <Word>finally</Word>
                        <Word>float</Word>
                        <Word>goto</Word>
                        <Word>implements</Word>
                        <Word>import</Word>
                        <Word>instanceof</Word>
                        <Word>int</Word>
                        <Word>interface</Word>
                        <Word>long</Word>
                        <Word>native</Word>
                        <Word>package</Word>
                        <Word>private</Word>
                        <Word>protected</Word>
                        <Word>public</Word>
                        <Word>short</Word>
                        <Word>static</Word>
                        <Word>super</Word>
                        <Word>switch</Word>
                        <Word>synchronized</Word>
                        <Word>throw</Word>
                        <Word>throws</Word>
                        <Word>transient</Word>
                        <Word>try</Word>
                        <Word>volatile</Word>
                    </Keywords>
                    <Keywords color=""JavaScriptIntrinsics"">
                        <Word>Array</Word>
                        <Word>Boolean</Word>
                        <Word>Date</Word>
                        <Word>Function</Word>
                        <Word>Global</Word>
                        <Word>Math</Word>
                        <Word>Number</Word>
                        <Word>Object</Word>
                        <Word>RegExp</Word>
                        <Word>String</Word>
                    </Keywords>
                    <Keywords color=""JavaScriptLiterals"">
                        <Word>false</Word>
                        <Word>null</Word>
                        <Word>true</Word>
                        <Word>NaN</Word>
                        <Word>Infinity</Word>
                    </Keywords>
                    <Keywords color=""JavaScriptGlobalFunctions"">
                        <Word>eval</Word>
                        <Word>parseInt</Word>
                        <Word>parseFloat</Word>
                        <Word>escape</Word>
                        <Word>unescape</Word>
                        <Word>isNaN</Word>
                        <Word>isFinite</Word>
                    </Keywords>
                    <Span color=""Comment"">
                        <Begin>//</Begin>
                    </Span>
                    <Span color=""Comment"" multiline=""true"">
                        <Begin>/\*</Begin>
                        <End>\*/</End>
                    </Span>
                    <Span color=""Regex"">
                        <Begin>(?&lt;!([})\]\w]+\s*))/</Begin>
                        <End>/</End>
                    </Span>
                    <Span color=""String"" multiline=""true"">
                        <Begin>""</Begin>
                        <End>""</End>
                        <RuleSet>
                            <Span begin=""\\"" end=""."" />
                        </RuleSet>
                    </Span>
                    <Span color=""Character"">
                        <Begin>'</Begin>
                        <End>'</End>
                        <RuleSet>
                            <Span begin=""\\"" end=""."" />
                        </RuleSet>
                    </Span>
                    <Rule color=""Digits"">\b0[xX][0-9a-fA-F]+|(\b\d+(\.[0-9]+)?|\.[0-9]+)([eE][+-]?[0-9]+)?</Rule>
                </RuleSet>
            </SyntaxDefinition>";
    }

    public partial class ComputerWindow : Window
    {
        public Computer computer;
        public ComputerWindow(Computer pc)
        {
            InitializeComponent();
            SetBackground("C:\\Users\\Josh\\source\\repos\\VM\\Background.png");
            KeyDown += Computer_KeyDown;
            computer = pc;
        }

        private void Computer_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.OemTilde:
                    Open(new CommandPrompt(computer), "Cmd", 350, 275);
                    break;
                
            }
        }

        private void SetBackground(string path)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bitmapImage.EndInit();
            desktopBackground.Source = bitmapImage;
        }

        public Dictionary<string, ResizableWindow> Windows = new();

        public void Open(UserControl control, string title = "window", int width = 200, int height = 100, Brush? background = null, Brush? foreground = null) 
        {
            background ??= Brushes.LightGray;
            foreground ??= Brushes.Black;

            var window = new UserWindow();

            var frame = new ResizableWindow
            {
                Content = window,
                Width = width,
                Height = height,
                Background = background,
                Foreground = foreground,
            };

            window.Init(frame, control);

            Windows[title] = frame;

            Desktop.Children.Add(frame);

            Button btn = GetTaskbarButton(title, window.ToggleMaximize);

            TaskbarStackPanel.Children.Add(btn);

            window.OnClosed += () => RemoveTaskbarButton(title);

        }

        private void RemoveTaskbarButton(string title)
        {
            System.Collections.IList list = TaskbarStackPanel.Children;
            for (int i = 0; i < list.Count; i++)
            {
                object? item = list[i];
                if (item is Button button && button.Content == title)
                {
                    TaskbarStackPanel.Children.Remove(button);
                    break;
                }
            }
        }

        private Button GetTaskbarButton(string title, RoutedEventHandler Toggle)
        {
            var btn = new Button()
            {
                Background = Brushes.LightGray,
                Width = 35,
                FontFamily = computer.OS.SystemFont,
                FontSize = 10,
                Content = title,
            };
            btn.Click += Toggle;
            return btn;
        }

        private void Taskbar_Click(object sender, RoutedEventArgs e)
        {
            Open(new FileExplorer(computer), "New File Explorer");
        }
    }
}
