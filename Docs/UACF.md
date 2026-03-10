# Техническое задание: Unity Autonomous Control Framework (UACF)

## 1. Общее описание

### 1.1 Назначение
UPM-пакет для Unity 6.3, предоставляющий встроенный HTTP API сервер в Unity Editor. Позволяет внешним AI-агентам (Cursor, Claude Code) полностью управлять проектом Unity через терминальные команды (`curl`), без ручного взаимодействия с редактором.

### 1.2 Целевой сценарий использования
```
AI-агент (Cursor / Claude Code)
    │
    ├── Создаёт/редактирует .cs файлы напрямую в Assets/
    │
    ├── curl → POST /api/assets/refresh
    ├── curl → GET  /api/compile/status
    ├── curl → GET  /api/compile/errors
    ├── curl → POST /api/scene/add-object
    ├── curl → POST /api/scene/attach-component
    ├── curl → POST /api/scene/set-field
    ├── curl → POST /api/scene/save
    │
    └── Человек нажимает Play
```

### 1.3 Ключевые принципы
- **Zero human interaction** — все операции через API, никаких диалоговых окон
- **Синхронный ответ** — каждый запрос возвращает результат выполнения
- **Main thread safety** — все Unity API вызовы диспатчатся в основной поток
- **Идемпотентность** — повторный вызов с теми же параметрами не ломает состояние
- **Детальные ошибки** — каждый ответ содержит достаточно информации для агента

---

## 2. Архитектура

### 2.1 Структура пакета (UPM)

```
com.uacf.editor/
├── package.json
├── README.md
├── CHANGELOG.md
├── Editor/
│   ├── com.uacf.editor.asmdef
│   │
│   ├── Core/
│   │   ├── UACFServer.cs              # HTTP-сервер (HttpListener)
│   │   ├── UACFBootstrap.cs           # [InitializeOnLoad] автозапуск
│   │   ├── MainThreadDispatcher.cs    # Диспатчер в основной поток
│   │   ├── RequestRouter.cs           # Маршрутизация URL → Handler
│   │   ├── RequestContext.cs          # Обёртка над HttpListenerContext
│   │   └── ResponseHelper.cs         # Формирование JSON-ответов
│   │
│   ├── Handlers/
│   │   ├── AssetsHandler.cs           # /api/assets/*
│   │   ├── CompileHandler.cs          # /api/compile/*
│   │   ├── SceneHandler.cs            # /api/scene/*
│   │   ├── PrefabHandler.cs           # /api/prefab/*
│   │   ├── GameObjectHandler.cs       # /api/gameobject/*
│   │   ├── ComponentHandler.cs        # /api/component/*
│   │   ├── ProjectHandler.cs          # /api/project/*
│   │   └── FileHandler.cs            # /api/file/*
│   │
│   ├── Services/
│   │   ├── CompilationService.cs      # Обёртка над CompilationPipeline
│   │   ├── SceneService.cs            # Обёртка над EditorSceneManager
│   │   ├── PrefabService.cs           # Обёртка над PrefabUtility
│   │   ├── GameObjectService.cs       # Создание/поиск/удаление объектов
│   │   ├── ComponentService.cs        # Добавление компонентов, установка полей
│   │   ├── AssetDatabaseService.cs    # Обёртка над AssetDatabase
│   │   ├── SerializationService.cs    # Сериализация состояния сцены в JSON
│   │   └── TypeResolverService.cs     # Поиск типов по имени строки
│   │
│   ├── Models/
│   │   ├── ApiResponse.cs             # Базовая модель ответа
│   │   ├── CompileError.cs            # Модель ошибки компиляции
│   │   ├── GameObjectInfo.cs          # Модель описания GameObject
│   │   ├── ComponentInfo.cs           # Модель описания компонента
│   │   ├── FieldInfo.cs               # Модель описания поля
│   │   └── SceneInfo.cs               # Модель описания сцены
│   │
│   ├── Config/
│   │   └── UACFSettings.cs            # ScriptableSingleton настройки
│   │
│   └── UI/
│       └── UACFEditorWindow.cs        # Окно статуса сервера (опционально)
│
└── Tests/
    └── Editor/
        ├── com.uacf.editor.tests.asmdef
        ├── ServerTests.cs
        ├── SceneHandlerTests.cs
        └── CompileHandlerTests.cs
```

### 2.2 Диаграмма компонентов

```
┌────────────────────────────────────────────────────────────┐
│                      Unity Editor Process                   │
│                                                            │
│  ┌──────────────┐    ┌─────────────────┐                  │
│  │ UACFBootstrap │───►│   UACFServer    │                  │
│  │ [InitOnLoad]  │    │  (HttpListener) │                  │
│  └──────────────┘    │  port: 7890     │                  │
│                      └────────┬────────┘                  │
│                               │                            │
│                      ┌────────▼────────┐                  │
│                      │  RequestRouter   │                  │
│                      └────────┬────────┘                  │
│                               │                            │
│          ┌────────────────────┼────────────────────┐      │
│          ▼                    ▼                    ▼      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │AssetsHandler  │  │CompileHandler│  │ SceneHandler  │   │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘   │
│         │                  │                  │           │
│         ▼                  ▼                  ▼           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │AssetDBService│  │CompileService│  │ SceneService  │   │
│  └──────────────┘  └──────────────┘  └──────────────┘   │
│         │                  │                  │           │
│         └──────────────────┼──────────────────┘           │
│                            ▼                               │
│                  ┌──────────────────┐                      │
│                  │MainThreadDispatch│                      │
│                  │  (EditorApp.     │                      │
│                  │   update loop)   │                      │
│                  └──────────────────┘                      │
└────────────────────────────────────────────────────────────┘
         ▲
         │ HTTP (localhost:7890)
         │
┌────────┴───────┐
│  curl / Agent   │
└────────────────┘
```

---

## 3. Компонент: Core

### 3.1 UACFBootstrap.cs

```
[InitializeOnLoad] статический класс
- При загрузке Editor автоматически запускает UACFServer
- Подписывается на EditorApplication.quitting для остановки сервера
- Подписывается на AssemblyReloadEvents для корректной перезагрузки
- Подписывается на CompilationPipeline.compilationFinished
- Проверяет UACFSettings для определения порта и auto-start флага
```

