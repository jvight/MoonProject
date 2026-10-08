using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.Tests
{
    /// <summary>The relay network in the mix: nearest lit node distance, the radio-hop, a mast coming back.</summary>
    public sealed class RelayNetworkAudioTests
    {
        private const float Frame = 1f / 60f;

        // 320 m from home: past the radio's signal edge (60 m clear + 140 m falloff) and into the near-silence.
        private static readonly Vector3 OutThere = new Vector3(320f, 0f, 0f);
        private static readonly Vector3 MastNearby = new Vector3(320f, 0f, 20f);

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
            _reach = new FakeStationReach();
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

        [Test]
        public void HomeOnly_OutThereIsNearSilent_ALitMastNearbyMakesItHomeAgain()
        {
            var signal = new RadioSignal(_radio);
            var model = new SoundscapeModel(_soundscape, _canyon);
            float distance = _reach.DistanceToNearestNode(OutThere);
            Assert.AreEqual(320f, distance, 1e-3f, "home only: the distance is to the base");
            signal.Snap(distance);
            model.SettleAt(distance, Edge);
            Assert.AreEqual(0f, signal.Clarity, 1e-3f, "static out here");
            Assert.Greater(model.Farness, 0.8f, "and fading towards near-silence");

            _reach.Light("relay.0");
            distance = _reach.DistanceToNearestNode(OutThere);
            Assert.AreEqual(20f, distance, 1e-3f, "now the nearest lit node is the mast");
            for (int i = 0; i < 30; i++)
            {
                signal.Step(distance, Frame);
                model.Step(distance, Edge, 0f, 0f, 0f, false, Frame);
            }

            Assert.Greater(model.Farness, 0.4f, "half a second after lighting: still warming, never a snap");
            for (int i = 0; i < 600; i++)
            {
                signal.Step(distance, Frame);
                model.Step(distance, Edge, 0f, 0f, 0f, false, Frame);
            }

            Assert.Less(model.Farness, 0.01f, "warm within a few seconds");
            Assert.Greater(signal.Clarity, 0.95f, "the radio is clear at a lit mast");
            Assert.AreEqual(1f, model.RadioGain, 0.02f);
        }

        [Test]
        public void ATallerTowerWidensTheClearZoneAroundEveryNode()
        {
            _reach.Light("relay.0");
            var signal = new RadioSignal(_radio);
            Vector3 beyondMast = MastNearby + new Vector3(0f, 0f, 150f);
            float distance = _reach.DistanceToNearestNode(beyondMast);
            signal.Snap(distance);
            float before = signal.Clarity;

            signal.SetTargetRadius(_radio.SignalRadius + 100f);
            for (int i = 0; i < 1200; i++)
            {
                signal.Step(distance, Frame);
            }

            Assert.Greater(signal.Clarity, before + 0.3f, "the upgrade is heard out at the masts too");
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
        public void RadioHop_EasesIntoStaticOverTheOutTime_HoldsInTheDark_ThenResolvesOverTheInTime()
        {
            var hop = new RadioHop();
            Assert.IsFalse(hop.Active);
            hop.Begin();
            float t = 0f;
            float half = -1f;
            while (hop.Step(Frame, _radio.HopOutTime, _radio.HopInTime) < 1f)
            {
                t += Frame;
                if (half < 0f && hop.Amount >= 0.5f)
                {
                    half = t;
                }

                Assert.Less(t, 5f);
            }

            Assert.AreEqual(_radio.HopOutTime, t + Frame, 2f * Frame, "all static as the screen goes dark");
            Assert.AreEqual(_radio.HopOutTime * 0.5f, half, 2f * Frame, "eased, symmetric");

            for (int i = 0; i < 120; i++)
            {
                hop.Step(Frame, _radio.HopOutTime, _radio.HopInTime);
            }

            Assert.AreEqual(1f, hop.Amount, "holds while 07 is moved");
            hop.Finish();
            t = 0f;
            while (hop.Active)
            {
                hop.Step(Frame, _radio.HopOutTime, _radio.HopInTime);
                t += Frame;
                Assert.Less(t, 5f);
            }

            Assert.AreEqual(_radio.HopInTime, t, 2f * Frame, "resolved as the view eases back in");
            Assert.AreEqual(0f, hop.Amount);
        }

        [Test]
        public void RadioHop_FinishedEarly_TurnsBackWithoutAJump()
        {
            var hop = new RadioHop();
            hop.Begin();
            for (int i = 0; i < 20; i++)
            {
                hop.Step(Frame, 1f, 1f);
            }

            float before = hop.Amount;
            hop.Finish();
            hop.Step(Frame, 1f, 1f);
            Assert.Less(Mathf.Abs(hop.Amount - before), 0.05f);
            Assert.Less(hop.Amount, before);
        }

        [Test]
        public void AMastComingBack_CreaksThenItsLampWarms_ThenHomeAnswers()
        {
            var sequence = new RelayRestoreSequence();
            sequence.Begin();
            int lamps = 0;
            int links = 0;
            float lampAt = -1f;
            float linkAt = -1f;
            for (float t = Frame; t < 4f; t += Frame)
            {
                sequence.Step(Frame, 0.9f, 1.9f, out bool lamp, out bool link);
                if (lamp)
                {
                    lamps++;
                    lampAt = t;
                }

                if (link)
                {
                    links++;
                    linkAt = t;
                }
            }

            Assert.AreEqual(1, lamps);
            Assert.AreEqual(1, links);
            Assert.AreEqual(0.9f, lampAt, 2f * Frame);
            Assert.AreEqual(1.9f, linkAt, 2f * Frame);
            Assert.IsFalse(sequence.Running);
        }

        /// <summary>An <see cref="IStationReach"/> over a list of nodes the test lights directly.</summary>
        private sealed class FakeStationReach : IStationReach
        {
            private readonly List<RelayNode> _nodes = new List<RelayNode>();

            public int LitCount
            {
                get
                {
                    int lit = 0;
                    foreach (RelayNode node in _nodes)
                    {
                        lit += node.Lit ? 1 : 0;
                    }

                    return lit;
                }
            }

            public int NodeCount => _nodes.Count;

            public RelayNode GetNode(int index)
            {
                return _nodes[index];
            }

            public bool IsInReach(Vector3 position)
            {
                return DistanceToNearestNode(position) <= 110f;
            }

            public float DistanceToNearestNode(Vector3 position)
            {
                float best = float.PositiveInfinity;
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
