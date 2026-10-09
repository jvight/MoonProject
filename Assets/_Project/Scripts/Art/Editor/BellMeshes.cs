using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Bell's cabinet (docs/STORY.md: Ro's DJ machine, "a dead receiver and four camera legs"): an old caramel-wood
    /// radio with rounded edges, a cream enamel face, a half-moon amber dial under a heavy lid that reads like a
    /// lowered brow, a round speaker, a cassette door, a row of part lamps and a telescopic antenna on her right side.
    /// Under the lid: a deck of glowing valves and Ro's setlist taped inside. Built in the Body's space: origin at the
    /// centre of the cabinet's underside, +Z = the dial face, +Y up; a viewer facing the dial sees +X on their left.
    /// </summary>
    internal static class BellMeshes
    {
        public const float Width = 0.9f;
        public const float Depth = 0.5f;

        /// <summary>Top of the cabinet body, where the lid closes on it.</summary>
        public const float BodyTop = 0.68f;

        public const float LidHeight = 0.145f;

        /// <summary>Radius of the dial's half-moon window (the DialLamp).</summary>
        public const float DialRadius = 0.17f;

        /// <summary>
        /// Needle angle at rest, measured from +X towards +Y: the left end of the band seen from the front.
        /// </summary>
        public const float NeedleRestDegrees = 20f;

        /// <summary>How far the needle travels from the left end of the band (0) to its right end.</summary>
        public const float NeedleSweepDegrees = 140f;

        public const int PartLampCount = 4;

        /// <summary>Lid hinge on the top-back edge of the body; the lid lies along +Z from it.</summary>
        public static readonly Vector3 LidHinge = new Vector3(0f, BodyTop, -Depth * 0.5f);

        /// <summary>Hub of the needle at the centre of the dial's flat bottom edge (DialFace pivot).</summary>
        public static readonly Vector3 DialCentre = new Vector3(0f, DialY, FaceFront);

        /// <summary>
        /// The tuning knob's pivot in the dial's space (Knob): on its axis at the face plate, right of the dial as
        /// seen from the front and a little over the needle hub, where 07's beam taps it. +Z out; Bell turns it about
        /// local Z.
        /// </summary>
        public static readonly Vector3 KnobCentre = new Vector3(-KnobX, KnobY - DialY, FacePlate - FaceFront);

        /// <summary>Centre of the speaker cone (Speaker pivot).</summary>
        public static readonly Vector3 SpeakerCentre = new Vector3(0.2f, 0.21f, FaceFront + 0.018f);

        /// <summary>Middle of the cassette door, on its glass (TapeSlot).</summary>
        public static readonly Vector3 TapeSlot = new Vector3(-0.16f, 0.21f, FaceFront + 0.012f);

        /// <summary>Swivel at the foot of the antenna, on the cabinet's right side near the back.</summary>
        public static readonly Vector3 AntennaBase = new Vector3(-Width * 0.5f - 0.012f, 0.6f, -0.15f);

        private const float FaceFront = Depth * 0.5f + 0.012f;
        private const float FacePlate = Depth * 0.5f + 0.006f;
        private const float DialY = 0.43f;
        private const float BodyChamfer = 0.03f;
        private const float PlateHeight = 0.03f;
        private const float LampY = 0.37f;
        private const float KnobY = 0.5f;
        private const float KnobX = 0.31f;
        private const float KnobRadius = 0.07f;
        private const float KnobDepth = 0.06f;
        private const float KnobSkirt = 0.09f;
        private const float SkirtDepth = 0.008f;
        private const int ValveCount = 4;
        private const int MissingValve = 2;
        private const int ArcSegments = 12;
        private const float SpeakerRadius = 0.105f;

        /// <summary>
        /// Part lamp <paramref name="index"/> on the face under the dial: 0..2 read left to right from the front
        /// (the parts), then a gap and 3 (the tape) above the cassette door.
        /// </summary>
        public static Vector3 PartLampPosition(int index)
        {
            switch (index)
            {
                case 0:
                    return new Vector3(0.1f, LampY, FaceFront);
                case 1:
                    return new Vector3(0.04f, LampY, FaceFront);
                case 2:
                    return new Vector3(-0.02f, LampY, FaceFront);
                case 3:
                    return new Vector3(-0.11f, LampY, FaceFront);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(index), index, "Bell has part lamps 0..3.");
            }
        }

        /// <summary>
        /// The cabinet as repaired, or as found in the canyon: valves dark (one pulled), dust on whatever faces the sky
        /// in her broken <paramref name="pose"/>. The tuning knob is its own node (<see cref="Knob"/>).
        /// </summary>
        public static LowPolyMeshBuilder Body(bool broken, Quaternion pose)
        {
            var b = new LowPolyMeshBuilder(2400);
            b.Box(At(0f, PlateHeight + (BodyTop - PlateHeight) * 0.5f, 0f),
                new Vector3(Width, BodyTop - PlateHeight, Depth), PaletteSwatch.Wood, BodyChamfer);
            if (broken)
            {
                Vector3 up = Quaternion.Inverse(pose) * Vector3.up;
                b.RepaintFacing(b.RangeFrom(0), up, 0.8f, PaletteSwatch.DustLight);
            }

            b.Box(At(0f, PlateHeight * 0.5f, 0f), new Vector3(Width - 0.1f, PlateHeight, Depth - 0.1f),
                PaletteSwatch.Metal, 0.008f);
            Face(b);
            Deck(b, broken);
            Back(b);
            Matrix4x4 side = At(new Vector3(Width * 0.5f + 0.001f, 0.36f, 0.06f), new Vector3(0f, 90f, 0f));
            b.Extrude(side, Glyphs.Star(0.05f, 0.022f), 0.004f, PaletteSwatch.Honey);
            b.Box(side * At(0f, -0.12f, 0f), new Vector3(0.26f, 0.016f, 0.004f), PaletteSwatch.Cream);
            return b;
        }

        /// <summary>
        /// The heavy lid (origin on its hinge, lying along +Z): wood with a carry handle and hinge barrels, Ro's
        /// setlist and a star sticker taped on its underside for whoever opens it.
        /// </summary>
        public static LowPolyMeshBuilder Lid()
        {
            var b = new LowPolyMeshBuilder(600);
            b.Box(At(0f, LidHeight * 0.5f, Depth * 0.5f), new Vector3(Width, LidHeight, Depth), PaletteSwatch.Wood,
                BodyChamfer);
            b.Box(At(0f, LidHeight * 0.5f, Depth + 0.001f), new Vector3(Width - 0.12f, 0.018f, 0.004f),
                PaletteSwatch.Honey);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Prism(At(new Vector3(side * 0.28f, 0.005f, -0.006f), AlongX), 0.013f, 0.12f, 6,
                    PaletteSwatch.Charcoal);
                RecipeKit.Rod(b, new Vector3(side * 0.13f, LidHeight - 0.005f, 0.25f),
                    new Vector3(side * 0.11f, LidHeight + 0.045f, 0.25f), 0.012f, 6, PaletteSwatch.Charcoal);
            }

            var grip = new Vector3(0.12f, LidHeight + 0.045f, 0.25f);
            RecipeKit.Rod(b, Vector3.Scale(grip, new Vector3(-1f, 1f, 1f)), grip, 0.016f, 6, PaletteSwatch.Charcoal);

            Matrix4x4 paper = At(new Vector3(0.12f, -0.002f, 0.24f), new Vector3(0f, 8f, 0f));
            b.Box(paper, new Vector3(0.3f, 0.004f, 0.34f), PaletteSwatch.Cream);
            for (int line = 0; line < 6; line++)
            {
                float length = 0.2f - (line % 3) * 0.04f;
                b.Box(paper * At(0.1f - length * 0.5f - 0.02f, -0.0025f, -0.12f + line * 0.05f),
                    new Vector3(length, 0.002f, 0.012f), PaletteSwatch.Charcoal);
            }

            b.Extrude(At(new Vector3(-0.22f, -0.003f, 0.2f), new Vector3(90f, 0f, 0f)), Glyphs.Star(0.055f, 0.024f),
                0.004f, PaletteSwatch.WarmAccent);
            return b;
        }

        /// <summary>The dial frame (origin on the needle hub, +Z out): a brass half-moon bezel, the scale.</summary>
        public static LowPolyMeshBuilder DialFace()
        {
            var b = new LowPolyMeshBuilder(500);
            const float rim = 0.024f;
            var arch = new Vector2[(ArcSegments + 1) * 2];
            for (int i = 0; i <= ArcSegments; i++)
            {
                float angle = Mathf.PI * i / ArcSegments;
                arch[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (DialRadius + rim);
                arch[arch.Length - 1 - i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * DialRadius;
            }

            b.Extrude(At(0f, 0f, 0.004f), arch, 0.018f, PaletteSwatch.Honey);
            b.Box(At(0f, -rim * 0.5f, 0.004f), new Vector3((DialRadius + rim) * 2f, rim, 0.018f), PaletteSwatch.Honey);
            for (int tick = 0; tick <= 14; tick++)
            {
                float degrees = NeedleRestDegrees + tick * NeedleSweepDegrees / 14f;
                bool station = tick % 7 == 0;
                float length = station ? 0.032f : 0.016f;
                float radius = DialRadius - 0.012f - length * 0.5f;
                Vector3 direction = Rotation(new Vector3(0f, 0f, degrees)) * Vector3.right;
                b.Box(At(direction * radius + Vector3.forward * 0.001f, new Vector3(0f, 0f, degrees - 90f)),
                    new Vector3(station ? 0.008f : 0.004f, length, 0.002f), PaletteSwatch.Charcoal);
            }

            return b;
        }

        /// <summary>The glass behind the scale (origin on the hub): dark until gameplay lights it amber.</summary>
        public static LowPolyMeshBuilder DialLamp()
        {
            var b = new LowPolyMeshBuilder(60);
            var disc = new Vector2[ArcSegments + 1];
            for (int i = 0; i <= ArcSegments; i++)
            {
                float angle = Mathf.PI * i / ArcSegments;
                disc[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (DialRadius + 0.002f);
            }

            b.Extrude(At(0f, 0f, -0.003f), disc, 0.006f, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>
        /// The tuning knob (Knob; origin on its axis at the face plate, +Z out): a chunky ridged bakelite knob on a
        /// cream scale skirt, a brass cap and an orange pointer, straight up at rest, that shows every turn.
        /// </summary>
        public static LowPolyMeshBuilder Knob()
        {
            var b = new LowPolyMeshBuilder(500);
            Matrix4x4 axis = Matrix4x4.Rotate(Rotation(AlongZ));
            Skirt(b, axis);
            float middle = SkirtDepth + KnobDepth * 0.5f;
            b.Frustum(axis * At(0f, middle, 0f), KnobRadius, KnobRadius * 0.85f, KnobDepth, 12, PaletteSwatch.Charcoal);
            for (int ridge = 0; ridge < 12; ridge++)
            {
                Matrix4x4 turn = Matrix4x4.Rotate(Rotation(new Vector3(0f, ridge * 30f + 15f, 0f)));
                b.Box(axis * turn * At(new Vector3(0f, middle, KnobRadius * 0.925f), new Vector3(-8f, 0f, 0f)),
                    new Vector3(0.016f, KnobDepth * 0.95f, 0.016f), PaletteSwatch.Charcoal, 0.003f);
            }

            float top = SkirtDepth + KnobDepth;
            b.Prism(axis * At(0f, top + 0.004f, 0f), KnobRadius * 0.6f, 0.008f, 12, PaletteSwatch.Honey);
            b.Box(axis * At(0f, top + 0.009f, -KnobRadius * 0.4f), new Vector3(0.014f, 0.004f, KnobRadius * 0.75f),
                PaletteSwatch.WarmAccent);
            return b;
        }

        /// <summary>
        /// The broken cabinet's Knob: the knob itself long lost (it is a pickup now), a bare shaft on its skirt.
        /// </summary>
        public static LowPolyMeshBuilder KnobShaft()
        {
            var b = new LowPolyMeshBuilder(120);
            Matrix4x4 axis = Matrix4x4.Rotate(Rotation(AlongZ));
            Skirt(b, axis);
            b.Prism(axis * At(0f, SkirtDepth + 0.018f, 0f), 0.01f, 0.036f, 6, PaletteSwatch.Metal);
            return b;
        }

        /// <summary>The tuning knob's cream scale skirt with its ticks (in the knob's axis frame, +Y out).</summary>
        private static void Skirt(LowPolyMeshBuilder b, Matrix4x4 axis)
        {
            b.Prism(axis * At(0f, SkirtDepth * 0.5f, 0f), KnobSkirt, SkirtDepth, 16, PaletteSwatch.Cream);
            for (int tick = 0; tick < 10; tick++)
            {
                Matrix4x4 turn = Matrix4x4.Rotate(Rotation(new Vector3(0f, tick * 36f, 0f)));
                float length = tick % 5 == 0 ? 0.014f : 0.008f;
                b.Box(axis * turn * At(0f, SkirtDepth + 0.001f, KnobSkirt - 0.003f - length * 0.5f),
                    new Vector3(0.006f, 0.003f, length), PaletteSwatch.Charcoal);
            }
        }

        /// <summary>The needle at rest (origin on the hub): at the left end of the band, an orange tip.</summary>
        public static LowPolyMeshBuilder Needle()
        {
            var b = new LowPolyMeshBuilder(80);
            Matrix4x4 along = Matrix4x4.Rotate(Rotation(new Vector3(0f, 0f, NeedleRestDegrees)));
            b.Box(along * At(0.065f, 0f, 0.006f), new Vector3(0.13f, 0.007f, 0.004f), PaletteSwatch.Charcoal);
            b.Box(along * At(0.145f, 0f, 0.006f), new Vector3(0.03f, 0.007f, 0.004f), PaletteSwatch.WarmAccent);
            b.Prism(At(new Vector3(0f, 0f, 0.008f), AlongZ), 0.016f, 0.008f, 8, PaletteSwatch.Metal);
            return b;
        }

        /// <summary>Speaker (origin on the cone centre, +Z out): grille ring and bars over a paper cone.</summary>
        public static LowPolyMeshBuilder Speaker(bool broken)
        {
            var b = new LowPolyMeshBuilder(400);
            b.Torus(At(new Vector3(0f, 0f, 0.004f), AlongZ), SpeakerRadius, 0.009f, 16, 4, PaletteSwatch.Metal);
            if (!broken)
            {
                Vector2[] cone =
                {
                    new Vector2(SpeakerRadius, 0f), new Vector2(0.065f, -0.008f), new Vector2(0.028f, -0.014f),
                    new Vector2(0f, -0.015f),
                };
                b.Lathe(At(Vector3.zero, AlongZ), cone, 14, PaletteSwatch.Cream);
                b.Icosphere(At(new Vector3(0f, 0f, -0.012f), Vector3.zero, new Vector3(1f, 1f, 0.45f)), 0.028f, 1,
                    PaletteSwatch.Charcoal);
            }

            for (int bar = -1; bar <= 1; bar++)
            {
                float y = bar * 0.048f;
                float half = Mathf.Sqrt(SpeakerRadius * SpeakerRadius - y * y);
                bool bent = broken && bar == 1;
                Matrix4x4 at = bent
                    ? At(new Vector3(-half * 0.45f, y - 0.03f, 0.012f), new Vector3(0f, 0f, 28f))
                    : At(0f, y, 0.008f);
                b.Box(at, new Vector3(bent ? half : half * 2f, 0.008f, 0.006f), PaletteSwatch.Metal);
            }

            return b;
        }

        /// <summary>Telescopic antenna (origin on its swivel): three chrome sections, an orange tip.</summary>
        public static LowPolyMeshBuilder Antenna()
        {
            return Whip(new[]
            {
                new Vector3(-0.07f, 0.2f, -0.03f), new Vector3(-0.14f, 0.38f, -0.065f),
                new Vector3(-0.2f, 0.54f, -0.095f),
            });
        }

        /// <summary>The antenna after the fall: pushed half shut and folded over at its second joint.</summary>
        public static LowPolyMeshBuilder BentAntenna()
        {
            return Whip(new[]
            {
                new Vector3(-0.06f, 0.18f, -0.03f), new Vector3(-0.2f, 0.22f, -0.06f),
                new Vector3(-0.29f, 0.12f, -0.07f),
            });
        }

        /// <summary>A round part lamp (origin on the lamp): dark glass the parts readout lights amber.</summary>
        public static LowPolyMeshBuilder PartLamp()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Icosphere(At(Vector3.zero, Vector3.zero, new Vector3(1f, 1f, 0.6f)), 0.019f, 1, PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>The tape lamp: a little cassette window, so the fourth "part" reads apart.</summary>
        public static LowPolyMeshBuilder TapeLamp()
        {
            var b = new LowPolyMeshBuilder(40);
            b.Box(Matrix4x4.identity, new Vector3(0.05f, 0.026f, 0.014f), PaletteSwatch.LampGlass, 0.004f);
            return b;
        }

        /// <summary>The cream enamel face: speaker well, cassette door, volume knob, the part lamps' bezels.</summary>
        private static void Face(LowPolyMeshBuilder b)
        {
            b.Box(At(0f, 0.37f, FacePlate - 0.003f), new Vector3(Width - 0.1f, 0.56f, 0.012f), PaletteSwatch.Enamel,
                0.004f);
            b.Box(At(0f, 0.088f, FacePlate), new Vector3(Width - 0.1f, 0.01f, 0.006f), PaletteSwatch.Honey);

            Vector3 well = SpeakerCentre;
            b.Prism(At(new Vector3(well.x, well.y, FacePlate + 0.001f), AlongZ), SpeakerRadius + 0.004f, 0.006f, 16,
                PaletteSwatch.Charcoal);

            Vector3 door = TapeSlot;
            b.Box(At(door.x, door.y, FacePlate + 0.002f), new Vector3(0.3f, 0.125f, 0.01f), PaletteSwatch.Metal,
                0.004f);
            b.Box(At(door.x, door.y + 0.005f, FacePlate + 0.006f), new Vector3(0.25f, 0.075f, 0.006f),
                PaletteSwatch.SkyHorizon);
            b.Box(At(door.x + 0.04f, door.y - 0.048f, FacePlate + 0.008f), new Vector3(0.05f, 0.016f, 0.012f),
                PaletteSwatch.Honey, 0.003f);
            for (int reel = -1; reel <= 1; reel += 2)
            {
                b.Prism(At(new Vector3(door.x + reel * 0.055f, door.y + 0.005f, FacePlate + 0.01f), AlongZ), 0.018f,
                    0.004f, 6, PaletteSwatch.Cream);
            }

            for (int i = 0; i < PartLampCount; i++)
            {
                Vector3 lamp = PartLampPosition(i);
                if (i == PartLampCount - 1)
                {
                    b.Box(At(lamp.x, lamp.y, FacePlate), new Vector3(0.064f, 0.04f, 0.008f), PaletteSwatch.Charcoal,
                        0.003f);
                }
                else
                {
                    b.Prism(At(new Vector3(lamp.x, lamp.y, FacePlate), AlongZ), 0.027f, 0.008f, 8,
                        PaletteSwatch.Charcoal);
                }
            }

            VolumeKnob(b, new Vector3(KnobX, KnobY, FacePlate), 0.042f);
        }

        /// <summary>A bakelite knob with a cream pointer, or just its bare shaft once the knob is lost.</summary>
        private static void VolumeKnob(LowPolyMeshBuilder b, Vector3 at, float radius)
        {
            b.Prism(At(at + Vector3.forward * 0.002f, AlongZ), radius + 0.012f, 0.004f, 12, PaletteSwatch.Metal);
            b.Frustum(At(at + Vector3.forward * 0.024f, AlongZ), radius, radius * 0.82f, 0.04f, 12,
                PaletteSwatch.Charcoal);
            b.Box(At(at + new Vector3(0f, radius * 0.45f, 0.045f)), new Vector3(0.008f, radius * 0.7f, 0.004f),
                PaletteSwatch.Cream);
        }

        /// <summary>
        /// Under the lid: a charcoal deck with a row of valves (glowing amber, or dark glass with one pulled out),
        /// a transformer and two loops of coloured wire.
        /// </summary>
        private static void Deck(LowPolyMeshBuilder b, bool broken)
        {
            b.Box(At(0f, BodyTop + 0.003f, 0f), new Vector3(Width - 0.08f, 0.006f, Depth - 0.08f),
                PaletteSwatch.Charcoal);
            Vector2[] valve =
            {
                new Vector2(0f, 0f), new Vector2(0.024f, 0.002f), new Vector2(0.026f, 0.06f),
                new Vector2(0.018f, 0.078f), new Vector2(0f, 0.084f),
            };
            for (int i = 0; i < ValveCount; i++)
            {
                var foot = new Vector3(0.27f - i * 0.12f, BodyTop + 0.006f, -0.1f);
                b.Prism(At(foot + Vector3.up * 0.008f), 0.03f, 0.016f, 8, PaletteSwatch.Metal);
                if (broken && i == MissingValve)
                {
                    b.Prism(At(foot + Vector3.up * 0.017f), 0.018f, 0.004f, 8, PaletteSwatch.SkyTop);
                    continue;
                }

                b.Lathe(At(foot + Vector3.up * 0.014f), valve, 8,
                    broken ? PaletteSwatch.SkyHorizon : PaletteSwatch.LampGlass);
            }

            b.Box(At(-0.28f, BodyTop + 0.04f, 0.1f), new Vector3(0.14f, 0.07f, 0.1f), PaletteSwatch.Metal, 0.008f);
            b.Box(At(-0.28f, BodyTop + 0.078f, 0.1f), new Vector3(0.1f, 0.008f, 0.06f), PaletteSwatch.Honey);
            RecipeKit.Rod(b, new Vector3(-0.2f, BodyTop + 0.03f, 0.1f), new Vector3(0.05f, BodyTop + 0.02f, 0.06f),
                0.007f, 5, PaletteSwatch.WarmAccent);
            RecipeKit.Rod(b, new Vector3(0.05f, BodyTop + 0.02f, 0.06f), new Vector3(0.21f, BodyTop + 0.03f, -0.05f),
                0.007f, 5, PaletteSwatch.WarmAccent);
            RecipeKit.Rod(b, new Vector3(-0.2f, BodyTop + 0.05f, 0.13f), new Vector3(0.15f, BodyTop + 0.012f, 0.14f),
                0.006f, 5, PaletteSwatch.Sage);
        }

        /// <summary>The back: a charcoal vent panel and the stub of the old mains cable.</summary>
        private static void Back(LowPolyMeshBuilder b)
        {
            const float back = -Depth * 0.5f - 0.004f;
            b.Box(At(0f, 0.36f, back), new Vector3(0.6f, 0.4f, 0.008f), PaletteSwatch.Charcoal);
            for (int slot = 0; slot < 5; slot++)
            {
                b.Box(At(0f, 0.22f + slot * 0.07f, back - 0.005f), new Vector3(0.5f, 0.016f, 0.004f),
                    PaletteSwatch.Metal);
            }

            RecipeKit.Rod(b, new Vector3(0.22f, 0.12f, back), new Vector3(0.24f, 0.05f, back - 0.08f), 0.012f, 6,
                PaletteSwatch.Charcoal);
        }

        private static LowPolyMeshBuilder Whip(Vector3[] joints)
        {
            var b = new LowPolyMeshBuilder(160);
            b.Prism(At(Vector3.zero, AlongX), 0.024f, 0.03f, 8, PaletteSwatch.Charcoal);
            float radius = 0.011f;
            Vector3 from = Vector3.zero;
            foreach (Vector3 joint in joints)
            {
                RecipeKit.Rod(b, from, joint, radius, 6, PaletteSwatch.Metal);
                Vector3 along = (joint - from).normalized * 0.007f;
                RecipeKit.Rod(b, joint - along, joint + along, radius + 0.003f, 6, PaletteSwatch.Charcoal);
                from = joint;
                radius -= 0.0025f;
            }

            b.Icosphere(At(from), 0.016f, 1, PaletteSwatch.WarmAccent);
            return b;
        }
    }
}