**Жизненный цикл:**
```
Editor Start
    → [InitializeOnLoad] UACFBootstrap.Initialize()
        → UACFServer.Start(port)

Domain Reload (после компиляции)
    → AssemblyReloadEvents.beforeAssemblyReload
        → UACFServer.Stop()
    → AssemblyReloadEvents.afterAssemblyReload
        → UACFServer.Start(port)

Editor Quit
    → EditorApplication.quitting
        → UACFServer.Stop()
```

### 3.2 UACFServer.cs

```
Класс, управляющий HttpListener.

Поля:
- HttpListener _listener
- Thread _listenerThread
- CancellationTokenSource _cts
- bool _isRunning
- int _port (default: 7890)
- RequestRouter _router

Методы:
- Start(int port) — запуск listener на http://localhost:{port}/
- Stop() — graceful shutdown
- ListenLoop() — цикл приёма запросов в фоновом потоке
- HandleRequest(HttpListenerContext) — передача в RequestRouter

Особенности:
- Listener работает в background thread
- Каждый запрос обрабатывается через ThreadPool
- Если нужен доступ к Unity API — через MainThreadDispatcher
- Таймаут обработки запроса: 30 секунд (для долгих операций типа компиляции)
- При ошибке порта — попытка port+1, port+2 (до 3 попыток)
```

### 3.3 MainThreadDispatcher.cs

```
Статический класс для выполнения действий в основном потоке Unity Editor.

Механизм:
- ConcurrentQueue<Action<TaskCompletionSource<object>>> _queue
- Подписка на EditorApplication.update
- В update — dequeue и выполнение

Методы:
- Task<T> Enqueue<T>(Func<T> action)
    Ставит действие в очередь, возвращает Task
    Вызывающий поток (HTTP thread) ждёт результата через await

- Task Enqueue(Action action)
    Версия без возвращаемого значения

Пример потока выполнения:
    HTTP Thread                          Main Thread
        │                                    │
        ├─ Enqueue(() => {                   │
        │    return SceneService.Add(...)     │
        │  })                                │
        │                                    │
        ├─ await task ◄──────────────────── EditorApplication.update
        │                                    ├─ dequeue action
        │                                    ├─ execute
        │                                    ├─ set task result
        │                                    │
        ├─ формируем response                │
        ▼                                    ▼

Обработка исключений:
- Если action бросает исключение → оно пробрасывается через TaskCompletionSource
- HTTP handler ловит его и возвращает 500 с деталями ошибки
```

### 3.4 RequestRouter.cs

```
Маршрутизатор HTTP-запросов к обработчикам.

Регистрация маршрутов:
- Dictionary<(string method, string pathPattern), Func<RequestContext, Task>> _routes

Методы:
- Register(string method, string pattern, Func<RequestContext, Task> handler)
- Route(HttpListenerContext context) — находит обработчик, создаёт RequestContext

Маршрутизация:
- Точное совпадение: /api/compile/errors
- С параметрами: /api/scene/objects/{id}
    (параметры извлекаются в RequestContext.PathParams)

Если маршрут не найден → 404 с описанием доступных endpoint-ов
```

### 3.5 RequestContext.cs

```
Обёртка над HttpListenerContext.

Свойства:
- string Method (GET/POST/PUT/DELETE)
- string Path
- Dictionary<string, string> PathParams
- Dictionary<string, string> QueryParams

Методы:
- Task<T> ReadBodyAsync<T>() — десериализация JSON body
- Task<string> ReadBodyRawAsync() — raw string body
- void Respond(int statusCode, object body) — JSON ответ
- void RespondOk(object data)
- void RespondError(int code, string message, object details = null)
```

### 3.6 ResponseHelper.cs / ApiResponse.cs

```
Стандартный формат ответа:

{
    "success": true|false,
    "data": { ... },           // при success=true
    "error": {                 // при success=false
        "code": "COMPILE_ERROR",
        "message": "...",
        "details": { ... }
    },
    "timestamp": "2025-01-15T12:00:00Z",
    "duration_ms": 150
}

Коды ошибок (enum):
- INVALID_REQUEST — неверные параметры
- NOT_FOUND — объект/компонент/сцена не найдены
- COMPILE_ERROR — ошибки компиляции
- TYPE_NOT_FOUND — тип компонента не найден
- FIELD_NOT_FOUND — поле не найдено в компоненте
- SCENE_NOT_LOADED — сцена не загружена
- INTERNAL_ERROR — необработанная ошибка
- SERVER_BUSY — сервер занят (компиляция в процессе)
```

---

## 4. API Endpoints

### 4.1 Здоровье и статус

#### `GET /api/ping`

Лёгкая проверка доступности сервера. Не использует Unity API и main thread — отвечает всегда, даже когда main thread заблокирован (компиляция, модальное окно). Используйте при зависании `/api/status`.

**Response:**
```json
{
    "success": true,
    "data": {
        "ok": true,
        "uptime_seconds": 3600
    }
}
```

**curl:**
```bash
curl http://localhost:7890/api/ping
```

---

#### `GET /api/status`

Проверка работоспособности сервера.

**Response:**
```json
{
    "success": true,
    "data": {
        "server_version": "1.0.0",
        "unity_version": "6000.3.0f1",
        "project_name": "MyProject",
        "project_path": "/Users/dev/MyProject",
        "is_compiling": false,
        "is_playing": false,
        "active_scene": "Assets/Scenes/SampleScene.unity",
        "loaded_scenes": ["Assets/Scenes/SampleScene.unity"],
        "uptime_seconds": 3600
    }
}
```

**curl:**
```bash
curl http://localhost:7890/api/status
```

---

### 4.2 Управление ассетами

#### `POST /api/assets/refresh`

Принудительный вызов `AssetDatabase.Refresh()`.

**Request body (опционально):**
```json
{
    "import_options": "ForceUpdate"
}
```

**Response:**
```json
{
    "success": true,
    "data": {
        "refreshed": true,
        "duration_ms": 1200
    }
}
```

