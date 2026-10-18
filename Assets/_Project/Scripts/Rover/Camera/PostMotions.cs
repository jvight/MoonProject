using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// What 07 does in each check of its power-on self-test, as pure functions of the check's progress (0..1) so each
    /// motion's shape is testable: the head's sweep and nod, each wheel's twitch, the motor's shudder and lurch, the
    /// cutter's failed ignitions with the turret's kicks, the wing's jerk-stall-fold, the antenna's rise, the cells'
    /// fill and the start (head to the beacon, the dust shiver). The same tables say when each <c>BootCheckPulse</c>
    /// fires, so the sounds and the motion cannot drift apart.
    /// </summary>
    public sealed class PostMotions
    {
        /// <summary>A pulse's moment in its check, as a share of the check (0..1), and its index.</summary>
        public readonly struct PulseMark
        {
            public PulseMark(float share, int index)
            {
                Share = share;
                Index = index;
            }

            public float Share { get; }

            public int Index { get; }
        }

        // Head: the sweep takes the first part, the nod the rest.
        private const float SweepShare = 0.62f;
        private const float NodStart = 0.66f;

        // Wheels: each twitches in its own window.
        private const float WheelFirst = 0.05f;
        private const float WheelStride = 0.2f;
        private const float WheelWindow = 0.22f;

        // Motor.
        private const float ShudderEnd = 0.7f;
        private const float LurchStart = 0.45f;
        private const float LurchEnd = 0.9f;

        // Cutter: three attempts, each weaker, then it dies.
        private const float AttemptWindow = 0.18f;
        private const int AttemptCount = 3;
        private const int BeaconPulseOffset = 3;

        // Wing: jerk, stall, fold.
        private const float JerkEnd = 0.2f;
        private const float FoldStart = 0.6f;
        private const float StallTremble = 0.03f;
        private const float StallWobbles = 9f;

        // Antenna: rises in the first part, then holds.
        private const float AntennaRiseShare = 0.6f;

        // Begin: the head turns, then the dust shivers off, then the headlamps are on.
        private const float TurnEnd = 0.5f;
        private const float ShiverEnd = 0.8f;
        private const float ShiverWobbles = 7f;

        private static readonly float[] AttemptStarts = { 0.1f, 0.4f, 0.7f };
        private static readonly float[] AttemptPeaks = { 1f, 0.8f, 0.55f };

        private readonly PostSettings _settings;

        public PostMotions(PostSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The check's fixed pulses in the order they fire, for the sequencer (the lamps' come from the lamps).</summary>
        public static int PulseCount(BootCheckStep step)
        {
            switch (step)
            {
                case BootCheckStep.Head: return 2;
                case BootCheckStep.Wheels: return 4;
                case BootCheckStep.Motor: return 2;
                case BootCheckStep.Cutter: return 2 * AttemptCount;
                case BootCheckStep.SolarWing: return 3;
                case BootCheckStep.Antenna: return 1;
                case BootCheckStep.Battery: return 0;
                case BootCheckStep.Begin: return 3;
                default: return 0;
            }
        }

        /// <summary>The <paramref name="n"/>th fixed pulse of <paramref name="step"/>, sorted by moment.</summary>
        public static PulseMark Pulse(BootCheckStep step, int n)
        {
            switch (step)
            {
                case BootCheckStep.Head:
                    return n == 0 ? new PulseMark(0f, 0) : new PulseMark(NodStart, 1);
                case BootCheckStep.Wheels:
                    return new PulseMark(WheelFirst + WheelStride * n, n);
                case BootCheckStep.Motor:
                    return n == 0 ? new PulseMark(0f, 0) : new PulseMark(LurchStart, 1);
                case BootCheckStep.Cutter:
                    // A spark and the beacon's blink at each attempt: (0.1: 0, 3), (0.4: 1, 4), (0.7: 2, 5).
                    int attempt = n / 2;
                    return new PulseMark(AttemptStarts[attempt], n % 2 == 0 ? attempt : BeaconPulseOffset + attempt);
                case BootCheckStep.SolarWing:
                    return n == 0 ? new PulseMark(0f, 0) : n == 1 ? new PulseMark(JerkEnd, 1) : new PulseMark(FoldStart, 2);
                case BootCheckStep.Antenna:
                    return new PulseMark(0f, 0);
                case BootCheckStep.Begin:
                    return n == 0 ? new PulseMark(0f, 0) : n == 1 ? new PulseMark(TurnEnd, 1) : new PulseMark(ShiverEnd, 2);
                default:
                    return default;
            }
        }

        /// <summary>
        /// The continuous pulse of a check: its index and where in the check it starts (0 for the whole check), or
        /// false for a check without one. The lamps' is the ramp (index 3), which starts when the lamps' own ramp does.
        /// </summary>
        public static bool TryGetContinuous(BootCheckStep step, out int index, out float startShare)
        {
            switch (step)
            {
                case BootCheckStep.Battery:
                    index = 0;
                    startShare = 0f;
                    return true;
                case BootCheckStep.Antenna:
                    index = 1;
                    startShare = 0f;
                    return true;
                default:
                    index = 0;
                    startShare = 0f;
                    return false;
            }
        }

        /// <summary>The eye's focus pulse (0..1) at progress <paramref name="p"/> of the lens check: it hunts near, then far, then settles.</summary>
        public static float LensFocus(float p)
        {
            float near = Mathf.Sin(Mathf.PI * Mathf.Clamp01(p / 0.5f));
            float far = 0.6f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((p - 0.4f) / 0.5f));
            return Mathf.Max(near, far);
        }

        /// <summary>The neck's yaw and pitch offsets (deg) in the head check: a sweep to both sides, then a nod.</summary>
        public Vector2 Head(float p)
        {
            float yaw = 0f;
            float pitch = 0f;
            if (p < SweepShare)
            {
                float u = p / SweepShare;
                yaw = _settings.HeadSweepDegrees * Mathf.Sin(2f * Mathf.PI * u) * Mathf.Sin(Mathf.PI * u);
            }
            else if (p > NodStart)
            {
                float u = Mathf.Clamp01((p - NodStart) / (1f - NodStart));
                pitch = -_settings.HeadNodDegrees * Mathf.Sin(Mathf.PI * u);
            }

            return new Vector2(yaw, pitch);
        }

        /// <summary>Wheel <paramref name="wheel"/>'s twitch (deg) at progress <paramref name="p"/> of the wheels check.</summary>
        public float Wheel(int wheel, float p)
        {
            float u = (p - (WheelFirst + WheelStride * wheel)) / WheelWindow;
            if (u <= 0f || u >= 1f)
            {
                return 0f;
            }

            float sign = wheel % 2 == 0 ? 1f : -1f;
            return sign * _settings.WheelTwitchDegrees * Mathf.Sin(2f * Mathf.PI * u) * Mathf.Sin(Mathf.PI * u);
        }

        /// <summary>The motor's vertical shudder (m) at progress <paramref name="p"/>: fast, building then settling.</summary>
        public float Shudder(float p)
        {
            if (p >= ShudderEnd)
            {
                return 0f;
            }

            float u = p / ShudderEnd;
            return _settings.ShudderMetres * Mathf.Sin(Mathf.PI * u) * Mathf.Sin(2f * Mathf.PI * 11f * u);
        }

        /// <summary>The motor's creep (m forward) at progress <paramref name="p"/>: a lurch out and back.</summary>
        public float Lurch(float p)
        {
            if (p <= LurchStart || p >= LurchEnd)
            {
                return 0f;
            }

            float u = (p - LurchStart) / (LurchEnd - LurchStart);
            return _settings.LurchMetres * Mathf.Sin(Mathf.PI * u);
        }

        /// <summary>The nozzle's flash (0..1) at progress <paramref name="p"/> of the cutter check: three failed ignitions.</summary>
        public float CutterFlash(float p)
        {
            float flash = 0f;
            for (int i = 0; i < AttemptCount; i++)
            {
                float u = (p - AttemptStarts[i]) / AttemptWindow;
                if (u > 0f && u < 1f)
                {
                    float s = Mathf.Sin(Mathf.PI * u);
                    flash = Mathf.Max(flash, AttemptPeaks[i] * s * s);
                }
            }

            return flash;
        }

        /// <summary>The turret's sideways kick (deg) at progress <paramref name="p"/>: it jerks with each attempt.</summary>
        public float Turret(float p)
        {
            float kick = 0f;
            for (int i = 0; i < AttemptCount; i++)
            {
                float u = (p - AttemptStarts[i]) / AttemptWindow;
                if (u > 0f && u < 1f)
                {
                    float sign = i % 2 == 0 ? 1f : -1f;
                    kick += sign * _settings.TurretKickDegrees * AttemptPeaks[i] * Mathf.Sin(Mathf.PI * u);
                }
            }

            return kick;
        }

        /// <summary>How open (0..1) the solar wing is at progress <paramref name="p"/>: it jerks open, stalls, folds back.</summary>
        public float Wing(float p)
        {
            float open = _settings.WingJerkOpen;
            if (p < JerkEnd)
            {
                return open * Smoothing.SmoothStep(0f, JerkEnd, p);
            }

            if (p < FoldStart)
            {
                float u = (p - JerkEnd) / (FoldStart - JerkEnd);
                return open + StallTremble * Mathf.Sin(2f * Mathf.PI * StallWobbles * u) * Mathf.Sin(Mathf.PI * u);
            }

            return open * (1f - Smoothing.SmoothStep(FoldStart, 1f, p));
        }

        /// <summary>The antenna's rise (m) at progress <paramref name="p"/>: up, then held.</summary>
        public float Antenna(float p)
        {
            return _settings.AntennaRiseMetres * Smoothing.SmoothStep(0f, AntennaRiseShare, p);
        }

        /// <summary>The cell lamps' fill (0..1 of full) at progress <paramref name="p"/>: up to the battery's share.</summary>
        public float Battery(float p)
        {
            return _settings.BatteryFill * Smoothing.SmoothStep(0f, 1f, p);
        }

        /// <summary>How far (0..1) the head has turned toward the beacon at progress <paramref name="p"/> of the start.</summary>
        public static float BeaconTurn(float p)
        {
            return Smoothing.SmoothStep(0f, TurnEnd, p);
        }

        /// <summary>How far (0..1) the shiver has shaken the dust off by progress <paramref name="p"/> of the start.</summary>
        public static float ShiverProgress(float p)
        {
            return Smoothing.SmoothStep(TurnEnd, ShiverEnd, p);
        }

        /// <summary>The dust shiver's strength (0..1) at progress <paramref name="p"/> of the start.</summary>
        public static float Shiver(float p)
        {
            if (p <= TurnEnd || p >= ShiverEnd)
            {
                return 0f;
            }

            float u = (p - TurnEnd) / (ShiverEnd - TurnEnd);
            return Mathf.Sin(Mathf.PI * u) * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * ShiverWobbles * u));
        }
    }
}
