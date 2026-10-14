using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The places finds live in (0.5.5 owner feedback §2, "no floating loot"): each container a sealed body with a lid
    /// whose seam glows until one cut opens it, and the debris pockets relics are dug from. Container space: the
    /// root on the ground at the body's middle, +Z its front. Lid space: the origin on the lid's hinge along its back
    /// edge, the lid reaching forward along +Z; turning it about local X by a negative angle lifts it open.
    /// </summary>
    internal static class LootContainerMeshes
    {
        private const float SealBead = 0.014f;

        /// <summary>A container's proportions: its body, its lid's thickness, its paint and its band.</summary>
        public readonly struct Shape
        {
            public Shape(Vector3 body, float lid, PaletteSwatch paint, PaletteSwatch band)
            {
                Body = body;
                Lid = lid;
                Paint = paint;
                Band = band;
            }

            public Vector3 Body { get; }

            public float Lid { get; }

            public PaletteSwatch Paint { get; }

            public PaletteSwatch Band { get; }

            /// <summary>The lid's hinge, along the top of the body's back edge (container space).</summary>
            public Vector3 Hinge => new Vector3(0f, Body.y, -Body.z * 0.5f);

            /// <summary>The seal's cut point (lid space), on the front seam, +Z out of it.</summary>
            public Vector3 SealPoint => new Vector3(0f, -0.004f, Body.z + 0.012f);

            /// <summary>Where the find sits inside (container space), on the body's floor.</summary>
            public Vector3 Item => new Vector3(0f, Body.y * 0.25f, 0f);
        }

        /// <summary>The depot's coil crate: a long wooden-banded crate the Tower Coil travels in.</summary>
        public static readonly Shape CoilCrate = new Shape(new Vector3(0.78f, 0.4f, 0.5f), 0.08f, PaletteSwatch.Sage,
            PaletteSwatch.Wood);

        /// <summary>Ro's tape case beside Bell: small, hard, in her orange.</summary>
        public static readonly Shape TapeCase = new Shape(new Vector3(0.34f, 0.12f, 0.24f), 0.04f,
            PaletteSwatch.WarmAccent, PaletteSwatch.Charcoal);

        /// <summary>The garage's junction box, on its post: grey steel, a yellow-banded lid.</summary>
        public static readonly Shape JunctionBox = new Shape(new Vector3(0.42f, 0.3f, 0.28f), 0.06f,
            PaletteSwatch.Metal, PaletteSwatch.Honey);

        /// <summary>
        /// The tape deck's crate at Kenji's port: small enough for his bench, in 07's cream with an orange band, so it
        /// reads as 07's own spare before the stencil is read.
        /// </summary>
        public static readonly Shape SpareCrate = new Shape(new Vector3(0.36f, 0.2f, 0.16f), 0.04f,
            PaletteSwatch.Cream, PaletteSwatch.WarmAccent);

        /// <summary>The supply pod's hatch (on top of its drum, pod space).</summary>
        public static readonly Shape PodHatch = new Shape(new Vector3(0.62f, 0.36f, 0.36f), 0.05f,
            PaletteSwatch.WornPanel, PaletteSwatch.FadedAccent);

        public static readonly Vector3 PodHatchHinge = new Vector3(0f, 0.735f, -0.18f);

        public static readonly Vector3 PodItem = new Vector3(0f, 0.32f, 0f);

        /// <summary>The supply pod's cracked locator beacon (pod space), atop its bent mast about 3.5 m up, where the
        /// base can see it (M3-04: the ledge's warm light reads from home).</summary>
        public static readonly Vector3 PodBeacon = new Vector3(-0.12f, 3.5f, 0.32f);

        // The locator mast: its foot on the pod's tail, the kink the crash bent into it, and its top under the beacon.
        private static readonly Vector3 MastFoot = new Vector3(-0.42f, 0.72f, 0f);
        private static readonly Vector3 MastKink = new Vector3(-0.46f, 2.05f, 0.06f);
        private static readonly Vector3 MastTop = PodBeacon - new Vector3(0f, 0.09f, 0f);

        /// <summary>A container's body: a box with a dark band round it, corner feet and a stencil plate.</summary>
        public static LowPolyMeshBuilder Body(Shape shape, float post)
        {
            LowPolyMeshBuilder b = Shell(shape, post);
            Vector3 size = shape.Body;
            b.Box(At(size.x * 0.22f, post + size.y * 0.7f, size.z * 0.5f + 0.004f),
                new Vector3(size.x * 0.25f, size.y * 0.18f, 0.006f), PaletteSwatch.Cream);
            return b;
        }

        /// <summary>The spare crate's body: the shell, "07 · SPARE" stencilled in charcoal over its band.</summary>
        public static LowPolyMeshBuilder SpareCrateBody()
        {
            Shape shape = SpareCrate;
            LowPolyMeshBuilder b = Shell(shape, 0f);
            Vector3 size = shape.Body;
            Matrix4x4 front = SiteKit.Face(new Vector3(0f, size.y * 0.76f, size.z * 0.5f + 0.002f), Vector3.forward,
                Vector3.up);
            Glyphs.Write(b, front, SpareStencil, size.y * 0.2f, PaletteSwatch.Charcoal, shape.Paint);
            return b;
        }

        /// <summary>The words on the spare crate.</summary>
        public const string SpareStencil = "07 · SPARE";

        private static LowPolyMeshBuilder Shell(Shape shape, float post)
        {
            var b = new LowPolyMeshBuilder(420);
            Vector3 size = shape.Body;
            if (post > 0f)
            {
                b.Box(At(0f, post * 0.5f, -size.z * 0.25f), new Vector3(0.06f, post, 0.06f), PaletteSwatch.Charcoal);
            }

            MeshRange box = b.Box(At(0f, post + size.y * 0.5f, 0f), size, shape.Paint, Mathf.Min(0.02f, size.y * 0.1f));
            SiteKit.DustCap(b, box);
            b.Box(At(0f, post + size.y * 0.45f, 0f), new Vector3(size.x + 0.01f, size.y * 0.16f, size.z + 0.01f),
                shape.Band);
            return b;
        }

        /// <summary>The lid (lid space): a slab reaching forward from the hinge, with a grab lip at its front.</summary>
        public static LowPolyMeshBuilder Lid(Shape shape)
        {
            var b = new LowPolyMeshBuilder(80);
            Vector3 size = shape.Body;
            MeshRange lid = b.Box(At(0f, shape.Lid * 0.5f, size.z * 0.5f),
                new Vector3(size.x + 0.02f, shape.Lid, size.z + 0.02f), shape.Paint,
                Mathf.Min(0.012f, shape.Lid * 0.2f));
            SiteKit.DustCap(b, lid);
            b.Box(At(0f, shape.Lid * 0.4f, size.z + 0.016f), new Vector3(size.x * 0.3f, shape.Lid * 0.5f, 0.016f),
                PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>The seal (lid space): a warm glass bead round the lid's seam.</summary>
        public static LowPolyMeshBuilder Seal(Shape shape)
        {
            var b = new LowPolyMeshBuilder(60);
            float width = shape.Body.x + 0.03f;
            float depth = shape.Body.z + 0.03f;
            b.Box(At(0f, -SealBead * 0.5f, depth - 0.015f), new Vector3(width, SealBead, SealBead),
                PaletteSwatch.LampGlass);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * width * 0.5f, -SealBead * 0.5f, depth * 0.5f - 0.015f),
                    new Vector3(SealBead, SealBead, depth), PaletteSwatch.LampGlass);
            }

            return b;
        }

        /// <summary>
        /// The supply pod (pod space: its middle on the ground, the drum lying along X, +Z its hatch side): a squat
        /// drum half-sunk in the dust, a cone nose, fins at the tail and the hatch on top, its edges dented from the
        /// fall; and its locator mast, bent at a kink from the crash, one guy wire still staked and the other snapped and
        /// hanging, the beacon's housing at its top.
        /// </summary>
        public static LowPolyMeshBuilder PodBody()
        {
            var b = new LowPolyMeshBuilder(500);
            Matrix4x4 drum = At(new Vector3(0f, 0.32f, 0f), new Vector3(0f, 0f, 90f));
            MeshRange shell = b.Prism(drum, 0.42f, 1.2f, 10, PaletteSwatch.WornPanel,
                displacement: new Displacement(31, 0.015f, 3f));
            SiteKit.DustCap(b, shell);
            b.Frustum(At(new Vector3(0.7f, 0.32f, 0f), new Vector3(0f, 0f, -90f)), 0.42f, 0.18f, 0.22f, 10,
                PaletteSwatch.Metal);
            b.Prism(At(new Vector3(-0.64f, 0.32f, 0f), new Vector3(0f, 0f, 90f)), 0.36f, 0.08f, 10,
                PaletteSwatch.Charcoal);
            for (int i = 0; i < 3; i++)
            {
                Quaternion turn = Quaternion.Euler(i * 120f + 20f, 0f, 0f);
                b.Box(At(new Vector3(-0.66f, 0.32f, 0f) + turn * new Vector3(0f, 0.5f, 0f), turn),
                    new Vector3(0.18f, 0.18f, 0.02f), PaletteSwatch.FadedAccent);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(new Vector3(side * 0.34f, 0.58f, 0f)), new Vector3(0.04f, 0.06f, 0.42f),
                    PaletteSwatch.Charcoal);
            }

            b.Prism(At(MastFoot + Vector3.up * 0.03f), 0.06f, 0.06f, 8, PaletteSwatch.Charcoal);
            SiteKit.Bar(b, MastFoot, MastKink, 0.045f, PaletteSwatch.Metal);
            SiteKit.Bar(b, MastKink, MastTop, 0.04f, PaletteSwatch.Metal);
            b.Icosphere(At(MastKink), 0.04f, 0, PaletteSwatch.Rust);
            b.Frustum(At(MastTop + Vector3.up * 0.02f), 0.05f, 0.075f, 0.05f, 8, PaletteSwatch.Charcoal);
            b.Frustum(At(PodBeacon + Vector3.up * 0.08f), 0.075f, 0.03f, 0.04f, 8, PaletteSwatch.Charcoal);
            Vector3 guyHigh = Vector3.Lerp(MastKink, MastTop, 0.45f);
            SiteKit.Cable(b, SiteKit.Sag(guyHigh, new Vector3(-1.6f, 0.02f, 1.1f), 0.06f, 6), 0.006f,
                PaletteSwatch.Charcoal);
            b.Box(At(new Vector3(-1.6f, 0.04f, 1.1f)), new Vector3(0.05f, 0.08f, 0.05f), PaletteSwatch.Metal);
            SiteKit.Cable(b, new[] { guyHigh, guyHigh + new Vector3(0.12f, -0.55f, -0.18f),
                guyHigh + new Vector3(0.2f, -0.95f, -0.2f) }, 0.006f, PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>The pod's cracked beacon glass (beacon space), which glows warm.</summary>
        public static LowPolyMeshBuilder PodBeaconGlass()
        {
            var b = new LowPolyMeshBuilder(60);
            b.Icosphere(At(Vector3.zero, Quaternion.identity, new Vector3(0.8f, 1f, 1f)), 0.055f, 1,
                PaletteSwatch.LampGlass, new Displacement(5, 0.008f, 30f));
            return b;
        }

        /// <summary>
        /// A debris pocket a relic is dug from (pocket space: its middle on the ground, +Z the open side): a dark hollow
        /// half-covered by a fallen panel resting on rubble, so the dig spot reads as a gap under the wreck.
        /// </summary>
        public static LowPolyMeshBuilder Pocket(int seed)
        {
            var b = new LowPolyMeshBuilder(400);
            b.Icosphere(At(Vector3.zero, Quaternion.identity, new Vector3(1f, 0.06f, 0.8f)), 0.34f, 1,
                PaletteSwatch.Charcoal, new Displacement(seed, 0.05f, 5f));
            b.Icosphere(At(Vector3.zero, Quaternion.identity, new Vector3(1f, 0.04f, 0.85f)), 0.46f, 1,
                PaletteSwatch.DustShadow, new Displacement(seed + 9, 0.06f, 4f));
            var random = new System.Random(seed);
            for (int i = 0; i < 5; i++)
            {
                float angle = (i * 72f + (float)random.NextDouble() * 30f) * Mathf.Deg2Rad;
                if (Mathf.Sin(angle) > 0.6f)
                {
                    continue;
                }

                var at = new Vector3(Mathf.Cos(angle) * 0.38f, 0.04f, Mathf.Sin(angle) * 0.38f);
                b.Icosphere(At(at, Quaternion.identity, new Vector3(1f, 0.6f, 1f)),
                    0.07f + 0.04f * (float)random.NextDouble(), 0, PaletteSwatch.RockDark,
                    new Displacement(seed + i, 0.02f, 9f));
            }

            Matrix4x4 panel = At(new Vector3(0.03f, 0.13f, -0.04f), new Vector3(-17f, 12f, 5f));
            MeshRange plate = b.Box(panel, new Vector3(0.72f, 0.035f, 0.52f), PaletteSwatch.WornPanel, 0.01f);
            SiteKit.DustCap(b, plate);
            b.Box(panel * At(0f, 0.022f, 0.1f), new Vector3(0.68f, 0.01f, 0.04f), PaletteSwatch.FadedAccent);
            return b;
        }

        /// <summary>Where the relic and its glint sit in the pocket, in the gap under the panel's raised edge.</summary>
        public static readonly Vector3 DigSpot = new Vector3(0f, 0.02f, 0.12f);

        /// <summary>Where a buried friend part and its glint sit in the front gap of a <see cref="RockfallPocket"/>.</summary>
        public static readonly Vector3 RockfallDigSpot = new Vector3(-0.04f, 0.06f, 0.24f);

        /// <summary>
        /// A small rockfall slumped against a canyon alcove wall (pocket space: root on the ground, -Z against the wall,
        /// +Z facing out into the alcove): five angular rocks over a talus drift with a dark hollow gap under the front
        /// lintel slab where Bell's buried part rests at <see cref="RockfallDigSpot"/>.
        /// </summary>
        public static LowPolyMeshBuilder RockfallPocket()
        {
            var b = new LowPolyMeshBuilder(600);
            Paint rock = Paint.Facing(PaletteSwatch.RockLight, PaletteSwatch.RockDark, Vector3.up, 0.35f);

            b.Icosphere(At(new Vector3(0f, 0.015f, 0.22f), Quaternion.identity, new Vector3(1f, 0.06f, 0.8f)), 0.38f, 1,
                PaletteSwatch.Charcoal, new Displacement(71, 0.04f, 5f));
            b.Icosphere(At(new Vector3(0f, 0.01f, 0.2f), Quaternion.identity, new Vector3(1f, 0.04f, 0.85f)), 0.52f, 1,
                PaletteSwatch.DustShadow, new Displacement(79, 0.05f, 4f));

            b.Icosphere(At(new Vector3(-0.36f, 0.05f, 0.04f), new Vector3(0f, -18f, 0f), new Vector3(1.1f, 0.32f, 0.9f)),
                0.34f, 1, PaletteSwatch.DustMid, new Displacement(83, 0.025f, 5f));
            b.Icosphere(At(new Vector3(0.38f, 0.05f, 0.02f), new Vector3(0f, 22f, 0f), new Vector3(1.1f, 0.32f, 0.9f)),
                0.32f, 1, PaletteSwatch.DustMid, new Displacement(89, 0.025f, 5f));

            b.Icosphere(At(new Vector3(0.02f, 0.38f, -0.24f), new Vector3(-14f, 8f, -4f), new Vector3(1.15f, 1.15f, 0.9f)),
                0.34f, 1, rock, new Displacement(101, 0.04f, 4.5f));
            b.Icosphere(At(new Vector3(-0.42f, 0.25f, -0.14f), new Vector3(-8f, -24f, 10f), new Vector3(1.1f, 0.95f, 0.95f)),
                0.28f, 1, rock, new Displacement(107, 0.035f, 5f));
            b.Icosphere(At(new Vector3(0.44f, 0.24f, -0.12f), new Vector3(-10f, 19f, -12f), new Vector3(1.05f, 0.9f, 0.95f)),
                0.27f, 1, rock, new Displacement(113, 0.035f, 5f));
            b.Box(At(new Vector3(-0.36f, 0.16f, 0.2f), new Vector3(12f, -28f, -14f)), new Vector3(0.44f, 0.22f, 0.36f),
                rock, 0.04f, new Displacement(127, 0.02f, 6f));
            b.Box(At(new Vector3(0.14f, 0.3f, 0.08f), new Vector3(-16f, 18f, 12f)), new Vector3(0.62f, 0.16f, 0.48f),
                rock, 0.035f, new Displacement(131, 0.02f, 5.5f));

            b.Shave(b.RangeFrom(0), Vector3.down, 0f);
            return b;
        }
    }
}

