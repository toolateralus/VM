using System;
using System.Linq;
using VM.GUI;

namespace VM
{
    public static class Notifications
    {
        public static void Now(string message)
        {
            var cw = Computer.Current.Window;
            cw.Dispatcher.Invoke(() =>
            {
                var cmd = Computer.TryGetProcess<CommandPrompt>(Computer.Current);

                cmd?.output?.AppendText("\n" + message);

                var notif = new NotificationControl() { Message = message };
                cw.NotificationStackPanel.Children.Add(notif);
                notif.Start();

            });
        }

        internal static void Exception(Exception e)
        {
            Now(e.Message.Split("at").FirstOrDefault(""));  
        }
    }
}
