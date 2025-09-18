#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TextureEditing;

namespace TextureEditingEditor
{
    /// <summary>
    /// Custom Unity Editor window for texture editing and mold creation
    /// </summary>
    public class TextureEditorWindow : EditorWindow
    {
        private TextureEditor textureEditor;
        private Texture2D previewTexture;
        private Vector2 scrollPosition;
        
        // UI State
        private int selectedTab = 0;
        private string[] tabNames = { "Deformation", "Molds", "Wrapping", "Generation" };
        
        // Deformation Settings
        private TextureDeformer.DeformationType selectedDeformationType;
        private float deformationIntensity = 0.5f;
        private Vector2 deformationCenter = new Vector2(0.5f, 0.5f);
        private float deformationRadius = 0.3f;
        
        // Mold Settings
        private string newMoldName = "New Mold";
        private Mold2D.MoldType selectedMoldType = Mold2D.MoldType.Planar;
        private float moldIntensity = 1f;
        
        // Generation Settings
        private int generateWidth = 512;
        private int generateHeight = 512;
        private TextureEditor.ProceduralPattern selectedPattern;
        
        // Wrapping Settings
        private ObjectWrapper.WrapMode selectedWrapMode = ObjectWrapper.WrapMode.UV;
        private ObjectWrapper targetWrapper;

        [MenuItem("Tools/Texture Editor")]
        public static void ShowWindow()
        {
            TextureEditorWindow window = GetWindow<TextureEditorWindow>("Texture Editor");
            window.minSize = new Vector2(400, 600);
        }

