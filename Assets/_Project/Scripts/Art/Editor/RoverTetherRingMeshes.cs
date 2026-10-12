using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Kenji's spare tether ring (docs/design/first-hour-v2.md §3): a brass bezel round 07's eye, where the beam comes
    /// from, with a thin glass band on its front face that rover lights faintly cyan at rest and brightly while the
    /// tether holds. Built in Eye space: +Z out of the lens, the ring round the lens rim so the face stays the same
    /// gentle eye, only framed.
    /// </summary>
    internal static class RoverTetherRingMeshes
    {
        private const float RingRadius = 0.152f;
        private const float RingTube = 0.012f;
        private const float BandTube = 0.005f;
        private const float BandForward = 0.012f;
        private const float StudRadius = 0.01f;
        private const int Segments = 24;
        private const int TubeSides = 6;
        private const int Studs = 3;

        /// <summary>Lays a torus's axis (local Y) along the lens's +Z.</summary>
        private static readonly Vector3 AlongZ = new Vector3(90f, 0f, 0f);

        public static LowPolyMeshBuilder Ring()
        {
            var b = new LowPolyMeshBuilder(600);
            b.Torus(At(Vector3.zero, AlongZ), RingRadius, RingTube, Segments, TubeSides, PaletteSwatch.Honey);
            for (int i = 0; i < Studs; i++)
            {
                float angle = (90f + i * 360f / Studs) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Cos(angle) * RingRadius, Mathf.Sin(angle) * RingRadius, -RingTube);
                b.Icosphere(At(at), StudRadius, 0, PaletteSwatch.Metal);
            }

            return b;
        }

        public static LowPolyMeshBuilder Band()
        {
            var b = new LowPolyMeshBuilder(300);
            b.Torus(At(new Vector3(0f, 0f, BandForward), AlongZ), RingRadius, BandTube, Segments, 4,
                PaletteSwatch.SignalGlass);
            return b;
        }
    }
}
