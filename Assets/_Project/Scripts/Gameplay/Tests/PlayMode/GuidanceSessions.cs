using System;
using System.Collections;
using System.Collections.Generic;
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
    /// Hints only when needed (docs/design/0.5.5-owner-feedback.md §4, text audit 0.5.5): the dock's card waits for a
    /// battery that has been used, the dark tower answers a listen with its plaque lamp, and the first-listen line waits
    /// for a listen away from home that a wreck answers.
    /// </summary>
    public sealed class GuidanceSessions : InputTestFixture
    {
        private const float Settle = 1f;

        private readonly List<IDisposable> _listening = new List<IDisposable>();
        private InputActionAsset _controls;
        private GameplayFixture _fixture;

        public override void Setup()
        {
            base.Setup();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            foreach (IDisposable listening in _listening)
            {
                listening.Dispose();
            }

            _listening.Clear();
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator TheDocksCard_WaitsForABatteryBelowFull()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            var places = new List<string>();
            _listening.Add(_fixture.Bootstrap.Context.Events.Subscribe<PlaceFirstApproached>(
                approached => places.Add(approached.PlaceId)));
            ChargingDock dock = _fixture.Gameplay.Home.Dock;
            BatteryModel battery = _fixture.Gameplay.Battery.Model;
            battery.Restore(new BatterySaveData { cells = battery.Cells, charge = battery.Cells });
            yield return Teleport(dock.Position + Vector3.forward * 40f);
            yield return Teleport(dock.Position);
            yield return new WaitForSeconds(Settle);
            Assert.IsFalse(places.Contains(ProgressionKeys.PlaceDock), "a full battery: the dock has nothing to say");

            battery.Restore(new BatterySaveData { cells = battery.Cells, charge = battery.Cells * 0.5f });
            yield return new WaitForSeconds(_fixture.ProgressionTuning.PlaceLineGap + Settle);
            Assert.IsTrue(places.Contains(ProgressionKeys.PlaceDock), "half a battery, at the dock: its card");
        }

        [UnityTest]
        public IEnumerator TheDarkTower_AnswersWithItsPlaqueLamp()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            RadioTower tower = _fixture.Gameplay.Tower;
            _fixture.Rover.Place(tower.PadCentre + Vector3.back * 60f, 0f);
            yield return new WaitForSeconds(Settle);
            float rest = tower.OfflineLampLevel;
            Assert.Greater(rest, 0f, "offline: the plaque lamp glows");
            _fixture.Rover.Place(tower.PadCentre, 0f);
            float lowest = rest;
            float until = Time.time + _fixture.TowerTuning.OfflineBlinkSeconds;
            while (Time.time < until)
            {
                lowest = Mathf.Min(lowest, tower.OfflineLampLevel);
                yield return null;
            }

            Assert.Less(lowest, rest * 0.5f, "the lamp blinks: the tower says not yet without words");
        }

        [UnityTest]
        public IEnumerator TheFirstListenLine_WaitsForAListenAwayFromHomeThatAWreckAnswers()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            EventBus events = _fixture.Bootstrap.Context.Events;
            Vector3 home = _fixture.Gameplay.Home.Dock.Position;
            events.Publish(new SonarPinged(home, 100f));
            events.Publish(new SiteAnswered("site.depot", home, 40f, false));
            yield return null;
            Assert.IsFalse(Said(ProgressionKeys.FirstListen), "a listen at home says nothing");

            Vector3 away = home + Vector3.forward * (_fixture.ProgressionTuning.ListenAwayFromHome + 10f);
            events.Publish(new SonarPinged(away, 100f));
            yield return null;
            Assert.IsFalse(Said(ProgressionKeys.FirstListen), "nothing answered yet");
            events.Publish(new SiteAnswered("site.depot", away, 40f, false));
            yield return null;
            Assert.IsTrue(Said(ProgressionKeys.FirstListen), "a wreck answers the listen away from home: the line");
        }

        /// <summary>Places 07 as the real rover does: the battery counts no drive for the jump.</summary>
        private IEnumerator Teleport(Vector3 at)
        {
            _fixture.Rover.PlaceAt(at, Quaternion.identity);
            _fixture.Bootstrap.Context.Events.Publish(new RoverPlaced(at, Quaternion.identity));
            yield return null;
        }

        private bool Said(string key)
        {
            return _fixture.Events.TickerLine.Exists(line => line.Value.Key == key);
        }
    }
}
