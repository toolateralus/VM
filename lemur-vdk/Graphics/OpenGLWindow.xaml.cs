using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using System;
using System.Windows.Controls;
using OpenTK;
using static OpenTK.Graphics.OpenGL4.GL;
using Lemur.FS;
using System.Threading.Tasks;
using System.Collections.Generic;
using Lemur.Game;

namespace Lemur.Graphics
{
    /// <summary>
    /// Interaction logic for OpenGLWindow.xaml
    /// </summary>
    public partial class OpenGL2Window : UserControl
    {
        internal readonly RendererOpenGL Renderer;
        internal readonly SceneOpenGL Scene;
        public static string? DesktopIcon => FileSystem.GetResourcePath("background.png");

        public event Action<TimeSpan>? Rendering;

        public OpenGL2Window()
        {
            InitializeComponent();

            var settings = new GLWpfControlSettings
            {
                MajorVersion = 4,
                MinorVersion = 0,
                RenderContinuously = true,
            };

            Scene = new(this);

            List<MeshRenderer> meshes = new()
            {
                new MeshRenderer((0,0,0), (0,0,0), (1,1,1), Cube.Unit())
            };

            Scene.Meshes = meshes;

            glRenderer.Start(settings);
            Renderer = new();
        }
        private void Renderer_Render(TimeSpan span)
        {
            ClearColor(Color4.Black);
            Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            // the gl pipeline
            Renderer?.Render();

            // the scene
            Rendering?.Invoke(span);
        }
    }
}
