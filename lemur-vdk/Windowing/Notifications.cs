using Lemur.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Lemur.Windowing
{
    public static class Notifications
    {

        public static void Now(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "")
        {

            var cw = Computer.Current.Window;

            if (cw is null || cw.Disposing != false || cw.Dispatcher is null)
                return;

            void send_notification_ui()
            {
                var terminals = Computer.TryGetAllProcessesOfTypeUnsafe<Terminal>();

                foreach (var term in terminals)
                {
                    var children = cw.NotificationStackPanel.Children;

                    if (children.Count > 10)
                        children.RemoveAt(0);

                    term?.output?.AppendText("\n" + message);
                }

                if (!IsValid(callerName, path))
                    return;

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
        /// <summary>
        /// this has been added to address the issue of notification spam from the poor design / code choice / practices of just printing EVERYTHING
        /// to the notifications. we should ultimately have 3 channels of printing, one for the last term, all terms, and then notifications
        /// </summary>
        /// <param name="callerName"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        static bool IsValid(string callerName, string path) {
            ArgumentNullException.ThrowIfNull(callerName);
            ArgumentNullException.ThrowIfNull(path);

            // this is an incredibly hacky solution.
            return !(callerName == "Send" || callerName == "ExecuteAsync" || path.Contains("JS") || path.Contains("Terminal.xaml.cs") || path.Contains("Embedded"));
        }

        internal static void Exception(Exception e, [CallerMemberName] string callerName = "", [CallerFilePath] string path = "")
        {
            if (!IsValid(callerName, path))
                return;

            Now(e.Message + $"\n{e.GetType().Name}");
        }
    }
}
