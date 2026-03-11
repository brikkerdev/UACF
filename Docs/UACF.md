

# ТЗ: UACF v1.1

---

## Философия

```
Агент должен уметь делать ВСЁ, что может делать человек в Unity Editor,
и получать полную обратную связь о результате.
Один протокол. Один формат. Curl — единственный инструмент.
```

---

## Единый формат запросов

**Все запросы — POST с JSON телом.**

```bash
curl -X POST http://localhost:6400/uacf \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer TOKEN" \
  -d '{
    "action": "scene.hierarchy.get",
    "params": {
      "depth": 2,
      "components": true
    }
  }'
```

**Структура запроса:**

```json
{
  "action": "string — точечная нотация действия",
  "params": { /* параметры действия, опционально */ }
}
```

---

## Единый формат ответов

**Успех:**

```json
{
  "ok": true,
  "data": { /* результат */ },
  "warnings": ["NavMesh не запечён"],
  "duration": 0.034
}
```

**Ошибка:**

```json
{
  "ok": false,
  "error": {
    "code": "OBJECT_NOT_FOUND",
    "message": "GameObject 'Player' not found in active scene",
    "suggestion": "Available objects: Main Camera, Directional Light, Cube"
  },
  "duration": 0.002
}
```

**Правила:**
- `ok` — всегда присутствует
- `data` — всегда присутствует при `ok: true`, может быть `null` для void-операций
- `warnings` — массив, присутствует только если есть предупреждения
- `error` — присутствует только при `ok: false`
- `suggestion` — подсказка агенту как исправить ситуацию (где возможно)
- `duration` — секунды выполнения

---

## Модуль 1: Ядро

### 1.1 Конфигурация

```json
// ProjectSettings/UACF/config.json
{
  "port": 6400,
  "host": "127.0.0.1",
  "token": "auto-generated-on-first-run",
  "allowExecute": true,
  "logRequests": true,
  "logFile": "Logs/UACF/session.log"
}
```

При первом запуске токен выводится в Unity Console:
```
[UACF] Server started on http://127.0.0.1:6400
[UACF] Auth token: a7f3b2c1d4e5...
```

### 1.2 API Discovery

```json
// Список всех доступных actions
{ "action": "api.list" }
// → {
//     "ok": true,
//     "data": {
//       "version": "2.0.0",
//       "actions": [
//         {
//           "action": "scene.hierarchy.get",
//           "description": "Get the full hierarchy of the active scene",
//           "params": [
//             { "name": "depth", "type": "int", "required": false, "description": "Max tree depth" },
//             { "name": "components", "type": "bool", "required": false, "description": "Include component names" }
//           ],
//           "example": { "action": "scene.hierarchy.get", "params": { "depth": 2, "components": true } }
//         },
//         ...
//       ]
//     }
//   }

// Справка по конкретному action
{ "action": "api.help", "params": { "action": "scene.object.create" } }

// Готовый system prompt для агента
{ "action": "api.prompt", "params": { "format": "compact" } }
// → { "ok": true, "data": { "prompt": "You have access to Unity Editor through UACF..." } }

// Формат "full" — со всеми примерами
{ "action": "api.prompt", "params": { "format": "full" } }
```

### 1.3 Лог запросов

```json
{ "action": "api.logs", "params": { "last": 20 } }
// → {
//     "ok": true,
//     "data": {
//       "entries": [
//         { "timestamp": 1705312800, "action": "scene.object.create", "ok": true, "duration": 0.012 },
//         { "timestamp": 1705312801, "action": "component.add", "ok": false, "error": "INVALID_TYPE" },
//         ...
//       ]
//     }
//   }
```

---

## Модуль 2: Execute

**Самый важный модуль. Делает фреймворк безграничным.**

### 2.1 Выполнение C# кода

