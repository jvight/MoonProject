using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The two-tier goals (docs/features/M3-21): the tech tree and the recovery rows are read from what the game holds
    /// and announced once after a load; the "next" line teaches in order, then names only the targets' seen needs.
    /// </summary>
    public sealed class GoalsTests
    {
        private static readonly TechKit[] Kits =
        {
            new TechKit(TechNodeId.HoverJump, "rover.hover_jump"),
            new TechKit(TechNodeId.CargoCradle, "rover.cargo_cradle"),
        };

        private readonly HashSet<string> _decoded = new HashSet<string>();
        private readonly Dictionary<string, int> _fitted = new Dictionary<string, int>();
        private EventBus _events;
        private int _tower;
        private int _masts;
        private int _chain;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _decoded.Clear();
            _fitted.Clear();
            _tower = 0;
            _masts = 0;
            _chain = 0;
        }

        [Test]
        public void TechTree_DerivesEachNodeFromTheGame_AndAnnouncesOnlyChanges()
        {
            var changes = new List<TechNodeChanged>();
            _events.Subscribe<TechNodeChanged>(changes.Add);
            TechTree tree = NewTree();

            tree.Refresh();
            Assert.IsEmpty(changes, "a fresh game knows nothing");
            Assert.AreEqual(0, tree.KnownCount);

            _tower = 1;
            _decoded.Add("rover.hover_jump");
            tree.Refresh();
            Assert.AreEqual(TechNodeState.Built, tree.StateOf(TechNodeId.TowerL1));
            Assert.AreEqual(TechNodeState.Decoded, tree.StateOf(TechNodeId.HoverJump));
            Assert.AreEqual(2, tree.KnownCount, "the porthole counts decoded and built nodes");
            Assert.AreEqual(2, changes.Count);
            Assert.IsFalse(changes[0].Restored);

            tree.Refresh();
            Assert.AreEqual(2, changes.Count, "nothing moved, nothing said");

            _fitted["rover.hover_jump"] = 1;
            _masts = 1;
            tree.Refresh();
            Assert.AreEqual(TechNodeState.Built, tree.StateOf(TechNodeId.HoverJump), "fitted in the bay");
            Assert.AreEqual(TechNodeState.Built, tree.StateOf(TechNodeId.RelayNetwork), "the first mast lit");
            Assert.AreEqual(TechNodeState.Unknown, tree.StateOf(TechNodeId.CellPackII), "not in this version yet");
            Assert.AreEqual(3, tree.KnownCount);
            Assert.AreEqual(4, changes.Count);
        }

        [Test]
        public void TechTree_AfterALoad_AnnouncesEachKnownNodeOnce_Restored()
        {
            var changes = new List<TechNodeChanged>();
            _events.Subscribe<TechNodeChanged>(changes.Add);
            TechTree tree = NewTree();
            _tower = 2;
            _decoded.Add("rover.cargo_cradle");

            tree.Announce();
            Assert.AreEqual(3, changes.Count, "L1, L2 and the cradle's sheet");
            Assert.IsTrue(changes.TrueForAll(change => change.Restored), "a load shows them at once, no moments");
            Assert.AreEqual(3, tree.KnownCount);

            tree.Refresh();
            Assert.AreEqual(3, changes.Count, "and nothing replays");
        }

        [Test]
        public void TechTree_RefusesAKitOnAStationNode_OrTwiceTheSameKit()
        {
            Assert.Throws<ArgumentException>(() => NewTree(new TechKit(TechNodeId.TowerL3, "rover.hover_jump")));
            Assert.Throws<ArgumentException>(() => NewTree(new TechKit(TechNodeId.HoverJump, "rover.hover_jump"),
                new TechKit(TechNodeId.CargoCradle, "rover.hover_jump")));
        }

        [Test]
        public void RecoveryRows_LightAsTheTowerAndTheChainLight_AndADarkRowStaysDark()
        {
            var changes = new List<RecoveryProgressChanged>();
            _events.Subscribe<RecoveryProgressChanged>(changes.Add);
            RecoveryProgress rows = NewRows();
            Assert.AreEqual(3, rows.Lamps(RecoveryTarget.Power));
            Assert.AreEqual(3, rows.Lamps(RecoveryTarget.Link), "two chain masts and the Peak's foot");
            Assert.AreEqual(4, rows.Lamps(RecoveryTarget.Dish));
            Assert.AreEqual(1, rows.Lamps(RecoveryTarget.Answer));

            rows.Announce();
            Assert.AreEqual(4, changes.Count, "after a load, every row once");
            Assert.IsTrue(changes.TrueForAll(change => change.Restored && change.LitLamps == 0));
            changes.Clear();

            _tower = 3;
            _chain = 2;
            rows.Refresh();
            Assert.IsTrue(rows.IsComplete(RecoveryTarget.Power), "Power 3/3: the first main target done");
            Assert.AreEqual(2, rows.LitLamps(RecoveryTarget.Link));
            Assert.IsFalse(rows.IsComplete(RecoveryTarget.Link), "the Peak's foot waits for a later version");
            Assert.AreEqual(0, rows.LitLamps(RecoveryTarget.Dish));
            Assert.AreEqual(2, changes.Count);
            Assert.AreEqual(RecoveryTarget.Power, changes[0].Target);
            Assert.AreEqual(3, changes[0].LitLamps);
            Assert.IsFalse(changes[0].Restored);
        }

        [Test]
        public void TheNextLine_TeachesInOrder_ThenNamesOnlyTheTargetsSeenNeeds_ThenFallsQuiet()
        {
            var lines = new List<string>();
            _events.Subscribe<NextStepChanged>(changed => lines.Add(changed.LineKey));
            bool taught = false;
            bool part = false;
            bool moundSeen = false;
            var steps = new StepFlow(_events);
            steps.Add("next.teach", () => taught);
            steps.AddNeed("next.power.part", () => part, () => true);
            steps.AddNeed("next.link.relay.0", () => _chain >= 1, () => moundSeen);
            steps.AddNeed("next.link", () => _chain >= 2, () => true);
            Assert.Throws<InvalidOperationException>(() => steps.Add("next.late", () => false));

            steps.Tick();
            Assert.AreEqual("next.teach", steps.Current);
            Assert.IsFalse(steps.PastTeaching);

            taught = true;
            steps.Tick();
            Assert.AreEqual("next.power.part", steps.Current, "Power first");
            Assert.IsTrue(steps.PastTeaching);

            part = true;
            steps.Tick();
            Assert.AreEqual("next.link", steps.Current, "the mound mast is not seen yet: it is not named");

            moundSeen = true;
            steps.Tick();
            Assert.AreEqual("next.link.relay.0", steps.Current, "seen, it may be named");

            _chain = 2;
            steps.Tick();
            Assert.AreEqual(string.Empty, steps.Current, "nothing else is reachable: the line falls quiet");
            CollectionAssert.AreEqual(new[] { "next.teach", "next.power.part", "next.link", "next.link.relay.0", "" },
                lines);
            Assert.IsTrue(lines.TrueForAll(line => line == "next.teach" || ProgressionKeys.SpeaksOfTargets(line)));
        }

        [Test]
        public void TheFirstEveningAfterARowCompletes_HoldsLongerOnTheTower_Once()
        {
            var opened = new List<DockEveningOpened>();
            _events.Subscribe<DockEveningOpened>(opened.Add);
            using (var evening = new DockEvening(_events, 4f, 2f))
            {
                _events.Publish(new RecoveryProgressChanged(RecoveryTarget.Power, 3, 3, true));
                Assert.IsFalse(evening.HasNews, "a load is no news");

                _events.Publish(new RecoveryProgressChanged(RecoveryTarget.Power, 3, 3, false));
                _events.Publish(new RoverDockChanged(true, Vector3.zero, Quaternion.identity));
                Assert.IsFalse(evening.Tick(0f));
                Assert.AreEqual(2f, opened[0].TowerHold, "Power restored: a beat longer on the tower");
                Assert.IsFalse(evening.Tick(5.9f), "the sweep waits for the longer hold");
                Assert.IsTrue(evening.Tick(6f));

                _events.Publish(new RoverDockChanged(false, Vector3.zero, Quaternion.identity));
                _events.Publish(new RecoveryProgressChanged(RecoveryTarget.Link, 1, 3, false));
                _events.Publish(new RelayRestored("relay.0", Vector3.zero, 1, 4, "home", 1f));
                _events.Publish(new RoverDockChanged(true, Vector3.zero, Quaternion.identity));
                evening.Tick(10f);
                Assert.AreEqual(0f, opened[1].TowerHold, "a lamp lit is no completed row: the usual hold");
            }
        }

        private TechTree NewTree(params TechKit[] kits)
        {
            return new TechTree(_events, () => _tower, () => _masts, kit => _decoded.Contains(kit),
                kit => _fitted.TryGetValue(kit, out int level) ? level : 0, kits.Length > 0 ? kits : Kits);
        }

        private RecoveryProgress NewRows()
        {
            return new RecoveryProgress(_events, () => _tower, 3, () => _chain, 2, new RecoveryLamps(1, 4, 1));
        }
    }
}
