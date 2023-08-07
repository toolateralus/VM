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
            App.Current.Dispatcher.Invoke(() =>
            {
                var notificationControl = new NotificationControl { Message = message };

                if (Runtime.Computers.Count > 0 && Runtime.Computers.First().Value is ComputerWindow cw)
                {
                    cw.Desktop.Children.Add(notificationControl);
                }
            });
        }
    }
}