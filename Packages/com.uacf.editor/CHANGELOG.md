# Changelog

## [1.0.0] - 2025-03-10

### Added

- HTTP API server in Unity Editor (localhost)
- GET /api/ping - lightweight health check (no main thread)
- GET /api/status - server status
- POST /api/assets/refresh - AssetDatabase refresh
- GET /api/assets/find - find assets
- POST /api/assets/create-folder - create folder
- DELETE /api/assets/delete - delete asset
- POST /api/compile/request - trigger compilation
- GET /api/compile/status - compilation status
- GET /api/compile/errors - get compile errors
- POST /api/file/write - write file with optional compile
- GET /api/file/read - read file
- GET /api/scene/list - list loaded scenes
- POST /api/scene/open - open scene
- POST /api/scene/save - save scene
- POST /api/scene/new - create new scene
- GET /api/scene/hierarchy - get scene hierarchy
- POST /api/gameobject/create - create GameObject with components
- GET /api/gameobject/find - find GameObjects
- PUT /api/gameobject/modify - modify GameObject
- DELETE /api/gameobject/destroy - destroy GameObject
- POST /api/gameobject/set-parent - set parent
- POST /api/gameobject/duplicate - duplicate GameObject
- POST /api/component/add - add component
- GET /api/component/get - get component fields
- PUT /api/component/set-fields - set component fields
- DELETE /api/component/remove - remove component
- GET /api/component/list-types - list component types
- POST /api/prefab/create - create prefab
- POST /api/prefab/instantiate - instantiate prefab
- PUT /api/prefab/modify - modify prefab
- POST /api/prefab/apply-overrides - apply overrides
- POST /api/batch/execute - batch operations
- POST /api/editor/play - start Play Mode
- POST /api/editor/stop - stop Play Mode
- POST /api/editor/pause - pause/resume
- GET /api/project/settings - project settings
- POST /api/project/add-tag - add tag
- POST /api/project/set-layer - set layer
- UACFSettings (Edit > Project Settings > UACF)
- UACF Editor Window (Window > UACF > Status)
- MainThreadDispatcher for Unity API safety
- Domain reload handling
- Play Mode conflict detection
