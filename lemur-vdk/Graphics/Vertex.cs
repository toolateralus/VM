using OpenTK.Mathematics;
using System.Runtime.InteropServices;

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
}
