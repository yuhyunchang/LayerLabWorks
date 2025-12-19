# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

LayerLab Asset is a Unity utility package providing Singleton, FSM, Popup Manager, and UI components. Requires Unity 6000.0+ (Unity 6).

**Required Dependencies** (must install separately):
- UniRx - Reactive extensions for popup management
- DOTween - Animation/tweening for UI components
- TextMeshPro - Built-in, import via Window > TextMeshPro

## Build & Development

This is a Unity Package Manager (UPM) package. No standalone build commands - open in Unity Editor.

**Solution file**: `LayerLabAsset.sln` for IDE integration (JetBrains Rider configured)

**Package location**: `Assets/LayerLabAsset/` contains all source code

## Architecture

### Namespace
All classes under `LayerLabAsset` namespace:
```csharp
using LayerLabAsset;
```

### Core Components

**Singleton Pattern** (`Runtime/Pattern/Singleton.cs`)
- Generic `Singleton<T>` base for MonoBehaviour managers
- Thread-safe with lazy initialization
- Auto-creates GameObject with DontDestroyOnLoad
- Usage: `public class GameManager : Singleton<GameManager> { }`

**FSM - Two Implementations** (`Runtime/Pattern/Fsm/`)
- `FsmClass<T>` - Enum-based state machine with Dictionary storage
- `CharacterFSM<T>` - MonoBehaviour-owned FSM using `CharacterStateBase<T>`
- Both support Enter/Update/Exit lifecycle hooks

**Popup Manager** (`Runtime/Manager/Popup/`)
- `PopupManager` singleton manages popup lifecycle via UniRx ReactiveCollections
- Four popup categories: `popupSystem`, `popupUI`, `popupIgnore`, `popupQueue`
- Resources loaded from `Assets/Resources/_UI/Popup/` path
- Extend `PopupType` enum to add new popup types
- `PopupBase` abstract class with `PopupAnimation` for DOTween animations

**UI Components** (`Runtime/UI/`)
- `UIButton` - Extended Button with DOTween scale animations and multiple event callbacks (onClick, onDown, onUp, onEnter, onExit)
- `UICanvasView` - Base class for canvas-based views with visibility/raycaster control

### Editor Tools (`Editor/`)
- `EditorTools` - Raycast target optimizer for UI hierarchy
- `FavoritesPanel`, `PlayModeStartScene` - Development workflow utilities

## Code Conventions

- Private fields: underscore prefix (`_fieldName`)
- Public methods/properties: PascalCase
- Comments: Mixed Korean/English (Korean primarily in PopupManager/FSM)
- Resource paths: Convention-based (e.g., `_UI/Popup/{PopupType}`)

## Key Integration Patterns

**Creating Popups**:
```csharp
PopupManager.Instance.Init(); // Call once at startup
PopupManager.Instance.Create(PopupType.MyPopup);
```

**Custom Popup**: Inherit from `PopupBase`, place prefab at `Resources/_UI/Popup/`, add to `PopupType` enum

**Using FSM**:
```csharp
// Enum-based
var fsm = new FsmClass<MyStateEnum>();
fsm.AddState(MyStateEnum.Idle, new IdleState());
fsm.ChangeState(MyStateEnum.Idle);

// Character-based
public class PlayerFSM : CharacterFSM<PlayerController> { }
```
