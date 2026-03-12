# UACF - Unity Autonomous Control Framework

This Unity project has UACF v1.1 enabled. When the Unity Editor is running, an HTTP API server is available at **http://localhost:7890** (or configured port).

## Workflow for AI Agents

**Before scene actions**: Call `editor.compilationStatus` first. If `isCompiling: true`, do not call scene actions — they will return `SERVER_BUSY`. On `SERVER_BUSY`, retry after a few seconds.

1. **List actions**: `curl -X POST http://127.0.0.1:7890/uacf -H "Content-Type: application/json" -d '{"action":"api.list"}'`
2. **Get help**: `curl -X POST http://127.0.0.1:7890/uacf -H "Content-Type: application/json" -d '{"action":"api.help","params":{"action":"scene.object.create"}}'`
3. **Create/edit C# files**: `{"action":"asset.file.write","params":{"path":"Assets/Scripts/Player.cs","content":"..."}}`
4. **Refresh assets**: `{"action":"asset.refresh"}`
5. **Get hierarchy**: `{"action":"scene.hierarchy.get","params":{"depth":2,"components":true}}`
6. **Create GameObject**: `{"action":"scene.object.create","params":{"name":"Player","tag":"Player","components":[{"type":"Rigidbody","properties":{"mass":1}}]}}`
7. **Save scene**: `{"action":"scene.save"}`
8. **Play**: `{"action":"editor.play"}`

## Request Format

All requests: **POST** to `/uacf` with JSON body:

```json
{
  "action": "scene.hierarchy.get",
  "params": { "depth": 2, "components": true }
}
```

## Response Format

Success:
```json
{ "ok": true, "data": { ... }, "duration": 0.034 }
```

Error:
```json
{ "ok": false, "error": { "code": "OBJECT_NOT_FOUND", "message": "...", "suggestion": "..." }, "duration": 0.002 }
```

## Key Actions

| Action | Description |
|--------|-------------|
| api.list | List all available actions |
| api.help | Get help for specific action |
| api.prompt | Get system prompt for agent |
| scene.hierarchy.get | Get scene hierarchy |
| scene.object.create | Create GameObject |
| scene.object.find | Find GameObjects |
| scene.save | Save scene |
| component.add | Add component |
| component.set | Set component properties |
| asset.file.write | Write file |
| asset.file.read | Read file |
| asset.refresh | Refresh AssetDatabase |
| editor.compilationStatus | Get compile status |
| editor.play | Enter Play Mode |
| editor.stop | Exit Play Mode |
| batch | Execute batch operations |

## Batch Operations

```json
{
  "action": "batch",
  "params": {
    "operations": [
      {"action": "scene.object.create", "params": {"name": "EnemySquad"}},
      {"action": "prefab.instantiate", "params": {"path": "Assets/Prefabs/Enemy.prefab", "parent": "EnemySquad", "name": "Enemy_01"}}
    ],
    "undoGroup": "Create Squad",
    "stopOnError": true
  }
}
```

## Configuration

- Config file: `ProjectSettings/UACF/config.json`
- Port: 7890 (default)

## Important Notes

- All Unity API calls run on the main thread - requests may queue
- In Play Mode, scene modification actions return CONFLICT
- During compilation, some requests may return SERVER_BUSY
- Component types: "Rigidbody", "BoxCollider", "PlayerController" (script name)
