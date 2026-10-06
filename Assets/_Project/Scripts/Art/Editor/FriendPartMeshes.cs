using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Tilly's three missing parts as pickups (0.25-0.4 m): each glints warm amber (WarmLamp glow, Honey tag) so it
    /// reads apart from the cyan scrap. Built upright on y = 0; the builder pivots them at the centre of mass.
    /// </summary>
    internal static class FriendPartMeshes
    {
        /// <summary>A spare ducted rotor: cream duct ring, motor with a glowing amber collar, two blades.</summary>
        public static LowPolyMeshBuilder TillyRotor()
        {
            var b = new LowPolyMeshBuilder(500);
            Vector2[] duct =
            {
                new Vector2(0.125f, -0.04f), new Vector2(0.145f, -0.04f), new Vector2(0.15f, 0.03f),
                new Vector2(0.125f, 0.035f), new Vector2(0.125f, -0.04f),
            };
            Matrix4x4 frame = At(new Vector3(0f, 0.16f, 0f), new Vector3(70f, 0f, 0f));
            b.Lathe(frame, duct, 14, PaletteSwatch.Enamel);
            b.Torus(frame * At(0f, 0.036f, 0f), 0.137f, 0.009f, 14, 3, PaletteSwatch.WarmLamp);
            b.Prism(frame * At(0f, -0.02f, 0f), 0.035f, 0.05f, 10, PaletteSwatch.Charcoal);
            b.Torus(frame * At(0f, -0.005f, 0f), 0.037f, 0.009f, 10, 3, PaletteSwatch.WarmLamp);
            b.Prism(frame * At(0f, 0.012f, 0f), 0.022f, 0.024f, 8, PaletteSwatch.Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(frame * At(new Vector3(side * 0.06f, 0.016f, 0f), new Vector3(side * 10f, 0f, 0f)),
                    new Vector3(0.11f, 0.006f, 0.032f), PaletteSwatch.Metal);
            }

            for (int spoke = 0; spoke < 3; spoke++)
            {
                float angle = spoke * 120f * Mathf.Deg2Rad;
                var outward = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Vector3 from = outward * 0.03f - Vector3.up * 0.025f;
                Vector3 to = outward * 0.13f - Vector3.up * 0.025f;
                b.Box(frame * Along(from, to), new Vector3(0.012f, 0.009f, Vector3.Distance(from, to)),
                    PaletteSwatch.Charcoal);
            }

            HoneyTag(b, frame * At(new Vector3(0.15f, 0f, 0f), new Vector3(0f, 0f, -90f)));
            b.Box(At(0f, 0.012f, 0.02f), new Vector3(0.12f, 0.024f, 0.16f), PaletteSwatch.Metal, 0.006f);
            return b;
        }

        /// <summary>A spare lens unit: dark glass in a charcoal barrel, metal bezel, glowing amber ring.</summary>
        public static LowPolyMeshBuilder TillyLens()
        {
            var b = new LowPolyMeshBuilder(400);
            Vector2[] glass =
            {
                new Vector2(0f, -0.006f), new Vector2(0.105f, -0.006f), new Vector2(0.09f, 0.016f),
                new Vector2(0.052f, 0.03f), new Vector2(0f, 0.035f),
            };
            Matrix4x4 frame = At(new Vector3(0f, 0.13f, 0f), new Vector3(-20f, 0f, 0f));
            b.Prism(frame * At(new Vector3(0f, 0f, -0.06f), AlongZ), 0.125f, 0.11f, 16, PaletteSwatch.Charcoal);
            b.Torus(frame * At(new Vector3(0f, 0f, 0.002f), AlongZ), 0.118f, 0.014f, 16, 4, PaletteSwatch.Metal);
            b.Torus(frame * At(new Vector3(0f, 0f, -0.045f), AlongZ), 0.128f, 0.01f, 16, 3, PaletteSwatch.WarmLamp);
            b.Lathe(frame * At(Vector3.zero, AlongZ), glass, 16, PaletteSwatch.SkyHorizon);
            b.Icosphere(frame * At(-0.035f, 0.04f, 0.03f), 0.015f, 1, PaletteSwatch.PilotLight);
            HoneyTag(b, frame * At(new Vector3(0f, -0.125f, -0.06f), new Vector3(0f, 0f, 180f)));
            return b;
        }

        /// <summary>A spare power cell on its side: cream body, honey band, metal caps, amber window.</summary>
        public static LowPolyMeshBuilder TillyCell()
        {
            var b = new LowPolyMeshBuilder(400);
            Matrix4x4 axis = At(new Vector3(0f, 0.075f, 0f), AlongX);
            b.Prism(axis, 0.075f, 0.24f, 12, PaletteSwatch.Enamel);
            b.Prism(axis * At(0f, -0.05f, 0f), 0.078f, 0.03f, 12, PaletteSwatch.Honey);
            for (int end = -1; end <= 1; end += 2)
            {
                b.Prism(axis * At(0f, end * 0.128f, 0f), 0.068f, 0.018f, 12, PaletteSwatch.Metal);
                b.Prism(axis * At(0f, end * 0.145f, 0f), 0.022f, 0.018f, 8, end > 0 ? PaletteSwatch.WarmAccent
                    : PaletteSwatch.Charcoal);
            }

            b.Box(At(0.03f, 0.075f, 0.071f), new Vector3(0.11f, 0.03f, 0.012f), PaletteSwatch.Charcoal);
            b.Box(At(0.03f, 0.075f, 0.075f), new Vector3(0.09f, 0.02f, 0.01f), PaletteSwatch.WarmLamp);
            b.Box(At(0.03f, 0.075f, -0.071f), new Vector3(0.11f, 0.03f, 0.012f), PaletteSwatch.Charcoal);
            b.Box(At(0.03f, 0.075f, -0.075f), new Vector3(0.09f, 0.02f, 0.01f), PaletteSwatch.WarmLamp);
            return b;
        }

        /// <summary>A little honey inventory tag on a short charcoal tab.</summary>
        private static void HoneyTag(LowPolyMeshBuilder b, Matrix4x4 at)
        {
            b.Box(at * At(0f, 0.015f, 0f), new Vector3(0.012f, 0.03f, 0.01f), PaletteSwatch.Charcoal);
            b.Box(at * At(0f, 0.045f, 0f), new Vector3(0.05f, 0.035f, 0.008f), PaletteSwatch.Honey, 0.003f);
        }
    }
}
