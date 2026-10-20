using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// 07 listening to the call in the real Main scene (opening v3 2.2): idle, its head swings round to the call's
    /// point of light; the antenna lamp stutters only while the call is broken, and keys three short and one long
    /// once the first charge locks it.
    /// </summary>
    public sealed class CallListenPlayModeTests
    {
        private const float FrameTime = 1f / 60f;
        private const float OffsetFromCallDegrees = 70f;
        private const float IdleSettleSeconds = 14f;
        private const float StutterWatchSeconds = 12f;
        private const float OnTargetDegrees = 12f;
        private const float LitGlow = 0.5f;
        private const float SignalWatchSeconds = 14f;

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
        public IEnumerator Idle_TheHeadSwingsToTheCall_AndTheLampStuttersWhileBroken_ThenKeysThePatternOnceLocked()
        {
            yield return _scene.Boot();
            ICallSource call = _scene.Context.Get<ICallSource>();
            Assert.AreEqual(CallState.Broken, call.State, "a new game starts on the reserve: the call is broken");
            RoverBodyLanguage body = _scene.Body;
            Vector3 toCall = Flat(call.Direction);
            float bearing = Mathf.Atan2(toCall.x, toCall.z) * Mathf.Rad2Deg + OffsetFromCallDegrees;

            _scene.Context.Get<IRoverRig>().SetHoldStill(_holdOwner, true);
            _scene.Context.Get<IRoverPlacement>().PlaceAt(_scene.Rover.Position, Quaternion.Euler(0f, bearing, 0f));
            yield return RealRoverScene.WaitSeconds(0.3f);
            float before = Vector3.Angle(Flat(body.ScreenTransform.forward), toCall);
            Assert.Greater(before, OffsetFromCallDegrees * 0.6f, "07 starts turned away from the call");

            float until = Time.time + IdleSettleSeconds;
            while (Time.time < until)
            {
                yield return null;
            }

            float after = Vector3.Angle(Flat(body.ScreenTransform.forward), toCall);
            Assert.Less(after, OnTargetDegrees, $"idle, the head has swung round to the call (was {before:F0} deg)");

            until = Time.time + StutterWatchSeconds;
            float highest = 0f;
            float lowest = float.MaxValue;
            while (Time.time < until)
            {
                yield return null;
                highest = Mathf.Max(highest, body.CallLampGlow);
                lowest = Mathf.Min(lowest, body.CallLampGlow);
            }

            Assert.Greater(highest, LitGlow, "the antenna lamp stutters on while the call is broken");
            Assert.Less(lowest, LitGlow * 0.5f, "and drops dark between its bursts");

            _scene.Context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
            yield return null;
            Assert.AreEqual(CallState.Locked, call.State, "the first charge locks the call");
            CallListenSettings settings = body.CallSettings;

            bool hasSignal = _scene.Context.TryGet(out ICallSignal signal);
            float lampHigh = 0f;
            float totalGap = 0f;
            int frames = 0;
            float watch = Time.time + SignalWatchSeconds;
            while (Time.time < watch)
            {
                yield return null;
                lampHigh = Mathf.Max(lampHigh, body.CallLampGlow);
                if (hasSignal)
                {
                    totalGap += Mathf.Abs(body.CallLampGlow - signal.Keying * settings.KeyGlow);
                    frames++;
                }
            }

            if (hasSignal)
            {
                Assert.Greater(lampHigh, LitGlow, "the lamp keys with the sign's tones");
                Assert.Less(totalGap / Mathf.Max(1, frames), settings.KeyGlow * 0.25f,
                    "in phase with ICallSignal.Keying: on average within the lamp's own soft edge of it");
            }
            else
            {
                Assert.Less(lampHigh, 0.05f, "with no ICallSignal the locked lamp stays dark");
            }

            _scene.Context.Get<IRoverRig>().SetHoldStill(_holdOwner, false);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator AFailedReply_LowersTheHead_AndDropsTheLamp()
        {
            yield return _scene.Boot();
            RoverBodyLanguage body = _scene.Body;
            _scene.Context.Get<IRoverRig>().SetHoldStill(_holdOwner, true);
            _scene.Context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
            _scene.Context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.NightReady, false));
            yield return RealRoverScene.WaitSeconds(IdleSettleSeconds);
            Assert.AreEqual(CallState.Clear, _scene.Context.Get<ICallSource>().State);
            Assert.Greater(body.CallLampGlow, 0.3f, "a clear call glows steadily at the antenna");
            float headUp = body.ScreenTransform.forward.y;

            _scene.Context.Events.Publish(new CallReplyFailed());
            yield return RealRoverScene.WaitSeconds(2f);
            Assert.Less(body.ScreenTransform.forward.y, headUp - 0.1f, "the head hangs lower");
            Assert.Less(body.CallLampGlow, 0.05f, "and the antenna lamp has dropped");
            _scene.Context.Get<IRoverRig>().SetHoldStill(_holdOwner, false);
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.normalized;
        }
    }
}
