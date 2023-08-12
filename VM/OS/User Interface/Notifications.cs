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
            MessageProcessed += ProcessNextMessage;
        }

        public static void Now(string message)
        {
            lock (queueLock)
            {
                MessageQueue.Enqueue(message);
                if (!Preoccupied)
                {
                    Preoccupied = true;
                    MessageProcessed?.Invoke(message);
                }
            }
        }

        private static async void ProcessNextMessage(string message)
        {

            try
            {

                await Application.Current?.Dispatcher?.InvokeAsync(() =>
                {
                    void onTimerComplete()
                    {
                        lock (queueLock)
                        {
                            Preoccupied = false;
                            if (MessageQueue.Any())
                            {
                                string nextMessage = MessageQueue.Dequeue();
                                MessageProcessed?.Invoke(nextMessage);
                            }
                        }
                    }


                    foreach (var cw in Runtime.Computers)
                    {
                        var notif = new NotificationControl(onTimerComplete) { Message = message };

                        cw.Value.Desktop.Children.Add(notif);

                        notif.Start();
                    }

                });
            }
            catch
            {

            }
        }
    }
}
