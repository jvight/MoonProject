using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Gameplay.Editor;
using MoonProject.Gameplay.PlayModeTests;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The scan's wireframe reaches its target (owner pick 2026-10-18, scan B "Wireframe reveal"): every target the
    /// game registers (its ruin sets' Scan_ nodes, its story scans and the lander's pad fragments) shows on a renderer
    /// whose every material can draw the scan, and a scan's progress and close are written to that renderer. A target
    /// that could not draw it says so at once.
    /// </summary>
    public sealed class ScanWireframeTests
    {
        /// <summary>Where Art's R1 ruin pieces are built.</summary>
        private const string RuinsFolder = "Assets/_Project/Generated/Art/R1/Models/Ruins";

        /// <summary>Where Art's R1 story props are built (the basin's story scans lie on them).</summary>
        private const string PropsFolder = "Assets/_Project/Generated/Art/R1/Models/Props";
        private const string RoverFolder = "Assets/_Project/Generated/Art/R1/Models/Rover";
        private const string WreckFolder = "Assets/_Project/Generated/Art/R1/Models/Wreck";

        private static readonly int ProgressId = Shader.PropertyToID("_ScanProgress");
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");

        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object made in _made)
            {
                Object.DestroyImmediate(made);
            }

            _made.Clear();
        }

        [Test]
        public void AScanOnAnR1Surface_WritesItsProgressAndCloseToItsRenderer()
        {
            var surface = Made(new Material(AssetDatabase.LoadAssetAtPath<Shader>(GameplayFixture.R1LitShaderPath)));
            Renderer shown = Part(surface);
            ScanFragment fragment = Fragment(shown.transform);

            Assert.IsNull(ScanFragment.WhyItCannotShowTheScan(shown));
            fragment.Draw(0.4f);
            Assert.AreEqual(0.4f, Block(shown).GetFloat(ProgressId), 1e-5f, "the sweep's progress");
            fragment.Dissolve(0.7f);
            Assert.AreEqual(0.7f, Block(shown).GetFloat(DissolveId), 1e-5f, "the close");
            Assert.AreEqual(0.4f, Block(shown).GetFloat(ProgressId), 1e-5f, "the close keeps the sweep");
        }

        [Test]
        public void AScanTargetOnASurfaceThatCannotDrawIt_SaysSoWithItsPath()
        {
            var glow = AssetDatabase.LoadAssetAtPath<Shader>(GameplayFixture.ShaderPath);
            Renderer shown = Part(Made(new Material(glow)));

            LogAssert.Expect(LogType.Error, new Regex(@"Scan target test\.0 \(Ruin/Scan_1\): material .* has no " +
                                                      "_ScanProgress/_Dissolve, so its scan would draw nothing"));
            Fragment(shown.transform);
            StringAssert.Contains("_ScanProgress", ScanFragment.WhyItCannotShowTheScan(shown));
        }

        [Test]
        public void AScanTargetWithNothingToShowIt_SaysSo()
        {
            var ruin = Made(new GameObject("Ruin"));
            var target = new GameObject(ScanFragment.RuinScanPrefix + "1").transform;
            target.SetParent(ruin.transform, false);

            LogAssert.Expect(LogType.Error, new Regex(@"Scan target test\.0 \(Ruin/Scan_1\): no renderer on its " +
                                                      "host shows it"));
            ScanFragment.ForRuinTarget("test.0", "test", Array.Empty<string>(), 0, 1, ScanSize.Small, "test", target,
                ruin.transform);
        }

        [Test]
        public void EveryScanTargetTheGameRegisters_ShowsOnASurfaceThatDrawsTheScan()
        {
            var scans = AssetDatabase.LoadAssetAtPath<ScanTuning>(GameplayAssetPaths.ScanTuning);
            var opening = AssetDatabase.LoadAssetAtPath<OpeningTuning>(GameplayAssetPaths.OpeningTuning);
            Assert.IsNotNull(scans, GameplayAssetPaths.ScanTuning);
            Assert.IsNotNull(opening, GameplayAssetPaths.OpeningTuning);
            int targets = 0;
            foreach (RuinScanSet set in scans.Ruins)
            {
                GameObject ruin = Instance(RuinsFolder + "/" + set.RuinId + ".prefab");
                for (int i = 0; i < set.Count; i++)
                {
                    Transform node = Descendant(ruin.transform, ScanFragment.RuinScanPrefix + (i + 1));
                    Assert.IsNotNull(node, $"{set.RuinId} has Scan_{i + 1}");
                    AssertShowsTheScan(ScanFragment.ForRuinTarget("ruin." + i, set.BlueprintId, set.StructureIds, i,
                        set.Count, set.Size(i), set.RuinId, node, ruin.transform), set.RuinId + "/" + node.name);
                    targets++;
                }
            }

            foreach (StoryScan story in scans.Stories)
            {
                GameObject ruin = Instance(StoryHostPrefab(story));
                Transform node = Descendant(ruin.transform, story.Node);
                Assert.IsNotNull(node, $"{story.HostId} has {story.Node}");
                var target = new GameObject(story.FragmentId).transform;
                target.SetParent(node, false);
                AssertShowsTheScan(ScanFragment.ForStoryTarget(story, story.HostId, target, ruin.transform),
                    story.FragmentId);
                targets++;
            }

            foreach (GameObject fragment in opening.CargoFragments)
            {
                GameObject instance = Made(Object.Instantiate(fragment));
                Transform node = Descendant(instance.transform, ScanFragment.RuinScanPrefix + "1");
                Assert.IsNotNull(node, $"{fragment.name} has Scan_1");
                AssertShowsTheScan(ScanFragment.ForRuinTarget("cargo." + targets, "cargo", Array.Empty<string>(), 0,
                    1, ScanSize.Small, fragment.name, node, instance.transform), fragment.name);
                targets++;
            }

            Assert.Greater(targets, 0, "the game registers scan targets");
        }

        private static void AssertShowsTheScan(ScanFragment fragment, string what)
        {
            Renderer solid = fragment.Solid;
            Assert.IsNull(ScanFragment.WhyItCannotShowTheScan(solid), what);
            fragment.Draw(0.5f);
            Assert.AreEqual(0.5f, Block(solid).GetFloat(ProgressId), 1e-5f, $"{what}'s progress reaches {solid.name}");
        }

        private GameObject Instance(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path);
            return Made(Object.Instantiate(prefab));
        }

        /// <summary>
        /// Art's prefab behind a story scan's host: a ruin piece, a basin prop, a machine (the elders), or the wreck
        /// part a World host (an empty parent) stands over, named after the scan's node.
        /// </summary>
        private static string StoryHostPrefab(StoryScan story)
        {
            string[] candidates =
            {
                RuinsFolder + "/" + story.HostId + ".prefab", PropsFolder + "/" + story.HostId + ".prefab",
                RoverFolder + "/" + story.HostId + ".prefab", WreckFolder + "/" + story.Node + ".prefab",
            };
            foreach (string path in candidates)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                {
                    return path;
                }
            }

            return candidates[1];
        }

        private static Transform Descendant(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            return null;
        }

        private Renderer Part(Material surface)
        {
            var ruin = Made(new GameObject("Ruin"));
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(ruin.transform, false);
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = surface;
            return renderer;
        }

        private static ScanFragment Fragment(Transform shown)
        {
            Transform ruin = shown.parent;
            var target = new GameObject(ScanFragment.RuinScanPrefix + "1").transform;
            target.SetParent(ruin, false);
            return ScanFragment.ForRuinTarget("test.0", "test", Array.Empty<string>(), 0, 1, ScanSize.Small, "test",
                target, ruin);
        }

        private static MaterialPropertyBlock Block(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block;
        }

        private T Made<T>(T made) where T : Object
        {
            _made.Add(made);
            return made;
        }
    }
}
