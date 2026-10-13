using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Preview-only candidates for 07's charge-up (0.5.5 owner feedback §5.4): symmetric hardware that carries the
    /// cutter's power from the battery bar to the emitter. Every variant shares the mirrored conduits (along both sides
    /// of the centred wing, then up the neck, each with a glass core line) and the lock ring round the Focus Lens
    /// socket. Its three stages are mirrored pairs on the deck conduits, spread down 07's length so they read from the
    /// chase camera: coils that turn a notch, vanes that stand up, or cells whose sleeves slide forward. Piece space:
    /// its centre on the conduit's axis, +Z along the conduit toward the neck, +Y up. Other spaces: 07's root for the
    /// deck, the Neck for the neck, the Eye for the ring.
    /// </summary>
    internal static class ChargeVariantMeshes
    {
        public const string Coils = "Coils";
        public const string Vanes = "Vanes";
        public const string Cells = "Cells";

        public static readonly string[] Variants = { Coils, Vanes, Cells };

        /// <summary>How far the conduit's axis sits above the lid, and its radius.</summary>
        public const float PipeRadius = 0.014f;

        /// <summary>Where along the deck conduits the three stage pairs sit (root z), battery end first.</summary>
        public static readonly float[] StageZ = { -0.36f, -0.13f, 0.1f };

        /// <summary>The deck conduits' run beside the wing (root x, the left one).</summary>
        public const float ConduitX = -0.29f;

        private const float CoreRadius = 0.006f;

        private static readonly Vector3 PoleTop = RoverMeshes.HeadHinge - new Vector3(0f, 0.03f, 0f);

        // A vane's outline in its plane: x along the conduit, y up from the pipe's top.
        private static readonly Vector2[] VaneOutline =
        {
            new Vector2(-0.06f, 0f), new Vector2(0.06f, 0f), new Vector2(0.055f, 0.04f), new Vector2(0.025f, 0.078f),
            new Vector2(-0.03f, 0.072f), new Vector2(-0.058f, 0.035f),
        };

        /// <summary>Stage <paramref name="stage"/>'s left piece on its conduit (root space).</summary>
        public static Vector3 StagePiece(int stage)
        {
            return new Vector3(ConduitX, RoverMeshes.LidTop + PipeRadius, StageZ[stage]);
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

        /// <summary>Both conduits along <paramref name="path"/> and its mirror: dark armoured pipe with metal
        /// clamps.</summary>
        public static LowPolyMeshBuilder ConduitPair(Vector3[] path)
        {
            var b = new LowPolyMeshBuilder(400);
            foreach (Vector3[] run in new[] { path, Mirror(path) })
            {
                for (int i = 0; i + 1 < run.Length; i++)
                {
                    RecipeKit.Rod(b, run[i], run[i + 1], PipeRadius, 6, PaletteSwatch.Charcoal);
                    b.Icosphere(At(run[i + 1]), PipeRadius * 1.25f, 0, PaletteSwatch.Metal);
                }
            }

            return b;
        }

        /// <summary>The glass core line on both conduits, the energy's path.</summary>
        public static LowPolyMeshBuilder ConduitCore(Vector3[] path, Vector3 up)
        {
            var b = new LowPolyMeshBuilder(160);
            foreach (Vector3[] run in new[] { path, Mirror(path) })
            {
                for (int i = 0; i + 1 < run.Length; i++)
                {
                    Vector3 lift = up * (PipeRadius - CoreRadius * 0.3f);
                    RecipeKit.Rod(b, run[i] + lift, run[i + 1] + lift, CoreRadius, 4, PaletteSwatch.EyeGlass);
                }
            }

            return b;
        }

        /// <summary>A capacitor coil round the conduit: a metal core, dark flanges and four tabs that show its notch
        /// as it turns about the conduit.</summary>
        public static LowPolyMeshBuilder CoilHousing()
        {
            var b = new LowPolyMeshBuilder(240);
            b.Prism(At(Vector3.zero, AlongZ), 0.03f, 0.06f, 10, PaletteSwatch.Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Torus(At(new Vector3(0f, 0f, side * 0.03f), new Vector3(90f, 0f, 0f)), 0.046f, 0.008f, 12, 3,
                    PaletteSwatch.Charcoal);
            }

            for (int i = 0; i < 4; i++)
            {
                Quaternion turn = Quaternion.Euler(0f, 0f, 45f + i * 90f);
                b.Box(At(turn * new Vector3(0f, 0.054f, 0.03f), turn), new Vector3(0.016f, 0.014f, 0.016f),
                    PaletteSwatch.Metal);
            }

            return b;
        }

        /// <summary>The coil's winding, the band that fills.</summary>
        public static LowPolyMeshBuilder CoilWinding()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Torus(At(Vector3.zero, new Vector3(90f, 0f, 0f)), 0.04f, 0.014f, 12, 3, PaletteSwatch.EyeGlass);
            return b;
        }

        /// <summary>A heat vane on the conduit: a rounded plate of dark metal standing on the pipe, folded flat outward
        /// at rest.</summary>
        public static LowPolyMeshBuilder VanePlate()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Extrude(VanePlacement(1f), VaneOutline, 0.012f, PaletteSwatch.Charcoal);
            b.Box(At(0f, PipeRadius, 0f), new Vector3(0.022f, 0.012f, 0.12f), PaletteSwatch.Metal);
            return b;
        }

        /// <summary>The vane's core, a smaller plate through it that fills on both faces.</summary>
        public static LowPolyMeshBuilder VaneCore()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Extrude(VanePlacement(0.6f) * At(0f, 0.012f, 0f), VaneOutline, 0.018f, PaletteSwatch.EyeGlass);
            return b;
        }

        /// <summary>A capacitor cell on the conduit: a metal canister with dark end rings and a sleeve guide.</summary>
        public static LowPolyMeshBuilder CellCanister()
        {
            var b = new LowPolyMeshBuilder(160);
            b.Prism(At(Vector3.zero, AlongZ), 0.03f, 0.12f, 10, PaletteSwatch.Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Prism(At(new Vector3(0f, 0f, side * 0.058f), AlongZ), 0.036f, 0.012f, 10, PaletteSwatch.Charcoal);
            }

            return b;
        }

        /// <summary>The cell's sleeve, a glass band that slides forward along the canister as it fills.</summary>
        public static LowPolyMeshBuilder CellSleeve()
        {
            var b = new LowPolyMeshBuilder(60);
            b.Prism(At(new Vector3(0f, 0f, -0.025f), AlongZ), 0.034f, 0.05f, 10, PaletteSwatch.EyeGlass);
            return b;
        }

        /// <summary>The lock ring round the Focus Lens socket (eye space): its dark track.</summary>
        public static LowPolyMeshBuilder LockTrack()
        {
            var b = new LowPolyMeshBuilder(160);
            b.Torus(At(new Vector3(0f, 0f, -0.004f), new Vector3(90f, 0f, 0f)), 0.19f, 0.011f, 18, 3,
                PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>The lock ring's band, which goes white at full lock.</summary>
        public static LowPolyMeshBuilder LockRing()
        {
            var b = new LowPolyMeshBuilder(120);
            b.Torus(At(new Vector3(0f, 0f, 0.004f), new Vector3(90f, 0f, 0f)), 0.19f, 0.007f, 18, 3,
                PaletteSwatch.EyeGlass);
            return b;
        }

        public static Vector3 Mirror(Vector3 point)
        {
            return new Vector3(-point.x, point.y, point.z);
        }

        public static Vector3[] Mirror(Vector3[] path)
        {
            var mirrored = new Vector3[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                mirrored[i] = Mirror(path[i]);
            }

            return mirrored;
        }

        private static Matrix4x4 VanePlacement(float scale)
        {
            return At(new Vector3(0f, PipeRadius, 0f)) * Matrix4x4.Rotate(Rotation(new Vector3(0f, -90f, 0f))) *
                   Matrix4x4.Scale(Vector3.one * scale);
        }
    }
}
