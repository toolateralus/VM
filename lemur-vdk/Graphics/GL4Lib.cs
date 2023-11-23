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

    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 positions;
        public Color4 color;
        public Vector3 normals;
        public Vertex(Vector3 position, Vector3 normal, Color4 color)
        {
            this.normals = normal;
            this.positions = position;
            this.color = color;
        }
    }
    public class Shape
    {
        internal Vertex[] Vertices;

        public Shape(Vertex[] vertices)
        {
            Vertices = vertices;
        }
    }
    public class GL4RenderLib
    {
        private int vertexBuffer, vertexArray;
        private int shaderProgram;

        private readonly Queue<Action<GL4RenderLib>> JobQueue = new();
        private readonly List<Shape> shapes = new();
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

            shapes.Add(ShapeGenerator.CreateCube());
        }
        private void InitializeShaders()
        {
            var vertShaderSource = @"
#version 400 core
layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec3 inNormal;

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

            GL.DeleteShader(vertShader);
            GL.DeleteShader(fragShader);
        }
        private static void SetupBuffers()
        {
            var vertexSize = Marshal.SizeOf<Vertex>();

            // vert
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, 0);

            // color
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, vertexSize, vertexSize);

            // normal
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, vertexSize + Marshal.SizeOf<Color4>());
        }
        public void Render(TimeSpan span)
        {
            var vertices = shapes.SelectMany(i => i.Vertices).ToArray();

            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.UseProgram(shaderProgram);
            GL.BindVertexArray(vertexArray);

            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Length);
        }
    }
}
