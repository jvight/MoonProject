using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay.Lift;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Scripted sessions for the crew's lab up the belly lift (docs/features/M3-19) on the real components behind the
    /// fake rover: the stick turns the turntable round the stations, looks at one and comes away, and brings the
    /// platform down turned toward the dock.
    /// </summary>
    public sealed class LabSessions : InputTestFixture
    {
        /// <summary>Seconds a key is held, as a player taps the stick.</summary>
        private const float KeySeconds = 0.15f;

        /// <summary>Seconds a turn of the turntable may take (a half turn and its ease).</summary>
        private const float TurnTimeout = 4f;

        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Keyboard _keyboard;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator TheTurntable_TurnsRoundTheStations_LooksAndComesAway_ThenTheLiftComesDownFacingOut()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            LiftStation lift = _fixture.Gameplay.Lift;
            var faced = new List<LabStation>();
            var looks = new List<bool>();
            int left = 0;
            EventBus events = _fixture.Bootstrap.Context.Events;
            using var stops = events.Subscribe<LabStationFaced>(e => faced.Add(e.Station));
            using var looked = events.Subscribe<LabStationLooked>(e => looks.Add(e.Looking));
            using var leaving = events.Subscribe<LabLeft>(_ => left++);

            yield return _fixture.RideIntoTheLab();
            Assert.AreEqual(LabStation.Chart, lift.Lab.Facing, "carrying nothing, 07 arrives facing the chart");
            Assert.IsTrue(lift.Lab.IsInside);
            Assert.AreSame(lift.Lab, _fixture.Bootstrap.Context.Get<ILabInterior>(), "the lab is registered");
            int arrived = faced.Count;

            yield return Tap(_keyboard.dKey);
            yield return Waits.Until(() => faced.Count > arrived && !lift.Turning, TurnTimeout);
            Assert.AreEqual(LabStation.Reader, faced[faced.Count - 1], "right turns it clockwise to the reader");
            yield return Tap(_keyboard.aKey);
            yield return Tap(_keyboard.aKey);
            yield return Waits.Until(() => faced.Count >= arrived + 3 && !lift.Turning, 2f * TurnTimeout);
            Assert.AreEqual(LabStation.Keepsakes, lift.Lab.Facing, "left twice: round past the chart to the shelf");

            yield return Tap(_keyboard.wKey);
            Assert.IsTrue(lift.Lab.IsLooking, "forward looks at the station");
            yield return Tap(_keyboard.sKey);
            Assert.IsFalse(lift.Lab.IsLooking, "back comes away first");
            CollectionAssert.AreEqual(new[] { true, false }, looks);
            Assert.AreEqual(LiftPhase.Inside, lift.Phase, "and keeps 07 in the lab");

            yield return Tap(_keyboard.sKey);
            Assert.AreEqual(LiftPhase.Lowering, lift.Phase, "back again brings the platform down");
            Assert.AreEqual(1, left);
            Assert.IsFalse(lift.Lab.IsInside);
            LiftTuning tuning = _fixture.LiftTuning;
            yield return Waits.Until(() => lift.Phase == LiftPhase.Ready,
                tuning.RideSeconds + tuning.SettleSeconds + 1f);
            Assert.AreEqual(LiftPhase.Ready, lift.Phase);
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount, "set down, 07 is free to drive off");
        }

        private IEnumerator Tap(KeyControl key)
        {
            Press(key);
            yield return new WaitForSeconds(KeySeconds);
            Release(key);
            yield return new WaitForSeconds(KeySeconds);
        }
    }
}
