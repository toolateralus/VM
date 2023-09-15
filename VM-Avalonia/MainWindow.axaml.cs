using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using VM;
using VM.FS;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
namespace VM.Avalonia;

public partial class MainWindow : Window
{
    public Computer Computer = new(); 
    public UserControl? ParseUserControl(string xaml)
    {
        UserControl product = null;
        Action<UserControl> output = (e) => { product = e; };

        if (xaml == "Not found!")
            return null;

        Dispatcher.UIThread.Invoke(delegate { 
            try
            {
                object parsedObject = AvaloniaXamlLoader.Load(new Uri(xaml));

                if (parsedObject is UserControl userControl)
                {
                    output.Invoke(userControl);
                }
                else
                {
                    IO.WriteLine("The provided XAML does not represent a UserControl.");
                }
            }
            catch (XamlLoadException ex)
            {
                IO.WriteLine($"XAML parsing error: {ex.Message}");
            }
        });

        return product;
    }
    public MainWindow()
    {
        InitializeComponent();
        Computer.Boot(0);
        desktopBackground.Source = LoadImage(FileSystem.GetResourcePath("Background.png") ?? "background.png");
        KeyDown += HandleKeyboardInput;
    }
    public static Bitmap LoadImage(string path)
    {
        Bitmap bitmapImage = new Bitmap(path);
        return bitmapImage;
    }
    private void HandleKeyboardInput(object? sender, KeyEventArgs e)
    {
        // command prompt shortcut, temporary. todo: remove this
        var hasModifiers = e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        
        if (hasModifiers && e.Key == Key.OemTilde)
        {
            InitializeWindow(new CommandPrompt());
        }
    }
    private void InitializeWindow(UserControl userControl)
    {
        // this just frames the user's content, and provides some helper behaviour, like 
        // a new, isolated javascript environment, for XAML/JS apps.
        var window = new UserWindow();

        // This is the container that is actually on the desktop
        var frame = new ResizableWindow(this)
        {
            Content = window,
            Width = Math.Max(window.MinWidth, window.Width),
            Height = Math.Max(window.MinHeight, window.Height),
            Margin = window.Margin,
            Background = window.Background,
            Foreground = window.Foreground,
        };

        // the window holds the reference to the engine so that it's sensibly accessible to the OS.
        window.InitializeUserContent(Computer, frame, userControl, window.JavaScriptEngine);

        Desktop.Children.Add(frame);

        System.Console.WriteLine($"opened app {userControl.GetType()}");
    }
    public void OnShutdownClicked(object? sender, RoutedEventArgs args)
    {

    }
    public void InitializeControl(Computer computer, UserControl control, List<Action<UserControl, Computer, object[]?>> initializations, List<object[]?> args)
    {
        for (int i = 0; i < initializations.Count; i++)
        {
            Action<UserControl, Computer, object[]?> init = initializations[i];
            init?.Invoke(control, computer, args[i]);
        }
    }
    public void CallInitializeComponent(UserControl userControl)
    {
        Type userControlType = userControl.GetType();
        MethodInfo initializeComponentMethod = userControlType.GetMethod("InitializeComponent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (initializeComponentMethod != null)
        {
            initializeComponentMethod.Invoke(userControl, null);
        }
        else
        {
            return;
        }
    }

}
   