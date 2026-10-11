using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MoonProject.Editor.Automation;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Captures 08 beside 07 in the Main scene's own light and grade (the finale's pairing, docs/design/gdd-v3.md
    /// §13.3): a chase frame from behind, as the game's default camera sees 07, and a three-quarter frame from 30 m,
    /// where silhouettes must still read (VISION pillar 5). Nothing is saved; the scene is only rendered.
    /// <code>python tools/unity_batch.py exec --method MoonProject.Art.Editor.RoverFamilyCapture.CaptureBeside07</code>
    /// </summary>
    public static class RoverFamilyCapture
    {
        private const string RoverPrefabPath = ArtPaths.RoverFolder + "/" + RoverModelBuilder.ModelName + ".prefab";

        /// <summary>A level stretch of the basin floor away from home and its own 07 (world x, z).</summary>
        private static readonly Vector2 PairSpot = new Vector2(-20f, -30f);

        private const float PairSpacing = 2.4f;
        private const float RayHeight = 400f;
        private const float GameFov = 60f;
        private const float FarDistance = 30f;
        private const float FarYawDegrees = 35f;
        private const float FarHeight = 4f;
        private static readonly Vector3 ChaseOffset = new Vector3(0.8f, 3.6f, -8.5f);
        private static readonly Vector3 ChaseLookAhead = new Vector3(0f, 0.8f, 6f);
        private static readonly Vector3 PairCentreHeight = new Vector3(0f, 0.8f, 0f);

        public static void CaptureBeside07()
        {
            BatchRunner.Run(nameof(CaptureBeside07), args =>
            {
                EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
                if (!TryGround(PairSpot, out Vector3 centre))
                {
                    Debug.LogError($"RoverFamilyCapture: no ground under {PairSpot} in the Main scene.");
                    return false;
                }

                if (!Place(RoverPrefabPath, centre + Vector3.left * (PairSpacing * 0.5f))
                    || !Place(Rover08ModelBuilder.PrefabPath, centre + Vector3.right * (PairSpacing * 0.5f)))
                {
                    return false;
                }

                float yaw = FarYawDegrees * Mathf.Deg2Rad;
                var far = new Vector3(Mathf.Sin(yaw) * FarDistance, FarHeight, Mathf.Cos(yaw) * FarDistance);
                var poses = new CameraPoseSet
                {
                    poses = new[]
                    {
                        Pose("chase", centre + ChaseOffset, centre + ChaseLookAhead),
                        Pose("30m", centre + far, centre + PairCentreHeight),
                    },
                };
                string folder = Path.Combine(args.OutputDirectory, "scene");
                return CaptureAutomation.CapturePoses(poses, folder, "rover07_08") == poses.poses.Length;
            });
        }

        private static bool TryGround(Vector2 spot, out Vector3 ground)
        {
            Physics.SyncTransforms();
            var origin = new Vector3(spot.x, RayHeight, spot.y);
            bool hit = Physics.Raycast(origin, Vector3.down, out RaycastHit info, RayHeight * 2f);
            ground = hit ? info.point : origin;
            return hit;
        }

        private static bool Place(string prefabPath, Vector3 spot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"RoverFamilyCapture: no prefab at {prefabPath}; run the Art builders first.");
                return false;
            }

            if (!TryGround(new Vector2(spot.x, spot.z), out Vector3 ground))
            {
                Debug.LogError($"RoverFamilyCapture: no ground under {spot} for {prefabPath}.");
                return false;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = ground;
            return true;
        }

        private static CameraPose Pose(string name, Vector3 position, Vector3 lookAt)
        {
            return new CameraPose
            {
                name = name,
                position = new[] { position.x, position.y, position.z },
                lookAt = new[] { lookAt.x, lookAt.y, lookAt.z },
                fov = GameFov,
            };
        }
    }
}
