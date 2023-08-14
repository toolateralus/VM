﻿using Microsoft.ClearScript.V8;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using VM;
using VM.OS;
using VM.OS.JS;
using VM.OS.UserInterface;

namespace VM.GUI
{
    public enum XAML_EVENTS
    {
        MOUSE_DOWN = 0,
        MOUSE_UP = 1,
        MOUSE_MOVE = 2,
        KEY_DOWN = 3,
        KEY_UP = 4,
        LOADED = 5,
        WINDOW_CLOSE = 6,
        RENDER = 7,
    }
    public static class JAVASCRIPT_SYNTAX_HIGHLIGHTING
    {
        public const string HIGHLIGHTING = @"<?xml version=""1.0""?>
            <!-- syntax definition for JavaScript 2.0 by Svante Lidman -->
            <!-- converted to AvalonEdit format by Siegfried Pammer in 2010 -->
            <!-- forked by me in 2023, not for commercial use -->

            <SyntaxDefinition name=""JavaScriptCustom"" extensions="".js"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
                <Color name=""Digits"" foreground=""#6D9C66"" exampleText=""3.14"" />
                <Color name=""Comment"" foreground=""#607D8B"" exampleText=""// comment"" />
                <Color name=""Method"" foreground=""#FAC05E"" exampleText=""functionName()"" />
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
                   <Span color=""Method"">
                          <Begin>\b\w+\b(?=\s*\()</Begin>
                         <End>\b</End>
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
        public List<string> CustomApps = new();
        public Dictionary<string, int> JS_WPF_APP_INSTANCES = new();
        public Dictionary<string, UserWindow> USER_WINDOW_INSTANCES { get; private set; } = new();

        public ComputerWindow(Computer pc)
        {
            InitializeComponent();
            desktopBackground.Source = LoadImage(Runtime.GetResourcePath("Background.png") ?? "background.png");
            KeyDown += Computer_KeyDown;
            computer = pc;
            Closing += (s, eA)
            =>
            {
                foreach (var item in USER_WINDOW_INSTANCES)
                {
                    item.Value.Destroy();
                }
            };
        }
        private void Computer_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.OemTilde:
                    var cmd = new CommandPrompt();
                    OpenApp(cmd, "Cmd");
                    cmd.LateInit(computer);
                    break;
            }
        }
        public static BitmapImage LoadImage(string path)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bitmapImage.EndInit();
            return bitmapImage;
        }
        public void OpenApp(UserControl control, string title = "window", Brush? background = null, Brush? foreground = null, JavaScriptEngine engine = null)
        {
            background ??= Brushes.LightGray;
            foreground ??= Brushes.Black;

            var window = new UserWindow();

            var frame = new ResizableWindow
            {
                Content = window,
                Width = 800,
                Height = 600,
                Margin = new(),
                Background = background,
                Foreground = foreground,
            };

            window.Init(frame, control, engine);

            USER_WINDOW_INSTANCES[title] = window;

            Desktop.Children.Add(frame);

            Button btn = GetTaskbarButton(title, window.ToggleVisibility);

            TaskbarStackPanel.Children.Add(btn);

            window.OnClosed += () => RemoveTaskbarButton(title);

            IDLabel.Content = $"computer {computer.ID()}";
            
            CompositionTarget.Rendering += (e, o) => UpdateComputerTime();

        }
        private void UpdateComputerTime()
        {
            DateTime now = DateTime.Now;
            string formattedDateTime = now.ToString("MM/dd/yy || h:mm");
            TimeLabel.Content = formattedDateTime;
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
        private Button GetOSThemeButton(double width = double.NaN, double height = double.NaN)
        {
            var btn = new Button()
            {
                Background = computer.OS.Theme.Background,
                BorderBrush = computer.OS.Theme.Border,
                BorderThickness = computer.OS.Theme.BorderThickness,
                FontFamily = computer.OS.Theme.Font,
                FontSize = computer.OS.Theme.FontSize,
                Width = width,
                Height = height,
            };
            return btn;
        }
        private Button GetDesktopIconButton(string appName)
        {
            var btn = GetOSThemeButton(width: 60, height: 60);

            btn.Margin = new Thickness(15, 15, 15, 15);
            btn.Content = appName;
            btn.Name = appName.Split(".")[0];
            return btn;
        }
        private Button GetTaskbarButton(string title, RoutedEventHandler Toggle)
        {
            var btn = GetOSThemeButton(width: 65);

            btn.Content = title;
            btn.Click += Toggle;
            return btn;
        }
        internal void RegisterApp(string exePath, Type type)
        {
            var name = exePath.Split('.')[0];

            var btn = GetDesktopIconButton(name);

            btn.MouseDoubleClick += OnDesktopIconPressed;

            void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
            {
                var members = type.GetMethods();

                if (IsValidType(members) && Activator.CreateInstance(type) is object instance && instance is UserControl userControl)
                {
                    AssignComputer(instance, computer);
                    OpenApp(userControl, name);
                }
            }

            SetupIcon(name, btn, type);

            DesktopIconPanel.Children.Add(btn);
            DesktopIconPanel.UpdateLayout();
        }
        private static void SetupIcon(string name, Button btn, Type type) 
        {
            if (GetIcon(type) is BitmapImage img)
            {
                btn.Background = new ImageBrush(img);
            }

            btn.Margin = new Thickness(15, 15, 15, 15);

            var contentBorder = new Border
            {
                Background = new ImageBrush(GetIcon(type)),
                CornerRadius = new CornerRadius(10),
                ToolTip = name,
            };

            btn.Content = contentBorder;
        }
        private static BitmapImage? GetIcon(Type type) 
        {
            var properties = type.GetProperties();

            foreach (var property in properties)
            {
                if (property.Name.Contains("DesktopIcon") &&
                    property.PropertyType == typeof(string) &&
                    property.GetValue(null) is string path &&
                    !string.IsNullOrEmpty(path))
                {
                    return LoadImage(path);
                }
            }

            return null;
        }

        /// <summary>
        /// performs init on LateInit method, explaied in tooltipf or IsValidType (a static method in this class)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="computer"></param>
        private static void AssignComputer(object instance, Computer computer)
        {
            var methods = instance.GetType().GetMethods();

            foreach (var method in methods)
            {
                if (method.Name.Contains("LateInit") &&
                    method.GetParameters().Length == 1 &&
                    method.GetParameters()[0].ParameterType == typeof(Computer))
                {
                    method.Invoke(instance, new[] { computer });
                }
            }
        }
        /// <summary>
        /// we rely on this <code>('public void LateInit(Computer pc){..}') </code>method being declared in the UserControl to attach the OS to the app
        /// </summary>
        /// <param name="members"></param>
        /// <returns></returns>
        /// 
        private static bool IsValidType(MemberInfo[] members)
        {
            foreach (var member in members)
            {
                if (member.Name.Contains("LateInit"))
                {
                    return true;
                }
            }
            return false;
        }
        JSApp AppModule;
        public async Task OpenCustom(string type)
        {
            var data = Runtime.GetAppDefinition(computer, type);

            var control = XamlJsInterop.ParseUserControl(data.XAML);

            if (control == null)
            {
                Notifications.Now($"Error parsing xaml for {type}.");
                return;
            }

            JavaScriptEngine engine = new(computer);

            var jsResult = await HandleJS(type, data, engine);

            var wnd = Runtime.GetWindow(pc: computer);
            
            wnd.OpenApp(control, title:  jsResult.id, engine: engine);

            await engine.Execute(jsResult.code);

        }
        private async Task<(string id, string code)> HandleJS(string type, (string XAML, string JS) data, JavaScriptEngine engine)
        {
            var name = type.Split('.')[0];
            int id = 0;

            if (JS_WPF_APP_INSTANCES.TryGetValue(type, out id))
            {
                JS_WPF_APP_INSTANCES[type]++;
            }
            else
            {
                JS_WPF_APP_INSTANCES.Add(type, 1);
            }

            var JS = new string(data.JS);

             _ = await engine.Execute(JS);

            var instance_name = "uid" + Guid.NewGuid().ToString().Split('-')[0];

            string instantiation_code = $"let {instance_name} = new {name}('{instance_name}')";



            return (instance_name, instantiation_code);
        }
        public void InstallWPF(string type)
        {
            Dispatcher.Invoke(delegate
            {
                CustomApps.Add(type);

                var btn = GetDesktopIconButton(type);
              
                btn.MouseDoubleClick += OnDesktopIconPressed;

                async void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
                {
                    await OpenCustom(type);
                }

                DesktopIconPanel.Children.Add(btn);
                DesktopIconPanel.UpdateLayout();

            });
            
        }
        internal void InstallJSHTML(string type)
        {
            Dispatcher.Invoke(delegate
            {
                CustomApps.Add(type);

                var btn = GetDesktopIconButton(type);

                btn.MouseDoubleClick += OnDesktopIconPressed;

                void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
                {
                    var app = new UserWebApplet();

                    // we add the appropriate extension within navigate.
                    app.Path = (type.Replace(".web", ""));
                    OpenApp(app);
                }

                DesktopIconPanel.Children.Add(btn);
                DesktopIconPanel.UpdateLayout();
            });

        }
        internal void Uninstall(string name)
        {
            CustomApps.Remove(name);

            Dispatcher.Invoke(() => { 
                System.Collections.IList list = DesktopIconPanel.Children;

                for (int i = 0; i < list.Count; i++)
                {
                    object? item = list[i];
                    if (item is Button btn && btn.Name == name)
                    {
                        DesktopIconPanel.Children.Remove(btn);
                    }
                }
            });
        }

        private void ShutdownClick(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
            Close();
        }
    }
}
