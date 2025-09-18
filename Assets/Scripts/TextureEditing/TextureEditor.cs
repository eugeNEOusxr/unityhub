using UnityEngine;
using System.Collections.Generic;

namespace TextureEditing
{
    /// <summary>
    /// Main texture editing interface that coordinates all texture editing operations
    /// </summary>
    public class TextureEditor : MonoBehaviour
    {
        [Header("Texture Settings")]
        [SerializeField] private Texture2D sourceTexture;
        [SerializeField] private Texture2D workingTexture;
        [SerializeField] private Material previewMaterial;
        
        [Header("Deformation Settings")]
        [SerializeField] private TextureDeformer.DeformationType currentDeformationType;
        [SerializeField] private float deformationIntensity = 0.5f;
        [SerializeField] private Vector2 deformationCenter = new Vector2(0.5f, 0.5f);
        [SerializeField] private float deformationRadius = 0.3f;
        
        [Header("Mold Settings")]
        [SerializeField] private List<Mold2D> savedMolds = new List<Mold2D>();
        [SerializeField] private Mold2D currentMold;
        [SerializeField] private float moldIntensity = 1f;
        
        [Header("History")]
        [SerializeField] private int maxHistorySize = 20;
        private List<Texture2D> textureHistory = new List<Texture2D>();
        private int currentHistoryIndex = -1;

        public Texture2D SourceTexture => sourceTexture;
        public Texture2D WorkingTexture => workingTexture;
        public List<Mold2D> SavedMolds => savedMolds;
        public Mold2D CurrentMold => currentMold;

        // Events
        public System.Action<Texture2D> OnTextureChanged;
        public System.Action<Mold2D> OnMoldCreated;
        public System.Action<string> OnOperationCompleted;

        private void Start()
        {
            if (sourceTexture != null)
            {
                LoadTexture(sourceTexture);
            }
        }

        /// <summary>
        /// Loads a new texture to edit
        /// </summary>
        public void LoadTexture(Texture2D texture)
        {
            if (texture == null) return;

            sourceTexture = texture;
            workingTexture = DuplicateTexture(texture);
            
            // Clear history and add initial state
            ClearHistory();
            AddToHistory(workingTexture);
            
            OnTextureChanged?.Invoke(workingTexture);
            OnOperationCompleted?.Invoke("Texture loaded");
        }

        /// <summary>
        /// Applies deformation to the current working texture
        /// </summary>
        public void ApplyDeformation()
        {
            if (workingTexture == null) return;

            Texture2D deformed = TextureDeformer.DeformTexture(
                workingTexture, 
                currentDeformationType, 
                deformationIntensity, 
                deformationCenter, 
                deformationRadius
            );

            if (deformed != null)
            {
                workingTexture = deformed;
                AddToHistory(workingTexture);
                OnTextureChanged?.Invoke(workingTexture);
                OnOperationCompleted?.Invoke($"Applied {currentDeformationType} deformation");
            }
        }

        /// <summary>
        /// Creates a mold from the current working texture
        /// </summary>
        public Mold2D CreateMold(string moldName = "New Mold", Mold2D.MoldType moldType = Mold2D.MoldType.Planar)
        {
            if (workingTexture == null) return null;

            Mold2D newMold = new Mold2D(DuplicateTexture(workingTexture), moldName);
            
            // Set custom control points based on current deformation
            Vector2[] controlPoints = GenerateControlPointsForMold(moldType);
            if (controlPoints != null)
            {
                for (int i = 0; i < controlPoints.Length && i < 4; i++)
                {
                    newMold.UpdateControlPoint(i, controlPoints[i]);
                }
            }

            savedMolds.Add(newMold);
            currentMold = newMold;
            
            OnMoldCreated?.Invoke(newMold);
            OnOperationCompleted?.Invoke($"Created mold: {moldName}");
            
            return newMold;
        }

