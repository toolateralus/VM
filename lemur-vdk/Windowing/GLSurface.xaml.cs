using Lemur.GUI;
using Lemur.Windowing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Controls;

using static OpenTK.Graphics.OpenGL4.GL;
using Vector3 = OpenTK.Mathematics.Vector3;

namespace Lemur {

    public readonly struct Vertex(Vector3 position, Vector2 uv, Vector3 normal) {
        public readonly Vector3 Position = position;
        public readonly Vector2 UV = uv;
        public readonly Vector3 Normal = normal;
    }

    public class Shader {
        readonly int handle;
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
        public Shader(string vertexSource, string fragSource) {
            int vertex = CreateShader(ShaderType.VertexShader);
            ShaderSource(vertex, vertexSource);
            CompileShader(vertex);
            CheckShader(vertex);

            int fragment = CreateShader(ShaderType.FragmentShader);
            ShaderSource(fragment, fragSource);
            CompileShader(fragment);
            CheckShader(fragment);

            handle = CreateProgram();
            AttachShader(handle, vertex);
            AttachShader(handle, fragment);
            LinkProgram(handle);
            CheckProgram(handle);

            DetachShader(handle, vertex);
            DetachShader(handle, fragment);
            DeleteShader(vertex);
            DeleteShader(fragment);
        }
        ~Shader() {
            DeleteProgram(handle);
        }
        public void Use() {
            UseProgram(handle);
        }
        static float[] Matrix4ToArray(Matrix4 matrix) {
            float[] data = new float[16];
            for (int i = 0; i < 4; i++) {
                for (int j = 0; j < 4; j++) {
                    data[i * 4 + j] = matrix[i, j];

                }
            }
            return data;
        }

        public void Set(string name, float v) {
            Uniform1(GetUniformLocation(handle, name), v);
        }
        public void Set(string name, double v) {
            Uniform1(GetUniformLocation(handle, name), v);
        }
        public void Set(string name, int v) {
            Uniform1(GetUniformLocation(handle, name), v);
        }
        public void Set(string name, Vector2 v) {
            Uniform2(GetUniformLocation(handle, name), v);
        }
        public void Set(string name, Vector3 v) {
            Uniform3(GetUniformLocation(handle, name), v);
        }
        public void Set(string name, Vector4 v) {
            Uniform4(GetUniformLocation(handle, name), v);
        }
        public void Set(string name, Matrix4 matrix) {
            UniformMatrix4(GetUniformLocation(handle, name), 1, false, Matrix4ToArray(matrix));
        }

    }

