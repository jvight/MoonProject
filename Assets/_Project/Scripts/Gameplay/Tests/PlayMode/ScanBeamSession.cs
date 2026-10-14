using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Rover;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Real Main-scene capture session for 07 scanning the garage's Cargo Cradle fragment (<c>rover.cargo_cradle.0</c>,
    /// Task SCAN-BEAM): boots <see cref="PlaythroughScene"/> with the garage's salvage pieces already cut (as in the
    /// playthrough when 07 scans the Cradle fragments), parks 07 on open ground at Kenji's garage with a clear
    /// chase-camera view of 07's lens, the scan beam and the fragment, holds E through the scan and captures 1920x1080
    /// frames at ~10%, 50%, 90% progress and mid-dissolve into <c>Logs/scan/</c>, together with pre-tonemapped linear
    /// HDR brightness metrics against the bloom threshold.
    /// </summary>
    [Category("ScanBeamSession")]
    public sealed class ScanBeamSession : InputTestFixture
    {
        private const string CargoCradleFirst = "rover.cargo_cradle.0";
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;
        private const float FrameTime = 1f / 60f;
        private const float WakeTimeout = 20f;
        private const float WakeEaseTimeout = 8f;
        private const float StickProbeSeconds = 0.3f;
        private const float LookStick = 0.8f;
        private const float SettleSeconds = 0.5f;
        private const float ParkClearance = 1.5f;
        private const float CameraBackCheck = 6.0f;
        private const float CameraHeightCheck = 2.2f;
        private const float Progress10 = 0.10f;
        private const float Progress50 = 0.50f;
        private const float Progress90 = 0.90f;
        private const float DissolveCaptureDelay = 0.45f;
        private const float ScanTimeout = 10f;
        private const float BloomThreshold = 1.25f;
        private const float LensApertureOffset = 0.56f;
        private const float ContactStandOff = 0.03f;
        private const float LensApertureRadiusPx = 10f;
        private const float BeamCorridorHalfWidthPx = 18f;
        private const float FragmentBoxPaddingPx = 2f;
        private const float CyanDominance = 1.05f;

        private static readonly float[] CandidateDistances = { 2.8f, 3.3f, 3.8f, 4.3f };
        private static readonly float[] CandidateYawOffsets = { -20f, 20f, -14f, 14f, 0f };

        private static readonly string ScanFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "scan"));

        private readonly List<string> _problems = new List<string>();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private RoverController _rover;
        private RoverCameraRig _cameraRig;
        private Camera _view;
        private Keyboard _keyboard;
        private Gamepad _gamepad;
        private IDisposable _awoke;
        private bool _awake;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            Application.logMessageReceived += OnLog;
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            Application.logMessageReceived -= OnLog;
            _awoke?.Dispose();
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator GarageCargoCradleScan_IsCapturedAt10_50_90_AndDissolve_FromChaseCamera()
        {
            yield return Boot();
            ClearGarageVerbsBeforeScan();
            yield return null;

            ScanField scans = _gameplay.Scans;
            ScanFragment fragment = scans.Find(CargoCradleFirst);
            Assert.IsNotNull(fragment, "the garage's first Cargo Cradle fragment lies on its socket");

            yield return PlaceRoverForCleanScanShot(fragment);
            yield return Wait(SettleSeconds);

            Assert.AreSame(fragment, scans.Candidate, "the Cargo Cradle fragment is the scan candidate");
            Assert.AreEqual(InteractionKind.Scan, _gameplay.Hints.Primary.Kind, "the Scan chip is offered");

            Directory.CreateDirectory(ScanFolder);
            string prefix = File.Exists(Path.Combine(ScanFolder, "before_10.png")) ? "after" : "before";
            var report = new StringBuilder();
            report.AppendLine($"# Scan Beam Capture Metrics ({prefix})");
            report.AppendLine();
            report.AppendLine($"- Fragment: `{fragment.FragmentId}` at {fragment.Point}");
            report.AppendLine($"- 07 position: {_rover.Position}, lens (`TetherOrigin`): {_rover.TetherOrigin.position}");
            report.AppendLine($"- Chase camera position: {_view.transform.position}, FOV: {_view.fieldOfView:F1}");
            report.AppendLine($"- Bloom threshold: {BloomThreshold.ToString("F2", CultureInfo.InvariantCulture)}");
            report.AppendLine();
            report.AppendLine("| Frame | Progress | Lens Max HDR | Mid-Beam Max HDR | Fragment Cyan Max HDR | Fragment Overall Max HDR | Lens Screen (px) | Target Screen (px) |");
            report.AppendLine("|---|---:|---:|---:|---:|---:|---|---|");

            Press(_keyboard.eKey);
            yield return Until(() => scans.Scanning == fragment, 2f, "holding E begins scanning the fragment");

            yield return Until(() => fragment.Progress >= Progress10, ScanTimeout, "scan reaches 10%");
            CaptureAndMeasure(prefix, "10", fragment, report);

            yield return Until(() => fragment.Progress >= Progress50, ScanTimeout, "scan reaches 50%");
            CaptureAndMeasure(prefix, "50", fragment, report);

            yield return Until(() => fragment.Progress >= Progress90, ScanTimeout, "scan reaches 90%");
            CaptureAndMeasure(prefix, "90", fragment, report);

            yield return Until(() => fragment.Scanned, ScanTimeout, "scan completes and begins dissolving");
            yield return Wait(DissolveCaptureDelay);
            CaptureAndMeasure(prefix, "dissolve", fragment, report);

            Release(_keyboard.eKey);
            yield return null;

            string reportPath = Path.Combine(ScanFolder, $"{prefix}_metrics.md");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[moon] " + report.ToString().Replace("\r\n", " | ").Replace("\n", " | "));
            Assert.IsEmpty(_problems, "no errors in log:\n" + string.Join("\n", _problems));
        }

        private IEnumerator Boot()
        {
#if UNITY_EDITOR
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            Type.GetType("MoonProject.Art.Editor.FragmentBuilder, MoonProject.Art.Editor")
                ?.GetMethod("Build")?.Invoke(null, null);
            Type.GetType("MoonProject.Art.Editor.RoverModelBuilder, MoonProject.Art.Editor")
                ?.GetMethod("Build")?.Invoke(null, null);
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("ScanBeamSession loads the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
                _rover = _rover != null ? _rover : root.GetComponentInChildren<RoverController>(true);
                _cameraRig = _cameraRig != null ? _cameraRig : root.GetComponentInChildren<RoverCameraRig>(true);
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_gameplay, "the scene has the gameplay system");
            Assert.IsNotNull(_rover, "the scene has 07");
            Assert.IsNotNull(_cameraRig, "the scene has the camera rig");
            _context = bootstrap.Context;
            _view = _context.Get<IViewCamera>().Camera;
            _awoke = _context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            yield return Until(() => _awake, WakeTimeout, "07 wakes up");

            if (_cameraRig.Opening.IsActive)
            {
                Set(_gamepad.rightStick, new Vector2(LookStick, 0f));
                yield return Wait(StickProbeSeconds);
                Set(_gamepad.rightStick, Vector2.zero);
                yield return Until(() => !_cameraRig.Opening.IsActive, WakeEaseTimeout, "the wake opening hands over");
            }
        }

        private void ClearGarageVerbsBeforeScan()
        {
            _gameplay.Salvage.Restore(new SalvageSaveData
            {
                sites = new[]
                {
                    new SalvageSiteSaveData
                    {
                        id = "site.garage",
                        discovered = false,
                        taken = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 },
                    },
                },
            });
            _gameplay.Crates.Restore(new CratesSaveData
            {
                opened = new[] { "site.garage" },
            });
            var relicEntries = new List<RelicSaveData>();
            foreach (Relic relic in _gameplay.Relics.Relics)
            {
                relicEntries.Add(new RelicSaveData
                {
                    id = relic.Definition.Id,
                    state = (int)RelicState.Displayed,
                    slot = 0,
                });
            }

            _gameplay.Relics.Restore(new RelicsSaveData
            {
                relics = relicEntries.ToArray(),
            });
        }

        private IEnumerator PlaceRoverForCleanScanShot(ScanFragment fragment)
        {
            var terrain = _context.Get<ITerrainQuery>();
            var placement = _context.Get<IRoverPlacement>();
            Transform host = fragment.Root.parent.parent;
            Vector3 away = Flat(fragment.Point - host.position);
            float baseBearing = away.sqrMagnitude > 1e-4f ? SurfaceRules.Bearing(away.normalized) : 180f;

            for (int strict = 1; strict >= 0; strict--)
            {
                foreach (float distance in CandidateDistances)
                {
                    for (int step = 0; step < 24; step++)
                    {
                        float sign = (step & 1) == 0 ? 1f : -1f;
                        float offset = (step / 2) * 15f * sign;
                        float bearing = baseBearing + offset;
                        Vector3 dir = SurfaceRules.BearingDirection(bearing);
                        Vector3 spot = Flat(fragment.Point + dir * distance);
                        float ground = terrain.SampleHeight(spot.x, spot.z);
                        var roverCheck = new Vector3(spot.x, ground + ParkClearance + 0.2f, spot.z);
                        bool openGround = !Physics.CheckSphere(roverCheck, ParkClearance, Layers.PropMask,
                            QueryTriggerInteraction.Ignore);
                        if (!openGround)
                        {
                            continue;
                        }

                        Vector3 lookDir = Flat(fragment.Point - spot).normalized;
                        float baseYaw = Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg;
                        foreach (float yawOffset in CandidateYawOffsets)
                        {
                            Quaternion chassisRot = Quaternion.Euler(0f, baseYaw + yawOffset, 0f);
                            Vector3 camPos = new Vector3(spot.x, ground, spot.z)
                                - (chassisRot * Vector3.forward) * CameraBackCheck + Vector3.up * CameraHeightCheck;
                            if (Physics.CheckSphere(camPos, 0.5f, Layers.PropMask, QueryTriggerInteraction.Ignore))
                            {
                                continue;
                            }

                            placement.PlaceAt(new Vector3(spot.x, ground, spot.z), chassisRot);
                            yield return null;
                            yield return null;
                            if (_gameplay.Scans.Candidate != fragment)
                            {
                                continue;
                            }

                            if (strict == 1)
                            {
                                Vector3 target = fragment.Point + Vector3.up * 0.12f;
                                Vector3 lens = _rover.TetherOrigin.position;
                                Vector3 cam = _view.transform.position;
                                float lensDist = Vector3.Distance(lens, target);
                                float camDist = Vector3.Distance(cam, target);
                                bool lensClear = !Physics.Raycast(lens, (target - lens).normalized,
                                    Mathf.Max(0.1f, lensDist - 0.55f), Layers.PropMask, QueryTriggerInteraction.Ignore);
                                bool camClear = !Physics.Raycast(cam, (target - cam).normalized,
                                    Mathf.Max(0.1f, camDist - 0.55f), Layers.PropMask, QueryTriggerInteraction.Ignore);
                                float screenSep = Mathf.Abs(
                                    _view.WorldToScreenPoint(lens).x - _view.WorldToScreenPoint(fragment.Point).x);
                                if (!lensClear || !camClear || screenSep < 80f)
                                {
                                    continue;
                                }
                            }

                            yield break;
                        }
                    }
                }
            }

            Assert.Fail($"Could not find a spot where {fragment.FragmentId} is the scan candidate " +
                        $"(primary hint: {_gameplay.Hints.Primary.Kind}, candidate: {_gameplay.Scans.Candidate?.FragmentId}).");
        }

        private void CaptureAndMeasure(string prefix, string label, ScanFragment fragment, StringBuilder report)
        {
            string pngPath = Path.Combine(ScanFolder, $"{prefix}_{label}.png");
            FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight, pngPath);
            Debug.Log($"[moon] capture {pngPath}");

            MeasureHdr(_view, _rover.TetherOrigin.position, fragment, out float lensMax, out float midBeamMax,
                out float fragmentCyanMax, out float fragmentAllMax, out Vector2 lensPx, out Vector2 targetPx);

            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "| `{0}_{1}.png` | {2:F2} | {3:F3} | {4:F3} | {5:F3} | {6:F3} | ({7:F0}, {8:F0}) | ({9:F0}, {10:F0}) |",
                prefix, label, fragment.Progress, lensMax, midBeamMax, fragmentCyanMax, fragmentAllMax,
                lensPx.x, lensPx.y, targetPx.x, targetPx.y));

            Assert.Less(midBeamMax, BloomThreshold, $"mid-beam ({label}) stays strictly below the bloom threshold");
            Assert.Less(fragmentCyanMax, BloomThreshold, $"fragment scan line ({label}) stays below the bloom threshold");
            if (label != "dissolve")
            {
                Assert.Greater(lensMax, BloomThreshold, $"lens aperture point ({label}) sits above the bloom threshold");
                Assert.Less(lensMax, BloomThreshold + 0.65f, $"lens aperture point ({label}) stays subtle");
                Assert.Greater(midBeamMax, 0.5f, $"mid-beam ({label}) visibly connects 07 to the fragment");
            }
        }

        private static void MeasureHdr(Camera camera, Vector3 lensWorld, ScanFragment fragment, out float lensMax,
            out float midBeamMax, out float fragmentCyanMax, out float fragmentAllMax, out Vector2 lensPx,
            out Vector2 targetPx)
        {
            var descriptor = new RenderTextureDescriptor(CaptureWidth, CaptureHeight,
                GraphicsFormat.R16G16B16A16_SFloat, 32)
            {
                msaaSamples = FrameCapture.Msaa,
            };
            RenderTexture multisampled = RenderTexture.GetTemporary(descriptor);
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            RenderTexture resolved = RenderTexture.GetTemporary(descriptor);
            RenderTexture prevTarget = camera.targetTexture;
            RenderTexture prevActive = RenderTexture.active;
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            bool prevPost = data.renderPostProcessing;
            var raw = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBAHalf, false, true);
            try
            {
                data.renderPostProcessing = false;
                camera.targetTexture = multisampled;
                camera.Render();
                Graphics.Blit(multisampled, resolved);
                RenderTexture.active = resolved;
                raw.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0, false);
                Color[] pixels = raw.GetPixels();

                Vector3 lineCentre = fragment.Scanned
                    ? fragment.Point
                    : fragment.Point + fragment.SweepSpan * (fragment.Progress - 0.5f);
                float distance = Vector3.Distance(lensWorld, lineCentre);
                Vector3 toLens = (lensWorld - lineCentre).normalized;
                float apertureScale = fragment.Scanned ? 1.15f : Mathf.Lerp(1.48f, 1.02f, fragment.Progress);
                float apertureForward = Mathf.Min(LensApertureOffset * apertureScale, distance * 0.42f);
                Vector3 apertureWorld = lensWorld - toLens * apertureForward;
                Vector3 contactWorld = lineCentre + toLens * ContactStandOff;
                Vector3 lScreen = camera.WorldToScreenPoint(apertureWorld);
                Vector3 tScreen = camera.WorldToScreenPoint(contactWorld);
                lensPx = new Vector2(lScreen.x, lScreen.y);
                targetPx = new Vector2(tScreen.x, tScreen.y);

                Rect fragRect = FragmentScreenRect(camera, fragment.Root, targetPx);
                Vector2 seg = targetPx - lensPx;
                float segLenSq = Mathf.Max(seg.sqrMagnitude, 1f);

                lensMax = 0f;
                midBeamMax = 0f;
                fragmentCyanMax = 0f;
                fragmentAllMax = 0f;

                int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(lensPx.x, fragRect.xMin) - BeamCorridorHalfWidthPx));
                int maxX = Mathf.Min(CaptureWidth - 1,
                    Mathf.CeilToInt(Mathf.Max(lensPx.x, fragRect.xMax) + BeamCorridorHalfWidthPx));
                int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(lensPx.y, fragRect.yMin) - BeamCorridorHalfWidthPx));
                int maxY = Mathf.Min(CaptureHeight - 1,
                    Mathf.CeilToInt(Mathf.Max(lensPx.y, fragRect.yMax) + BeamCorridorHalfWidthPx));

                for (int y = minY; y <= maxY; y++)
                {
                    int row = y * CaptureWidth;
                    for (int x = minX; x <= maxX; x++)
                    {
                        Color c = pixels[row + x];
                        float peak = c.maxColorComponent;
                        var pt = new Vector2(x, y);
                        bool isCyan = c.b > c.r * CyanDominance || c.g > c.r * CyanDominance;
                        float distLens = Vector2.Distance(pt, lensPx);
                        if (distLens <= LensApertureRadiusPx)
                        {
                            if (isCyan && peak > lensMax)
                            {
                                lensMax = peak;
                            }

                            continue;
                        }

                        float along = Vector2.Dot(pt - lensPx, seg) / segLenSq;
                        if (along >= 0.2f && along <= 0.76f)
                        {
                            Vector2 proj = lensPx + seg * along;
                            if (Vector2.Distance(pt, proj) <= BeamCorridorHalfWidthPx && isCyan && peak > midBeamMax)
                            {
                                midBeamMax = peak;
                            }

                            continue;
                        }

                        if (along > 0.76f && fragRect.Contains(pt))
                        {
                            if (peak > fragmentAllMax)
                            {
                                fragmentAllMax = peak;
                            }

                            if (isCyan && peak > fragmentCyanMax)
                            {
                                fragmentCyanMax = peak;
                            }
                        }
                    }
                }
            }
            finally
            {
                data.renderPostProcessing = prevPost;
                camera.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(multisampled);
                RenderTexture.ReleaseTemporary(resolved);
                Object.DestroyImmediate(raw);
            }
        }

        private static Rect FragmentScreenRect(Camera camera, Transform root, Vector2 fallback)
        {
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            float minX = fallback.x;
            float maxX = fallback.x;
            float minY = fallback.y;
            float maxY = fallback.y;
            foreach (MeshFilter mf in filters)
            {
                if (mf.name != "Solid" && mf.name != "Lines" || mf.sharedMesh == null)
                {
                    continue;
                }

                Vector3[] verts = mf.sharedMesh.vertices;
                Transform tr = mf.transform;
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 sp = camera.WorldToScreenPoint(tr.TransformPoint(verts[i]));
                    if (sp.z > 0f)
                    {
                        minX = Mathf.Min(minX, sp.x);
                        maxX = Mathf.Max(maxX, sp.x);
                        minY = Mathf.Min(minY, sp.y);
                        maxY = Mathf.Max(maxY, sp.y);
                    }
                }
            }

            return Rect.MinMaxRect(minX - FragmentBoxPaddingPx, minY - FragmentBoxPaddingPx,
                maxX + FragmentBoxPaddingPx, maxY + FragmentBoxPaddingPx);
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        private static IEnumerator Until(Func<bool> condition, float timeout, string what)
        {
            float deadline = Time.time + timeout;
            while (!condition())
            {
                Assert.Less(Time.time, deadline, $"Timed out waiting for: {what}");
                yield return null;
            }
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _problems.Add($"{type}: {message}");
            }
        }
    }
}