**curl:**
```bash
curl -X POST http://localhost:7890/api/assets/refresh
```

---

#### `GET /api/assets/find`

Поиск ассетов по фильтру.

**Query params:**
- `filter` — строка фильтра AssetDatabase (например `t:Script PlayerController`)
- `path` — ограничение папки (например `Assets/Scripts`)

**Response:**
```json
{
    "success": true,
    "data": {
        "assets": [
            {
                "guid": "abc123...",
                "path": "Assets/Scripts/PlayerController.cs",
                "type": "MonoScript"
            }
        ],
        "count": 1
    }
}
```

**curl:**
```bash
curl "http://localhost:7890/api/assets/find?filter=t:Script&path=Assets/Scripts"
```

---

#### `POST /api/assets/create-folder`

Создание папки в проекте.

**Request body:**
```json
{
    "path": "Assets/Scripts/Player"
}
```

**curl:**
```bash
curl -X POST http://localhost:7890/api/assets/create-folder \
  -H "Content-Type: application/json" \
  -d '{"path":"Assets/Scripts/Player"}'
```

---

#### `DELETE /api/assets/delete`

Удаление ассета.

**Request body:**
```json
{
    "path": "Assets/Scripts/OldScript.cs"
}
```

---

### 4.3 Компиляция

#### `POST /api/compile/request`

Инициирует компиляцию (вызывает `AssetDatabase.Refresh()` + ожидание завершения компиляции).

**Request body (опционально):**
```json
{
    "wait": true,
    "timeout_seconds": 60
}
```

**Поведение при `wait: true`:**
1. Вызывает `AssetDatabase.Refresh()`
2. Ожидает `EditorApplication.isCompiling == false`
3. Собирает ошибки через `CompilationPipeline`
4. Возвращает результат

**Response (успех):**
```json
{
    "success": true,
    "data": {
        "compiled": true,
        "has_errors": false,
        "error_count": 0,
        "warning_count": 2,
        "warnings": [
            {
                "message": "Variable 'x' is assigned but never used",
                "file": "Assets/Scripts/Test.cs",
                "line": 15,
                "column": 9,
                "severity": "warning",
                "id": "CS0219"
            }
        ],
        "duration_ms": 3500
    }
}
```

**Response (ошибки компиляции):**
```json
{
    "success": true,
    "data": {
        "compiled": true,
        "has_errors": true,
        "error_count": 2,
        "warning_count": 0,
        "errors": [
            {
                "message": "The type or namespace name 'Rigidbody2D' could not be found",
                "file": "Assets/Scripts/PlayerController.cs",
                "line": 8,
                "column": 12,
                "severity": "error",
                "id": "CS0246"
            },
            {
                "message": "; expected",
                "file": "Assets/Scripts/PlayerController.cs",
                "line": 22,
                "column": 1,
                "severity": "error",
                "id": "CS1002"
            }
        ],
        "duration_ms": 2100
    }
}
```

**curl:**
```bash
curl -X POST http://localhost:7890/api/compile/request \
  -H "Content-Type: application/json" \
  -d '{"wait":true,"timeout_seconds":60}'
```

---

#### `GET /api/compile/status`

Текущий статус компиляции (не инициирует новую).

**Response:**
```json
{
    "success": true,
    "data": {
        "is_compiling": false,
        "last_compile_success": true,
        "last_compile_time": "2025-01-15T12:00:00Z",
        "last_error_count": 0,
        "last_warning_count": 2
    }
}
```

**curl:**
```bash
curl http://localhost:7890/api/compile/status
```

---

#### `GET /api/compile/errors`

Получить ошибки последней компиляции.

**Query params:**
- `severity` — фильтр: `error`, `warning`, `all` (default: `all`)
- `file` — фильтр по файлу (например `Assets/Scripts/Player.cs`)

**Response:**
```json
{
    "success": true,
    "data": {
        "errors": [...],
        "total_errors": 0,
        "total_warnings": 2
    }
}
```

**curl:**
```bash
curl "http://localhost:7890/api/compile/errors?severity=error"
```

---

### 4.4 Управление сценами

#### `GET /api/scene/list`

Список загруженных сцен.

**Response:**
```json
{
    "success": true,
    "data": {
        "scenes": [
            {
                "name": "SampleScene",
                "path": "Assets/Scenes/SampleScene.unity",
                "is_loaded": true,
                "is_dirty": false,
                "is_active": true,
                "root_count": 5,
                "build_index": 0
            }
        ]
    }
}
```

**curl:**
```bash
curl http://localhost:7890/api/scene/list
```

---

#### `POST /api/scene/open`

Открыть сцену.

**Request body:**
```json
{
    "path": "Assets/Scenes/GameScene.unity",
    "mode": "Single"
}
```

`mode`: `Single` | `Additive`

---

#### `POST /api/scene/save`

Сохранить текущую сцену.

**Request body (опционально):**
```json
{
    "path": "Assets/Scenes/SampleScene.unity"
}
```

Если `path` не указан — сохраняет активную сцену.

---

#### `POST /api/scene/new`

Создать новую сцену.

**Request body:**
```json
{
    "path": "Assets/Scenes/NewLevel.unity",
    "template": "default"
}
```

`template`: `default` (камера + свет) | `empty`

---

### 4.5 Управление GameObject-ами

#### `GET /api/scene/hierarchy`

Получить полную иерархию активной сцены.

**Query params:**
- `depth` — глубина вложенности (default: `-1`, все уровни)
- `include_components` — включать список компонентов (default: `false`)
- `scene` — путь к сцене (default: активная сцена)

**Response:**
```json
{
    "success": true,
    "data": {
        "scene": "SampleScene",
        "objects": [
            {
                "instance_id": 12345,
                "name": "Main Camera",
                "active": true,
                "tag": "MainCamera",
                "layer": 0,
                "layer_name": "Default",
                "static": false,
                "transform": {
                    "local_position": {"x": 0, "y": 1, "z": -10},
                    "local_rotation": {"x": 0, "y": 0, "z": 0, "w": 1},
                    "local_scale": {"x": 1, "y": 1, "z": 1}
                },
                "components": ["Transform", "Camera", "AudioListener"],
                "children": []
            },
            {
                "instance_id": 12346,
                "name": "Directional Light",
                "active": true,
                "tag": "Untagged",
                "layer": 0,
                "layer_name": "Default",
                "static": false,
                "transform": {
                    "local_position": {"x": 0, "y": 3, "z": 0},
                    "local_rotation": {"x": 0.408, "y": -0.234, "z": 0.064, "w": 0.878},
                    "local_scale": {"x": 1, "y": 1, "z": 1}
                },
                "components": ["Transform", "Light"],
                "children": []
            }
        ],
        "total_count": 2
    }
}
```

