# UACF - Unity Autonomous Control Framework

HTTP API server in Unity Editor for AI agents (Cursor, Claude Code) to control Unity projects via `curl` without manual interaction.

**UACF v1.1** uses a unified action-based API. All requests go to `POST /uacf`.

## Requirements

- Unity 6.3+
- No external dependencies (uses Newtonsoft.Json from Unity's collab-proxy package)

## Installation

Add to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.uacf.editor": "file:com.uacf.editor"
  }
}
```

## Quick Start

1. Open Unity Editor - the UACF server starts automatically on port 7890
2. List actions: `curl -X POST http://localhost:7890/uacf -H "Content-Type: application/json" -d '{"action":"api.list"}'`
3. Configure: `ProjectSettings/UACF/config.json` or Edit > Project Settings > UACF

## API Format

All requests: **POST** to `/uacf` with JSON:

```json
{
  "action": "action.name",
  "params": { /* optional parameters */ }
}
```

## API Examples

### List all actions
```bash
curl -X POST http://localhost:7890/uacf \
  -H "Content-Type: application/json" \
  -d '{"action":"api.list"}'
```

### Get scene hierarchy
```bash
curl -X POST http://localhost:7890/uacf \
  -H "Content-Type: application/json" \
  -d '{"action":"scene.hierarchy.get","params":{"depth":2,"components":true}}'
```

### Create GameObject with components
```bash
curl -X POST http://localhost:7890/uacf \
  -H "Content-Type: application/json" \
  -d '{
    "action": "scene.object.create",
    "params": {
      "name": "Player",
      "tag": "Player",
      "components": [
        {"type": "Rigidbody", "properties": {"mass": 1.0}},
        {"type": "BoxCollider", "properties": {"size": [1,1,1]}}
      ]
    }
  }'
```

### Write file
```bash
curl -X POST http://localhost:7890/uacf \
  -H "Content-Type: application/json" \
  -d '{
    "action": "asset.file.write",
    "params": {
      "path": "Assets/Scripts/PlayerController.cs",
      "content": "using UnityEngine;\n\npublic class PlayerController : MonoBehaviour { }"
    }
  }'
```

### Create ScriptableObject asset
```bash
curl -X POST http://localhost:7890/uacf \
  -H "Content-Type: application/json" \
  -d '{
    "action": "asset.create.scriptableObject",
    "params": {
      "path": "Assets/Data/EnemyConfig.asset",
      "type": "EnemyConfig",
      "properties": { "health": 100, "speed": 3.5 },
      "overwrite": true
    }
  }'
```

### Save scene
```bash
curl -X POST http://localhost:7890/uacf \
  -H "Content-Type: application/json" \
  -d '{"action":"scene.save"}'
```

### Enter Play Mode
```bash
curl -X POST http://localhost:7890/uacf \
  -H "Content-Type: application/json" \
  -d '{"action":"editor.play"}'
```

## Key Actions

| Action | Description |
|--------|-------------|
| api.list | List all actions with docs |
| api.help | Help for specific action |
| api.prompt | System prompt for agent |
| scene.hierarchy.get | Scene hierarchy |
| scene.object.create | Create GameObject |
| scene.object.find | Find GameObjects |
| scene.save | Save scene |
| component.add | Add component |
| component.set | Set component properties |
| asset.file.write | Write file |
| asset.file.read | Read file |
| asset.refresh | Refresh AssetDatabase |
| asset.create.panelSettings | Create UI Toolkit PanelSettings |
| asset.create.scriptableObject | Create ScriptableObject asset |
| asset.create.material | Create material asset |
| asset.create.physicMaterial | Create PhysicMaterial asset |
| asset.create.animationClip | Create AnimationClip asset |
| editor.compilationStatus | Compilation status |
| editor.play | Play Mode |
| editor.stop | Stop Play Mode |
| batch | Batch operations |

## Configuration

**ProjectSettings/UACF/config.json** (created on first run):

```json
{
  "port": 7890,
  "host": "127.0.0.1",
  "allowExecute": true,
  "logRequests": true,
  "logFile": "Logs/UACF/session.log"
}
```

**Edit > Project Settings > UACF** (ScriptableSingleton fallback):

- Port, Auto Start, Log Requests, Request Timeout, Compile Timeout, Enable Batch Endpoint

## Asset creation without Roslyn

For asset authoring, prefer dedicated actions:

- `asset.create.panelSettings`
- `asset.create.scriptableObject`
- `asset.create.material`
- `asset.create.physicMaterial`
- `asset.create.animationClip`

This path is stable in Unity Editor and does not depend on dynamic C# execution.

## Optional: Roslyn execute support

The `execute` action (arbitrary C# execution) works only when **Roslyn scripting assemblies** are available. Unity’s built-in `DotNetSdkRoslyn` is not compatible (Invalid data directory / NotImplementedException). To add a working Roslyn in the Editor:

1. **Install NuGetForUnity** (e.g. from [GitHub](https://github.com/GlitchEnzo/NuGetForUnity) or Asset Store / OpenUPM).
2. **Install these packages** (NuGetForUnity → Manage NuGet Packages):
   - `Microsoft.CodeAnalysis.CSharp.Scripting` — pick a **.NET Standard 2.0** build (e.g. 4.0.0 or 4.3.0 if available for netstandard2.0).
   - `System.Runtime.Loader` — try **4.0.0** first (some Unity setups need this exact version to avoid `AssemblyLoadContext.LoadFromStream` NotImplementedException).
3. **Reference the DLLs** in `Packages/com.uacf.editor/Editor/com.uacf.editor.asmdef`: add to `precompiledReferences` the Roslyn DLLs that NuGetForUnity placed in your project (e.g. under `Packages` or `Assets/Plugins`), so the Editor assembly can see them. Avoid referencing Unity’s own `DotNetSdkRoslyn` path.
4. **Restart Unity** and call `execute` again (e.g. `{"action":"execute","params":{"code":"1+1"}}`). If you still get `NOT_AVAILABLE`, check the Unity Console for the exact exception; it may be necessary to try an older `Microsoft.CodeAnalysis.CSharp.Scripting` (e.g. 3.11.0) that matches the loaded `System.Runtime.Loader`.

**Alternative without Roslyn:** use `execute.method` to call static methods in your project (e.g. a helper that creates `ScriptableObject.CreateInstance<T>()` and saves assets) so you don’t need dynamic C# execution for creating PanelSettings and similar assets.

## Security

- Server listens only on localhost (127.0.0.1)
- All operations support Undo
