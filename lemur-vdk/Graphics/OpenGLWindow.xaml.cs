using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using System;
using System.Windows.Controls;
using OpenTK;
using static OpenTK.Graphics.OpenGL4.GL;
using Lemur.FS;
using System.Threading.Tasks;

namespace Lemur.Graphics
{
    /// <summary>
    /// Interaction logic for OpenGLWindow.xaml
    /// </summary>
    public partial class OpenGL2Window : UserControl
    {
        internal readonly GL4Renderer renderLib;
        public static string? DesktopIcon => FileSystem.GetResourcePath("background.png");
        public OpenGL2Window()
        {
            InitializeComponent();

            var settings = new GLWpfControlSettings
            {
                MajorVersion = 4,
                MinorVersion = 0,
                RenderContinuously = true,
            };

            Renderer.Start(settings);
            renderLib = new();
        }
        private void Renderer_Render(TimeSpan span)
        {
            ClearColor(Color4.Black);
            Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            renderLib.Render(span);
        }
    }
}
