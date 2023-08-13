using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace VM.GUI
{
    public partial class NotificationControl : UserControl
    {

        const int NOTIFICATION_SIZE_X = 350, NOTIFICATION_SIZE_Y = 100;

        private DispatcherTimer fadeOutTimer;

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(NotificationControl), new PropertyMetadata(string.Empty));

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }
        public void Start()
        {
            fadeOutTimer.Start();
        }
        public NotificationControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;

            MaxHeight = NOTIFICATION_SIZE_Y;
            MaxWidth = NOTIFICATION_SIZE_X;

            HorizontalAlignment = HorizontalAlignment.Right;
            VerticalAlignment = VerticalAlignment.Bottom;

            DataContext = this;

            TextBox.FontFamily = new("Consolas MS");

            fadeOutTimer = new DispatcherTimer();
            fadeOutTimer.Interval = TimeSpan.FromSeconds(2);
            fadeOutTimer.Tick += OnFadeOutTimerTick;
            MouseDoubleClick += NotificationControl_MouseDoubleClick;

        }

        private void NotificationControl_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OnFadeOutCompleted();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            var fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1));
            BeginAnimation(OpacityProperty, fadeInAnimation);

            var margin = new Thickness(Margin.Left, Margin.Top, Margin.Right, Margin.Bottom + 15);
            var popUpAnim = new ThicknessAnimation(margin, TimeSpan.FromSeconds(1));
            BeginAnimation(MarginProperty, popUpAnim);
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
            var fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(1.5));
            fadeOutAnimation.Completed += (s, _) =>
            {
                OnFadeOutCompleted();
            };
            BeginAnimation(OpacityProperty, fadeOutAnimation);
            fadeOutTimer.Stop();
        }

        private void OnFadeOutCompleted()
        {
            var parent = Parent as Panel;
            parent?.Children.Remove(this);
        }
    }
}