        private void OnEnable()
        {
            // Find or create texture editor in scene
            textureEditor = FindObjectOfType<TextureEditor>();
            if (textureEditor == null)
            {
                GameObject editorGO = new GameObject("Texture Editor");
                textureEditor = editorGO.AddComponent<TextureEditor>();
            }

            // Subscribe to events
            if (textureEditor != null)
            {
                textureEditor.OnTextureChanged += OnTextureChanged;
                textureEditor.OnOperationCompleted += OnOperationCompleted;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (textureEditor != null)
            {
                textureEditor.OnTextureChanged -= OnTextureChanged;
                textureEditor.OnOperationCompleted -= OnOperationCompleted;
            }
        }

        private void OnGUI()
        {
            if (textureEditor == null)
            {
                EditorGUILayout.HelpBox("No TextureEditor found in scene. Please add one.", MessageType.Warning);
                if (GUILayout.Button("Create TextureEditor"))
                {
                    GameObject editorGO = new GameObject("Texture Editor");
                    textureEditor = editorGO.AddComponent<TextureEditor>();
                }
                return;
            }

            DrawHeader();
            DrawTabs();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            switch (selectedTab)
            {
                case 0:
                    DrawDeformationTab();
                    break;
                case 1:
                    DrawMoldsTab();
                    break;
                case 2:
                    DrawWrappingTab();
                    break;
                case 3:
                    DrawGenerationTab();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
            
            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label("Texture Editor", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Load Texture", GUILayout.Width(100)))
            {
                string path = EditorUtility.OpenFilePanel("Load Texture", "", "png,jpg,jpeg");
                if (!string.IsNullOrEmpty(path))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(path);
                    Texture2D loadedTexture = new Texture2D(2, 2);
                    loadedTexture.LoadImage(fileData);
                    textureEditor.LoadTexture(loadedTexture);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Preview texture
            if (textureEditor.WorkingTexture != null)
            {
                GUILayout.Label("Preview:", EditorStyles.boldLabel);
                Rect previewRect = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(true));
                EditorGUI.DrawPreviewTexture(previewRect, textureEditor.WorkingTexture);
            }
            
            EditorGUILayout.Space();
        }

        private void DrawTabs()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            EditorGUILayout.Space();
        }

        private void DrawDeformationTab()
        {
            GUILayout.Label("Deformation Settings", EditorStyles.boldLabel);
            
            selectedDeformationType = (TextureDeformer.DeformationType)EditorGUILayout.EnumPopup("Type", selectedDeformationType);
            deformationIntensity = EditorGUILayout.Slider("Intensity", deformationIntensity, 0f, 2f);
            deformationCenter = EditorGUILayout.Vector2Field("Center", deformationCenter);
            deformationRadius = EditorGUILayout.Slider("Radius", deformationRadius, 0.01f, 1f);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Apply Deformation", GUILayout.Height(30)))
            {
                textureEditor.SetDeformationType(selectedDeformationType);
                textureEditor.SetDeformationIntensity(deformationIntensity);
                textureEditor.SetDeformationCenter(deformationCenter);
                textureEditor.SetDeformationRadius(deformationRadius);
                textureEditor.ApplyDeformation();
            }
            
            EditorGUILayout.Space();
            
            // Quick deformation buttons
            GUILayout.Label("Quick Deformations:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Bend")) ApplyQuickDeformation(TextureDeformer.DeformationType.Bend);
            if (GUILayout.Button("Twist")) ApplyQuickDeformation(TextureDeformer.DeformationType.Twist);
            if (GUILayout.Button("Wave")) ApplyQuickDeformation(TextureDeformer.DeformationType.Wave);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Spherize")) ApplyQuickDeformation(TextureDeformer.DeformationType.Spherize);
            if (GUILayout.Button("Pinch")) ApplyQuickDeformation(TextureDeformer.DeformationType.Pinch);
            if (GUILayout.Button("Bulge")) ApplyQuickDeformation(TextureDeformer.DeformationType.Bulge);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMoldsTab()
        {
            GUILayout.Label("Mold Creation & Management", EditorStyles.boldLabel);
            
            // Create new mold
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Create New Mold", EditorStyles.boldLabel);
            newMoldName = EditorGUILayout.TextField("Name", newMoldName);
            selectedMoldType = (Mold2D.MoldType)EditorGUILayout.EnumPopup("Type", selectedMoldType);
            
            if (GUILayout.Button("Create Mold from Current Texture"))
            {
                textureEditor.CreateMold(newMoldName, selectedMoldType);
                newMoldName = "New Mold";
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Apply molds
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Apply Molds", EditorStyles.boldLabel);
            moldIntensity = EditorGUILayout.Slider("Intensity", moldIntensity, 0f, 2f);
            
            if (textureEditor.SavedMolds.Count > 0)
            {
                foreach (var mold in textureEditor.SavedMolds)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(mold.Name);
                    GUILayout.Label($"({mold.Type})");
                    
                    if (GUILayout.Button("Apply", GUILayout.Width(60)))
                    {
                        textureEditor.SetMoldIntensity(moldIntensity);
                        textureEditor.ApplyMold(mold);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No molds created yet.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawWrappingTab()
        {
            GUILayout.Label("Object Wrapping", EditorStyles.boldLabel);
            
            targetWrapper = (ObjectWrapper)EditorGUILayout.ObjectField("Target Object", targetWrapper, typeof(ObjectWrapper), true);
            selectedWrapMode = (ObjectWrapper.WrapMode)EditorGUILayout.EnumPopup("Wrap Mode", selectedWrapMode);
            
            EditorGUILayout.Space();
            
            if (targetWrapper != null)
            {
                if (GUILayout.Button("Wrap Current Texture", GUILayout.Height(30)))
                {
                    if (textureEditor.WorkingTexture != null)
                    {
                        targetWrapper.WrapTexture(textureEditor.WorkingTexture, selectedWrapMode);
                    }
                }
                
                EditorGUILayout.Space();
                
                GUILayout.Label("Available Molds for Wrapping:", EditorStyles.boldLabel);
                foreach (var mold in textureEditor.SavedMolds)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(mold.Name);
                    if (GUILayout.Button("Wrap", GUILayout.Width(60)))
                    {
                        targetWrapper.ApplyMold(mold, selectedWrapMode);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select an ObjectWrapper component to wrap textures around objects.", MessageType.Info);
                
                if (GUILayout.Button("Create ObjectWrapper on Selected"))
                {
                    if (Selection.activeGameObject != null)
                    {
                        targetWrapper = Selection.activeGameObject.GetComponent<ObjectWrapper>();
                        if (targetWrapper == null)
                        {
                            targetWrapper = Selection.activeGameObject.AddComponent<ObjectWrapper>();
                        }
                    }
                }
            }
        }

        private void DrawGenerationTab()
        {
            GUILayout.Label("Procedural Generation", EditorStyles.boldLabel);
            
            generateWidth = EditorGUILayout.IntField("Width", generateWidth);
            generateHeight = EditorGUILayout.IntField("Height", generateHeight);
            selectedPattern = (TextureEditor.ProceduralPattern)EditorGUILayout.EnumPopup("Pattern", selectedPattern);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Generate Texture", GUILayout.Height(30)))
            {
                textureEditor.GenerateProceduralTexture(generateWidth, generateHeight, selectedPattern);
            }
            
            EditorGUILayout.Space();
            
            // Quick generation buttons
            GUILayout.Label("Quick Patterns:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Checkerboard")) GenerateQuickPattern(TextureEditor.ProceduralPattern.Checkerboard);
            if (GUILayout.Button("Stripes")) GenerateQuickPattern(TextureEditor.ProceduralPattern.Stripes);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Noise")) GenerateQuickPattern(TextureEditor.ProceduralPattern.Noise);
            if (GUILayout.Button("Gradient")) GenerateQuickPattern(TextureEditor.ProceduralPattern.Gradient);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.BeginDisabledGroup(!textureEditor.CanUndo());
            if (GUILayout.Button("Undo"))
            {
                textureEditor.Undo();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(!textureEditor.CanRedo());
            if (GUILayout.Button("Redo"))
            {
                textureEditor.Redo();
            }
            EditorGUI.EndDisabledGroup();
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Save Texture"))
            {
                SaveCurrentTexture();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyQuickDeformation(TextureDeformer.DeformationType type)
        {
            textureEditor.SetDeformationType(type);
            textureEditor.SetDeformationIntensity(0.5f);
            textureEditor.SetDeformationCenter(new Vector2(0.5f, 0.5f));
            textureEditor.SetDeformationRadius(0.3f);
            textureEditor.ApplyDeformation();
        }

        private void GenerateQuickPattern(TextureEditor.ProceduralPattern pattern)
        {
            textureEditor.GenerateProceduralTexture(512, 512, pattern);
        }

        private void SaveCurrentTexture()
        {
            if (textureEditor.WorkingTexture == null) return;
            
            string path = EditorUtility.SaveFilePanel("Save Texture", "", "texture", "png");
            if (!string.IsNullOrEmpty(path))
            {
                byte[] pngData = textureEditor.WorkingTexture.EncodeToPNG();
                System.IO.File.WriteAllBytes(path, pngData);
                AssetDatabase.Refresh();
            }
        }

        private void OnTextureChanged(Texture2D newTexture)
        {
            previewTexture = newTexture;
            Repaint();
        }

        private void OnOperationCompleted(string operation)
        {
            Debug.Log($"Texture Editor: {operation}");
        }
    }

    /// <summary>
    /// Custom inspector for TextureEditor component
    /// </summary>
    [CustomEditor(typeof(TextureEditor))]
    public class TextureEditorInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Open Texture Editor Window"))
            {
                TextureEditorWindow.ShowWindow();
            }
        }
    }

    /// <summary>
    /// Custom inspector for ObjectWrapper component
    /// </summary>
    [CustomEditor(typeof(ObjectWrapper))]
    public class ObjectWrapperInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            ObjectWrapper wrapper = (ObjectWrapper)target;
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Open Texture Editor"))
            {
                TextureEditorWindow.ShowWindow();
            }
            
            EditorGUILayout.Space();
            
            if (wrapper.AvailableMolds.Count > 0)
            {
                EditorGUILayout.LabelField("Quick Mold Application:");
                foreach (var mold in wrapper.AvailableMolds)
                {
                    if (GUILayout.Button($"Apply {mold.Name}"))
                    {
                        wrapper.ApplyMold(mold);
                    }
                }
            }
        }
    }
}
#endif