```json
// Простое выражение
{
  "action": "execute",
  "params": {
    "code": "GameObject.FindObjectsOfType<Light>().Length"
  }
}
// → { "ok": true, "data": { "result": 3 } }

// Многострочный скрипт
{
  "action": "execute",
  "params": {
    "code": "var go = new GameObject(\"Test\"); go.AddComponent<Rigidbody>(); go.transform.position = new Vector3(1,2,3);",
    "return": "go.GetInstanceID()"
  }
}
// → { "ok": true, "data": { "result": 14520 } }

// С using-ами
{
  "action": "execute",
  "params": {
    "code": "EditorSceneManager.SaveOpenScenes();",
    "usings": ["UnityEditor", "UnityEditor.SceneManagement"]
  }
}

// С таймаутом
{
  "action": "execute",
  "params": {
    "code": "SomeLongOperation();",
    "timeout": 10000
  }
}
```

### 2.2 Проверка компиляции без выполнения

```json
{
  "action": "execute.validate",
  "params": {
    "code": "var x = new GameObjec();"
  }
}
// → { "ok": false, "error": { "code": "COMPILATION_ERROR", "message": "The type 'GameObjec' could not be found..." } }
```

### 2.3 Выполнение статического метода из проекта

```json
{
  "action": "execute.method",
  "params": {
    "type": "LevelBuilder",
    "method": "BuildLevel",
    "args": ["level1", "hard"]
  }
}
```

---

## Модуль 3: Сцена

### 3.1 Иерархия

```json
{ "action": "scene.hierarchy.get" }

{ "action": "scene.hierarchy.get", "params": { 
  "depth": 2, 
  "components": true, 
  "filter": "Enemy",
  "tag": "Enemy",
  "layer": "Enemies"
}}
```

**Ответ:**

```json
{
  "ok": true,
  "data": {
    "sceneName": "SampleScene",
    "scenePath": "Assets/Scenes/SampleScene.unity",
    "isDirty": true,
    "rootObjects": [
      {
        "name": "Main Camera",
        "instanceId": 100,
        "active": true,
        "tag": "MainCamera",
        "layer": "Default",
        "components": ["Transform", "Camera", "AudioListener"],
        "children": []
      },
      {
        "name": "Player",
        "instanceId": 102,
        "active": true,
        "tag": "Player",
        "components": ["Transform", "CharacterController", "PlayerMovement"],
        "children": [
          {
            "name": "Model",
            "instanceId": 103,
            "components": ["Transform", "MeshRenderer", "MeshFilter"],
            "children": []
          }
        ]
      }
    ]
  }
}
```

### 3.2 Управление сценами

```json
// Открыть
{ "action": "scene.open", "params": { "path": "Assets/Scenes/Level1.unity", "mode": "single" } }
// mode: "single" | "additive"

// Создать новую
{ "action": "scene.new", "params": { "setup": "empty" } }
// setup: "empty" | "default"

// Сохранить
{ "action": "scene.save" }
{ "action": "scene.save", "params": { "path": "Assets/Scenes/Level2.unity" } }

// Список всех сцен в проекте
{ "action": "scene.list" }

// Build Settings
{ "action": "scene.buildSettings.get" }
{ "action": "scene.buildSettings.add", "params": { "path": "Assets/Scenes/Level1.unity" } }
{ "action": "scene.buildSettings.remove", "params": { "path": "Assets/Scenes/Level1.unity" } }
```

### 3.3 GameObject CRUD

```json
// Создать
{ "action": "scene.object.create", "params": {
  "name": "Enemy",
  "parent": "EnemyContainer",
  "position": [10, 0, 5],
  "rotation": [0, 90, 0],
  "scale": [1, 1, 1],
  "tag": "Enemy",
  "layer": "Enemies",
  "static": false,
  "components": [
    { "type": "Rigidbody", "properties": { "mass": 2.0, "useGravity": true } },
    { "type": "BoxCollider", "properties": { "size": [1, 2, 1] } },
    { "type": "EnemyAI" }
  ]
}}
// parent, position, rotation, scale, tag, layer, static, components — всё опционально

// Найти
{ "action": "scene.object.find", "params": { "name": "Player" } }
{ "action": "scene.object.find", "params": { "instanceId": 14520 } }
{ "action": "scene.object.find", "params": { "tag": "Enemy" } }
{ "action": "scene.object.find", "params": { "component": "Camera" } }
{ "action": "scene.object.find", "params": { "path": "Player/Model" } }
// → Всегда возвращает массив объектов

// Подробная информация (все компоненты со всеми свойствами)
{ "action": "scene.object.details", "params": { "name": "Player" } }

// Изменить
{ "action": "scene.object.set", "params": {
  "target": "Player",
  "name": "MainPlayer",
  "parent": "Characters",
  "position": [0, 5, 0],
  "active": false,
  "tag": "Untagged"
}}
// target — обязательно (name или instanceId), остальное — что нужно изменить

// Удалить
{ "action": "scene.object.destroy", "params": { "name": "Enemy" } }
{ "action": "scene.object.destroy", "params": { "tag": "Enemy" } }

// Дублировать
{ "action": "scene.object.duplicate", "params": { 
  "target": "Enemy", 
  "newName": "Enemy_Copy", 
  "count": 5 
}}

// Создать примитив
{ "action": "scene.object.createPrimitive", "params": {
  "type": "Cube",
  "name": "Wall",
  "position": [0, 1, 5],
  "scale": [10, 2, 0.5]
}}
// type: "Cube" | "Sphere" | "Capsule" | "Cylinder" | "Plane" | "Quad"
```

