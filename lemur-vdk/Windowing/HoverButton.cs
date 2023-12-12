using System.Windows.Controls;
using System.Windows.Input;

namespace Lemur.GUI
{
    /// <summary>
    /// The 'resize' buttons for the windows, they are just outside the bounds of the window.
    /// hovering changes it's opacity.
    /// </summary>
    class HoverButton : Button
    {
        internal static float OpacityOff = 0.25f;
        internal static float OpacityOn = 0.85f;
        public HoverButton()
        {
            Opacity = OpacityOff;
        }
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            Opacity = OpacityOn;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            Opacity = OpacityOff;
        }
    }
}
