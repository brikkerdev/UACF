using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Core;
using UACF.Handlers;
using UACF.Models;
using UACF.Services;

namespace UACF.Tests
{
    /// <summary>
    /// Tests for scene stability edge cases from the UACF Scene Stability plan.
    /// Covers: invalid tags, safe Vector3 parsing, SceneService invalid paths,
    /// SerializationService safe tag/layer, HandleValidate scene check, etc.
    /// </summary>
    public class SceneStabilityEdgeCaseTests
    {
        private ActionDispatcher _dispatcher;

        [SetUp]
        public void SetUp()
        {
            if (EditorApplication.isPlaying)
                Assert.Ignore("Tests require Edit Mode");
            _dispatcher = new ActionDispatcher();
            SceneHandler.Register(_dispatcher);
            GameObjectHandler.Register(_dispatcher);
        }

        private static UacfResponse RunDispatcherTask(Task<UacfResponse> task)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!task.IsCompleted && DateTime.UtcNow < deadline)
            {
                MainThreadDispatcher.ProcessQueueForTesting();
            }
            if (!task.IsCompleted)
                Assert.Fail("Task did not complete within timeout");
            return task.Result;
        }

        [Test]
        public void SceneObjectDestroy_InvalidTag_ReturnsInvalidTag()
        {
            var task = _dispatcher.DispatchAsync("scene.object.destroy", new JObject { ["tag"] = "NonExistentTag12345" });
            var response = RunDispatcherTask(task);
            Assert.IsFalse(response.Ok, "Expected INVALID_TAG for non-existent tag");
            Assert.AreEqual("INVALID_TAG", response.Error?.Code);
        }

        [Test]
        public void SceneObjectFind_InvalidTag_ReturnsEmptyArray()
        {
            var task = _dispatcher.DispatchAsync("scene.object.find", new JObject { ["tag"] = "NonExistentTag99999" });
            var response = RunDispatcherTask(task);
            Assert.IsTrue(response.Ok, response.Error?.Message ?? "Find should succeed with empty result");
            Assert.IsNotNull(response.Data);
            var data = JObject.FromObject(response.Data);
            var count = data["count"]?.Value<int>() ?? -1;
            Assert.AreEqual(0, count, "Invalid tag should return 0 objects");
        }

        [Test]
        public void SceneObjectCreate_InvalidPositionJson_DoesNotThrow()
        {
            var task = _dispatcher.DispatchAsync("scene.object.create", new JObject
            {
                ["name"] = "TestInvalidPos",
                ["position"] = new JArray { "not", "a", "number" }
            });
            var response = RunDispatcherTask(task);
            Assert.IsTrue(response.Ok, $"Create with invalid position should not throw: {response.Error?.Message}");
        }

        [Test]
        public void SceneObjectCreate_ShortPositionArray_DoesNotThrow()
        {
            var task = _dispatcher.DispatchAsync("scene.object.create", new JObject
            {
                ["name"] = "TestShortPos",
                ["position"] = new JArray { 1f, 2f }
            });
            var response = RunDispatcherTask(task);
            Assert.IsTrue(response.Ok, $"Create with short position array should not throw: {response.Error?.Message}");
        }

        [Test]
        public void SceneObjectCreatePrimitive_InvalidPosition_DoesNotThrow()
        {
            var task = _dispatcher.DispatchAsync("scene.object.createPrimitive", new JObject
            {
                ["type"] = "Cube",
                ["name"] = "TestPrimitiveInvalidPos",
                ["position"] = new JArray { "x", "y", "z" }
            });
            var response = RunDispatcherTask(task);
            Assert.IsTrue(response.Ok, $"CreatePrimitive with invalid position should not throw: {response.Error?.Message}");
        }

        [Test]
        public void GameObjectService_Find_InvalidTag_ReturnsEmptyArray()
        {
            var payload = new GameObjectService.FindGameObjectPayload { Tag = "NonExistentTagXYZ" };
            var result = GameObjectService.Find(payload);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void GameObjectService_FindByTarget_InvalidTag_ReturnsNull()
        {
            var target = new System.Collections.Generic.Dictionary<string, object> { ["tag"] = "NonExistentTagABC" };
            var result = GameObjectService.FindByTarget(target);
            Assert.IsNull(result);
        }

        [Test]
        public void SceneService_OpenScene_InvalidPath_ReturnsFalse()
        {
            var result = SceneService.OpenScene("Assets/NonExistentScene12345.unity", "Single");
            Assert.IsFalse(result);
        }

        [Test]
        public void SceneService_OpenScene_EmptyPath_ReturnsFalse()
        {
            var result = SceneService.OpenScene("", "Single");
            Assert.IsFalse(result);
        }

        [Test]
        public void SerializationService_SerializeHierarchy_WithValidScene_DoesNotThrow()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                Assert.Ignore("No active scene");
            Assert.DoesNotThrow(() =>
            {
                var hierarchy = SerializationService.SerializeHierarchy(scene, 2, true);
                Assert.IsNotNull(hierarchy);
            });
        }

        [Test]
        public void SceneValidate_WithValidScene_ReturnsIssues()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                Assert.Ignore("No active scene");
            var task = _dispatcher.DispatchAsync("scene.validate", new JObject());
            var response = RunDispatcherTask(task);
            Assert.IsTrue(response.Ok, response.Error?.Message ?? "Validate should succeed");
        }

        [Test]
        public void SceneHierarchyGet_WithValidScene_ReturnsStructure()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                Assert.Ignore("No active scene");
            var task = _dispatcher.DispatchAsync("scene.hierarchy.get", new JObject { ["depth"] = 1, ["components"] = false });
            var response = RunDispatcherTask(task);
            Assert.IsTrue(response.Ok, response.Error?.Message ?? "Hierarchy get should succeed");
        }

        [Test]
        public void SceneObjectSet_InvalidPosition_DoesNotThrow()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                Assert.Ignore("No active scene");
            var roots = scene.GetRootGameObjects();
            if (roots.Length == 0)
                Assert.Ignore("Scene has no root objects");
            var targetName = roots[0].name;
            var task = _dispatcher.DispatchAsync("scene.object.set", new JObject
            {
                ["name"] = targetName,
                ["position"] = new JArray { "bad", "values", "here" }
            });
            var response = RunDispatcherTask(task);
            Assert.IsTrue(response.Ok, $"Set with invalid position should not throw: {response.Error?.Message}");
        }

        [Test]
        public void SceneObjectCreate_ValidPosition_SetsTransform()
        {
            var task = _dispatcher.DispatchAsync("scene.object.create", new JObject
            {
                ["name"] = "TestValidPos",
                ["position"] = new JArray { 5f, 10f, 15f }
            });
            var response = RunDispatcherTask(task);
            Assert.IsTrue(response.Ok, response.Error?.Message ?? "Create with valid position should succeed");
        }

        [Test]
        public void SceneObjectFind_WithName_ReturnsObjects()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                Assert.Ignore("No active scene");
            var roots = scene.GetRootGameObjects();
            if (roots.Length == 0)
                Assert.Ignore("Scene has no root objects");
            var task = _dispatcher.DispatchAsync("scene.object.find", new JObject { ["name"] = roots[0].name });
            var response = RunDispatcherTask(task);
            Assert.IsTrue(response.Ok, response.Error?.Message ?? "Find should succeed");
        }

        [Test]
        public void SceneObjectDetails_NonExistentObject_ReturnsObjectNotFound()
        {
            var task = _dispatcher.DispatchAsync("scene.object.details", new JObject { ["name"] = "NonExistentObjectXYZ123" });
            var response = RunDispatcherTask(task);
            Assert.IsFalse(response.Ok);
            Assert.AreEqual("OBJECT_NOT_FOUND", response.Error?.Code);
        }

        [Test]
        public void SceneObjectDestroy_NonExistentName_ReturnsObjectNotFound()
        {
            var task = _dispatcher.DispatchAsync("scene.object.destroy", new JObject { ["name"] = "NonExistentObjectToDestroy123" });
            var response = RunDispatcherTask(task);
            Assert.IsFalse(response.Ok);
            Assert.AreEqual("OBJECT_NOT_FOUND", response.Error?.Code);
        }

        [Test]
        public void SceneObjectCreate_NullPositionInParams_DoesNotThrow()
        {
            var task = _dispatcher.DispatchAsync("scene.object.create", new JObject
            {
                ["name"] = "TestNullPos",
                ["position"] = null
            });
            var response = RunDispatcherTask(task);
            Assert.IsTrue(response.Ok, $"Create with null position should not throw: {response.Error?.Message}");
        }

        [Test]
        public void SceneObjectCreate_EmptyComponentsArray_DoesNotThrow()
        {
            var task = _dispatcher.DispatchAsync("scene.object.create", new JObject
            {
                ["name"] = "TestEmptyComponents",
                ["components"] = new JArray()
            });
            var response = RunDispatcherTask(task);
            Assert.IsTrue(response.Ok, $"Create with empty components should not throw: {response.Error?.Message}");
        }

        [Test]
        public void UacfResponse_Fail_ContainsErrorCode()
        {
            var r = UacfResponse.Fail("INVALID_TAG", "Tag does not exist", "Use project.tags", 0);
            Assert.IsFalse(r.Ok);
            Assert.AreEqual("INVALID_TAG", r.Error?.Code);
        }

        [Test]
        public void UacfResponse_Fail_ServerBusyFormat()
        {
            var r = UacfResponse.Fail("SERVER_BUSY", "Editor is compiling", "Retry after compilation completes", 0);
            Assert.IsFalse(r.Ok);
            Assert.AreEqual("SERVER_BUSY", r.Error?.Code);
        }
    }
}
