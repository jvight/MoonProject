using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Editor.Automation
{
    /// <summary>
    /// Screenshot automation. Needs a GPU: never pass --nographics to these runs.
    /// <code>
    /// # Turntable: 6 views (hero 3/4, front, right, back, left, top) + a contact sheet per prefab
    /// python tools/unity_batch.py exec --method MoonProject.Editor.Automation.CaptureAutomation.CaptureTurntable
    ///     --arg prefab=Assets/_Project/Generated/Art/Rocks/Rock_01.prefab   (';'-separated list allowed)
    ///     --arg folder=Assets/_Project/Generated/Art                        (every prefab below, alternative)
    ///     [--arg size=640] [--arg floor=false]
    /// # Scene: one PNG per pose of a JSON pose file (see CameraPoseSet)
    /// python tools/unity_batch.py exec --method MoonProject.Editor.Automation.CaptureAutomation.CaptureScene
    ///     --arg poses=Assets/_Project/Scripts/World/Editor/Captures/overview.json
    ///     [--arg scene=Assets/_Project/Scenes/Main.unity]
    /// </code>
    /// PNGs land in the run's output folder (Logs/batch/&lt;stamp&gt;-exec-.../turntables or scene/); each written file
    /// is announced with a "[moon] capture" line.
    /// </summary>
    public static class CaptureAutomation
    {
        public const string MainScenePath = "Assets/_Project/Scenes/Main.unity";

        private const int DefaultTurntableSize = 640;
        private const int DefaultSceneWidth = 1600;
        private const int DefaultSceneHeight = 900;
        private const float DefaultSceneFov = 50f;
        private const float SceneNearClip = 0.1f;
        private const float SceneFarClip = 5000f;
        private const int SheetColumns = 3;
        private const int WarmUpSize = 64;

        private static readonly TurntableView[] Views =
        {
            new TurntableView("hero", 35f, 22f, 1f),
            new TurntableView("front", 0f, 12f, 1f),
            new TurntableView("right", 90f, 12f, 1f),
            new TurntableView("back", 180f, 12f, 1f),
            new TurntableView("left", 270f, 12f, 1f),
            new TurntableView("top", 0f, 89f, 1f),
        };

        public static void CaptureTurntable()
        {
            BatchRunner.Run(nameof(CaptureTurntable), args =>
            {
                List<string> prefabPaths = CollectPrefabs(args);
                int size = args.GetInt("size", DefaultTurntableSize);
                bool floor = args.GetBool("floor", true);
                string outputDirectory = Path.Combine(args.OutputDirectory, "turntables");
                foreach (string prefabPath in prefabPaths)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null)
                    {
                        Debug.LogError($"CaptureTurntable: no prefab at {prefabPath}");
                        continue;
                    }

                    Turntable(prefab, outputDirectory, size, floor);
                }

                return prefabPaths.Count > 0;
            });
        }

        public static void CaptureScene()
        {
            BatchRunner.Run(nameof(CaptureScene), args =>
            {
                string scenePath = args.GetString("scene", MainScenePath);
                string posesPath = BatchArgs.ProjectPath(args.GetRequiredString("poses"));
                CameraPoseSet poses = JsonUtility.FromJson<CameraPoseSet>(File.ReadAllText(posesPath));
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                {
                    Debug.LogError($"CaptureScene: no scene at {scenePath}");
                    return false;
                }

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                string outputDirectory = Path.Combine(args.OutputDirectory, "scene");
                return CapturePoses(poses, outputDirectory, Path.GetFileNameWithoutExtension(scenePath)) > 0;
            });
        }

        /// <summary>
        /// Renders <paramref name="prefab"/> in a fresh scene with the <see cref="StudioRig"/> from six views and
        /// writes &lt;name&gt;/&lt;view&gt;.png plus &lt;name&gt;_sheet.png. Returns the contact sheet path.
        /// </summary>
        public static string Turntable(GameObject prefab, string outputDirectory, int size, bool floor)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            using (new SynchronousShaders())
            using (var rig = new StudioRig(floor))
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Bounds bounds = RendererBounds(instance, prefab.name);
                string folder = Path.Combine(outputDirectory, prefab.name);
                var sheet = new Texture2D(size * SheetColumns, size * ((Views.Length + SheetColumns - 1) / SheetColumns),
                    TextureFormat.RGBA32, false, false);
                FillSheetBackground(sheet);
                try
                {
                    rig.Frame(bounds, Views[0].Yaw, Views[0].Elevation, Views[0].DistanceScale);
                    Object.DestroyImmediate(FrameCapture.Render(rig.Camera, WarmUpSize, WarmUpSize));
                    for (int i = 0; i < Views.Length; i++)
                    {
                        TurntableView view = Views[i];
                        rig.Frame(bounds, view.Yaw, view.Elevation, view.DistanceScale);
                        Texture2D image = FrameCapture.Render(rig.Camera, size, size);
                        try
                        {
                            FrameCapture.WritePng(image, Path.Combine(folder, view.Name + ".png"));
                            int column = i % SheetColumns;
                            int rowFromTop = i / SheetColumns;
                            int rows = sheet.height / size;
                            sheet.SetPixels32(column * size, (rows - 1 - rowFromTop) * size, size, size,
                                image.GetPixels32());
                        }
                        finally
                        {
                            Object.DestroyImmediate(image);
                        }
                    }

                    sheet.Apply(false);
                    string sheetPath = Path.Combine(outputDirectory, prefab.name + "_sheet.png");
                    FrameCapture.WritePng(sheet, sheetPath);
                    BatchRunner.Log($"capture {sheetPath}");
                    return sheetPath;
                }
                finally
                {
                    Object.DestroyImmediate(sheet);
                    Object.DestroyImmediate(instance);
                }
            }
        }

        /// <summary>
        /// Renders every pose of <paramref name="poses"/> in the currently open scene(s) to
        /// &lt;outputDirectory&gt;/&lt;prefix&gt;_&lt;pose&gt;.png. Invalid poses are logged as errors. Returns the number written.
        /// </summary>
        public static int CapturePoses(CameraPoseSet poses, string outputDirectory, string prefix)
        {
            if (poses?.poses == null || poses.poses.Length == 0)
            {
                Debug.LogError("CaptureScene: the pose file has no poses.");
                return 0;
            }

            int width = poses.width > 0 ? poses.width : DefaultSceneWidth;
            int height = poses.height > 0 ? poses.height : DefaultSceneHeight;
            var cameraObject = new GameObject("[CaptureCamera]") { hideFlags = HideFlags.HideAndDontSave };
            int written = 0;
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.nearClipPlane = SceneNearClip;
                camera.farClipPlane = SceneFarClip;
                camera.allowHDR = true;
                UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
                cameraData.renderPostProcessing = poses.postProcessing;
                cameraData.antialiasing = AntialiasingMode.None;
                cameraData.renderShadows = true;
                using (new SynchronousShaders())
                {
                    foreach (CameraPose pose in poses.poses)
                    {
                        if (!TryApplyPose(camera, pose))
                        {
                            continue;
                        }

                        string path = Path.Combine(outputDirectory, $"{prefix}_{pose.name}.png");
                        FrameCapture.SavePng(camera, width, height, path);
                        BatchRunner.Log($"capture {path}");
                        written++;
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }

            return written;
        }

        private static bool TryApplyPose(Camera camera, CameraPose pose)
        {
            if (pose == null || string.IsNullOrWhiteSpace(pose.name) || pose.position == null ||
                pose.position.Length != 3)
            {
                Debug.LogError($"CaptureScene: pose '{pose?.name}' needs a name and a 3-number position.");
                return false;
            }

            var position = new Vector3(pose.position[0], pose.position[1], pose.position[2]);
            camera.transform.position = position;
            if (pose.lookAt != null && pose.lookAt.Length == 3)
            {
                var target = new Vector3(pose.lookAt[0], pose.lookAt[1], pose.lookAt[2]);
                camera.transform.rotation = Quaternion.LookRotation(target - position, Vector3.up);
            }
            else if (pose.euler != null && pose.euler.Length == 3)
            {
                camera.transform.rotation = Quaternion.Euler(pose.euler[0], pose.euler[1], pose.euler[2]);
            }
            else
            {
                Debug.LogError($"CaptureScene: pose '{pose.name}' needs lookAt or euler (3 numbers).");
                return false;
            }

            camera.fieldOfView = pose.fov > 0f ? pose.fov : DefaultSceneFov;
            return true;
        }

        private static List<string> CollectPrefabs(BatchArgs args)
        {
            var paths = new List<string>(args.GetList("prefab"));
            foreach (string folder in args.GetList("folder"))
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Debug.LogError($"CaptureTurntable: no folder {folder}");
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                var found = new List<string>(guids.Length);
                foreach (string guid in guids)
                {
                    found.Add(AssetDatabase.GUIDToAssetPath(guid));
                }

                found.Sort(StringComparer.Ordinal);
                paths.AddRange(found);
            }

            if (paths.Count == 0)
            {
                Debug.LogError("CaptureTurntable: pass --arg prefab=Assets/...prefab or --arg folder=Assets/...");
            }

            return paths;
        }

        private static Bounds RendererBounds(GameObject instance, string label)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            bool any = false;
            var bounds = new Bounds();
            foreach (Renderer renderer in renderers)
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                {
                    continue;
                }

                if (any)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    any = true;
                }
            }

            if (!any)
            {
                throw new InvalidOperationException($"{label} has no MeshRenderer/SkinnedMeshRenderer to capture.");
            }

            return bounds;
        }

        private static void FillSheetBackground(Texture2D sheet)
        {
            var pixels = new Color32[sheet.width * sheet.height];
            Color32 background = StudioRig.Background;
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = background;
            }

            sheet.SetPixels32(pixels);
        }

        private readonly struct TurntableView
        {
            public TurntableView(string name, float yaw, float elevation, float distanceScale)
            {
                Name = name;
                Yaw = yaw;
                Elevation = elevation;
                DistanceScale = distanceScale;
            }

            public string Name { get; }

            public float Yaw { get; }

            public float Elevation { get; }

            public float DistanceScale { get; }
        }

        /// <summary>Forces synchronous shader compilation so captures never show the cyan placeholder shader.</summary>
        private sealed class SynchronousShaders : IDisposable
        {
            private readonly bool _previous;

            public SynchronousShaders()
            {
                _previous = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = false;
            }

            public void Dispose()
            {
                ShaderUtil.allowAsyncCompilation = _previous;
            }
        }
    }
}
