using NUnit.Framework;
using UACF.Services;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace UACF.Tests
{
    public class ComponentReferenceTests
    {
        private const string Root = "Assets/UACF_Tests/RefCases";

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder("Assets/UACF_Tests"))
                AssetDatabase.CreateFolder("Assets", "UACF_Tests");
            if (!AssetDatabase.IsValidFolder(Root))
                AssetDatabase.CreateFolder("Assets/UACF_Tests", "RefCases");
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            CleanupObject("RefCase_Target");
            CleanupObject("RefCase_Follower");
            CleanupObject("RefCase_Collider");
            AssetDatabase.DeleteAsset(Root);
            AssetDatabase.Refresh();
        }

        [Test]
        public void SetSerializedFields_AssignsAssetReference_FromStringPath()
        {
            var mat = CreatePhysicsMaterialAsset(Root + "/ByPath.physicMaterial");
            var go = new GameObject("RefCase_Collider");
            var collider = go.AddComponent<BoxCollider>();

            ComponentService.SetSerializedFields(collider, new JObject
            {
                ["m_Material"] = Root + "/ByPath.physicMaterial"
            });

            Assert.AreEqual(mat, collider.sharedMaterial);
        }

        [Test]
        public void SetSerializedFields_AssignsAssetReference_FromAssetObject()
        {
            var mat = CreatePhysicsMaterialAsset(Root + "/ByAssetField.physicMaterial");
            var go = new GameObject("RefCase_Collider");
            var collider = go.AddComponent<BoxCollider>();

            ComponentService.SetSerializedFields(collider, new JObject
            {
                ["m_Material"] = new JObject
                {
                    ["asset"] = Root + "/ByAssetField.physicMaterial"
                }
            });

            Assert.AreEqual(mat, collider.sharedMaterial);
        }

        [Test]
        public void SetSerializedFields_ClearsObjectReference_WhenNull()
        {
            CreatePhysicsMaterialAsset(Root + "/ClearRef.physicMaterial");
            var go = new GameObject("RefCase_Collider");
            var collider = go.AddComponent<BoxCollider>();

            ComponentService.SetSerializedFields(collider, new JObject
            {
                ["m_Material"] = Root + "/ClearRef.physicMaterial"
            });
            Assert.IsNotNull(collider.sharedMaterial);

            ComponentService.SetSerializedFields(collider, new JObject
            {
                ["m_Material"] = null
            });
            Assert.IsNull(collider.sharedMaterial);
        }

        [Test]
        public void SetSerializedFields_AssignsSceneComponentReference_FromTopLevelName()
        {
            var targetRb = CreateTargetRigidbody();
            var joint = CreateFollowerJoint();

            ComponentService.SetSerializedFields(joint, new JObject
            {
                ["m_ConnectedBody"] = new JObject
                {
                    ["name"] = "RefCase_Target"
                }
            });

            Assert.AreEqual(targetRb, joint.connectedBody);
        }

        [Test]
        public void SetSerializedFields_AssignsSceneComponentReference_FromNestedReferenceName()
        {
            var targetRb = CreateTargetRigidbody();
            var joint = CreateFollowerJoint();

            ComponentService.SetSerializedFields(joint, new JObject
            {
                ["m_ConnectedBody"] = new JObject
                {
                    ["reference"] = new JObject
                    {
                        ["name"] = "RefCase_Target"
                    }
                }
            });

            Assert.AreEqual(targetRb, joint.connectedBody);
        }

        [Test]
        public void SetSerializedFields_AssignsSceneComponentReference_FromNestedInstanceId()
        {
            var targetRb = CreateTargetRigidbody();
            var joint = CreateFollowerJoint();

            ComponentService.SetSerializedFields(joint, new JObject
            {
                ["m_ConnectedBody"] = new JObject
                {
                    ["reference"] = new JObject
                    {
                        ["instance_id"] = targetRb.GetInstanceID()
                    }
                }
            });

            Assert.AreEqual(targetRb, joint.connectedBody);
        }

        private static PhysicsMaterial CreatePhysicsMaterialAsset(string path)
        {
            var mat = new PhysicsMaterial("RefMaterial");
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        }

        private static Rigidbody CreateTargetRigidbody()
        {
            var target = new GameObject("RefCase_Target");
            return target.AddComponent<Rigidbody>();
        }

        private static SpringJoint CreateFollowerJoint()
        {
            var follower = new GameObject("RefCase_Follower");
            follower.AddComponent<Rigidbody>();
            return follower.AddComponent<SpringJoint>();
        }

        private static void CleanupObject(string name)
        {
            var obj = GameObject.Find(name);
            if (obj != null)
                Object.DestroyImmediate(obj);
        }
    }
}
