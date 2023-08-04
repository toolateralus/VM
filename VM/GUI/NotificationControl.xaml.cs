using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace VM.GUI
{
    public partial class NotificationControl : UserControl
    {

        const int NOTIFICATION_SIZE_X = 200, NOTIFICATION_SIZE_Y = 100;

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
            MaxHeight = NOTIFICATION_SIZE_Y;
            MaxWidth = NOTIFICATION_SIZE_X;

            InitializeComponent();
            DataContext = this;

            fadeOutTimer = new DispatcherTimer();
            fadeOutTimer.Interval = TimeSpan.FromSeconds(2); 
            fadeOutTimer.Tick += OnFadeOutTimerTick;

            Loaded += OnLoaded;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
        }
        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            var fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1));
            BeginAnimation(OpacityProperty, fadeInAnimation);

            var margin = new Thickness(Margin.Left, Margin.Top, Margin.Right, Margin.Bottom + 15);
            var popUpAnim = new ThicknessAnimation(margin, TimeSpan.FromSeconds(1));
            BeginAnimation(MarginProperty, popUpAnim);
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
            var fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(2));
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