        /// <summary>
        /// Applies a mold to the current working texture
        /// </summary>
        public void ApplyMold(Mold2D mold, float intensity = -1f)
        {
            if (mold == null || workingTexture == null) return;

            float actualIntensity = intensity >= 0 ? intensity : moldIntensity;
            Texture2D molded = mold.ApplyToTexture(workingTexture, actualIntensity);

            if (molded != null)
            {
                workingTexture = molded;
                AddToHistory(workingTexture);
                OnTextureChanged?.Invoke(workingTexture);
                OnOperationCompleted?.Invoke($"Applied mold: {mold.Name}");
            }
        }

        /// <summary>
        /// Applies multiple deformations in sequence
        /// </summary>
        public void ApplyDeformationSequence(DeformationStep[] steps)
        {
            if (steps == null || workingTexture == null) return;

            Texture2D result = workingTexture;
            
            foreach (var step in steps)
            {
                result = TextureDeformer.DeformTexture(
                    result,
                    step.type,
                    step.intensity,
                    step.center,
                    step.radius
                );
            }

            if (result != null)
            {
                workingTexture = result;
                AddToHistory(workingTexture);
                OnTextureChanged?.Invoke(workingTexture);
                OnOperationCompleted?.Invoke($"Applied {steps.Length} deformation steps");
            }
        }

        /// <summary>
        /// Generates a texture with procedural patterns
        /// </summary>
        public void GenerateProceduralTexture(int width, int height, ProceduralPattern pattern)
        {
            Texture2D generated = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 uv = new Vector2((float)x / width, (float)y / height);
                    pixels[y * width + x] = GeneratePatternColor(uv, pattern);
                }
            }

            generated.SetPixels(pixels);
            generated.Apply();

