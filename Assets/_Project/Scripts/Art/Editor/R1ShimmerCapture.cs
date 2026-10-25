using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MoonProject.Editor.Automation;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Consecutive-frame capture of 07 turning slowly under the night earthlight, through the game's own camera
    /// settings (post-processing with the World volume's bloom, no post anti-aliasing, MSAA as the URP asset sets it),
    /// so specular sparkle on its small lenses and rims shows up as frame-to-frame flicker. Frames go to
    /// <c>Logs/captures/shimmer/&lt;tag&gt;/&lt;view&gt;_NN.png</c> for the chase view and a slow front three-quarter
    /// orbit; <c>--arg smaa=true</c> adds the camera's SMAA and <c>--arg clamp=N</c> clamps the bloom and <c>--arg msaa=N</c> overrides the URP asset's MSAA, to test
    /// them:
    /// <c>python tools/unity_batch.py exec --project . --method MoonProject.Art.Editor.R1ShimmerCapture.Capture --arg tag=current</c>
    /// </summary>
    public static class R1ShimmerCapture
    {
        private const string VolumeProfilePath = "Assets/_Project/Generated/World/WorldVolumeProfile.asset";
        private const int Width = 1920;
        private const int Height = 1080;
        private const int Frames = 30;
        private const float TurnDegreesPerFrame = 0.3f;
        private const float OrbitDegreesPerFrame = 0.4f;
        private const float ChaseDistance = 7.5f;
        private const float ChasePitch = 16f;
        private const float ChaseTargetHeight = 1.1f;
        private const float GameFov = 52f;

        /// <summary>Renders both frame sequences; see the class summary.</summary>
        public static void Capture()
        {
            BatchRunner.Run(nameof(Capture), args =>
            {
                AssetDatabase.Refresh();
                R1MaterialLibrary.BuildMaterials();
                R1ModelBuilder.Build();

                string tag = args.GetString("tag", "current");
                bool smaa = args.GetBool("smaa", false);
                float bloomClamp = args.GetFloat("clamp", 0f);
                int msaa = args.GetInt("msaa", Mathf.Max(1, UniversalRenderPipeline.asset.msaaSampleCount));
                string outDir = Path.GetFullPath("Logs/captures/shimmer/" + tag);
                Directory.CreateDirectory(outDir);

                bool prevAsync = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = false;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var temp = new ArtLightingPreview.TemporaryObjects();
                try
                {
                    SetUpNight(temp, bloomClamp);
                    var rover = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(R1RoverCapture.RoverPrefabPath));
                    temp.Add(rover);
                    var target = new Vector3(0f, ChaseTargetHeight, 0f);
                    for (int i = 0; i < Frames; i++)
                    {
                        rover.transform.rotation = Quaternion.Euler(0f, i * TurnDegreesPerFrame, 0f);
                        Vector3 back = Quaternion.Euler(ChasePitch, i * TurnDegreesPerFrame, 0f) * Vector3.back;
                        Render(target + back * ChaseDistance, target, smaa, Path.Combine(outDir, $"chase_{i:00}.png"), msaa);
                    }

                    rover.transform.rotation = Quaternion.identity;
                    var head = new Vector3(0f, 1.0f, 0.2f);
                    for (int i = 0; i < Frames; i++)
                    {
                        Vector3 eye = Quaternion.Euler(10f, 35f + i * OrbitDegreesPerFrame, 0f) * Vector3.forward * 3.6f;
                        Render(head + eye, head, smaa, Path.Combine(outDir, $"front34_{i:00}.png"), msaa);
                    }
                }
                finally
                {
                    temp.Dispose();
                    ShaderUtil.allowAsyncCompilation = prevAsync;
                }

                return true;
            });
        }

        private static void SetUpNight(ArtLightingPreview.TemporaryObjects temp, float bloomClamp)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.10f, 0.11f, 0.16f);
            RenderSettings.ambientEquatorColor = new Color(0.07f, 0.07f, 0.10f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.04f, 0.05f);

            Light earthlight = ArtLightingPreview.NewLight("Earthlight", LightType.Directional,
                new Color(0.86f, 0.90f, 1.0f), 1.3f);
            earthlight.transform.rotation = Quaternion.Euler(28f, 145f, 0f);
            earthlight.shadows = LightShadows.Soft;
            earthlight.GetUniversalAdditionalLightData().usePipelineSettings = false;
            earthlight.shadowBias = 1f;
            earthlight.shadowNormalBias = 0.5f;
            RenderSettings.sun = earthlight;
            temp.Add(earthlight.gameObject);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(8f, 1f, 8f);
            ground.GetComponent<Renderer>().sharedMaterial = R1MaterialLibrary.LoadMaterial(R1MaterialSlot.GreyMetal);
            temp.Add(ground);

            var volumeObject = new GameObject("WorldVolume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            VolumeProfile profile = Object.Instantiate(AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath));
            if (bloomClamp > 0f && profile.TryGet(out Bloom bloom))
            {
                bloom.clamp.Override(bloomClamp);
            }

            volume.sharedProfile = profile;
            temp.Add(volumeObject);
        }

        private static void Render(Vector3 position, Vector3 lookAt, bool smaa, string path, int msaa)
        {
            var camObject = new GameObject("[ShimmerCamera]") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.012f, 0.02f);
            cam.fieldOfView = GameFov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 2000f;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            cam.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position, Vector3.up));
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = smaa ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.None;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;

            var descriptor = new RenderTextureDescriptor(Width, Height, RenderTextureFormat.DefaultHDR, 24)
            {
                msaaSamples = msaa,
            };
            var rt = RenderTexture.GetTemporary(descriptor);
            var resolved = RenderTexture.GetTemporary(Width, Height, 0, RenderTextureFormat.DefaultHDR);
            RenderTexture previous = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                cam.Render();
                cam.targetTexture = null;
                Graphics.Blit(rt, resolved);
                RenderTexture.active = resolved;
                var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                tex.Apply(false);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(resolved);
                RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(camObject);
            }
        }
    }
}
