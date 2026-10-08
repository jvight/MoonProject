using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Scripted sessions for the Cargo Cradle (docs/features/M3-11) on the real components behind the fake rover's
    /// cargo seat: a press lifts an aimed relic into the rack instead of towing it, it rides there through driving and
    /// teleports, a press at the shelf sets it down there, it survives a save and a reboot, the tether still takes a
    /// second relic, and with the cradle off 07 the relic is set down loose (nothing is lost).
    /// </summary>
    public sealed class CradleSessions : InputTestFixture
    {
        /// <summary>Seconds a held press may take to be seen (a slow first frame must not eat it).</summary>
        private const float PressTimeout = 1f;

        private const string Duck = "rubber_duck";
        private const string Gnome = "garden_gnome";
        private const string CargoCradle = "rover.cargo_cradle";

        /// <summary>A spot well out of the museum shelf's reach.</summary>
        private static readonly Vector3 AwayFromHome = new Vector3(-30f, 0f, -20f);

        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Cradle_LiftsAnAimedRelicIn_CarriesIt_AndSetsItDownOnTheShelf()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            CargoCradle cradle = _fixture.Gameplay.Cradle;
            TetherSystem tether = _fixture.Gameplay.Tether;
            Relic duck = LooseRelicAt(Duck, new Vector3(1f, 0.6f, 9f));
            yield return new WaitForSeconds(1f);
            yield return AimAt(duck);
            Assert.AreEqual(TetherAimState.Hovering, tether.State);
            Assert.IsFalse(cradle.CanStow, "no cradle on 07 yet");
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Tether, out _), "so it is a tow");

            GiveCradle();
            yield return null;
            Assert.IsTrue(cradle.CanStow);
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Stow, out InteractionHint stow),
                "the prompt says it goes in the rack");
            Assert.Less(Vector3.Distance(duck.transform.position, stow.Position), 1e-3f);
            Assert.IsFalse(_fixture.Gameplay.Hints.TryGet(InteractionKind.Tether, out _));

            Press(_mouse.rightButton, queueEventOnly: true);
            yield return Waits.Until(() => cradle.Carried != null, PressTimeout);
            Assert.AreSame(duck, cradle.Carried, "the press lifts it into the rack...");
            Assert.AreEqual(RelicState.Cradled, duck.State);
            Assert.IsTrue(duck.Body.isKinematic, "no physics fight");
            Assert.AreNotEqual(TetherAimState.Towing, tether.State, "...instead of towing it");
            Assert.IsEmpty(_fixture.Events.TetherAttached);
            Assert.IsFalse(cradle.IsSettled, "it floats in gently");
            yield return new WaitForSeconds(0.2f);
            _fixture.Rover.Aim(_fixture.Rover.Position + new Vector3(5f, 3f, -3f), duck.transform.position);
            _fixture.Capture("40-cradle-lifting");
            yield return Waits.Until(() => _fixture.Events.RelicStowed.Count > 0, 5f);
            Release(_mouse.rightButton);
            Assert.AreEqual(1, _fixture.Events.RelicStowed.Count, "it settles in");
            Assert.AreEqual(Duck, _fixture.Events.RelicStowed[0].Value.RelicId);
            Assert.IsTrue(cradle.IsSettled);
            yield return null;
            AssertSeated(duck);
            Assert.IsFalse(duck.AnswersSonar, "it rides with 07: nothing to find");

            Vector3 start = _fixture.Rover.Position;
            float began = Time.time;
            while (Time.time - began < 2f)
            {
                float t = Time.time - began;
                _fixture.Rover.MoveTo(start + new Vector3(Mathf.Sin(t * 3f) * 2f, 0f, 6f * t), t * 40f);
                yield return null;
                AssertSeated(duck);
            }

            _fixture.Rover.Place(new Vector3(-60f, 0f, 20f), 120f);
            yield return null;
            AssertSeated(duck);
            Assert.AreEqual(RelicState.Cradled, duck.State, "a recovery or a hop takes it along in the rack");
            _fixture.Rover.Aim(_fixture.Rover.Position + new Vector3(-4f, 3f, 4f), duck.transform.position);
            _fixture.Capture("41-cradle-riding");

            HomeBase home = _fixture.Gameplay.Home;
            Vector3 front = (Vector3.zero - home.ShelfPosition).normalized;
            front.y = 0f;
            Vector3 park = home.ShelfPosition + front * 4f;
            _fixture.Rover.Place(new Vector3(park.x, 0f, park.z), Mathf.Atan2(-front.x, -front.z) * Mathf.Rad2Deg);
            yield return null;
            yield return null;
            Assert.IsTrue(cradle.CanUnload, "parked by the shelf, the rack is in reach of it");
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Deposit, out InteractionHint deposit));
            Assert.AreEqual(home.ShelfPosition, deposit.Position);
            Assert.GreaterOrEqual(home.HintedSlot, 0, "its slot glows");

            Press(_mouse.rightButton, queueEventOnly: true);
            yield return Waits.Until(() => cradle.Carried == null, PressTimeout);
            Release(_mouse.rightButton);
            Assert.AreEqual(RelicState.Depositing, duck.State, "the press sets it down onto the shelf");
            Assert.IsEmpty(_fixture.Events.TetherAttached, "and reaches for nothing else");
            yield return Waits.Until(() => _fixture.Events.RelicDeposited.Count > 0,
                _fixture.BaseTuning.DepositDuration + 2f);
            Assert.AreEqual(Duck, _fixture.Events.RelicDeposited[0].Value.RelicId);
            Assert.AreEqual(RelicState.Displayed, duck.State);
        }

        [UnityTest]
        public IEnumerator Cradle_KeepsItsRelicThroughAReboot_AndTheTetherTakesASecondOne()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            _fixture.GiveMaterials(4, 0, 0);
            Assert.AreEqual(PurchaseResult.Purchased, _fixture.Gameplay.Upgrades.Purchase(CargoCradle));

            // Away from the museum shelf, where a press would set the rack's relic down instead.
            _fixture.Rover.Place(AwayFromHome, 0f);
            Relic duck = LooseRelicAt(Duck, AwayFromHome + new Vector3(1f, 0.6f, 9f));
            yield return new WaitForSeconds(1f);
            yield return AimAt(duck);
            Press(_mouse.rightButton, queueEventOnly: true);
            yield return Waits.Until(() => _fixture.Events.RelicStowed.Count > 0, 5f);
            Release(_mouse.rightButton);
            Assert.IsTrue(_fixture.Gameplay.Cradle.IsSettled, "stowing is a checkpoint");
            _fixture.Dispose(true);
            yield return null;

            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            yield return null;
            CargoCradle cradle = _fixture.Gameplay.Cradle;
            duck = _fixture.FindRelic(Duck);
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.CargoCradle), "the cradle is back on 07");
            Assert.AreEqual(RelicState.Cradled, duck.State, "and the relic in it");
            Assert.AreSame(duck, cradle.Carried);
            Assert.IsTrue(cradle.IsSettled);
            AssertSeated(duck);
            Assert.AreEqual(0, _fixture.Events.RelicStowed.Count, "a load is not a stow");

            _fixture.Rover.Place(AwayFromHome, 0f);
            yield return null;
            AssertSeated(duck);
            Relic gnome = LooseRelicAt(Gnome, AwayFromHome + new Vector3(-1f, 0.6f, 9f));
            yield return new WaitForSeconds(1f);
            yield return AimAt(gnome);
            Assert.IsFalse(cradle.CanStow, "one relic at a time");
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Tether, out _));
            Press(_mouse.rightButton, queueEventOnly: true);
            yield return Waits.Until(() => _fixture.Events.TetherAttached.Count > 0, PressTimeout);
            Assert.AreSame(gnome, _fixture.Gameplay.Tether.Towed, "the second relic goes on the tether");
            Assert.AreSame(duck, cradle.Carried, "while the first rides in the rack");
            Release(_mouse.rightButton);
        }

        [UnityTest]
        public IEnumerator Cradle_TakenOff07_SetsItsRelicDownLoose()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GiveCradle();
            Relic duck = LooseRelicAt(Duck, new Vector3(1f, 0.6f, 9f));
            yield return new WaitForSeconds(1f);
            yield return AimAt(duck);
            Press(_mouse.rightButton, queueEventOnly: true);
            yield return Waits.Until(() => _fixture.Events.RelicStowed.Count > 0, 5f);
            Release(_mouse.rightButton);

            _fixture.Rover.SeatRemoved = true;
            yield return null;
            yield return null;
            Assert.IsNull(_fixture.Gameplay.Cradle.Carried);
            Assert.AreEqual(RelicState.Loose, duck.State, "nothing is lost: it is set down loose, looked after");
            Assert.IsFalse(duck.Body.isKinematic);
        }

        [UnityTest]
        public IEnumerator Boot_WithoutTheRoversCargoSeat_FailsLoudly()
        {
            LogAssert.Expect(LogType.Error, new Regex("no IRoverCargoSeat is registered"));
            _fixture = GameplayFixture.Boot(_controls, cargoSeat: false);
            yield return null;
            Assert.IsFalse(_fixture.Gameplay.enabled, "gameplay refuses to run without 07's cargo seat");
        }

        private void GiveCradle()
        {
            _fixture.GiveMaterials(4, 0, 0);
            Assert.AreEqual(PurchaseResult.Purchased, _fixture.Gameplay.Upgrades.Purchase(CargoCradle));
            Assert.IsTrue(_fixture.Rover.IsFitted, "the rack is on 07");
        }

        private Relic LooseRelicAt(string id, Vector3 position)
        {
            Relic relic = _fixture.FindRelic(id);
            relic.BeginLift();
            relic.SetLiftPose(position, Quaternion.identity, 1f);
            relic.Surface(Vector3.zero, Vector3.zero);
            return relic;
        }

        private IEnumerator AimAt(Relic relic)
        {
            Vector3 camera = _fixture.Rover.Camera.transform.position;
            _fixture.Rover.Aim(camera, relic.transform.position);
            yield return null;
            yield return null;
            Assert.AreSame(relic, _fixture.Gameplay.Tether.Hovered);
        }

        private void AssertSeated(Relic relic)
        {
            Transform seat = _fixture.Rover.CargoSeat;
            Vector3 expected = seat.position + seat.rotation * Vector3.up * relic.RestHeight;
            Assert.Less(Vector3.Distance(expected, relic.transform.position), 1e-3f, "it sits in the rack");
            Assert.Less(Quaternion.Angle(seat.rotation, relic.transform.rotation), 0.1f);
        }
    }
}
