using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor.Concept
{
    /// <summary>
    /// What each concept robot is shown working on (preview only): a dusty solar panel with the mites' wiped lanes,
    /// a sweeper's clean swath, a trench, a rock wall with a crew cable, a frost sheet and a cave wall.
    /// </summary>
    internal static class ConceptStageMeshes
    {
        /// <summary>
        /// The panel's tilt about X (degrees) and its centre's height: the mites crawl on its top face.
        /// </summary>
        public const float PanelTilt = 24f;

        public static readonly Vector3 PanelCentre = new Vector3(0f, 0.75f, 0f);

        public static readonly Vector2 PanelSize = new Vector2(2.4f, 1.3f);

        /// <summary>
        /// An old crew solar panel on two legs, tipped toward the camera: violet cells under caked dust, with three
        /// clean lanes the mites have wiped.
        /// </summary>
        public static LowPolyMeshBuilder SolarPanel()
        {
            var b = new LowPolyMeshBuilder(1200);
            Matrix4x4 panel = At(PanelCentre, new Vector3(PanelTilt, 0f, 0f));
            b.Box(panel, new Vector3(PanelSize.x + 0.06f, 0.05f, PanelSize.y + 0.06f), PaletteSwatch.Metal, 0.01f);
            b.Box(panel * At(0f, 0.028f, 0f), new Vector3(PanelSize.x, 0.01f, PanelSize.y), PaletteSwatch.SkyHorizon);
            for (int i = 0; i < 6; i++)
            {
                float x = -PanelSize.x * 0.5f + 0.2f + i * 0.4f;
                if (i == 1 || i == 3 || i == 4)
                {
                    continue;
                }

                b.Box(panel * At(x, 0.036f, 0f), new Vector3(0.38f, 0.008f, PanelSize.y - 0.04f),
                    PaletteSwatch.CakedDust);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                SiteKit.Bar(b, new Vector3(side * 0.9f, 0f, 0.1f), panel.MultiplyPoint3x4(new Vector3(side * 0.9f, 0f,
                    0f)), 0.06f, PaletteSwatch.Metal);
            }

            return b;
        }

        /// <summary>
        /// Where mite <paramref name="index"/> of <paramref name="count"/> crawls (panel top, world).
        /// </summary>
        public static Vector3 MiteSpot(int index, int count, out Quaternion rotation)
        {
            Matrix4x4 panel = At(PanelCentre, new Vector3(PanelTilt, 0f, 0f));
            int lane = index % 3;
            float x = -PanelSize.x * 0.5f + 0.2f + new[] { 1, 3, 4 }[lane] * 0.4f + ((index * 7) % 5 - 2) * 0.04f;
            float z = -PanelSize.y * 0.45f + (index / 3) * (PanelSize.y * 0.9f / Mathf.Max(1, count / 3));
            rotation = panel.rotation * Quaternion.Euler(0f, (index * 47) % 40 - 20f, 0f);
            return panel.MultiplyPoint3x4(new Vector3(x, 0.034f, z));
        }

        /// <summary>
        /// The sweeper's clean swath: a long, slightly curved band of darker, smoothed regolith behind it, edged with
        /// low windrows of swept dust (the old crew road showing through).
        /// </summary>
        public static LowPolyMeshBuilder Swath(float length)
        {
            var b = new LowPolyMeshBuilder(400);
            const int Steps = 8;
            for (int i = 0; i < Steps; i++)
            {
                float z0 = -i * length / Steps;
                float bend = Mathf.Sin(i * 0.35f) * 0.3f;
                b.Box(At(new Vector3(bend, 0.004f, z0 - length / Steps * 0.5f), new Vector3(0f, bend * 6f, 0f)),
                    new Vector3(1.3f, 0.006f, length / Steps + 0.05f), PaletteSwatch.DustShadow);
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Icosphere(At(new Vector3(bend + side * 0.75f, 0f, z0 - length / Steps * 0.5f), Vector3.zero,
                        new Vector3(0.25f, 0.1f, length / Steps * 1.1f)), 0.5f, 1, PaletteSwatch.DustLight);
                }
            }

            return b;
        }

        /// <summary>
        /// The trencher's trench, running back from the root along -Z: a dark smooth groove floor between two long
        /// spoil berms.
        /// </summary>
        public static LowPolyMeshBuilder Trench(float length)
        {
            var b = new LowPolyMeshBuilder(300);
            b.Box(At(0f, 0.004f, -length * 0.5f), new Vector3(0.9f, 0.006f, length), PaletteSwatch.RockDark);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Icosphere(At(new Vector3(side * 0.75f, 0f, -length * 0.5f), Vector3.zero,
                    new Vector3(0.6f, 0.45f, length)), 0.5f, 2, PaletteSwatch.DustLight);
            }

            return b;
        }

        /// <summary>
        /// A terrace wall slab (its foot on the ground, its face at Z 0 facing +Z) with a crew cable pinned down it at
        /// X 0, orange ties every half metre.
        /// </summary>
        public static LowPolyMeshBuilder CableWall()
        {
            var b = new LowPolyMeshBuilder(800);
            b.Box(At(0f, 1.6f, -0.35f), new Vector3(3.2f, 3.2f, 0.7f), PaletteSwatch.RockLight, 0.08f);
            RockGenerator.Build(b, 701, 0.8f, RockStyle.Slab, At(new Vector3(1.4f, 0f, 0.3f)));
            SiteKit.Bar(b, new Vector3(0f, 0f, 0.04f), new Vector3(0f, 3.2f, 0.04f), 0.03f, PaletteSwatch.Charcoal);
            for (int i = 1; i < 6; i++)
            {
                b.Box(At(0f, i * 0.5f, 0.03f), new Vector3(0.12f, 0.03f, 0.05f), PaletteSwatch.WarmAccent);
            }

            return b;
        }

        /// <summary>
        /// A frost sheet in permanent shadow, about 3 m: pale cracked plates with sorted fragments heaped.
        /// </summary>
        public static LowPolyMeshBuilder FrostSheet()
        {
            LowPolyMeshBuilder b = LowPolyMeshBuilder.WithVertexColours(800);
            for (int i = 0; i < 9; i++)
            {
                float x = (i % 3 - 1) * 0.95f + (i * 13 % 7 - 3) * 0.03f;
                float z = (i / 3 - 1) * 0.85f;
                MeshRange plate = b.Box(At(new Vector3(x, 0.02f, z), new Vector3(0f, i * 17f, 0f)),
                    new Vector3(0.88f, 0.04f, 0.78f), PaletteSwatch.DustLight);
                b.Shade(plate, new Color32(190, 225, 255, 255), 0.55f);
            }

            for (int i = 0; i < 7; i++)
            {
                MeshRange chip = b.Icosphere(At(new Vector3(1.3f + (i % 3) * 0.1f, 0.05f, -0.9f + i * 0.06f),
                    new Vector3(i * 30f, i * 50f, 0f)), 0.07f, 0, PaletteSwatch.DustLight);
                b.Shade(chip, new Color32(200, 235, 255, 255), 0.6f);
            }

            return b;
        }

        /// <summary>
        /// A stretch of lava-tube wall (its face at Z 0 facing +Z): smooth dark basalt bulging out, where the echo
        /// beacons are spiked in a line along the safe path.
        /// </summary>
        public static LowPolyMeshBuilder CaveWall()
        {
            var b = new LowPolyMeshBuilder(1200);
            b.Box(At(0f, 1.4f, -0.4f), new Vector3(6f, 2.8f, 0.8f), PaletteSwatch.RockDark, 0.2f);
            b.Box(At(new Vector3(0f, 3f, -0.9f), new Vector3(-20f, 0f, 0f)), new Vector3(6.4f, 0.6f, 2f),
                PaletteSwatch.RockDark, 0.2f);
            for (int i = 0; i < 4; i++)
            {
                RockGenerator.Build(b, 720 + i, 0.5f + i * 0.15f, RockStyle.Rounded,
                    At(new Vector3(-2.6f + i * 1.7f, 0f, 0.35f)));
            }

            return b;
        }

        /// <summary>
        /// Where echo beacon <paramref name="index"/> sits on the cave wall (world), +Z out of the rock.
        /// </summary>
        public static Vector3 BeaconSpot(int index)
        {
            return new Vector3(-2f + index * 1f, 0.7f + Mathf.Sin(index * 1.3f) * 0.25f, 0.02f);
        }
    }
}
