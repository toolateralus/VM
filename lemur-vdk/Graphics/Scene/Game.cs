using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;
using System;
using Lemur.Graphics;
using Lemur.GUI;
using Lemur.JS;

namespace Lemur.Game
{
    public class Node
    {
        // m prefix because the lowercase javascript syntax, distinguish from field and method.
        public Vector3 mPosition = default;
        public Vector3 mRotation = default;
        public Vector3 mScale = default;
        internal Matrix4 Transform
        {
            get
            {
                var scale = Matrix4.CreateScale(mScale);
                var rotation = Matrix4.CreateFromQuaternion(Quaternion.FromEulerAngles(mRotation));
                var translation = Matrix4.CreateTranslation(mPosition);
                return scale * rotation * translation;
            }
        }
        // javascript wrappers - hence the naming violations.
        public void scale(float x, float y, float z)
        {
            mScale += (x, y, z);
        }
        public void setScale(float x, float y, float z)
        {
            mScale = (x, y, z);
        }
        public void rotate(float x, float y, float z)
        {
            mRotation += (x, y, z);
        }
        public void setRotation(float x, float y, float z)
        {
            mRotation = (x, y, z);
        }
        public void move(float x, float y, float z)
        {
            mPosition += (x, y, z);
        }
        public void setPosition(float x, float y, float z)
        {
            mPosition = (x, y, z);
        }
        public Node(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            this.mPosition = position;
            this.mRotation = rotation;
            this.mScale = scale;
        }
        public Node() { }
        
    }
    public class Scene
    {
        public Scene(string id)
        {
            //var content =  JS.app.GetUserContent(Computer.Current);
        }
        internal GL4Renderer renderer;

        public List<Node> nodes = new();

        internal void Draw()
        {
            // jobs are executed at the first safe opportunity.
            // directly modifying vertices could behave poorly
            renderer.EnqueueJob(() => {
                IEnumerable<MeshRenderer> meshes = nodes.OfType<MeshRenderer>();
                renderer.meshes = meshes.ToList();
            });
        }
    }
    public class MeshRenderer : Node
    {
        public List<LemurShape> shapes = new();
        public MeshRenderer(Vector3 position, Vector3 rotation, Vector3 scale, params LemurShape[] shapes) : base(position, rotation, scale)
        {
            this.shapes = shapes.ToList();
        }
    }
    public class Camera : Node
    {
        public Camera(float perspectiveFov)
        {
            FoV = perspectiveFov;
        }
        public Camera(Vector2 orthographicRect)
        {
            OrthographicRect = orthographicRect;
            ProjectionType = CameraProjection.Orthographic;
        }
        private CameraProjection ProjectionType { get; set; } = CameraProjection.Perspective;
        public float FoV { get; set; }
        public float NearPlane { get; set; } = 0.01f;
        public float FarPlane { get; set; } = 10000f; // really far default. Limit this if you start drawing larger scenes.
        public Vector2 OrthographicRect { get; set; } = (100, 100); // width, height
        public Matrix4 CalculateProjection()
        {
            var fovy = MathHelper.DegreesToRadians(Math.Clamp(FoV, 0, 180));

            var depthNear = NearPlane;
            var depthFar = FarPlane;

            if (fovy <= 0f || (double)fovy > Math.PI)
                throw new ArgumentOutOfRangeException(nameof(FoV));
           
            if (depthNear <= 0f)
                throw new ArgumentOutOfRangeException(nameof(depthNear));
            if (depthFar <= 0f)
                throw new ArgumentOutOfRangeException(nameof(depthFar));

            if (ProjectionType == CameraProjection.Orthographic)
                return Matrix4.CreateOrthographic(OrthographicRect.X, OrthographicRect.Y, depthNear, depthFar);

            // todo: add dynamic aspect ratios and resolutions.
            var aspect = 800 / 600;

            if (aspect <= 0f)
                throw new ArgumentOutOfRangeException(nameof(aspect));

            return Matrix4.CreatePerspectiveFieldOfView(fovy, aspect, depthNear, depthFar);
        }
    }
    public enum CameraProjection
    {
        Perspective,
        Orthographic,
    }
    
}