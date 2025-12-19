# LayerLab Asset

Unity utility package with Singleton, FSM, Popup Manager, and UI components.

## Requirements

- **Unity 6000.0** (Unity 6) or higher

### Required Dependencies

This package requires the following packages to be installed separately:

| Package | Installation |
|---------|--------------|
| **UniRx** | [OpenUPM](https://openupm.com/packages/com.neuecc.unirx/) or [GitHub](https://github.com/neuecc/UniRx) |
| **DOTween** | [Asset Store](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) |
| **TextMeshPro** | Built-in (Window > TextMeshPro > Import TMP Essential Resources) |

## Installation

### Via Git URL (Unity Package Manager)

1. Open Unity Package Manager (Window > Package Manager)
2. Click the **+** button and select **Add package from git URL...**
3. Enter the repository URL:

```
https://github.com/[username]/LayerLabAsset.git
```

### Specific Version

```
https://github.com/[username]/LayerLabAsset.git#1.0.0
```

## Features

### Singleton Pattern
Generic singleton base class for MonoBehaviour.

```csharp
public class GameManager : Singleton<GameManager>
{
    // Your code here
}
```

### FSM (Finite State Machine)
State machine pattern implementation for character controllers.

### Popup Manager
Reactive popup management system with animations.

### UI Components
- **UIButton**: Extended button with DOTween animations
- **UICanvasView**: Base class for canvas-based UI views

## Namespace

All classes are under the `LayerLabAsset` namespace.

```csharp
using LayerLabAsset;
```

## License

MIT License