**curl:**
```bash
curl "http://localhost:7890/api/scene/hierarchy?include_components=true&depth=3"
```

---

#### `POST /api/gameobject/create`

Создать новый GameObject на сцене.

**Request body:**
```json
{
    "name": "Player",
    "parent": null,
    "tag": "Player",
    "layer": "Default",
    "static": false,
    "active": true,
    "transform": {
        "position": {"x": 0, "y": 0, "z": 0},
        "rotation": {"x": 0, "y": 0, "z": 0},
        "scale": {"x": 1, "y": 1, "z": 1}
    },
    "components": [
        {
            "type": "Rigidbody",
            "fields": {
                "mass": 1.0,
                "useGravity": true
            }
        },
        {
            "type": "BoxCollider",
            "fields": {
                "size": {"x": 1, "y": 2, "z": 1},
                "center": {"x": 0, "y": 1, "z": 0}
            }
        },
        {
            "type": "PlayerController",
            "fields": {
                "moveSpeed": 5.0,
                "jumpForce": 10.0
            }
        }
    ]
}
```

`parent` — может быть:
- `null` — корень сцены
- `instance_id` (число) — по Instance ID
- `"name:Canvas/Panel"` — по пути в иерархии

**Response:**
```json
{
    "success": true,
    "data": {
        "instance_id": 13000,
        "name": "Player",
        "components_added": ["Transform", "Rigidbody", "BoxCollider", "PlayerController"],
        "fields_set": 5,
        "fields_failed": []
    }
}
```

**curl:**
```bash
curl -X POST http://localhost:7890/api/gameobject/create \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Player",
    "components": [
      {"type": "Rigidbody", "fields": {"mass": 1.0}},
      {"type": "PlayerController", "fields": {"moveSpeed": 5.0}}
    ]
  }'
```

---

#### `GET /api/gameobject/find`

Найти GameObject(ы) на сцене.

**Query params:**
- `name` — точное имя
- `tag` — по тегу
- `path` — путь в иерархии (например `Canvas/Panel/Button`)
- `component` — имеющий компонент определённого типа
- `instance_id` — по Instance ID

**Response:**
```json
{
    "success": true,
    "data": {
        "objects": [
            {
                "instance_id": 13000,
                "name": "Player",
                "path": "/Player",
                "active_self": true,
                "active_hierarchy": true,
                "tag": "Player",
                "layer_name": "Default",
                "components": ["Transform", "Rigidbody", "BoxCollider", "PlayerController"]
            }
        ],
        "count": 1
    }
}
```

**curl:**
```bash
curl "http://localhost:7890/api/gameobject/find?name=Player"
curl "http://localhost:7890/api/gameobject/find?tag=Enemy"
curl "http://localhost:7890/api/gameobject/find?component=Camera"
```

---

#### `PUT /api/gameobject/modify`

Изменить свойства GameObject.

**Request body:**
```json
{
    "target": {"instance_id": 13000},
    "set": {
        "name": "PlayerCharacter",
        "active": true,
        "tag": "Player",
        "layer": "Player",
        "static": false,
        "transform": {
            "position": {"x": 5, "y": 0, "z": 3},
            "rotation": {"x": 0, "y": 90, "z": 0},
            "scale": {"x": 1, "y": 1, "z": 1}
        }
    }
}
```

`target` — объект идентификации:
```json
{"instance_id": 12345}
{"name": "Player"}
{"path": "Canvas/Panel"}
{"tag": "MainCamera"}
```

---

#### `DELETE /api/gameobject/destroy`

Удалить GameObject.

**Request body:**
```json
{
    "target": {"instance_id": 13000},
    "destroy_children": true
}
```

---

#### `POST /api/gameobject/set-parent`

Изменить родителя.

**Request body:**
```json
{
    "target": {"instance_id": 13000},
    "parent": {"name": "Environment"},
    "world_position_stays": true
}
```

`parent: null` — переместить в корень сцены.

---

#### `POST /api/gameobject/duplicate`

Дублировать GameObject.

**Request body:**
```json
{
    "target": {"instance_id": 13000},
    "new_name": "Player (2)",
    "offset": {"x": 2, "y": 0, "z": 0}
}
```

---

### 4.6 Управление компонентами

#### `POST /api/component/add`

Добавить компонент к GameObject.

**Request body:**
```json
{
    "target": {"instance_id": 13000},
    "component_type": "CharacterController",
    "fields": {
        "height": 2.0,
        "radius": 0.5,
        "center": {"x": 0, "y": 1, "z": 0}
    }
}
```

`component_type` — поддерживаемые форматы:
- `"Rigidbody"` — поиск в UnityEngine
- `"UnityEngine.UI.Image"` — полное имя с namespace
- `"PlayerController"` — поиск пользовательского скрипта
- `"MyNamespace.PlayerController"` — полное имя пользовательского скрипта

---

#### `GET /api/component/get`

Получить все поля компонента.

**Query params:**
- `instance_id` — Instance ID GameObject
- `component` — имя типа компонента
- `index` — индекс компонента (если несколько одного типа, default: 0)

**Response:**
```json
{
    "success": true,
    "data": {
        "component_type": "PlayerController",
        "game_object": "Player",
        "instance_id": 13001,
        "fields": {
            "moveSpeed": {
                "value": 5.0,
                "type": "float",
                "serialized": true
            },
            "jumpForce": {
                "value": 10.0,
                "type": "float",
                "serialized": true
            },
            "groundCheck": {
                "value": null,
                "type": "Transform",
                "serialized": true,
                "is_object_reference": true
            },
            "isGrounded": {
                "value": false,
                "type": "bool",
                "serialized": false
            }
        }
    }
}
```

