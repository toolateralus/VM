using System.Windows.Controls;
using System.Windows.Input;

namespace Lemur.GUI
{
    class HoverButton : Button
    {
        public HoverButton() 
        {
            Opacity = 0;
        }
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            Opacity = 1;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            Opacity = 0;
        }
    }
}
