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
    /// fake rover: the reader takes a tube 07 brought up, decodes it and hands it off, and the plotter in the bay draws
    /// its sheet only once the bay is awake; the stick turns the turntable round the stations, looks at one and comes
    /// away, and brings the platform down turned toward the dock.
    /// </summary>
    public sealed class LabSessions : InputTestFixture
    {
        private const string CargoCradle = "rover.cargo_cradle";

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
        public IEnumerator TheReader_TakesTheTube_DecodesIt_AndThePlotterDrawsOnceTheBayWakes()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            Blueprints blueprints = gameplay.Blueprints;
            blueprints.Restore(new BlueprintsSaveData { carried = new[] { CargoCradle } });
            var log = new List<string>();
            float handOffSeconds = 0f;
            EventBus events = _fixture.Bootstrap.Context.Events;
            using var taken = events.Subscribe<BlueprintDelivered>(e => log.Add("taken " + e.KitId));
            using var decoded = events.Subscribe<FindDecoded>(e => log.Add("decoded " + e.KitId));
            using var handOff = events.Subscribe<DecodeHandOff>(e =>
            {
                log.Add("hand-off " + e.KitId);
                handOffSeconds = e.Seconds;
            });
            using var drawing = events.Subscribe<BlueprintDrawing>(e => log.Add("drawing " + e.KitId));

            yield return _fixture.RideIntoTheLab();
            LiftStation lift = gameplay.Lift;
            Assert.AreEqual(LabStation.Reader, lift.Lab.Facing, "carrying a tube, 07 is turned to the reader");
            ProgressionTuning tuning = _fixture.ProgressionTuning;
            yield return Waits.Until(() => log.Contains("taken " + CargoCradle), tuning.ReaderFeedSeconds + 1f);
            Assert.AreEqual(ReaderPhase.Decoding, lift.ReaderPhase, "the beam fed it in: the reel turns");
            Assert.IsFalse(blueprints.IsCarrying, "the tube left 07's back");
            Assert.IsFalse(blueprints.IsPinned(CargoCradle), "not decoded yet: a moment, not a timer");

            yield return Waits.Until(() => log.Contains("decoded " + CargoCradle), tuning.ReaderDecodeSeconds + 1f);
            CollectionAssert.AreEqual(new[] { "taken " + CargoCradle, "decoded " + CargoCradle,
                "hand-off " + CargoCradle }, log, "taken, decoded, then the hand-off pulse, once each");
            Assert.AreEqual(tuning.HandOffSeconds, handOffSeconds);
            Assert.IsTrue(blueprints.IsPinned(CargoCradle), "decoded: the bay can build it");
            Assert.AreEqual(ReaderPhase.Idle, lift.ReaderPhase);
            Assert.IsTrue(System.IO.File.Exists(_fixture.Bootstrap.Context.Get<MoonProject.Core.Save.ISaveService>()
                .FilePath), "the decode is saved");

            yield return new WaitForSeconds(handOffSeconds + 0.5f);
            Assert.IsFalse(gameplay.Workshop.Awake, "the bay still sleeps");
            Assert.IsFalse(gameplay.Plotter.Drawing, "the pulse stops at the dark bay: its sheet waits");
            Assert.IsFalse(blueprints.IsDrawn(CargoCradle));

            _fixture.PowerTo(2);
            yield return Waits.Until(() => gameplay.Plotter.Drawing, 3f);
            Assert.AreEqual(CargoCradle, gameplay.Plotter.DrawingKit, "the woken bay draws the waiting sheet");
            Assert.AreEqual("drawing " + CargoCradle, log[log.Count - 1]);
            yield return Waits.Until(() => blueprints.IsDrawn(CargoCradle), tuning.PlotterDrawSeconds + 1f);
            Assert.IsTrue(gameplay.Plotter.ShowsSheet(CargoCradle), "and hangs it in the bay");
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
