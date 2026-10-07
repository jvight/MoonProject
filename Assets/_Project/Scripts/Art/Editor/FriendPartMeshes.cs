using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Friends' missing parts as pickups (0.25-0.4 m): Tilly's rotor, lens and cell, Bell's tuning knob, speaker
    /// cone and valve. Each glints warm amber (WarmLamp glow, Honey tag) so it reads apart from the cyan scrap.
    /// Built upright on y = 0; the builders pivot them at the centre of mass.
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

        /// <summary>Bell's tuning knob: knurled bakelite on a cream scale skirt, an amber ring, a brass cap.</summary>
        public static LowPolyMeshBuilder BellKnob()
        {
            var b = new LowPolyMeshBuilder(500);
            Matrix4x4 frame = At(new Vector3(0f, 0.15f, 0f), new Vector3(68f, 0f, 0f));
            b.Prism(frame * At(0f, -0.045f, 0f), 0.02f, 0.05f, 8, PaletteSwatch.Metal);
            b.Prism(frame * At(0f, -0.01f, 0f), 0.135f, 0.02f, 16, PaletteSwatch.Cream);
            b.Torus(frame * At(0f, -0.01f, 0f), 0.138f, 0.009f, 16, 3, PaletteSwatch.WarmLamp);
            for (int tick = 0; tick < 12; tick++)
            {
                Matrix4x4 turn = Matrix4x4.Rotate(Rotation(new Vector3(0f, tick * 30f, 0f)));
                float length = tick % 3 == 0 ? 0.03f : 0.016f;
                b.Box(frame * turn * At(0f, 0.002f, 0.115f), new Vector3(0.008f, 0.006f, length),
                    PaletteSwatch.Charcoal);
            }

            b.Frustum(frame * At(0f, 0.04f, 0f), 0.1f, 0.084f, 0.08f, 12, PaletteSwatch.Charcoal);
            for (int ridge = 0; ridge < 12; ridge++)
            {
                Matrix4x4 turn = Matrix4x4.Rotate(Rotation(new Vector3(0f, ridge * 30f + 15f, 0f)));
                b.Box(frame * turn * At(new Vector3(0f, 0.04f, 0.092f), new Vector3(-11f, 0f, 0f)),
                    new Vector3(0.022f, 0.078f, 0.02f), PaletteSwatch.Charcoal, 0.004f);
            }

            b.Prism(frame * At(0f, 0.084f, 0f), 0.07f, 0.01f, 12, PaletteSwatch.Honey);
            b.Box(frame * At(0f, 0.09f, 0.035f), new Vector3(0.014f, 0.006f, 0.06f), PaletteSwatch.WarmAccent);
            HoneyTag(b, frame * At(new Vector3(-0.13f, -0.03f, 0f), new Vector3(0f, 0f, 90f)));
            return b;
        }

        /// <summary>Bell's speaker cone: cream paper in a chrome basket, a charcoal magnet, an amber cap.</summary>
        public static LowPolyMeshBuilder BellCone()
        {
            var b = new LowPolyMeshBuilder(500);
            Vector2[] paper =
            {
                new Vector2(0f, 0f), new Vector2(0.04f, 0f), new Vector2(0.08f, 0.035f), new Vector2(0.145f, 0.09f),
                new Vector2(0.152f, 0.1f), new Vector2(0.138f, 0.1f), new Vector2(0.075f, 0.048f),
                new Vector2(0.03f, 0.022f), new Vector2(0f, 0.022f),
            };
            PaletteSwatch[] bands =
            {
                PaletteSwatch.Charcoal, PaletteSwatch.Charcoal, PaletteSwatch.Charcoal, PaletteSwatch.Metal,
                PaletteSwatch.Metal, PaletteSwatch.Cream, PaletteSwatch.Cream, PaletteSwatch.Cream,
            };
            Matrix4x4 frame = At(new Vector3(0f, 0.12f, 0f), new Vector3(58f, 0f, 0f));
            b.Lathe(frame * At(0f, -0.04f, 0f), paper, 14, bands);
            b.Torus(frame * At(0f, 0.062f, 0f), 0.15f, 0.011f, 14, 3, PaletteSwatch.WarmLamp);
            b.Icosphere(frame * At(Vector3.zero, Vector3.zero, new Vector3(1f, 0.5f, 1f)), 0.04f, 1,
                PaletteSwatch.WarmLamp);
            b.Prism(frame * At(0f, -0.065f, 0f), 0.055f, 0.05f, 10, PaletteSwatch.Charcoal);
            b.Prism(frame * At(0f, -0.094f, 0f), 0.04f, 0.008f, 10, PaletteSwatch.Metal);
            for (int strut = 0; strut < 4; strut++)
            {
                Matrix4x4 turn = Matrix4x4.Rotate(Rotation(new Vector3(0f, strut * 90f + 45f, 0f)));
                Vector3 rim = turn.MultiplyPoint3x4(new Vector3(0f, 0.058f, 0.15f));
                Vector3 magnet = turn.MultiplyPoint3x4(new Vector3(0f, -0.06f, 0.05f));
                b.Box(frame * Along(rim, magnet), new Vector3(0.02f, 0.008f, Vector3.Distance(rim, magnet)),
                    PaletteSwatch.Metal);
            }

            HoneyTag(b, frame * At(new Vector3(0f, -0.1f, -0.05f), new Vector3(180f, 0f, 0f)));
            return b;
        }

        /// <summary>
        /// A spare valve for Bell's deck: amber-glowing glass under a silvered getter dome, on a charcoal octal socket
        /// with pins.
        /// </summary>
        public static LowPolyMeshBuilder BellValve()
        {
            var b = new LowPolyMeshBuilder(400);
            Vector2[] glass =
            {
                new Vector2(0f, 0.05f), new Vector2(0.046f, 0.052f), new Vector2(0.052f, 0.09f),
                new Vector2(0.052f, 0.22f), new Vector2(0.044f, 0.26f), new Vector2(0.028f, 0.282f),
                new Vector2(0.01f, 0.29f), new Vector2(0f, 0.29f),
            };
            b.Prism(At(0f, 0.024f, 0f), 0.068f, 0.036f, 8, PaletteSwatch.Charcoal);
            b.Prism(At(0f, 0.048f, 0f), 0.056f, 0.014f, 12, PaletteSwatch.Metal);
            PaletteSwatch[] bands =
            {
                PaletteSwatch.LampGlass, PaletteSwatch.LampGlass, PaletteSwatch.LampGlass, PaletteSwatch.Metal,
                PaletteSwatch.Metal, PaletteSwatch.Metal, PaletteSwatch.Metal,
            };
            b.Lathe(Matrix4x4.identity, glass, 12, bands);
            b.Torus(At(0f, 0.075f, 0f), 0.053f, 0.008f, 12, 3, PaletteSwatch.WarmLamp);
            b.Cone(At(0f, 0.3f, 0f), 0.012f, 0.025f, 6, PaletteSwatch.Metal);
            for (int pin = 0; pin < 4; pin++)
            {
                float angle = (pin * 90f + 45f) * Mathf.Deg2Rad;
                b.Prism(At(0.035f * Mathf.Sin(angle), 0.006f, 0.035f * Mathf.Cos(angle)), 0.006f, 0.012f, 5,
                    PaletteSwatch.Metal);
            }

            HoneyTag(b, At(new Vector3(0.07f, 0.02f, 0f), new Vector3(0f, 0f, -90f)));
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
