using System.Windows;
using System.Windows.Media;

namespace VM.OS
{
    /// <summary>
    /// The default initialization for a parameterless construction of this object represents a fully implemented default theme, and
    /// it's meant to be customized.
    /// </summary>
    public class Theme
    {
        public Brush Background = Brushes.LightGray;
        public Brush Foreground = Brushes.White;
        public Brush Border = Brushes.Transparent;
        public FontFamily Font = new("Consolas");
        public Thickness BorderThickness = new(0, 0, 0, 0);
        public double FontSize = 12;
    }
}