### 3.4 Валидация сцены

```json
{ "action": "scene.validate" }
// → {
//     "ok": true,
//     "data": {
//       "issues": [
//         { "severity": "error", "message": "Missing script on 'Enemy_03'", "object": "Enemy_03" },
//         { "severity": "warning", "message": "Light has no shadows", "object": "Spot Light" },
//         { "severity": "info", "message": "3 objects have negative scale" }
//       ]
//     }
//   }
```

---

## Модуль 4: Компоненты

### 4.1 CRUD

```json
// Список компонентов объекта
{ "action": "component.list", "params": { "object": "Player" } }
// → { "ok": true, "data": { "components": ["Transform", "CharacterController", "PlayerMovement"] } }

// Подробности компонента (все свойства)
{ "action": "component.get", "params": { 
  "object": "Player", 
  "component": "CharacterController" 
}}
// → { "ok": true, "data": { 
//     "type": "CharacterController",
//     "enabled": true,
//     "properties": {
//       "slopeLimit": 45.0,
//       "stepOffset": 0.3,
//       "skinWidth": 0.08,
//       "center": [0, 1, 0],
//       "radius": 0.5,
//       "height": 2.0
//     }
//   }}

// Добавить
{ "action": "component.add", "params": {
  "object": "Player",
  "type": "Rigidbody",
  "properties": {
    "mass": 5.0,
    "drag": 0.1,
    "useGravity": true,
    "isKinematic": false
  }
}}

// Изменить свойства
{ "action": "component.set", "params": {
  "object": "Player",
  "component": "Rigidbody",
  "properties": {
    "mass": 10.0,
    "useGravity": false
  }
}}

// Удалить
{ "action": "component.remove", "params": { 
  "object": "Player", 
  "component": "Rigidbody" 
}}

// Включить/выключить
{ "action": "component.setEnabled", "params": { 
  "object": "Player", 
  "component": "MeshRenderer", 
  "enabled": false 
}}
```

### 4.2 Serialized Properties (глубокий доступ)

```json
// Получить все serialized свойства (работает с кастомными скриптами)
{ "action": "component.serialized.get", "params": {
  "object": "Player",
  "component": "PlayerMovement"
}}
// → {
//     "ok": true,
//     "data": {
//       "properties": [
//         { "name": "speed", "type": "float", "value": 5.0 },
//         { "name": "jumpHeight", "type": "float", "value": 2.0 },
//         { "name": "groundLayer", "type": "LayerMask", "value": 256 },
//         { "name": "weaponPrefab", "type": "ObjectReference", 
//           "value": { "guid": "abc123...", "name": "Sword.prefab" } }
//       ]
//     }
//   }

// Установить через serialized properties
{ "action": "component.serialized.set", "params": {
  "object": "Player",
  "component": "PlayerMovement",
  "properties": {
    "speed": 10.0,
    "jumpHeight": 3.5,
    "weaponPrefab": { "guid": "abc123..." }
  }
}}
```

---

## Модуль 5: Ассеты

### 5.1 Поиск