**curl:**
```bash
curl "http://localhost:7890/api/component/get?instance_id=13000&component=PlayerController"
```

---

#### `PUT /api/component/set-fields`

Установить значения полей компонента.

**Request body:**
```json
{
    "target": {"instance_id": 13000},
    "component": "PlayerController",
    "index": 0,
    "fields": {
        "moveSpeed": 7.5,
        "jumpForce": 12.0,
        "groundCheck": {"reference": {"name": "GroundCheck"}},
        "playerName": "Hero",
        "maxHealth": 100,
        "spawnPoints": [
            {"reference": {"name": "Spawn1"}},
            {"reference": {"name": "Spawn2"}}
        ]
    }
}
```

**Форматы значений полей:**

```
Примитивы:        "fieldName": 5.0
Строки:           "fieldName": "hello"
Bool:             "fieldName": true
Vector2:          "fieldName": {"x": 1, "y": 2}
Vector3:          "fieldName": {"x": 1, "y": 2, "z": 3}
Color:            "fieldName": {"r": 1, "g": 0, "b": 0, "a": 1}
Enum:             "fieldName": "Running"  (по имени)
                  "fieldName": 2          (по индексу)

Ссылки на объект: "fieldName": {"reference": {"instance_id": 12345}}
                  "fieldName": {"reference": {"name": "Player"}}
                  "fieldName": {"reference": {"path": "Canvas/Panel"}}

Ссылки на ассет:  "fieldName": {"asset": "Assets/Materials/Red.mat"}
                  "fieldName": {"asset": "Assets/Prefabs/Enemy.prefab"}

Null ссылка:      "fieldName": null

Массивы/списки:   "fieldName": [1, 2, 3]
                  "fieldName": [{"reference": {"name": "A"}}, {"reference": {"name": "B"}}]
```

**Response:**
```json
{
    "success": true,
    "data": {
        "fields_set": [
            {"name": "moveSpeed", "value": 7.5, "status": "ok"},
            {"name": "jumpForce", "value": 12.0, "status": "ok"},
            {"name": "groundCheck", "value": "GroundCheck (Transform)", "status": "ok"}
        ],
        "fields_failed": [],
        "total_set": 3,
        "total_failed": 0
    }
}
```

**curl:**
```bash
curl -X PUT http://localhost:7890/api/component/set-fields \
  -H "Content-Type: application/json" \
  -d '{
    "target": {"name": "Player"},
    "component": "PlayerController",
    "fields": {
        "moveSpeed": 7.5,
        "groundCheck": {"reference": {"name": "GroundCheck"}}
    }
  }'
```

---

#### `DELETE /api/component/remove`

Удалить компонент.

**Request body:**
```json
{
    "target": {"instance_id": 13000},
    "component": "BoxCollider",
    "index": 0
}
```

---

#### `GET /api/component/list-types`

Получить список доступных типов компонентов.

**Query params:**
- `filter` — фильтр по имени (например `"Collider"`)
- `category` — `unity` | `custom` | `all` (default: `all`)

**Response:**
```json
{
    "success": true,
    "data": {
        "types": [
            {"name": "BoxCollider", "full_name": "UnityEngine.BoxCollider", "category": "unity"},
            {"name": "SphereCollider", "full_name": "UnityEngine.SphereCollider", "category": "unity"},
            {"name": "PlayerController", "full_name": "PlayerController", "category": "custom"}
        ]
    }
}
```

---

### 4.7 Управление префабами

#### `POST /api/prefab/create`

Создать префаб из GameObject на сцене.

**Request body:**
```json
{
    "source": {"instance_id": 13000},
    "path": "Assets/Prefabs/Player.prefab",
    "keep_connection": true
}
```

---

#### `POST /api/prefab/instantiate`

Инстанцировать префаб на сцену.

**Request body:**
```json
{
    "prefab_path": "Assets/Prefabs/Enemy.prefab",
    "name": "Enemy_01",
    "parent": null,
    "position": {"x": 10, "y": 0, "z": 5},
    "rotation": {"x": 0, "y": 180, "z": 0},
    "component_overrides": {
        "EnemyAI": {
            "patrolSpeed": 3.0,
            "detectionRange": 15.0
        }
    }
}
```

---

#### `PUT /api/prefab/modify`

Модифицировать содержимое префаба (открывает prefab stage, вносит изменения, сохраняет).

**Request body:**
```json
{
    "prefab_path": "Assets/Prefabs/Player.prefab",
    "operations": [
        {
            "action": "add_component",
            "target_path": "",
            "component": "AudioSource",
            "fields": {"playOnAwake": false}
        },
        {
            "action": "add_child",
            "name": "GroundCheck",
            "transform": {
                "position": {"x": 0, "y": -1, "z": 0}
            }
        },
        {
            "action": "set_fields",
            "target_path": "",
            "component": "PlayerController",
            "fields": {"moveSpeed": 8.0}
        }
    ]
}
```

---

#### `POST /api/prefab/apply-overrides`

Применить override-ы экземпляра обратно в префаб.

**Request body:**
```json
{
    "instance": {"instance_id": 13000},
    "apply_all": true
}
```

---

### 4.8 Работа с файлами проекта

#### `POST /api/file/write`

Записать файл в проект (альтернатива прямой записи агентом, с автоматическим Refresh).

**Request body:**
```json
{
    "path": "Assets/Scripts/NewScript.cs",
    "content": "using UnityEngine;\n\npublic class NewScript : MonoBehaviour\n{\n    public float speed = 5f;\n}",
    "auto_refresh": true,
    "wait_compile": true
}
```

**Response (если wait_compile=true):**
```json
{
    "success": true,
    "data": {
        "file_written": true,
        "path": "Assets/Scripts/NewScript.cs",
        "compiled": true,
        "has_errors": false,
        "errors": [],
        "warnings": []
    }
}
```

