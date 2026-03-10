# UACF - Unity Autonomous Control Framework

This Unity project has UACF enabled. When the Unity Editor is running, an HTTP API server is available at **http://localhost:7890**.

## Workflow for AI Agents

1. **Check status**: `curl http://localhost:7890/api/status`
2. **Create/edit C# files** in Assets/ (via file system or `POST /api/file/write`)
3. **Compile**: `curl -X POST http://localhost:7890/api/compile/request -H "Content-Type: application/json" -d '{"wait":true}'`
4. **Check errors**: `curl http://localhost:7890/api/compile/errors?severity=error`
5. **Create GameObjects**: Use `POST /api/gameobject/create` with components
6. **Set component fields**: Use `PUT /api/component/set-fields` for references (e.g. groundCheck)
7. **Save scene**: `curl -X POST http://localhost:7890/api/scene/save`
8. **Play**: `curl -X POST http://localhost:7890/api/editor/play`

## Key Endpoints

| Action | Endpoint |
|--------|----------|
| Status | GET /api/status |
| Compile & wait | POST /api/compile/request (body: `{"wait":true}`) |
| Create GameObject | POST /api/gameobject/create |
| Add component | POST /api/component/add |
| Set fields | PUT /api/component/set-fields |
| Hierarchy | GET /api/scene/hierarchy?include_components=true |
| Save scene | POST /api/scene/save |
| Write file | POST /api/file/write |

## Batch Operations

Use `POST /api/batch/execute` to run multiple operations atomically:

```json
{
  "operations": [
    {"id": "1", "endpoint": "POST /api/gameobject/create", "body": {"name": "Player", "tag": "Player"}},
    {"id": "2", "endpoint": "POST /api/component/add", "body": {"target": {"name": "Player"}, "component_type": "Rigidbody", "fields": {"mass": 1}}}
  ],
  "stop_on_error": true
}
```

## Important Notes

- All Unity API calls run on the main thread - requests may queue
- In Play Mode, scene modification endpoints return 409 CONFLICT
- During compilation, some endpoints return 503 SERVER_BUSY
- Component type names: use "Rigidbody", "BoxCollider", "PlayerController" (script name)
- For object references in fields: `{"reference": {"name": "OtherObject"}}`
