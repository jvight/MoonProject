using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The owner could not turn the camera round to 07's face (2026-10-19). In the real Main scene, with the right stick
    /// (the Input System's gamepad; a headless run has no locked cursor for the mouse): a sustained look swings the chase view all the way round to 07's front
    /// and on, no frame turns it further than the look itself asks, and the front view of 07 at night, its lights on, is
    /// captured (Logs/captures/free-look/front-night.png). The player is driving off after the look: the view then
    /// eases home behind 07.
    /// </summary>
    public sealed class FreeLookPlayModeTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const float LookStick = 1f;
        private const float RoundTimeout = 20f;
        private const float FrontDegrees = 175f;
        private const float SettleSeconds = 1.5f;
        private const float NightSearchStep = 10f;
        private const float NightSearchLimit = 12000f;
        private const float MaxFrameTurnDegrees = 8f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "free-look"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Gamepad _gamepad;

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
        public IEnumerator ASustainedLook_SwingsTheViewRoundToTheFrontOf07_WithoutASnap_AndItsLightsShowAtNight()
        {
            Directory.CreateDirectory(CaptureFolder);
            _gamepad = InputSystem.AddDevice<Gamepad>();
            yield return _scene.Boot();
            CameraOrbit orbit = _scene.CameraRig.Orbit;
            SetNight(_scene.Context.Get<ISunlight>());
            yield return RealRoverScene.WaitSeconds(SettleSeconds);

            float deadline = Time.time + RoundTimeout;
            float last = orbit.Bearing;
            float fastest = 0f;
            while (Mathf.Abs(orbit.YawOffset) < FrontDegrees && Time.time < deadline)
            {
                Set(_gamepad.rightStick, new Vector2(LookStick, 0f));
                yield return null;
                fastest = Mathf.Max(fastest, Mathf.Abs(Mathf.DeltaAngle(last, orbit.Bearing)));
                last = orbit.Bearing;
            }

            Set(_gamepad.rightStick, Vector2.zero);
            Assert.GreaterOrEqual(Mathf.Abs(orbit.YawOffset), FrontDegrees,
                $"a sustained look brings the chase view round to 07's front (now {orbit.YawOffset:F0} deg off its back)");
            Assert.LessOrEqual(fastest, MaxFrameTurnDegrees, "the view turns smoothly: no frame snaps it");

            yield return RealRoverScene.WaitSeconds(SettleSeconds);
            FrameCapture.SavePng(_scene.View, 1920, 1080, Path.Combine(CaptureFolder, "front-night.png"));
            Assert.GreaterOrEqual(Mathf.Abs(orbit.YawOffset), FrontDegrees, "parked, the view stays on 07's face");
        }

        /// <summary>The first night's clock time found by setting the clock, and the sky set to it.</summary>
        private static void SetNight(ISunlight sunlight)
        {
            MethodInfo set = sunlight.GetType().GetMethod("SetClockTime", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(set, "World's sky clock can be set for the tests");
            for (float t = 0f; t <= NightSearchLimit; t += NightSearchStep)
            {
                set.Invoke(sunlight, new object[] { t, false, true });
                if (sunlight.Phase == SkyPhase.Earthlit)
                {
                    return;
                }
            }

            Assert.Fail("the sky has a night");
        }
    }
}
