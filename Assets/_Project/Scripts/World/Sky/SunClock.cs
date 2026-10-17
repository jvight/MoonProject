using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.World
{
    /// <summary>
    /// Pure C# lunar day clock, eclipse geometry and local terrain-horizon sunlight query (0.5.7 SKY-CYCLE, decision
    /// L1).
    /// <para>
    /// <b>Sun path model:</b> The landing site sits near the lunar pole, so the Sun is always low and circles the
    /// horizon once per lunar day (<see cref="SkyCycleSettings.DayLengthSeconds"/>, default 7200 s = 2 hours of play):
    /// <c>Bearing(t) = Normalize360(startBearing + 360 * (t / DayLengthSeconds))</c>, where <c>startBearing =
    /// EarthBearing + StartBearingOffset</c> (338 + 6.20 = 344.20 degrees). The Sun's elevation follows a tilted
    /// sinusoidal path between <see cref="SkyCycleSettings.MinElevation"/> (-4.5 degrees) and
    /// <see cref="SkyCycleSettings.MaxElevation"/> (+17.3 degrees):
    /// <c>Elevation(t) = mid + amp * cos(Bearing(t) - PeakBearing(t))</c>, spending roughly one third of each circle
    /// below 0 degrees (the earthlit night). The peak-elevation bearing starts <see cref="SkyCycleSettings.InitialPeakLead"/>
    /// (18 degrees) ahead of <c>startBearing</c> on Day 1 and precesses by <see cref="SkyCycleSettings.NodalDriftPerDay"/>
    /// (64 degrees) per lunar day. On Day 1 at <c>t = 0</c> the Sun sits at (344.20 degrees, 16.77 degrees), just
    /// inside Earth's right limb (Earth centre 338 degrees, 16 degrees, radius 6.5 degrees; Sun radius 0.40 degrees;
    /// centre separation 6.00 degrees &lt;= 6.10 degrees), so <see cref="EclipseFactor"/> is 1. On release the Sun's
    /// limb breaks out within ~5 s (diamond-ring bead) and clears Earth by ~20 s; because the nodal peak drifts 64
    /// degrees per day, on Days 2-4 the Sun passes underneath Earth's lower limb (elevation &lt;= 8.1 degrees at
    /// bearing 338 degrees, separation &gt;= 7.9 degrees &gt; 6.90 degrees) with <see cref="EclipseFactor"/> = 0.
    /// </para>
    /// </summary>
    public sealed class SunClock
    {
        private const float FullCircleDegrees = 360f;
        private const float HalfCircleDegrees = 180f;

        private readonly SkyCycleSettings _settings;
        private readonly SkySettings _sky;
        private SkyPhase _lastPublishedPhase;
        private bool _restoredFromSave;

        public SunClock(SkyCycleSettings settings, SkySettings sky)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _sky = sky ?? throw new ArgumentNullException(nameof(sky));
            TimeSeconds = 0f;
            IsHoldingEclipse = true;
            Recompute();
            _lastPublishedPhase = Phase;
        }

        /// <summary>Seconds elapsed on the lunar day clock since the opening eclipse hold ended.</summary>
        public float TimeSeconds { get; private set; }

        /// <summary>
        /// True while the opening eclipse hold is active on a fresh game: it holds until the opening releases it (the
        /// first charge, <see cref="OpeningSkyPacing"/>), never by itself.
        /// </summary>
        public bool IsHoldingEclipse { get; private set; }

        /// <summary>Current horizontal bearing of the Sun, 0..360 degrees clockwise from +Z.</summary>
        public float Bearing { get; private set; }

        /// <summary>Current elevation of the Sun above the geometric horizon, degrees.</summary>
        public float Elevation { get; private set; }

        /// <summary>Unit vector from the world toward the Sun's centre.</summary>
        public Vector3 SunDirection { get; private set; }

        /// <summary>
        /// Illuminated fraction of Earth's disc as seen from the Moon, 0..1: 0 ("new") when the Sun is at Earth's
        /// bearing, 1 ("full") when the Sun is opposite Earth's bearing.
        /// </summary>
        public float EarthPhase { get; private set; }

        /// <summary>
        /// Fraction of the Sun's disc hidden behind Earth's disc, 0..1, computed from the exact circle-circle overlap
        /// of the two discs' angular radii in the sky.
        /// </summary>
        public float EclipseFactor { get; private set; }

        /// <summary>
        /// Global basin-wide sunlight factor, 0..1: depends only on the Sun's elevation above the geometric horizon
        /// and <see cref="EclipseFactor"/>, never on a single point's local terrain shadow. Drives the sky and
        /// directional key light so distant sunlit mountains and the basin never go dark when 07 enters a local shadow.
        /// </summary>
        public float GlobalSunlight { get; private set; }

        /// <summary>Current phase of the lunar sky cycle.</summary>
        public SkyPhase Phase { get; private set; }

        /// <summary>
        /// Releases the opening eclipse hold so the clock advances from the hold point and the Sun's limb breaks out
        /// from behind Earth's edge. Idempotent when already released.
        /// </summary>
        public void ReleaseOpeningEclipse()
        {
            if (!IsHoldingEclipse)
            {
                return;
            }

            IsHoldingEclipse = false;
            Recompute();
        }

        /// <summary>
        /// Advances the clock by <paramref name="deltaTime"/> seconds and returns true when <see cref="Phase"/>
        /// changed, populating <paramref name="phaseChanged"/> with the new phase and current eclipse factor.
        /// </summary>
        public bool Tick(float deltaTime, out SkyPhaseChanged phaseChanged)
        {
            if (deltaTime > 0f && !IsHoldingEclipse)
            {
                TimeSeconds += deltaTime;
                Recompute();
            }

            if (Phase != _lastPublishedPhase)
            {
                _lastPublishedPhase = Phase;
                phaseChanged = new SkyPhaseChanged(Phase, EclipseFactor);
                return true;
            }

            phaseChanged = default;
            return false;
        }

        /// <summary>
        /// Advances the clock by <paramref name="deltaTime"/> seconds and publishes <see cref="SkyPhaseChanged"/> to
        /// <paramref name="events"/> if the phase changed.
        /// </summary>
        public void Tick(float deltaTime, EventBus events)
        {
            if (Tick(deltaTime, out SkyPhaseChanged changed) && events != null)
            {
                events.Publish(changed);
            }
        }

        /// <summary>
        /// Sets the clock to an explicit time and hold state (for save restore, capture sessions and tests) and
        /// synchronises the tracked phase so no spurious transition fires for the jump itself.
        /// </summary>
        public void SetTime(float timeSeconds, bool holdingEclipse = false)
        {
            TimeSeconds = Mathf.Max(0f, timeSeconds);
            IsHoldingEclipse = holdingEclipse;
            Recompute();
            _lastPublishedPhase = Phase;
        }

        /// <summary>
        /// Sets the clock to the tuned sunlit start time after the opening eclipse, used when loading an older save
        /// that has no <see cref="WorldSaveKeys.Sky"/> section.
        /// </summary>
        public void StartAtSunlitFallback()
        {
            SetTime(_settings.FallbackSunlitTimeSeconds, false);
        }

        /// <summary>Creates the versioned <see cref="ISaveSection"/> for <see cref="WorldSaveKeys.Sky"/>.</summary>
        public ISaveSection CreateSaveSection()
        {
            return new SaveSection<SkySaveData>(WorldSaveKeys.Sky, WorldSaveKeys.SkyVersion, CaptureSave, RestoreSave);
        }

        /// <summary>Captures the clock's serialisable save state.</summary>
        public SkySaveData CaptureSave()
        {
            return new SkySaveData
            {
                timeSeconds = TimeSeconds,
                holdingEclipse = IsHoldingEclipse,
            };
        }

        /// <summary>Restores the clock from saved data.</summary>
        public void RestoreSave(SkySaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _restoredFromSave = true;
            SetTime(data.timeSeconds, data.holdingEclipse);
        }

        /// <summary>
        /// Called after <see cref="ISaveService"/> has loaded: if a save file was loaded (or recovered from backup)
        /// but held no <see cref="WorldSaveKeys.Sky"/> section, jumps past the opening eclipse to the tuned sunlit
        /// fallback time. On a fresh game (<see cref="SaveLoadResult.NoSave"/>), keeps the opening eclipse hold.
        /// </summary>
        public void ResolveLoadedSave(ISaveService save)
        {
            if (save == null || !save.IsLoaded || _restoredFromSave)
            {
                return;
            }

            if (save.LoadResult == SaveLoadResult.Loaded || save.LoadResult == SaveLoadResult.RecoveredFromBackup)
            {
                StartAtSunlitFallback();
            }
        }

        /// <summary>
        /// Direct sunlight reaching <paramref name="position"/> right now, 0..1: 0 when eclipsed or when the Sun is
        /// below the local terrain horizon along the Sun's horizontal bearing, with a soft ramp of width
        /// <see cref="SkyCycleSettings.HorizonSoftness"/> degrees across the horizon edge. Allocation-free.
        /// </summary>
        public float SunlightAt(ITerrainQuery terrain, Vector3 position)
        {
            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            if (IsHoldingEclipse || EclipseFactor >= 1f)
            {
                return 0f;
            }

            float softness = Mathf.Max(_settings.HorizonSoftness, 1e-4f);
            float halfSoftness = softness * 0.5f;
            if (Elevation <= -halfSoftness)
            {
                return 0f;
            }

            float groundY = terrain.SampleHeight(position.x, position.z);
            float originY = Mathf.Max(position.y, groundY) + _settings.ReceiverHeight;
            Vector2 dir = MoonSurface.BearingToDirection(Bearing);
            float step = Mathf.Max(_settings.HorizonStep, 1f);
            float maxDistance = _settings.HorizonDistance;
            float maxSlope = 0f;
            for (float distance = step; distance <= maxDistance; distance += step)
            {
                float sampleHeight = terrain.SampleHeight(position.x + dir.x * distance, position.z + dir.y * distance);
                float slope = (sampleHeight - originY) / distance;
                if (slope > maxSlope)
                {
                    maxSlope = slope;
                }
            }

            float horizonElevation = Mathf.Atan(maxSlope) * Mathf.Rad2Deg;
            float t = Mathf.Clamp01((Elevation - horizonElevation + halfSoftness) / softness);
            float horizonFactor = t * t * (3f - 2f * t);
            return horizonFactor * (1f - EclipseFactor);
        }

        /// <summary>The Sun's elevation, degrees, at clock time <paramref name="timeSeconds"/> (the clock is unchanged).</summary>
        public float ElevationAt(float timeSeconds)
        {
            SunAt(timeSeconds, out _, out float elevation, out _);
            return elevation;
        }

        /// <summary>
        /// The share of the Sun's disc hidden behind Earth at clock time <paramref name="timeSeconds"/>, 0..1 (the clock
        /// is unchanged; the opening hold is ignored).
        /// </summary>
        public float EclipseFactorAt(float timeSeconds)
        {
            SunAt(timeSeconds, out float bearing, out float elevation, out _);
            return ComputeEclipseFactor(Direction(bearing, elevation), _sky.EarthDirection,
                _settings.SunDiameter * 0.5f * Mathf.Deg2Rad, _sky.EarthDiameter * 0.5f * Mathf.Deg2Rad);
        }

        private static Vector3 Direction(float bearing, float elevation)
        {
            float elevationRadians = elevation * Mathf.Deg2Rad;
            Vector2 horizontal = MoonSurface.BearingToDirection(bearing) * Mathf.Cos(elevationRadians);
            return new Vector3(horizontal.x, Mathf.Sin(elevationRadians), horizontal.y).normalized;
        }

        private void SunAt(float timeSeconds, out float bearing, out float elevation, out float phaseRadians)
        {
            float dayLength = Mathf.Max(_settings.DayLengthSeconds, 1f);
            float days = timeSeconds / dayLength;
            float startBearing = NormalizeBearing(_sky.EarthBearing + _settings.StartBearingOffset);
            bearing = NormalizeBearing(startBearing + days * FullCircleDegrees);

            float peakBearing = startBearing + _settings.InitialPeakLead + days * _settings.NodalDriftPerDay;
            phaseRadians = (bearing - peakBearing) * Mathf.Deg2Rad;
            float midElevation = (_settings.MaxElevation + _settings.MinElevation) * 0.5f;
            float ampElevation = (_settings.MaxElevation - _settings.MinElevation) * 0.5f;
            elevation = Mathf.Clamp(midElevation + ampElevation * Mathf.Cos(phaseRadians),
                _settings.MinElevation, _settings.MaxElevation);
        }

        private void Recompute()
        {
            SunAt(TimeSeconds, out float bearing, out float elevation, out float phaseRadians);
            Bearing = bearing;
            Elevation = elevation;

            SunDirection = Direction(Bearing, Elevation);

            float elongationRadians = (Bearing - _sky.EarthBearing) * Mathf.Deg2Rad;
            EarthPhase = Mathf.Clamp01(0.5f * (1f - Mathf.Cos(elongationRadians)));

            EclipseFactor = ComputeEclipseFactor(SunDirection, _sky.EarthDirection,
                _settings.SunDiameter * 0.5f * Mathf.Deg2Rad,
                _sky.EarthDiameter * 0.5f * Mathf.Deg2Rad);

            if (IsHoldingEclipse || EclipseFactor >= 1f)
            {
                GlobalSunlight = 0f;
            }
            else
            {
                float softness = Mathf.Max(_settings.HorizonSoftness, 1e-4f);
                float halfSoftness = softness * 0.5f;
                float t = Mathf.Clamp01((Elevation + halfSoftness) / softness);
                float horizonFactor = t * t * (3f - 2f * t);
                GlobalSunlight = horizonFactor * (1f - EclipseFactor);
            }

            bool isRising = Mathf.Sin(phaseRadians) < 0f;
            Phase = DeterminePhase(isRising);
        }

        private SkyPhase DeterminePhase(bool isRising)
        {
            if (IsHoldingEclipse || EclipseFactor > 0f)
            {
                return SkyPhase.Eclipse;
            }

            if (Elevation < 0f)
            {
                return SkyPhase.Earthlit;
            }

            if (Elevation < _settings.TwilightElevation)
            {
                return isRising ? SkyPhase.Sunrise : SkyPhase.Sunset;
            }

            if (TimeSeconds < _settings.PostEclipseSunriseSeconds)
            {
                return SkyPhase.Sunrise;
            }

            return SkyPhase.Sunlit;
        }

        private static float ComputeEclipseFactor(Vector3 sunDirection, Vector3 earthDirection, float sunRadiusRad,
            float earthRadiusRad)
        {
            float separation = Mathf.Acos(Mathf.Clamp(Vector3.Dot(sunDirection, earthDirection), -1f, 1f));
            if (sunRadiusRad <= 0f)
            {
                return separation <= earthRadiusRad ? 1f : 0f;
            }

            if (separation >= earthRadiusRad + sunRadiusRad)
            {
                return 0f;
            }

            if (earthRadiusRad >= sunRadiusRad && separation <= earthRadiusRad - sunRadiusRad)
            {
                return 1f;
            }

            if (sunRadiusRad > earthRadiusRad && separation <= sunRadiusRad - earthRadiusRad)
            {
                return Mathf.Clamp01((earthRadiusRad * earthRadiusRad) / (sunRadiusRad * sunRadiusRad));
            }

            float d2 = separation * separation;
            float rS2 = sunRadiusRad * sunRadiusRad;
            float rE2 = earthRadiusRad * earthRadiusRad;
            float cosAlpha = Mathf.Clamp((d2 + rS2 - rE2) / (2f * separation * sunRadiusRad), -1f, 1f);
            float cosBeta = Mathf.Clamp((d2 + rE2 - rS2) / (2f * separation * earthRadiusRad), -1f, 1f);
            float p = (-separation + sunRadiusRad + earthRadiusRad)
                * (separation + sunRadiusRad - earthRadiusRad)
                * (separation - sunRadiusRad + earthRadiusRad)
                * (separation + sunRadiusRad + earthRadiusRad);
            float overlapArea = rS2 * Mathf.Acos(cosAlpha) + rE2 * Mathf.Acos(cosBeta)
                - 0.5f * Mathf.Sqrt(Mathf.Max(0f, p));
            return Mathf.Clamp01(overlapArea / (Mathf.PI * rS2));
        }

        private static float NormalizeBearing(float degrees)
        {
            float wrapped = degrees % FullCircleDegrees;
            return wrapped < 0f ? wrapped + FullCircleDegrees : wrapped;
        }
    }
}
