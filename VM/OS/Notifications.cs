using System;
using System.Linq;
using System.Windows;
using VM.GUI;

namespace VM
{
    public static class Notifications
    {
        internal static void Now(string message)
        {
            var wnd = Application.Current.MainWindow as MainWindow;
            var notificationControl = new NotificationControl { Message = message };
            (wnd?.Computers.First().Value as ComputerWindow).Desktop.Children.Add(notificationControl);
        }
    }
}