**curl:**
```bash
curl -X POST http://localhost:7890/api/file/write \
  -H "Content-Type: application/json" \
  -d '{
    "path": "Assets/Scripts/PlayerController.cs",
    "content": "using UnityEngine;\n\npublic class PlayerController : MonoBehaviour\n{\n    public float moveSpeed = 5f;\n    public float jumpForce = 10f;\n    public Transform groundCheck;\n\n    void Update()\n    {\n        float h = Input.GetAxis(\"Horizontal\");\n        transform.Translate(Vector3.right * h * moveSpeed * Time.deltaTime);\n    }\n}",
    "auto_refresh": true,
    "wait_compile": true
  }'
```

---

#### `GET /api/file/read`

Прочитать файл из проекта.

**Query params:**
- `path` — путь к файлу (например `Assets/Scripts/Player.cs`)

**Response:**
```json
{
    "success": true,
    "data": {
        "path": "Assets/Scripts/Player.cs",
        "content": "using UnityEngine;...",
        "exists": true,
        "size_bytes": 1234
    }
}
```

---

### 4.9 Управление проектом

#### `GET /api/project/settings`

Получить настройки проекта.

**Query params:**
- `category` — `player` | `physics` | `input` | `tags` | `layers` | `quality`

**Response (tags/layers):**
```json
{
    "success": true,
    "data": {
        "tags": ["Untagged", "Respawn", "Finish", "EditorOnly", "MainCamera", "Player", "GameController"],
        "sorting_layers": ["Default"],
        "layers": {
            "0": "Default",
            "1": "TransparentFX",
            "2": "Ignore Raycast",
            "3": "",
            "4": "Water",
            "5": "UI",
            "8": "",
            "...": "..."
        }
    }
}
```

---

#### `POST /api/project/add-tag`

Добавить тег.

**Request body:**
```json
{
    "tag": "Enemy"
}
```

---

#### `POST /api/project/set-layer`

Установить имя пользовательского слоя.

**Request body:**
```json
{
    "layer_index": 8,
    "name": "Player"
}
```

---

### 4.10 Управление EditorPlayMode

#### `POST /api/editor/play`

Запустить Play Mode.

**Response:**
```json
{
    "success": true,
    "data": {
        "is_playing": true
    }
}
```

---

#### `POST /api/editor/stop`

Остановить Play Mode.

---

#### `POST /api/editor/pause`

Приостановить / возобновить.

---

### 4.11 Пакетные операции

#### `POST /api/batch/execute`

Выполнить несколько операций атомарно.

**Request body:**
```json
{
    "operations": [
        {
            "id": "op1",
            "endpoint": "POST /api/gameobject/create",
            "body": {
                "name": "Player",
                "tag": "Player"
            }
        },
        {
            "id": "op2",
            "endpoint": "POST /api/component/add",
            "body": {
                "target": {"name": "Player"},
                "component_type": "Rigidbody",
                "fields": {"mass": 1.0}
            }
        },
        {
            "id": "op3",
            "endpoint": "POST /api/component/add",
            "body": {
                "target": {"name": "Player"},
                "component_type": "PlayerController",
                "fields": {
                    "moveSpeed": 5.0
                }
            }
        }
    ],
    "stop_on_error": true
}
```

**Response:**
```json
{
    "success": true,
    "data": {
        "results": [
            {"id": "op1", "success": true, "data": {"instance_id": 13000}},
            {"id": "op2", "success": true, "data": {"component_added": "Rigidbody"}},
            {"id": "op3", "success": true, "data": {"component_added": "PlayerController"}}
        ],
        "total": 3,
        "succeeded": 3,
        "failed": 0
    }
}
```

**curl:**
```bash
curl -X POST http://localhost:7890/api/batch/execute \
  -H "Content-Type: application/json" \
  -d @batch_setup.json
```

---

## 5. Сервисы (детальная спецификация)

### 5.1 CompilationService

```
Ответственность:
- Отслеживание состояния компиляции
- Сбор ошибок и предупреждений
- Ожидание завершения компиляции

Подписки:
- CompilationPipeline.compilationStarted
- CompilationPipeline.compilationFinished
- CompilationPipeline.assemblyCompilationFinished

Хранимое состояние:
- List<CompilerMessage> _lastErrors
- List<CompilerMessage> _lastWarnings
- bool _isCompiling
- DateTime _lastCompileTime
- bool _lastCompileSuccess

Методы:
- Task<CompileResult> RequestCompilationAsync(int timeoutSeconds)
    1. AssetDatabase.Refresh()
    2. Ожидание compilationFinished через TaskCompletionSource
    3. Сбор сообщений из CompilationPipeline.GetSystemAssemblyPaths
       и фильтрация через CompilerMessage

- CompileResult GetLastResult()
- bool IsCompiling()
```

### 5.2 TypeResolverService

```
Ответственность:
- Нахождение System.Type по строковому имени компонента

Алгоритм поиска (по приоритету):
1. Точное совпадение: Type.GetType(fullName)
2. Поиск во всех загруженных assemblies:
   - AppDomain.CurrentDomain.GetAssemblies()
   - Для каждой assembly ищем тип по имени
3. Поиск с подстановкой namespace:
   - "Rigidbody" → ищем "UnityEngine.Rigidbody"
   - Перебор стандартных namespace-ов: UnityEngine, UnityEngine.UI,
     UnityEngine.Rendering, UnityEngine.EventSystems и т.д.
4. Если тип наследует MonoBehaviour — ищем через MonoScript:
   - AssetDatabase.FindAssets("t:MonoScript {name}")
   - MonoScript.GetClass()

Кэширование:
- Dictionary<string, Type> _typeCache
- Инвалидация кэша при recompile (подписка на compilationFinished)
```

### 5.3 ComponentService

