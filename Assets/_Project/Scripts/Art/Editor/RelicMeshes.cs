using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The six relics: precious, slightly worn memories of Earth, drawn to match the memory each one carries in
    /// its RelicDefinition. Each is drawn upright on y = 0 facing +Z at a large working size with chunky details;
    /// the builder scales it to its true size (VISION ruling 13: a duck stays a duck) and moves the pivot to the
    /// centre of mass. Details are drawn thick enough to survive that shrink.
    /// </summary>
    internal static class RelicMeshes
    {
        /// <summary>"Mixtape Walkman": a tape labelled for the long drive inside, orange-foam headphones.</summary>
        public static LowPolyMeshBuilder CassettePlayer()
        {
            var b = new LowPolyMeshBuilder(500);
            b.Box(At(0f, 0.2f, 0f), new Vector3(0.56f, 0.4f, 0.15f), PaletteSwatch.Sage, 0.035f);
            b.Box(At(0f, 0.2f, 0.07f), new Vector3(0.5f, 0.34f, 0.02f), PaletteSwatch.Metal, 0.006f);
            b.Box(At(-0.03f, 0.21f, 0.083f), new Vector3(0.36f, 0.21f, 0.012f), PaletteSwatch.Charcoal);
            b.Box(At(-0.03f, 0.285f, 0.09f), new Vector3(0.3f, 0.055f, 0.006f), PaletteSwatch.Cream);
            b.Box(At(new Vector3(-0.05f, 0.285f, 0.094f), new Vector3(0f, 0f, 3f)), new Vector3(0.2f, 0.026f, 0.004f),
                PaletteSwatch.WarmAccent);
            foreach (float x in new[] { -0.12f, 0.06f })
            {
                b.Prism(At(new Vector3(x, 0.195f, 0.09f), AlongZ), 0.045f, 0.01f, 10, PaletteSwatch.Cream);
                b.Prism(At(new Vector3(x, 0.195f, 0.096f), AlongZ), 0.022f, 0.008f, 6, PaletteSwatch.Charcoal);
            }

            for (int i = 0; i < 4; i++)
            {
                PaletteSwatch button = i == 1 ? PaletteSwatch.WarmAccent : PaletteSwatch.Metal;
                b.Box(At(-0.15f + i * 0.08f, 0.415f, 0.01f), new Vector3(0.06f, 0.03f, 0.08f), button, 0.008f);
            }

            b.Box(At(0.21f, 0.12f, 0.085f), new Vector3(0.05f, 0.1f, 0.012f), PaletteSwatch.Charcoal);
            b.Torus(At(new Vector3(0f, 0.38f, 0f), AlongZ), 0.26f, 0.022f, 16, 4, PaletteSwatch.Charcoal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Prism(At(new Vector3(side * 0.275f, 0.38f, 0f), AlongX), 0.07f, 0.04f, 10, PaletteSwatch.Charcoal);
                b.Icosphere(At(new Vector3(side * 0.31f, 0.38f, 0f), Vector3.zero, new Vector3(0.45f, 1f, 1f)), 0.085f,
                    1, PaletteSwatch.WarmAccent);
            }

            return b;
        }

        /// <summary>"Bath Duck": round and yellow, its left side sun-faded to cream.</summary>
        public static LowPolyMeshBuilder RubberDuck()
        {
            var b = new LowPolyMeshBuilder(800);
            MeshRange body = b.Icosphere(At(new Vector3(0f, 0.24f, 0f), Vector3.zero, new Vector3(1f, 0.74f, 1.25f)),
                0.3f, 2, PaletteSwatch.Honey);
            body = b.Shave(body, Vector3.down, -0.035f);
            b.Cone(At(new Vector3(0f, 0.37f, -0.34f), new Vector3(-55f, 0f, 0f)), 0.1f, 0.2f, 8, PaletteSwatch.Honey);
            b.Icosphere(At(new Vector3(0f, 0.57f, 0.18f)), 0.19f, 2, PaletteSwatch.Honey);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Icosphere(At(new Vector3(side * 0.27f, 0.27f, -0.03f), new Vector3(0f, side * 10f, 0f),
                    new Vector3(0.35f, 0.55f, 1f)), 0.2f, 1, PaletteSwatch.Honey);
            }

            b.RepaintFacing(b.RangeFrom(body.FirstTriangle), new Vector3(-1f, 0.35f, 0.1f), 0.55f,
                PaletteSwatch.Cream);
            b.Cone(At(new Vector3(0f, 0.54f, 0.37f), AlongZ, new Vector3(1f, 1f, 0.5f)), 0.085f, 0.16f, 8,
                PaletteSwatch.WarmAccent);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Icosphere(At(side * 0.095f, 0.62f, 0.34f), 0.045f, 1, PaletteSwatch.Charcoal);
            }

            return b;
        }

        /// <summary>"Golden Record": the golden disc sliding out of its engraved aluminium cover.</summary>
        public static LowPolyMeshBuilder GoldenRecord()
        {
            var b = new LowPolyMeshBuilder(600);
            const float centreY = 0.46f;
            b.Box(At(0f, centreY, -0.03f), new Vector3(0.86f, 0.86f, 0.035f), PaletteSwatch.Metal, 0.015f);
            b.Box(At(0f, 0.03f, -0.12f), new Vector3(0.5f, 0.06f, 0.24f), PaletteSwatch.Metal, 0.015f);
            var pulsar = new Vector3(-0.22f, centreY - 0.22f, -0.01f);
            for (int i = 0; i < 5; i++)
            {
                float angle = (i * 72f + 10f) * Mathf.Deg2Rad;
                float length = 0.1f + (i % 3) * 0.045f;
                Vector3 end = pulsar + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * length;
                b.Box(Along(pulsar, end), new Vector3(0.02f, 0.006f, length), PaletteSwatch.Charcoal);
            }

            b.Torus(At(new Vector3(-0.25f, centreY + 0.25f, -0.01f), AlongZ), 0.065f, 0.016f, 10, 3,
                PaletteSwatch.Charcoal);
            b.Box(At(-0.25f, centreY + 0.08f, -0.01f), new Vector3(0.16f, 0.02f, 0.006f), PaletteSwatch.Charcoal);

            var disc = new Vector3(0.12f, centreY, 0.012f);
            b.Prism(At(disc, AlongZ), 0.4f, 0.025f, 24, PaletteSwatch.Honey);
            foreach (float ring in new[] { 0.33f, 0.24f })
            {
                b.Torus(At(disc + new Vector3(0f, 0f, 0.012f), AlongZ), ring, 0.013f, 24, 3, PaletteSwatch.Charcoal);
            }

            b.Prism(At(disc + new Vector3(0f, 0f, 0.014f), AlongZ), 0.11f, 0.008f, 14, PaletteSwatch.Cream);
            b.Prism(At(disc + new Vector3(0f, 0f, 0.016f), AlongZ), 0.022f, 0.01f, 8, PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>"Moonwalker's Boot": one big suit boot, its sole lugs still packed with dust.</summary>
        public static LowPolyMeshBuilder AstronautBoot()
        {
            var b = new LowPolyMeshBuilder(700);
            b.Box(At(0f, 0.04f, 0.05f), new Vector3(0.34f, 0.08f, 0.66f), PaletteSwatch.Charcoal, 0.03f);
            for (int i = 0; i < 8; i++)
            {
                float z = -0.24f + i * 0.08f;
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Box(At(side * 0.172f, 0.035f, z), new Vector3(0.03f, 0.05f, 0.045f), PaletteSwatch.DustLight);
                }
            }

            b.Box(At(0f, 0.16f, 0.06f), new Vector3(0.32f, 0.17f, 0.52f), PaletteSwatch.Cream, 0.07f);
            b.Icosphere(At(new Vector3(0f, 0.14f, 0.26f), Vector3.zero, new Vector3(1f, 0.62f, 1f)), 0.165f, 1,
                PaletteSwatch.Cream);
            b.Frustum(At(0f, 0.43f, -0.1f), 0.155f, 0.175f, 0.44f, 10, PaletteSwatch.Cream);
            b.Torus(At(0f, 0.66f, -0.1f), 0.17f, 0.035f, 12, 4, PaletteSwatch.Metal);
            b.Torus(At(0f, 0.38f, -0.1f), 0.16f, 0.02f, 12, 3, PaletteSwatch.Sage);
            b.Box(At(new Vector3(0f, 0.255f, 0.12f), new Vector3(-28f, 0f, 0f)), new Vector3(0.34f, 0.04f, 0.06f),
                PaletteSwatch.Sage);
            b.Box(At(0.17f, 0.25f, 0.12f), new Vector3(0.025f, 0.05f, 0.05f), PaletteSwatch.Metal, 0.008f);
            b.Box(At(0.16f, 0.38f, -0.1f), new Vector3(0.025f, 0.05f, 0.05f), PaletteSwatch.Metal, 0.008f);
            b.Box(At(new Vector3(-0.15f, 0.5f, -0.1f), new Vector3(0f, -90f, 0f)), new Vector3(0.1f, 0.07f, 0.02f),
                PaletteSwatch.WarmAccent);
            return b;
        }

        /// <summary>"Enamel Teapot": sage enamelware with dark rims, one dented side and a couple of chips.</summary>
        public static LowPolyMeshBuilder Teapot()
        {
            var b = new LowPolyMeshBuilder(800);
            Vector2[] profile =
            {
                new Vector2(0f, 0f), new Vector2(0.21f, 0f), new Vector2(0.28f, 0.06f), new Vector2(0.32f, 0.18f),
                new Vector2(0.3f, 0.32f), new Vector2(0.21f, 0.42f), new Vector2(0.14f, 0.45f),
                new Vector2(0f, 0.46f),
            };
            MeshRange body = b.Lathe(Matrix4x4.identity, profile, 14, PaletteSwatch.Sage);
            Vector3 dent = new Vector3(0.65f, 0.25f, 0.72f).normalized;
            b.Shave(body, dent, b.Support(body, dent) - 0.035f);

            b.Torus(At(0f, 0.016f, 0f), 0.205f, 0.024f, 14, 3, PaletteSwatch.Charcoal);
            b.Torus(At(0f, 0.448f, 0f), 0.14f, 0.022f, 14, 3, PaletteSwatch.Charcoal);
            b.Frustum(At(0f, 0.475f, 0f), 0.06f, 0.035f, 0.04f, 8, PaletteSwatch.Charcoal);
            b.Icosphere(At(0f, 0.52f, 0f), 0.045f, 1, PaletteSwatch.Cream);

            var spoutBase = new Vector3(0f, 0.17f, 0.27f);
            Vector3 spoutTip = spoutBase + new Vector3(0f, 0.766f, 0.643f) * 0.3f;
            b.Frustum(Along(spoutBase, spoutTip) * Matrix4x4.Rotate(Rotation(AlongZ)), 0.065f, 0.032f, 0.3f, 8,
                PaletteSwatch.Sage);
            b.Torus(At(spoutTip + new Vector3(0f, 0.005f, 0.004f), new Vector3(-40f, 0f, 0f)), 0.034f, 0.015f, 8, 3,
                PaletteSwatch.Charcoal);
            b.Torus(At(new Vector3(0f, 0.25f, -0.3f), AlongX), 0.13f, 0.024f, 12, 5, PaletteSwatch.Charcoal);

            foreach (Vector3 chip in new[] { new Vector3(-0.27f, 0.22f, 0.15f), new Vector3(-0.12f, 0.33f, -0.24f) })
            {
                float yaw = Mathf.Atan2(chip.x, chip.z) * Mathf.Rad2Deg;
                b.Prism(At(chip * 1.02f, new Vector3(90f, yaw, 0f)), 0.035f, 0.012f, 7, PaletteSwatch.Charcoal);
            }

            return b;
        }

        /// <summary>"Wandering Gnome": sage coat, cream beard, and a red hat with a chip off its tip.</summary>
        public static LowPolyMeshBuilder GardenGnome()
        {
            var b = new LowPolyMeshBuilder(900);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Icosphere(At(new Vector3(side * 0.08f, 0.05f, 0.04f), Vector3.zero, new Vector3(0.8f, 0.55f, 1.2f)),
                    0.09f, 1, PaletteSwatch.Charcoal);
            }

            b.Frustum(At(0f, 0.27f, 0f), 0.2f, 0.14f, 0.38f, 9, PaletteSwatch.Sage);
            b.Torus(At(0f, 0.25f, 0f), 0.18f, 0.028f, 12, 4, PaletteSwatch.Charcoal);
            b.Box(At(0f, 0.25f, 0.2f), new Vector3(0.07f, 0.06f, 0.025f), PaletteSwatch.Honey, 0.008f);
            for (int side = -1; side <= 1; side += 2)
            {
                var shoulder = new Vector3(side * 0.15f, 0.42f, 0.02f);
                var hand = new Vector3(side * 0.07f, 0.3f, 0.19f);
                RecipeKit.Rod(b, shoulder, hand, 0.045f, 7, PaletteSwatch.Sage);
                b.Icosphere(At(hand), 0.045f, 1, PaletteSwatch.Honey);
            }

            b.Icosphere(At(0f, 0.55f, 0.02f), 0.12f, 1, PaletteSwatch.Honey);
            b.Cone(At(new Vector3(0f, 0.43f, 0.1f), new Vector3(180f, 0f, 0f), new Vector3(1f, 1f, 0.7f)), 0.13f, 0.3f,
                9, PaletteSwatch.Cream);
            b.Icosphere(At(0f, 0.54f, 0.14f), 0.035f, 1, PaletteSwatch.WarmAccent);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Icosphere(At(side * 0.045f, 0.585f, 0.115f), 0.022f, 0, PaletteSwatch.Charcoal);
            }

            b.Torus(At(0f, 0.64f, 0f), 0.125f, 0.022f, 12, 4, PaletteSwatch.WarmAccent);
            MeshRange hat = b.Cone(At(new Vector3(0f, 0.84f, -0.03f), new Vector3(-12f, 0f, 0f)), 0.14f, 0.42f, 9,
                PaletteSwatch.WarmAccent);
            Vector3 chip = new Vector3(0.45f, 1f, -0.2f).normalized;
            b.Shave(hat, chip, b.Support(hat, chip) - 0.05f);
            return b;
        }
    }
}
