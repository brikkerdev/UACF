using System;
using System.Collections.Generic;

namespace UACF.Core
{
    public static class ActionsRegistry
    {
        public const string Version = "1.1.0";

        public static IReadOnlyList<ActionDefinition> All => _actions;

        private static readonly List<ActionDefinition> _actions = new List<ActionDefinition>();

        static ActionsRegistry()
        {
            RegisterApi();
            RegisterScene();
            RegisterComponent();
            RegisterAsset();
            RegisterEditor();
            RegisterPrefab();
            RegisterProject();
            RegisterConsole();
            RegisterExecute();
            RegisterRuntime();
            RegisterTests();
            RegisterBatch();
        }

        private static void RegisterApi()
        {
            Add("api.list", "List all available actions with descriptions",
                Array.Empty<ParamDef>(),
                new { action = "api.list" });

            Add("api.help", "Get detailed help for a specific action",
                new ParamDef[] { new ParamDef("action", "string", true, "Action name (e.g. scene.hierarchy.get)") },
                new { action = "api.help", @params = new { action = "scene.hierarchy.get" } });

            Add("api.prompt", "Get system prompt for AI agent",
                new ParamDef[] { new ParamDef("format", "string", false, "compact or full") },
                new { action = "api.prompt", @params = new { format = "compact" } });

            Add("api.logs", "Get request log entries",
                new ParamDef[] { new ParamDef("last", "int", false, "Number of last entries") },
                new { action = "api.logs", @params = new { last = 20 } });
        }

