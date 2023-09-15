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
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.KeyModifiers.HasFlag(KeyModifiers.Shift) && e.Key == Key.OemTilde)
        {
            var cmd = new CommandPrompt();
            cmd.BringIntoView();
            Desktop.Children.Add(cmd);
            System.Console.WriteLine("Added terminal");
        }
    }

    public void OnShutdownClicked(object? sender, RoutedEventArgs args)
    {

        
        
    }
}