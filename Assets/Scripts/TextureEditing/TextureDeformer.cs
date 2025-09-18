using UnityEngine;
using System;

namespace TextureEditing
{
    /// <summary>
    /// Core class for deforming and shaping textures with various algorithms
    /// </summary>
    public class TextureDeformer
    {
        public enum DeformationType
        {
            Bend,
            Twist,
            Spherize,
            Cylindrical,
            Wave,
            Pinch,
            Bulge,
            Custom
        }

        /// <summary>
        /// Deforms a texture based on the specified type and parameters
        /// </summary>
        public static Texture2D DeformTexture(Texture2D source, DeformationType type, float intensity, Vector2 center, float radius = 1f)
        {
            if (source == null) return null;

            Texture2D result = new Texture2D(source.width, source.height, source.format, false);
            Color[] sourcePixels = source.GetPixels();
            Color[] resultPixels = new Color[sourcePixels.Length];

            int width = source.width;
            int height = source.height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 uv = new Vector2((float)x / width, (float)y / height);
                    Vector2 deformedUV = ApplyDeformation(uv, type, intensity, center, radius);
                    
                    // Sample from the deformed position
                    Color sampledColor = SampleBilinear(sourcePixels, deformedUV, width, height);
                    resultPixels[y * width + x] = sampledColor;
                }
            }

            result.SetPixels(resultPixels);
            result.Apply();
            return result;
        }

        /// <summary>
        /// Applies specific deformation based on type
        /// </summary>
        private static Vector2 ApplyDeformation(Vector2 uv, DeformationType type, float intensity, Vector2 center, float radius)
        {
            Vector2 offset = uv - center;
            float distance = offset.magnitude;

            if (distance > radius) return uv;

            float falloff = 1f - (distance / radius);
            falloff = Mathf.Pow(falloff, 2f); // Smooth falloff

            switch (type)
            {
                case DeformationType.Bend:
                    return ApplyBend(uv, offset, intensity * falloff);
                
                case DeformationType.Twist:
                    return ApplyTwist(uv, center, distance, intensity * falloff);
                
                case DeformationType.Spherize:
                    return ApplySpherize(uv, center, offset, intensity * falloff);
                
                case DeformationType.Cylindrical:
                    return ApplyCylindrical(uv, center, offset, intensity * falloff);
                
                case DeformationType.Wave:
                    return ApplyWave(uv, intensity * falloff);
                
                case DeformationType.Pinch:
                    return ApplyPinch(uv, center, offset, distance, intensity * falloff);
                
                case DeformationType.Bulge:
                    return ApplyBulge(uv, center, offset, distance, intensity * falloff);
                
                default:
                    return uv;
            }
        }

        private static Vector2 ApplyBend(Vector2 uv, Vector2 offset, float intensity)
        {
            float bendAmount = intensity * 0.5f;
            return new Vector2(
                uv.x + offset.y * bendAmount,
                uv.y + offset.x * bendAmount
            );
        }

        private static Vector2 ApplyTwist(Vector2 uv, Vector2 center, float distance, float intensity)
        {
            float angle = intensity * Mathf.PI;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            
            Vector2 offset = uv - center;
            return center + new Vector2(
                offset.x * cos - offset.y * sin,
                offset.x * sin + offset.y * cos
            );
        }

        private static Vector2 ApplySpherize(Vector2 uv, Vector2 center, Vector2 offset, float intensity)
        {
            float sphereRadius = intensity * 0.5f;
            Vector2 normalized = offset.normalized;
            float currentDistance = offset.magnitude;
            float newDistance = Mathf.Lerp(currentDistance, sphereRadius, intensity);
            
            return center + normalized * newDistance;
        }

        private static Vector2 ApplyCylindrical(Vector2 uv, Vector2 center, Vector2 offset, float intensity)
        {
            float cylinderEffect = Mathf.Sin(offset.x * Mathf.PI * 2f) * intensity * 0.1f;
            return new Vector2(uv.x, uv.y + cylinderEffect);
        }

        private static Vector2 ApplyWave(Vector2 uv, float intensity)
        {
            float waveX = Mathf.Sin(uv.y * Mathf.PI * 4f) * intensity * 0.1f;
            float waveY = Mathf.Sin(uv.x * Mathf.PI * 4f) * intensity * 0.1f;
            return new Vector2(uv.x + waveX, uv.y + waveY);
        }

        private static Vector2 ApplyPinch(Vector2 uv, Vector2 center, Vector2 offset, float distance, float intensity)
        {
            float pinchFactor = 1f - intensity * 0.8f;
            return center + offset * pinchFactor;
        }

        private static Vector2 ApplyBulge(Vector2 uv, Vector2 center, Vector2 offset, float distance, float intensity)
        {
            float bulgeFactor = 1f + intensity * 0.8f;
            return center + offset * bulgeFactor;
        }

        /// <summary>
        /// Bilinear sampling for smooth texture interpolation
        /// </summary>
        private static Color SampleBilinear(Color[] pixels, Vector2 uv, int width, int height)
        {
            // Clamp UV coordinates
            uv.x = Mathf.Clamp01(uv.x);
            uv.y = Mathf.Clamp01(uv.y);

            float x = uv.x * (width - 1);
            float y = uv.y * (height - 1);

            int x1 = Mathf.FloorToInt(x);
            int y1 = Mathf.FloorToInt(y);
            int x2 = Mathf.Min(x1 + 1, width - 1);
            int y2 = Mathf.Min(y1 + 1, height - 1);

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
    }
}