```json
{ "action": "asset.find", "params": { "type": "prefab" } }
{ "action": "asset.find", "params": { "type": "material", "name": "*Metal*" } }
{ "action": "asset.find", "params": { "type": "script", "folder": "Assets/Scripts" } }
{ "action": "asset.find", "params": { "type": "texture", "label": "Environment" } }
// → { "ok": true, "data": { "assets": [
//     { "path": "Assets/Materials/Metal.mat", "guid": "...", "type": "Material" },
//     ...
//   ]}}

// Информация об ассете
{ "action": "asset.info", "params": { "path": "Assets/Prefabs/Player.prefab" } }
// → {
//     "ok": true,
//     "data": {
//       "path": "Assets/Prefabs/Player.prefab",
//       "guid": "a1b2c3d4...",
//       "type": "GameObject (Prefab)",
//       "fileSize": 4520,
//       "dependencies": ["Assets/Materials/Player.mat"],
//       "usedBy": ["Assets/Scenes/Level1.unity"]
//     }
//   }

// Дерево папок
{ "action": "asset.tree", "params": { "path": "Assets", "depth": 2 } }
```

### 5.2 Файловые операции

```json
// Записать файл (скрипты, шейдеры, любой текст)
{ "action": "asset.file.write", "params": {
  "path": "Assets/Scripts/EnemyAI.cs",
  "content": "using UnityEngine;\n\npublic class EnemyAI : MonoBehaviour\n{\n    public float speed = 5f;\n}"
}}

// Прочитать файл
{ "action": "asset.file.read", "params": { "path": "Assets/Scripts/PlayerMovement.cs" } }
// → { "ok": true, "data": { "content": "using UnityEngine;\n\npublic class..." } }

// Переместить/переименовать
{ "action": "asset.file.move", "params": { 
  "from": "Assets/Scripts/Old/Enemy.cs", 
  "to": "Assets/Scripts/AI/EnemyAI.cs" 
}}

// Удалить
{ "action": "asset.file.delete", "params": { "path": "Assets/Scripts/Unused.cs" } }

// Создать папку
{ "action": "asset.folder.create", "params": { "path": "Assets/Prefabs/Enemies" } }

// Обновить базу ассетов (после записи файлов)
{ "action": "asset.refresh" }
{ "action": "asset.refresh", "params": { "path": "Assets/Scripts/EnemyAI.cs" } }
```

### 5.3 Создание ассетов

```json
// Материал
{ "action": "asset.create.material", "params": {
  "path": "Assets/Materials/EnemyRed.mat",
  "shader": "Standard",
  "properties": {
    "_Color": [1, 0, 0, 1],
    "_Metallic": 0.5,
    "_Glossiness": 0.8
  }
}}

// ScriptableObject
{ "action": "asset.create.scriptableObject", "params": {
  "path": "Assets/Data/EnemyConfig.asset",
  "type": "EnemyConfig",
  "properties": {
    "health": 100,
    "speed": 3.5,
    "damage": 10
  }
}}

// Physic Material
{ "action": "asset.create.physicMaterial", "params": {
  "path": "Assets/Physics/Bouncy.physicMaterial",
  "properties": {
    "dynamicFriction": 0.2,
    "bounciness": 0.8
  }
}}

// Animation Clip
{ "action": "asset.create.animationClip", "params": {
  "path": "Assets/Animations/Spin.anim",
  "curves": [
    {
      "path": "",
      "property": "localEulerAnglesRaw.y",
      "type": "Transform",
      "keyframes": [
        { "time": 0, "value": 0 },
        { "time": 1, "value": 360 }
      ]
    }
  ],
  "wrapMode": "Loop"
}}
```

---

## Модуль 6: Префабы

