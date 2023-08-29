using System;   
using VM.GUI;

namespace VM
{
    public static class Notifications
    {
        public static void Now(string message)
        {
            foreach (var cw in Computer.Computers)
            {
                cw.Value.Dispatcher.Invoke(() =>
                {
                    var cmd = Computer.SearchForOpenWindowType<CommandPrompt>(cw.Key);
                    cmd?.Dispatcher?.Invoke(() => { cmd?.output?.AppendText("\n" + message); });

                    var notif = new NotificationControl() { Message = message };
                    cw.Value.NotificationStackPanel.Children.Add(notif);
                    notif.Start();

                });
            }
        }

        internal static void Exception(Exception e)
        {
            Now(e.Message + "\n" + e.InnerException + "\n" + e.Source);  
        }
    }
}
