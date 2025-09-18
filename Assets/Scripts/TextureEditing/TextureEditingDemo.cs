using UnityEngine;
using TextureEditing;

namespace TextureEditing.Samples
{
    /// <summary>
    /// Demo script showing how to use the texture editing system
    /// </summary>
    public class TextureEditingDemo : MonoBehaviour
    {
        [Header("Demo Objects")]
        [SerializeField] private GameObject sphereObject;
        [SerializeField] private GameObject cubeObject;
        [SerializeField] private GameObject cylinderObject;
        
        [Header("Sample Textures")]
        [SerializeField] private Texture2D[] sampleTextures;
        
        [Header("Demo Settings")]
        [SerializeField] private bool autoRunDemo = false;
        [SerializeField] private float demoStepDelay = 2f;
        
        private TextureEditor textureEditor;
        private ObjectWrapper[] objectWrappers;
        private int currentDemoStep = 0;

        private void Start()
        {
            // Initialize components
            textureEditor = FindObjectOfType<TextureEditor>();
            if (textureEditor == null)
            {
                GameObject editorGO = new GameObject("Texture Editor");
                textureEditor = editorGO.AddComponent<TextureEditor>();
            }
            
            // Setup object wrappers
            SetupObjectWrappers();
            
            // Generate sample textures if none provided
            if (sampleTextures == null || sampleTextures.Length == 0)
            {
                GenerateSampleTextures();
            }
            
            if (autoRunDemo)
            {
                InvokeRepeating(nameof(RunDemoStep), 1f, demoStepDelay);
            }
        }

        private void SetupObjectWrappers()
        {
            GameObject[] demoObjects = { sphereObject, cubeObject, cylinderObject };
            objectWrappers = new ObjectWrapper[demoObjects.Length];
            
            for (int i = 0; i < demoObjects.Length; i++)
            {
                if (demoObjects[i] != null)
                {
                    ObjectWrapper wrapper = demoObjects[i].GetComponent<ObjectWrapper>();
                    if (wrapper == null)
                    {
                        wrapper = demoObjects[i].AddComponent<ObjectWrapper>();
                    }
                    objectWrappers[i] = wrapper;
                }
            }
        }

        private void GenerateSampleTextures()
        {
            sampleTextures = new Texture2D[4];
            
            // Generate checkerboard
            sampleTextures[0] = GenerateCheckerboard(256, 256);
            sampleTextures[0].name = "Checkerboard";
            
            // Generate noise
            sampleTextures[1] = GenerateNoise(256, 256);
            sampleTextures[1].name = "Noise";
            
            // Generate gradient
            sampleTextures[2] = GenerateGradient(256, 256);
            sampleTextures[2].name = "Gradient";
            
            // Generate pattern
            sampleTextures[3] = GeneratePattern(256, 256);
            sampleTextures[3].name = "Pattern";
        }

        private void RunDemoStep()
        {
            switch (currentDemoStep)
            {
                case 0:
                    DemoStep1_LoadTexture();
                    break;
                case 1:
                    DemoStep2_ApplyDeformation();
                    break;
                case 2:
                    DemoStep3_CreateMold();
                    break;
                case 3:
                    DemoStep4_WrapToSphere();
                    break;
                case 4:
                    DemoStep5_WrapToCube();
                    break;
                case 5:
                    DemoStep6_WrapToCylinder();
                    break;
                default:
                    CancelInvoke(nameof(RunDemoStep));
                    return;
            }
            
            currentDemoStep++;
        }

        private void DemoStep1_LoadTexture()
        {
            Debug.Log("Demo Step 1: Loading sample texture");
            if (sampleTextures.Length > 0)
            {
                textureEditor.LoadTexture(sampleTextures[0]);
            }
        }

        private void DemoStep2_ApplyDeformation()
        {
            Debug.Log("Demo Step 2: Applying deformation");
            textureEditor.SetDeformationType(TextureDeformer.DeformationType.Wave);
            textureEditor.SetDeformationIntensity(0.7f);
            textureEditor.ApplyDeformation();
        }

        private void DemoStep3_CreateMold()
        {
            Debug.Log("Demo Step 3: Creating mold");
            textureEditor.CreateMold("Demo Mold", Mold2D.MoldType.Spherical);
        }