        public static void RegisterScene()
        {
            Add("scene.hierarchy.get", "Get the full hierarchy of the active scene",
                new ParamDef[] {
                    new ParamDef("depth", "int", false, "Max tree depth"),
                    new ParamDef("components", "bool", false, "Include component names"),
                    new ParamDef("filter", "string", false, "Filter by name"),
                    new ParamDef("tag", "string", false, "Filter by tag"),
                    new ParamDef("layer", "string", false, "Filter by layer")
                },
                new { action = "scene.hierarchy.get", @params = new { depth = 2, components = true } });

            Add("scene.open", "Open a scene",
                new ParamDef[] { new ParamDef("path", "string", true, "Scene path"), new ParamDef("mode", "string", false, "single or additive") },
                new { action = "scene.open", @params = new { path = "Assets/Scenes/Level1.unity" } });

            Add("scene.new", "Create new scene",
                new ParamDef[] { new ParamDef("setup", "string", false, "empty or default") },
                new { action = "scene.new", @params = new { setup = "empty" } });

            Add("scene.save", "Save scene",
                new ParamDef[] { new ParamDef("path", "string", false, "Path to save as") },
                new { action = "scene.save" });

            Add("scene.list", "List all scenes in project",
                Array.Empty<ParamDef>(),
                new { action = "scene.list" });

            Add("scene.buildSettings.get", "Get Build Settings scenes",
                Array.Empty<ParamDef>(),
                new { action = "scene.buildSettings.get" });

            Add("scene.buildSettings.add", "Add scene to Build Settings",
                new ParamDef[] { new ParamDef("path", "string", true, "Scene path") },
                new { action = "scene.buildSettings.add", @params = new { path = "Assets/Scenes/Level1.unity" } });

            Add("scene.buildSettings.remove", "Remove scene from Build Settings",
                new ParamDef[] { new ParamDef("path", "string", true, "Scene path") },
                new { action = "scene.buildSettings.remove", @params = new { path = "Assets/Scenes/Level1.unity" } });

            Add("scene.validate", "Validate scene for issues",
                Array.Empty<ParamDef>(),
                new { action = "scene.validate" });

            Add("scene.object.create", "Create GameObject",
                new ParamDef[] {
                    new ParamDef("name", "string", false, "Object name"),
                    new ParamDef("parent", "string", false, "Parent object name"),
                    new ParamDef("position", "array", false, "[x,y,z]"),
                    new ParamDef("rotation", "array", false, "[x,y,z]"),
                    new ParamDef("scale", "array", false, "[x,y,z]"),
                    new ParamDef("tag", "string", false, "Tag"),
                    new ParamDef("layer", "string", false, "Layer"),
                    new ParamDef("components", "array", false, "Component configs")
                },
                new { action = "scene.object.create", @params = new { name = "Enemy", tag = "Enemy" } });

            Add("scene.object.find", "Find GameObjects",
                new ParamDef[] {
                    new ParamDef("name", "string", false, "Object name"),
                    new ParamDef("instanceId", "int", false, "Instance ID"),
                    new ParamDef("tag", "string", false, "Tag"),
                    new ParamDef("component", "string", false, "Component type"),
                    new ParamDef("path", "string", false, "Hierarchy path")
                },
                new { action = "scene.object.find", @params = new { name = "Player" } });

            Add("scene.object.details", "Get detailed object info",
                new ParamDef[] { new ParamDef("name", "string", false, "Object name"), new ParamDef("instanceId", "int", false, "Instance ID") },
                new { action = "scene.object.details", @params = new { name = "Player" } });

            Add("scene.object.set", "Modify GameObject",
                new ParamDef[] {
                    new ParamDef("target", "string|int", true, "Name or instanceId"),
                    new ParamDef("name", "string", false, "New name"),
                    new ParamDef("parent", "string", false, "New parent"),
                    new ParamDef("position", "array", false, "[x,y,z]"),
                    new ParamDef("active", "bool", false, "Active state")
                },
                new { action = "scene.object.set", @params = new { target = "Player", position = new[] { 0, 5, 0 } } });

            Add("scene.object.destroy", "Destroy GameObject(s)",
                new ParamDef[] { new ParamDef("name", "string", false, "Object name"), new ParamDef("tag", "string", false, "Destroy by tag") },
                new { action = "scene.object.destroy", @params = new { name = "Enemy" } });

            Add("scene.object.duplicate", "Duplicate GameObject",
                new ParamDef[] {
                    new ParamDef("target", "string|int", true, "Name or instanceId"),
                    new ParamDef("newName", "string", false, "Name for copy"),
                    new ParamDef("count", "int", false, "Number of copies")
                },
                new { action = "scene.object.duplicate", @params = new { target = "Enemy", newName = "Enemy_Copy" } });

            Add("scene.object.createPrimitive", "Create primitive",
                new ParamDef[] {
                    new ParamDef("type", "string", true, "Cube|Sphere|Capsule|Cylinder|Plane|Quad"),
                    new ParamDef("name", "string", false, "Object name"),
                    new ParamDef("position", "array", false, "[x,y,z]"),
                    new ParamDef("scale", "array", false, "[x,y,z]")
                },
                new { action = "scene.object.createPrimitive", @params = new { type = "Cube", name = "Wall" } });
        }

