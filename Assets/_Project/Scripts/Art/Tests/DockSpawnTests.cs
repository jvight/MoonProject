using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonProject.Art.Editor;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// 07 wakes on its dock (VISION ruling 8, cutscenes O1-O4): in the built Main scene the lander's DockAnchor stands
    /// where 07 spawns, facing the way it first looks. Reads Main.unity as the scene build last wrote it.
    /// </summary>
    public sealed class DockSpawnTests
    {
        private const float Reach = 0.2f;
        private const float Turn = 3f;

        private Scene _scene;

        [SetUp]
        public void OpenMain()
        {
            _scene = EditorSceneManager.OpenScene(MainSceneBuilder.ScenePath, OpenSceneMode.Additive);
        }

        [TearDown]
        public void CloseMain()
        {
            EditorSceneManager.CloseScene(_scene, true);
        }

        [Test]
        public void Dock_StandsOn07sSpawn_FacingItsFirstLook()
        {
            Transform rover = Root(SceneBuildContext.RoverRootName).GetChild(0);
            Transform dock = Find(Root(SceneBuildContext.GameplayRootName), "DockAnchor");
            var gap = new Vector2(dock.position.x - rover.position.x, dock.position.z - rover.position.z);
            Assert.Less(gap.magnitude, Reach, $"the dock at {dock.position} stands on 07's spawn at {rover.position}");
            float turn = Mathf.DeltaAngle(dock.eulerAngles.y, rover.eulerAngles.y);
            Assert.Less(Mathf.Abs(turn), Turn, $"07 rests facing {dock.eulerAngles.y}, it spawns facing " +
                                                $"{rover.eulerAngles.y}");
            Transform lander = Find(Root(SceneBuildContext.GameplayRootName), BaseModelBuilder.LanderName);
            Vector3 local = lander.InverseTransformPoint(dock.position);
            Assert.Greater(local.z, 0f, "the dock stands in front of the lander's belly");
        }

        private Transform Root(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root.transform;
                }
            }

            Assert.Fail($"Main.unity has no {name} root: run the scene build first.");
            return null;
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            Assert.Fail($"{root.name} has no {name} in Main.unity.");
            return null;
        }
    }
}
