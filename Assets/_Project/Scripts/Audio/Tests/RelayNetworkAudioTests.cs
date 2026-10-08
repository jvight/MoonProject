using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.Tests
{
    /// <summary>The relay network in the mix: distance through the network, the radio-hop and home's answer.</summary>
    public sealed class RelayNetworkAudioTests
    {
        private const float Frame = 1f / 60f;
        private const float MastReach = 110f;

        // 320 m from home: past the radio's signal edge (60 m clear + 140 m falloff) and into the near-silence.
        private static readonly Vector3 OutThere = new Vector3(320f, 0f, 0f);
        private static readonly Vector3 MastNearby = new Vector3(320f, 0f, 80f);

        private SoundscapeTuning _soundscape;
        private CanyonAudioTuning _canyon;
        private RadioTuning _radio;
        private FakeStationReach _reach;

        [SetUp]
        public void SetUp()
        {
            _soundscape = ScriptableObject.CreateInstance<SoundscapeTuning>();
            _canyon = ScriptableObject.CreateInstance<CanyonAudioTuning>();
            _radio = ScriptableObject.CreateInstance<RadioTuning>();
            _reach = new FakeStationReach(_radio.SignalRadius, MastReach);
            _reach.Add("home", Vector3.zero, true);
            _reach.Add("relay.0", MastNearby, false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_soundscape);
            Object.DestroyImmediate(_canyon);
            Object.DestroyImmediate(_radio);
        }

        private float Edge => _radio.SignalRadius + _radio.FalloffWidth;

        private float Distance(Vector3 position)
        {
            return StationDistance.Equivalent(_reach, position, _radio.SignalRadius, MastReach);
        }

        [Test]
        public void HomeOnly_TheDistanceIsToTheBase()
        {
            Assert.AreEqual(320f, Distance(OutThere), 1e-3f);
            Assert.AreEqual(30f, Distance(new Vector3(30f, 0f, 0f)), 1e-3f);
        }

        [Test]
        public void ALitMast_IsClearWithinItsReach_AndFallsOffFromItsEdge()
        {
            _reach.Light("relay.0");
            Assert.LessOrEqual(Distance(OutThere), _radio.SignalRadius, "80 m from a lit mast: inside its 110 m reach");
            Vector3 pastTheMast = MastNearby + new Vector3(0f, 0f, MastReach + 40f);
            Assert.AreEqual(_radio.SignalRadius + 40f, Distance(pastTheMast), 1e-3f, "40 m past its edge");
        }

        [Test]
        public void InReach_IsAlwaysClear_EvenIfTheRadiiDisagree()
        {
            _reach.Light("relay.0");
            float tooSmall = StationDistance.Equivalent(_reach, OutThere, _radio.SignalRadius, 40f);
            Assert.LessOrEqual(tooSmall, _radio.SignalRadius, "the network says it reaches: the radio is warm");
        }

        [Test]
        public void LightingAMastBeside07_WarmsTheRadioInOverAFewSeconds_NeverASnap()
        {
            var signal = new RadioSignal(_radio);
            var model = new SoundscapeModel(_soundscape, _canyon);
            float distance = Distance(OutThere);
            signal.Snap(distance);
            model.SettleAt(distance, Edge);
            Assert.AreEqual(0f, signal.Clarity, 1e-3f, "static out here");
            Assert.Greater(model.Farness, 0.8f, "and fading towards near-silence");

            _reach.Light("relay.0");
            distance = Distance(OutThere);
            for (int i = 0; i < 30; i++)
            {
                signal.Step(distance, Frame);
                model.Step(distance, Edge, 0f, 0f, 0f, false, Frame);
            }

            Assert.Greater(model.Farness, 0.4f, "half a second after lighting: still warming");
            for (int i = 0; i < 600; i++)
            {
                signal.Step(distance, Frame);
                model.Step(distance, Edge, 0f, 0f, 0f, false, Frame);
            }

            Assert.Less(model.Farness, 0.01f, "warm within a few seconds");
            Assert.Greater(signal.Clarity, 0.99f, "the radio is clear in the mast's reach");
            Assert.AreEqual(1f, model.RadioGain, 0.01f);
        }

        [Test]
        public void ATallerTower_WidensHomesCircle_NotTheMasts()
        {
            _reach.Light("relay.0");
            Vector3 pastTheMast = MastNearby + new Vector3(0f, 0f, MastReach + 40f);
            float before = Distance(pastTheMast) - _radio.SignalRadius;
            float after = StationDistance.Equivalent(_reach, pastTheMast, _radio.SignalRadius + 100f, MastReach) -
                          (_radio.SignalRadius + 100f);
            Assert.AreEqual(before, after, 1e-3f, "still 40 m past the mast's edge");
            Assert.AreEqual(0f, StationDistance.Equivalent(_reach, new Vector3(150f, 0f, -200f),
                _radio.SignalRadius + 300f, MastReach) - (_radio.SignalRadius + 300f), 1e-3f, "inside the wider home");
        }

        [Test]
        public void SettleAt_TakesANewPlaceAtOnce_ForAHop()
        {
            var model = new SoundscapeModel(_soundscape, _canyon);
            model.SettleAt(400f, Edge);
            Assert.AreEqual(1f, model.Farness, 1e-4f);
            model.SettleAt(0f, Edge);
            Assert.AreEqual(0f, model.Farness, 1e-4f);
        }

        [Test]
        public void RadioHop_FollowsTheView_OutThenDarkThenIn_LandingOnce()
        {
            var hop = new RadioHop();
            Assert.IsFalse(hop.Active);
            hop.Begin();
            float t = 0f;
            float fullAt = -1f;
            float landedAt = -1f;
            int landings = 0;
            while (t < 4f)
            {
                hop.Step(Frame, _radio.HopOutTime, _radio.HopDarkTime, _radio.HopInTime, out bool landing);
                t += Frame;
                if (fullAt < 0f && hop.Amount >= 1f)
                {
                    fullAt = t;
                }

                if (landing)
                {
                    landings++;
                    landedAt = t;
                    Assert.AreEqual(1f, hop.Amount, 1e-4f, "the static is full through the dark");
                }

                if (!hop.Active)
                {
                    break;
                }
            }

            Assert.AreEqual(1, landings);
            Assert.AreEqual(_radio.HopOutTime, fullAt, 2f * Frame, "all static as the screen goes dark");
            Assert.AreEqual(_radio.HopOutTime + _radio.HopDarkTime, landedAt, 3f * Frame, "resolving as it eases in");
            Assert.AreEqual(_radio.HopOutTime + _radio.HopDarkTime + _radio.HopInTime, t, 3f * Frame);
            Assert.AreEqual(0f, hop.Amount);
        }

        [Test]
        public void RadioHop_FinishedBeforeItsDarkEnds_ResolvesFromWhereItIs()
        {
            var hop = new RadioHop();
            hop.Begin();
            for (int i = 0; i < 20; i++)
            {
                hop.Step(Frame, 0.8f, 0.4f, 0.8f, out _);
            }

            float before = hop.Amount;
            hop.End();
            hop.Step(Frame, 0.8f, 0.4f, 0.8f, out bool landing);
            Assert.IsTrue(landing);
            Assert.Less(Mathf.Abs(hop.Amount - before), 0.05f, "no jump");
            Assert.Less(hop.Amount, before);

            hop.End();
            hop.Step(Frame, 0.8f, 0.4f, 0.8f, out landing);
            Assert.IsFalse(landing, "a late finish changes nothing");
        }

        [Test]
        public void HomesAnswer_ComesFromTheNearestOtherLitNode_WhenThePulseArrives()
        {
            _reach.Light("relay.0");
            _reach.Add("relay.1", new Vector3(320f, 0f, 260f), true);
            Assert.IsTrue(RelayLink.TryFindLinkedNode(_reach, "relay.1", new Vector3(320f, 6f, 260f), out Vector3 to));
            Assert.AreEqual(MastNearby, to, "the nearest lit node, not itself");

            Assert.AreEqual(2f, RelayLink.AnswerDelay(90f, 45f, 3f), 1e-4f);
            Assert.AreEqual(3f, RelayLink.AnswerDelay(400f, 45f, 3f), 1e-4f, "a long link still answers in time");
        }

        [Test]
        public void HomesAnswer_NeedsAnotherLitNode()
        {
            var lonely = new FakeStationReach(60f, MastReach);
            lonely.Add("relay.0", Vector3.zero, true);
            Assert.IsFalse(RelayLink.TryFindLinkedNode(lonely, "relay.0", Vector3.zero, out _));
        }

        /// <summary>An <see cref="IStationReach"/> over nodes the test lights: home first, then masts.</summary>
        private sealed class FakeStationReach : IStationReach
        {
            private readonly List<RelayNode> _nodes = new List<RelayNode>();
            private readonly float _homeRadius;
            private readonly float _mastReach;

            public FakeStationReach(float homeRadius, float mastReach)
            {
                _homeRadius = homeRadius;
                _mastReach = mastReach;
            }

            public int LitCount => _nodes.FindAll(node => node.Lit).Count;

            public int NodeCount => _nodes.Count;

            public RelayNode GetNode(int index)
            {
                return _nodes[index];
            }

            public bool IsInReach(Vector3 position)
            {
                for (int i = 0; i < _nodes.Count; i++)
                {
                    float radius = i == 0 ? _homeRadius : _mastReach;
                    if (_nodes[i].Lit && SignalField.HorizontalDistance(position, _nodes[i].Position) <= radius)
                    {
                        return true;
                    }
                }

                return false;
            }

            public float DistanceToNearestNode(Vector3 position)
            {
                float best = float.MaxValue;
                foreach (RelayNode node in _nodes)
                {
                    if (node.Lit)
                    {
                        best = Mathf.Min(best, SignalField.HorizontalDistance(position, node.Position));
                    }
                }

                return best;
            }

            public void Add(string id, Vector3 position, bool lit)
            {
                _nodes.Add(new RelayNode(id, position, lit));
            }

            public void Light(string id)
            {
                for (int i = 0; i < _nodes.Count; i++)
                {
                    if (_nodes[i].Id == id)
                    {
                        _nodes[i] = new RelayNode(id, _nodes[i].Position, true);
                    }
                }
            }
        }
    }
}
