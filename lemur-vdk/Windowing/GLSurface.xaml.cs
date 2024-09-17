using Assimp;
using Lemur.JS.Embedded;
using Lemur.Windowing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Transactions;
using System.Windows.Controls;

using static OpenTK.Graphics.OpenGL4.GL;
using PrimitiveType = OpenTK.Graphics.OpenGL4.PrimitiveType;
using Quaternion = OpenTK.Mathematics.Quaternion;
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
    
    public class Mesh {
        public Vertex[] vertices = [];
        public int[] indices;
        public Mesh(string path) {
            Notifications.Now("Mesh created!");
            using AssimpContext importer = new();
            Scene scene = importer.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals | PostProcessSteps.FlipUVs);
            if (scene.HasMeshes) {
                Assimp.Mesh mesh = scene.Meshes[0];
                vertices = new Vertex[mesh.VertexCount];
                for (int i = 0; i < mesh.VertexCount; i++) {
                    Vector3D vertex = mesh.Vertices[i];
                    Vector3D normal = mesh.HasNormals ? mesh.Normals[i] : new Vector3D(0, 0, 0);
                    Vector3D texCoord = mesh.HasTextureCoords(0) ? mesh.TextureCoordinateChannels[0][i] : new Vector3D(0, 0, 0);
                    vertices[i] = new Vertex(
                        new Vector3(vertex.X, vertex.Y, vertex.Z),
                        new Vector2(texCoord.X, texCoord.Y),
                        new Vector3(normal.X, normal.Y, normal.Z)
                    );
                }
                indices = mesh.GetIndices();
            }

            string n = "Mesh Cube: \n";
            foreach (Vertex vert in vertices) {
                n += $"pos: {vert.Position}, uv: {vert.UV}, normal: {vert.Normal}\n";
            }
            Notifications.Now(n);
        }
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

    public unsafe class Renderer : embedable {
        private readonly int vao, vbo, ebo;
        private Shader shader;
        private readonly Camera camera;

        Queue<Action> drawCommands = [];

        static List<Shader> shaders = [];


        int shadersBacklog ;

        [ApiDoc("Compile a shader from vertex and fragment source")]
        public int compileShader(string vertexShader, string fragmentShader) {
            drawCommands.Enqueue(delegate {
                shadersBacklog--;
                shaders.Add(new(vertexShader, fragmentShader));
            });
            var len = shaders.Count + shadersBacklog;
            shadersBacklog++;
            return len;
        }
        
        [ApiDoc("Set a shader by index, shaders can be compiled with .compileShader(vert, frag)")]
        public void setShader(int index) {
            drawCommands.Enqueue(delegate { 
                if (index >= 0 && index < shaders.Count) {
                    shader = shaders[index];
                    shader.Use();
                }
            });
        }

        [ApiDoc("draw a Mesh instance")]
        public void drawMesh(Mesh mesh, Vector3 translation, Vector3 rotation, Vector3 scale) {
            drawCommands.Enqueue(delegate {
                Matrix4 srtMatrix = GetModelMatrix(translation, rotation, scale);
                shader?.Set("modelMatrix", srtMatrix);
                BindBuffer(BufferTarget.ArrayBuffer, vbo);
                BufferData(BufferTarget.ArrayBuffer, mesh.vertices.Length * sizeof(Vertex), mesh.vertices, BufferUsageHint.DynamicDraw);
                BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
                BufferData(BufferTarget.ElementArrayBuffer, mesh.indices.Length * sizeof(int), mesh.indices, BufferUsageHint.DynamicDraw);
                DrawElements(PrimitiveType.Triangles, mesh.indices.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);
            });
        }

        private static Matrix4 GetModelMatrix(Vector3 translation, Vector3 rotation, Vector3 scale) => Matrix4.CreateScale(scale) * Matrix4.CreateFromQuaternion(Quaternion.FromEulerAngles(rotation)) * Matrix4.CreateTranslation(translation);

        public Renderer() {
            camera = new(new(0, 0, -5), new(0, 90, 0), 70, 0.01f, 1000.0f);
            Enable(EnableCap.DebugOutput);
            Enable(EnableCap.DebugOutputSynchronous);
            DebugMessageCallback(DebugCallback, IntPtr.Zero);
            Enable(EnableCap.CullFace);
            // Setup 
            {
                fixed (int* vao = &this.vao)
                    GenVertexArrays(1, vao);

                fixed (int* vbo = &this.vbo)
                    GenBuffers(1, vbo);

                fixed (int* ebo = &this.ebo)
                    GenBuffers(1, ebo);

                BindVertexArray(vao);
                BindBuffer(BufferTarget.ArrayBuffer, vbo);

                VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), Marshal.OffsetOf<Vertex>("Position"));
                EnableVertexAttribArray(0);
                VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(Vertex), Marshal.OffsetOf<Vertex>("UV"));
                EnableVertexAttribArray(1);
                VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, sizeof(Vertex), Marshal.OffsetOf<Vertex>("Normal"));
                EnableVertexAttribArray(2);

                // Bind the EBO
                BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            }

            ClearColor(Color4.Black);
        }
        private static void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) {
            if (severity == DebugSeverity.DebugSeverityNotification) {
                return;
            }
            var msg = $"[{source}] [{type}] [{severity}] ID: {id} Message: {Marshal.PtrToStringAnsi(message)}";
            Notifications.Now(msg);
            Debug.WriteLine(msg);
        }

        public void setDrawCallback(string pid, string functionName) {
            var process = Computer.Current.ProcessManager.GetProcess(pid);
            drawCallback = delegate(float delta) {
                _ = process?.UI.Engine.Execute($"{pid}.{functionName}({delta});");
            };
        }

        Action<float> drawCallback;

        public void Render(TimeSpan span) {
            drawCallback?.Invoke((float)span.TotalMilliseconds);
            Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (shader is not null) {
                shader.Set("viewProjectionMatrix", camera.GetViewProjectionMatrix());
                shader.Set("time", GLFW.GetTime());
            }

            while (drawCommands.Count > 0) {
                var command = drawCommands.Dequeue();
                command?.Invoke();
            }
        }

        public void Dispose() {
            fixed(int* vbo = &this.vbo)
                DeleteBuffers(1, vbo);
            fixed (int* vao = &this.vao)
                DeleteVertexArrays(1, vao);
        }
    }

    /// <summary>
    /// Interaction logic for GLSurface.xaml
    /// </summary>
    public partial class GLSurface : UserControl {
        public Renderer renderer;
        readonly GLWpfControlSettings settings = new() {
            TransparentBackground = false,
            MajorVersion = 4,
            MinorVersion = 5,
            RenderContinuously = true,
            Profile = OpenTK.Windowing.Common.ContextProfile.Core,
        };
        public GLSurface() {
            InitializeComponent();
            surface.Start(settings);
            renderer = new();
        }
        ~GLSurface() {
            renderer.Dispose();
        }
        public void OnRender(TimeSpan delta) {
            renderer.Render(delta);
        }
    }
}