        public static void RegisterComponent()
        {
            Add("component.list", "List components on object",
                new ParamDef[] { new ParamDef("object", "string", true, "Object name or instanceId") },
                new { action = "component.list", @params = new { @object = "Player" } });

            Add("component.get", "Get component properties",
                new ParamDef[] {
                    new ParamDef("object", "string", true, "Object name"),
                    new ParamDef("component", "string", true, "Component type")
                },
                new { action = "component.get", @params = new { @object = "Player", component = "Rigidbody" } });

            Add("component.add", "Add component",
                new ParamDef[] {
                    new ParamDef("object", "string", true, "Object name"),
                    new ParamDef("type", "string", true, "Component type"),
                    new ParamDef("properties", "object", false, "Initial properties")
                },
                new { action = "component.add", @params = new { @object = "Player", type = "Rigidbody" } });

            Add("component.set", "Set component properties",
                new ParamDef[] {
                    new ParamDef("object", "string", true, "Object name"),
                    new ParamDef("component", "string", true, "Component type"),
                    new ParamDef("properties", "object", true, "Properties to set")
                },
                new { action = "component.set", @params = new { @object = "Player", component = "Rigidbody", properties = new { mass = 10 } } });

            Add("component.remove", "Remove component",
                new ParamDef[] {
                    new ParamDef("object", "string", true, "Object name"),
                    new ParamDef("component", "string", true, "Component type")
                },
                new { action = "component.remove", @params = new { @object = "Player", component = "Rigidbody" } });

            Add("component.setEnabled", "Enable/disable component",
                new ParamDef[] {
                    new ParamDef("object", "string", true, "Object name"),
                    new ParamDef("component", "string", true, "Component type"),
                    new ParamDef("enabled", "bool", true, "Enabled state")
                },
                new { action = "component.setEnabled", @params = new { @object = "Player", component = "MeshRenderer", enabled = false } });

            Add("component.serialized.get", "Get serialized properties",
                new ParamDef[] {
                    new ParamDef("object", "string", true, "Object name"),
                    new ParamDef("component", "string", true, "Component type")
                },
                new { action = "component.serialized.get", @params = new { @object = "Player", component = "PlayerMovement" } });

            Add("component.serialized.set", "Set serialized properties",
                new ParamDef[] {
                    new ParamDef("object", "string", true, "Object name"),
                    new ParamDef("component", "string", true, "Component type"),
                    new ParamDef("properties", "object", true, "Property values")
                },
                new { action = "component.serialized.set", @params = new { @object = "Player", component = "PlayerMovement", properties = new { speed = 10 } } });
        }

