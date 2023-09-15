using Newtonsoft.Json.Linq;
using System;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Remote.Protocol.Input;


namespace VM.Avalonia
{
    public partial class ResizableWindow : ContentControl
    {
        private bool isDragging = false;
        private bool isResizing = false;
        private Point dragOffset;
        public Action OnClosed { get; internal set; }
        MainWindow Owner {get;set;}
        
        public float ResizeSpeed => Owner?.Computer?.Config.Value<float?>("WINDOW_RESIZE_SPEED") ?? 4f;
        public ResizableWindow(MainWindow owner)
        {
           InitializeComponent();
           Owner = owner;
           PointerPressed += OnPointerPressed;
           PointerMoved += OnPointerMoved;
           PointerReleased += OnMouseUp;
           PointerExited += onPointerExited;

            // Todo: remove magic numbers, replace with config file read or something like that.

           MinWidth = Owner?.Computer?.Config.Value<float?>("MIN_WIN_WIDTH") ?? 50;
           MinHeight = Owner?.Computer?.Config.Value<float?>("MIN_WIN_HEIGHT") ?? 50;
           MaxWidth = Owner?.Computer?.Config.Value<float?>("MAX_WIN_WIDTH") ?? 1920;
           MaxHeight = Owner?.Computer?.Config.Value<float?>("MAX_WIN_HEIGHT") ?? 1080 - 25;
        }
        private void onPointerExited(object sender, PointerEventArgs e)
        {
            isDragging = false;
            isResizing = false;
            dragOffset = new();
        }
        protected void OnPointerPressed(object sender, PointerEventArgs e)
        {
            var point = e.GetCurrentPoint(this);
            var props = point.Properties;
            dragOffset = point.Position;

            if (props.IsLeftButtonPressed)
            {
                BringToTopOfDesktop();
                isDragging = true;
            }
        }
        public void BringToTopOfDesktop()
        {
            if (Parent is Canvas grid && grid.Children.Contains(this))
            {
                grid.Children.Remove(this);
                grid.Children.Add(this);
                // TODO: Set z index of window to top here.
            }
        }
        protected void OnPointerMoved(object sender, PointerEventArgs e)
        {
            var pos = e.GetPosition(this);

            // TODO: refaactor this so we don't have to do this.
            //var altDown = Keyboard.IsKeyDown(Key.LeftAlt) || IsKeyDown(Key.RightAlt);
            
            // THIS WAS FROM WPF.. NO LONGER RELEVNAT ENTIRELY
            // Todo: refactor this entire drag/move/resize system. It's been nothing but trouble, and surprisingly
            // This is the best state it has ever been in.

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var delta = pos - dragOffset;
                double maxDelta = ResizeSpeed;
                delta = NormalizeMouseDelta(delta, maxDelta);
                PerformResize(delta);

            }
            else if (isDragging)
            {
                double left = pos.X - dragOffset.X;
                double top = pos.Y - dragOffset.Y;
                Canvas.SetLeft(this, left);
                Canvas.SetTop(this, top);
            }
        }

        private void PerformResize(Vector delta)
        {
            if (Width + delta.X >= MinWidth && Width + delta.X <= MaxWidth)
                Width += delta.X;
            else if (Width + delta.X < MinWidth)
                Width = MinWidth;
            else if (Width + delta.X > MaxWidth)
                Width = MaxWidth;

            if (Height + delta.Y >= MinHeight && Height + delta.Y <= MaxHeight)
                Height += delta.Y;
            else if (Height + delta.Y < MinHeight)
                Height = MinHeight;
            else if (Height + delta.Y > MaxHeight)
                Height = MaxHeight;
        }

        private static Point NormalizeMouseDelta(Point delta, double maxDelta)
        {
            if (Math.Abs(delta.X) > Math.Abs(delta.Y))
                return new (Math.Clamp(delta.X, -maxDelta, maxDelta), 0);
            return new (Math.Clamp(delta.Y, -maxDelta, maxDelta), 0);
        }

        protected void OnMouseUp(object sender, RoutedEventArgs e)
        {
            if (isDragging || isResizing)
            {
                isDragging = false;
                isResizing = false;
            }
        }
    }
}
