using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>Bell's signals: what she picks, how she says it, and how she moves on once it is found.</summary>
    public sealed class BellSignalTests
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly List<BellSignalPicked> _picked = new List<BellSignalPicked>();
        private readonly List<BellSignalFound> _found = new List<BellSignalFound>();
        private readonly List<TickerLine> _ticker = new List<TickerLine>();
        private EventBus _events;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _subscriptions.Add(_events.Subscribe<BellSignalPicked>(_picked.Add));
            _subscriptions.Add(_events.Subscribe<BellSignalFound>(_found.Add));
            _subscriptions.Add(_events.Subscribe<TickerLine>(_ticker.Add));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
            _picked.Clear();
            _found.Clear();
            _ticker.Clear();
        }

        [Test]
        public void Picker_PrefersReachableCassettes_ThenLogs_ThenRelics_NearestToHome()
        {
            var targets = new FakeTargets();
            targets.Add(BellSignalTarget.Relic, "duck", new Vector3(10f, 0f, 0f));
            targets.Add(BellSignalTarget.CrewLog, "ro_1", new Vector3(30f, 0f, 0f));
            targets.Add(BellSignalTarget.Cassette, "far", new Vector3(0f, 0f, 150f));
            targets.Add(BellSignalTarget.Cassette, "near", new Vector3(0f, 0f, 90f));
            targets.Add(BellSignalTarget.Cassette, "gated", new Vector3(0f, 0f, 20f), reachable: false);
            Assert.AreEqual(3, BellSignalPicker.Pick(targets, Vector3.zero),
                "the nearest reachable cassette, never one behind a gate");

            targets.Find("near").Waiting = false;
            Assert.AreEqual(2, BellSignalPicker.Pick(targets, Vector3.zero), "found things are not pointed at");
            targets.Find("far").Waiting = false;
            Assert.AreEqual(1, BellSignalPicker.Pick(targets, Vector3.zero), "no cassette left: the crew log");
            targets.Find("ro_1").Waiting = false;
            Assert.AreEqual(0, BellSignalPicker.Pick(targets, Vector3.zero), "then a relic that has not answered");
            targets.Find("duck").Waiting = false;
            Assert.AreEqual(-1, BellSignalPicker.Pick(targets, Vector3.zero), "nothing reachable left");

            targets.Find("gated").Reachable = true;
            Assert.AreEqual(4, BellSignalPicker.Pick(targets, Vector3.zero), "the gate opened");
            targets.Add(BellSignalTarget.Cassette, "twin", new Vector3(0f, 0f, 20f));
            Assert.AreEqual(4, BellSignalPicker.Pick(targets, Vector3.zero), "a tie goes to the earlier target");
        }

        [Test]
        public void Bearing_IsWholeDegreesClockwiseFromNorth()
        {
            Assert.AreEqual(0, BellSignals.BearingFrom(Vector3.zero, new Vector3(0f, 0f, 10f)));
            Assert.AreEqual(90, BellSignals.BearingFrom(Vector3.zero, new Vector3(10f, 0f, 0f)));
            Assert.AreEqual(180, BellSignals.BearingFrom(Vector3.zero, new Vector3(0f, 0f, -10f)));
            Assert.AreEqual(270, BellSignals.BearingFrom(Vector3.zero, new Vector3(-10f, 0f, 0f)));
            Assert.AreEqual(354, BellSignals.BearingFrom(Vector3.zero, new Vector3(-1.05f, 0f, 10f)));
            Assert.AreEqual(0, BellSignals.BearingFrom(Vector3.zero, new Vector3(-0.01f, 0f, 10f)), "never 360");
            Assert.AreEqual(140, BellSignals.BearingFrom(new Vector3(5f, 2f, 5f),
                new Vector3(5f + Mathf.Sin(140f * Mathf.Deg2Rad), 0f, 5f + Mathf.Cos(140f * Mathf.Deg2Rad))));
        }

        [Test]
        public void Signals_PointAtOneThing_SayWhenItIsFound_AndMoveOn()
        {
            var targets = new FakeTargets();
            targets.Add(BellSignalTarget.Cassette, "dust_and_honey", new Vector3(100f, 0f, 0f));
            targets.Add(BellSignalTarget.CrewLog, "ro_1", new Vector3(0f, 0f, -50f));
            targets.Add(BellSignalTarget.Relic, "duck", new Vector3(-30f, 0f, 0f));
            targets.Add(BellSignalTarget.Relic, "gnome", new Vector3(-60f, 0f, 0f));
            var bell = new BellSignals(_events, targets, Vector3.zero);
            bell.Step();
            Assert.AreEqual(0, _picked.Count, "silent until she is home and listening");

            bell.Start();
            bell.Start();
            Assert.IsTrue(bell.Running);
            Assert.AreEqual(0, bell.Current);
            Assert.AreEqual(1, _picked.Count, "one signal, never stacked");
            Assert.AreEqual(BellSignalTarget.Cassette, _picked[0].Target);
            Assert.AreEqual(new Vector3(100f, 0f, 0f), _picked[0].Position);
            Assert.AreEqual(BellSignals.PickedLine, _ticker[0].Key);
            Assert.AreEqual("90", _ticker[0].Argument, "bearing from home");
            Assert.IsFalse(_ticker[0].ArgumentIsKey, "a plain number");

            for (int frame = 0; frame < 100; frame++)
            {
                bell.Step();
            }

            Assert.AreEqual(1, _picked.Count, "it never expires");
            Assert.AreEqual(0, _found.Count);

            targets.Find("dust_and_honey").Waiting = false;
            bell.Step();
            Assert.AreEqual(1, _found.Count);
            Assert.AreEqual(BellSignalTarget.Cassette, _found[0].Target);
            Assert.AreEqual(BellSignals.FoundLine0, _ticker[1].Key);
            Assert.AreEqual(2, _picked.Count, "and she picks the next");
            Assert.AreEqual(BellSignalTarget.CrewLog, _picked[1].Target);
            Assert.AreEqual("180", _ticker[2].Argument);

            targets.Find("ro_1").Waiting = false;
            bell.Step();
            targets.Find("duck").Waiting = false;
            bell.Step();
            targets.Find("gnome").Waiting = false;
            bell.Step();
            Assert.AreEqual(4, bell.FoundCount);
            Assert.AreEqual(4, _found.Count);
            CollectionAssert.AreEqual(new[]
            {
                BellSignals.FoundLine0, BellSignals.FoundLine1, BellSignals.FoundLine2, BellSignals.FoundLine0,
            }, FoundLines(), "her kind lines in turn");
            Assert.AreEqual(-1, bell.Current, "nothing left: she just plays music");
            int lines = _ticker.Count;
            bell.Step();
            Assert.AreEqual(lines, _ticker.Count, "and stays quiet");

            targets.Add(BellSignalTarget.Cassette, "slow_orbit", new Vector3(0f, 0f, 200f), reachable: false);
            bell.Step();
            Assert.AreEqual(-1, bell.Current, "never behind a gate");
            targets.Find("slow_orbit").Reachable = true;
            bell.Step();
            Assert.AreEqual(4, bell.Current, "a new ability opens a way: she points again");
            Assert.AreEqual("0", _ticker[_ticker.Count - 1].Argument);
        }

        [Test]
        public void Signals_ResumeTheSavedTarget_AndKeepTheirLineCount()
        {
            var targets = new FakeTargets();
            targets.Add(BellSignalTarget.CrewLog, "ro_1", new Vector3(0f, 0f, -50f));
            targets.Add(BellSignalTarget.Relic, "duck", new Vector3(-30f, 0f, 0f));
            var bell = new BellSignals(_events, targets, Vector3.zero);
            bell.Start();
            targets.Find("ro_1").Waiting = false;
            bell.Step();
            Assert.AreEqual(1, bell.Current);
            BellSignalSaveData saved = JsonUtility.FromJson<BellSignalSaveData>(JsonUtility.ToJson(bell.Capture()));
            Assert.AreEqual("duck", saved.targetId);
            Assert.AreEqual((int)BellSignalTarget.Relic, saved.targetKind);
            Assert.AreEqual(1, saved.found);

            var later = new FakeTargets();
            later.Add(BellSignalTarget.CrewLog, "ro_1", new Vector3(0f, 0f, -50f), waiting: false);
            later.Add(BellSignalTarget.Relic, "duck", new Vector3(-30f, 0f, 0f));
            later.Add(BellSignalTarget.Cassette, "slow_orbit", new Vector3(0f, 0f, 200f));
            var resumed = new BellSignals(_events, later, Vector3.zero);
            resumed.Restore(saved);
            Assert.AreEqual(saved.targetId, resumed.Capture().targetId, "kept until she starts");
            resumed.Start();
            Assert.AreEqual(1, resumed.Current, "the signal she was following never expires");
            later.Find("duck").Waiting = false;
            resumed.Step();
            Assert.AreEqual(BellSignals.FoundLine1, _ticker[_ticker.Count - 2].Key, "the line count carried over");
            Assert.AreEqual(2, resumed.Current);

            var stale = new BellSignals(_events, later, Vector3.zero);
            stale.Restore(saved);
            stale.Start();
            Assert.AreEqual(2, stale.Current, "a saved target found meanwhile is replaced by a fresh pick");
        }

        private List<string> FoundLines()
        {
            var lines = new List<string>();
            foreach (TickerLine line in _ticker)
            {
                if (line.Key != BellSignals.PickedLine)
                {
                    lines.Add(line.Key);
                }
            }

            return lines;
        }

        private sealed class FakeTargets : ISignalTargets
        {
            private readonly List<Target> _targets = new List<Target>();

            public int Count => _targets.Count;

            public void Add(BellSignalTarget kind, string id, Vector3 position, bool waiting = true,
                bool reachable = true)
            {
                _targets.Add(new Target
                {
                    Kind = kind, Id = id, Position = position, Waiting = waiting, Reachable = reachable,
                });
            }

            public Target Find(string id)
            {
                return _targets.Find(target => target.Id == id);
            }

            public SignalCandidate Get(int index)
            {
                Target target = _targets[index];
                return new SignalCandidate(target.Kind, target.Id, target.Position, target.Waiting, target.Reachable);
            }

            public sealed class Target
            {
                public BellSignalTarget Kind { get; set; }
                public string Id { get; set; }
                public Vector3 Position { get; set; }
                public bool Waiting { get; set; }
                public bool Reachable { get; set; }
            }
        }
    }
}
