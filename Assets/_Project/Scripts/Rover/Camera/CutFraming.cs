using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The beam's work framing, aiming at the middle of the beam from 07's shoulder on the side the beam runs to so
    /// 07's head never hides it. The cut's charge-up push-in (docs/design/0.5.5-owner-feedback.md §5.6) eases a little
    /// closer and lower only for a fresh charge on a big piece; the dig's framing (feel-pass §3.1) eases over the
    /// shoulder and a little higher. Plain logic: whether the cut earns it, which side, where the eye goes, and how
    /// far the framing has eased in.
    /// </summary>
    public sealed class CutFraming
    {
        private float _progress;
        private float _target;
        private float _seconds = 1f;

        /// <summary>How far the camera is into the framing, 0 (chase) .. 1, eased at both ends.</summary>
        public float Weight => Smoothing.SmoothStep(0f, 1f, _progress);

        /// <summary>True from <see cref="Begin"/> until <see cref="End"/>.</summary>
        public bool IsActive => _target > 0f;

        /// <summary>+1 when the camera sits to 07's right, -1 to its left.</summary>
        public float Side { get; private set; } = 1f;

        /// <summary>The cut point to look at the beam toward.</summary>
        public Vector3 CutPoint { get; private set; }

        /// <summary>
        /// Whether a charge earns the push-in: a fresh one (a warm emitter repeats too often to move the camera) on a
        /// piece needing at least <paramref name="minStages"/> stages, and never with the Gentle camera.
        /// </summary>
        public static bool Wanted(bool warm, int stagesNeeded, int minStages, bool gentle)
        {
            return !warm && !gentle && stagesNeeded >= minStages;
        }

        /// <summary>
        /// The side the beam runs to as seen from behind 07 (+1 right, -1 left; straight ahead counts as right): the
        /// camera sits there, so the line from 07's emitter to the cut point clears 07's head.
        /// </summary>
        public static float ChooseSide(Vector3 rover, Vector3 forward, Vector3 cutPoint)
        {
            Vector3 right = Vector3.Cross(Vector3.up, new Vector3(forward.x, 0f, forward.z));
            return Vector3.Dot(cutPoint - rover, right) >= 0f ? 1f : -1f;
        }

        /// <summary>
        /// Where the eye goes: behind the emitter along the beam's direction by <paramref name="back"/>, out to
        /// <see cref="Side"/> by <paramref name="offset"/> and up by <paramref name="rise"/> (all metres).
        /// </summary>
        public Vector3 Eye(Vector3 emitter, float back, float offset, float rise)
        {
            Vector3 along = CutPoint - emitter;
            along.y = 0f;
            along = along.sqrMagnitude > 1e-6f ? along.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, along);
            return emitter - along * back + right * (Side * offset) + Vector3.up * rise;
        }

        /// <summary>Starts easing in over <paramref name="seconds"/> toward framing the beam to <paramref name="cutPoint"/>.</summary>
        public void Begin(Vector3 cutPoint, float side, float seconds)
        {
            CutPoint = cutPoint;
            Side = side;
            _target = 1f;
            _seconds = Mathf.Max(seconds, 1e-3f);
        }

        /// <summary>
        /// The orbit pushed in by <paramref name="weight"/>: the radius <paramref name="pushIn"/> (a share) shorter and
        /// the camera <paramref name="lower"/> metres lower, at the same follow point. Elevations in degrees.
        /// </summary>
        public static void PushIn(float weight, float pushIn, float lower, ref float elevation, ref float radius)
        {
            float height = radius * Mathf.Sin(elevation * Mathf.Deg2Rad) - lower * weight;
            radius *= 1f - pushIn * weight;
            elevation = Mathf.Asin(Mathf.Clamp(height / Mathf.Max(radius, 1e-3f), -1f, 1f)) * Mathf.Rad2Deg;
        }

        /// <summary>Eases back to the chase camera over <paramref name="seconds"/> (the fold, a release, the player's hands).</summary>
        public void End(float seconds)
        {
            _target = 0f;
            _seconds = Mathf.Max(seconds, 1e-3f);
        }

        /// <summary>Back to the chase camera at once (07 was placed).</summary>
        public void Reset()
        {
            _target = 0f;
            _progress = 0f;
        }

        public void Step(float deltaTime)
        {
            _progress = Mathf.MoveTowards(_progress, _target, Mathf.Max(deltaTime, 0f) / _seconds);
        }
    }
}