    public class Mesh(Vertex[]? vertices = null) {
#pragma warning disable CA1819 // Properties should not return arrays
        public Vertex[] Vertices { get; } = vertices ?? [
#pragma warning restore CA1819 // Properties should not return arrays
            // Front face
            new(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f)),
            new(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f)),
            new(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(0.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f)),
            new(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f)),

            // Back face
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f)),
            new(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f)),
            new(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector3(0.0f, 0.0f, -1.0f)),
            new(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(1.0f, 1.0f), new Vector3(0.0f, 0.0f, -1.0f)),

            // Left face
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0.0f, 0.0f), new Vector3(-1.0f, 0.0f, 0.0f)),
            new(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector3(-1.0f, 0.0f, 0.0f)),
            new(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector3(-1.0f, 0.0f, 0.0f)),
            new(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(1.0f, 1.0f), new Vector3(-1.0f, 0.0f, 0.0f)),

            // Right face
            new(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f)),
            new(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f)),
            new(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector3(1.0f, 0.0f, 0.0f)),
            new(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1.0f, 1.0f), new Vector3(1.0f, 0.0f, 0.0f)),

            // Top face
            new(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f)),
            new(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f)),
            new(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector3(0.0f, 1.0f, 0.0f)),
            new(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1.0f, 1.0f), new Vector3(0.0f, 1.0f, 0.0f)),

            // Bottom face
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)),
            new(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)),
            new(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector3(0.0f, -1.0f, 0.0f)),
            new(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1.0f, 1.0f), new Vector3(0.0f, -1.0f, 0.0f))
        ];
    }

    public class Camera {
        public Vector3 Position;
        public Vector3 Rotation;
        public float FieldOfView { get; set; }
        public float NearPlane { get; set; }
        public float FarPlane { get; set; }

        public Camera(Vector3 position, Vector3 rotation, float fieldOfView, float nearPlane, float farPlane) {
            Position = position;
            Rotation = rotation;
            FieldOfView = fieldOfView;
            NearPlane = nearPlane;
            FarPlane = farPlane;
        }

        private Vector3 Forward() {
            float pitch = MathHelper.DegreesToRadians(Rotation.X);
            float yaw = MathHelper.DegreesToRadians(Rotation.Y);

            Vector3 forward;
            forward.X = MathF.Cos(pitch) * MathF.Cos(yaw);
            forward.Y = MathF.Sin(pitch);
            forward.Z = MathF.Cos(pitch) * MathF.Sin(yaw);

            return Vector3.Normalize(forward);
        }

        public Matrix4 GetViewProjectionMatrix() {
            Vector3 forward = Forward();

            Matrix4 viewMatrix = Matrix4.LookAt(Position, Position + forward, Vector3.UnitY);

            Matrix4 projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(FieldOfView),
                16.0f / 9.0f,
                NearPlane,
                FarPlane
            );
            return viewMatrix * projectionMatrix;
        }

        public void Move(Vector3 translation) => Position += translation;
        public void Rotate(Vector3 rotation) => Rotation += rotation;
        public void SetFieldOfView(float fieldOfView) => FieldOfView = fieldOfView;
        public void SetNearPlane(float nearPlane) => NearPlane = nearPlane;
        public void SetFarPlane(float farPlane) => FarPlane = farPlane;
    }
    public unsafe class Renderer {
        private readonly int vao, vbo;
        private readonly Shader shader;
        private readonly List<Mesh> meshes = [ new Mesh() ];
        private readonly Camera camera;


        /// <summary>
        /// Add a mesh and return an index to the mesh added.
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public int AddMesh(Mesh mesh) {
            meshes.Add(mesh);
            return meshes.Count - 1;
        }
        public Renderer() {
            camera = new(new(0,0,-5), new(0, 90, 0), 70, 0.01f, 1000.0f);

            Enable(EnableCap.DebugOutput);
            Enable(EnableCap.DebugOutputSynchronous);
            DebugMessageCallback(DebugCallback, IntPtr.Zero);

            // Setup 
            {
                fixed (int* vao = &this.vao)
                    GenVertexArrays(1, vao);

                fixed (int* vbo = &this.vbo)
                    GenBuffers(1, vbo);

                BindVertexArray(vao);
                BindBuffer(BufferTarget.ArrayBuffer, vbo);

                VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), Marshal.OffsetOf<Vertex>("Position"));
                EnableVertexAttribArray(0);
                VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(Vertex), Marshal.OffsetOf<Vertex>("UV"));
                EnableVertexAttribArray(1);
                VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), Marshal.OffsetOf<Vertex>("Normal"));
                EnableVertexAttribArray(2);
            }
           
            string vertexSource = @"
                #version 450 core

                layout(location = 0) in vec3 aPos;
                layout(location = 1) in vec2 aUV;
                layout(location = 2) in vec3 aNormal;

                uniform float time;
                uniform mat4 viewProjectionMatrix;
                uniform vec3 modelPos;

                void main() {
                    gl_Position = viewProjectionMatrix * vec4(modelPos + aPos, 1.0);
                }   
                ";
            string fragSource = @"
                #version 450 core
                out vec4 FragColor;
                void main() {
                    FragColor = vec4(1.0);
                } ";
            
            shader = new(vertexSource, fragSource);
            shader.Use();
            ClearColor(Color4.Black);
        }

        private static void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) {
            var msg = $"[{source}] [{type}] [{severity}] ID: {id} Message: {Marshal.PtrToStringAnsi(message)}";
            Notifications.Now(msg);
            Debug.WriteLine(msg);
        }

        public void Render(TimeSpan _) {
            
            Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            shader.Set("viewProjectionMatrix", camera.GetViewProjectionMatrix());
            
            shader.Set("time", GLFW.GetTime());

            foreach (var mesh in meshes) {
                shader.Set("modelPos", new Vector3(0));
                BindVertexArray(vao);
                BindBuffer(BufferTarget.ArrayBuffer, vbo);
                BufferData(BufferTarget.ArrayBuffer, mesh.Vertices.Length * sizeof(Vertex), mesh.Vertices, BufferUsageHint.StaticDraw);
                DrawArrays(PrimitiveType.TriangleStrip, 0, mesh.Vertices.Length);
            }
        }

    }

    /// <summary>
    /// Interaction logic for GLSurface.xaml
    /// </summary>
    public unsafe partial class GLSurface : UserControl {
        Renderer renderer;
        readonly GLWpfControlSettings settings = new() {
            TransparentBackground = false,
            MajorVersion = 4,
            MinorVersion = 5,
            RenderContinuously = true,
            GraphicsProfile = OpenTK.Windowing.Common.ContextProfile.Core,
        };

        public GLSurface() {
            InitializeComponent();
        }

        bool started = false;

        public void LateInit(Computer comptuer, ResizableWindow window) {
            surface.Start(settings);
            renderer = new();
            started = true;
        }

        public void OnRender(TimeSpan delta) {
            if (!started) return;
            renderer.Render(delta);
        }
    }
}