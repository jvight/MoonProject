using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The first dusk follows progress (docs/design/opening-v3-purpose.md R5–R6): after power-on it begins at the first
    /// of 07 docking after visiting either near site, or 07 learning the battery bank from the power module; never
    /// before power-on, never from a dock before a visit, only once; a loaded game keeps a visit and replays a begun
    /// dusk as Restored.
    /// </summary>
    public sealed class DuskCueTests
    {
        private const float Reach = 12f;

        private static readonly Vector3 Seismic = new Vector3(100f, 0f, 40f);
        private static readonly Vector3 Habitat = new Vector3(-60f, 0f, 120f);
        private static readonly Vector3 Pad = Vector3.zero;

        private readonly List<OpeningDuskBegan> _began = new List<OpeningDuskBegan>();
        private EventBus _events;
        private DuskCue _cue;

        [SetUp]
        public void SetUp()
        {
            _began.Clear();
            _events = new EventBus();
            _events.Subscribe<OpeningDuskBegan>(_began.Add);
            _cue = new DuskCue(_events, new[] { Seismic, Habitat }, Reach);
        }

        [TearDown]
        public void TearDown()
        {
            _cue.Dispose();
        }

        [Test]
        public void AVisitToTheSeismicStation_ThenADock_BeginsTheDusk_Once()
        {
            _cue.Step(true, Seismic + new Vector3(Reach - 1f, 0f, 0f), false);
            Assert.IsTrue(_cue.Visited, "within reach of the station");
            _cue.Step(true, Pad, false);
            Assert.IsFalse(_cue.Begun, "back at the pad, not docked yet: the afternoon holds");

            Dock();
            _cue.Step(true, Pad, false);
            Assert.IsTrue(_cue.Begun, "the next dock after the visit begins the dusk");
            Assert.AreEqual(1, _began.Count);
            Assert.IsFalse(_began[0].Restored, "begun now, not replayed");

            Dock();
            _cue.Step(true, Seismic, true);
            Assert.AreEqual(1, _began.Count, "only once");
        }

        [Test]
        public void AVisitToTheHabitat_ThenADock_BeginsTheDusk()
        {
            _cue.Step(true, Habitat, false);
            Dock();
            _cue.Step(true, Pad, false);
            Assert.IsTrue(_cue.Begun);
            Assert.AreEqual(1, _began.Count);
        }

        [Test]
        public void ScanningThePowerModule_BeginsTheDuskAtOnce_WithNoDock()
        {
            _cue.Step(true, Habitat, true);
            Assert.IsTrue(_cue.Begun, "the battery bank learned: night is the next goal");
            Assert.AreEqual(1, _began.Count);
        }

        [Test]
        public void ADockBeforeAnyVisit_OrAVisitBeforePowerOn_BeginsNothing()
        {
            Dock();
            _cue.Step(true, Pad, false);
            Assert.IsFalse(_cue.Begun, "a dock with no visit is just a charge");

            _cue.Step(true, new Vector3(Seismic.x + Reach + 1f, 0f, Seismic.z), false);
            Assert.IsFalse(_cue.Visited, "beyond the station's reach");

            var before = new DuskCue(_events, new[] { Seismic, Habitat }, Reach);
            before.Step(false, Seismic, true);
            Dock();
            before.Step(false, Pad, false);
            Assert.IsFalse(before.Visited, "a visit before power-on does not count (the habitat's airlock is read at B7)");
            Assert.IsFalse(before.Begun, "nor does the bank learned before power-on: the afternoon comes first");
            before.Step(true, Pad, true);
            Assert.IsTrue(before.Begun, "the bank already known begins the dusk the moment the base is powered");
            before.Dispose();

            Assert.IsFalse(_cue.Begun);
        }

        [Test]
        public void ADockBeforeTheVisit_DoesNotCount_OnlyTheNextOne()
        {
            Dock();
            _cue.Step(true, Seismic, false);
            Assert.IsTrue(_cue.Visited);
            Assert.IsFalse(_cue.Begun, "the dock came before the visit");
            _cue.Step(true, Pad, false);
            Assert.IsFalse(_cue.Begun);
        }

        [Test]
        public void ALoadedGame_KeepsAVisit_AndReplaysABegunDuskAsRestored()
        {
            _cue.Step(true, Seismic, false);
            var visited = new OpeningSaveData();
            _cue.Capture(visited);

            var loaded = new DuskCue(_events, new[] { Seismic, Habitat }, Reach);
            loaded.Restore(visited);
            loaded.Announce();
            Assert.IsEmpty(_began, "visited, not begun: nothing to replay");
            Dock();
            loaded.Step(true, Pad, false);
            Assert.IsTrue(loaded.Begun, "the visit survived the load: the next dock begins the dusk");
            var begun = new OpeningSaveData();
            loaded.Capture(begun);
            loaded.Dispose();

            _began.Clear();
            var reloaded = new DuskCue(_events, new[] { Seismic, Habitat }, Reach);
            reloaded.Restore(begun);
            reloaded.Announce();
            Assert.AreEqual(1, _began.Count, "a begun dusk is stated at the session start");
            Assert.IsTrue(_began[0].Restored, "replayed, so the sky snaps to it");
            Dock();
            reloaded.Step(true, Pad, true);
            Assert.AreEqual(1, _began.Count, "and never begins again");
            reloaded.Dispose();
        }

        private void Dock()
        {
            _events.Publish(new RoverDockChanged(true, Pad, Quaternion.identity));
        }
    }
}
