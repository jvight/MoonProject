using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The cutter's charge-up on 07 (0.5.5 owner feedback §5.4, the owner's pick "Cells"): power runs from the battery
    /// bar through two mirrored armoured conduits along both sides of the solar wing, through three pairs of capacitor
    /// cells whose glass sleeves slide forward as each stage fills, up the neck, to the lock ring round the Focus Lens
    /// socket; and the Focus Lens itself, a ringed lens clipped into the eye. Every glass is
    /// <see cref="PaletteSwatch.EyeGlass"/>: dark until rover raises it, then the cutter's cyan. Cell space: its centre
    /// on the conduit's axis, +Z along the conduit toward the neck. Lens space: the origin on the eye's axis at the lens
    /// front, +Z out of the eye.
    /// </summary>
    internal static class ChargeRunMeshes
    {
        /// <summary>Where the lens's glass sits in its ring (lens space).</summary>
        public static readonly Vector3 LensSeat = new Vector3(0f, 0f, LensZ);

        /// <summary>The conduits' radius; their axis rides this high above the lid.</summary>
        public const float PipeRadius = 0.014f;

        /// <summary>The deck conduits' run beside the wing (root x, the left one; the right mirrors it).</summary>
        public const float ConduitX = -0.29f;

        /// <summary>Where along the deck conduits the three cell pairs sit (root z), the battery end first: clear of the
        /// antenna, the carried tube and the neck's base.</summary>
        public static readonly float[] CellZ = { -0.4f, -0.13f, 0.1f };

        /// <summary>How far a cell's sleeve slides forward along its local +Z when its stage fills.</summary>
        public const float SleeveTravel = 0.065f;

        private const float CoreRadius = 0.006f;
        private const float CellRadius = 0.036f;
        private const float CellLength = 0.144f;
        private const float SleeveLength = 0.06f;
        private const float LockRadius = 0.19f;

        // The clip ring hugs the eye's rim; the lens floats on three spokes in front of the eye's own glass.
        private const float ClipRadius = 0.165f;
        private const float ClipZ = -0.029f;
        private const float LensRadius = 0.074f;
        private const float LensZ = 0.026f;
        private static readonly float[] SpokeDegrees = { 90f, 210f, 330f };

        private static readonly Vector3 PoleTop = RoverMeshes.HeadHinge - new Vector3(0f, 0.03f, 0f);

        /// <summary>Stage <paramref name="stage"/>'s (0..2) left cell on its conduit (root space).</summary>
        public static Vector3 Cell(int stage)
        {
            return new Vector3(ConduitX, RoverMeshes.LidTop + PipeRadius, CellZ[stage]);
        }

        /// <summary>The deck conduit (root space, the left one): from the battery bar's end, beside the wing, to the
        /// neck's base.</summary>
        public static Vector3[] DeckConduit()
        {
            float lid = RoverMeshes.LidTop + PipeRadius;
            return new[]
            {
                new Vector3(-0.24f, lid, -0.55f), new Vector3(ConduitX, lid, -0.46f), new Vector3(ConduitX, lid, 0.16f),
                new Vector3(-0.085f, lid, 0.25f),
            };
        }

        /// <summary>The neck conduit (neck space, the left one): up the pole's side into the head.</summary>
        public static Vector3[] NeckConduit()
        {
            return new[] { new Vector3(-0.072f, 0.03f, -0.01f), new Vector3(-0.072f, PoleTop.y, PoleTop.z - 0.01f) };
        }

        /// <summary>Both conduits along <paramref name="path"/> and its mirror: dark armoured pipe.</summary>
        public static LowPolyMeshBuilder ConduitPair(Vector3[] path)
        {
            var b = new LowPolyMeshBuilder(400);
            foreach (Vector3[] run in new[] { path, Mirror(path) })
            {
                for (int i = 0; i + 1 < run.Length; i++)
                {
                    RecipeKit.Rod(b, run[i], run[i + 1], PipeRadius, 4, PaletteSwatch.Charcoal);
                }
            }

            return b;
        }

        /// <summary>The glass core line along both conduits, the energy's path, on their <paramref name="up"/>
        /// side.</summary>
        public static LowPolyMeshBuilder ConduitCore(Vector3[] path, Vector3 up)
        {
            var b = new LowPolyMeshBuilder(160);
            foreach (Vector3[] run in new[] { path, Mirror(path) })
            {
                for (int i = 0; i + 1 < run.Length; i++)
                {
                    Vector3 lift = up * (PipeRadius - CoreRadius * 0.3f);
                    RecipeKit.Rod(b, run[i] + lift, run[i + 1] + lift, CoreRadius, 3, PaletteSwatch.EyeGlass);
                }
            }

            return b;
        }

        /// <summary>A capacitor cell on its conduit (cell space): a hexagonal metal canister.</summary>
        public static LowPolyMeshBuilder Cell()
        {
            var b = new LowPolyMeshBuilder(40);
            b.Prism(At(Vector3.zero, AlongZ), CellRadius, CellLength, 6, PaletteSwatch.Metal);
            return b;
        }

        /// <summary>The cell's sleeve (sleeve space, its rest seat at the canister's back end): a glass band that
        /// slides forward by <see cref="SleeveTravel"/> as its stage fills.</summary>
        public static LowPolyMeshBuilder Sleeve()
        {
            var b = new LowPolyMeshBuilder(60);
            b.Prism(At(new Vector3(0f, 0f, -SleeveLength * 0.5f), AlongZ), CellRadius * 1.08f, SleeveLength, 6,
                PaletteSwatch.EyeGlass);
            return b;
        }

        /// <summary>The lock ring's dark track round the Focus Lens socket (eye space).</summary>
        public static LowPolyMeshBuilder LockTrack()
        {
            var b = new LowPolyMeshBuilder(160);
            b.Torus(At(new Vector3(0f, 0f, -0.004f), new Vector3(90f, 0f, 0f)), LockRadius, 0.011f, 12, 3,
                PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>The lock ring's band, which lights at full lock (pushed toward white).</summary>
        public static LowPolyMeshBuilder LockRing()
        {
            var b = new LowPolyMeshBuilder(120);
            b.Torus(At(new Vector3(0f, 0f, 0.004f), new Vector3(90f, 0f, 0f)), LockRadius, 0.007f, 12, 3,
                PaletteSwatch.EyeGlass);
            return b;
        }

        public static Vector3 Mirror(Vector3 point)
        {
            return new Vector3(-point.x, point.y, point.z);
        }

        /// <summary>The Focus Lens's frame: a clip ring on the eye's rim, three spokes and a brass ring round the
        /// lens.</summary>
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

        private static Vector3[] Mirror(Vector3[] path)
        {
            var mirrored = new Vector3[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                mirrored[i] = Mirror(path[i]);
            }

            return mirrored;
        }
    }
}
