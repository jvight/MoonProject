using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>07, parked on flat ground, reacts to gameplay events and IRoverRig gaze requests.</summary>
    public sealed class RoverBodyLanguageTests
    {
        private const float TurnedHead = 20f;

        private InputActionAsset _actions;
        private TestWorld _world;
        private TestRover _rover;
        private float _peakPerk;

        [SetUp]
        public void SetUp()
        {
            _actions = TestControls.Create();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_actions, _world, new Vector3(0f, 0f, -300f), 0f);
        }

        [TearDown]
        public void TearDown()
        {
            _rover.Dispose();
            _world.Dispose();
            Object.Destroy(_actions);
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
