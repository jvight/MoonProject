using System;
using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// The first lunar day follows the opening's progress, never a timer (opening v2 §9.2): the eclipse holds until
    /// the first charge on a built pad; the Sun then clears Earth and holds at golden hour until the base is powered;
    /// after power-on it sinks to last light, low over the rim, and holds there until a battery bank is ready for the
    /// night; then the first sunset runs, and after nightfall the day cycle runs at its normal pace. A milestone
    /// replayed from a save snaps the clock to where that milestone leaves the Sun, with no transition. Plain logic:
    /// it only decides how far the <see cref="SunClock"/> may advance each frame.
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
        }

        /// <summary>Clock time when the Sun's limb first breaks out from behind Earth after the opening eclipse.</summary>
        public float BreakoutTime { get; }

        /// <summary>Clock time where the Sun holds at golden hour, after it clears Earth, until power-on.</summary>
        public float GoldenHourTime { get; }

        /// <summary>Clock time of last light on the first day, where the Sun holds until the night is ready.</summary>
        public float LastLightTime { get; }

        /// <summary>Clock time when the first day's Sun has set: the normal pace takes over from here.</summary>
        public float NightfallTime { get; }

        public bool FirstCharge { get; private set; }

        public bool PowerOn { get; private set; }

        public bool NightReady { get; private set; }

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
            FirstCharge = true;
            PowerOn |= milestone >= OpeningMilestone.PowerOn;
            NightReady |= milestone >= OpeningMilestone.NightReady;
            clock.ReleaseOpeningEclipse();
            if (!restored)
            {
                // The limb breaks out a breath after the first charge, however the clock stood in the hold.
                float lead = BreakoutTime - _settings.EclipseBreakoutLead;
                if (releasing && clock.TimeSeconds < lead)
                {
                    clock.SetTime(lead);
                }

                return;
            }

            float settled = NightReady ? NightfallTime : PowerOn ? LastLightTime : GoldenHourTime;
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

        private float Hold()
        {
            if (NightReady)
            {
                return float.PositiveInfinity;
            }

            return PowerOn ? LastLightTime : GoldenHourTime;
        }

        private float Rate(float time)
        {
            if (time >= GoldenHourTime && time < LastLightTime)
            {
                return (LastLightTime - GoldenHourTime) / _settings.LastLightSinkSeconds;
            }

            if (time >= LastLightTime && time < NightfallTime)
            {
                return (NightfallTime - LastLightTime) / _settings.FirstSunsetSeconds;
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
