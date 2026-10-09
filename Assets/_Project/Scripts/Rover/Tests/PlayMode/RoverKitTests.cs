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
    /// Visible progression on 07 (M3-11, real wiring): owned kit is there at once on load, and so is kit granted
    /// without a purchase (bought kit waits for the Rover Bay to fit it: <see cref="RoverBayTests"/>); the cargo seat
    /// follows the rack; friends' gifts show silently on load or softly when 07 comes home; the Boost Coils give a
    /// gentle extra cruise and the drums glow with it.
    /// </summary>
    public sealed class RoverKitTests : InputTestFixture
    {
        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;
        private readonly List<RoverKitInstalling> _installing = new List<RoverKitInstalling>();
        private readonly List<RoverKitFitted> _fitted = new List<RoverKitFitted>();
        private readonly List<bool> _boosts = new List<bool>();

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _installing.Clear();
            _fitted.Clear();
            _boosts.Clear();
        }

        public override void TearDown()
        {
            _rover?.Dispose();
            _rover = null;
            _world.Dispose();
            _physics.Dispose();
            base.TearDown();
        }

        /// <summary>
        /// Spawns 07 in the test body, so whatever the test sets before its first yield is what a loaded game had
        /// (the kit and gifts are judged on the first frame).
        /// </summary>
        private void Spawn()
        {
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -150f), 0f);
            EventBus events = _rover.Context.Events;
            events.Subscribe<RoverKitInstalling>(_installing.Add);
            events.Subscribe<RoverKitFitted>(_fitted.Add);
            events.Subscribe<RoverBoostChanged>(changed => _boosts.Add(changed.Boosting));
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        private IRoverAbilities Abilities => _rover.Context.Get<IRoverAbilities>();

        [UnityTest]
        public IEnumerator OwnedOnLoad_IsThereAtOnce_NoMoment()
        {
            Spawn();
            Abilities.Grant(RoverAbility.WarmHeadlamp);
            Abilities.Grant(RoverAbility.BoostCoils);
            Abilities.Grant(RoverAbility.CargoCradle);
            yield return null;
            yield return null;
            Assert.IsTrue(_rover.LampBar.gameObject.activeSelf, "The lamp bar is there from the first frame.");
            Assert.IsTrue(_rover.Drums[0].gameObject.activeSelf && _rover.Drums[1].gameObject.activeSelf);
            Assert.IsTrue(_rover.CargoRack.gameObject.activeSelf);
            Assert.AreEqual(Vector3.zero, _rover.LampBar.localPosition, "At rest on its socket.");
            Assert.IsEmpty(_installing, "No moment replayed on load.");
            Assert.IsEmpty(_fitted);
            Assert.AreEqual(_rover.RigTuning.Kit.WarmSpotAngle, _rover.Headlamp.spotAngle, 0.01f, "Warm road light.");
        }

        [UnityTest]
        public IEnumerator GrantedWithoutAPurchase_IsThereAtOnce_NoMoment()
        {
            Spawn();
            yield return Wait(0.5f);
            float plainAngle = _rover.Headlamp.spotAngle;
            Abilities.Grant(RoverAbility.WarmHeadlamp);
            yield return null;
            Assert.IsTrue(_rover.LampBar.gameObject.activeSelf, "Not bought at the bay: simply there.");
            Assert.AreEqual(Vector3.zero, _rover.LampBar.localPosition, "At rest on its socket.");
            Assert.IsEmpty(_installing);
            Assert.IsEmpty(_fitted);
            Assert.Greater(_rover.Headlamp.spotAngle, plainAngle + 20f, "The warm road light at once.");
        }

        [UnityTest]
        public IEnumerator CargoSeat_IsFittedWithTheCradle_AndFollowsTheRack()
        {
            Spawn();
            IRoverCargoSeat seat = _rover.Context.Get<IRoverCargoSeat>();
            Assert.IsFalse(seat.IsFitted, "No cradle yet.");
            Abilities.Grant(RoverAbility.CargoCradle);
            Assert.IsTrue(seat.IsFitted, "Fitted as soon as it is owned (a load restores a carried relic at once).");
            _rover.Drive.Drive = new Vector2(0.5f, 1f);
            float until = Time.time + 2f;
            while (Time.time < until)
            {
                yield return null;
                Assert.AreEqual(_rover.RelicSeat.position, seat.Position, "The seat is the rack's RelicSeat.");
                Assert.AreEqual(_rover.RelicSeat.rotation, seat.Rotation);
            }

            Assert.Greater(_rover.Controller.Speed, 2f, "Measured while driving and turning.");
            _rover.Drive.Drive = Vector2.zero;
        }

        [UnityTest]
        public IEnumerator FriendGifts_ShowSilentlyOnLoad_OrSoftlyWhen07ComesHome()
        {
            Spawn();
            _rover.Friends.Bell.Activity = FriendActivity.Home;
            yield return null;
            yield return null;
            Assert.IsTrue(_rover.FreshSerial.gameObject.activeSelf, "Bell home on load: the fresh '07' is there.");
            Assert.IsTrue(_rover.Pennant.gameObject.activeSelf, "...and the pennant.");
            Assert.IsFalse(_rover.CellFilled.gameObject.activeSelf, "Tilly still broken: the wing's gap remains.");
            Assert.IsEmpty(_installing, "Silently.");

            _rover.Friends.Tilly.Activity = FriendActivity.Following;
            yield return Wait(2f);
            Assert.IsFalse(_rover.CellFilled.gameObject.activeSelf, "Repaired out in the field: the gift waits...");

            _rover.Layout.BasePosition = _rover.Controller.Position;
            yield return null;
            yield return null;
            Assert.AreEqual(1, _installing.Count, "...until 07 is home.");
            Assert.AreEqual(RoverKitPiece.SolarCell, _installing[0].Piece);
            Assert.IsTrue(_installing[0].Gift, "The soft version.");
            yield return Wait(3f);
            Assert.IsTrue(_rover.CellFilled.gameObject.activeSelf, "The cell is in the wing.");
            Assert.AreEqual(1, _fitted.Count);
            Assert.IsTrue(_rover.Kit.HasMendedWing);

            yield return Wait(_rover.CharacterTuning.IdleDelay + 16f);
            Debug.Log($"[rover-kit] mended wing rests open at {_rover.BodyLanguage.Mood.WingOpen:0.00}");
            Assert.AreEqual(_rover.CharacterTuning.WingIdleOpenMended, _rover.BodyLanguage.Mood.WingOpen, 0.05f,
                "With the cell back, the wing settles open wider.");
        }

        [UnityTest]
        public IEnumerator BoostCoils_GiveAGentleExtraCruise_AndTheDrumsGlowWithIt()
        {
            Spawn();
            Abilities.Grant(RoverAbility.BoostCoils);
            _rover.Drive.Drive = new Vector2(0f, 1f);
            float top = _rover.Tuning.Drive.TopSpeed;
            float fastest = 0f;
            float brightest = 0f;
            float until = Time.time + 9f;
            while (Time.time < until)
            {
                yield return null;
                fastest = Mathf.Max(fastest, _rover.Controller.Speed);
                brightest = Mathf.Max(brightest, GlowOf(_rover.DrumGlows[0]));
            }

            float extra = _rover.Tuning.Boost.ExtraSpeed;
            Debug.Log($"[rover-kit] boost: top {top:0.0} m/s, fastest {fastest:0.00} m/s, drum glow {brightest:0.00}");
            Assert.Greater(fastest, top + 0.8f * extra, "Held at cruise on flat ground: a little faster.");
            Assert.Less(fastest, top + extra + 0.2f, "Gently: never more than the tuned extra.");
            CollectionAssert.AreEqual(new[] { true }, _boosts, "Engaging is announced once.");
            Assert.Greater(brightest, 0.8f * _rover.RigTuning.Kit.DrumGlow, "The drums glow with the boost.");

            _rover.Drive.Drive = new Vector2(0f, 0.5f);
            yield return Wait(3f);
            CollectionAssert.AreEqual(new[] { true, false }, _boosts, "Easing off lets go.");
            Assert.Less(GlowOf(_rover.DrumGlows[0]), 0.3f * _rover.RigTuning.Kit.DrumGlow, "The glow fades.");
            _rover.Drive.Drive = Vector2.zero;
        }

        [UnityTest]
        public IEnumerator WithoutTheCoils_NoBoost()
        {
            Spawn();
            _rover.Drive.Drive = new Vector2(0f, 1f);
            float fastest = 0f;
            float until = Time.time + 8f;
            while (Time.time < until)
            {
                yield return null;
                fastest = Mathf.Max(fastest, _rover.Controller.Speed);
            }

            Assert.LessOrEqual(fastest, _rover.Tuning.Drive.TopSpeed + 0.05f);
            Assert.IsEmpty(_boosts);
            _rover.Drive.Drive = Vector2.zero;
        }

        private static float GlowOf(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block.GetVector(Shader.PropertyToID("_EmissionColor")).x;
        }
    }
}