```
Ответственность:
- Добавление компонентов к GameObject
- Чтение/запись полей компонентов через SerializedObject/SerializedProperty

Установка полей через SerializedObject:
1. new SerializedObject(component)
2. FindProperty(fieldName)
3. В зависимости от propertyType:
   - Integer → intValue
   - Float → floatValue
   - String → stringValue
   - Boolean → boolValue
   - Vector2/3/4 → vector2/3/4Value
   - Color → colorValue
   - ObjectReference → objectReferenceValue
   - Enum → enumValueIndex или enumValueFlag
   - ArraySize → arraySize + GetArrayElementAtIndex
4. serializedObject.ApplyModifiedProperties()

Почему SerializedObject а не Reflection:
- Работает с [SerializeField] private полями
- Автоматически помечает сцену как dirty
- Поддерживает Undo
- Корректно работает с prefab override-ами

Разрешение ссылок:
- {"reference": {"instance_id": N}} → EditorUtility.InstanceIDToObject(N)
- {"reference": {"name": "X"}} → GameObject.Find("X"), затем GetComponent если нужен тип
- {"reference": {"path": "A/B/C"}} → поиск по иерархии
- {"asset": "Assets/..."} → AssetDatabase.LoadAssetAtPath(path, type)
```

### 5.4 GameObjectService

```
Ответственность:
- Создание, поиск, модификация, удаление GameObject
- Работа с иерархией

Поиск (приоритет):
- По instance_id: EditorUtility.InstanceIDToObject()
- По name: GameObject.Find() + перебор сцены если несколько
- По path: рекурсивный поиск через Transform.Find()
- По tag: GameObject.FindGameObjectsWithTag()
- По component: Object.FindObjectsByType<T>()

Создание:
- ObjectFactory.CreateGameObject(name) — поддерживает Undo
- Установка parent через transform.SetParent()
- Установка tag, layer, static flags
- Вызов ComponentService для добавления компонентов

Сериализация иерархии:
- Рекурсивный обход всех root объектов сцены
- Scene.GetRootGameObjects()
- Для каждого — transform.childCount, GetChild(i)
- Ограничение глубины для предотвращения огромных ответов
```

### 5.5 SceneService

```
Ответственность:
- Открытие, закрытие, создание, сохранение сцен

Методы:
- OpenScene(path, mode):
    EditorSceneManager.OpenScene(path, (OpenSceneMode)mode)

- SaveScene(path):
    если path == null → EditorSceneManager.SaveOpenScenes()
    иначе → EditorSceneManager.SaveScene(scene, path)

- NewScene(path, template):
    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects / EmptyScene)
    EditorSceneManager.SaveScene(scene, path)

- GetLoadedScenes():
    перебор SceneManager.sceneCount, SceneManager.GetSceneAt(i)
```

### 5.6 PrefabService

```
Ответственность:
- Создание, инстанцирование, модификация префабов

Создание:
- PrefabUtility.SaveAsPrefabAsset(gameObject, path)
- PrefabUtility.SaveAsPrefabAssetAndConnect (если keep_connection)

Инстанцирование:
- AssetDatabase.LoadAssetAtPath<GameObject>(path)
- PrefabUtility.InstantiatePrefab(prefab) as GameObject
- Установка position/rotation
- Применение component_overrides через ComponentService

Модификация содержимого:
- var prefabRoot = PrefabUtility.LoadPrefabContents(path)
- Выполнение операций (add_component, add_child, set_fields)
- PrefabUtility.SaveAsPrefabAsset(prefabRoot, path)
- PrefabUtility.UnloadPrefabContents(prefabRoot)
```

---

## 6. Настройки (UACFSettings)

```csharp
// ScriptableSingleton<UACFSettings> — сохраняется в ProjectSettings/
class UACFSettings : ScriptableSingleton<UACFSettings>
{
    int port = 7890;
    bool autoStart = true;
    bool logRequests = true;
    bool logResponses = false;
    int requestTimeoutSeconds = 30;
    int compileTimeoutSeconds = 120;
    string[] allowedOrigins = {"*"};
    bool enableBatchEndpoint = true;
    LogLevel logLevel = LogLevel.Info; // None, Error, Warning, Info, Debug
}
```

Настройки доступны через:
- `Edit > Project Settings > UACF` (SettingsProvider)
- `GET /api/settings` / `PUT /api/settings`

---

## 7. Логирование

```
Все запросы логируются в Unity Console:

[UACF] POST /api/gameobject/create → 200 (45ms)
[UACF] GET /api/compile/errors → 200 (3ms)
[UACF] POST /api/component/add → 500 TYPE_NOT_FOUND: "PlayerControllerr" (2ms)

Уровни:
- Debug: тело запроса/ответа
- Info: метод, путь, статус, время
- Warning: медленные запросы (>5s)
- Error: ошибки обработки

Логирование через кастомный класс UACFLogger, НЕ Debug.Log напрямую
(чтобы можно было отключать и фильтровать)
```

---

## 8. Обработка ошибок и граничные случаи

### 8.1 Domain Reload

```
При перекомпиляции Unity делает domain reload:
- Все статические поля сбрасываются
- HttpListener уничтожается
- Потоки останавливаются

Решение:
1. AssemblyReloadEvents.beforeAssemblyReload → Stop server gracefully
2. AssemblyReloadEvents.afterAssemblyReload → Restart server
3. Port и state сохраняются в SessionState (переживает domain reload)
```

### 8.2 Concurrent Requests

```
- HttpListener обрабатывает запросы в ThreadPool
- MainThreadDispatcher сериализует Unity API вызовы
- Два запроса, пришедших одновременно, выполнятся последовательно в main thread
- HTTP-ответ отправляется только после завершения действия в main thread
```

### 8.3 Компиляция во время запроса

```
Если во время обработки запроса начинается компиляция:
- Запросы к компиляции (/api/compile/*) — продолжают работу
- Запросы к сцене/объектам — возвращают 503 SERVER_BUSY
  с заголовком Retry-After: 5
```

### 8.4 Play Mode

```
В Play Mode некоторые операции невозможны:
- Создание/удаление объектов на сцене
- Модификация префабов
- Сохранение сцены

Если запрос приходит в Play Mode:
- /api/editor/stop — работает
- Операции модификации → 409 CONFLICT с сообщением "Exit Play Mode first"
- Операции чтения (hierarchy, find, status) — работают
```

### 8.5 Несуществующие типы

```
Если агент указывает тип компонента, которого нет:
- Ответ 422 с TYPE_NOT_FOUND
- В details — список похожих типов (Levenshtein distance ≤ 3):

{
    "error": {
        "code": "TYPE_NOT_FOUND",
        "message": "Type 'PlayerControllr' not found",
        "details": {
            "suggestions": ["PlayerController", "CharacterController"]
        }
    }
}
```