        public static void RegisterAsset()
        {
            Add("asset.find", "Find assets",
                new ParamDef[] {
                    new ParamDef("type", "string", false, "Asset type"),
                    new ParamDef("name", "string", false, "Name filter"),
                    new ParamDef("folder", "string", false, "Folder path"),
                    new ParamDef("label", "string", false, "Asset label")
                },
                new { action = "asset.find", @params = new { type = "prefab" } });

            Add("asset.info", "Get asset info",
                new ParamDef[] { new ParamDef("path", "string", true, "Asset path") },
                new { action = "asset.info", @params = new { path = "Assets/Prefabs/Player.prefab" } });

            Add("asset.tree", "Get folder tree",
                new ParamDef[] {
                    new ParamDef("path", "string", false, "Root path"),
                    new ParamDef("depth", "int", false, "Max depth")
                },
                new { action = "asset.tree", @params = new { path = "Assets", depth = 2 } });

            Add("asset.file.write", "Write file",
                new ParamDef[] {
                    new ParamDef("path", "string", true, "File path"),
                    new ParamDef("content", "string", true, "File content"),
                    new ParamDef("refresh", "bool", false, "Refresh AssetDatabase after write (default: false for compile-affecting files)"),
                    new ParamDef("waitForCompilation", "bool", false, "Wait for compilation to finish after write"),
                    new ParamDef("compileTimeoutSeconds", "int", false, "Timeout for code compilation wait")
                },
                new { action = "asset.file.write", @params = new { path = "Assets/Scripts/Test.cs", content = "// code", refresh = false, waitForCompilation = false, compileTimeoutSeconds = 120 } });

            Add("asset.file.read", "Read file",
                new ParamDef[] { new ParamDef("path", "string", true, "File path") },
                new { action = "asset.file.read", @params = new { path = "Assets/Scripts/Test.cs" } });

            Add("asset.file.move", "Move/rename file",
                new ParamDef[] {
                    new ParamDef("from", "string", true, "Source path"),
                    new ParamDef("to", "string", true, "Destination path")
                },
                new { action = "asset.file.move", @params = new { from = "Assets/Old.cs", to = "Assets/New.cs" } });

            Add("asset.file.delete", "Delete file",
                new ParamDef[] { new ParamDef("path", "string", true, "File path") },
                new { action = "asset.file.delete", @params = new { path = "Assets/Unused.cs" } });

            Add("asset.folder.create", "Create folder",
                new ParamDef[] { new ParamDef("path", "string", true, "Folder path") },
                new { action = "asset.folder.create", @params = new { path = "Assets/Prefabs/Enemies" } });

            Add("asset.refresh", "Refresh AssetDatabase",
                new ParamDef[] {
                    new ParamDef("path", "string", false, "Specific path to refresh"),
                    new ParamDef("waitForCompilation", "bool", false, "Wait until compilation is finished"),
                    new ParamDef("compileTimeoutSeconds", "int", false, "Timeout for compilation wait")
                },
                new { action = "asset.refresh", @params = new { waitForCompilation = true, compileTimeoutSeconds = 120 } });

            Add("asset.create.scriptableObject", "Create ScriptableObject asset",
                new ParamDef[] {
                    new ParamDef("path", "string", true, "Asset path under Assets/"),
                    new ParamDef("type", "string", true, "ScriptableObject type name"),
                    new ParamDef("properties", "object", false, "Initial fields/properties"),
                    new ParamDef("overwrite", "bool", false, "Overwrite existing asset")
                },
                new { action = "asset.create.scriptableObject", @params = new { path = "Assets/Data/EnemyConfig.asset", type = "EnemyConfig", overwrite = true } });

            Add("asset.create.panelSettings", "Create UI Toolkit PanelSettings asset",
                new ParamDef[] {
                    new ParamDef("path", "string", true, "Asset path under Assets/"),
                    new ParamDef("properties", "object", false, "Initial PanelSettings values"),
                    new ParamDef("overwrite", "bool", false, "Overwrite existing asset")
                },
                new { action = "asset.create.panelSettings", @params = new { path = "Assets/UI/DefaultPanelSettings.asset", overwrite = true } });

            Add("asset.create.material", "Create material asset",
                new ParamDef[] {
                    new ParamDef("path", "string", true, "Material asset path under Assets/"),
                    new ParamDef("shader", "string", false, "Shader name (default Standard)"),
                    new ParamDef("properties", "object", false, "Material properties"),
                    new ParamDef("overwrite", "bool", false, "Overwrite existing asset")
                },
                new { action = "asset.create.material", @params = new { path = "Assets/Materials/EnemyRed.mat", shader = "Standard", overwrite = true } });

            Add("asset.create.physicMaterial", "Create PhysicMaterial asset",
                new ParamDef[] {
                    new ParamDef("path", "string", true, "PhysicMaterial path under Assets/"),
                    new ParamDef("properties", "object", false, "Initial PhysicMaterial values"),
                    new ParamDef("overwrite", "bool", false, "Overwrite existing asset")
                },
                new { action = "asset.create.physicMaterial", @params = new { path = "Assets/Physics/Bouncy.physicMaterial", overwrite = true } });

            Add("asset.create.animationClip", "Create AnimationClip asset",
                new ParamDef[] {
                    new ParamDef("path", "string", true, "AnimationClip path under Assets/"),
                    new ParamDef("curves", "array", false, "Animation curve definitions"),
                    new ParamDef("wrapMode", "string", false, "Once|Loop|PingPong|ClampForever"),
                    new ParamDef("overwrite", "bool", false, "Overwrite existing asset")
                },
                new { action = "asset.create.animationClip", @params = new { path = "Assets/Animations/Spin.anim", wrapMode = "Loop", overwrite = true } });
        }

