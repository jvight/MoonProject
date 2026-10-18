using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The camera's way from the title's frame into 07's closed lens, as a pure function: it sweeps round 07 at no
    /// less than a clear radius, then closes in on the lens from the front. A straight line from a frame behind 07
    /// to its face runs through the hull, so the picture sweeps over bright metal for a moment (seen over a save).
    /// </summary>
    public static class PushInPath
    {
        /// <summary>Share of the push (0..1) after which the camera closes in from the clear radius to the lens.</summary>
        private const float CloseInFrom = 0.7f;

        /// <summary>The camera's position at eased progress <paramref name="t"/> (0 at <paramref name="from"/>, 1 at <paramref name="to"/>).</summary>
        /// <param name="center">07's position: the push sweeps round it.</param>
        /// <param name="clearRadius">The ground radius (m) the camera keeps from 07 until it closes in on the lens.</param>
        public static Vector3 Position(Vector3 center, Vector3 from, Vector3 to, float t, float clearRadius)
        {
            t = Mathf.Clamp01(t);
            Vector3 a = from - center;
            Vector3 b = to - center;
            float radiusA = Mathf.Sqrt(a.x * a.x + a.z * a.z);
            float radiusB = Mathf.Sqrt(b.x * b.x + b.z * b.z);
            float angleA = Mathf.Atan2(a.x, a.z) * Mathf.Rad2Deg;
            float angleB = Mathf.Atan2(b.x, b.z) * Mathf.Rad2Deg;
            float angle = angleA + Mathf.DeltaAngle(angleA, angleB) * t;
            float closeIn = Smoothing.SmoothStep(CloseInFrom, 1f, t);
            float swept = Mathf.Max(Mathf.Lerp(radiusA, radiusB, t), Mathf.Lerp(clearRadius, radiusB, closeIn));
            float radius = Mathf.Lerp(swept, radiusB, closeIn * closeIn);
            float height = Mathf.Lerp(a.y, b.y, t);
            Vector3 horizontal = WideShotComposer.Direction(angle) * radius;
            return center + new Vector3(horizontal.x, height, horizontal.z);
        }
    }
}
