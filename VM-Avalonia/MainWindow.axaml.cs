using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using VM;
using VM.FS;

namespace VM.Avalonia;

public partial class MainWindow : Window
{

    public Computer Computer = new(); 
    public MainWindow()
    {
        InitializeComponent();
        Computer.Boot(0);
        desktopBackground.Source = LoadImage(FileSystem.GetResourcePath("Background.png") ?? "background.png");
        KeyDown += HandleKeys;
    }
    public static Bitmap LoadImage(string path)
    {
        Bitmap bitmapImage = new Bitmap(path);
        return bitmapImage;
    }
    private void HandleKeys(object? sender, KeyEventArgs e)
    {
        // command prompt shortcut, temporary. todo: remove this
        var hasModifiers = e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        
        if (hasModifiers && e.Key == Key.OemTilde)
        {
            var window = new UserWindow();

            var frame = new ResizableWindow(this)
            {
                Content=window,
                Width = Math.Max(window.MinWidth, window.Width),
                Height = Math.Max(window.MinHeight, window.Height),
                Margin = window.Margin,
                Background = window.Background,
                Foreground = window.Foreground,
            };
            
            // the window holds the reference to the engine so that it's sensibly accessible to the OS.
            window.InitializeUserContent(Computer, frame, new CommandPrompt(), window.JavaScriptEngine);

            Desktop.Children.Add(frame);

            System.Console.WriteLine("Added terminal");
        }
    }

    public void OnShutdownClicked(object? sender, RoutedEventArgs args)
    {

    }
}