        public static void RegisterEditor()
        {
            Add("editor.compilationStatus", "Get compilation status",
                Array.Empty<ParamDef>(),
                new { action = "editor.compilationStatus" });

            Add("editor.screenshot", "Take screenshot",
                new ParamDef[] {
                    new ParamDef("view", "string", false, "scene or game"),
                    new ParamDef("width", "int", false, "Width"),
                    new ParamDef("height", "int", false, "Height")
                },
                new { action = "editor.screenshot", @params = new { view = "scene" } });

            Add("editor.play", "Enter Play Mode", Array.Empty<ParamDef>(), new { action = "editor.play" });
            Add("editor.stop", "Exit Play Mode", Array.Empty<ParamDef>(), new { action = "editor.stop" });
            Add("editor.pause", "Pause Play Mode", Array.Empty<ParamDef>(), new { action = "editor.pause" });
            Add("editor.step", "Step one frame", Array.Empty<ParamDef>(), new { action = "editor.step" });
            Add("editor.playState", "Get Play Mode state", Array.Empty<ParamDef>(), new { action = "editor.playState" });
            Add("editor.undo", "Undo", Array.Empty<ParamDef>(), new { action = "editor.undo" });
            Add("editor.redo", "Redo", Array.Empty<ParamDef>(), new { action = "editor.redo" });
            Add("editor.undoHistory", "Get undo history", Array.Empty<ParamDef>(), new { action = "editor.undoHistory" });
            Add("editor.select", "Select object(s)", new ParamDef[] { new ParamDef("object", "string", false, "Object name"), new ParamDef("objects", "array", false, "Object names") }, new { action = "editor.select", @params = new { @object = "Player" } });
            Add("editor.selection", "Get current selection", Array.Empty<ParamDef>(), new { action = "editor.selection" });
            Add("editor.focus", "Focus camera on object", new ParamDef[] { new ParamDef("object", "string", true, "Object name") }, new { action = "editor.focus", @params = new { @object = "Player" } });
        }

        public static void RegisterPrefab()
        {
            Add("prefab.create", "Create prefab from object",
                new ParamDef[] {
                    new ParamDef("sourceObject", "string", true, "Source object name"),
                    new ParamDef("path", "string", true, "Prefab path")
                },
                new { action = "prefab.create", @params = new { sourceObject = "Enemy", path = "Assets/Prefabs/Enemy.prefab" } });

            Add("prefab.instantiate", "Instantiate prefab",
                new ParamDef[] {
                    new ParamDef("path", "string", true, "Prefab path"),
                    new ParamDef("position", "array", false, "[x,y,z]"),
                    new ParamDef("rotation", "array", false, "[x,y,z]"),
                    new ParamDef("parent", "string", false, "Parent name"),
                    new ParamDef("name", "string", false, "Instance name")
                },
                new { action = "prefab.instantiate", @params = new { path = "Assets/Prefabs/Enemy.prefab", name = "Enemy_01" } });

            Add("prefab.contents", "Get prefab contents",
                new ParamDef[] { new ParamDef("path", "string", true, "Prefab path") },
                new { action = "prefab.contents", @params = new { path = "Assets/Prefabs/Player.prefab" } });

            Add("prefab.edit", "Edit prefab",
                new ParamDef[] {
                    new ParamDef("path", "string", true, "Prefab path"),
                    new ParamDef("operations", "array", true, "Edit operations")
                },
                new { action = "prefab.edit", @params = new { path = "Assets/Prefabs/Enemy.prefab", operations = new object[] { } } });

            Add("prefab.apply", "Apply overrides",
                new ParamDef[] { new ParamDef("object", "string", true, "Instance name") },
                new { action = "prefab.apply", @params = new { @object = "Enemy_01" } });

            Add("prefab.revert", "Revert overrides",
                new ParamDef[] { new ParamDef("object", "string", true, "Instance name") },
                new { action = "prefab.revert", @params = new { @object = "Enemy_01" } });

            Add("prefab.createVariant", "Create prefab variant",
                new ParamDef[] {
                    new ParamDef("basePrefab", "string", true, "Base prefab path"),
                    new ParamDef("path", "string", true, "Variant path"),
                    new ParamDef("overrides", "object", false, "Overrides")
                },
                new { action = "prefab.createVariant", @params = new { basePrefab = "Assets/Prefabs/Enemy.prefab", path = "Assets/Prefabs/EnemyBoss.prefab" } });
        }

        public static void RegisterProject()
        {
            Add("project.info", "Get project info", Array.Empty<ParamDef>(), new { action = "project.info" });
            Add("project.tags", "Get tags", Array.Empty<ParamDef>(), new { action = "project.tags" });
            Add("project.layers", "Get layers", Array.Empty<ParamDef>(), new { action = "project.layers" });
            Add("project.settings.get", "Get project settings", new ParamDef[] { new ParamDef("category", "string", true, "Settings category") }, new { action = "project.settings.get", @params = new { category = "physics" } });
            Add("project.settings.set", "Set project settings", new ParamDef[] { new ParamDef("category", "string", true, "Category"), new ParamDef("properties", "object", true, "Properties") }, new { action = "project.settings.set", @params = new { category = "physics", properties = new { } } });
        }

