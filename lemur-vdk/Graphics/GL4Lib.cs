using Lemur.Game;
using Lemur.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Quaternion = OpenTK.Mathematics.Quaternion;

namespace Lemur.Graphics
{
    
    public class GL4Renderer : IDisposable
    {
        // vertex buffer object, vertex array object.
        private int vbo, vao;
        private int shader;
        internal List<MeshRenderer> meshes = new();
        private bool disposedValue;
        internal Matrix4 viewProjection;

        public Queue<Action> Jobs { get; private set; } = new();

        public GL4Renderer()
        {
            Jobs.Enqueue(() =>
            {

                vbo = GL.GenBuffer();
                vao = GL.GenVertexArray();

                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BindVertexArray(vao);

                var vertexSize = Marshal.SizeOf<Vertex>();

                // vert
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, 0);

                // normal
                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, vertexSize);

                // color
                GL.EnableVertexAttribArray(2);
                GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, vertexSize, vertexSize + Marshal.SizeOf<Vector3>());

                var vertShaderSource = @"
                #version 400 core
                layout(location = 0) in vec3 inPosition;
                layout(location = 1) in vec3 inNormal;
                layout(location = 2) in vec3 inColor;
            
                uniform mat4 mvp;

                out vec3 pass_Color;
                out vec3 pass_Normal;

                void main()
                {
                    gl_Position = mvp * vec4(inPosition, 1.0);
                    pass_Color = inColor;
                    pass_Normal = inNormal;
                }
            ";

                var fragShaderSource = @"
                #version 400 core
                in vec3 pass_Color;
                in vec3 pass_Normal;
                
                uniform mat4 mvp;
                out vec4 fragColor;

                void main()
                {
                    fragColor = vec4(pass_Color, 1.0);
                }
            ";

                // Compile and link shaders to create a shader program
                int vertShader = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(vertShader, vertShaderSource);
                GL.CompileShader(vertShader);

                int fragShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(fragShader, fragShaderSource);
                GL.CompileShader(fragShader);

                shader = GL.CreateProgram();
                GL.AttachShader(shader, vertShader);
                GL.AttachShader(shader, fragShader);
                GL.LinkProgram(shader);
                GL.UseProgram(shader); // right now we only use one shader.

                GL.DeleteShader(vertShader);
                GL.DeleteShader(fragShader);
            });


            Jobs.Enqueue(() => {
                var shape = Cube.Unit();
                var mesh = new MeshRenderer(Vector3.One, Vector3.One, Vector3.One, shape);
                meshes.Add(mesh);
            });

            Game.Camera cam = new(60f);

            var view = Matrix4.Invert(Matrix4.CreateTranslation(0,0,-5));
            var proj = cam.CalculateProjection();

            viewProjection = view * proj;
        }

        public void Render(TimeSpan span)
        {

            while (Jobs.Count > 0)
            {
                Jobs.Dequeue()?.Invoke();
                ThrowGLError();
            }

            Matrix4 mvp = Matrix4.Identity;
            foreach(var mesh in meshes)
            {
                var vertices = mesh.shapes.SelectMany(i => i.Vertices).ToArray();
                var size = Marshal.SizeOf<Vertex>() * vertices.Length;

                var mvpLocation = GL.GetUniformLocation(shader, "mvp");

                mvp = viewProjection * mesh.Transform;

                GL.UniformMatrix4(mvpLocation, false, ref mvp);

                GL.BufferData(BufferTarget.ArrayBuffer, size, vertices, BufferUsageHint.DynamicDraw);

                GL.DrawArrays(PrimitiveType.TriangleFan, 0, vertices.Length);
            }

        }

        private static void ThrowGLError()
        {
            ErrorCode err = GL.GetError();
            if (err != ErrorCode.NoError)
                Notifications.Now(err.ToString());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }

                GL.DeleteBuffers(2, new int[] { vbo, vao });
                GL.DeleteShader(shader);

                disposedValue = true;
            }
        }
        ~GL4Renderer()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void EnqueueJob(Action value)
        {
            Jobs.Enqueue(value);
        }
    }
}
