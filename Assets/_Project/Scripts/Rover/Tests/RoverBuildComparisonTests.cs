using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;
using MoonProject.Rover.Editor;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// On 07's real build (the generated art and tuning on disk), the comparison the Rover/Rover builder trusts before
    /// skipping a save: two builds are alike, and changes inside the nested art prefab are seen, including which of
    /// the wheels (instances of one node) is which.
    /// </summary>
    public sealed class RoverBuildComparisonTests
    {
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
        public void AShownHiddenNode_IsADifference()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject first = RoverPrefabBuilder.Assemble(scratch);
                GameObject second = RoverPrefabBuilder.Assemble(scratch);
                Transform wing = BuildWiring.Node(second.transform, RoverModelNodes.SolarWing);
                BuildWiring.Node(wing, RoverModelNodes.CellFilled).gameObject.SetActive(true);
                StringAssert.Contains($"{RoverModelNodes.CellFilled}: GameObject.m_IsActive",
                    HierarchyComparison.FirstDifference(first, second));
            }
        }

        [Test]
        public void SwappedWheels_AreADifference()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject first = RoverPrefabBuilder.Assemble(scratch);
                GameObject second = RoverPrefabBuilder.Assemble(scratch);
                var rig = new SerializedObject(second.GetComponentInChildren<RoverVisualRig>(true));
                SerializedProperty wheels = rig.FindProperty("_wheels");
                SerializedProperty left = wheels.GetArrayElementAtIndex(0);
                SerializedProperty right = wheels.GetArrayElementAtIndex(1);
                Object leftWheel = left.objectReferenceValue;
                left.objectReferenceValue = right.objectReferenceValue;
                right.objectReferenceValue = leftWheel;
                rig.ApplyModifiedPropertiesWithoutUndo();
                StringAssert.Contains("RoverVisualRig._wheels", HierarchyComparison.FirstDifference(first, second));
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
        public void WheelFx_HasFourTracks_AndBallisticNonGrowingDustSystems()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject rover = RoverPrefabBuilder.Assemble(scratch);
                Transform wheelFx = BuildWiring.Node(rover.transform, "WheelFx");
                RoverTrackRenderer[] tracks = wheelFx.GetComponentsInChildren<RoverTrackRenderer>(true);
                Assert.AreEqual(4, tracks.Length, "All 4 wheels must have track renderers.");

                foreach (string name in new[] { "DustLeft", "DustRight", "LandingDust" })
                {
                    ParticleSystem ps = BuildWiring.Node(wheelFx, name).GetComponent<ParticleSystem>();
                    Assert.IsFalse(ps.limitVelocityOverLifetime.enabled, $"{name} must not have atmospheric air drag.");
                    if (ps.sizeOverLifetime.enabled)
                    {
                        AnimationCurve sizeCurve = ps.sizeOverLifetime.size.curve;
                        Assert.LessOrEqual(sizeCurve.Evaluate(1f), sizeCurve.Evaluate(0.1f) + 1e-3f,
                            $"{name} grains must not swell/grow in vacuum.");
                    }
                }
            }
        }

        [Test]
        public void LampGlasses_OnRover_FaceWithinThreeDegreesOfHeadlampForward()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject rover = RoverPrefabBuilder.Assemble(scratch);
                Transform headlampSocket = BuildWiring.Node(rover.transform, RoverModelNodes.HeadlampSocket);
                Transform headlamp = BuildWiring.Node(headlampSocket, "Headlamp");
                Vector3 lightForward = headlamp.forward.normalized;

                // Base rover headlamp glasses (Glass_L and Glass_R merged into Body's R1_LampAmber submesh at z > 0.5m)
                Transform bodyNode = BuildWiring.Node(rover.transform, RoverModelNodes.Body);
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

            }
        }

        [Test]
        public void ScreenBezel_OnHead_ExposesSingleMaterialSlotDefaultingToCreamEnamel()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject rover = RoverPrefabBuilder.Assemble(scratch);
                Transform head = BuildWiring.Node(rover.transform, RoverModelNodes.Head);
                Transform bezel = BuildWiring.Node(head, "ScreenBezel");
                var mr = bezel.GetComponent<MeshRenderer>();
                Assert.IsNotNull(mr, "ScreenBezel must have a MeshRenderer.");
                Assert.AreEqual(1, mr.sharedMaterials.Length, "ScreenBezel must expose a single material slot for data-driven bezel switching.");
                Assert.IsNotNull(mr.sharedMaterial, "ScreenBezel default material must be assigned.");
                StringAssert.Contains("R1_CreamEnamel", mr.sharedMaterial.name,
                    "ScreenBezel must default to Variant A (R1_CreamEnamel).");
            }
        }

        [Test]
        public void CutterNozzle_IsBuiltUnderCutterOriginWithR1Materials_AndFailsFastWhenUnwired()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject rover = RoverPrefabBuilder.Assemble(scratch);
                Transform cutterOrigin = BuildWiring.Node(rover.transform, RoverModelNodes.CutterOrigin);
                Assert.IsNotNull(cutterOrigin, "Rover must have CutterOrigin.");

                for (int i = 0; i < RoverModelNodes.NozzleBladeCount; i++)
                {
                    Transform blade = cutterOrigin.Find(RoverModelNodes.NozzleBlade(i));
                    Assert.IsNotNull(blade, $"Missing {RoverModelNodes.NozzleBlade(i)} under CutterOrigin.");
                    var bladeRenderer = blade.GetComponent<MeshRenderer>();
                    Assert.IsNotNull(bladeRenderer);
                    Assert.IsNotNull(bladeRenderer.sharedMaterial);
                    StringAssert.Contains("R1_BareSteel", bladeRenderer.sharedMaterial.name);
                }

                Transform core = cutterOrigin.Find(RoverModelNodes.NozzleHotCore);
                Assert.IsNotNull(core, "Missing NozzleHotCore under CutterOrigin.");
                var coreRenderer = core.GetComponent<MeshRenderer>();
                Assert.IsNotNull(coreRenderer);
                Assert.IsNotNull(coreRenderer.sharedMaterial);
                StringAssert.Contains("R1_LampAmber", coreRenderer.sharedMaterial.name);

                Transform spillNode = cutterOrigin.Find(RoverModelNodes.NozzleSpillLight);
                Assert.IsNotNull(spillNode, "Missing NozzleSpillLight under CutterOrigin.");
                var spillLight = spillNode.GetComponent<Light>();
                Assert.IsNotNull(spillLight);
                Assert.AreEqual(LightShadows.None, spillLight.shadows);

                var controller = rover.GetComponent<RoverController>();
                Assert.IsNotNull(controller.NozzleDevice, "RoverController.NozzleDevice must be wired at build time.");
            }
        }
    }
}
