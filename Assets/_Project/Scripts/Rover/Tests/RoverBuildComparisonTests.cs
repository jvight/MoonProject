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
        public void GroundModel_ClassifiesRegolithRockAndMetal_AndSelectsSurfaceSoundHooks()
        {
            Assert.AreEqual(GroundSurface.Regolith, GroundModel.ClassifySurface("TerrainChunk_0_0", Core.Layers.Ground));
            Assert.AreEqual(GroundSurface.Rock, GroundModel.ClassifySurface("Boulder", Core.Layers.Prop));
            Assert.AreEqual(GroundSurface.Metal, GroundModel.ClassifySurface("Site_KestrelPad_Deck", Core.Layers.Prop));

            Assert.AreEqual("regolith", GroundModel.SurfaceSoundHook(GroundSurface.Regolith));
            Assert.AreEqual("rock", GroundModel.SurfaceSoundHook(GroundSurface.Rock));
            Assert.AreEqual("metal", GroundModel.SurfaceSoundHook(GroundSurface.Metal));
        }
    }
}

