using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The release's view from the rise (opening v3 R9) on 07: its head looks at relay.1's lamp, the next goal, then
    /// glances at the Peak's dish where the glint is, and never at Kestrel-3.
    /// </summary>
    public sealed class ReleaseViewPlayModeTests
    {
        private const float RelayLookSeconds = 4.5f;
        private const float PeakLookSeconds = 1.6f;
        private const float DishGlanceAfterSeconds = 0.9f;
        private const float FrameTime = 1f / 60f;
        private const float SecondsEach = 2.5f;
        private const float SettleSeconds = 1.4f;
        private const float OnTargetDegrees = 22f;
        private const float LureDistance = 300f;

        private readonly RealRoverScene _scene = new RealRoverScene();
        private readonly object _holdOwner = new object();

        [SetUp]
        public void SetUp()
        {
            Time.captureDeltaTime = FrameTime;
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            _scene.Unload();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TheViewFromTheRise_Has07LookAtRelay1_ThenGlanceAtThePeak_AndNeverAtKestrel()
        {
            yield return _scene.Boot();
            RoverBodyLanguage body = _scene.Body;
            _scene.Context.Get<IRoverRig>().SetHoldStill(_holdOwner, true);
            Vector3 at = _scene.Rover.Position;
            _scene.Context.Get<IRoverPlacement>().PlaceAt(at, Quaternion.identity);
            yield return RealRoverScene.WaitSeconds(1f);

            Vector3 relay = at + Bearing(-70f) * LureDistance + Vector3.up * 3f;
            Vector3 dish = at + Bearing(10f) * LureDistance + Vector3.up * 60f;
            Vector3 kestrel = at + Bearing(60f) * LureDistance;
            _scene.Context.Events.Publish(new ReleaseViewOpened(relay, dish, kestrel, SecondsEach));

            yield return RealRoverScene.WaitSeconds(SettleSeconds);
            Assert.Less(Aim(body, at, relay), OnTargetDegrees, "first, relay.1's lamp");
            yield return RealRoverScene.WaitSeconds(RelayLookSeconds - SettleSeconds + DishGlanceAfterSeconds);
            Assert.Less(Aim(body, at, dish), OnTargetDegrees, "then a glance at the Peak's dish");
            yield return RealRoverScene.WaitSeconds(PeakLookSeconds + SettleSeconds);
            Assert.Greater(Aim(body, at, kestrel), OnTargetDegrees, "and never a look at Kestrel-3: the lamp is the goal");
            _scene.Context.Get<IRoverRig>().SetHoldStill(_holdOwner, false);
        }

        private static Vector3 Bearing(float degrees)
        {
            return new Vector3(Mathf.Sin(degrees * Mathf.Deg2Rad), 0f, Mathf.Cos(degrees * Mathf.Deg2Rad));
        }

        private static float Aim(RoverBodyLanguage body, Vector3 from, Vector3 to)
        {
            Vector3 headForward = body.ScreenTransform.forward;
            headForward.y = 0f;
            Vector3 toward = to - from;
            toward.y = 0f;
            return Vector3.Angle(headForward, toward);
        }
    }
}
