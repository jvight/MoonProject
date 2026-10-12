using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's body answering a beat (docs/design/feel-pass.md §4, R8: weight shows on the body, not the camera), as
    /// springs on the chassis: surge (+ forward, m), heave (+ up, m) and pitch (+ nose down, deg). A kick throws one
    /// out so it peaks at the settings' travel and springs back with its frequency and damping; a hold leans it to a
    /// steady pitch until released. Plain logic, reused for the cut now and drive, leap and land next.
    /// </summary>
    public sealed class ImpactNudge
    {
        /// <summary>A critically damped spring kicked with v0 peaks at v0 / (omega e); kicks use the inverse.</summary>
        private const float PeakFactor = 2.71828f;

        private DampedSpring _surge;
        private DampedSpring _heave;
        private DampedSpring _pitch;
        private NudgeSettings _surgeSpring;
        private NudgeSettings _heaveSpring;
        private NudgeSettings _pitchSpring;
        private float _heldPitch;

        public float Surge => _surge.Value;

        public float Heave => _heave.Value;

        public float Pitch => _pitch.Value;

        /// <summary>Whether everything is back at rest (no hold, springs settled).</summary>
        public bool IsAtRest(float tolerance)
        {
            return _heldPitch == 0f && Mathf.Abs(_surge.Value) < tolerance && Mathf.Abs(_heave.Value) < tolerance
                   && Mathf.Abs(_pitch.Value) < tolerance && Mathf.Abs(_surge.Velocity) < tolerance
                   && Mathf.Abs(_heave.Velocity) < tolerance && Mathf.Abs(_pitch.Velocity) < tolerance;
        }

        /// <summary>Throws the chassis back (recoil) by the settings' distance.</summary>
        public void KickSurge(NudgeSettings settings, float sign = -1f)
        {
            _surgeSpring = settings;
            _surge.AddVelocity(sign * Velocity(settings.Distance, settings));
        }

        /// <summary>Drops the chassis (a dip) by the settings' distance.</summary>
        public void KickHeave(NudgeSettings settings, float sign = -1f)
        {
            _heaveSpring = settings;
            _heave.AddVelocity(sign * Velocity(settings.Distance, settings));
        }

        /// <summary>Tips the chassis by the settings' angle (+ nose down, − the rear dipping).</summary>
        public void KickPitch(NudgeSettings settings, float sign = 1f)
        {
            _pitchSpring = settings;
            _pitch.AddVelocity(sign * Velocity(settings.Angle, settings));
        }

        /// <summary>Leans to a steady pitch (+ forward) with the settings' spring until <see cref="Release"/>.</summary>
        public void Hold(NudgeSettings settings)
        {
            _pitchSpring = settings;
            _heldPitch = settings.Angle;
        }

        /// <summary>Lets any hold go; everything eases back.</summary>
        public void Release()
        {
            _heldPitch = 0f;
        }

        /// <summary>Back to rest at once (07 was placed).</summary>
        public void Reset()
        {
            _heldPitch = 0f;
            _surge.Reset(0f);
            _heave.Reset(0f);
            _pitch.Reset(0f);
        }

        /// <param name="pitchJitter">Extra pitch (deg) this step, e.g. a sustained beam's micro-motions.</param>
        public void Step(float deltaTime, float pitchJitter = 0f)
        {
            Step(ref _surge, 0f, _surgeSpring, deltaTime);
            Step(ref _heave, 0f, _heaveSpring, deltaTime);
            Step(ref _pitch, _heldPitch + pitchJitter, _pitchSpring, deltaTime);
        }

        private static void Step(ref DampedSpring spring, float target, NudgeSettings settings, float deltaTime)
        {
            if (settings == null)
            {
                return;
            }

            spring.Step(target, settings.Frequency, settings.Damping, deltaTime);
        }

        private static float Velocity(float peak, NudgeSettings settings)
        {
            return peak * 2f * Mathf.PI * settings.Frequency * PeakFactor;
        }
    }
}
