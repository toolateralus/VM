using Lemur.JavaScript.Api;
using Lemur.JS;
using Lemur.Windowing;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;

namespace Lemur.GUI
{
    /// <summary>
    /// This type is the container for any 'window' or application that appears in Lemur.
    /// it composes of the JavaScript engine, access to the UI above, and has events to clean up and close
    /// applications.
    /// </summary>
    public partial class UserWindow : UserControl
    {
        /// <summary>
        /// The window manager container that has the logic for resizing, moving, etc.
        /// </summary>
        public ResizableWindow ResizableParent { get; set; }
        /// <summary>
        /// The JavaScript engine that powers this app.
        /// </summary>
        public Engine Engine { get; set; }
        public bool WindowIsFocused => ResizableParent?.WindowIsFocused ?? false;


        /// <summary>
        /// called during Close to cleanup any extra UI, Threading, JavaScript resources etc.
        /// </summary>
        internal event Action? OnApplicationClose;
        readonly string pID;
        public UserWindow(string pID)
        {
            InitializeComponent();
            this.pID = pID;
            xBtn.Click += CloseWindow;

            // TODO: fix this up
            minimizeBtn.Click += (_, _) =>
            {
                ResizableParent?.ToggleVisibility();

                if (ResizableParent.Visibility == Visibility.Visible) {
                    ResizableParent.BringIntoViewAndToTop();
                }
            };

            maximizeBtn.Click += (_, _) =>
            {
                ResizableParent?.ToggleMaximize();
                ResizableParent?.BringIntoViewAndToTop();
            };

            long lastClickedTime = 0;

            MouseLeftButtonDown += (_, e) =>
            {
                if (DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastClickedTime < 500)
                    ResizableParent?.ToggleMaximize();
                else
                    ResizableParent?.BeginMove(e.GetPosition(this));
                 
                lastClickedTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                e.Handled = true;
            };

            Computer.Current.Window.PreviewKeyDown += UserWindow_PreviewKeyDown;

        }
        private void UserWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.C &&
                WindowIsFocused &&
                Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) &&
                Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift))
            {
                if (Computer.GetProcess(pID) is not Process proc)
                {
                    Notifications.Now("Failed to find process from user window terminate.. this is not good.");
                    return;
                }

                proc.Terminate();
            }
                
            if (Engine == null)
                return;

            if (Engine.EventHandlers.Count == 0)
                return;

            var events = Engine.EventHandlers.Where(e => e is InteropEvent iE
                                                                && iE.Event == XAML_EVENTS.KEY_DOWN).ToList();

            if (events.Count == 0)
                return;

            var interopEvents = events.Cast<InteropEvent>();

            foreach (var interopEvent in interopEvents)
                interopEvent.InvokeKeyboard(sender, e);
        }
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            ResizableParent?.BringIntoViewAndToTop();
        }
        internal void InitializeContent(ResizableWindow frame, UserControl actualUserContent, Engine? engine)
        {
            ResizableParent = frame;

            ContentsFrame.Content = actualUserContent;

            if (engine != null)
                Engine = engine;
        }
        internal void Close()
        {
            ResizableParent.OnApplicationClose?.Invoke();
            OnApplicationClose?.Invoke();
        }
        /// <summary>
        /// Close() wrapper for the Shut Down button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            var proc = Computer.GetProcess(pID) ?? throw new InvalidOperationException("Failed to find process on window close. What?");
            proc.Terminate();
            e.Handled = true;
        }
        // this is in use. the IDE just says it isn't
        private void OnResizeBorderClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string tag)
                return;
            ResizeEdge edge = (ResizeEdge)Enum.Parse(typeof(ResizeEdge), tag);
            ResizableParent.BeginResize(edge, e.GetPosition(this));
            e.Handled = true;
        }
    }
}
