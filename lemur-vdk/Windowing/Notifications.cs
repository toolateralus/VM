using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemur;
using Lemur.GUI;
using Lemur.OS;

namespace lemur.Windowing
{
    public static class Notifications
    {

        public static void Now(string message)
        {

            var cw = Computer.Current.Window;

            if (cw.Disposing || cw is null || cw.Dispatcher is null)
                return;

            void send_notification_ui()
            {
                var cmd = Computer.TryGetProcessOfTypeUnsafe<CommandPrompt>();

                var children = cw.NotificationStackPanel.Children;

                if (children.Count > 10)
                    children.RemoveAt(0);

                cmd?.output?.AppendText("\n" + message);

                // todo: pool these notification objects.
                var notification = new NotificationControl() { Message = message };

                cw.NotificationStackPanel.Children.Add(notification);

                notification.Start();
            }

            cw.Dispatcher.Invoke(send_notification_ui);
        }

        internal static void Clear()
        {
            var stopped = new List<NotificationControl>();

            foreach (var control in Computer.Current.Window.NotificationStackPanel.Children)
            {
                if (control is NotificationControl notification)
                    stopped.Add(notification);
            }

            foreach (var control in stopped)
                control.Stop();
        }

        internal static void Exception(Exception e)
        {
            Now(e.Message + $"\n{e.GetType().Name}");
        }
    }
}
