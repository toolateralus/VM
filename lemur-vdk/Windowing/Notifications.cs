using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemur;
using Lemur.GUI;

namespace lemur.Windowing
{
    public static class Notifications
    {
        public static void Now(string message)
        {
            var cw = Computer.Current.Window;


            // closing the app.
            if (cw.Disposing || cw is null || cw.Dispatcher is null)
                return;

            cw.Dispatcher.Invoke(() =>
            {
                var children = cw.NotificationStackPanel.Children;

                if (children.Count > 10)
                    children.RemoveAt(0);

                var cmd = Computer.TryGetProcessOfType<CommandPrompt>();
                cmd?.output?.AppendText("\n" + message);

                StringBuilder sb = new();


                // Todo : make this an event you can subscribe to so we don't have to add so many special cases here.
                // just like the UI stuff.

                // Todo: really we should have a set of system calls and more structured things, not just a loose spread out linkage.

                sb.AppendLine($"let message = {{body : {message}, type : stdout}}");

                // selects all the process id's 
                foreach (var pid in Computer.ProcessLookupTable.Values.Select(i => i.Select(i => i)))
                {
                    sb.AppendLine($"if ({pid}.OS_MSG != undefined) {{");
                    sb.AppendLine($"{pid}.OS_MSG({message})");
                    sb.AppendLine($"}}");
                }


                var notif = new NotificationControl() { Message = message };


                cw.NotificationStackPanel.Children.Add(notif);
                notif.Start();
            });
        }

        internal static void Clear()
        {
            var stopped = new List<NotificationControl>();

            foreach (var control in Computer.Current.Window.NotificationStackPanel.Children)
            {
                if (control is NotificationControl notif)
                    stopped.Add(notif);
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
