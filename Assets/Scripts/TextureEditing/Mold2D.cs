using UnityEngine;
using System.Collections.Generic;
using System;

namespace TextureEditing
{
    /// <summary>
    /// Creates and manages 2D graphics molds that can be applied to objects
    /// </summary>
    [System.Serializable]
    public class Mold2D
    {
        [SerializeField] private Texture2D sourceTexture;
        [SerializeField] private Vector2[] controlPoints;
        [SerializeField] private float[] controlWeights;
        [SerializeField] private MoldType moldType;
        [SerializeField] private string moldName;

        public enum MoldType
        {
            Planar,
            Cylindrical,
            Spherical,
            Cubic,
            Custom
        }

        public Texture2D SourceTexture => sourceTexture;
        public Vector2[] ControlPoints => controlPoints;
        public float[] ControlWeights => controlWeights;
        public MoldType Type => moldType;
        public string Name => moldName;

        public Mold2D(Texture2D texture, string name = "New Mold")
        {
            sourceTexture = texture;
            moldName = name;
            moldType = MoldType.Planar;
            InitializeControlPoints();
        }

        /// <summary>
        /// Initialize default control points for the mold
        /// </summary>
        private void InitializeControlPoints()
        {
            controlPoints = new Vector2[]
            {
                new Vector2(0f, 0f),    // Bottom-left
                new Vector2(1f, 0f),    // Bottom-right
                new Vector2(1f, 1f),    // Top-right
                new Vector2(0f, 1f)     // Top-left
            };
            
            controlWeights = new float[] { 1f, 1f, 1f, 1f };
        }

        /// <summary>
        /// Creates a mold from a texture with specific control points
        /// </summary>
        public static Mold2D CreateMold(Texture2D texture, Vector2[] points, MoldType type, string name = "Custom Mold")
        {
            Mold2D mold = new Mold2D(texture, name);
            mold.moldType = type;
            mold.controlPoints = points ?? mold.controlPoints;
            mold.controlWeights = new float[mold.controlPoints.Length];
            
            for (int i = 0; i < mold.controlWeights.Length; i++)
            {
                mold.controlWeights[i] = 1f;
            }
            
            return mold;
        }

        /// <summary>
        /// Applies the mold to a target texture, creating a wrapped version
        /// </summary>
        public Texture2D ApplyToTexture(Texture2D target, float intensity = 1f)
        {
            if (target == null || sourceTexture == null) return target;

            Texture2D result = new Texture2D(target.width, target.height, target.format, false);
            Color[] targetPixels = target.GetPixels();
            Color[] sourcePixels = sourceTexture.GetPixels();
            Color[] resultPixels = new Color[targetPixels.Length];

            int width = target.width;
            int height = target.height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 uv = new Vector2((float)x / width, (float)y / height);
                    Vector2 moldUV = MapToMold(uv, intensity);
                    
                    // Sample from both textures and blend
                    Color targetColor = targetPixels[y * width + x];
                    Color moldColor = SampleTexture(sourcePixels, moldUV, sourceTexture.width, sourceTexture.height);
                    
                    resultPixels[y * width + x] = BlendColors(targetColor, moldColor, intensity);
                }
            }

