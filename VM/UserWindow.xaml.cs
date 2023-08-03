using System;
using System.Windows;
using System.Windows.Controls;

namespace VM
{
    /// <summary>
    /// Interaction logic for UserWindow.xaml
    /// </summary>
    public partial class UserWindow : Page
    {
        private ResizableWindow owner;

        public UserWindow()
        {
            InitializeComponent();
        }

        internal void Init(ResizableWindow frame)
        {
            owner = frame;
        }
        internal void Destroy()
        {
            if (owner is null || owner.Parent is not Grid grid)
                throw new InvalidOperationException("Window was destroyed but it had no parent");

            grid.Children.Remove(owner);
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Destroy();
        }
    }

   
}
