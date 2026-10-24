using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The range ring (opening v3, plan 4.2): the energy out and back, on flat ground, with a slope's cost, and with
    /// an extra charge point; and the ring's showing (only at its two moments — faint at the first charge, full once at
    /// the release — drawing out over ~2 s, holding ~1.5 s, easing out over ~3 s until gone, and never lingering on load).
    /// </summary>
    public sealed class RangeRingTests
    {
        private const float StepSlack = 8f;
        private const int East = 18;
        private const int West = 54;
        private const float BloomThreshold = 1.25f;
        private const float RelayLampSteadyIntensity = 1.02f;

        private sealed class FakeTerrain : ITerrainQuery
        {
            public Func<float, float, float> Height = (x, z) => 0f;

            public Rect PlayableArea => new Rect(-5000f, -5000f, 10000f, 10000f);

            public bool IsDrivable(float x, float z) => true;

            public float SampleHeight(float x, float z) => Height(x, z);

            public Vector3 SampleNormal(float x, float z) => Vector3.up;
        }

        private sealed class FakeRover : IRoverState
        {
            public Vector3 Position { get; set; }

            public Quaternion Rotation => Quaternion.identity;

            public Vector3 Velocity => Vector3.zero;

            public float Speed => 0f;

            public float NormalizedSpeed => 0f;

            public Vector2 DriveInput => Vector2.zero;

            public bool IsGrounded => true;

            public float AirTime => 0f;

            public Vector3 GroundNormal => Vector3.up;
        }

        private sealed class FakePoints : IChargePointSource
        {
            public readonly List<Vector3> Points = new List<Vector3> { Vector3.zero };

            public void CopyChargePoints(List<Vector3> points)
            {
                points.AddRange(Points);
            }
        }

        private BatteryTuning _battery;
        private RangeRingSettings _settings;
        private FakeTerrain _terrain;
        private RangeRingModel _model;
        private Vector3[] _ring;

        [SetUp]
        public void SetUp()
        {
            _battery = ScriptableObject.CreateInstance<BatteryTuning>();
            _settings = _battery.RangeRing;
            _terrain = new FakeTerrain();
            _model = new RangeRingModel(_battery, _settings, _terrain);
            _ring = new Vector3[_settings.Points];
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_battery);
        }

        private float Radius(int index, Vector3 origin)
        {
            Vector3 flat = _ring[index] - origin;
            flat.y = 0f;
            return flat.magnitude;
        }

        [Test]
        public void OnFlatGround_TheRingIsHalfTheRange_OnEveryBearing_ForOneCellOfCharge()
        {
            _model.Compute(Vector3.zero, 1f, new List<Vector3> { Vector3.zero }, _ring);
            float half = _battery.MetresPerCell * 0.5f;
            for (int k = 0; k < _ring.Length; k++)
            {
                Assert.AreEqual(half, Radius(k, Vector3.zero), StepSlack, $"bearing {k}: out and back is one cell");
            }
        }

        [Test]
        public void TheRing_GrowsWithTheCharge_AndIsGoneWithNone()
        {
            var home = new List<Vector3> { Vector3.zero };
            _model.Compute(Vector3.zero, 0.5f, home, _ring);
            float small = Radius(East, Vector3.zero);
            _model.Compute(Vector3.zero, 1.5f, home, _ring);
            float large = Radius(East, Vector3.zero);
            Assert.AreEqual(small * 3f, large, StepSlack * 2f, "three times the charge, three times the ring");
            _model.Compute(Vector3.zero, 0f, home, _ring);
            Assert.AreEqual(0f, Radius(East, Vector3.zero), 1e-3f, "nothing in the battery, no reach");
        }

        [Test]
        public void WithNoChargePointAtAll_07sOwnPlaceIsTheWayHome()
        {
            _model.Compute(Vector3.zero, 1f, new List<Vector3>(), _ring);
            Assert.AreEqual(_battery.MetresPerCell * 0.5f, Radius(East, Vector3.zero), StepSlack);
        }

        [Test]
        public void ASlope_CostsMoreUphill_AndLessDownhill_SoTheWayBackIsNotTheWayOut()
        {
            float rise = Mathf.Tan(10f * Mathf.Deg2Rad);
            _terrain.Height = (x, z) => x * rise;
            Vector3 foot = Vector3.zero;
            Vector3 top = new Vector3(100f, 100f * rise, 0f);
            float up = _model.PathCost(foot, top, _settings.PathStepMetres);
            float down = _model.PathCost(top, foot, _settings.PathStepMetres);
            _terrain.Height = (x, z) => 0f;
            float flat = _model.PathCost(Vector3.zero, new Vector3(100f, 0f, 0f), _settings.PathStepMetres);
            Assert.Greater(up, flat * 1.2f, "uphill is dearer than the flat");
            Assert.Less(down, flat * 0.8f, "downhill is cheaper");
            Assert.AreEqual(flat * 2f, up + down, flat * 0.05f, "and the pair of them comes to the flat's cost both ways");
        }

        [Test]
        public void WithAChargePointUphill_TheRingReachesFurtherPastItThanAwayFromIt()
        {
            float rise = Mathf.Tan(10f * Mathf.Deg2Rad);
            _terrain.Height = (x, z) => x * rise;
            var top = new List<Vector3> { new Vector3(100f, 100f * rise, 0f) };
            _model.Compute(Vector3.zero, 1f, top, _ring);
            float past = Radius(East, Vector3.zero);
            float away = Radius(West, Vector3.zero);
            Assert.Greater(past, away + 50f, "the way home is uphill from the foot: the ring is lopsided");
            for (int k = 0; k < _ring.Length; k++)
            {
                Vector3 edge = _ring[k];
                float way = _model.PathCost(Vector3.zero, edge, _settings.StepMetres) +
                            _model.PathCost(edge, top[0], _settings.PathStepMetres);
                Assert.LessOrEqual(way, 1.15f, $"bearing {k}: the edge can still be come back from");
            }
        }

        [Test]
        public void AnExtraChargePoint_WithinReach_ExtendsTheRing()
        {
            var home = new List<Vector3> { Vector3.zero };
            _model.Compute(Vector3.zero, 1f, home, _ring);
            float before = Radius(East, Vector3.zero);
            var two = new List<Vector3> { Vector3.zero, new Vector3(150f, 0f, 0f) };
            _model.Compute(Vector3.zero, 1f, two, _ring);
            Assert.Greater(Radius(East, Vector3.zero), before + 50f, "toward the new point the ring grows");
            Assert.Greater(Radius(West, Vector3.zero), before - StepSlack, "and never shrinks elsewhere");
        }

        [Test]
        public void AChargePointOutOfReach_ChangesNothing()
        {
            var home = new List<Vector3> { Vector3.zero };
            _model.Compute(Vector3.zero, 1f, home, _ring);
            float before = Radius(East, Vector3.zero);
            var far = new List<Vector3> { Vector3.zero, new Vector3(600f, 0f, 0f) };
            _model.Compute(Vector3.zero, 1f, far, _ring);
            Assert.AreEqual(before, Radius(East, Vector3.zero), StepSlack * 2f, "07 could not get there to use it");
        }

        [Test]
        public void TheRing_ShowsFaintAtTheFirstCharge_FullWhenAsked_AndFadesInAndOut()
        {
            var events = new EventBus();
            var shown = new List<RangeRingShown>();
            events.Subscribe<RangeRingShown>(shown.Add);
            var rover = new FakeRover();
            var model = new BatteryModel(_battery, events);
            var field = new RangeRingField(events, _settings, _model, model, new FakePoints(), rover);
            Assert.IsFalse(field.IsShown);

            events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, true));
            Assert.IsFalse(field.IsShown, "a loaded game's first charge shows no ring");
            events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
            Assert.IsTrue(field.IsShown);
            Assert.AreEqual(RangeRingMode.Faint, field.Mode);
            Assert.AreEqual(1, shown.Count);
            Assert.AreEqual(_settings.FaintSeconds, shown[0].Seconds);
            Assert.AreEqual(0f, field.Visibility01, 1e-3f, "it fades in");
            Assert.AreEqual(_settings.Points, field.Count);

            float peak = 0f;
            float seconds = 0f;
            while (field.IsShown && seconds < 30f)
            {
                field.Step(0.05f);
                seconds += 0.05f;
                peak = Mathf.Max(peak, field.Visibility01);
            }

            Assert.AreEqual(1f, peak, 0.02f, "to its full visibility");
            Assert.AreEqual(_settings.FaintSeconds, seconds, 0.15f, "and is gone after its seconds");
            Assert.AreEqual(0f, field.Visibility01);
            Assert.Less(_settings.FaintStrength, _settings.FullStrength, "faint is fainter than full");

            field.Show(RangeRingMode.Full);
            Assert.AreEqual(0f, field.Reveal01, 1e-3f, "the full ring starts at 07");
            Assert.AreEqual(rover.Position, field.Centre);
            float lastReveal = 0f;
            for (float t = 0f; t < _settings.RevealSeconds + 0.5f; t += 0.05f)
            {
                field.Step(0.05f);
                Assert.GreaterOrEqual(field.Reveal01, lastReveal, "it only draws outward");
                lastReveal = field.Reveal01;
            }

            Assert.AreEqual(1f, lastReveal, 1e-3f, "to its edge, then holds");
            Assert.AreEqual(RangeRingMode.Full, field.Mode);
            Assert.AreEqual(2, shown.Count);
            Assert.AreEqual(_settings.FullSeconds, shown[1].Seconds);
            field.Dispose();
        }

        [Test]
        public void TheRing_IsWarmMoonlitAmber_DrawsTwoHoldsOneAndAHalfEasesThree_ShowsOnlyAtTwoMoments_AndNeverLingersOnLoad()
        {
            Color color = _settings.Color;
            Assert.Greater(color.r, color.g, "warm moonlit-amber: red leads green");
            Assert.Greater(color.g, color.b, "warm moonlit-amber: green leads blue (never cool white)");
            Assert.Greater(color.r - color.b, 0.35f, "distinctly warm amber, not white");
            float maxChannel = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            Assert.Less(maxChannel * _settings.ShaderIntensity, RelayLampSteadyIntensity,
                "below relay.1's steady lamp in brightness");
            Assert.Less(maxChannel * _settings.ShaderIntensity, BloomThreshold, "strictly under bloom (< 1.25)");

            Assert.AreEqual(2f, _settings.RevealSeconds, 1e-3f, "draws out from 07 over ~2 s");
            Assert.AreEqual(1.5f, _settings.HoldSeconds, 1e-3f, "holds ~1.5 s");
            Assert.AreEqual(3f, _settings.FadeOutSeconds, 1e-3f, "eases out over ~3 s");
            Assert.AreEqual(6.5f, _settings.FullSeconds, 1e-3f, "2 s draw + 1.5 s hold + 3 s fade = 6.5 s");

            var events = new EventBus();
            var shown = new List<RangeRingShown>();
            events.Subscribe<RangeRingShown>(shown.Add);
            var rover = new FakeRover();
            var battery = new BatteryModel(_battery, events);
            var field = new RangeRingField(events, _settings, _model, battery, new FakePoints(), rover);

            field.Show(RangeRingMode.Full);
            Assert.IsTrue(field.IsShown);
            field.Step(2f);
            Assert.AreEqual(1f, field.Reveal01, 1e-3f, "finished drawing out at 2 s");
            Assert.AreEqual(1f, field.Visibility01, 1e-3f, "at full visibility during hold");
            field.Step(1.5f);
            Assert.AreEqual(1f, field.Reveal01, 1e-3f, "still at full reach at the end of the 1.5 s hold");
            Assert.AreEqual(1f, field.Visibility01, 1e-3f, "begins its 3 s ease-out at 3.5 s");
            field.Step(1.5f);
            Assert.AreEqual(0.5f, field.Visibility01, 0.05f, "halfway through the 3 s ease-out at 5 s");
            field.Step(1.5f);
            Assert.IsFalse(field.IsShown, "completely gone after the 3 s ease-out");
            Assert.AreEqual(0f, field.Visibility01, 1e-5f);
            Assert.AreEqual(0f, field.Strength, 1e-5f);

            field.Show(RangeRingMode.Full);
            Assert.IsFalse(field.IsShown, "never shows a second full ring (e.g. at relay relight)");
            field.Show(RangeRingMode.Faint);
            Assert.IsFalse(field.IsShown, "never shows after the release moment");
            Assert.AreEqual(1, shown.Count);
            field.Dispose();

            var loadField = new RangeRingField(events, _settings, _model, battery, new FakePoints(), rover);
            events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
            Assert.IsTrue(loadField.IsShown);
            events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, true));
            Assert.IsFalse(loadField.IsShown, "never lingers when a save is restored");
            events.Publish(new OpeningMilestoneReached(OpeningMilestone.CallAnswered, true));
            loadField.Show(RangeRingMode.Full);
            Assert.IsFalse(loadField.IsShown, "a loaded game past the answer does not re-show the release ring");
            loadField.Dispose();
        }
    }
}
