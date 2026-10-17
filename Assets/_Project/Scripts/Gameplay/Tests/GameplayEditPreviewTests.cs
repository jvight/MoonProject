using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonProject.Core;
using MoonProject.Editor.Builders;
using MoonProject.Gameplay.Editor;
using MoonProject.Gameplay.PlayModeTests;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The editor's preview of what the game places at boot (<see cref="GameplayEditPreview"/>). On a scratch scene
    /// with a stand-in world (<see cref="StandInWorldPreview"/>): each wreck stands level on its anchor and nothing
    /// else is placed, nothing of it is ever saved, building it again leaves one, and a scene without the gameplay or
    /// a generated world preview is left alone. On the built Main scene: the World's preview is found, every wreck
    /// stands on it, and the preview steps aside for Play and comes back.
    /// </summary>
    public sealed class GameplayEditPreviewTests
    {
        private const string MainScene = "Assets/_Project/Scenes/Main.unity";
        private const string SitePrefix = "Site_";

        /// <summary>Metres a placed piece may stand off its anchor: float noise only.</summary>
        private const float PositionTolerance = 1e-3f;

        /// <summary>Degrees a placed piece may turn off its anchor's level heading: float noise of an acos only.</summary>
        private const float AngleTolerance = 0.1f;

        private Scene _scene;
        private BuilderScratchScene _scratch;

        [TearDown]
        public void CloseScenes()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                EditorSceneManager.CloseScene(_scene, true);
            }

            _scene = default;
            _scratch?.Dispose();
            _scratch = null;
        }

        [Test]
        public void Preview_StandsEachWreck_LevelOnItsAnchor_AndNothingElse()
        {
            StandInWorldPreview world = NewStandInScene(out SalvageField salvage);
            world.Generate(StandInAnchors(salvage.Catalog));

            GameplayEditPreview.Rebuild(_scratch.Scene);

            GameObject root = GameplayEditPreview.Root(_scratch.Scene);
            Assert.IsNotNull(root, "a generated world preview builds the preview");
            IReadOnlyList<SalvageSiteEntry> sites = salvage.Catalog.Sites;
            Assert.AreEqual(sites.Count, root.transform.childCount, "a wreck on every site, nothing on other anchors");
            for (int i = 0; i < sites.Count; i++)
            {
                AssertStandsOn(root.transform.GetChild(i), sites[i].Prefab.name, world, sites[i].AnchorId);
            }

            AssertNeverSaved(root.transform);
        }

        [Test]
        public void Preview_BuildsOnce_AndRemovesCleanly()
        {
            StandInWorldPreview world = NewStandInScene(out SalvageField salvage);
            world.Generate(StandInAnchors(salvage.Catalog));
            Scene scene = _scratch.Scene;

            GameplayEditPreview.Rebuild(scene);
            int pieces = GameplayEditPreview.Root(scene).transform.childCount;
            GameplayEditPreview.Rebuild(scene);
            GameplayEditPreview.Ensure(scene);
            Assert.AreEqual(1, Roots(scene), "building again leaves one preview");
            Assert.AreEqual(pieces, GameplayEditPreview.Root(scene).transform.childCount);

            GameplayEditPreview.Remove(scene);
            Assert.AreEqual(0, Roots(scene));
        }

        [Test]
        public void Preview_WaitsForTheWorldToBeGenerated()
        {
            NewStandInScene(out _);

            GameplayEditPreview.Rebuild(_scratch.Scene);

            Assert.IsNull(GameplayEditPreview.Root(_scratch.Scene), "no anchors to stand on yet");
        }

        [Test]
        public void Preview_LeavesAScene_WithoutAWorldPreviewAlone()
        {
            NewGameplayScene();

            Assert.DoesNotThrow(() => GameplayEditPreview.Rebuild(_scratch.Scene));
            Assert.IsNull(GameplayEditPreview.Root(_scratch.Scene));
        }

        [Test]
        public void Preview_LeavesAScene_WithoutGameplayAlone()
        {
            _scratch = new BuilderScratchScene();
            var world = _scratch.Create("World").AddComponent<StandInWorldPreview>();
            world.Generate(new[] { Anchor(WorldAnchorIds.KestrelTrail, 0) });

            GameplayEditPreview.Rebuild(_scratch.Scene);

            Assert.IsNull(GameplayEditPreview.Root(_scratch.Scene));
        }

        [Test]
        public void Preview_OnTheBuiltMainScene_ShowsEveryWreck_AndStepsAsideForPlay()
        {
            _scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Additive);
            IWorldPreview world = Find<IWorldPreview>(_scene,
                "the World's scene system implements MoonProject.Core.IWorldPreview: the anchors the preview reads");
            Assert.IsTrue(world.HasGeneratedWorld, "the saved scene holds its generated world");
            GameObject root = GameplayEditPreview.Root(_scene);
            Assert.IsNotNull(root, "opening the scene builds the preview");
            SalvageField salvage = Find<SalvageField>(_scene, "the gameplay's salvage");
            int sites = Count(root.transform, SitePrefix);
            Assert.AreEqual(salvage.Catalog.Sites.Count, sites, "every salvage site's wreck");
            Assert.AreEqual(sites, root.transform.childCount, "and nothing else");
            foreach (SalvageSiteEntry site in salvage.Catalog.Sites)
            {
                AssertStandsOn(root.transform.Find(site.Prefab.name), site.Prefab.name, world, site.AnchorId);
            }

            AssertNeverSaved(root.transform);

            GameplayEditPreview.OnPlayModeChanged(PlayModeStateChange.ExitingEditMode);
            Assert.IsNull(GameplayEditPreview.Root(_scene), "Play never sees the preview");
            GameplayEditPreview.OnPlayModeChanged(PlayModeStateChange.EnteredEditMode);
            Assert.AreEqual(1, Roots(_scene), "back once editing resumes");
        }

        /// <summary>
        /// A scratch scene holding the gameplay's salvage field, wired to the built catalog: what the preview reads of
        /// the gameplay. Returns the salvage field.
        /// </summary>
        private SalvageField NewGameplayScene()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SalvageCatalog>(GameplayAssetPaths.SalvageCatalog);
            Assert.IsNotNull(catalog, $"{GameplayAssetPaths.SalvageCatalog} is built");
            _scratch = new BuilderScratchScene();
            var salvage = _scratch.Create("Salvage").AddComponent<SalvageField>();
            salvage.Wire(null, catalog);
            return salvage;
        }

        /// <summary>The gameplay's scratch scene and a stand-in world preview in it, not generated yet.</summary>
        private StandInWorldPreview NewStandInScene(out SalvageField salvage)
        {
            salvage = NewGameplayScene();
            return _scratch.Create("World").AddComponent<StandInWorldPreview>();
        }

        /// <summary>
        /// An anchor for every site of <paramref name="catalog"/>, each somewhere else on its own heading, plus a
        /// canyon and a trail anchor that are no site.
        /// </summary>
        private static List<WorldAnchor> StandInAnchors(SalvageCatalog catalog)
        {
            var anchors = new List<WorldAnchor>
            {
                new WorldAnchor(WorldAnchorIds.CanyonMouth, new Vector3(0f, 1f, 200f), Vector3.forward, 6f),
                new WorldAnchor(WorldAnchorIds.KestrelTrail, new Vector3(-60f, 0.5f, 30f), Vector3.right, 4f),
            };
            int index = 0;
            foreach (SalvageSiteEntry site in catalog.Sites)
            {
                anchors.Add(Anchor(site.AnchorId, index++));
            }

            return anchors;
        }

        /// <summary>
        /// The <paramref name="index"/>th stand-in anchor: its own place and heading, its forward tipped a little out
        /// of level so the preview has to level it.
        /// </summary>
        private static WorldAnchor Anchor(string id, int index)
        {
            Vector3 forward = Quaternion.Euler(-10f, 40f * index, 0f) * Vector3.forward;
            var position = new Vector3(30f + 25f * index, 0.5f * index, -40f + 10f * index);
            return new WorldAnchor(id, position, forward, 5f);
        }

        private static void AssertStandsOn(Transform piece, string prefabName, IWorldPreview world, string anchorId)
        {
            Assert.IsTrue(world.Anchors.TryGet(anchorId, out WorldAnchor anchor), $"the stand-in has {anchorId}");
            Assert.AreEqual(prefabName, piece.name, $"what stands on {anchorId}");
            Assert.Less(Vector3.Distance(anchor.Position, piece.position), PositionTolerance,
                $"{piece.name} stands on {anchorId}");
            Vector3 heading = Vector3.ProjectOnPlane(anchor.Forward, Vector3.up).normalized;
            Assert.Less(Vector3.Angle(heading, piece.forward), AngleTolerance,
                $"{piece.name} faces {anchorId}'s level heading");
            Assert.Less(Vector3.Angle(Vector3.up, piece.up), AngleTolerance, $"{piece.name} stands upright");
        }

        private static void AssertNeverSaved(Transform node)
        {
            Assert.AreEqual(HideFlags.HideAndDontSave, node.gameObject.hideFlags, node.name);
            for (int i = 0; i < node.childCount; i++)
            {
                AssertNeverSaved(node.GetChild(i));
            }
        }

        private static int Roots(Scene scene)
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                count += root.name == GameplayEditPreview.RootName ? 1 : 0;
            }

            return count;
        }

        private static int Count(Transform root, string prefix)
        {
            int count = 0;
            for (int i = 0; i < root.childCount; i++)
            {
                count += root.GetChild(i).name.StartsWith(prefix, System.StringComparison.Ordinal) ? 1 : 0;
            }

            return count;
        }

        private static T Find<T>(Scene scene, string role) where T : class
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            Assert.Fail($"{scene.name} has no {typeof(T).Name} ({role})");
            return null;
        }
    }
}
