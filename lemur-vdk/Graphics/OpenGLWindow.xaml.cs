using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using System;
using System.Windows.Controls;
using OpenTK;
using static OpenTK.Graphics.OpenGL4.GL;
using Lemur.FS;
using System.Threading.Tasks;

namespace lemur.Graphics
{
    /// <summary>
    /// Interaction logic for OpenGLWindow.xaml
    /// </summary>
    public partial class OpenGLWindow : UserControl
    {
        public static string? DesktopIcon => FileSystem.GetResourcePath("background.png");
        public OpenGLWindow()
        {
            InitializeComponent();
            var settings = new GLWpfControlSettings
            {
                MajorVersion = 4,
                MinorVersion = 0,
                RenderContinuously = true,
            };

            Renderer.Start(settings);

        }
        private void Renderer_Render(TimeSpan obj)
        {
            ClearColor(Color4.Blue);
            Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }
    }
}