```json
// Создать префаб из объекта на сцене
{ "action": "prefab.create", "params": {
  "sourceObject": "Enemy",
  "path": "Assets/Prefabs/Enemy.prefab"
}}

// Инстанцировать на сцену
{ "action": "prefab.instantiate", "params": {
  "path": "Assets/Prefabs/Enemy.prefab",
  "position": [10, 0, 5],
  "rotation": [0, 180, 0],
  "parent": "EnemyContainer",
  "name": "Enemy_01"
}}

// Содержимое префаба (иерархия + компоненты без инстанцирования)
{ "action": "prefab.contents", "params": { "path": "Assets/Prefabs/Player.prefab" } }

// Редактировать префаб
{ "action": "prefab.edit", "params": {
  "path": "Assets/Prefabs/Enemy.prefab",
  "operations": [
    { "op": "addComponent", "target": ".", "type": "AudioSource" },
    { "op": "setProperty", "target": ".", "component": "EnemyAI", "property": "speed", "value": 10 },
    { "op": "addChild", "name": "HealthBar", "components": ["Canvas", "Slider"] },
    { "op": "removeComponent", "target": "Model", "type": "MeshCollider" }
  ]
}}
// target: "." — корень префаба, "Model" — дочерний объект по имени, "Model/Mesh" — по пути

// Применить overrides
{ "action": "prefab.apply", "params": { "object": "Enemy_01" } }

// Сбросить overrides
{ "action": "prefab.revert", "params": { "object": "Enemy_01" } }

// Создать Prefab Variant
{ "action": "prefab.createVariant", "params": {
  "basePrefab": "Assets/Prefabs/Enemy.prefab",
  "path": "Assets/Prefabs/EnemyBoss.prefab",
  "overrides": {
    ".": {
      "EnemyAI": { "health": 500, "speed": 2.0 }
    }
  }
}}
```

---

## Модуль 7: Обратная связь

### 7.1 Console

```json
{ "action": "console.get" }
{ "action": "console.get", "params": { "type": "error" } }
{ "action": "console.get", "params": { "type": "warning", "last": 10 } }
{ "action": "console.get", "params": { "contains": "NullReference" } }
{ "action": "console.get", "params": { "since": 1705312800 } }
// → {
//     "ok": true,
//     "data": {
//       "entries": [
//         {
//           "type": "error",
//           "message": "NullReferenceException: Object reference not set...",
//           "stackTrace": "at EnemyAI.Update() in Assets/Scripts/EnemyAI.cs:42",
//           "timestamp": 1705312856,
//           "count": 3
//         }
//       ]
//     }
//   }

// Очистить
{ "action": "console.clear" }
```

### 7.2 Статус компиляции

```json
{ "action": "editor.compilationStatus" }
// → {
//     "ok": true,
//     "data": {
//       "isCompiling": false,
//       "hasErrors": true,
//       "errors": [
//         {
//           "file": "Assets/Scripts/EnemyAI.cs",
//           "line": 42,
//           "column": 15,
//           "message": "'playerTransform' does not exist in the current context",
//           "severity": "error"
//         }
//       ],
//       "warnings": [
//         {
//           "file": "Assets/Scripts/Utils.cs",
//           "line": 10,
//           "message": "Variable 'temp' is assigned but never used",
//           "severity": "warning"
//         }
//       ]
//     }
//   }
```

### 7.3 Скриншот

```json
{ "action": "editor.screenshot", "params": { "view": "scene" } }
{ "action": "editor.screenshot", "params": { "view": "game", "width": 1920, "height": 1080 } }
{ "action": "editor.screenshot", "params": { "camera": "SecurityCam", "width": 512, "height": 512 } }
// → { "ok": true, "data": { "base64": "iVBORw0KGgo...", "format": "png", "width": 1920, "height": 1080 } }
```

---

## Модуль 8: Editor

### 8.1 Play Mode

```json
{ "action": "editor.play" }
{ "action": "editor.stop" }
{ "action": "editor.pause" }
{ "action": "editor.step" }
{ "action": "editor.playState" }
// → { "ok": true, "data": { "state": "playing", "time": 12.5, "frameCount": 750 } }
```

### 8.2 Undo/Redo

```json
{ "action": "editor.undo" }
{ "action": "editor.redo" }
{ "action": "editor.undoHistory" }
// → { "ok": true, "data": { "history": ["Create 'Enemy'", "Add Rigidbody", "Move 'Enemy'"] } }
```

### 8.3 Selection и Focus

```json
// Выделить объект (чтобы человек видел что агент делает)
{ "action": "editor.select", "params": { "object": "Player" } }
{ "action": "editor.select", "params": { "objects": ["Enemy_01", "Enemy_02"] } }

// Что выделено
{ "action": "editor.selection" }

// Фокус камеры Scene View
{ "action": "editor.focus", "params": { "object": "Player" } }
```

