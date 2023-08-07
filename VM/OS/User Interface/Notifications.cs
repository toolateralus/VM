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

        // Define an event to signal processing the next message
        public static event Action<string> MessageProcessed;

        static Notifications()
        {
            // Subscribe to the MessageProcessed event
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
                    // Trigger the MessageProcessed event to process the message
                    MessageProcessed?.Invoke(message);
                }
            }
        }

        private static async void ProcessNextMessage(string message)
        {
            // Show the notification
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                void onTimerComplete()
                {
                    lock (queueLock)
                    {
                        Preoccupied = false;
                        // If there are more messages in the queue, process the next one
                        if (MessageQueue.Any())
                        {
                            string nextMessage = MessageQueue.Dequeue();
                            MessageProcessed?.Invoke(nextMessage);
                        }
                    }
                }

                var notif = new NotificationControl(onTimerComplete) { Message = message };

                foreach (var cw in Runtime.Computers)
                {
                    cw.Value.Desktop.Children.Add(notif);
                }

                notif.Start();
            });
        }
    }
}
