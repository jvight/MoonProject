using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonProject.Gameplay.Editor;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The editor's preview of what the game places at boot (<see cref="GameplayEditPreview"/>) on the built Main
    /// scene: the wrecks and broken masts stand in the basin once the scene is open, nothing of it is ever saved,
    /// building it again leaves one, and it steps aside for Play and comes back after.
    /// </summary>
    public sealed class GameplayEditPreviewTests
    {
        private const string MainScene = "Assets/_Project/Scenes/Main.unity";
        private const string SitePrefix = "Site_";
        private const string MastName = "RelayMast_Broken";

        /// <summary>Metres from the lander the nearest site lies at least (the supply depot, ~55 m from home).</summary>
        private const float SitesOutFromHome = 20f;

        private Scene _scene;

        [TearDown]
        public void CloseScene()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                EditorSceneManager.CloseScene(_scene, true);
            }
        }

        [Test]
        public void Preview_ShowsTheWrecksAndMasts_IsNeverSaved_BuildsOnce_AndStepsAsideForPlay()
        {
            _scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Additive);
            GameObject root = GameplayEditPreview.Root(_scene);
            Assert.IsNotNull(root, "opening the scene builds the preview");
            SalvageField salvage = Find<SalvageField>(_scene);
            HomeBase home = Find<HomeBase>(_scene);
            int sites = Count(root.transform, SitePrefix);
            int masts = Count(root.transform, MastName);
            Assert.AreEqual(salvage.Catalog.Sites.Count, sites, "every salvage site's wreck");
            Assert.Greater(masts, 0, "the broken relay masts");
            Assert.AreEqual(sites + masts, root.transform.childCount, "and nothing else");
            for (int i = 0; i < root.transform.childCount; i++)
            {
                Transform piece = root.transform.GetChild(i);
                Assert.Greater(Vector3.Distance(piece.position, home.LanderPosition), SitesOutFromHome,
                    $"{piece.name} stands out on its anchor in the basin");
            }

            AssertNeverSaved(root.transform);

            GameplayEditPreview.Rebuild(_scene);
            GameplayEditPreview.Ensure(_scene);
            Assert.AreEqual(1, Roots(_scene), "building again leaves one preview");
            Assert.AreEqual(sites + masts, GameplayEditPreview.Root(_scene).transform.childCount);

            GameplayEditPreview.OnPlayModeChanged(PlayModeStateChange.ExitingEditMode);
            Assert.IsNull(GameplayEditPreview.Root(_scene), "Play never sees the preview");
            GameplayEditPreview.OnPlayModeChanged(PlayModeStateChange.EnteredEditMode);
            Assert.AreEqual(1, Roots(_scene), "back once editing resumes");

            GameplayEditPreview.Remove(_scene);
            Assert.AreEqual(0, Roots(_scene));
        }

        [Test]
        public void Preview_LeavesAScene_WithoutGameplayAlone()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameplayEditPreview.Rebuild(scene);
            Assert.IsNull(GameplayEditPreview.Root(scene));
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

        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            Assert.Fail($"{scene.name} has no {typeof(T).Name}");
            return null;
        }
    }
}
