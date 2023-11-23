using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Lemur.Graphics
{
    public class GL4RenderLib
    {
        private int vertexBuffer, vertexArray;
        private int shaderProgram;

        private readonly ConcurrentQueue<Action<GL4RenderLib>> JobQueue = new();
        public void EnqueueWork(Action<GL4RenderLib> job) => JobQueue.Enqueue(job);

        public GL4RenderLib()
        {
            InitializeOpenGL();
            InitializeShaders();
            SetupBuffers();
        }

        private void InitializeOpenGL()
        {
            vertexBuffer = GL.GenBuffer();
            vertexArray = GL.GenVertexArray();

            GL.BindVertexArray(vertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
        }
        private void InitializeShaders()
        {
            var vertShaderSource = @"
                #version 400 core

                layout(location = 0) in vec2 inPosition;

                void main()
                {
                    gl_Position = vec4(inPosition, 0.0, 1.0);
                }
            ";

            var fragShaderSource = @"
                #version 400 core

                out vec4 fragColor;

                void main()
                {
                    fragColor = vec4(1.0, 0.0, 0.0, 1.0); // Red color
                }
            ";

            // Compile and link shaders to create a shader program
            int vertShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertShader, vertShaderSource);
            GL.CompileShader(vertShader);

            int fragShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragShader, fragShaderSource);
            GL.CompileShader(fragShader);

            shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertShader);
            GL.AttachShader(shaderProgram, fragShader);
            GL.LinkProgram(shaderProgram);

            GL.DeleteShader(vertShader);
            GL.DeleteShader(fragShader);
        }
        private static void SetupBuffers()
        {
            float[] vertices =
            {
                // Position         // Color
                -0.5f, -0.5f, 1.0f, 0.0f, 0.0f, // Bottom-left
                0.5f, -0.5f, 0.0f, 1.0f, 0.0f, // Bottom-right
                0.5f, 0.5f, 0.0f, 0.0f, 1.0f, // Top-right
                -0.5f, 0.5f, 1.0f, 1.0f, 1.0f  // Top-left
            };

            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // Set up vertex attribute pointers
            int vertexSize = 5 * sizeof(float);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, vertexSize, 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, 2 * sizeof(float));
        }
        public void Render(TimeSpan span)
        {
            GL.UseProgram(shaderProgram);
            GL.BindVertexArray(vertexArray);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
        }
    }
}
