using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace VM
{
    public partial class NotificationControl : UserControl
    {
        private DispatcherTimer fadeOutTimer;

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(NotificationControl), new PropertyMetadata(string.Empty));

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public NotificationControl()
        {
            InitializeComponent();
            fadeOutTimer = new DispatcherTimer();
            fadeOutTimer.Interval = TimeSpan.FromSeconds(2); 
            fadeOutTimer.Tick += OnFadeOutTimerTick;

            Loaded += OnLoaded;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            var fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            BeginAnimation(OpacityProperty, fadeInAnimation);

            fadeOutTimer.Start();
        }

        private void OnMouseEnter(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            fadeOutTimer.Stop();
        }

        private void OnMouseLeave(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            fadeOutTimer.Start();
        }

        private void OnFadeOutTimerTick(object? sender, EventArgs e)
        {
            var fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5));
            fadeOutAnimation.Completed += (s, _) =>
            {
                var parent = Parent as Panel;
                parent?.Children.Remove(this);
            };
            BeginAnimation(OpacityProperty, fadeOutAnimation);
            fadeOutTimer.Stop();
        }
    }
}
