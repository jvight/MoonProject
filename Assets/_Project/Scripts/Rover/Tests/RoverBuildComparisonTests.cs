using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;
using MoonProject.Rover.Editor;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// On 07's real build (the generated art and tuning on disk), the comparison the Rover/Rover builder trusts before
    /// skipping a save: two builds are alike, and changes inside the nested kit prefabs are seen, including which of
    /// the two capacitor drums (instances of one prefab) is which.
    /// </summary>
    public sealed class RoverBuildComparisonTests
    {
        private const string DrumName = "Kit_CapacitorDrum";

        [Test]
        public void TwoBuilds_AreAlike()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject first = RoverPrefabBuilder.Assemble(scratch);
                GameObject second = RoverPrefabBuilder.Assemble(scratch);
                Assert.IsNull(HierarchyComparison.FirstDifference(first, second));
            }
        }

        [Test]
        public void ShownDrum_IsADifference()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject first = RoverPrefabBuilder.Assemble(scratch);
                GameObject second = RoverPrefabBuilder.Assemble(scratch);
                Transform socket = BuildWiring.Node(second.transform, RoverModelNodes.DrumSocketRight);
                BuildWiring.Node(socket, DrumName).gameObject.SetActive(true);
                StringAssert.Contains($"{RoverModelNodes.DrumSocketRight}/{DrumName}: GameObject.m_IsActive",
                    HierarchyComparison.FirstDifference(first, second));
            }
        }

        [Test]
        public void SwappedDrums_AreADifference()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject first = RoverPrefabBuilder.Assemble(scratch);
                GameObject second = RoverPrefabBuilder.Assemble(scratch);
                var kit = new SerializedObject(second.GetComponentInChildren<RoverKit>(true));
                SerializedProperty drums = kit.FindProperty("_drums");
                SerializedProperty left = drums.GetArrayElementAtIndex(0);
                SerializedProperty right = drums.GetArrayElementAtIndex(1);
                Object leftDrum = left.objectReferenceValue;
                left.objectReferenceValue = right.objectReferenceValue;
                right.objectReferenceValue = leftDrum;
                kit.ApplyModifiedPropertiesWithoutUndo();
                StringAssert.Contains("RoverKit._drums", HierarchyComparison.FirstDifference(first, second));
            }
        }

        [Test]
        public void Headlamp_HasTunedDownTilt_AndCentreRayHitsFlatGroundFourToSevenMetresAhead()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<RoverRigTuning>(RoverAssetPaths.RigTuning);
            Assert.IsNotNull(tuning, $"{RoverAssetPaths.RigTuning} missing.");
            Assert.Greater(tuning.HeadlampDownTilt, 0f, "Headlamp must tilt down below horizontal.");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoverAssetPaths.RoverPrefab);
            Assert.IsNotNull(prefab, $"{RoverAssetPaths.RoverPrefab} missing: run the Rover builders.");

            Transform socket = BuildWiring.Node(prefab.transform, RoverModelNodes.HeadlampSocket);
            Transform headlamp = BuildWiring.Node(socket, "Headlamp");

            Vector3 forward = headlamp.forward;
            Assert.Less(forward.y, 0f, "Headlamp forward must pitch down toward the ground.");
            Assert.Greater(forward.z, 0f, "Headlamp forward must point ahead (+Z).");

            float pitchDownDeg = Mathf.Atan2(-forward.y, forward.z) * Mathf.Rad2Deg;
            Assert.AreEqual(tuning.HeadlampDownTilt, pitchDownDeg, 0.01f,
                "Built prefab's headlamp forward must have the tuned down-tilt.");

            float socketHeight = headlamp.position.y;
            Assert.Greater(socketHeight, 0f, "HeadlampSocket must sit above flat ground (y = 0).");

            float distanceAheadFromSocket = socketHeight / Mathf.Tan(tuning.HeadlampDownTilt * Mathf.Deg2Rad);
            float rayT = -headlamp.position.y / forward.y;
            Vector3 hitPoint = headlamp.position + forward * rayT;

            Assert.That(distanceAheadFromSocket, Is.InRange(4f, 7f),
                "Centre ray horizontal reach from HeadlampSocket must land within 4-7 m on flat ground.");
            Assert.That(hitPoint.z, Is.InRange(4f, 7f),
                "Centre ray flat-ground hit Z ahead of 07 must land within 4-7 m.");
        }

        [Test]
        public void LampGlasses_OnRoverAndLampBar_FaceWithinThreeDegreesOfHeadlampForward()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoverAssetPaths.RoverPrefab);
            Assert.IsNotNull(prefab, $"{RoverAssetPaths.RoverPrefab} missing: run the Rover builders.");

            Transform headlampSocket = BuildWiring.Node(prefab.transform, RoverModelNodes.HeadlampSocket);
            Transform headlamp = BuildWiring.Node(headlampSocket, "Headlamp");
            Vector3 lightForward = headlamp.forward.normalized;

            // 1. Base rover headlamp glasses (Glass_L and Glass_R merged into Body's R1_LampAmber submesh at z > 0.5m)
            Transform bodyNode = BuildWiring.Node(prefab.transform, RoverModelNodes.Body);
            var bodyMf = bodyNode.GetComponent<MeshFilter>();
            var bodyMr = bodyNode.GetComponent<MeshRenderer>();
            Assert.IsNotNull(bodyMf);
            Assert.IsNotNull(bodyMr);

            Mesh bodyMesh = bodyMf.sharedMesh;
            Material[] bodyMats = bodyMr.sharedMaterials;
            int amberSubmesh = -1;
            for (int s = 0; s < bodyMats.Length; s++)
            {
                if (bodyMats[s] != null && bodyMats[s].name.Contains("R1_LampAmber"))
                {
                    amberSubmesh = s;
                    break;
                }
            }
            Assert.GreaterOrEqual(amberSubmesh, 0, "Body must have an R1_LampAmber submesh for headlamp glasses.");

            Vector3[] bodyVerts = bodyMesh.vertices;
            int[] amberTris = bodyMesh.GetTriangles(amberSubmesh);
            Vector3 leftGlassNormalSum = Vector3.zero;
            Vector3 rightGlassNormalSum = Vector3.zero;

            for (int t = 0; t < amberTris.Length; t += 3)
            {
                Vector3 v0 = bodyNode.TransformPoint(bodyVerts[amberTris[t]]);
                Vector3 v1 = bodyNode.TransformPoint(bodyVerts[amberTris[t + 1]]);
                Vector3 v2 = bodyNode.TransformPoint(bodyVerts[amberTris[t + 2]]);
                Vector3 center = (v0 + v1 + v2) / 3f;
                if (center.z < 0.50f)
                {
                    continue;
                }

                Vector3 areaNormal = Vector3.Cross(v1 - v0, v2 - v0);
                if (areaNormal.z <= 0f)
                {
                    continue;
                }

                if (center.x < 0f)
                {
                    leftGlassNormalSum += areaNormal;
                }
                else
                {
                    rightGlassNormalSum += areaNormal;
                }
            }

            Assert.Greater(leftGlassNormalSum.sqrMagnitude, 1e-8f, "Glass_L front dome triangles not found.");
            Assert.Greater(rightGlassNormalSum.sqrMagnitude, 1e-8f, "Glass_R front dome triangles not found.");

            float leftAngle = Vector3.Angle(leftGlassNormalSum.normalized, lightForward);
            float rightAngle = Vector3.Angle(rightGlassNormalSum.normalized, lightForward);
            Assert.LessOrEqual(leftAngle, 3f, $"Glass_L facing direction deviated {leftAngle:F2}° from Headlamp forward.");
            Assert.LessOrEqual(rightAngle, 3f, $"Glass_R facing direction deviated {rightAngle:F2}° from Headlamp forward.");

            // 2. Kit_LampBar glasses (Lamp_0, Lamp_1, Lamp_2 under HeadlampSocket/Kit_LampBar)
            Transform lampBar = BuildWiring.Node(headlampSocket, "Kit_LampBar");
            foreach (string lampName in new[] { "Lamp_0", "Lamp_1", "Lamp_2" })
            {
                Transform lampNode = BuildWiring.Node(lampBar, lampName);
                var mf = lampNode.GetComponent<MeshFilter>();
                Assert.IsNotNull(mf, $"{lampName} missing MeshFilter.");
                Mesh m = mf.sharedMesh;
                Vector3[] verts = m.vertices;
                int[] tris = m.triangles;
                Vector3 domeNormalSum = Vector3.zero;
                for (int t = 0; t < tris.Length; t += 3)
                {
                    Vector3 v0 = lampNode.TransformPoint(verts[tris[t]]);
                    Vector3 v1 = lampNode.TransformPoint(verts[tris[t + 1]]);
                    Vector3 v2 = lampNode.TransformPoint(verts[tris[t + 2]]);
                    Vector3 areaNormal = Vector3.Cross(v1 - v0, v2 - v0);
                    if (areaNormal.normalized.z > 0.2f)
                    {
                        domeNormalSum += areaNormal;
                    }
                }

                Assert.Greater(domeNormalSum.sqrMagnitude, 1e-8f, $"{lampName} front dome triangles not found.");
                float angle = Vector3.Angle(domeNormalSum.normalized, lightForward);
                Assert.LessOrEqual(angle, 3f, $"{lampName} facing direction deviated {angle:F2}° from Headlamp forward.");
            }
        }

        [Test]
        public void ScreenBezel_OnHead_ExposesSingleMaterialSlotDefaultingToCreamEnamel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoverAssetPaths.RoverPrefab);
            Assert.IsNotNull(prefab, $"{RoverAssetPaths.RoverPrefab} missing: run the Rover builders.");

            Transform head = BuildWiring.Node(prefab.transform, RoverModelNodes.Head);
            Transform bezel = BuildWiring.Node(head, "ScreenBezel");
            var mr = bezel.GetComponent<MeshRenderer>();
            Assert.IsNotNull(mr, "ScreenBezel must have a MeshRenderer.");
            Assert.AreEqual(1, mr.sharedMaterials.Length, "ScreenBezel must expose a single material slot for data-driven bezel switching.");
            Assert.IsNotNull(mr.sharedMaterial, "ScreenBezel default material must be assigned.");
            StringAssert.Contains("R1_CreamEnamel", mr.sharedMaterial.name,
                "ScreenBezel must default to Variant A (R1_CreamEnamel).");
        }
    }
}
