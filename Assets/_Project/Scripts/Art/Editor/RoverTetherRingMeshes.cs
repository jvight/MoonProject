using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Kenji's spare tether ring (docs/design/first-hour-v2.md §3): a small warm-brass ring seated low behind 07's head,
    /// on its RingSocket, with a thin glass band that rover lights cyan only while the tether holds. Built in socket
    /// space: +Z out of the ring's face (backwards from the head), so the eye's silhouette stays clear.
    /// </summary>
    internal static class RoverTetherRingMeshes
    {
        private const float RingRadius = 0.1f;
        private const float RingTube = 0.008f;
        private const float BandTube = 0.0035f;
        private const float BandForward = 0.008f;
        private const float StudRadius = 0.0065f;
        private const int Segments = 24;
        private const int TubeSides = 6;
        private const int Studs = 3;

        /// <summary>Lays a torus's axis (local Y) along the socket's +Z.</summary>
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
