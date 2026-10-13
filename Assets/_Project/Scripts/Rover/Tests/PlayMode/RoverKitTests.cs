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
    /// Visible progression on 07 (M3-11, real wiring): a loaded game's fitted parts are there at once
    /// (<see cref="IRoverEquipment"/>); an ability alone puts nothing on 07 (bay kit is fitted by the Rover Bay:
    /// <see cref="RoverBayTests"/>); the cargo seat follows the rack; Tilly's gifted cell and Kenji's port parts grow
    /// in place when fitted, Bell's gift shows silently on load; a part taken off shrinks away; the Boost Coils give a
    /// gentle extra cruise and the drums glow with it.
    /// </summary>
    public sealed class RoverKitTests : InputTestFixture
    {
        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;
        private readonly List<FeelBeat> _installing = new List<FeelBeat>();
        private readonly List<FeelBeat> _fitted = new List<FeelBeat>();
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
            events.Subscribe<FeelBeat>(beat =>
            {
                if (beat.Kind == FeelKind.Install && beat.Phase == FeelPhase.Anticipate)
                {
                    _installing.Add(beat);
                }
                else if (beat.Kind == FeelKind.Install && beat.Phase == FeelPhase.Impact)
                {
                    _fitted.Add(beat);
                }
            });
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

        /// <summary>Gameplay fits <paramref name="part"/> in play.</summary>
        private void Fit(RoverKitPiece part, bool gift)
        {
            _rover.Context.Events.Publish(new PartFitted(TestEquipment.SocketFor(part), part, gift, false, default, 0));
        }

        [UnityTest]
        public IEnumerator ACarriedStoryPart_ShowsOnItsOwn_UntilDelivered()
        {
            Spawn();
            yield return null;
            Transform[] parts = _rover.StoryPartNodes;
            foreach (Transform part in parts)
            {
                Assert.IsFalse(part.gameObject.activeSelf, $"{part.name} hidden until 07 carries it.");
            }

            EventBus events = _rover.Context.Events;
            events.Publish(new ComponentFound(TestRover.StoryParts[1], 2, Vector3.zero, false));
            yield return Wait(1f);
            Assert.IsTrue(parts[1].gameObject.activeSelf, "The carried part rides on 07's back.");
            Assert.IsFalse(parts[0].gameObject.activeSelf || parts[2].gameObject.activeSelf, "Only that one.");
            events.Publish(new ComponentDelivered(TestRover.StoryParts[1], 2, Vector3.zero));
            yield return Wait(2f);
            Assert.IsFalse(parts[1].gameObject.activeSelf, "Delivered, it eases off 07's back.");
        }

        [UnityTest]
        public IEnumerator ARestoredStoryPart_IsOn07AtOnce()
        {
            Spawn();
            _rover.Context.Events.Publish(new ComponentFound(TestRover.StoryParts[0], 1, Vector3.zero, true));
            yield return null;
            yield return null;
            Assert.IsTrue(_rover.StoryPartNodes[0].gameObject.activeSelf, "A loaded game's part is there at once.");
            Assert.AreEqual(Vector3.one * 0.2f, _rover.StoryPartNodes[0].localScale, "At full size, no ride-in.");
        }

        [UnityTest]
        public IEnumerator FittedOnLoad_IsThereAtOnce_NoMoment()
        {
            Spawn();
            _rover.Equipment.Fitted(RoverKitPiece.LampBar);
            _rover.Equipment.Fitted(RoverKitPiece.CapacitorDrums);
            _rover.Equipment.Fitted(RoverKitPiece.CargoRack);
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
        public IEnumerator AnAbilityAlone_PutsNoKitOn07()
        {
            Spawn();
            yield return Wait(0.5f);
            float plainAngle = _rover.Headlamp.spotAngle;
            Abilities.Grant(RoverAbility.WarmHeadlamp);
            yield return Wait(0.5f);
            Assert.IsFalse(_rover.LampBar.gameObject.activeSelf, "Crafted, the bar waits in the row to be fitted.");
            Assert.IsEmpty(_installing);
            Assert.AreEqual(plainAngle, _rover.Headlamp.spotAngle, 0.01f, "The plain road light until it is on.");
        }

        [UnityTest]
        public IEnumerator APartTakenOff_ShrinksAway()
        {
            Spawn();
            _rover.Equipment.Fitted(RoverKitPiece.CargoRack);
            yield return Wait(0.5f);
            Assert.IsTrue(_rover.CargoRack.gameObject.activeSelf);
            _rover.Context.Events.Publish(new PartRemoved(RoverSocket.Hold, RoverKitPiece.CargoRack));
            yield return null;
            yield return null;
            Assert.IsTrue(_rover.CargoRack.gameObject.activeSelf, "Not gone in a frame: it shrinks away...");
            Assert.Less(_rover.CargoRack.localScale.x, 1f);
            yield return Wait(2f);
            Assert.IsFalse(_rover.CargoRack.gameObject.activeSelf, "...then it is off.");
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
        public IEnumerator Gifts_ShowSilentlyOnLoad_OrSettleSoftlyWhenFitted()
        {
            Spawn();
            _rover.Friends.Bell.Activity = FriendActivity.Home;
            yield return null;
            yield return null;
            Assert.IsTrue(_rover.FreshSerial.gameObject.activeSelf, "Bell home on load: the fresh '07' is there.");
            Assert.IsTrue(_rover.Pennant.gameObject.activeSelf, "...and the pennant.");
            Assert.IsFalse(_rover.CellFilled.gameObject.activeSelf, "Tilly still broken: the wing's gap remains.");
            Assert.IsEmpty(_installing, "Silently.");

            Fit(RoverKitPiece.SolarCell, true);
            Assert.AreEqual(1, _installing.Count, "Tilly's cell, fitted when 07 is home: its moment begins.");
            Assert.AreEqual(0.5f, _installing[0].Strength, "The soft version.");
            yield return Wait(3f);
            Assert.IsTrue(_rover.CellFilled.gameObject.activeSelf, "The cell is in the wing.");
            Assert.AreEqual(1, _fitted.Count);
            Assert.AreEqual(0.5f, _fitted[0].Strength, "A gift's soft settle.");
            Assert.IsTrue(_rover.Kit.HasMendedWing);

            yield return Wait(_rover.CharacterTuning.IdleDelay + 16f);
            Debug.Log($"[rover-kit] mended wing rests open at {_rover.BodyLanguage.Mood.WingOpen:0.00}");
            Assert.AreEqual(_rover.CharacterTuning.WingIdleOpenMended, _rover.BodyLanguage.Mood.WingOpen, 0.05f,
                "With the cell back, the wing settles open wider.");
        }

        [UnityTest]
        public IEnumerator KenjisPort_FitsTheRingThenTheDeck_EachWithItsMoment()
        {
            Spawn();
            yield return null;
            Assert.IsFalse(_rover.TetherRing.gameObject.activeSelf, "No ring before Kenji's port (first-hour v2).");
            Assert.IsFalse(_rover.TapeDeck.gameObject.activeSelf, "No deck either.");

            foreach (RoverKitPiece piece in new[] { RoverKitPiece.TetherRing, RoverKitPiece.TapeDeck })
            {
                _installing.Clear();
                _fitted.Clear();
                Fit(piece, false);
                Assert.AreEqual(1, _installing.Count, $"{piece}: the port's install moment begins.");
                Assert.AreEqual(1f, _installing[0].Strength, "a port part, not a friend's gift");
                yield return Wait(3f);
                Assert.AreEqual(1, _fitted.Count, $"{piece}: it seats once it is on 07.");
                Assert.AreEqual(1f, _fitted[0].Strength);
            }

            Assert.IsTrue(_rover.TetherRing.gameObject.activeSelf, "The ring frames the eye.");
            Assert.IsTrue(_rover.TapeDeck.gameObject.activeSelf, "The deck is on 07's flank.");
            _installing.Clear();
            Fit(RoverKitPiece.TapeDeck, false);
            Assert.IsEmpty(_installing, "A piece already on 07 has no second moment.");
        }

        [UnityTest]
        public IEnumerator ALoadedGamesPortParts_AreThereAtOnce_NoMoment()
        {
            Spawn();
            _rover.Equipment.Fitted(RoverKitPiece.TetherRing);
            _rover.Equipment.Fitted(RoverKitPiece.TapeDeck);
            yield return null;
            yield return null;
            Assert.IsTrue(_rover.TetherRing.gameObject.activeSelf && _rover.TapeDeck.gameObject.activeSelf,
                "There from the first frame.");
            Assert.IsEmpty(_installing, "No moment replayed on load.");
            Assert.IsEmpty(_fitted);
        }

        [UnityTest]
        public IEnumerator BoostCoils_GiveAGentleExtraCruise_AndTheDrumsGlowWithIt()
        {
            Spawn();
            _rover.Equip(RoverKitPiece.CapacitorDrums);
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