        public static void RegisterConsole()
        {
            Add("console.get", "Get console logs", new ParamDef[] { new ParamDef("type", "string", false, "error|warning|log"), new ParamDef("last", "int", false, "Last N"), new ParamDef("contains", "string", false, "Filter") }, new { action = "console.get" });
            Add("console.clear", "Clear console", Array.Empty<ParamDef>(), new { action = "console.clear" });
        }

        public static void RegisterExecute()
        {
            Add("execute", "Execute C# code",
                new ParamDef[] {
                    new ParamDef("code", "string", true, "C# code"),
                    new ParamDef("return", "string", false, "Return expression"),
                    new ParamDef("usings", "array", false, "Using directives"),
                    new ParamDef("timeout", "int", false, "Timeout ms")
                },
                new { action = "execute", @params = new { code = "1 + 1" } });

            Add("execute.validate", "Validate code without executing",
                new ParamDef[] { new ParamDef("code", "string", true, "C# code") },
                new { action = "execute.validate", @params = new { code = "var x = 1;" } });

            Add("execute.method", "Call static method",
                new ParamDef[] {
                    new ParamDef("type", "string", true, "Type name"),
                    new ParamDef("method", "string", true, "Method name"),
                    new ParamDef("args", "array", false, "Arguments")
                },
                new { action = "execute.method", @params = new { type = "LevelBuilder", method = "BuildLevel", args = new[] { "level1" } } });
        }

        public static void RegisterRuntime()
        {
            Add("runtime.inspect", "Inspect runtime values",
                new ParamDef[] {
                    new ParamDef("object", "string", true, "Object name"),
                    new ParamDef("component", "string", true, "Component type")
                },
                new { action = "runtime.inspect", @params = new { @object = "Player", component = "Rigidbody" } });

            Add("runtime.invoke", "Invoke method in Play Mode",
                new ParamDef[] {
                    new ParamDef("object", "string", true, "Object name"),
                    new ParamDef("component", "string", true, "Component type"),
                    new ParamDef("method", "string", true, "Method name"),
                    new ParamDef("args", "array", false, "Arguments")
                },
                new { action = "runtime.invoke", @params = new { @object = "Player", component = "PlayerHealth", method = "TakeDamage", args = new object[] { 25 } } });
        }

        public static void RegisterTests()
        {
            Add("tests.run", "Run tests", new ParamDef[] { new ParamDef("filter", "string", false, "Test filter") }, new { action = "tests.run" });
            Add("tests.results", "Get test results", Array.Empty<ParamDef>(), new { action = "tests.results" });
        }

        public static void RegisterBatch()
        {
            Add("batch", "Execute batch operations",
                new ParamDef[] {
                    new ParamDef("operations", "array", true, "Array of {action, params}"),
                    new ParamDef("undoGroup", "string", false, "Undo group name"),
                    new ParamDef("stopOnError", "bool", false, "Stop on first error")
                },
                new { action = "batch", @params = new { operations = new object[] { }, stopOnError = true } });
        }

        private static void Add(string action, string description, ParamDef[] paramDefs, object example)
        {
            _actions.Add(new ActionDefinition
            {
                Action = action,
                Description = description,
                Params = paramDefs,
                Example = example
            });
        }

        public static ActionDefinition Find(string action)
        {
            foreach (var a in _actions)
                if (a.Action == action) return a;
            return null;
        }
    }

    public class ActionDefinition
    {
        public string Action;
        public string Description;
        public ParamDef[] Params;
        public object Example;
    }

    public class ParamDef
    {
        public string Name;
        public string Type;
        public bool Required;
        public string Description;

        public ParamDef(string name, string type, bool required, string description)
        {
            Name = name;
            Type = type;
            Required = required;
            Description = description;
        }
    }
}