            LoadTexture(generated);
            OnOperationCompleted?.Invoke($"Generated {pattern} texture");
        }

        /// <summary>
        /// Blends two textures together
        /// </summary>
        public void BlendTextures(Texture2D textureA, Texture2D textureB, BlendMode blendMode, float blendAmount)
        {
            if (textureA == null || textureB == null) return;

            int width = Mathf.Max(textureA.width, textureB.width);
            int height = Mathf.Max(textureA.height, textureB.height);
            
            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 uv = new Vector2((float)x / width, (float)y / height);
                    
                    Color colorA = SampleTexture(textureA, uv);
                    Color colorB = SampleTexture(textureB, uv);
                    
                    pixels[y * width + x] = BlendColors(colorA, colorB, blendMode, blendAmount);
                }
            }

            result.SetPixels(pixels);
            result.Apply();

            workingTexture = result;
            AddToHistory(workingTexture);
            OnTextureChanged?.Invoke(workingTexture);
            OnOperationCompleted?.Invoke($"Blended textures using {blendMode}");
        }

        #region History Management

        private void AddToHistory(Texture2D texture)
        {
            if (texture == null) return;

            // Remove any history after current index
            if (currentHistoryIndex < textureHistory.Count - 1)
            {
                textureHistory.RemoveRange(currentHistoryIndex + 1, textureHistory.Count - currentHistoryIndex - 1);
            }

            // Add new texture
            textureHistory.Add(DuplicateTexture(texture));
            currentHistoryIndex = textureHistory.Count - 1;

            // Maintain max history size
            while (textureHistory.Count > maxHistorySize)
            {
                textureHistory.RemoveAt(0);
                currentHistoryIndex--;
            }
        }

        public void Undo()
        {
            if (CanUndo())
            {
                currentHistoryIndex--;
                workingTexture = DuplicateTexture(textureHistory[currentHistoryIndex]);
                OnTextureChanged?.Invoke(workingTexture);
                OnOperationCompleted?.Invoke("Undo");
            }
        }

        public void Redo()
        {
            if (CanRedo())
            {
                currentHistoryIndex++;
                workingTexture = DuplicateTexture(textureHistory[currentHistoryIndex]);
                OnTextureChanged?.Invoke(workingTexture);
                OnOperationCompleted?.Invoke("Redo");
            }
        }

        public bool CanUndo() => currentHistoryIndex > 0;
        public bool CanRedo() => currentHistoryIndex < textureHistory.Count - 1;

        private void ClearHistory()
        {
            textureHistory.Clear();
            currentHistoryIndex = -1;
        }

        #endregion

        #region Utility Methods

        private Texture2D DuplicateTexture(Texture2D source)
        {
            if (source == null) return null;

            Texture2D duplicate = new Texture2D(source.width, source.height, source.format, false);
            duplicate.SetPixels(source.GetPixels());
            duplicate.Apply();
            return duplicate;
        }

        private Vector2[] GenerateControlPointsForMold(Mold2D.MoldType moldType)
        {
            switch (moldType)
            {
                case Mold2D.MoldType.Cylindrical:
                    return new Vector2[]
                    {
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.1f),
                        new Vector2(1f, 0.9f),
                        new Vector2(0f, 1f)
                    };

                case Mold2D.MoldType.Spherical:
                    return new Vector2[]
                    {
                        new Vector2(0.1f, 0.1f),
                        new Vector2(0.9f, 0.1f),
                        new Vector2(0.9f, 0.9f),
                        new Vector2(0.1f, 0.9f)
                    };

                default:
                    return null; // Use default control points
            }
        }

        private Color GeneratePatternColor(Vector2 uv, ProceduralPattern pattern)
        {
            switch (pattern)
            {
                case ProceduralPattern.Checkerboard:
                    return ((Mathf.FloorToInt(uv.x * 8) + Mathf.FloorToInt(uv.y * 8)) % 2 == 0) ? Color.white : Color.black;

                case ProceduralPattern.Stripes:
                    return (Mathf.FloorToInt(uv.x * 10) % 2 == 0) ? Color.white : Color.black;

                case ProceduralPattern.Noise:
                    float noise = Mathf.PerlinNoise(uv.x * 10, uv.y * 10);
                    return new Color(noise, noise, noise, 1f);

                case ProceduralPattern.Gradient:
                    return Color.Lerp(Color.black, Color.white, uv.x);

                default:
                    return Color.white;
            }
        }

        private Color SampleTexture(Texture2D texture, Vector2 uv)
        {
            if (texture == null) return Color.clear;

            int x = Mathf.FloorToInt(uv.x * texture.width);
            int y = Mathf.FloorToInt(uv.y * texture.height);
            
            x = Mathf.Clamp(x, 0, texture.width - 1);
            y = Mathf.Clamp(y, 0, texture.height - 1);
            
            return texture.GetPixel(x, y);
        }

        private Color BlendColors(Color a, Color b, BlendMode mode, float amount)
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
                    Color overlay = a.r < 0.5f ? 2f * a * b : Color.white - 2f * (Color.white - a) * (Color.white - b);
                    return Color.Lerp(a, overlay, amount);
                default:
                    return a;
            }
        }

        #endregion

        #region Settings

        public void SetDeformationType(TextureDeformer.DeformationType type)
        {
            currentDeformationType = type;
        }

        public void SetDeformationIntensity(float intensity)
        {
            deformationIntensity = Mathf.Clamp01(intensity);
        }

        public void SetDeformationCenter(Vector2 center)
        {
            deformationCenter = center;
        }

        public void SetDeformationRadius(float radius)
        {
            deformationRadius = Mathf.Clamp(radius, 0.01f, 1f);
        }

        public void SetMoldIntensity(float intensity)
        {
            moldIntensity = Mathf.Clamp01(intensity);
        }

        #endregion

        #region Enums

        [System.Serializable]
        public struct DeformationStep
        {
            public TextureDeformer.DeformationType type;
            public float intensity;
            public Vector2 center;
            public float radius;
        }

        public enum ProceduralPattern
        {
            Checkerboard,
            Stripes,
            Noise,
            Gradient
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
}