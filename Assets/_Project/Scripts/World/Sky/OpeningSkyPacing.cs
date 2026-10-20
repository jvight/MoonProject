using System;
using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// The first lunar day follows the opening's progress, never a timer (opening v2 §9.2, opening v3 R5–R6): the
    /// eclipse holds until the first charge on a built pad; the Sun then clears Earth and holds at golden hour, the
    /// afternoon, through power-on and the first free choice, until the first dusk begins
    /// (<see cref="OpeningDuskBegan"/>: 07 docked after visiting a near site, or learned the battery bank); it then
    /// sinks to last light, low over the rim, and holds there until a battery bank is ready for the night. The moment
    /// the night is ready the first sunset runs, a watched beat rather than a wait: from wherever the sink has brought
    /// the Sun to nightfall in one tuned length. After nightfall the day cycle runs at its normal pace. A milestone or a
    /// dusk replayed from a save snaps the clock to where it leaves the Sun, with no transition. Plain logic: it only
    /// decides how far the <see cref="SunClock"/> may advance each frame.
    /// </summary>
    public sealed class OpeningSkyPacing
    {
        // The search step for the first day's key times, clock seconds: fine enough that a hold lands within a
        // second of its elevation.
        private const float SearchStep = 1f;

        // The breakout happens within a minute of the opening's hold point (SunClock's Day 1 geometry); searching an
        // hour leaves room for retuned sky settings.
        private const float SearchLimit = 3600f;

        private readonly SkyCycleSettings _settings;

        // Clock time the first sunset starts from: where the Sun stood when the night was ready (until then, last
        // light).
        private float _sunsetFrom;

        public OpeningSkyPacing(SkyCycleSettings settings, SunClock clock)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            BreakoutTime = FirstBreakout(clock);
            GoldenHourTime = Mathf.Max(settings.GoldenHourSeconds, BreakoutTime);
            LastLightTime = FirstTimeAtOrBelow(clock, settings.LastLightElevation, GoldenHourTime);
            NightfallTime = FirstTimeAtOrBelow(clock, settings.NightfallElevation, LastLightTime);
            _sunsetFrom = LastLightTime;
        }

        /// <summary>Clock time when the Sun's limb first breaks out from behind Earth after the opening eclipse.</summary>
        public float BreakoutTime { get; }

        /// <summary>Clock time where the Sun holds at golden hour, after it clears Earth, until the first dusk.</summary>
        public float GoldenHourTime { get; }

        /// <summary>Clock time of last light on the first day, where the Sun holds until the night is ready.</summary>
        public float LastLightTime { get; }

        /// <summary>Clock time when the first day's Sun has set: the normal pace takes over from here.</summary>
        public float NightfallTime { get; }

        public bool FirstCharge { get; private set; }

        public bool PowerOn { get; private set; }

        public bool NightReady { get; private set; }

        /// <summary>The first dusk has begun: the Sun may sink from golden hour to last light.</summary>
        public bool DuskBegun { get; private set; }

        /// <summary>
        /// The opening reached <paramref name="milestone"/> (a later milestone implies the earlier ones). The first
        /// charge releases the eclipse; a milestone replayed from a save (<paramref name="restored"/>) moves the clock
        /// straight to where it leaves the Sun.
        /// </summary>
        public void Reach(OpeningMilestone milestone, bool restored, SunClock clock)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            bool releasing = clock.IsHoldingEclipse;
            bool nightWasReady = NightReady;
            FirstCharge = true;
            PowerOn |= milestone >= OpeningMilestone.PowerOn;
            NightReady |= milestone >= OpeningMilestone.NightReady;
            clock.ReleaseOpeningEclipse();
            if (!restored)
            {
                if (NightReady && !nightWasReady && clock.TimeSeconds < LastLightTime)
                {
                    // The night is ready while the Sun is still sinking: the sunset starts here, not at last light.
                    _sunsetFrom = Mathf.Max(clock.TimeSeconds, GoldenHourTime);
                }

                // The limb breaks out a breath after the first charge, however the clock stood in the hold.
                float lead = BreakoutTime - _settings.EclipseBreakoutLead;
                if (releasing && clock.TimeSeconds < lead)
                {
                    clock.SetTime(lead);
                }

                return;
            }

            Settle(clock);
        }

        /// <summary>
        /// The first dusk began (after power-on): the Sun leaves its golden-hour hold and sinks to last light in its
        /// tuned length; a dusk replayed from a save (<paramref name="restored"/>) moves the clock straight there.
        /// </summary>
        public void BeginDusk(bool restored, SunClock clock)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            FirstCharge = true;
            PowerOn = true;
            DuskBegun = true;
            clock.ReleaseOpeningEclipse();
            if (restored)
            {
                Settle(clock);
            }
        }

        // A loaded game: the clock moves straight to where the opening's progress leaves the Sun, never back.
        private void Settle(SunClock clock)
        {
            float settled = NightReady ? NightfallTime : DuskBegun ? LastLightTime : GoldenHourTime;
            if (clock.TimeSeconds < settled)
            {
                clock.SetTime(settled);
            }
        }

        /// <summary>
        /// How many clock seconds to advance from <paramref name="time"/> over <paramref name="realDelta"/> real
        /// seconds: the transitions between holds run at tuned lengths, and the clock never passes the current hold.
        /// </summary>
        public float ClockDelta(float time, float realDelta)
        {
            if (realDelta <= 0f)
            {
                return 0f;
            }

            float delta = realDelta * Rate(time);
            float hold = Hold();
            return Mathf.Min(delta, Mathf.Max(0f, hold - time));
        }

        /// <summary>
        /// Real seconds the clock takes from <paramref name="from"/> to <paramref name="to"/> at the current pace:
        /// each stretch of the first day at its tuned rate, <see cref="float.PositiveInfinity"/> when a hold stops it
        /// short of <paramref name="to"/>, 0 when <paramref name="to"/> is not ahead.
        /// </summary>
        public float RealSecondsBetween(float from, float to)
        {
            if (to <= from)
            {
                return 0f;
            }

            if (Hold() < to)
            {
                return float.PositiveInfinity;
            }

            float seconds = 0f;
            float time = from;
            while (time < to)
            {
                float next = Mathf.Min(to, NextRateChange(time));
                seconds += (next - time) / Rate(time);
                time = next;
            }

            return seconds;
        }

        // The next clock time after `time` where the first day's pace changes (infinity once it runs normally).
        private float NextRateChange(float time)
        {
            if (time < GoldenHourTime)
            {
                return GoldenHourTime;
            }

            float sunset = NightReady ? _sunsetFrom : LastLightTime;
            if (time < sunset)
            {
                return sunset;
            }

            return time < NightfallTime ? NightfallTime : float.PositiveInfinity;
        }

        private float Hold()
        {
            if (NightReady)
            {
                return float.PositiveInfinity;
            }

            return DuskBegun ? LastLightTime : GoldenHourTime;
        }

        private float Rate(float time)
        {
            if (NightReady && time >= _sunsetFrom && time < NightfallTime)
            {
                return (NightfallTime - _sunsetFrom) / _settings.FirstSunsetSeconds;
            }

            if (time >= GoldenHourTime && time < LastLightTime)
            {
                return (LastLightTime - GoldenHourTime) / _settings.LastLightSinkSeconds;
            }

            return 1f;
        }

        // The first clock time when the Sun's limb clears Earth's edge (its disc no longer wholly hidden).
        private static float FirstBreakout(SunClock clock)
        {
            for (float t = 0f; t <= SearchLimit; t += SearchStep)
            {
                if (clock.EclipseFactorAt(t) < 1f)
                {
                    return t;
                }
            }

            throw new InvalidOperationException("The Sun never breaks out from behind Earth; check the sky cycle.");
        }

        // The first clock time at or after `from` when the Sun is at or below `elevation` degrees, searched in steps
        // over one lunar day (the first day's descent always reaches the night's elevations).
        private float FirstTimeAtOrBelow(SunClock clock, float elevation, float from)
        {
            float end = from + _settings.DayLengthSeconds;
            for (float t = from; t <= end; t += SearchStep)
            {
                if (clock.ElevationAt(t) <= elevation)
                {
                    return t;
                }
            }

            throw new InvalidOperationException(
                $"The Sun never reaches {elevation} degrees within a lunar day after {from} s; check the sky cycle.");
        }
    }
}
