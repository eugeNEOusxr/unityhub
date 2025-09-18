using UnityEngine;
using System.Collections.Generic;

namespace TextureEditing
{
    /// <summary>
    /// Handles wrapping 2D textures and molds around 3D objects
    /// </summary>
    public class ObjectWrapper : MonoBehaviour
    {
        [SerializeField] private Material targetMaterial;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private List<Mold2D> availableMolds = new List<Mold2D>();
        [SerializeField] private WrapMode wrapMode = WrapMode.UV;
        [SerializeField] private float wrapIntensity = 1f;
        [SerializeField] private Vector3 wrapDirection = Vector3.up;

        public enum WrapMode
        {
            UV,
            Planar,
            Cylindrical,
            Spherical,
            BoxProjection,
            Triplanar
        }

        public Material TargetMaterial => targetMaterial;
        public List<Mold2D> AvailableMolds => availableMolds;
        public WrapMode CurrentWrapMode => wrapMode;

        private void Start()
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
                
            if (targetMaterial == null && meshRenderer != null)
                targetMaterial = meshRenderer.material;
        }

        /// <summary>
        /// Wraps a texture around the object using the specified mode
        /// </summary>
        public void WrapTexture(Texture2D texture, WrapMode mode = WrapMode.UV)
        {
            if (texture == null || meshRenderer == null) return;

            wrapMode = mode;
            Texture2D wrappedTexture = GenerateWrappedTexture(texture, mode);
            
            if (wrappedTexture != null)
            {
                ApplyTextureToMaterial(wrappedTexture);
            }
        }

        /// <summary>
        /// Applies a mold to the object with texture wrapping
        /// </summary>
        public void ApplyMold(Mold2D mold, WrapMode mode = WrapMode.UV)
        {
            if (mold == null || mold.SourceTexture == null) return;

            // First apply the mold deformation
            Texture2D moldedTexture = mold.ApplyToTexture(mold.SourceTexture, wrapIntensity);
            
            // Then wrap it around the object
            WrapTexture(moldedTexture, mode);
        }

        /// <summary>
        /// Generates a wrapped texture based on the object's geometry and wrap mode
        /// </summary>
        private Texture2D GenerateWrappedTexture(Texture2D source, WrapMode mode)
        {
            if (meshRenderer == null || meshRenderer.GetComponent<MeshFilter>() == null)
                return source;

            Mesh mesh = meshRenderer.GetComponent<MeshFilter>().mesh;
            if (mesh == null) return source;

            int textureSize = 512; // Can be made configurable
            Texture2D result = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[textureSize * textureSize];

            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            int[] triangles = mesh.triangles;

            // Generate new UVs based on wrap mode
            Vector2[] newUVs = GenerateUVsForWrapMode(vertices, mode);

            // Sample the source texture using the new UVs
            for (int i = 0; i < newUVs.Length; i++)
            {
                Color sampledColor = SampleTexture(source, newUVs[i]);
                // Map vertex to texture space and set pixel
                Vector2 texturePos = uvs[i % uvs.Length];
                int x = Mathf.FloorToInt(texturePos.x * (textureSize - 1));
                int y = Mathf.FloorToInt(texturePos.y * (textureSize - 1));
                
                if (x >= 0 && x < textureSize && y >= 0 && y < textureSize)
                {
                    pixels[y * textureSize + x] = sampledColor;
                }
            }

            // Fill in gaps with interpolation
            FillTextureGaps(pixels, textureSize);

            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        /// <summary>
        /// Generates UV coordinates based on the wrap mode
        /// </summary>
        private Vector2[] GenerateUVsForWrapMode(Vector3[] vertices, WrapMode mode)
        {
            Vector2[] newUVs = new Vector2[vertices.Length];
            Bounds bounds = CalculateBounds(vertices);

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 localPos = transform.InverseTransformPoint(vertices[i]);
                newUVs[i] = ProjectVertex(localPos, mode, bounds);
            }

            return newUVs;
        }

        /// <summary>
        /// Projects a vertex to UV space based on the wrap mode
        /// </summary>
        private Vector2 ProjectVertex(Vector3 vertex, WrapMode mode, Bounds bounds)
        {
            switch (mode)
            {
                case WrapMode.Planar:
                    return ProjectPlanar(vertex, bounds);
                
                case WrapMode.Cylindrical:
                    return ProjectCylindrical(vertex, bounds);
                
                case WrapMode.Spherical:
                    return ProjectSpherical(vertex, bounds);
                
                case WrapMode.BoxProjection:
                    return ProjectBox(vertex, bounds);
                
                case WrapMode.Triplanar:
                    return ProjectTriplanar(vertex, bounds);
                
                default: // UV mode - use existing UVs
                    return new Vector2(
                        (vertex.x - bounds.min.x) / bounds.size.x,
                        (vertex.z - bounds.min.z) / bounds.size.z
                    );
            }
        }

        private Vector2 ProjectPlanar(Vector3 vertex, Bounds bounds)
        {
            Vector3 dir = wrapDirection.normalized;
            Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
            Vector3 up = Vector3.Cross(right, dir).normalized;

            float u = Vector3.Dot(vertex, right);
            float v = Vector3.Dot(vertex, up);

            return new Vector2(
                (u - bounds.min.x) / bounds.size.x,
                (v - bounds.min.z) / bounds.size.z
            );
        }

