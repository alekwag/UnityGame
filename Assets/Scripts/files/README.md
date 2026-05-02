# Scanner Gun System — Setup Guide

## File Overview

```
ScannerGunSystem/
├── Scripts/
│   ├── VentState.cs                  — Enums (VentState, ScannableType)
│   ├── IScannableTarget.cs           — Interface for all scannable objects
│   ├── VentController.cs             — Vent behaviour, state machine, NavMesh, highlight
│   ├── ScannerCamera.cs              — Scanner cam → RenderTexture, raycast fire
│   ├── ScannerGun.cs                 — Player input, cooldown, HUD label
│   └── HighlightVisibilityController.cs — Hides highlights from main camera
├── Shaders/
│   ├── ScannerHighlight.shader       — ZTest Always overlay (through-wall highlight)
│   └── ScannerScreen.shader          — Unlit scanline shader for the gun screen quad
└── README.md                         — This file
```

---

## Step-by-Step Setup

### 1. Layers

In **Edit → Project Settings → Tags and Layers**, add:

| Slot | Name |
|------|------|
| 8    | `Vent` |

### 2. RenderTexture

1. **Assets → Create → Render Texture**
2. Name it `ScannerRT`
3. Settings: **512 × 512**, Depth Buffer: **24 bit**, Color Format: **R8G8B8A24**

### 3. Shaders & Materials

| Material name       | Shader                         | Notes |
|---------------------|--------------------------------|-------|
| `M_VentHighlight`   | `ScannerSystem/VentHighlight`  | Leave defaults; VentController instances it per vent |
| `M_ScannerScreen`   | `ScannerSystem/ScannerScreen`  | Set `_MainTex` = `ScannerRT` |

### 4. Vent GameObjects

For each vent:

1. Set **Layer** to `Vent`
2. Add a **Collider** (Box or Mesh)
3. Add **VentController** component
   - `ventRenderers` → assign all MeshRenderer children
   - `highlightMaterial` → `M_VentHighlight`
   - `navMeshObstacle` → assign the NavMeshObstacle component (or leave blank)
   - `initialState` → Open / Closed / Locked
4. If you want NavMesh blocking, also add **NavMeshObstacle** to the vent:
   - Shape: Box, size matching collider
   - **Carving** will be toggled automatically by `VentController`

### 5. Scanner Gun Hierarchy

```
ScannerGunRoot              ← ScannerGun.cs here
  ├── GunModel               ← your mesh
  ├── ScreenQuad             ← plane mesh, M_ScannerScreen material
  └── ScannerCameraObj       ← Camera component + ScannerCamera.cs
```

**ScannerCameraObj Camera settings:**
- Clear Flags: Skybox (or match main cam)
- Culling Mask: Everything
- Depth: **−2**
- Target Texture: `ScannerRT`

**ScannerCamera.cs settings:**
- `renderTexture` → `ScannerRT`
- `mainCameraTransform` → Main Camera transform

**ScannerGun.cs settings:**
- `scannerCamera` → ScannerCamera component
- `screenQuadRenderer` → ScreenQuad MeshRenderer
- `hudLabel` → (optional) TextMeshPro UI element
- `fireButton` → `"Fire1"` (or leave empty and call `Fire()` from your Input System)

### 6. Main Camera

Add **HighlightVisibilityController.cs** to your **Main Camera** GameObject.

- `ventLayerName` → `"Vent"`
- Call `RefreshVentList()` whenever you spawn/destroy vents at runtime.

---

## How the Highlight-Only-In-Scanner-Cam Works

```
Frame render order:
  1. Main Camera (depth = -1) → OnPreRender fires:
       HighlightVisibilityController sets _Alpha=0 on all overlay materials
     → Scene renders: vents look normal, no highlight
     → OnPostRender fires: _Alpha restored to -1 (use material colour)

  2. Scanner Camera (depth = -2) → No callbacks → overlays are visible
       ZTest Always → highlights draw through walls
     → Output goes to ScannerRT

  3. Screen Quad (on gun) samples ScannerRT → shows scanner view with highlights
```

No stencil buffer. No extra layers. Works with any render pipeline (Built-in).

---

## Vent States

| State  | Highlight colour | NavMeshObstacle | Notes |
|--------|-----------------|-----------------|-------|
| Open   | Green           | Disabled        | AI can path through |
| Closed | Red             | Enabled+Carving | AI blocked |
| Locked | Grey flicker    | Enabled+Carving | Cannot be toggled by scanner |

---

## Expanding the System

To add a new scannable type (e.g. doors, enemies):

1. Create a new script that implements **`IScannableTarget`**:
   ```csharp
   public class DoorController : MonoBehaviour, IScannableTarget
   {
       public void OnScanned()      { /* toggle door */ }
       public Renderer[] GetHighlightRenderers() { return doorRenderers; }
       public string GetScanLabel() { return "DOOR [LOCKED]"; }
   }
   ```
2. Add `M_VentHighlight` (or a new colour variant) as the last material on its renderers.
3. `HighlightVisibilityController.RefreshVentList()` — rename to `RefreshAll()` if needed, and use `FindObjectsByType<IScannableTarget>()` instead.

No changes needed to `ScannerCamera` or `ScannerGun`.

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Highlights visible in main camera | Check `HighlightVisibilityController` is on the **Main Camera** object |
| Highlights not visible in scanner | Make sure scanner cam `depth = -2` (renders before main cam callbacks run) |
| Vent not hit by raycast | Ensure vent has a Collider and is not on an ignored layer |
| NavMesh not updating | Confirm NavMeshObstacle has `Carving` checked in inspector (script enables it) |
| Grey flicker too fast/slow | Adjust `flickerFrequency` on VentController |
| Screen quad black | Confirm `ScannerRT` is assigned to both the camera's `targetTexture` and the screen quad's material `_MainTex` |
