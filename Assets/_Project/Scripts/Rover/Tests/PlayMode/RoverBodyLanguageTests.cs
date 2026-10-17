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

        private float NeckLiftAngle => Mathf.DeltaAngle(0f, _rover.NeckLift.localEulerAngles.x);

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
                float tuckedNeck = NeckLiftAngle;
                float shutLid = _rover.BodyLanguage.Mood.LidClosure;
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
                Assert.Less(_rover.BodyLanguage.Mood.LidClosure, shutLid - 0.5f, "The lid opens to its resting half-lid.");
                Assert.Less(NeckLiftAngle, tuckedNeck - 10f, "The neck lifts out of its sleeping tuck.");
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
            _rover.Context.Events.Publish(new RelicAnswered(right, 10f, "teapot"));
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
        public IEnumerator ChainedSalvage_PerksUpMoreAsTheComboClimbs()
        {
            Spawn(false);
            yield return Wait(1f);
            yield return PeakPerkAfter(new MaterialSalvaged(SalvageMaterial.Metal, 2, Vector3.zero, "site.depot", 0));
            float first = _peakPerk;
            yield return Wait(4f);
            yield return PeakPerkAfter(new MaterialSalvaged(SalvageMaterial.Metal, 2, Vector3.zero, "site.depot", 4));
            Assert.Greater(first, 0.05f, "Every pickup gets a little perk-up.");
            Assert.Greater(_peakPerk, first + 0.1f, "Chained pickups make 07 visibly happier.");
        }

        [UnityTest]
        public IEnumerator CutWithoutCollectReward_AndDig_ReleaseGazeSoHeadReturnsForward()
        {
            Spawn(false);
            yield return Wait(1f);

            EventBus events = _rover.Context.Events;
            Vector3 startPosition = _rover.Controller.Position;
            Quaternion startRotation = Quaternion.Euler(0f, _rover.Controller.Heading, 0f);
            Vector3 sealOnRight = startPosition + Vector3.right * 6f + Vector3.forward * 2f;
            events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Anticipate, sealOnRight, Vector3.back, 0f, 0.5f));
            events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Act, sealOnRight, Vector3.back, 0f, 0f));
            yield return Wait(1f);
            Assert.Greater(NeckYaw, TurnedHead, "07 turns its head to the container seal while cutting.");

            events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Impact, sealOnRight, Vector3.back, 1f, 0f));
            yield return Wait(4f);
            Assert.AreEqual(0f, NeckYaw, 5f,
                "After a seal cut completes without Collect.Reward, 07 releases cut gaze and looks ahead again.");

            // The cut turned the whole body toward the seal; the dig below is staged from the starting pose.
            _rover.Context.Get<IRoverPlacement>().PlaceAt(startPosition, startRotation);
            yield return Wait(1f);

            Vector3 relicOnRight = _rover.Controller.Position + Vector3.right * 6f + Vector3.forward * 2f;
            events.Publish(new FeelBeat(FeelKind.Dig, FeelPhase.Anticipate, relicOnRight, Vector3.up, 0f, 0.5f));
            events.Publish(new FeelBeat(FeelKind.Dig, FeelPhase.Impact, relicOnRight, Vector3.up, 1f, 0f));
            events.Publish(new FeelBeat(FeelKind.Dig, FeelPhase.Follow, relicOnRight, Vector3.up, 0f, 0f));
            events.Publish(new RelicSurfaced(relicOnRight, "teapot"));
            yield return Wait(1f);
            Assert.Greater(NeckYaw, TurnedHead, "07 glances at the surfaced relic.");

            yield return Wait(4.5f);
            Assert.AreEqual(0f, NeckYaw, 5f,
                "After the surfaced-relic glance expires, 07 looks ahead again instead of staying stuck to one side.");

            events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Anticipate, sealOnRight, Vector3.back, 0f, 0.5f));
            events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Impact, sealOnRight, Vector3.back, 1f, 0f));
            events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Act, sealOnRight, Vector3.up, 1f, 0.42f));
            events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Impact, _rover.Controller.CargoSocket.position,
                Vector3.up, 1f, 0f));
            _rover.Drive.Drive = new Vector2(-1f, 1f);
            yield return Wait(2f);
            Assert.Less(NeckYaw, -5f,
                "Carrying a part away from a cut container and steering left, 07 looks into the left turn.");
            _rover.Drive.Drive = Vector2.zero;
        }

        [UnityTest]
        public IEnumerator TetherStrain_SquintsFaceAndLiftsEffort_AndDrivingAccumulatesDustCleanedOnDock()
        {
            Spawn(false);
            yield return Wait(0.5f);

            EventBus events = _rover.Context.Events;
            FaceState faceState = _rover.BodyLanguage.FaceDirector.Display.State;
            float baseSquint = faceState.LeftEye.Squint;
            float baseOpen = faceState.LeftEye.Open;

            events.Publish(new TetherAttached(_rover.Controller.Position + Vector3.back * 4f, 10f));
            events.Publish(new FeelBeat(FeelKind.Tether, FeelPhase.Act, _rover.Controller.Position, Vector3.back, 0.9f, 0f));
            yield return Wait(0.25f);

            Assert.AreEqual(0.9f, _rover.BodyLanguage.TetherStrain, 1e-3f);
            Assert.AreEqual(0.9f, faceState.Strain, 1e-3f);
            Assert.Greater(faceState.LeftEye.Squint, baseSquint + 0.3f, "07 squints under tether strain.");
            Assert.Less(faceState.LeftEye.Open, baseOpen - 0.1f, "07's eyes narrow under tether strain.");

            events.Publish(new TetherReleased(_rover.Controller.Position, true));
            yield return null;
            Assert.AreEqual(0f, _rover.BodyLanguage.TetherStrain, 1e-4f);
            Assert.AreEqual(0f, faceState.Strain, 1e-4f);

            RoverVisualRig rig = _rover.Controller.GetComponentInChildren<RoverVisualRig>();
            float initialDust = rig.AppliedDustAmount;
            _rover.Drive.Drive = new Vector2(0f, 1f);
            yield return Wait(1.5f);
            _rover.Drive.Drive = Vector2.zero;

            Assert.Greater(rig.DustLevel, 0.02f, "Driving across the regolith accumulates lower-body dust.");
            Assert.Greater(rig.AppliedDustAmount, initialDust + 0.01f, "Lower-body renderers receive increased _DustAmount.");

            float dustedLevel = rig.DustLevel;
            events.Publish(new RoverDockChanged(true, _rover.Controller.Position, _rover.Controller.Rotation));
            yield return Wait(1.2f);
            Assert.Less(rig.DustLevel, dustedLevel * 0.5f, "Docking at home brushes off lower-body regolith dust.");
            events.Publish(new RoverDockChanged(false, _rover.Controller.Position, _rover.Controller.Rotation));
        }

        private IEnumerator PeakPerkAfter(MaterialSalvaged salvage)
        {
            _peakPerk = 0f;
            _rover.Context.Events.Publish(salvage);
            float until = Time.time + 1f;
            while (Time.time < until)
            {
                yield return null;
                _peakPerk = Mathf.Max(_peakPerk, _rover.BodyLanguage.Mood.Perk);
            }
        }
    }
}
