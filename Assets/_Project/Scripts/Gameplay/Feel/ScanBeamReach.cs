using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How far the scan beam reaches and which way it points while 07's head turns onto the target (owner playtest
    /// f3d1ff265: the beam shot off the object). The beam leaves the lens along the lens's own axis; as the axis comes
    /// within a few degrees of the target the beam bends onto it and grows out to it, so it never jumps to the object
    /// before the head has aimed and its start never leaves the lens.
    /// </summary>
    public sealed class ScanBeamReach
    {
        private const float MinDistance = 1e-4f;
        private const float MinHalfLife = 1e-3f;
        private const float Settled = 0.97f;

        private float _aligned;

        /// <summary>0 while the lens axis is far from the target, 1 once it has settled on it.</summary>
        public float Aligned => _aligned;

        /// <summary>The beam starts closed at the lens.</summary>
        public void Reset()
        {
            _aligned = 0f;
        }

        /// <summary>
        /// The beam's far end this frame. <paramref name="alignedDegrees"/> is the lens-axis error up to which the beam
        /// is fully on the target, <paramref name="lostDegrees"/> the error from which it is fully off it (a short
        /// beam of <paramref name="unalignedReach"/> of the distance along the axis); <paramref name="halfLife"/> eases
        /// the change.
        /// </summary>
        public Vector3 End(float deltaTime, Vector3 lens, Vector3 axis, Vector3 contact, float alignedDegrees,
            float lostDegrees, float unalignedReach, float halfLife)
        {
            Vector3 toContact = contact - lens;
            float distance = toContact.magnitude;
            if (distance < MinDistance || axis.sqrMagnitude < MinDistance)
            {
                return contact;
            }

            Vector3 direction = toContact / distance;
            float error = Vector3.Angle(axis, direction);
            float target = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(alignedDegrees, lostDegrees, error));
            _aligned += (target - _aligned) * (1f - Mathf.Pow(0.5f, deltaTime / Mathf.Max(halfLife, MinHalfLife)));
            if (target >= 1f && _aligned >= Settled)
            {
                _aligned = 1f;
                return contact;
            }

            Vector3 along = Vector3.Slerp(axis.normalized, direction, _aligned);
            return lens + along * (distance * Mathf.Lerp(unalignedReach, 1f, _aligned));
        }
    }
}
