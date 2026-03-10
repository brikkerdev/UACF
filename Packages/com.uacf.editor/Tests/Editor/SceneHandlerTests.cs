using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UACF.Services;

namespace UACF.Tests
{
    public class SceneHandlerTests
    {
        [Test]
        public void SceneService_GetLoadedScenes_ReturnsScenes()
        {
            var scenes = SceneService.GetLoadedScenes();
            Assert.IsNotNull(scenes);
        }

        [Test]
        public void SerializationService_SerializeHierarchy_ReturnsStructure()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) Assert.Ignore("No active scene");

            var hierarchy = SerializationService.SerializeHierarchy(scene, 2, true);
            Assert.IsNotNull(hierarchy);
        }
    }
}
