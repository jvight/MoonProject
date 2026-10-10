using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Kenji's blueprints (M3-16, "the bench's purpose"): his old human bench's pegboard becomes the board of kits the
    /// bay can build, one pinned sheet per kit over an empty pin, each a slate-blue sheet with the kit drawn in chalky
    /// cream lines (no words: the world carries no text to translate). Plus the blueprint tube 07 finds in a wreck's
    /// heart. Sheets are in their own space (centre at the origin, facing +Z); slots and pins in the bench's space.
    /// </summary>
    internal static class BlueprintMeshes
    {
        /// <summary>
        /// The kits with a sheet, in the order of their slots from the left, named as Core's RoverAbility members (Art
        /// does not reference Core; Gameplay maps the names).
        /// </summary>
        public static readonly string[] Kits =
        {
            "HoverJump", "CargoCradle", "WarmHeadlamp", "BoostCoils", "WideSonar",
        };

        private const float SheetWidth = 0.3f;
        private const float SheetHeight = 0.36f;
        private const float SheetThickness = 0.004f;
        private const float Line = 0.008f;
        private const float SlotSpacing = 0.4f;
        private const float SlotHeight = 1.78f;

        // The pegboard's face in bench space (WorkbenchMeshes), and how far a sheet stands proud of it.
        private const float BoardFace = -0.38f;
        private const float SheetProud = 0.006f;

        private const float TubeRadius = 0.045f;
        private const float TubeLength = 0.42f;

        /// <summary>Where slot <paramref name="index"/>'s sheet hangs on the pegboard, in bench space.</summary>
        public static Vector3 Slot(int index)
        {
            return new Vector3((index - (Kits.Length - 1) * 0.5f) * SlotSpacing, SlotHeight, BoardFace + SheetProud);
        }

        /// <summary>The empty pins waiting on the pegboard, one at every slot's top (bench space).</summary>
        public static LowPolyMeshBuilder Pins()
        {
            var b = new LowPolyMeshBuilder(120);
            for (int i = 0; i < Kits.Length; i++)
            {
                Pin(b, Slot(i) + new Vector3(0f, SheetHeight * 0.5f - 0.03f, -SheetProud));
            }

            return b;
        }

        /// <summary>
        /// One blueprint sheet: slate-blue paper with a chalky border, the kit drawn in cream lines, and its pin.
        /// </summary>
        public static LowPolyMeshBuilder Sheet(string kit)
        {
            var b = new LowPolyMeshBuilder(300);
            b.Box(Matrix4x4.identity, new Vector3(SheetWidth, SheetHeight, SheetThickness), PaletteSwatch.DustShadow);
            float x = SheetWidth * 0.5f - 0.02f;
            float y = SheetHeight * 0.5f - 0.02f;
            Draw(b, new Vector2(-x, -y), new Vector2(x, -y));
            Draw(b, new Vector2(x, -y), new Vector2(x, y));
            Draw(b, new Vector2(x, y), new Vector2(-x, y));
            Draw(b, new Vector2(-x, y), new Vector2(-x, -y));
            switch (kit)
            {
                case "HoverJump":
                    Body(b);
                    for (int i = 0; i < 4; i++)
                    {
                        Spring(b, -0.075f + i * 0.05f);
                    }

                    break;
                case "CargoCradle":
                    Body(b);
                    Box(b, new Vector2(-0.1f, -0.02f), new Vector2(-0.035f, 0.04f));
                    Draw(b, new Vector2(-0.1f, 0.01f), new Vector2(-0.035f, 0.01f));
                    break;
                case "WarmHeadlamp":
                    Draw(b, new Vector2(-0.1f, -0.02f), new Vector2(0.1f, -0.02f));
                    for (int i = -1; i <= 1; i++)
                    {
                        Ring(b, new Vector2(i * 0.075f, 0.025f), 0.03f);
                    }

                    break;
                case "BoostCoils":
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Box(b, new Vector2(-0.1f, i * 0.045f - 0.025f), new Vector2(0.1f, i * 0.045f + 0.025f));
                        Draw(b, new Vector2(0f, i * 0.045f - 0.025f), new Vector2(0f, i * 0.045f + 0.025f));
                    }

                    break;
                default:
                    Ring(b, new Vector2(0f, 0.04f), 0.07f);
                    Draw(b, new Vector2(0f, 0.04f), new Vector2(0f, -0.1f));
                    Draw(b, new Vector2(-0.06f, -0.1f), new Vector2(0.06f, -0.1f));
                    break;
            }

            Pin(b, new Vector3(0f, SheetHeight * 0.5f - 0.03f, SheetThickness * 0.5f));
            return b;
        }

        /// <summary>
        /// The blueprint tube a wreck's heart holds: a worn sage tube with metal caps, an orange band and a rusted
        /// cap rim, centred on its mass.
        /// </summary>
        public static LowPolyMeshBuilder Tube()
        {
            var b = new LowPolyMeshBuilder(200);
            b.Prism(At(Vector3.zero, AlongX), TubeRadius, TubeLength, 10, PaletteSwatch.Sage);
            for (int end = -1; end <= 1; end += 2)
            {
                b.Prism(At(new Vector3(end * (TubeLength * 0.5f + 0.012f), 0f, 0f), AlongX), TubeRadius + 0.006f,
                    0.03f, 10, PaletteSwatch.Metal);
            }

            b.Prism(At(new Vector3(0.06f, 0f, 0f), AlongX), TubeRadius + 0.002f, 0.05f, 10, PaletteSwatch.WarmAccent,
                false);
            b.Prism(At(new Vector3(-TubeLength * 0.5f + 0.004f, 0f, 0f), AlongX), TubeRadius + 0.008f, 0.008f, 10,
                PaletteSwatch.Rust, false);
            return RecipeKit.CentredOnMass(b);
        }

        /// <summary>07 seen from the side, small: a body and three wheels.</summary>
        private static void Body(LowPolyMeshBuilder b)
        {
            Box(b, new Vector2(-0.07f, 0f), new Vector2(0.07f, 0.06f));
            for (int i = -1; i <= 1; i++)
            {
                Ring(b, new Vector2(i * 0.05f, -0.03f), 0.022f);
            }
        }

        private static void Spring(LowPolyMeshBuilder b, float x)
        {
            for (int k = 0; k < 3; k++)
            {
                float top = -0.06f - k * 0.02f;
                Draw(b, new Vector2(x - 0.01f, top), new Vector2(x + 0.01f, top - 0.01f));
                Draw(b, new Vector2(x + 0.01f, top - 0.01f), new Vector2(x - 0.01f, top - 0.02f));
            }
        }

        private static void Box(LowPolyMeshBuilder b, Vector2 low, Vector2 high)
        {
            Draw(b, low, new Vector2(high.x, low.y));
            Draw(b, new Vector2(high.x, low.y), high);
            Draw(b, high, new Vector2(low.x, high.y));
            Draw(b, new Vector2(low.x, high.y), low);
        }

        private static void Ring(LowPolyMeshBuilder b, Vector2 centre, float radius)
        {
            b.Torus(At(new Vector3(centre.x, centre.y, SheetThickness * 0.5f), AlongZ), radius, Line * 0.5f, 10, 3,
                PaletteSwatch.Cream);
        }

        /// <summary>A chalky cream line on the sheet's face.</summary>
        private static void Draw(LowPolyMeshBuilder b, Vector2 from, Vector2 to)
        {
            var a = new Vector3(from.x, from.y, SheetThickness * 0.5f);
            var c = new Vector3(to.x, to.y, SheetThickness * 0.5f);
            b.Box(Along(a, c), new Vector3(Line, Line, Vector3.Distance(a, c) + Line), PaletteSwatch.Cream);
        }

        private static void Pin(LowPolyMeshBuilder b, Vector3 at)
        {
            b.Prism(At(at + Vector3.forward * 0.006f, AlongZ), 0.004f, 0.012f, 6, PaletteSwatch.Metal);
            b.Prism(At(at + Vector3.forward * 0.014f, AlongZ), 0.012f, 0.008f, 8, PaletteSwatch.WarmAccent);
        }
    }
}