---

## 9. Безопасность

```
- Сервер слушает ТОЛЬКО localhost (127.0.0.1)
- Никакого внешнего доступа по умолчанию
- Опциональный API key (header X-UACF-Key)
- Все операции логируются
- Undo поддержка для всех модификаций
  (Undo.RegisterCreatedObjectUndo, Undo.RecordObject, etc.)
```

---

## 10. Полный пример рабочего цикла агента

### Сценарий: создать 2D платформер-персонаж

```bash
# 1. Проверить статус сервера
curl http://localhost:7890/api/status

# 2. Агент создаёт файл скрипта напрямую через файловую систему
# (Cursor/Claude Code пишут файл Assets/Scripts/PlayerController2D.cs)

# 3. Запросить компиляцию и дождаться результата
curl -X POST http://localhost:7890/api/compile/request \
  -H "Content-Type: application/json" \
  -d '{"wait":true,"timeout_seconds":60}'

# 4. Если ошибки — агент исправляет файл и повторяет шаг 3

# 5. Создать GameObject с компонентами (батч)
curl -X POST http://localhost:7890/api/batch/execute \
  -H "Content-Type: application/json" \
  -d '{
    "operations": [
      {
        "id": "create_player",
        "endpoint": "POST /api/gameobject/create",
        "body": {
          "name": "Player",
          "tag": "Player",
          "transform": {"position": {"x":0,"y":2,"z":0}},
          "components": [
            {"type": "SpriteRenderer", "fields": {}},
            {"type": "Rigidbody2D", "fields": {"gravityScale": 2.0, "freezeRotation": true}},
            {"type": "BoxCollider2D", "fields": {"size": {"x":1,"y":1}}},
            {"type": "PlayerController2D", "fields": {"moveSpeed": 7.0, "jumpForce": 12.0}}
          ]
        }
      },
      {
        "id": "create_ground_check",
        "endpoint": "POST /api/gameobject/create",
        "body": {
          "name": "GroundCheck",
          "parent": {"name": "Player"},
          "transform": {"position": {"x":0,"y":-0.5,"z":0}}
        }
      }
    ],
    "stop_on_error": true
  }'

# 6. Назначить ссылку groundCheck
curl -X PUT http://localhost:7890/api/component/set-fields \
  -H "Content-Type: application/json" \
  -d '{
    "target": {"name": "Player"},
    "component": "PlayerController2D",
    "fields": {
      "groundCheck": {"reference": {"name": "GroundCheck"}}
    }
  }'

# 7. Проверить результат
curl "http://localhost:7890/api/scene/hierarchy?include_components=true"

# 8. Сохранить сцену
curl -X POST http://localhost:7890/api/scene/save

# 9. Человек нажимает Play (или агент сам)
curl -X POST http://localhost:7890/api/editor/play
```

---

## 11. Требования к реализации

### 11.1 Зависимости
- Unity 6.3+ (6000.3.x)
- .NET Standard 2.1 / .NET Framework (Unity default)
- Нет внешних NuGet пакетов
- Только `UnityEditor`, `UnityEngine` API

### 11.2 JSON сериализация
- `JsonUtility` для простых Unity-типов (Vector3, Color)
- Встроенный минимальный JSON сериализатор/десериализатор для API моделей
- **Не** использовать Newtonsoft.Json (чтобы не тянуть зависимость)
- Альтернатива: использовать `Unity.Plastic.Newtonsoft.Json` (встроен в Unity 6)

### 11.3 Тестирование
- EditMode тесты для каждого Service
- Интеграционные тесты: запуск сервера → curl → проверка состояния сцены
- Тесты на edge cases: несуществующие объекты, дублирующиеся имена, domain reload

### 11.4 Производительность
- Ответ на простые GET запросы: < 50ms
- Создание GameObject: < 100ms
- Компиляция: зависит от проекта (таймаут настраивается)
- Иерархия сцены: < 500ms для 1000 объектов

---

## 12. Этапы реализации

### Этап 1: Ядро (Core)
- [ ] UACFServer + HttpListener
- [ ] MainThreadDispatcher
- [ ] RequestRouter + RequestContext
- [ ] ResponseHelper + ApiResponse
- [ ] UACFBootstrap
- [ ] GET /api/status

### Этап 2: Компиляция
- [ ] CompilationService
- [ ] POST /api/compile/request
- [ ] GET /api/compile/status
- [ ] GET /api/compile/errors
- [ ] POST /api/assets/refresh

### Этап 3: Работа с файлами
- [ ] POST /api/file/write (с auto-compile)
- [ ] GET /api/file/read
- [ ] GET /api/assets/find
- [ ] POST /api/assets/create-folder
- [ ] DELETE /api/assets/delete

### Этап 4: Сцены и GameObject
- [ ] SceneService
- [ ] GameObjectService
- [ ] GET /api/scene/hierarchy
- [ ] POST /api/gameobject/create
- [ ] GET /api/gameobject/find
- [ ] PUT /api/gameobject/modify
- [ ] DELETE /api/gameobject/destroy

### Этап 5: Компоненты
- [ ] TypeResolverService
- [ ] ComponentService
- [ ] POST /api/component/add
- [ ] GET /api/component/get
- [ ] PUT /api/component/set-fields
- [ ] DELETE /api/component/remove

### Этап 6: Префабы
- [ ] PrefabService
- [ ] POST /api/prefab/create
- [ ] POST /api/prefab/instantiate
- [ ] PUT /api/prefab/modify

### Этап 7: Батч и дополнительное
- [ ] POST /api/batch/execute
- [ ] POST /api/editor/play|stop|pause
- [ ] Управление тегами/слоями
- [ ] UACFSettings + SettingsProvider
- [ ] UACFEditorWindow (статус)

### Этап 8: Тестирование и документация
- [ ] Unit тесты
- [ ] Интеграционные тесты
- [ ] README с примерами curl
- [ ] Cursor Rules файл (.cursorrules) с описанием API для агента
- [ ] CLAUDE.md файл с инструкциями для Claude Code