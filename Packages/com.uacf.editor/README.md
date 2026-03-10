# UACF - Unity Autonomous Control Framework

HTTP API server in Unity Editor for AI agents (Cursor, Claude Code) to control Unity projects via `curl` without manual interaction.

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
2. Check status: `curl http://localhost:7890/api/status`
3. Configure: Edit > Project Settings > UACF

## API Examples

### Status
```bash
curl http://localhost:7890/api/status
```

### Ping
```bash
curl http://127.0.0.1:7890/api/ping
```

### Compile (wait for completion)
```bash
curl -X POST http://localhost:7890/api/compile/request \
  -H "Content-Type: application/json" \
  -d '{"wait":true,"timeout_seconds":60}'
```

### Create GameObject with components
```bash
curl -X POST http://localhost:7890/api/gameobject/create \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Player",
    "tag": "Player",
    "components": [
      {"type": "Rigidbody", "fields": {"mass": 1.0}},
      {"type": "BoxCollider", "fields": {"size": {"x":1,"y":1,"z":1}}}
    ]
  }'
```

### Save scene
```bash
curl -X POST http://localhost:7890/api/scene/save
```

### Write file and compile
```bash
curl -X POST http://localhost:7890/api/file/write \
  -H "Content-Type: application/json" \
  -d '{
    "path": "Assets/Scripts/PlayerController.cs",
    "content": "using UnityEngine;\n\npublic class PlayerController : MonoBehaviour { }",
    "auto_refresh": true,
    "wait_compile": true
  }'
```

## Endpoints Overview

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/ping | Health check (no main thread) |
| GET | /api/status | Server status |
| POST | /api/assets/refresh | Refresh AssetDatabase |
| GET | /api/assets/find | Find assets |
| POST | /api/compile/request | Trigger compilation |
| GET | /api/compile/status | Compilation status |
| GET | /api/compile/errors | Get compile errors |
| POST | /api/file/write | Write file |
| GET | /api/file/read | Read file |
| GET | /api/scene/list | List scenes |
| POST | /api/scene/open | Open scene |
| POST | /api/scene/save | Save scene |
| GET | /api/scene/hierarchy | Get hierarchy |
| POST | /api/gameobject/create | Create GameObject |
| GET | /api/gameobject/find | Find GameObjects |
| POST | /api/component/add | Add component |
| PUT | /api/component/set-fields | Set component fields |
| POST | /api/prefab/create | Create prefab |
| POST | /api/prefab/instantiate | Instantiate prefab |
| POST | /api/batch/execute | Execute batch operations |
| POST | /api/editor/play | Start Play Mode |
| POST | /api/editor/stop | Stop Play Mode |

## Configuration

Edit > Project Settings > UACF

- **Port**: HTTP server port (default: 7890)
- **Auto Start**: Start server when Editor loads
- **Log Requests**: Log each request to Console
- **Request Timeout**: Request timeout in seconds
- **Compile Timeout**: Max wait for compilation
- **Enable Batch Endpoint**: Allow /api/batch/execute

## Security

- Server listens only on localhost (127.0.0.1)
- No external network access by default
- All operations support Undo
