using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Captures a 2 s frame strip of 07 accelerating (0..1 s) and turning hard (1..2 s) from the chase camera in the
    /// real Main scene, both in sunlit day and earthlit night (<c>Logs/captures/dust_*.png</c>).
    /// Run on demand with <c>--filter RoverDustCaptureTests</c>.
    /// </summary>
    [Explicit("Slow real-game capture session; run on demand with --filter RoverDustCaptureTests.")]
    [Category("Playthrough")]
    public sealed class RoverDustCaptureTests : InputTestFixture
    {
        private const int FrameWidth = 1280;
        private const int FrameHeight = 720;
        private const int PanelWidth = 640;
        private const int PanelHeight = 360;
        private const float FrameTime = 1f / 60f;
        private const int FramesPerStep = 24;
        private const int Steps = 5;
        private const int AccelSteps = 2;
        private const float SettleSeconds = 0.6f;
        private const float SunlitDaySeconds = 210f;
        private const float EarthlitNightSeconds = 4816f;

        private static readonly Vector2 FlatGround = new Vector2(-12f, -38f);

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Keyboard _keyboard;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            _scene.Unload();
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator ChaseCam_2sAccelerateAndTurnStrip_DayAndNight()
        {
            Directory.CreateDirectory(CaptureFolder);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            yield return _scene.Boot();

            yield return CaptureRun("day", SunlitDaySeconds);
            yield return CaptureRun("night", EarthlitNightSeconds);
        }

        private IEnumerator CaptureRun(string lightingTag, float clockSeconds)
        {
            SetSkyClock(clockSeconds);
            PlaceRover(FlatGround, 0f);
            yield return RealRoverScene.WaitSeconds(SettleSeconds);

            var panels = new Texture2D[Steps];
            try
            {
                Press(_keyboard.wKey);
                for (int step = 0; step < Steps; step++)
                {
                    if (step == AccelSteps)
                    {
                        Press(_keyboard.dKey);
                    }

                    for (int f = 0; f < FramesPerStep; f++)
                    {
                        yield return null;
                    }

                    float elapsed = (step + 1) * FramesPerStep * FrameTime;
                    string phase = step < AccelSteps ? "accel" : "turn";
                    string frameName = $"dust_{lightingTag}_{step + 1:00}_{phase}_{elapsed:0.0}s.png";
                    FrameCapture.SavePng(_scene.View, FrameWidth, FrameHeight, Path.Combine(CaptureFolder, frameName));
                    panels[step] = FrameCapture.Render(_scene.View, PanelWidth, PanelHeight);
                }

                StitchStrip(panels, Path.Combine(CaptureFolder, $"dust_{lightingTag}_strip.png"));
            }
            finally
            {
                Release(_keyboard.wKey);
                Release(_keyboard.dKey);
                for (int i = 0; i < panels.Length; i++)
                {
                    if (panels[i] != null)
                    {
                        Object.DestroyImmediate(panels[i]);
                    }
                }
            }
        }

        private void PlaceRover(Vector2 xz, float yawDeg)
        {
            ITerrainQuery terrain = _scene.Context.Get<ITerrainQuery>();
            Vector3 p = new Vector3(xz.x, terrain.SampleHeight(xz.x, xz.y), xz.y);
            _scene.Context.Get<IRoverPlacement>().PlaceAt(p, Quaternion.Euler(0f, yawDeg, 0f));
            _scene.CameraRig.Snap();
        }

        private void SetSkyClock(float timeSeconds)
        {
            if (!_scene.Context.TryGet(out ISunlight sunlight))
            {
                return;
            }

            MethodInfo method = sunlight.GetType().GetMethod(
                "SetClockTime",
                BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(sunlight, new object[] { timeSeconds, false, true });
        }

        private static void StitchStrip(Texture2D[] panels, string outputPath)
        {
            var strip = new Texture2D(PanelWidth * panels.Length, PanelHeight, TextureFormat.RGBA32, false);
            try
            {
                for (int i = 0; i < panels.Length; i++)
                {
                    strip.SetPixels(i * PanelWidth, 0, PanelWidth, PanelHeight, panels[i].GetPixels());
                }

                strip.Apply(false);
                FrameCapture.WritePng(strip, outputPath);
            }
            finally
            {
                Object.DestroyImmediate(strip);
            }
        }
    }
}