### 8.4 Проект

```json
{ "action": "project.info" }
// → {
//     "ok": true,
//     "data": {
//       "unityVersion": "2022.3.20f1",
//       "projectName": "MyGame",
//       "projectPath": "/Users/dev/MyGame",
//       "renderPipeline": "URP",
//       "scriptingBackend": "IL2CPP",
//       "targetPlatform": "Windows",
//       "packages": [
//         { "name": "com.unity.render-pipelines.universal", "version": "14.0.9" }
//       ]
//     }
//   }

// Теги, слои
{ "action": "project.tags" }
{ "action": "project.layers" }

// Project Settings
{ "action": "project.settings.get", "params": { "category": "physics" } }
{ "action": "project.settings.set", "params": { 
  "category": "physics", 
  "properties": { "gravity": [0, -15, 0] } 
}}
```

---

## Модуль 9: Runtime (Play Mode)

```json
// Инспекция runtime-значений (только в Play Mode)
{ "action": "runtime.inspect", "params": {
  "object": "Player",
  "component": "Rigidbody"
}}
// → { "ok": true, "data": { "velocity": [2.3, -0.1, 1.5], "angularVelocity": [0,0,0] } }

// Вызвать метод
{ "action": "runtime.invoke", "params": {
  "object": "Player",
  "component": "PlayerHealth",
  "method": "TakeDamage",
  "args": [25]
}}
// → { "ok": true, "data": { "result": null } }
```

---

## Модуль 10: Тесты

```json
// Запустить тесты
{ "action": "tests.run" }
{ "action": "tests.run", "params": { "filter": "EditMode" } }
{ "action": "tests.run", "params": { "filter": "EnemyAITests" } }

// Результаты
{ "action": "tests.results" }
// → {
//     "ok": true,
//     "data": {
//       "passed": 15,
//       "failed": 2,
//       "skipped": 1,
//       "failures": [
//         { 
//           "test": "EnemyAITests.TestPathfinding", 
//           "message": "Expected 5 but got 3",
//           "stackTrace": "..."
//         }
//       ]
//     }
//   }
```

---

## Модуль 11: Batch

```json
{
  "action": "batch",
  "params": {
    "undoGroup": "Create Enemy Squad",
    "stopOnError": true,
    "operations": [
      { "action": "scene.object.create", "params": { "name": "EnemySquad" } },
      { "action": "prefab.instantiate", "params": { 
        "path": "Assets/Prefabs/Enemy.prefab", "parent": "EnemySquad", 
        "position": [0,0,0], "name": "Enemy_01" 
      }},
      { "action": "prefab.instantiate", "params": { 
        "path": "Assets/Prefabs/Enemy.prefab", "parent": "EnemySquad", 
        "position": [5,0,0], "name": "Enemy_02" 
      }},
      { "action": "prefab.instantiate", "params": { 
        "path": "Assets/Prefabs/Enemy.prefab", "parent": "EnemySquad", 
        "position": [10,0,0], "name": "Enemy_03" 
      }}
    ]
  }
}
// → {
//     "ok": true,
//     "data": {
//       "results": [
//         { "ok": true, "data": { "instanceId": 200 } },
//         { "ok": true, "data": { "instanceId": 201 } },
//         { "ok": true, "data": { "instanceId": 202 } },
//         { "ok": true, "data": { "instanceId": 203 } }
//       ],
//       "undoGroup": "Create Enemy Squad"
//     }
//   }
// При stopOnError: true — если операция N упала, операции N+1... не выполняются,
// предыдущие откатываются через Undo
```

---

## Полный список actions

