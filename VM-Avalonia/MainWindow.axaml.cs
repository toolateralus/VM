using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VM.Avalonia;

public partial class MainWindow : Window
{

    public Computer Computer = new(); 
    public MainWindow()
    {
        InitializeComponent();
        Computer.Boot(0);
    }

    public void OnShutdownClicked(object? sender, RoutedEventArgs args)
    {

        
        
    }
}