using OpenTK.Graphics.OpenGL4;

namespace Lemur.Graphics
{
    internal static class DefaultShaders
    {
        /// <summary>
        ///  If this isn't null, it contains a bare minimal shader.
        ///  only useful when using multiple contexts, if possible.
        /// </summary>
        public static int? DefaultShader { get; private set; }

        /// <summary>
        /// This must get called from the graphics context.
        /// </summary>
        internal static void InitializeShaders()
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

            int vertShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertShader, vertShaderSource);
            GL.CompileShader(vertShader);

            int fragShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragShader, fragShaderSource);
            GL.CompileShader(fragShader);

            DefaultShader = GL.CreateProgram();
            GL.AttachShader(DefaultShader.Value, vertShader);
            GL.AttachShader(DefaultShader.Value, fragShader);
            GL.LinkProgram(DefaultShader.Value);

            GL.DeleteShader(vertShader);
            GL.DeleteShader(fragShader);
        }
    }
}