        private Vector2 ProjectCylindrical(Vector3 vertex, Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 offset = vertex - center;
            
            float angle = Mathf.Atan2(offset.z, offset.x);
            float height = (vertex.y - bounds.min.y) / bounds.size.y;
            
            return new Vector2(
                (angle + Mathf.PI) / (2f * Mathf.PI),
                height
            );
        }

        private Vector2 ProjectSpherical(Vector3 vertex, Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 offset = (vertex - center).normalized;
            
            float phi = Mathf.Atan2(offset.z, offset.x);
            float theta = Mathf.Acos(offset.y);
            
            return new Vector2(
                (phi + Mathf.PI) / (2f * Mathf.PI),
                theta / Mathf.PI
            );
        }

        private Vector2 ProjectBox(Vector3 vertex, Bounds bounds)
        {
            // Find the dominant axis and project accordingly
            Vector3 absVertex = new Vector3(Mathf.Abs(vertex.x), Mathf.Abs(vertex.y), Mathf.Abs(vertex.z));
            
            if (absVertex.x >= absVertex.y && absVertex.x >= absVertex.z)
            {
                // X-axis dominant
                return new Vector2(vertex.z / bounds.size.z + 0.5f, vertex.y / bounds.size.y + 0.5f);
            }
            else if (absVertex.y >= absVertex.z)
            {
                // Y-axis dominant
                return new Vector2(vertex.x / bounds.size.x + 0.5f, vertex.z / bounds.size.z + 0.5f);
            }
            else
            {
                // Z-axis dominant
                return new Vector2(vertex.x / bounds.size.x + 0.5f, vertex.y / bounds.size.y + 0.5f);
            }
        }

        private Vector2 ProjectTriplanar(Vector3 vertex, Bounds bounds)
        {
            // Simplified triplanar - can be expanded for proper triplanar mapping
            Vector3 normal = CalculateVertexNormal(vertex);
            Vector3 absNormal = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
            
            if (absNormal.x > absNormal.y && absNormal.x > absNormal.z)
                return new Vector2(vertex.z, vertex.y);
            else if (absNormal.y > absNormal.z)
                return new Vector2(vertex.x, vertex.z);
            else
                return new Vector2(vertex.x, vertex.y);
        }

        private Vector3 CalculateVertexNormal(Vector3 vertex)
        {
            // Simplified normal calculation - in a real implementation,
            // you'd use the mesh normals or calculate them properly
            return vertex.normalized;
        }

        private Bounds CalculateBounds(Vector3[] vertices)
        {
            if (vertices.Length == 0) return new Bounds();
            
            Vector3 min = vertices[0];
            Vector3 max = vertices[0];
            
            for (int i = 1; i < vertices.Length; i++)
            {
                min = Vector3.Min(min, vertices[i]);
                max = Vector3.Max(max, vertices[i]);
            }
            
            return new Bounds((min + max) * 0.5f, max - min);
        }

        private Color SampleTexture(Texture2D texture, Vector2 uv)
        {
            if (texture == null) return Color.white;
            
            int x = Mathf.FloorToInt(uv.x * texture.width) % texture.width;
            int y = Mathf.FloorToInt(uv.y * texture.height) % texture.height;
            
            if (x < 0) x += texture.width;
            if (y < 0) y += texture.height;
            
            return texture.GetPixel(x, y);
        }

        private void FillTextureGaps(Color[] pixels, int size)
        {
            // Simple gap filling - can be improved with better interpolation
            for (int y = 1; y < size - 1; y++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    int index = y * size + x;
                    if (pixels[index].a == 0) // Empty pixel
                    {
                        Color avg = Color.clear;
                        int count = 0;
                        
                        // Sample neighboring pixels
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int neighborIndex = (y + dy) * size + (x + dx);
                                if (pixels[neighborIndex].a > 0)
                                {
                                    avg += pixels[neighborIndex];
                                    count++;
                                }
                            }
                        }
                        
                        if (count > 0)
                        {
                            pixels[index] = avg / count;
                        }
                    }
                }
            }
        }

        private void ApplyTextureToMaterial(Texture2D texture)
        {
            if (targetMaterial == null)
            {
                targetMaterial = new Material(Shader.Find("Standard"));
                if (meshRenderer != null)
                    meshRenderer.material = targetMaterial;
            }
            
            targetMaterial.mainTexture = texture;
        }

        /// <summary>
        /// Adds a mold to the available molds list
        /// </summary>
        public void AddMold(Mold2D mold)
        {
            if (mold != null && !availableMolds.Contains(mold))
            {
                availableMolds.Add(mold);
            }
        }

        /// <summary>
        /// Removes a mold from the available molds list
        /// </summary>
        public void RemoveMold(Mold2D mold)
        {
            availableMolds.Remove(mold);
        }

        /// <summary>
        /// Sets the wrap intensity for mold applications
        /// </summary>
        public void SetWrapIntensity(float intensity)
        {
            wrapIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// Sets the wrap direction for planar projection
        /// </summary>
        public void SetWrapDirection(Vector3 direction)
        {
            wrapDirection = direction.normalized;
        }
    }
}