using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// A slow camera moment framing 07 together with a subject (a surfacing relic, the base after an upgrade).
    /// <list type="bullet">
    /// <item>Idle -> EasingIn on <see cref="Start"/>: <see cref="Weight"/> eases from wherever it is up to 1, so
    /// starting a new moment during another never jumps.</item>
    /// <item>EasingIn -> Holding -> EasingOut -> Idle on its own timeline. A held moment stays in Holding until
    /// <see cref="Release"/> (e.g. while the excavation beam is on).</item>
    /// <item><see cref="Cancel"/> (any look input) eases out from the current weight over the cancel time.</item>
    /// </list>
    /// The camera rig blends its framing by <see cref="Weight"/>; at 0 it is exactly the normal chase camera.
    /// </summary>
    public sealed class CameraMoment
    {
        private const float Epsilon = 1e-4f;

        private enum Phase
        {
            Idle,
            EasingIn,
            Holding,
            EasingOut,
        }

        private Phase _phase;
        private float _elapsed;
        private float _from;
        private float _outDuration;
        private bool _held;

        public float Weight { get; private set; }

        public bool IsActive => _phase != Phase.Idle;

        public Vector3 Subject { get; private set; }

        public CameraMomentSettings Settings { get; private set; }

        /// <param name="holdUntilReleased">Keep the framing until <see cref="Release"/>, then the timed hold.</param>
        public void Start(CameraMomentSettings settings, Vector3 subject, bool holdUntilReleased)
        {
            Settings = settings;
            Subject = subject;
            _held = holdUntilReleased;
            _from = Weight;
            _elapsed = 0f;
            _phase = Phase.EasingIn;
        }

        /// <summary>Lets a held moment go: it eases out after its hold. Does nothing to timed moments.</summary>
        public void Release()
        {
            if (!_held)
            {
                return;
            }

            _held = false;
            if (_phase == Phase.EasingIn)
            {
                BeginEaseOut(Settings.EaseOut);
            }
            else if (_phase == Phase.Holding)
            {
                _elapsed = 0f;
            }
        }

        /// <summary>Ends the moment at once, no ease (07 was placed somewhere else while the view was dark).</summary>
        public void Clear()
        {
            _held = false;
            Weight = 0f;
            _phase = Phase.Idle;
        }

        /// <summary>Ends the moment early, easing out from the current weight over the given seconds.</summary>
        public void Cancel(float easeOut)
        {
            _held = false;
            if (_phase == Phase.Idle || (_phase == Phase.EasingOut && _outDuration <= easeOut))
            {
                return;
            }

            BeginEaseOut(easeOut);
        }

        public void Step(float deltaTime)
        {
            if (_phase == Phase.Idle || deltaTime <= 0f)
            {
                return;
            }

            _elapsed += deltaTime;
            switch (_phase)
            {
                case Phase.EasingIn:
                    float t = _elapsed / Settings.EaseIn;
                    Weight = Mathf.Lerp(_from, 1f, Smoothing.SmoothStep(0f, 1f, t));
                    if (t >= 1f)
                    {
                        _phase = Phase.Holding;
                        _elapsed = 0f;
                    }

                    break;

                case Phase.Holding:
                    Weight = 1f;
                    if (!_held && _elapsed >= Settings.Hold)
                    {
                        BeginEaseOut(Settings.EaseOut);
                    }

                    break;

                case Phase.EasingOut:
                    float u = _elapsed / _outDuration;
                    Weight = _from * (1f - Smoothing.SmoothStep(0f, 1f, u));
                    if (u >= 1f || Weight < Epsilon)
                    {
                        Weight = 0f;
                        _phase = Phase.Idle;
                    }

                    break;
            }
        }

        private void BeginEaseOut(float duration)
        {
            _from = Weight;
            _elapsed = 0f;
            _outDuration = Mathf.Max(duration, Epsilon);
            _phase = Phase.EasingOut;
        }

        /// <summary>
        /// How strongly the camera may turn toward a subject <paramref name="distance"/> metres from 07: fully when
        /// near, fading to nothing at the settings' focus limit.
        /// </summary>
        public static float FocusReach(CameraMomentSettings settings, float distance)
        {
            float limit = settings.MaxFocusDistance;
            return 1f - Smoothing.SmoothStep(0.75f * limit, limit, distance);
        }

        /// <summary>
        /// Where the camera looks: from 07's follow point toward the subject by the settings' share (scaled by
        /// <paramref name="amount"/>), never farther than the settings' maximum shift.
        /// </summary>
        public static Vector3 LookPoint(CameraMomentSettings settings, Vector3 follow, Vector3 subject, float amount)
        {
            Vector3 shift = (subject - follow) * (settings.LookShare * Mathf.Clamp01(amount));
            return follow + Vector3.ClampMagnitude(shift, settings.MaxLookShift);
        }

        /// <summary>
        /// Camera target yaw (deg) for 07 at <paramref name="roverYaw"/> looking toward a subject at bearing
        /// <paramref name="subjectYaw"/>: the swing is a share of the way, at least the settings' minimum (toward the
        /// subject's side, so it is not hidden behind 07) and at most their maximum.
        /// </summary>
        public static float BlendYaw(CameraMomentSettings settings, float roverYaw, float subjectYaw, float amount)
        {
            float delta = Mathf.DeltaAngle(roverYaw, subjectYaw);
            float swing = delta * settings.YawShare;
            if (Mathf.Abs(swing) < settings.MinYawSwing)
            {
                swing = delta < 0f ? -settings.MinYawSwing : settings.MinYawSwing;
            }

            swing = Mathf.Clamp(swing, -settings.MaxYawSwing, settings.MaxYawSwing);
            return roverYaw + swing * Mathf.Clamp01(amount);
        }
    }
}