            result.SetPixels(resultPixels);
            result.Apply();
            return result;
        }

        /// <summary>
        /// Maps UV coordinates to the mold space based on control points
        /// </summary>
        private Vector2 MapToMold(Vector2 uv, float intensity)
        {
            switch (moldType)
            {
                case MoldType.Cylindrical:
                    return MapCylindrical(uv, intensity);
                case MoldType.Spherical:
                    return MapSpherical(uv, intensity);
                case MoldType.Cubic:
                    return MapCubic(uv, intensity);
                case MoldType.Custom:
                    return MapCustom(uv, intensity);
                default:
                    return MapPlanar(uv, intensity);
            }
        }

        private Vector2 MapPlanar(Vector2 uv, float intensity)
        {
            // Simple bilinear interpolation between control points
            Vector2 bottom = Vector2.Lerp(controlPoints[0], controlPoints[1], uv.x);
            Vector2 top = Vector2.Lerp(controlPoints[3], controlPoints[2], uv.x);
            Vector2 result = Vector2.Lerp(bottom, top, uv.y);
            
            return Vector2.Lerp(uv, result, intensity);
        }

        private Vector2 MapCylindrical(Vector2 uv, float intensity)
        {
            float angle = uv.x * Mathf.PI * 2f;
            float height = uv.y;
            
            Vector2 cylindrical = new Vector2(
                0.5f + Mathf.Cos(angle) * 0.3f,
                height
            );
            
            return Vector2.Lerp(uv, cylindrical, intensity);
        }

        private Vector2 MapSpherical(Vector2 uv, float intensity)
        {
            float phi = uv.x * Mathf.PI * 2f;
            float theta = uv.y * Mathf.PI;
            
            Vector2 spherical = new Vector2(
                0.5f + Mathf.Sin(theta) * Mathf.Cos(phi) * 0.3f,
                0.5f + Mathf.Cos(theta) * 0.3f
            );
            
            return Vector2.Lerp(uv, spherical, intensity);
        }

        private Vector2 MapCubic(Vector2 uv, float intensity)
        {
            // Simple cubic distortion
            Vector2 cubic = new Vector2(
                uv.x + Mathf.Pow(uv.y - 0.5f, 3f) * 0.2f,
                uv.y + Mathf.Pow(uv.x - 0.5f, 3f) * 0.2f
            );
            
            return Vector2.Lerp(uv, cubic, intensity);
        }

        private Vector2 MapCustom(Vector2 uv, float intensity)
        {
            // Use control points for custom mapping
            if (controlPoints.Length < 4) return uv;
            
            // Weighted interpolation based on distance to control points
            Vector2 result = Vector2.zero;
            float totalWeight = 0f;
            
            for (int i = 0; i < controlPoints.Length; i++)
            {
                float distance = Vector2.Distance(uv, controlPoints[i]);
                float weight = controlWeights[i] / (distance + 0.001f);
                result += controlPoints[i] * weight;
                totalWeight += weight;
            }
            
            if (totalWeight > 0f)
            {
                result /= totalWeight;
            }
            
            return Vector2.Lerp(uv, result, intensity);
        }

        private Color SampleTexture(Color[] pixels, Vector2 uv, int width, int height)
        {
            uv.x = Mathf.Repeat(uv.x, 1f);
            uv.y = Mathf.Repeat(uv.y, 1f);

            float x = uv.x * (width - 1);
            float y = uv.y * (height - 1);

            int x1 = Mathf.FloorToInt(x);
            int y1 = Mathf.FloorToInt(y);
            int x2 = (x1 + 1) % width;
            int y2 = (y1 + 1) % height;

            float fx = x - x1;
            float fy = y - y1;

            Color c1 = pixels[y1 * width + x1];
            Color c2 = pixels[y1 * width + x2];
            Color c3 = pixels[y2 * width + x1];
            Color c4 = pixels[y2 * width + x2];

            Color top = Color.Lerp(c1, c2, fx);
            Color bottom = Color.Lerp(c3, c4, fx);
            
            return Color.Lerp(top, bottom, fy);
        }

        private Color BlendColors(Color target, Color mold, float intensity)
        {
            // Multiple blend modes can be implemented here
            return Color.Lerp(target, mold, intensity * mold.a);
        }

        /// <summary>
        /// Updates a specific control point
        /// </summary>
        public void UpdateControlPoint(int index, Vector2 newPosition, float weight = 1f)
        {
            if (index >= 0 && index < controlPoints.Length)
            {
                controlPoints[index] = newPosition;
                if (index < controlWeights.Length)
                {
                    controlWeights[index] = weight;
                }
            }
        }

        /// <summary>
        /// Adds a new control point to the mold
        /// </summary>
        public void AddControlPoint(Vector2 position, float weight = 1f)
        {
            var pointsList = new List<Vector2>(controlPoints);
            var weightsList = new List<float>(controlWeights);
            
            pointsList.Add(position);
            weightsList.Add(weight);
            
            controlPoints = pointsList.ToArray();
            controlWeights = weightsList.ToArray();
        }
    }
}