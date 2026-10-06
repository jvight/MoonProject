using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The tether's curve: a quadratic bezier from 07's eye to the relic whose middle bows up a little and drifts in
    /// a slow, energetic wobble (stronger when the tether strains).
    /// </summary>
    public static class TetherBeamShape
    {
        // Unrelated rates per axis so the wobble never repeats in an obvious loop.
        private const float VerticalRate = 1.31f;
        private const float DepthRate = 0.73f;

        /// <summary>Control point of the curve at <paramref name="time"/> seconds.</summary>
        public static Vector3 Control(Vector3 start, Vector3 end, float arc, float wobble, float frequency,
            float time)
        {
            Vector3 middle = (start + end) * 0.5f;
            float length = Vector3.Distance(start, end);
            float phase = 2f * Mathf.PI * frequency * time;
            var drift = new Vector3(Mathf.Sin(phase), Mathf.Cos(phase * VerticalRate), Mathf.Sin(phase * DepthRate));
            return middle + Vector3.up * (arc * length) + drift * wobble;
        }

        public static Vector3 Point(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            float u = 1f - t;
            return u * u * start + 2f * u * t * control + t * t * end;
        }
    }
}
