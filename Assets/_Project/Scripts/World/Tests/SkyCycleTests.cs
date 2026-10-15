using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="SunClock"/>, the opening eclipse hold, local terrain-horizon sunlight queries,
    /// save round-tripping and <see cref="SkyPhaseChanged"/> events (0.5.7 SKY-CYCLE).
    /// </summary>
    public sealed class SkyCycleTests
    {
        private const float AngleTolerance = 1e-3f;
        private const float ValueTolerance = 1e-4f;

        private SkyCycleSettings _cycle;
        private SkySettings _sky;
        private MoonSurface _surface;
        private string _tempSaveDir;

        [OneTimeSetUp]
        public void CreateWorld()
        {
            _cycle = new SkyCycleSettings();
            _sky = new SkySettings();
            _surface = new MoonSurface(new SurfaceSettings(), WorldSettings.DefaultSeed);
        }

        [SetUp]
        public void SetUp()
        {
            _tempSaveDir = Path.Combine(Path.GetTempPath(), "MoonProject_SkyCycleTests_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_tempSaveDir) && Directory.Exists(_tempSaveDir))
            {
                Directory.Delete(_tempSaveDir, true);
            }
        }

        [Test]
        public void Bearing_WrapsOncePerDay_AndElevationStaysWithinLimits_WithRoughlyOneThirdEarthlit()
        {
            var clock = new SunClock(_cycle, _sky);
            clock.ReleaseOpeningEclipse();

            float startBearing = clock.Bearing;
            float day = _cycle.DayLengthSeconds;
            int stepsPerDay = 360;
            float dt = day / stepsPerDay;
            int belowHorizonCount = 0;

            float previousUnwrapped = startBearing;
            for (int i = 1; i <= stepsPerDay; i++)
            {
                clock.SetTime(i * dt, false);
                Assert.That(clock.Bearing, Is.GreaterThanOrEqualTo(0f).And.LessThan(360f));
                Assert.That(clock.Elevation, Is.InRange(_cycle.MinElevation - AngleTolerance,
                    _cycle.MaxElevation + AngleTolerance));

                float delta = Mathf.DeltaAngle(previousUnwrapped, clock.Bearing);
                Assert.Greater(delta, 0f, "bearing must advance clockwise throughout the day");
                Assert.AreEqual(360f / stepsPerDay, delta, 1e-2f, "bearing advances at a uniform rate");
                previousUnwrapped = clock.Bearing;

                if (clock.Elevation < 0f)
                {
                    belowHorizonCount++;
                }
            }

            clock.SetTime(day, false);
            Assert.AreEqual(0f, Mathf.Abs(Mathf.DeltaAngle(startBearing, clock.Bearing)), AngleTolerance,
                "bearing wraps back to startBearing after exactly one lunar day");

            float earthlitShare = (float)belowHorizonCount / stepsPerDay;
            Assert.AreEqual(1f / 3f, earthlitShare, 0.04f,
                "the Sun stays below the horizon for roughly one third of the lunar day");

            // Check elevation limits across four full lunar days.
            for (float t = 0f; t <= 4f * day; t += 30f)
            {
                clock.SetTime(t, false);
                Assert.That(clock.Elevation, Is.InRange(_cycle.MinElevation - AngleTolerance,
                    _cycle.MaxElevation + AngleTolerance));
            }
        }

        [Test]
        public void EclipseFactor_IsOneAtHold_ZeroAwayFromEarth_AndZeroAcrossDaysTwoToFour()
        {
            var clock = new SunClock(_cycle, _sky);
            Assert.IsTrue(clock.IsHoldingEclipse);
            Assert.AreEqual(1f, clock.EclipseFactor, ValueTolerance, "Sun is completely eclipsed at the hold point");

            clock.ReleaseOpeningEclipse();
            clock.Tick(5f, out _);
            Assert.That(clock.EclipseFactor, Is.GreaterThan(0.5f).And.LessThan(1f),
                "5 s after release the Sun's limb is breaking out from Earth's edge (diamond-ring bead)");

            clock.SetTime(20f, false);
            Assert.AreEqual(0f, clock.EclipseFactor, ValueTolerance,
                "by 20 s after release the Sun's disc has cleared Earth's limb");

            float day = _cycle.DayLengthSeconds;
            clock.SetTime(day * 0.25f, false);
            Assert.AreEqual(0f, clock.EclipseFactor, ValueTolerance, "0 quarter-day away from Earth");
            clock.SetTime(day * 0.5f, false);
            Assert.AreEqual(0f, clock.EclipseFactor, ValueTolerance, "0 opposite Earth");

            // Sample finely (every 5 s) across Days 2, 3 and 4: t in [1 * day, 4 * day].
            for (float t = day; t <= 4f * day; t += 5f)
            {
                clock.SetTime(t, false);
                Assert.AreEqual(0f, clock.EclipseFactor, ValueTolerance,
                    $"Days 2-4 must not eclipse again (t = {t:F0} s, day {t / day + 1f:F2}, " +
                    $"bearing {clock.Bearing:F1}, elevation {clock.Elevation:F1})");
            }
        }

        [Test]
        public void EarthPhase_IsNewBehindEarth_AndFullOppositeEarth()
        {
            var clock = new SunClock(_cycle, _sky);
            float day = _cycle.DayLengthSeconds;
            float startBearing = _sky.EarthBearing + _cycle.StartBearingOffset;

            // Time when Bearing == EarthBearing (338 degrees): Sun is behind Earth -> Earth is "new" (0).
            float toEarthBearing = (360f - _cycle.StartBearingOffset) / 360f * day;
            clock.SetTime(toEarthBearing, false);
            Assert.AreEqual(0f, Mathf.Abs(Mathf.DeltaAngle(_sky.EarthBearing, clock.Bearing)), AngleTolerance);
            Assert.AreEqual(0f, clock.EarthPhase, ValueTolerance, "Earth is new (0) when the Sun is at Earth's bearing");

            // Time when Bearing == EarthBearing + 180 degrees: Sun is opposite Earth -> Earth is "full" (1).
            float toOppositeBearing = ((180f - _cycle.StartBearingOffset + 360f) % 360f) / 360f * day;
            clock.SetTime(toOppositeBearing, false);
            Assert.AreEqual(0f, Mathf.Abs(Mathf.DeltaAngle(_sky.EarthBearing + 180f, clock.Bearing)), AngleTolerance);
            Assert.AreEqual(1f, clock.EarthPhase, ValueTolerance, "Earth is full (1) when the Sun is opposite Earth");

            // Quadrature (90 degrees from Earth): Earth is half-lit (0.5).
            float toQuarterBearing = ((90f - _cycle.StartBearingOffset + 360f) % 360f) / 360f * day;
            clock.SetTime(toQuarterBearing, false);
            Assert.AreEqual(0.5f, clock.EarthPhase, 1e-3f, "Earth is half-lit (0.5) at 90 degrees elongation");
        }

        [Test]
        public void EarthLitDirection_FollowsSunBearingFromNewCrescentHalfToFull()
        {
            Vector3 toEarth = _sky.EarthDirection;
            Vector3 frontNormal = -toEarth;
            Vector3 rightLimb = Vector3.Cross(Vector3.up, toEarth).normalized;
            float earthBearing = _sky.EarthBearing;
            float earthElevation = _sky.EarthElevation;

            // 1. New Earth (Sun directly behind Earth): lit direction points away from the Moon (dot frontNormal == -1).
            Vector3 newDir = SkyShaderGlobals.EarthLitDirection(_sky, earthBearing, earthElevation);
            Assert.AreEqual(-1f, Vector3.Dot(frontNormal, newDir), ValueTolerance,
                "new Earth has lit direction opposite the Moon-facing normal");
            Assert.AreEqual(0f, Vector3.Dot(rightLimb, newDir), ValueTolerance);

            // 2. Thin waxing crescent (EarthPhase ~ 0.02, Sun ~16.3 deg clockwise of Earth): lit on the right limb,
            // while the centre of Earth's disc stays in shadow (dot frontNormal < -0.95).
            Vector3 crescentDir = SkyShaderGlobals.EarthLitDirection(_sky, earthBearing + 16.3f, earthElevation);
            Assert.Greater(Vector3.Dot(rightLimb, crescentDir), 0.25f,
                "thin waxing crescent lights Earth's right (Sunward) limb");
            Assert.Less(Vector3.Dot(frontNormal, crescentDir), -0.95f,
                "thin waxing crescent leaves Earth's disc centre on the dark night side");

            // 3. First quarter (Sun 90 deg clockwise of Earth, EarthPhase == 0.5): lit direction is +rightLimb.
            Vector3 halfRightDir = SkyShaderGlobals.EarthLitDirection(_sky, earthBearing + 90f, 0f);
            Assert.AreEqual(1f, Vector3.Dot(rightLimb, halfRightDir), ValueTolerance,
                "at +90 deg elongation the right half of Earth's disc is lit");
            Assert.AreEqual(0f, Vector3.Dot(frontNormal, halfRightDir), ValueTolerance,
                "at +90 deg elongation the terminator bisects Earth's disc centre");

            // 4. Third quarter (Sun 90 deg counter-clockwise of Earth, EarthPhase == 0.5): lit direction is -rightLimb.
            Vector3 halfLeftDir = SkyShaderGlobals.EarthLitDirection(_sky, earthBearing - 90f, 0f);
            Assert.AreEqual(-1f, Vector3.Dot(rightLimb, halfLeftDir), ValueTolerance,
                "at -90 deg elongation the left half of Earth's disc is lit");
            Assert.AreEqual(0f, Vector3.Dot(frontNormal, halfLeftDir), ValueTolerance);

            // 5. Full Earth (Sun 180 deg opposite Earth, EarthPhase == 1.0): lit direction points straight at the Moon
            // (dot frontNormal == +1).
            Vector3 fullDir = SkyShaderGlobals.EarthLitDirection(_sky, earthBearing + 180f, -4.5f);
            Assert.AreEqual(1f, Vector3.Dot(frontNormal, fullDir), ValueTolerance,
                "at 180 deg elongation Earth's Moon-facing hemisphere is fully lit");
            Assert.AreEqual(0f, Vector3.Dot(rightLimb, fullDir), ValueTolerance);
        }

        [Test]
        public void OpeningHold_HoldsUntilReleased_AndReleasesAutomaticallyAtMaxHold()
        {
            var clock = new SunClock(_cycle, _sky);
            Assert.IsTrue(clock.IsHoldingEclipse);
            Assert.AreEqual(0f, clock.TimeSeconds, ValueTolerance);

            clock.Tick(300f, out SkyPhaseChanged changed);
            Assert.IsTrue(clock.IsHoldingEclipse, "still holding before ReleaseOpeningEclipse or MaxHoldSeconds");
            Assert.AreEqual(0f, clock.TimeSeconds, ValueTolerance);
            Assert.AreEqual(1f, clock.EclipseFactor, ValueTolerance);
            Assert.AreEqual(SkyPhase.Eclipse, changed.Phase);

            clock.ReleaseOpeningEclipse();
            Assert.IsFalse(clock.IsHoldingEclipse);
            clock.Tick(25f, out _);
            Assert.AreEqual(25f, clock.TimeSeconds, ValueTolerance);
            Assert.AreEqual(0f, clock.EclipseFactor, ValueTolerance);

            // Second clock: verify automatic release after MaxHoldSeconds (default 25 min = 1500 s).
            var autoClock = new SunClock(_cycle, _sky);
            autoClock.Tick(_cycle.MaxHoldSeconds - 10f, out _);
            Assert.IsTrue(autoClock.IsHoldingEclipse, "holds just before MaxHoldSeconds");
            Assert.AreEqual(0f, autoClock.TimeSeconds, ValueTolerance);

            autoClock.Tick(25f, out _);
            Assert.IsFalse(autoClock.IsHoldingEclipse, "releases itself at MaxHoldSeconds");
            Assert.AreEqual(15f, autoClock.TimeSeconds, ValueTolerance,
                "excess time past MaxHoldSeconds advances the clock");
        }

        [Test]
        public void SunlightAt_IsZeroInEclipse_ZeroBehindARidge_AndOneOnOpenHighGround()
        {
            var clock = new SunClock(_cycle, _sky);
            Vector3 peak = _surface.PeakSummit;
            Vector3 home = Vector3.zero;

            // 1. In eclipse: 0 everywhere, even on the highest summit.
            Assert.AreEqual(0f, clock.SunlightAt(_surface, peak), ValueTolerance, "0 on PeakSummit during eclipse");
            Assert.AreEqual(0f, clock.SunlightAt(_surface, home), ValueTolerance, "0 at home during eclipse");

            // 2. In full sunlit: 1 on open high ground and open basin floor.
            clock.SetTime(_cycle.FallbackSunlitTimeSeconds, false);
            Assert.AreEqual(0f, clock.EclipseFactor, ValueTolerance);
            Assert.Greater(clock.Elevation, 14f);
            Assert.AreEqual(1f, clock.SunlightAt(_surface, peak), ValueTolerance, "1 on open high ground");
            Assert.AreEqual(1f, clock.SunlightAt(_surface, home), ValueTolerance, "1 on open pad when Sun clears rim");

            // 3. Behind a ridge: 0 when a tall ridge sits along the Sun's bearing in front of the sample point.
            Vector2 sunDir2D = MoonSurface.BearingToDirection(clock.Bearing);
            var ridgeTerrain = new RidgeTerrain(sunDir2D, 100f, 60f);
            Assert.AreEqual(0f, clock.SunlightAt(ridgeTerrain, Vector3.zero), ValueTolerance,
                "0 behind a 60 m ridge at 100 m distance (~31 deg horizon > Sun elevation)");
            Assert.AreEqual(1f, clock.SunlightAt(ridgeTerrain, new Vector3(sunDir2D.x * 100f, 60f, sunDir2D.y * 100f)),
                ValueTolerance, "1 on top of the ridge itself");

            // Also verify on the real MoonSurface just inside the steep inner crater rim wall toward the Sun.
            Vector3 rimFoot = new Vector3(sunDir2D.x * 330f, _surface.SampleHeight(sunDir2D.x * 330f, sunDir2D.y * 330f),
                sunDir2D.y * 330f);
            Assert.AreEqual(0f, clock.SunlightAt(_surface, rimFoot), ValueTolerance,
                "0 at the steep inner foot of the crater rim wall facing into the rim");
        }

        [Test]
        public void Save_RoundTripsClockAndHold_AndStartsSunlitWhenSaveLacksSkySection()
        {
            // 1. Round-trip an active clock after eclipse release.
            var save1 = new SaveService(_tempSaveDir, "slot-active");
            var clock1 = new SunClock(_cycle, _sky);
            using (save1.Register(clock1.CreateSaveSection()))
            {
                save1.Load();
                clock1.ResolveLoadedSave(save1);
                Assert.IsTrue(clock1.IsHoldingEclipse, "new game starts in opening eclipse hold");

                clock1.SetTime(1425.5f, false);
                Assert.IsTrue(save1.SaveNow());
            }

            var save1Reloaded = new SaveService(_tempSaveDir, "slot-active");
            var restoredClock = new SunClock(_cycle, _sky);
            using (save1Reloaded.Register(restoredClock.CreateSaveSection()))
            {
                save1Reloaded.Load();
                restoredClock.ResolveLoadedSave(save1Reloaded);
                Assert.IsFalse(restoredClock.IsHoldingEclipse);
                Assert.AreEqual(1425.5f, restoredClock.TimeSeconds, ValueTolerance);
                Assert.AreEqual(clock1.Bearing, restoredClock.Bearing, AngleTolerance);
                Assert.AreEqual(clock1.Elevation, restoredClock.Elevation, AngleTolerance);
            }

            // 2. Round-trip a save made while still in the opening eclipse hold.
            var saveHeld = new SaveService(_tempSaveDir, "slot-held");
            var heldClock = new SunClock(_cycle, _sky);
            using (saveHeld.Register(heldClock.CreateSaveSection()))
            {
                saveHeld.Load();
                heldClock.ResolveLoadedSave(saveHeld);
                Assert.IsTrue(saveHeld.SaveNow());
            }

            var saveHeldReloaded = new SaveService(_tempSaveDir, "slot-held");
            var restoredHeld = new SunClock(_cycle, _sky);
            using (saveHeldReloaded.Register(restoredHeld.CreateSaveSection()))
            {
                saveHeldReloaded.Load();
                restoredHeld.ResolveLoadedSave(saveHeldReloaded);
                Assert.IsTrue(restoredHeld.IsHoldingEclipse, "save made during the hold restores the hold");
                Assert.AreEqual(1f, restoredHeld.EclipseFactor, ValueTolerance);
            }

            // 3. Loading a save that has no world.sky section starts after the eclipse at the tuned sunlit time.
            var legacySave = new SaveService(_tempSaveDir, "slot-legacy");
            using (legacySave.Register(new SaveSection<DummySaveData>("other.section", 1,
                () => new DummySaveData { value = 42 }, _ => { })))
            {
                legacySave.Load();
                Assert.IsTrue(legacySave.SaveNow());
            }

            var legacyReloaded = new SaveService(_tempSaveDir, "slot-legacy");
            var fallbackClock = new SunClock(_cycle, _sky);
            using (legacyReloaded.Register(fallbackClock.CreateSaveSection()))
            {
                legacyReloaded.Load();
                fallbackClock.ResolveLoadedSave(legacyReloaded);
                Assert.IsFalse(fallbackClock.IsHoldingEclipse, "legacy save without sky section skips the hold");
                Assert.AreEqual(_cycle.FallbackSunlitTimeSeconds, fallbackClock.TimeSeconds, ValueTolerance);
                Assert.AreEqual(0f, fallbackClock.EclipseFactor, ValueTolerance);
                Assert.AreEqual(SkyPhase.Sunlit, fallbackClock.Phase);
            }
        }

        [Test]
        public void PhaseEvents_FireOncePerPhaseTransition()
        {
            var events = new EventBus();
            var received = new List<SkyPhaseChanged>();
            using (events.Subscribe<SkyPhaseChanged>(received.Add))
            {
                var clock = new SunClock(_cycle, _sky);
                Assert.AreEqual(SkyPhase.Eclipse, clock.Phase);

                // Ticking while held in Eclipse must not publish any change event.
                clock.Tick(10f, events);
                clock.Tick(10f, events);
                Assert.IsEmpty(received, "no event while phase remains Eclipse");

                clock.ReleaseOpeningEclipse();
                float day = _cycle.DayLengthSeconds;
                for (float t = 0f; t < day * 1.12f; t += 2f)
                {
                    clock.Tick(2f, events);
                }

                // Over one full Day 1 from the opening eclipse:
                // Eclipse -> Sunrise (post-eclipse dawn) -> Sunlit -> Sunset -> Earthlit -> Sunrise -> Sunlit.
                SkyPhase[] expected =
                {
                    SkyPhase.Sunrise,
                    SkyPhase.Sunlit,
                    SkyPhase.Sunset,
                    SkyPhase.Earthlit,
                    SkyPhase.Sunrise,
                    SkyPhase.Sunlit,
                };
                Assert.AreEqual(expected.Length, received.Count,
                    "each phase transition fires once: " + string.Join(" -> ", received));
                for (int i = 0; i < expected.Length; i++)
                {
                    Assert.AreEqual(expected[i], received[i].Phase, $"transition {i}");
                    if (i > 0)
                    {
                        Assert.AreNotEqual(received[i - 1].Phase, received[i].Phase,
                            "consecutive events must never repeat the same phase");
                    }
                }
            }
        }

        [Serializable]
        private sealed class DummySaveData
        {
            public int value;
        }

        private sealed class RidgeTerrain : ITerrainQuery
        {
            private readonly Vector2 _axis;
            private readonly float _ridgeDistance;
            private readonly float _ridgeHeight;

            public RidgeTerrain(Vector2 axis, float ridgeDistance, float ridgeHeight)
            {
                _axis = axis.normalized;
                _ridgeDistance = ridgeDistance;
                _ridgeHeight = ridgeHeight;
            }

            public Rect PlayableArea => new Rect(-500f, -500f, 1000f, 1000f);

            public bool IsDrivable(float x, float z)
            {
                return true;
            }

            public float SampleHeight(float x, float z)
            {
                float along = x * _axis.x + z * _axis.y;
                float dist = Mathf.Abs(along - _ridgeDistance);
                return dist < 35f ? _ridgeHeight * (1f - dist / 35f) : 0f;
            }

            public Vector3 SampleNormal(float x, float z)
            {
                return Vector3.up;
            }
        }
    }
}
