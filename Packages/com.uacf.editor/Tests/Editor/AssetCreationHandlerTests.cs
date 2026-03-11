using System.Threading.Tasks;
using NUnit.Framework;
using UACF.Core;
using UACF.Handlers;
using UACF.Services;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace UACF.Tests
{
    public class AssetCreationHandlerTests
    {
        private const string Root = "Assets/UACF_Tests/CreatedAssets";

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder("Assets/UACF_Tests"))
                AssetDatabase.CreateFolder("Assets", "UACF_Tests");
            if (!AssetDatabase.IsValidFolder(Root))
                AssetDatabase.CreateFolder("Assets/UACF_Tests", "CreatedAssets");
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Root);
            AssetDatabase.Refresh();
        }

        [Test]
        public void CreateScriptableObject_CreatesAsset()
        {
            var response = AssetCreationService.CreateScriptableObject(new JObject
            {
                ["path"] = Root + "/Config.asset",
                ["type"] = nameof(TestConfigAsset),
                ["properties"] = new JObject { ["speed"] = 7.5f, ["enabledFlag"] = true },
                ["overwrite"] = true
            });

            Assert.IsTrue(response.Ok, response.Error?.Message);
            var created = AssetDatabase.LoadAssetAtPath<TestConfigAsset>(Root + "/Config.asset");
            Assert.IsNotNull(created);
            Assert.AreEqual(7.5f, created.speed);
            Assert.IsTrue(created.enabledFlag);
        }

        [Test]
        public void CreatePanelSettings_CreatesAsset_OrInconclusive()
        {
            var response = AssetCreationService.CreatePanelSettings(new JObject
            {
                ["path"] = Root + "/PanelSettings.asset",
                ["overwrite"] = true
            });

            if (!response.Ok && response.Error?.Code == "TYPE_NOT_FOUND")
                Assert.Inconclusive("PanelSettings type is not available in this Unity environment.");

            Assert.IsTrue(response.Ok, response.Error?.Message);
            var loaded = AssetDatabase.LoadAssetAtPath<ScriptableObject>(Root + "/PanelSettings.asset");
            Assert.IsNotNull(loaded);
        }

        [Test]
        public void CreateMaterial_CreatesAsset()
        {
            var response = AssetCreationService.CreateMaterial(new JObject
            {
                ["path"] = Root + "/EnemyRed.mat",
                ["shader"] = "Standard",
                ["properties"] = new JObject
                {
                    ["_Color"] = new JObject { ["r"] = 1f, ["g"] = 0f, ["b"] = 0f, ["a"] = 1f }
                },
                ["overwrite"] = true
            });

            Assert.IsTrue(response.Ok, response.Error?.Message);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/EnemyRed.mat");
            Assert.IsNotNull(material);
        }

        [Test]
        public void CreatePhysicMaterial_CreatesAsset()
        {
            var response = AssetCreationService.CreatePhysicMaterial(new JObject
            {
                ["path"] = Root + "/Bounce.physicMaterial",
                ["properties"] = new JObject { ["dynamicFriction"] = 0.1f, ["bounciness"] = 0.9f },
                ["overwrite"] = true
            });

            Assert.IsTrue(response.Ok, response.Error?.Message);
            var physicMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Root + "/Bounce.physicMaterial");
            Assert.IsNotNull(physicMaterial);
        }

        [Test]
        public void CreateAnimationClip_CreatesAsset()
        {
            var response = AssetCreationService.CreateAnimationClip(new JObject
            {
                ["path"] = Root + "/Spin.anim",
                ["wrapMode"] = "Loop",
                ["curves"] = new JArray
                {
                    new JObject
                    {
                        ["path"] = "",
                        ["property"] = "localEulerAnglesRaw.y",
                        ["type"] = "Transform",
                        ["keyframes"] = new JArray
                        {
                            new JObject { ["time"] = 0f, ["value"] = 0f },
                            new JObject { ["time"] = 1f, ["value"] = 360f }
                        }
                    }
                },
                ["overwrite"] = true
            });

            Assert.IsTrue(response.Ok, response.Error?.Message);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "/Spin.anim");
            Assert.IsNotNull(clip);
        }

        [Test]
        public void CreateScriptableObject_ReturnsErrorsForInvalidInput()
        {
            var badPath = AssetCreationService.CreateScriptableObject(new JObject
            {
                ["path"] = "ProjectSettings/NotAllowed.asset",
                ["type"] = nameof(TestConfigAsset)
            });
            Assert.IsFalse(badPath.Ok);
            Assert.AreEqual("INVALID_REQUEST", badPath.Error?.Code);

            var badType = AssetCreationService.CreateScriptableObject(new JObject
            {
                ["path"] = Root + "/Unknown.asset",
                ["type"] = "TypeThatDoesNotExist"
            });
            Assert.IsFalse(badType.Ok);
            Assert.AreEqual("TYPE_NOT_FOUND", badType.Error?.Code);
        }

        [Test]
        public async Task Dispatch_CreateMaterial_Works()
        {
            var dispatcher = new ActionDispatcher();
            AssetHandler.Register(dispatcher);

            var response = await dispatcher.DispatchAsync("asset.create.material", new JObject
            {
                ["path"] = Root + "/DispatchMaterial.mat",
                ["overwrite"] = true
            });

            Assert.IsTrue(response.Ok, response.Error?.Message);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/DispatchMaterial.mat");
            Assert.IsNotNull(material);
        }
    }

    public class TestConfigAsset : ScriptableObject
    {
        public float speed;
        public bool enabledFlag;
    }
}
