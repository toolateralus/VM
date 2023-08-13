using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VM.GUI;

namespace VM
{
    public static class Notifications
    {
        private static Queue<string> MessageQueue = new Queue<string>();
        private static bool Preoccupied = false;
        private static object queueLock = new object();

        public static event Action<string> MessageProcessed;

        static Notifications()
        {
        }

        public static void Now(string message)
        {
            foreach (var cw in Runtime.Computers)
            {
                var notif = new NotificationControl() { Message = message };

                cw.Value.NotificationStackPanel.Children.Add(notif);

                notif.Start();
            }
        }
    }
}
