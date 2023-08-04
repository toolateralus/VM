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
            var notificationControl = new NotificationControl { Message = message };

            if (MainWindow.Computers.Count > 0 && MainWindow.Computers.First().Value is ComputerWindow cw)
            {
                cw.Desktop.Children.Add(notificationControl);
            }

        }
    }
}