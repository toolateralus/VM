using Lemur.Windowing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Controls;

using static OpenTK.Graphics.OpenGL4.GL;

namespace Lemur {
    /// <summary>
    /// Interaction logic for GLSurface.xaml
    /// </summary>
    public unsafe partial class GLSurface : UserControl {
        readonly int vao, vbo, shaderProgram;

        readonly float[] vertices = {
                // Positions       // Texture Coordinates // Normals
                -0.5f, -0.5f, 0.0f,  0.0f, 0.0f,  0.0f, 0.0f, 1.0f, // Bottom-left
                 0.5f, -0.5f, 0.0f,  1.0f, 0.0f,  0.0f, 0.0f, 1.0f, // Bottom-right
                -0.5f,  0.5f, 0.0f,  0.0f, 1.0f,  0.0f, 0.0f, 1.0f, // Top-left
                 0.5f,  0.5f, 0.0f,  1.0f, 1.0f,  0.0f, 0.0f, 1.0f  // Top-right
        };

        public GLSurface() {
            InitializeComponent();

            var settings = new GLWpfControlSettings {
                TransparentBackground = false,
                MajorVersion = 4,
                MinorVersion = 4,
                RenderContinuously = true,
                GraphicsProfile = OpenTK.Windowing.Common.ContextProfile.Core,
            };

            surface.Start(settings);

            Enable(EnableCap.DebugOutput);
            Enable(EnableCap.DebugOutputSynchronous);
            DebugMessageCallback(DebugCallback, IntPtr.Zero);

            fixed (int* vao = &this.vao)
                GenVertexArrays(1, vao);

            fixed (int* vbo = &this.vbo)
                GenBuffers(1, vbo);

            BindVertexArray(vao);
            BindBuffer(BufferTarget.ArrayBuffer, vbo);
            BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 8, 0);
            EnableVertexAttribArray(0);
            VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(float) * 8, sizeof(float) * 3);
            EnableVertexAttribArray(1);
            VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, sizeof(float) * 8, sizeof(float) * 5);
            EnableVertexAttribArray(2);

            string vertexSource = @"
                #version 450 core
                layout(location = 0) in vec3 aPos;
                layout(location = 1) in vec2 aUV;
                layout(location = 2) in vec3 aNormal;
                void main() {
                    gl_Position = vec4(aPos, 1.0);
                }   
                ";

            string fragSource = @"
                #version 450 core
                out vec4 FragColor;
                void main() {
                    FragColor = vec4(1.0);
                } ";

            int vertex = CreateShader(ShaderType.VertexShader);
            ShaderSource(vertex, vertexSource);
            CompileShader(vertex);
            CheckShader(vertex);

            int fragment = CreateShader(ShaderType.FragmentShader);
            ShaderSource(fragment, fragSource);
            CompileShader(fragment);
            CheckShader(fragment);

            shaderProgram = CreateProgram();
            AttachShader(shaderProgram, vertex);
            AttachShader(shaderProgram, fragment);
            LinkProgram(shaderProgram);
            CheckProgram(shaderProgram);

            DetachShader(shaderProgram, vertex);
            DetachShader(shaderProgram, fragment);
            DeleteShader(vertex);
            DeleteShader(fragment);

            ClearColor(Color4.Black);
        }

        private static void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) {
            var msg = $"[{source}] [{type}] [{severity}] ID: {id} Message: {Marshal.PtrToStringAnsi(message)}";
            Notifications.Now(msg);
            Debug.WriteLine(msg);
        }

        private static void CheckProgram(int program) {
            GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0) {
                string infoLog = GetProgramInfoLog(program);
                Notifications.Now(infoLog);
            }
        }

        private static void CheckShader(int shader) {
            GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0) {
                string infoLog = GetShaderInfoLog(shader);
                Notifications.Now(infoLog);
            }
        }

        public void OnRender(TimeSpan delta) {
            Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            UseProgram(shaderProgram);
            BindVertexArray(vao);
            DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }
    }
}