```
── API ──
api.list                          Список всех actions с описаниями
api.help                          Подробная справка по action
api.prompt                        Готовый system prompt для агента
api.logs                          Лог запросов к UACF

── EXECUTE ──
execute                           Выполнить произвольный C# код
execute.validate                  Проверить компиляцию без выполнения
execute.method                    Вызвать статический метод из проекта

── SCENE ──
scene.hierarchy.get               Иерархия активной сцены
scene.open                        Открыть сцену
scene.new                         Создать новую сцену
scene.save                        Сохранить сцену
scene.list                        Все сцены в проекте
scene.buildSettings.get           Сцены в Build Settings
scene.buildSettings.add           Добавить сцену в Build Settings
scene.buildSettings.remove        Убрать сцену из Build Settings
scene.validate                    Валидация сцены
scene.object.create               Создать GameObject
scene.object.createPrimitive      Создать примитив
scene.object.find                 Найти GameObject
scene.object.details              Подробная информация об объекте
scene.object.set                  Изменить GameObject
scene.object.destroy              Удалить GameObject
scene.object.duplicate            Дублировать GameObject

── COMPONENT ──
component.list                    Список компонентов объекта
component.get                     Свойства компонента
component.add                     Добавить компонент
component.set                     Изменить свойства компонента
component.remove                  Удалить компонент
component.setEnabled              Включить/выключить компонент
component.serialized.get          Serialized properties
component.serialized.set          Установить serialized properties

── ASSET ──
asset.find                        Поиск ассетов
asset.info                        Информация об ассете
asset.tree                        Дерево папок
asset.file.write                  Записать файл
asset.file.read                   Прочитать файл
asset.file.move                   Переместить файл
asset.file.delete                 Удалить файл
asset.folder.create               Создать папку
asset.refresh                     Обновить базу ассетов
asset.create.material             Создать материал
asset.create.scriptableObject     Создать ScriptableObject
asset.create.physicMaterial       Создать Physic Material
asset.create.animationClip        Создать Animation Clip

── PREFAB ──
prefab.create                     Создать префаб из объекта
prefab.instantiate                Инстанцировать на сцену
prefab.contents                   Содержимое префаба
prefab.edit                       Редактировать префаб
prefab.apply                      Применить overrides
prefab.revert                     Сбросить overrides
prefab.createVariant              Создать Prefab Variant

── CONSOLE ──
console.get                       Получить логи
console.clear                     Очистить консоль

── EDITOR ──
editor.compilationStatus          Статус компиляции
editor.screenshot                 Скриншот Scene/Game View
editor.play                       Войти в Play Mode
editor.stop                       Выйти из Play Mode
editor.pause                      Пауза
editor.step                       Один кадр
editor.playState                  Текущее состояние Play Mode
editor.undo                       Отменить
editor.redo                       Повторить
editor.undoHistory                История undo
editor.select                     Выделить объект
editor.selection                  Текущее выделение
editor.focus                      Фокус камеры на объекте

── PROJECT ──
project.info                      Информация о проекте
project.tags                      Список тегов
project.layers                    Список слоёв
project.settings.get              Настройки проекта
project.settings.set              Изменить настройки

── RUNTIME ──
runtime.inspect                   Инспекция в Play Mode
runtime.invoke                    Вызов метода в Play Mode

── TESTS ──
tests.run                         Запустить тесты
tests.results                     Результаты тестов

── BATCH ──
batch                             Пакетное выполнение операций
```

---

## Приоритеты реализации

### Фаза 1 — Без этого не работает

```
 1. Единый POST endpoint + формат запросов/ответов
 2. api.list, api.help, api.prompt
 3. execute (произвольный C#)
 4. scene.hierarchy.get
 5. scene.object.create / find / set / destroy
 6. component.list / get / add / set / remove
 7. asset.file.write / read
 8. asset.refresh
 9. console.get
10. editor.compilationStatus
```

### Фаза 2 — Продуктивная работа

```
11. scene.open / save / new / list
12. scene.object.duplicate / createPrimitive
13. prefab.create / instantiate / contents / edit
14. asset.find / info / tree
15. asset.create.material
16. editor.undo / redo
17. editor.play / stop / pause / playState
18. editor.select / focus
19. scene.validate
20. project.info
21. batch
```

### Фаза 3 — Полнота

```
22. component.serialized.get / set
23. component.setEnabled
24. asset.file.move / delete
25. asset.folder.create
26. asset.create.scriptableObject / physicMaterial / animationClip
27. prefab.apply / revert / createVariant
28. editor.screenshot
29. editor.undoHistory / selection
30. project.tags / layers / settings
31. runtime.inspect / invoke
32. tests.run / results
33. execute.validate / execute.method
34. scene.buildSettings.*
35. api.logs
36. editor.step
```