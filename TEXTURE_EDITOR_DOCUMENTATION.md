# Advanced Texture Editing Tool for Unity

This Unity package provides a comprehensive texture editing system that allows you to shape textures in any degree and create "molds" of 2D graphics that can be wrapped around 3D objects seamlessly.

## Features

### 🎨 Advanced Texture Deformation
- **Multiple Deformation Types**: Bend, Twist, Spherize, Cylindrical, Wave, Pinch, Bulge, and Custom
- **Real-time Preview**: See changes instantly in the Unity Editor
- **Precise Control**: Adjustable intensity, center point, and radius for each deformation
- **Non-destructive Editing**: Full undo/redo history system

### 🔄 2D Graphics Molding System
- **Flexible Mold Types**: Planar, Cylindrical, Spherical, Cubic, and Custom molds
- **Control Points**: Define custom deformation patterns with weighted control points
- **Reusable Templates**: Save and apply molds to multiple textures
- **Blending Modes**: Various ways to combine molds with existing textures

### 🌐 3D Object Wrapping
- **Multiple Projection Modes**: UV, Planar, Cylindrical, Spherical, Box, and Triplanar projection
- **Automatic UV Generation**: Smart UV coordinate generation for complex geometries
- **Material Integration**: Seamless integration with Unity's material system
- **Real-time Application**: Apply textures and molds to objects in real-time

## Components

### TextureEditor
The main component that coordinates all texture editing operations.

```csharp
// Basic usage
TextureEditor editor = GetComponent<TextureEditor>();
editor.LoadTexture(myTexture);
editor.SetDeformationType(TextureDeformer.DeformationType.Wave);
editor.ApplyDeformation();
```

### TextureDeformer
Handles the mathematical deformation of textures.

```csharp
// Apply deformation directly
Texture2D deformed = TextureDeformer.DeformTexture(
    sourceTexture, 
    TextureDeformer.DeformationType.Spherize, 
    0.5f,           // intensity
    Vector2.one * 0.5f,  // center
    0.3f            // radius
);
```

### Mold2D
Creates and manages 2D graphics molds.

```csharp
// Create a new mold
Mold2D mold = new Mold2D(sourceTexture, "My Mold");
// Apply to another texture
Texture2D result = mold.ApplyToTexture(targetTexture, 1.0f);
```

### ObjectWrapper
Wraps textures around 3D objects using various projection methods.

```csharp
// Wrap texture around object
ObjectWrapper wrapper = GetComponent<ObjectWrapper>();
wrapper.WrapTexture(myTexture, ObjectWrapper.WrapMode.Spherical);
```

## Unity Editor Integration

### Texture Editor Window
Access the main editing interface through `Tools > Texture Editor`.

**Features:**
- **Deformation Tab**: Apply various deformations with real-time preview
- **Molds Tab**: Create and manage texture molds
- **Wrapping Tab**: Wrap textures around 3D objects
- **Generation Tab**: Create procedural textures

### Custom Inspectors
Enhanced inspectors for `TextureEditor` and `ObjectWrapper` components provide quick access to common operations.

## Getting Started

### 1. Basic Texture Editing

```csharp
// Create a texture editor in your scene
GameObject editorGO = new GameObject("Texture Editor");
TextureEditor editor = editorGO.AddComponent<TextureEditor>();

// Load a texture
editor.LoadTexture(yourTexture);

// Apply some deformations
editor.SetDeformationType(TextureDeformer.DeformationType.Bend);
editor.SetDeformationIntensity(0.7f);
editor.ApplyDeformation();
```

### 2. Creating and Using Molds

```csharp
// Create a mold from current texture
Mold2D mold = editor.CreateMold("Wave Pattern", Mold2D.MoldType.Cylindrical);

// Apply the mold to another texture
editor.LoadTexture(anotherTexture);
editor.ApplyMold(mold, 0.8f);
```

### 3. Wrapping Textures on Objects

```csharp
// Add ObjectWrapper to a GameObject
ObjectWrapper wrapper = myGameObject.AddComponent<ObjectWrapper>();

// Wrap current texture
wrapper.WrapTexture(editor.WorkingTexture, ObjectWrapper.WrapMode.Spherical);
```

## Advanced Usage

### Custom Deformation Algorithms

You can extend the system with custom deformation algorithms:

```csharp
// In TextureDeformer.ApplyDeformation, add custom case:
case DeformationType.Custom:
    return ApplyCustomDeformation(uv, offset, intensity);
```

### Custom Mold Types

Create specialized mold behaviors:

```csharp
// Extend Mold2D for custom mapping functions
private Vector2 MapCustom(Vector2 uv, float intensity)
{
    // Your custom mapping logic here
    return transformedUV;
}
```

### Procedural Texture Generation

Generate textures programmatically:

```csharp
editor.GenerateProceduralTexture(512, 512, TextureEditor.ProceduralPattern.Noise);
```

## Demo Scene

The package includes a `TextureEditingDemo` component that showcases all features:

1. **Load Sample Textures**: Generates procedural test textures
2. **Apply Deformations**: Demonstrates various deformation types
3. **Create Molds**: Shows mold creation and application
4. **Object Wrapping**: Wraps textures around sphere, cube, and cylinder

### Running the Demo

```csharp
// Attach TextureEditingDemo to a GameObject
// Set up demo objects (sphere, cube, cylinder)
// Enable "Auto Run Demo" for automatic demonstration
```

## Performance Considerations

- **Texture Size**: Larger textures require more processing time
- **History Size**: Limit undo history to manage memory usage
- **Real-time Updates**: Use lower resolution for real-time editing, higher for final output
- **Mold Complexity**: Complex molds with many control points may impact performance

## Supported Formats

- **Input**: PNG, JPG, JPEG (through Unity's texture import)
- **Output**: PNG (through Texture2D.EncodeToPNG())
- **Internal**: RGBA32 for maximum compatibility

## Limitations

- Requires Unity 2020.3 or later
- Editor-time tools require `UNITY_EDITOR` compilation
- Real-time deformation performance depends on texture resolution
- Some projection modes work better with specific mesh topologies

## Extension Points

The system is designed for extensibility:

1. **New Deformation Types**: Add to `TextureDeformer.DeformationType` enum
2. **Custom Projections**: Extend `ObjectWrapper.WrapMode` 
3. **Additional Blend Modes**: Modify `BlendColors` methods
4. **Custom UI**: Create additional editor windows for specialized workflows

## Troubleshooting

### Common Issues

**Texture not updating**: Ensure `Texture2D.Apply()` is called after pixel modifications.

**Performance issues**: Reduce texture resolution or limit the number of history states.

**UV mapping problems**: Check mesh UV coordinates and ensure proper unwrapping.

**Editor window not opening**: Verify assembly definitions are properly configured.

## API Reference

### Core Classes
- `TextureEditor`: Main editing interface
- `TextureDeformer`: Deformation algorithms
- `Mold2D`: 2D graphics molding system
- `ObjectWrapper`: 3D object texture wrapping
- `TextureEditingDemo`: Demonstration and testing

### Enumerations
- `TextureDeformer.DeformationType`: Available deformation types
- `Mold2D.MoldType`: Mold projection types
- `ObjectWrapper.WrapMode`: 3D wrapping projection modes
- `TextureEditor.ProceduralPattern`: Procedural generation patterns

### Events
- `OnTextureChanged`: Fired when working texture changes
- `OnMoldCreated`: Fired when new mold is created
- `OnOperationCompleted`: Fired when any operation completes

## License

This tool is provided as-is for educational and development purposes. Please ensure you have proper rights to any textures you process with this system.