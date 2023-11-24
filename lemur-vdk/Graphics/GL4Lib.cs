using Lemur.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Lemur.Graphics
{
    public class GL4RenderLib
    {
        private int vertexBuffer, vertexArray;
        private int shaderProgram;

        private readonly Queue<Action<GL4RenderLib>> JobQueue = new();
        private readonly List<Shape> shapes = new();
        public void EnqueueWork(Action<GL4RenderLib> job) => JobQueue.Enqueue(job);
        public GL4RenderLib()
        {
            vertexBuffer = GL.GenBuffer();
            vertexArray = GL.GenVertexArray();
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.BindVertexArray(vertexArray);

            var vertexSize = Marshal.SizeOf<Vertex>();

            // vert
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, 0);

            // normal
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, vertexSize);

            // color
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, vertexSize, vertexSize + Marshal.SizeOf<Vector3>());

            CompileShader();

            shapes.Add(Cube.Unit());
        }
        private void CompileShader()
        {
            var vertShaderSource = @"
                #version 400 core
                layout(location = 0) in vec3 inPosition;
                layout(location = 1) in vec3 inNormal;
                layout(location = 2) in vec3 inColor;
                
                out vec3 pass_Color;
                out vec3 pass_Normal;

                void main()
                {
                    gl_Position = vec4(inPosition, 1.0);
                    pass_Color = inColor;
                    pass_Normal = inNormal;
                }
            ";

            var fragShaderSource = @"
                #version 400 core
                in vec3 pass_Color;
                in vec3 pass_Normal;

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

            shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertShader);
            GL.AttachShader(shaderProgram, fragShader);
            GL.LinkProgram(shaderProgram);
            GL.UseProgram(shaderProgram); // right now we only use one shader.

            GL.DeleteShader(vertShader);
            GL.DeleteShader(fragShader);
        }

        
        public void Render(TimeSpan span)
        {
            var vertices = shapes.SelectMany(i => i.Vertices).ToArray();

            GL.BufferData(BufferTarget.ArrayBuffer, Marshal.SizeOf<Vertex>() * vertices.Length, vertices, BufferUsageHint.DynamicDraw);

            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Length);
            
            ErrorCode err;

            if ((err = GL.GetError()) != ErrorCode.NoError)
                Notifications.Now(err.ToString());

        }
    }
}
