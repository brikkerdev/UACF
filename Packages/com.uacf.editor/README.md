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
  "token": "auto-generated",
  "allowExecute": true,
  "logRequests": true,
  "logFile": "Logs/UACF/session.log"
}
```

**Edit > Project Settings > UACF** (ScriptableSingleton fallback):

- Port, Auto Start, Log Requests, Request Timeout, Compile Timeout, Enable Batch Endpoint

## Security

- Server listens only on localhost (127.0.0.1)
- Optional Bearer token (set in config.json)
- All operations support Undo
