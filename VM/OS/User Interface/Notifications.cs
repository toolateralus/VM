using System;   
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VM.GUI;

namespace VM
{
    public static class Notifications
    {
        public static void Now(string message)
        {
            foreach (var cw in Runtime.Computers)
            {
                cw.Value.Dispatcher.Invoke(() =>
                {
                    var cmd = Runtime.SearchForOpenWindowType<CommandPrompt>(cw.Key);
                    cmd?.Dispatcher?.Invoke(() => { cmd?.output?.AppendText("\n" + message); });

                    var notif = new NotificationControl() { Message = message };
                    cw.Value.NotificationStackPanel.Children.Add(notif);
                    notif.Start();

                });
            }
        }
    }
}
