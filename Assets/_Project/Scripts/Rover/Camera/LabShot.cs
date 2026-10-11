using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Where the lab camera stands and looks (docs/features/M3-19 §6), as plain math: the ring-rail pose opposite the
    /// way 07 faces, over its shoulder; the crane path from the chase view under the belly and up through the hatch to
    /// the ring (and back the other way); and the three-quarter look in at a station's face.
    /// </summary>
    public static class LabShot
    {
        private const float HalfTurn = 180f;

        /// <summary>The horizontal direction of a world yaw (deg).</summary>
        public static Vector3 Direction(float yaw)
        {
            float radians = yaw * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
        }

        /// <summary>
        /// The ring camera for 07 facing <paramref name="facingYaw"/> at the shaft <paramref name="axis"/>: on the ring
        /// of <paramref name="radius"/> at <paramref name="railHeight"/> (less the drop), opposite the way 07 faces
        /// and turned round by the shoulder angle.
        /// </summary>
        public static Vector3 RingPosition(Vector3 axis, float railHeight, float radius, float facingYaw,
            LabCameraSettings settings)
        {
            Vector3 around = Direction(facingYaw + HalfTurn + settings.Shoulder) * radius;
            return new Vector3(axis.x + around.x, railHeight - settings.RailDrop, axis.z + around.z);
        }

        /// <summary>
        /// What the ring camera looks at: past 07 toward the station it faces, the tuned share of the way to the
        /// stations' faces, at their height.
        /// </summary>
        public static Vector3 RingAim(Vector3 axis, float stationHeight, float stationReach, float facingYaw,
            LabCameraSettings settings)
        {
            Vector3 ahead = Direction(facingYaw) * (stationReach * settings.AimAhead);
            return new Vector3(axis.x + ahead.x, stationHeight, axis.z + ahead.z);
        }

        /// <summary>The close look at a station's face: out from it (its +Z), to its side and a little above.</summary>
        public static Vector3 FocusPosition(Vector3 face, Vector3 outward, Vector3 side, LabCameraSettings settings)
        {
            return face + outward * settings.FocusDistance + side * settings.FocusSide
                   + Vector3.up * settings.FocusHeight;
        }

        /// <summary>
        /// The crane path at <paramref name="t"/> (0..1, eased by the caller) through <paramref name="from"/>, under
        /// the belly, up the shaft's gap from the hatch to just over the cabin floor and on to <paramref name="to"/>: a
        /// smooth curve through the five points, a quarter of the way per stretch.
        /// </summary>
        public static Vector3 Crane(Vector3 from, Vector3 underBelly, Vector3 hatch, Vector3 floor, Vector3 to, float t)
        {
            float span = Mathf.Clamp01(t) * 4f;
            int segment = Mathf.Min(3, (int)span);
            float local = span - segment;
            switch (segment)
            {
                case 0:
                    return CatmullRom(from, from, underBelly, hatch, local);
                case 1:
                    return CatmullRom(from, underBelly, hatch, floor, local);
                case 2:
                    return CatmullRom(underBelly, hatch, floor, to, local);
                default:
                    return CatmullRom(hatch, floor, to, to, local);
            }
        }

        /// <summary>
        /// The point under the belly the crane passes, out from the shaft toward <paramref name="toward"/>.
        /// </summary>
        public static Vector3 UnderBelly(Vector3 ground, Vector3 toward, LabCameraSettings settings)
        {
            return ground + Outward(ground, toward) * settings.UnderBellyOut + Vector3.up * settings.UnderBellyHeight;
        }

        /// <summary>
        /// The point at <paramref name="height"/> in the shaft's gap the crane rises through, out from the shaft's
        /// <paramref name="axis"/> toward the ring pose.
        /// </summary>
        public static Vector3 Shaft(Vector3 axis, float height, Vector3 ring, LabCameraSettings settings)
        {
            Vector3 point = axis + Outward(axis, ring) * settings.HatchOut;
            return new Vector3(point.x, height, point.z);
        }

        /// <summary>Smoothstep: eased at both ends.</summary>
        public static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static Vector3 Outward(Vector3 from, Vector3 toward)
        {
            Vector3 flat = toward - from;
            flat.y = 0f;
            return flat.sqrMagnitude > 1e-6f ? flat.normalized : Vector3.forward;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                           + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }
    }
}
