using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// 07, parked on flat ground, reacts to gameplay events and IRoverRig gaze requests. InputTestFixture keeps real
    /// devices (a mouse moving the camera in the interactive editor) out of the session.
    /// </summary>
    public sealed class RoverBodyLanguageTests : InputTestFixture
    {
        private const float TurnedHead = 20f;

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;
        private float _peakPerk;

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
        }

        public override void TearDown()
        {
            _rover?.Dispose();
            _rover = null;
            _world.Dispose();
            _physics.Dispose();
            base.TearDown();
        }

        private float EyelidAngle => Mathf.DeltaAngle(0f, _rover.Eyelid.localEulerAngles.x);

        private void Spawn(bool asleep)
        {
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -300f), 0f, asleep);
        }

        [UnityTest]
        public IEnumerator FirstBoot_SleepsThenWakesSlowlyTowardEarth()
        {
            Spawn(true);
            var awoke = new List<RoverAwoke>();
            using (_rover.Context.Events.Subscribe<RoverAwoke>(awoke.Add))
            {
                yield return Wait(0.5f);
                Assert.AreEqual(0f, _rover.EyeLight.intensity, 1e-3f, "Asleep: the eye is dark.");
                float shutLid = EyelidAngle;
                Assert.Greater(HeadDip, 5f, "Asleep: the head is bowed.");
                Assert.IsEmpty(awoke);

                yield return Wait(1.5f);
                Assert.AreEqual(1, awoke.Count, "RoverAwoke as 07 starts waking (the radio crackles on).");
                Assert.IsFalse(awoke[0].WokenByPlayer);

                float highestLook = float.MaxValue;
                float until = Time.time + 6f;
                while (Time.time < until)
                {
                    yield return null;
                    highestLook = Mathf.Min(highestLook, HeadDip);
                }

                Assert.Less(highestLook, -10f, "Waking, 07 looks up toward Earth.");
                Assert.Less(EyelidAngle, shutLid - 10f, "The lid opens to its resting half-lid.");
                Assert.Greater(_rover.EyeLight.intensity, 0.3f, "The eye glows.");
                Assert.AreEqual(1, awoke.Count);
            }
        }

        [UnityTest]
        public IEnumerator FirstBoot_DrivingWakes07AtOnce()
        {
            Spawn(true);
            var awoke = new List<RoverAwoke>();
            using (_rover.Context.Events.Subscribe<RoverAwoke>(awoke.Add))
            {
                yield return Wait(0.3f);
                _rover.Drive.Drive = new Vector2(0f, 1f);
                yield return Wait(0.3f);
                Assert.AreEqual(1, awoke.Count);
                Assert.IsTrue(awoke[0].WokenByPlayer);
                Assert.Greater(_rover.Controller.Speed, 0.3f, "Control is never blocked by the intro.");
                yield return Wait(1f);
                Assert.Greater(_rover.EyeLight.intensity, 0.3f, "Awake within a second of driving.");
                _rover.Drive.Drive = Vector2.zero;
            }
        }

        private float NeckYaw => Mathf.DeltaAngle(0f, _rover.Neck.localEulerAngles.y);

        private float HeadDip => Mathf.DeltaAngle(0f, _rover.Head.localEulerAngles.x);

        private float WingOpening => Mathf.DeltaAngle(0f, _rover.SolarWing.localEulerAngles.x);

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator RelicAnswer_TurnsTheHead_AndGameplayGazeOverridesIt()
        {
            Spawn(false);
            yield return Wait(1f);
            Vector3 right = _rover.Controller.Position + Vector3.right * 10f + Vector3.forward * 4f;
            _rover.Context.Events.Publish(new RelicAnswered(right, 10f));
            yield return Wait(1.5f);
            Assert.Greater(NeckYaw, TurnedHead, "07 glances toward the relic that answered.");

            var tether = new object();
            IRoverRig rig = _rover.Context.Get<IRoverRig>();
            rig.SetGazeTarget(tether, _rover.Controller.Position + Vector3.left * 10f + Vector3.forward * 4f, 5);
            yield return Wait(1.5f);
            Assert.Less(NeckYaw, -TurnedHead, "A gameplay gaze request outranks 07's own glance.");

            rig.ClearGazeTarget(tether);
            yield return Wait(4f);
            Assert.AreEqual(0f, NeckYaw, 5f, "With no requests left, 07 looks where it is going again.");
        }

        [UnityTest]
        public IEnumerator LeftAlone_DaydreamsUpTowardEarth()
        {
            Spawn(false);
            yield return Wait(1f);
            float activeLid = _rover.BodyLanguage.Mood.LidClosure;
            yield return Wait(11f);
            Assert.Less(HeadDip, -15f, "Left alone, 07 slowly looks up toward Earth.");
            Assert.Greater(_rover.BodyLanguage.Mood.LidClosure, activeLid + 0.15f, "The lid droops.");
            Assert.Greater(WingOpening, 2f, "The tired wing sighs open a little.");
        }

        /// <summary>Both reactions run before 07 would start daydreaming (which also sighs) at IdleDelay.</summary>
        [UnityTest]
        public IEnumerator SnappedTether_Sighs_AndDeposit_Nods()
        {
            Spawn(false);
            yield return Wait(0.5f);
            float restWing = WingOpening;
            _rover.Context.Events.Publish(new TetherReleased(Vector3.zero, true));
            float widest = restWing;
            float until = Time.time + 2.5f;
            while (Time.time < until)
            {
                yield return null;
                widest = Mathf.Max(widest, WingOpening);
            }

            Assert.Greater(widest, restWing + 2f, "A snapped tether earns a sigh: the tired wing lifts a little.");

            float restDip = HeadDip;
            _rover.Context.Events.Publish(new RelicDeposited("test", Vector3.zero, 1));
            float deepest = restDip;
            until = Time.time + 0.8f;
            while (Time.time < until)
            {
                yield return null;
                deepest = Mathf.Max(deepest, HeadDip);
            }

            Assert.Greater(deepest, restDip + 4f, "A contented nod dips the head.");
        }

        [UnityTest]
        public IEnumerator ChainedScrap_PerksUpMoreAsTheComboClimbs()
        {
            Spawn(false);
            yield return Wait(1f);
            yield return PeakPerkAfter(new ScrapCollected(Vector3.zero, 1, 0));
            float first = _peakPerk;
            yield return Wait(4f);
            yield return PeakPerkAfter(new ScrapCollected(Vector3.zero, 1, 4));
            Assert.Greater(first, 0.05f, "Every pickup gets a little perk-up.");
            Assert.Greater(_peakPerk, first + 0.1f, "Chained pickups make 07 visibly happier.");
        }

        private IEnumerator PeakPerkAfter(ScrapCollected scrap)
        {
            _peakPerk = 0f;
            _rover.Context.Events.Publish(scrap);
            float until = Time.time + 1f;
            while (Time.time < until)
            {
                yield return null;
                _peakPerk = Mathf.Max(_peakPerk, _rover.BodyLanguage.Mood.Perk);
            }
        }
    }
}
