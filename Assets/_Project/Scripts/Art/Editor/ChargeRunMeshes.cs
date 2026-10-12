using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The cutter's charge run on 07 (M3-26 §2.1): tier 0's small dusty conduit lamps the run lights from the battery
    /// to the emitter, the Focus Lens's cleaner, brighter ones, and the Focus Lens itself, a ringed lens clipped into
    /// the eye.
    /// Every glass is <see cref="PaletteSwatch.EyeGlass"/>: dark until gameplay raises it, then the cutter's cyan.
    /// Lamp space: the origin on the surface it sits on, +Y out of it. Lens space: the origin on the eye's axis at the
    /// lens front, +Z out of the eye.
    /// </summary>
    internal static class ChargeRunMeshes
    {
        /// <summary>Where the lens's glass sits in its ring (lens space).</summary>
        public static readonly Vector3 LensSeat = new Vector3(0f, 0f, LensZ);

        private const float ConduitBezel = 0.022f;
        private const float ConduitGlass = 0.013f;
        private const float BrowBezel = 0.024f;
        private const float BrowGlass = 0.015f;
        private const float BezelHeight = 0.01f;
        private static readonly Vector3 GlassSeat = new Vector3(0f, 0.006f, 0f);

        // The clip ring hugs the eye's rim; the lens floats on three spokes in front of the eye's own glass.
        private const float ClipRadius = 0.165f;
        private const float ClipZ = -0.029f;
        private const float LensRadius = 0.074f;
        private const float LensZ = 0.026f;
        private static readonly float[] SpokeDegrees = { 90f, 210f, 330f };

        /// <summary>A tier-0 conduit lamp's bezel: low, flush and a little dusty.</summary>
        public static LowPolyMeshBuilder ConduitBezelMesh()
        {
            var b = new LowPolyMeshBuilder(60);
            b.Frustum(At(0f, BezelHeight * 0.5f - 0.002f, 0f), ConduitBezel, ConduitBezel * 0.8f, BezelHeight, 8,
                Paint.WithCaps(PaletteSwatch.Charcoal, PaletteSwatch.CakedDust));
            return b;
        }

        /// <summary>A Focus Lens lamp's bezel: a little proud of the surface, clean metal.</summary>
        public static LowPolyMeshBuilder BrowBezelMesh()
        {
            var b = new LowPolyMeshBuilder(60);
            b.Frustum(At(0f, BezelHeight * 0.5f, 0f), BrowBezel, BrowBezel * 0.85f, BezelHeight + 0.004f, 8,
                PaletteSwatch.Metal);
            return b;
        }

        /// <summary>The conduit lamp's glass (lamp space, seated in its bezel): a dusty little dome.</summary>
        public static LowPolyMeshBuilder ConduitGlassMesh()
        {
            var b = new LowPolyMeshBuilder(40);
            b.Icosphere(At(GlassSeat, Quaternion.identity, new Vector3(1f, 0.6f, 1f)), ConduitGlass, 0,
                PaletteSwatch.EyeGlass);
            return b;
        }

        /// <summary>A Focus Lens lamp's glass: bigger and rounder, so the tier-1 run reads brighter.</summary>
        public static LowPolyMeshBuilder BrowGlassMesh()
        {
            var b = new LowPolyMeshBuilder(90);
            b.Icosphere(At(GlassSeat, Quaternion.identity, new Vector3(1f, 0.75f, 1f)), BrowGlass, 0,
                PaletteSwatch.EyeGlass);
            return b;
        }

        /// <summary>The Focus Lens's frame: a clip ring on the eye's rim, three spokes and a brass ring round the lens.</summary>
        public static LowPolyMeshBuilder LensFrame()
        {
            var b = new LowPolyMeshBuilder(300);
            b.Torus(At(new Vector3(0f, 0f, ClipZ), new Vector3(90f, 0f, 0f)), ClipRadius, 0.009f, 18, 4,
                PaletteSwatch.Metal);
            foreach (float degrees in SpokeDegrees)
            {
                Vector3 direction = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad),
                    0f);
                SiteKit.Bar(b, direction * ClipRadius + Vector3.forward * ClipZ,
                    direction * (LensRadius + 0.008f) + Vector3.forward * LensZ, 0.01f, PaletteSwatch.Metal);
            }

            b.Torus(At(LensSeat, new Vector3(90f, 0f, 0f)), LensRadius + 0.006f, 0.011f, 16, 4, PaletteSwatch.Honey);
            b.Torus(At(LensSeat + Vector3.back * 0.01f, new Vector3(90f, 0f, 0f)), LensRadius * 0.7f, 0.004f, 12, 3,
                PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>The Focus Lens's glass (lens space at <see cref="LensSeat"/>): a shallow biconvex disc.</summary>
        public static LowPolyMeshBuilder LensGlass()
        {
            var b = new LowPolyMeshBuilder(120);
            b.Icosphere(At(Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 0.22f)), LensRadius, 2,
                PaletteSwatch.EyeGlass);
            return b;
        }
    }
}
