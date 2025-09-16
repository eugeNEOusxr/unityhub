using UnityEngine;
using System.Collections.Generic;

namespace TextureEditing.Utilities
{
    /// <summary>
    /// Utility class for texture operations and helper functions
    /// </summary>
    public static class TextureUtils
    {
        /// <summary>
        /// Creates a copy of a texture
        /// </summary>
        public static Texture2D DuplicateTexture(Texture2D source)
        {
            if (source == null) return null;

            Texture2D duplicate = new Texture2D(source.width, source.height, source.format, false);
            duplicate.SetPixels(source.GetPixels());
            duplicate.Apply();
            return duplicate;
        }

        /// <summary>
        /// Resizes a texture to new dimensions
        /// </summary>
        public static Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
        {
            if (source == null) return null;

            Texture2D resized = new Texture2D(newWidth, newHeight, source.format, false);
            Color[] sourcePixels = source.GetPixels();
            Color[] resizedPixels = new Color[newWidth * newHeight];

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    float u = (float)x / newWidth;
                    float v = (float)y / newHeight;
                    
                    Color sampledColor = BilinearSample(sourcePixels, u, v, source.width, source.height);
                    resizedPixels[y * newWidth + x] = sampledColor;
                }
            }

            resized.SetPixels(resizedPixels);
            resized.Apply();
            return resized;
        }

        /// <summary>
        /// Crops a texture to specified bounds
        /// </summary>
        public static Texture2D CropTexture(Texture2D source, int startX, int startY, int width, int height)
        {
            if (source == null) return null;

            // Clamp values to source texture bounds
            startX = Mathf.Clamp(startX, 0, source.width - 1);
            startY = Mathf.Clamp(startY, 0, source.height - 1);
            width = Mathf.Clamp(width, 1, source.width - startX);
            height = Mathf.Clamp(height, 1, source.height - startY);

            Texture2D cropped = new Texture2D(width, height, source.format, false);
            Color[] croppedPixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = source.GetPixel(startX + x, startY + y);
                    croppedPixels[y * width + x] = pixel;
                }
            }

            cropped.SetPixels(croppedPixels);
            cropped.Apply();
            return cropped;
        }

        /// <summary>
        /// Applies a color filter to a texture
        /// </summary>
        public static void ApplyColorFilter(Texture2D texture, Color filter, float intensity = 1f)
        {
            if (texture == null) return;

            Color[] pixels = texture.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color filtered = pixels[i] * filter;
                pixels[i] = Color.Lerp(pixels[i], filtered, intensity);
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }

        /// <summary>
        /// Adjusts brightness and contrast of a texture
        /// </summary>
        public static void AdjustBrightnessContrast(Texture2D texture, float brightness, float contrast)
        {
            if (texture == null) return;

            Color[] pixels = texture.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                
                // Apply contrast
                pixel.r = ((pixel.r - 0.5f) * contrast) + 0.5f;
                pixel.g = ((pixel.g - 0.5f) * contrast) + 0.5f;
                pixel.b = ((pixel.b - 0.5f) * contrast) + 0.5f;
                
                // Apply brightness
                pixel.r += brightness;
                pixel.g += brightness;
                pixel.b += brightness;
                
                // Clamp values
                pixel.r = Mathf.Clamp01(pixel.r);
                pixel.g = Mathf.Clamp01(pixel.g);
                pixel.b = Mathf.Clamp01(pixel.b);
                
                pixels[i] = pixel;
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }

        /// <summary>
        /// Generates UV coordinates for a mesh based on projection type
        /// </summary>
        public static Vector2[] GenerateUVCoordinates(Vector3[] vertices, UVProjectionType projectionType)
        {
            if (vertices == null || vertices.Length == 0) return new Vector2[0];

            Vector2[] uvs = new Vector2[vertices.Length];
            Bounds bounds = CalculateBounds(vertices);

            for (int i = 0; i < vertices.Length; i++)
            {
                uvs[i] = ProjectVertexToUV(vertices[i], bounds, projectionType);
            }

            return uvs;
        }

        /// <summary>
        /// Creates a normal map from a height map
        /// </summary>
        public static Texture2D CreateNormalMap(Texture2D heightMap, float strength = 1f)
        {
            if (heightMap == null) return null;

            Texture2D normalMap = new Texture2D(heightMap.width, heightMap.height, TextureFormat.RGBA32, false);
            Color[] normalPixels = new Color[heightMap.width * heightMap.height];

            for (int y = 0; y < heightMap.height; y++)
            {
                for (int x = 0; x < heightMap.width; x++)
                {
                    // Sample neighboring pixels for gradient calculation
                    float left = GetHeightValue(heightMap, x - 1, y);
                    float right = GetHeightValue(heightMap, x + 1, y);
                    float top = GetHeightValue(heightMap, x, y + 1);
                    float bottom = GetHeightValue(heightMap, x, y - 1);

                    // Calculate gradients
                    float dx = (right - left) * strength;
                    float dy = (top - bottom) * strength;

                    // Convert to normal vector
                    Vector3 normal = new Vector3(-dx, -dy, 1f).normalized;

                    // Convert to color (0-1 range)
                    Color normalColor = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f,
                        1f
                    );

                    normalPixels[y * heightMap.width + x] = normalColor;
                }
            }

            normalMap.SetPixels(normalPixels);
            normalMap.Apply();
            return normalMap;
        }

        /// <summary>
        /// Blends multiple textures together
        /// </summary>
        public static Texture2D BlendTextures(List<Texture2D> textures, List<float> weights, BlendMode blendMode = BlendMode.Normal)
        {
            if (textures == null || textures.Count == 0) return null;
            if (weights == null || weights.Count != textures.Count) return textures[0];

            // Find maximum dimensions
            int maxWidth = 0, maxHeight = 0;
            foreach (var texture in textures)
            {
                if (texture != null)
                {
                    maxWidth = Mathf.Max(maxWidth, texture.width);
                    maxHeight = Mathf.Max(maxHeight, texture.height);
                }
            }

            Texture2D result = new Texture2D(maxWidth, maxHeight, TextureFormat.RGBA32, false);
            Color[] resultPixels = new Color[maxWidth * maxHeight];

            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    Vector2 uv = new Vector2((float)x / maxWidth, (float)y / maxHeight);
                    Color blendedColor = Color.clear;
                    float totalWeight = 0f;

                    for (int i = 0; i < textures.Count; i++)
                    {
                        if (textures[i] != null && weights[i] > 0f)
                        {
                            Color sampledColor = BilinearSample(textures[i].GetPixels(), uv.x, uv.y, textures[i].width, textures[i].height);
                            blendedColor = BlendColors(blendedColor, sampledColor, blendMode, weights[i]);
                            totalWeight += weights[i];
                        }
                    }

                    if (totalWeight > 0f)
                    {
                        blendedColor /= totalWeight;
                    }

                    resultPixels[y * maxWidth + x] = blendedColor;
                }
            }

            result.SetPixels(resultPixels);
            result.Apply();
            return result;
        }

        #region Private Helper Methods

        private static Color BilinearSample(Color[] pixels, float u, float v, int width, int height)
        {
            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);

            float x = u * (width - 1);
            float y = v * (height - 1);

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

        private static Bounds CalculateBounds(Vector3[] vertices)
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

        private static Vector2 ProjectVertexToUV(Vector3 vertex, Bounds bounds, UVProjectionType projectionType)
        {
            switch (projectionType)
            {
                case UVProjectionType.Planar:
                    return new Vector2(
                        (vertex.x - bounds.min.x) / bounds.size.x,
                        (vertex.z - bounds.min.z) / bounds.size.z
                    );

                case UVProjectionType.Cylindrical:
                    Vector3 center = bounds.center;
                    Vector3 offset = vertex - center;
                    float angle = Mathf.Atan2(offset.z, offset.x);
                    float height = (vertex.y - bounds.min.y) / bounds.size.y;
                    return new Vector2((angle + Mathf.PI) / (2f * Mathf.PI), height);

                case UVProjectionType.Spherical:
                    Vector3 sphereCenter = bounds.center;
                    Vector3 sphereOffset = (vertex - sphereCenter).normalized;
                    float phi = Mathf.Atan2(sphereOffset.z, sphereOffset.x);
                    float theta = Mathf.Acos(sphereOffset.y);
                    return new Vector2((phi + Mathf.PI) / (2f * Mathf.PI), theta / Mathf.PI);

                default:
                    return new Vector2(
                        (vertex.x - bounds.min.x) / bounds.size.x,
                        (vertex.y - bounds.min.y) / bounds.size.y
                    );
            }
        }

        private static float GetHeightValue(Texture2D heightMap, int x, int y)
        {
            x = Mathf.Clamp(x, 0, heightMap.width - 1);
            y = Mathf.Clamp(y, 0, heightMap.height - 1);
            Color pixel = heightMap.GetPixel(x, y);
            return (pixel.r + pixel.g + pixel.b) / 3f; // Convert to grayscale
        }

        private static Color BlendColors(Color a, Color b, BlendMode mode, float amount)
        {
            switch (mode)
            {
                case BlendMode.Normal:
                    return Color.Lerp(a, b, amount);
                case BlendMode.Multiply:
                    return Color.Lerp(a, a * b, amount);
                case BlendMode.Screen:
                    return Color.Lerp(a, Color.white - (Color.white - a) * (Color.white - b), amount);
                case BlendMode.Overlay:
                    Color overlay = new Color(
                        a.r < 0.5f ? 2f * a.r * b.r : 1f - 2f * (1f - a.r) * (1f - b.r),
                        a.g < 0.5f ? 2f * a.g * b.g : 1f - 2f * (1f - a.g) * (1f - b.g),
                        a.b < 0.5f ? 2f * a.b * b.b : 1f - 2f * (1f - a.b) * (1f - b.b),
                        a.a
                    );
                    return Color.Lerp(a, overlay, amount);
                default:
                    return a;
            }
        }

        #endregion

        #region Enums

        public enum UVProjectionType
        {
            Planar,
            Cylindrical,
            Spherical,
            Box
        }

        public enum BlendMode
        {
            Normal,
            Multiply,
            Screen,
            Overlay
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for texture operations
    /// </summary>
    public static class TextureExtensions
    {
        /// <summary>
        /// Saves a texture to file
        /// </summary>
        public static void SaveToFile(this Texture2D texture, string path)
        {
            if (texture == null) return;

            byte[] pngData = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, pngData);
        }

        /// <summary>
        /// Gets the average color of a texture
        /// </summary>
        public static Color GetAverageColor(this Texture2D texture)
        {
            if (texture == null) return Color.clear;

            Color[] pixels = texture.GetPixels();
            Color average = Color.clear;

            foreach (Color pixel in pixels)
            {
                average += pixel;
            }

            return average / pixels.Length;
        }

        /// <summary>
        /// Checks if a texture is grayscale
        /// </summary>
        public static bool IsGrayscale(this Texture2D texture, float tolerance = 0.01f)
        {
            if (texture == null) return false;

            Color[] pixels = texture.GetPixels();

            foreach (Color pixel in pixels)
            {
                float diff = Mathf.Max(
                    Mathf.Abs(pixel.r - pixel.g),
                    Mathf.Abs(pixel.g - pixel.b),
                    Mathf.Abs(pixel.r - pixel.b)
                );

                if (diff > tolerance)
                    return false;
            }

            return true;
        }
    }
}