using Lemur.Graphics;
using OpenTK.Mathematics;

namespace Lemur.Graphics
{
    public static class ShapeGenerator
    {
        public static Shape CreateCube()
        {
            // Define the vertices for a cube centered at the origin (0,0,0)
            Vector3[] positions = new Vector3[]
            {
            // Front face
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f),
            // Back face
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
            // Top face
            new Vector3(-0.5f,  0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f),
            // Bottom face
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f),
            // Right face
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            // Left face
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f),
            };

            // Define the normals for each face of the cube
            Vector3[] normals = new Vector3[]
            {
            // Front face
            new Vector3( 0.0f,  0.0f,  1.0f),
            new Vector3( 0.0f,  0.0f,  1.0f),
            new Vector3( 0.0f,  0.0f,  1.0f),
            new Vector3( 0.0f,  0.0f,  1.0f),
            // Back face
            new Vector3( 0.0f,  0.0f, -1.0f),
            new Vector3( 0.0f,  0.0f, -1.0f),
            new Vector3( 0.0f,  0.0f, -1.0f),
            new Vector3( 0.0f,  0.0f, -1.0f),
            // Top face
            new Vector3( 0.0f,  1.0f,  0.0f),
            new Vector3( 0.0f,  1.0f,  0.0f),
            new Vector3( 0.0f,  1.0f,  0.0f),
            new Vector3( 0.0f,  1.0f,  0.0f),
            // Bottom face
            new Vector3( 0.0f, -1.0f,  0.0f),
            new Vector3( 0.0f, -1.0f,  0.0f),
            new Vector3( 0.0f, -1.0f,  0.0f),
            new Vector3( 0.0f, -1.0f,  0.0f),
            // Right face
            new Vector3( 1.0f,  0.0f,  0.0f),
            new Vector3( 1.0f,  0.0f,  0.0f),
            new Vector3( 1.0f,  0.0f,  0.0f),
            new Vector3( 1.0f,  0.0f,  0.0f),
            // Left face
            new Vector3(-1.0f,  0.0f,  0.0f),
            new Vector3(-1.0f,  0.0f,  0.0f),
            new Vector3(-1.0f,  0.0f,  0.0f),
            new Vector3(-1.0f,  0.0f,  0.0f),
            };

            Color4[] colors = new Color4[positions.Length];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color4.White;

            Vertex[] vertices = new Vertex[positions.Length];
            for (int i = 0; i < positions.Length; i++)
                vertices[i] = new Vertex(positions[i], normals[i], colors[i]);

            return new Shape(vertices);
        }
    }
}