        private void DemoStep4_WrapToSphere()
        {
            Debug.Log("Demo Step 4: Wrapping to sphere");
            if (objectWrappers[0] != null && textureEditor.SavedMolds.Count > 0)
            {
                objectWrappers[0].ApplyMold(textureEditor.SavedMolds[0], ObjectWrapper.WrapMode.Spherical);
            }
        }

        private void DemoStep5_WrapToCube()
        {
            Debug.Log("Demo Step 5: Wrapping to cube");
            if (objectWrappers[1] != null && textureEditor.WorkingTexture != null)
            {
                objectWrappers[1].WrapTexture(textureEditor.WorkingTexture, ObjectWrapper.WrapMode.BoxProjection);
            }
        }

        private void DemoStep6_WrapToCylinder()
        {
            Debug.Log("Demo Step 6: Wrapping to cylinder");
            if (objectWrappers[2] != null && textureEditor.WorkingTexture != null)
            {
                objectWrappers[2].WrapTexture(textureEditor.WorkingTexture, ObjectWrapper.WrapMode.Cylindrical);
            }
        }

        // Manual demo controls
        [ContextMenu("Run Demo Step 1")]
        public void ManualDemoStep1() => DemoStep1_LoadTexture();
        
        [ContextMenu("Run Demo Step 2")]
        public void ManualDemoStep2() => DemoStep2_ApplyDeformation();
        
        [ContextMenu("Run Demo Step 3")]
        public void ManualDemoStep3() => DemoStep3_CreateMold();
        
        [ContextMenu("Run Demo Step 4")]
        public void ManualDemoStep4() => DemoStep4_WrapToSphere();
        
        [ContextMenu("Run Demo Step 5")]
        public void ManualDemoStep5() => DemoStep5_WrapToCube();
        
        [ContextMenu("Run Demo Step 6")]
        public void ManualDemoStep6() => DemoStep6_WrapToCylinder();

        // Texture generation utilities
        private Texture2D GenerateCheckerboard(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool checker = ((x / 32) + (y / 32)) % 2 == 0;
                    pixels[y * width + x] = checker ? Color.white : Color.black;
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private Texture2D GenerateNoise(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float noise = Mathf.PerlinNoise((float)x / width * 8f, (float)y / height * 8f);
                    pixels[y * width + x] = new Color(noise, noise, noise, 1f);
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private Texture2D GenerateGradient(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float gradientX = (float)x / width;
                    float gradientY = (float)y / height;
                    Color color = Color.Lerp(Color.red, Color.blue, gradientX);
                    color = Color.Lerp(color, Color.green, gradientY * 0.5f);
                    pixels[y * width + x] = color;
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private Texture2D GeneratePattern(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    float v = (float)y / height;
                    
                    float pattern = Mathf.Sin(u * Mathf.PI * 8f) * Mathf.Sin(v * Mathf.PI * 8f);
                    pattern = (pattern + 1f) * 0.5f; // Normalize to 0-1
                    
                    Color color = Color.Lerp(Color.cyan, Color.magenta, pattern);
                    pixels[y * width + x] = color;
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            if (!autoRunDemo)
            {
                GUILayout.BeginArea(new Rect(10, 10, 200, 300));
                GUILayout.Label("Texture Editing Demo", EditorGUIUtility.isProSkin ? GUI.skin.label : GUI.skin.box);
                
                if (GUILayout.Button("Load Texture")) ManualDemoStep1();
                if (GUILayout.Button("Apply Deformation")) ManualDemoStep2();
                if (GUILayout.Button("Create Mold")) ManualDemoStep3();
                if (GUILayout.Button("Wrap to Sphere")) ManualDemoStep4();
                if (GUILayout.Button("Wrap to Cube")) ManualDemoStep5();
                if (GUILayout.Button("Wrap to Cylinder")) ManualDemoStep6();
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("Run Auto Demo"))
                {
                    autoRunDemo = true;
                    currentDemoStep = 0;
                    InvokeRepeating(nameof(RunDemoStep), 1f, demoStepDelay);
                }
                
                GUILayout.EndArea();
            }
        }
    }
}