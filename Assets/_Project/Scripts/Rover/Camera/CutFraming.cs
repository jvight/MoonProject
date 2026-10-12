using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The cut's work framing (docs/design/feel-pass.md §3.1, Cam): while 07 stands still cutting, the camera eases
    /// over its shoulder, on the side the beam runs to so 07's head never hides it, a little higher, aiming at the
    /// middle of the beam. Plain logic: which side, where the eye goes, and how far the framing has eased in.
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
