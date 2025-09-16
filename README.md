# Unity Hub - Advanced Texture Editing Tool

A comprehensive Unity package for advanced texture editing and 2D graphics molding. This tool allows you to shape textures with precision and create reusable "molds" that can be wrapped around 3D objects seamlessly.

## 🚀 Features

- **Advanced Texture Deformation**: 8 different deformation types including Bend, Twist, Spherize, Wave, and more
- **2D Graphics Molding**: Create reusable texture molds with customizable control points
- **3D Object Wrapping**: Multiple projection modes for wrapping textures around any 3D geometry
- **Unity Editor Integration**: Intuitive editor window with real-time preview
- **Non-destructive Editing**: Full undo/redo system with operation history
- **Procedural Generation**: Built-in procedural texture generation

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── TextureEditing/           # Core texture editing system
│   │   ├── TextureEditor.cs      # Main editing interface
│   │   ├── TextureDeformer.cs    # Deformation algorithms
│   │   ├── Mold2D.cs            # 2D graphics molding system
│   │   ├── ObjectWrapper.cs      # 3D object wrapping
│   │   └── TextureEditingDemo.cs # Demo and examples
│   └── Editor/
│       └── TextureEditorWindow.cs # Unity Editor integration
├── Textures/                     # Sample textures
├── Materials/                    # Sample materials
└── Scenes/                       # Demo scenes
```

## 🛠️ Quick Start

1. **Open the Texture Editor**: Go to `Tools > Texture Editor` in Unity
2. **Load a texture**: Click "Load Texture" or drag a texture into the editor
3. **Apply deformations**: Use the Deformation tab to bend, twist, or spherize your texture
4. **Create molds**: Switch to the Molds tab to create reusable texture templates
5. **Wrap to objects**: Use the Wrapping tab to apply textures to 3D objects

## 📖 Documentation

For detailed documentation, see [TEXTURE_EDITOR_DOCUMENTATION.md](TEXTURE_EDITOR_DOCUMENTATION.md)

## 🎮 Demo

The project includes a comprehensive demo system:
- Attach `TextureEditingDemo` component to a GameObject
- Set up demo objects (sphere, cube, cylinder) 
- Enable "Auto Run Demo" for automatic demonstration

## 🔧 System Requirements

- Unity 2020.3 or later
- .NET Framework 4.x equivalent

## 🤝 Contributing

This is an open-source educational project. Feel free to contribute improvements and extensions!