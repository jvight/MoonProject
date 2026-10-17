using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// Animated or static pose parameters for <see cref="RoverLineArtPainter.DrawRover07"/>, shared by the title screen
    /// (<see cref="TitleScenePainter"/>) and the inventory screen (<see cref="FaceInventoryView"/>).
    /// </summary>
    internal struct RoverPose
    {
        public float WheelAngle;
        public float Bob;
        public float Pitch;
        public float BumpRear;
        public float BumpFront;
        public float MoonDropAtAxle;
        public float ScoopNod;
        public float SolarTilt;
        public float AntennaSway;
        public float HeadNod;
        public bool MorseOn;
        public bool EyeBlink;
        public bool DrawGroundArc;
        public bool AlwaysDrawHeadlampBeam;
        public uint InstalledSocketsMask;

        public bool IsInstalled(RoverSocket socket)
        {
            return (InstalledSocketsMask & (1u << (int)socket)) != 0u;
        }
    }

    /// <summary>
    /// Shared 2D vector line-art painter for Rover 07 (side-3/4 profile) and CRT vector primitives. Used by both
    /// <see cref="TitleScenePainter"/> (driving across the turning Moon) and <see cref="FaceInventoryView"/> (the
    /// equipment diagram with installed kit hardware highlights and socket mount anchors). Zero per-frame allocations.
    /// </summary>
    internal static class RoverLineArtPainter
    {
        public const float RefRoverScale = 82f;

        private static readonly Vector2[] ScratchQuad = new Vector2[4];
        private static readonly Vector2[] ScratchBeam = new Vector2[4];

        // Default CRT palette matching TitleSettings and Variant A (screen-ui.md Round 2).
        public static readonly Color32 DefaultBgWarm = new Color32(28, 19, 10, 255);
        public static readonly Color32 DefaultAmber = new Color32(255, 194, 92, 255);
        public static readonly Color32 DefaultAmberHi = new Color32(255, 214, 128, 255);
        public static readonly Color32 DefaultCream = new Color32(255, 243, 204, 255);
        public static readonly Color32 DefaultAmberDim = new Color32(232, 174, 82, 255);
        public static readonly Color32 DefaultAmberFaint = new Color32(204, 150, 68, 255);
        public static readonly Color32 DefaultCyan = new Color32(95, 243, 255, 255);
        public static readonly Color32 DefaultCyanDim = new Color32(146, 240, 252, 255);

        /// <summary>
        /// Draws Rover 07 in side-3/4 profile at <paramref name="origin"/> with meter-to-pixel scale <paramref name="s"/>.
        /// Matches the title-screen 4-wheel beetle-carapace Rover 07: two near wheels at <c>±halfWheelbase</c> and two
        /// faint, non-occluding far-side wheels offset behind (<c>+6, -4</c> at <c>RefRoverScale = 82</c>).
        /// When <paramref name="pose"/> has <see cref="RoverPose.InstalledSocketsMask"/> bits set, the corresponding
        /// kit hardware on 07 is highlighted.
        /// </summary>
        public static void DrawRover07(Painter2D painter, Vector2 origin, float s, in RoverPose pose,
            TitleSettings titleSettings = null, CargoScreenSettings cargoSettings = null)
        {
            float sc = s / RefRoverScale;
            float wheelR = (cargoSettings != null
                ? cargoSettings.RoverWheelRadius
                : (titleSettings != null ? titleSettings.WheelRadius : 0.35f)) * s;
            float rimR = (cargoSettings != null
                ? cargoSettings.RoverWheelRimRadius
                : (titleSettings != null ? titleSettings.WheelRimRadius : 0.185f)) * s;
            float halfWheelbase = (cargoSettings != null
                ? cargoSettings.RoverWheelbaseHalf
                : (titleSettings != null ? titleSettings.WheelbaseHalf : 0.53f)) * s;
            float farScale = cargoSettings != null
                ? cargoSettings.RoverFarWheelScale
                : (titleSettings != null ? titleSettings.FarWheelScale : 0.94f);
            float farAlpha = cargoSettings != null
                ? Mathf.Max(cargoSettings.RoverFarWheelAlpha, 0.85f)
                : (titleSettings != null ? titleSettings.FarWheelAlpha : 0.42f);
            Vector2 farOffsetRef = cargoSettings != null
                ? cargoSettings.RoverFarWheelOffset
                : new Vector2(6f, -4f);
            int lugCount = cargoSettings != null
                ? cargoSettings.RoverWheelLugCount
                : (titleSettings != null ? titleSettings.WheelLugCount : 12);
            int spokeCount = cargoSettings != null
                ? cargoSettings.RoverWheelSpokeCount
                : (titleSettings != null ? titleSettings.WheelSpokeCount : 6);
            float pivotHeight = cargoSettings != null
                ? cargoSettings.RoverBodyPivotHeight
                : (titleSettings != null ? titleSettings.BodyPivotHeight : 0.58f);
            float baseWingTilt = cargoSettings != null ? cargoSettings.RoverWingTilt : -0.11f;

            Color32 bgWarm = cargoSettings != null
                ? cargoSettings.CellBackground
                : (titleSettings != null ? WithAlpha((Color32)titleSettings.BgWarm, 178) : DefaultBgWarm);
            Color32 amber = cargoSettings != null
                ? cargoSettings.AmberText
                : (titleSettings != null ? (Color32)titleSettings.Amber : DefaultAmber);
            Color32 amberHi = cargoSettings != null
                ? cargoSettings.AmberBright
                : (titleSettings != null ? (Color32)titleSettings.AmberHi : DefaultAmberHi);
            Color32 cream = titleSettings != null ? (Color32)titleSettings.Cream : DefaultCream;
            Color32 amberDim = cargoSettings != null
                ? cargoSettings.AmberDim
                : (titleSettings != null ? (Color32)titleSettings.AmberDim : DefaultAmberDim);
            Color32 amberFaint = cargoSettings != null
                ? cargoSettings.AmberFaint
                : (titleSettings != null ? (Color32)titleSettings.AmberFaint : DefaultAmberFaint);
            Color32 amberEdge = titleSettings != null ? (Color32)titleSettings.AmberEdge : new Color32(200, 106, 20, 255);
            Color32 cyan = cargoSettings != null
                ? cargoSettings.CyanAccent
                : (titleSettings != null ? (Color32)titleSettings.Cyan : DefaultCyan);
            Color32 cyanDim = cargoSettings != null
                ? cargoSettings.CyanDim
                : (titleSettings != null ? (Color32)titleSettings.CyanDim : DefaultCyanDim);

            bool titleGlow = titleSettings != null;
            float outerGlowMul = titleSettings != null ? titleSettings.GlowOuterWidthMultiplier : 4.6f;
            float outerGlowAlpha = titleSettings != null ? titleSettings.GlowOuterAlpha : 0.22f;
            float innerGlowMul = titleSettings != null ? titleSettings.GlowInnerWidthMultiplier : 2.4f;
            float innerGlowAlpha = titleSettings != null ? titleSettings.GlowInnerAlpha : 0.42f;

            Color32 CoreCol(Color32 col, bool isCyan = false)
            {
                return (titleGlow && !isCyan) ? Color32.Lerp(col, WithAlpha(cream, col.a), 0.35f) : col;
            }

            void DrawGlowLine(Vector2 a, Vector2 b, Color32 col, float w)
            {
                if (titleGlow)
                {
                    DrawLine(painter, a, b, ScaleAlpha(amberEdge, col.a, outerGlowAlpha), w * outerGlowMul);
                    DrawLine(painter, a, b, ScaleAlpha(amber, col.a, innerGlowAlpha), w * innerGlowMul);
                }

                DrawLine(painter, a, b, CoreCol(col), w);
            }

            void StrokeGlowPolygon(Vector2[] pts, int count, Color32 col, float w, bool closed = true)
            {
                if (titleGlow)
                {
                    StrokePolygon(painter, pts, count, ScaleAlpha(amberEdge, col.a, outerGlowAlpha), w * outerGlowMul, closed);
                    StrokePolygon(painter, pts, count, ScaleAlpha(amber, col.a, innerGlowAlpha), w * innerGlowMul, closed);
                }

                StrokePolygon(painter, pts, count, CoreCol(col), w, closed);
            }

            void StrokeGlowArcRad(Vector2 center, float radius, float a0, float a1, Color32 col, float w, int steps)
            {
                if (titleGlow)
                {
                    StrokeArcRad(painter, center, radius, a0, a1, ScaleAlpha(amberEdge, col.a, outerGlowAlpha), w * outerGlowMul, steps);
                    StrokeArcRad(painter, center, radius, a0, a1, ScaleAlpha(amber, col.a, innerGlowAlpha), w * innerGlowMul, steps);
                }

                StrokeArcRad(painter, center, radius, a0, a1, CoreCol(col), w, steps);
            }

            void StrokeGlowCircle(Vector2 center, float radius, Color32 col, float w, bool isCyan = false)
            {
                if (titleGlow)
                {
                    Color32 outerCol = isCyan ? cyanDim : amberEdge;
                    Color32 innerCol = isCyan ? cyan : amber;
                    StrokeCircle(painter, center, radius, ScaleAlpha(outerCol, col.a, outerGlowAlpha), w * outerGlowMul);
                    StrokeCircle(painter, center, radius, ScaleAlpha(innerCol, col.a, innerGlowAlpha), w * innerGlowMul);
                }

                StrokeCircle(painter, center, radius, CoreCol(col, isCyan), w);
            }

            float crownX = origin.x;
            float crownY = origin.y;
            float wxRear = crownX - halfWheelbase;
            float wxFront = crownX + halfWheelbase;
            float wyRear = (crownY + pose.MoonDropAtAxle) - wheelR + pose.BumpRear * s;
            float wyFront = (crownY + pose.MoonDropAtAxle) - wheelR + pose.BumpFront * s;

            // 0. Subtle lunar ground arc under 07 (in inventory diagram mode)
            if (pose.DrawGroundArc)
            {
                StrokeQuadratic(painter,
                    new Vector2(crownX - 1.35f * s, crownY + 14f * sc),
                    new Vector2(crownX, crownY - 6f * sc),
                    new Vector2(crownX + 1.35f * s, crownY + 18f * sc),
                    WithAlpha(amberFaint, 225), 1.5f);
                if (cargoSettings == null)
                {
                    FillEllipse(painter, new Vector2(crownX, crownY + 2f * sc), 0.86f * s, 11f * sc, 0f,
                        new Color32(10, 6, 3, 140));
                }
            }

            // 1. Far-side wheels (2 wheels in 3/4 depth offset, drawn behind chassis without interior disc fill)
            float farOffX = farOffsetRef.x * sc;
            float farOffY = farOffsetRef.y * sc;
            DrawWheel(painter, new Vector2(wxRear + farOffX, wyRear + farOffY), wheelR * farScale, rimR * farScale,
                pose.WheelAngle, lugCount, spokeCount, farAlpha, true, sc, bgWarm, amber, amberHi, amberDim, amberEdge,
                titleGlow, outerGlowMul, outerGlowAlpha, innerGlowMul, innerGlowAlpha);
            DrawWheel(painter, new Vector2(wxFront + farOffX, wyFront + farOffY), wheelR * farScale, rimR * farScale,
                pose.WheelAngle + 0.7f, lugCount, spokeCount, farAlpha, true, sc, bgWarm, amber, amberHi, amberDim, amberEdge,
                titleGlow, outerGlowMul, outerGlowAlpha, innerGlowMul, innerGlowAlpha);

            // Body transform: origin at (crownX, crownY + Bob * s), pivoted around -pivotHeight * s
            float bodyOriginX = crownX;
            float bodyOriginY = crownY + pose.Bob * s;
            float pivotOffsetY = -pivotHeight * s;
            float cosP = Mathf.Cos(pose.Pitch);
            float sinP = Mathf.Sin(pose.Pitch);
            float pivotX = bodyOriginX;
            float pivotY = bodyOriginY + pivotOffsetY;

            Vector2 B(float bx, float by)
            {
                float dy = by - pivotOffsetY;
                return new Vector2(
                    pivotX + bx * cosP - dy * sinP,
                    pivotY + bx * sinP + dy * cosP);
            }

            // 2. Underbody chassis rails & suspension struts to the 2 axles
            DrawGlowLine(B(-0.58f * s, -0.40f * s), B(0.58f * s, -0.40f * s), WithAlpha(amberDim, 230), 2.0f * sc);
            DrawGlowLine(B(-0.42f * s, -0.42f * s), new Vector2(wxRear, wyRear), WithAlpha(amber, 240), 2.2f * sc);
            DrawGlowLine(B(0.42f * s, -0.42f * s), new Vector2(wxFront, wyFront), WithAlpha(amber, 240), 2.2f * sc);

            if (pose.IsInstalled(RoverSocket.Belly))
            {
                ScratchQuad[0] = B(-0.24f * s, -0.40f * s);
                ScratchQuad[1] = B(0.34f * s, -0.40f * s);
                ScratchQuad[2] = B(0.34f * s, -0.33f * s);
                ScratchQuad[3] = B(-0.24f * s, -0.33f * s);
                FillPolygon(painter, ScratchQuad, 4, bgWarm);
                StrokePolygon(painter, ScratchQuad, 4, WithAlpha(amberHi, 225), 1.8f * sc);
            }

            // 3. Rear stowed cargo / sample rack behind main tub
            ScratchQuad[0] = B(-0.74f * s, -0.50f * s);
            ScratchQuad[1] = B(-0.74f * s, -0.78f * s);
            ScratchQuad[2] = B(-0.56f * s, -0.78f * s);
            ScratchQuad[3] = B(-0.56f * s, -0.50f * s);
            FillPolygon(painter, ScratchQuad, 4, bgWarm);
            StrokeGlowPolygon(ScratchQuad, 4, WithAlpha(amberDim, 230), 1.6f * sc);
            DrawGlowLine(B(-0.74f * s, -0.64f * s), B(-0.56f * s, -0.64f * s), WithAlpha(amberDim, 215), 1.2f * sc);

            if (pose.IsInstalled(RoverSocket.Hold))
            {
                ScratchQuad[0] = B(-0.84f * s, -0.52f * s);
                ScratchQuad[1] = B(-0.84f * s, -0.82f * s);
                ScratchQuad[2] = B(-0.56f * s, -0.82f * s);
                ScratchQuad[3] = B(-0.56f * s, -0.52f * s);
                StrokePolygon(painter, ScratchQuad, 4, WithAlpha(amberHi, 235), 1.9f * sc);
            }

            // 4. Main rounded beetle-carapace body tub (opaque bgWarm fill occludes upper far wheels)
            painter.fillColor = bgWarm;
            painter.BeginPath();
            Vector2 tubStart = B(-0.58f * s, -0.44f * s);
            painter.MoveTo(tubStart);
            painter.LineTo(B(-0.62f * s, -0.68f * s));
            AppendQuadraticTo(painter, B(-0.62f * s, -0.68f * s), B(-0.62f * s, -0.96f * s), B(-0.32f * s, -0.96f * s), 12);
            painter.LineTo(B(0.28f * s, -0.96f * s));
            AppendQuadraticTo(painter, B(0.28f * s, -0.96f * s), B(0.64f * s, -0.96f * s), B(0.68f * s, -0.58f * s), 12);
            painter.LineTo(B(0.60f * s, -0.44f * s));
            painter.ClosePath();
            painter.Fill();

            int tubPasses = titleGlow ? 3 : 1;
            for (int pass = 0; pass < tubPasses; pass++)
            {
                if (titleGlow)
                {
                    if (pass == 0)
                    {
                        painter.strokeColor = ScaleAlpha(amberEdge, 245, outerGlowAlpha);
                        painter.lineWidth = 2.3f * outerGlowMul * sc;
                    }
                    else if (pass == 1)
                    {
                        painter.strokeColor = ScaleAlpha(amber, 245, innerGlowAlpha);
                        painter.lineWidth = 2.3f * innerGlowMul * sc;
                    }
                    else
                    {
                        painter.strokeColor = CoreCol(WithAlpha(amber, 245));
                        painter.lineWidth = 2.3f * sc;
                    }
                }
                else
                {
                    painter.strokeColor = WithAlpha(amber, 248);
                    painter.lineWidth = 2.3f * sc;
                }

                painter.lineCap = LineCap.Round;
                painter.lineJoin = LineJoin.Round;
                painter.BeginPath();
                painter.MoveTo(tubStart);
                painter.LineTo(B(-0.62f * s, -0.68f * s));
                AppendQuadraticTo(painter, B(-0.62f * s, -0.68f * s), B(-0.62f * s, -0.96f * s), B(-0.32f * s, -0.96f * s), 12);
                painter.LineTo(B(0.28f * s, -0.96f * s));
                AppendQuadraticTo(painter, B(0.28f * s, -0.96f * s), B(0.64f * s, -0.96f * s), B(0.68f * s, -0.58f * s), 12);
                painter.LineTo(B(0.60f * s, -0.44f * s));
                painter.ClosePath();
                painter.Stroke();
            }

            // 5. Wheel-arch fenders over the rear and front wheels
            StrokeGlowArcRad(B(-halfWheelbase, -0.42f * s), wheelR * 1.10f,
                Mathf.PI * 1.06f + pose.Pitch, Mathf.PI * 1.94f + pose.Pitch,
                WithAlpha(amberHi, 225), 1.8f * sc, 16);
            StrokeGlowArcRad(B(halfWheelbase, -0.42f * s), wheelR * 1.10f,
                Mathf.PI * 1.06f + pose.Pitch, Mathf.PI * 1.94f + pose.Pitch,
                WithAlpha(amberHi, 225), 1.8f * sc, 16);

            // 6. Horizontal chassis beltline seams
            DrawGlowLine(B(-0.60f * s, -0.60f * s), B(0.65f * s, -0.60f * s), WithAlpha(amberDim, 210), 1.2f * sc);
            DrawLine(painter, B(-0.56f * s, -0.78f * s), B(0.56f * s, -0.78f * s), WithAlpha(amberFaint, 220), 1.0f * sc);

            // 7. Side inspection hatch + 4 vertical louver vents (highlighted when Panniers is installed)
            bool hasPanniers = pose.IsInstalled(RoverSocket.Panniers);
            ScratchQuad[0] = B(-0.34f * s, -0.75f * s);
            ScratchQuad[1] = B(0.06f * s, -0.75f * s);
            ScratchQuad[2] = B(0.06f * s, -0.48f * s);
            ScratchQuad[3] = B(-0.34f * s, -0.48f * s);
            StrokeGlowPolygon(ScratchQuad, 4,
                WithAlpha(hasPanniers ? amberHi : amberDim, (byte)(hasPanniers ? 235 : 210)),
                (hasPanniers ? 1.9f : 1.3f) * sc);
            for (int v = 0; v < 4; v++)
            {
                float vx = (-0.27f + v * 0.065f) * s;
                DrawLine(painter, B(vx, -0.71f * s), B(vx, -0.52f * s), WithAlpha(amberFaint, 220), 1.0f * sc);
            }

            // 8. 6-segment side battery charge indicator bar (highlighted when Flanks is installed)
            bool hasFlanks = pose.IsInstalled(RoverSocket.Flanks);
            float bx0 = 0.14f * s;
            float by0 = -0.74f * s;
            float bw = 0.34f * s;
            float bh = 0.11f * s;
            ScratchQuad[0] = B(bx0, by0);
            ScratchQuad[1] = B(bx0 + bw, by0);
            ScratchQuad[2] = B(bx0 + bw, by0 + bh);
            ScratchQuad[3] = B(bx0, by0 + bh);
            StrokeGlowPolygon(ScratchQuad, 4,
                WithAlpha(hasFlanks ? amberHi : amberDim, (byte)(hasFlanks ? 235 : 210)),
                (hasFlanks ? 1.9f : 1.2f) * sc);

            const int segs = 6;
            float segW = (bw - 4f * sc) / segs;
            for (int i = 0; i < segs; i++)
            {
                float sx = bx0 + 2f * sc + i * segW;
                ScratchQuad[0] = B(sx + 0.8f * sc, by0 + 2f * sc);
                ScratchQuad[1] = B(sx + segW - 1.2f * sc, by0 + 2f * sc);
                ScratchQuad[2] = B(sx + segW - 1.2f * sc, by0 + bh - 2f * sc);
                ScratchQuad[3] = B(sx + 0.8f * sc, by0 + bh - 2f * sc);
                if (i < 5)
                {
                    FillPolygon(painter, ScratchQuad, 4, WithAlpha(amberHi, (byte)(hasFlanks ? 235 : 215)));
                }

                StrokePolygon(painter, ScratchQuad, 4, WithAlpha(amberDim, 220), 0.9f * sc);
            }

            // 9. Front lower dig scoop / blade
            Vector2 scoopPivot = B(0.60f * s, -0.46f * s);
            float scoopAng = pose.Pitch + pose.ScoopNod;
            float cosS = Mathf.Cos(scoopAng);
            float sinS = Mathf.Sin(scoopAng);
            Vector2 S(float sx, float sy)
            {
                return new Vector2(
                    scoopPivot.x + sx * cosS - sy * sinS,
                    scoopPivot.y + sx * sinS + sy * cosS);
            }

            DrawGlowLine(scoopPivot, S(12f * sc, 5f * sc), WithAlpha(amber, 235), 1.8f * sc);
            DrawGlowLine(S(0f, 4f * sc), S(12f * sc, -3f * sc), WithAlpha(amber, 235), 1.8f * sc);
            StrokeGlowArcRad(S(15f * sc, 1f * sc), 7f * sc,
                -0.42f * Mathf.PI + scoopAng, 0.45f * Mathf.PI + scoopAng,
                WithAlpha(amberHi, 240), 2.0f * sc, 10);

            // 10. Rear tow hook
            StrokeGlowArcRad(B(-0.64f * s, -0.44f * s), 4.5f * sc,
                0.5f * Mathf.PI + pose.Pitch, 1.6f * Mathf.PI + pose.Pitch,
                WithAlpha(amberDim, 225), 1.6f * sc, 10);

            // 11. Front headlamp dome + beam
            Vector2 lampCenter = B(0.67f * s, -0.62f * s);
            FillArcRad(painter, lampCenter, 6f * sc, -Mathf.PI * 0.5f + pose.Pitch, Mathf.PI * 0.5f + pose.Pitch,
                bgWarm, 10);
            StrokeGlowArcRad(lampCenter, 6f * sc, -Mathf.PI * 0.5f + pose.Pitch, Mathf.PI * 0.5f + pose.Pitch,
                WithAlpha(amberHi, 245), 2.0f * sc, 10);
            FillCircle(painter, B(0.71f * s, -0.62f * s), 1.8f * sc, cream);

            if (titleGlow && pose.AlwaysDrawHeadlampBeam)
            {
                // Prototype 2-line headlamp ray shimmer from _render_anim.py.
                DrawGlowLine(B(0.76f * s, -0.63f * s), B(1.28f * s, -0.55f * s), WithAlpha(amber, 72), 1.1f * sc);
                DrawGlowLine(B(0.76f * s, -0.60f * s), B(1.22f * s, -0.36f * s), WithAlpha(amber, 72), 1.1f * sc);
            }
            else if (pose.AlwaysDrawHeadlampBeam || pose.IsInstalled(RoverSocket.Lamp))
            {
                float maxReach = pose.AlwaysDrawHeadlampBeam
                    ? 1.58f
                    : (cargoSettings != null ? cargoSettings.HeadlampBeamLength : 1.15f);
                float minReach = pose.AlwaysDrawHeadlampBeam ? 0.98f : Mathf.Min(0.84f, maxReach * 0.72f);
                float sliceAlphaFloat = pose.AlwaysDrawHeadlampBeam
                    ? 0.032f
                    : (cargoSettings != null ? cargoSettings.HeadlampBeamSliceAlpha : 0f);
                byte sliceAlpha = (byte)Mathf.Clamp(Mathf.RoundToInt(sliceAlphaFloat * 255f), 0, 255);
                if (sliceAlpha > 0)
                {
                    Color32 beamCol = WithAlpha(amberHi, sliceAlpha);
                    for (int slice = 0; slice < 3; slice++)
                    {
                        float t = slice / 2f;
                        float reach = Mathf.Lerp(minReach, maxReach, t) * s;
                        float spreadTop = Mathf.Lerp(-0.70f, pose.AlwaysDrawHeadlampBeam ? -0.78f : -0.72f, t) * s;
                        float spreadBot = Mathf.Lerp(-0.52f, pose.AlwaysDrawHeadlampBeam ? -0.34f : -0.48f, t) * s;
                        ScratchBeam[0] = B(0.73f * s, -0.64f * s);
                        ScratchBeam[1] = B(reach, spreadTop);
                        ScratchBeam[2] = B(reach, spreadBot);
                        ScratchBeam[3] = B(0.73f * s, -0.59f * s);
                        FillPolygon(painter, ScratchBeam, 4, beamCol);
                    }
                }
                else
                {
                    DrawLine(painter, B(0.75f * s, -0.63f * s), B(0.96f * s, -0.68f * s), WithAlpha(amberDim, 230), 1.2f * sc);
                    DrawLine(painter, B(0.75f * s, -0.60f * s), B(0.96f * s, -0.53f * s), WithAlpha(amberDim, 230), 1.2f * sc);
                }
            }

            // 11b. Installed Back / Deck module highlights on upper rear deck
            if (pose.IsInstalled(RoverSocket.Back))
            {
                ScratchQuad[0] = B(-0.34f * s, -1.03f * s);
                ScratchQuad[1] = B(-0.14f * s, -1.03f * s);
                ScratchQuad[2] = B(-0.14f * s, -0.96f * s);
                ScratchQuad[3] = B(-0.34f * s, -0.96f * s);
                FillPolygon(painter, ScratchQuad, 4, bgWarm);
                StrokePolygon(painter, ScratchQuad, 4, WithAlpha(amberHi, 235), 1.8f * sc);
            }

            if (pose.IsInstalled(RoverSocket.Deck))
            {
                ScratchQuad[0] = B(-0.54f * s, -1.03f * s);
                ScratchQuad[1] = B(-0.38f * s, -1.03f * s);
                ScratchQuad[2] = B(-0.38f * s, -0.96f * s);
                ScratchQuad[3] = B(-0.54f * s, -0.96f * s);
                FillPolygon(painter, ScratchQuad, 4, bgWarm);
                StrokePolygon(painter, ScratchQuad, 4, WithAlpha(amberHi, 235), 1.8f * sc);
            }

            // 12. Tilted roof solar panel wing
            bool hasWing = pose.IsInstalled(RoverSocket.Wing);
            Vector2 wingPivot = B(-0.08f * s, -0.96f * s);
            float wingAng = pose.Pitch + baseWingTilt + pose.SolarTilt;
            float cosW = Mathf.Cos(wingAng);
            float sinW = Mathf.Sin(wingAng);
            Vector2 W(float wx, float wy)
            {
                return new Vector2(
                    wingPivot.x + wx * cosW - wy * sinW,
                    wingPivot.y + wx * sinW + wy * cosW);
            }

            DrawGlowLine(W(-18f * sc, 0f), W(-18f * sc, -6f * sc), WithAlpha(amberDim, 230), 1.5f * sc);
            DrawGlowLine(W(18f * sc, 0f), W(18f * sc, -6f * sc), WithAlpha(amberDim, 230), 1.5f * sc);
            ScratchQuad[0] = W(-32f * sc, -9f * sc);
            ScratchQuad[1] = W(26f * sc, -9f * sc);
            ScratchQuad[2] = W(26f * sc, -6f * sc);
            ScratchQuad[3] = W(-32f * sc, -6f * sc);
            FillPolygon(painter, ScratchQuad, 4, bgWarm);
            StrokeGlowPolygon(ScratchQuad, 4,
                WithAlpha(hasWing ? amberHi : amber, 240), (hasWing ? 2.1f : 1.8f) * sc);
            for (int c = -20; c <= 16; c += 12)
            {
                DrawLine(painter, W(c * sc, -9f * sc), W(c * sc, -6f * sc), WithAlpha(amberDim, 220), 1.0f * sc);
            }

            // 13. Rear whip antenna + diamond sonar loop + Morse beacon
            Vector2 antBase = B(-0.48f * s, -0.95f * s);
            Vector2 antCtrl = B((-0.54f + pose.AntennaSway * 0.4f) * s, -1.24f * s);
            Vector2 antTip = B((-0.44f + pose.AntennaSway) * s, -1.50f * s);
            if (titleGlow)
            {
                StrokeQuadratic(painter, antBase, antCtrl, antTip, ScaleAlpha(amberEdge, 235, outerGlowAlpha), 1.6f * outerGlowMul * sc);
                StrokeQuadratic(painter, antBase, antCtrl, antTip, ScaleAlpha(amber, 235, innerGlowAlpha), 1.6f * innerGlowMul * sc);
            }

            StrokeQuadratic(painter, antBase, antCtrl, antTip, WithAlpha(amber, 235), 1.6f * sc);

            Vector2 loopC = B((-0.50f + pose.AntennaSway * 0.3f) * s, -1.14f * s);
            ScratchQuad[0] = loopC + new Vector2(0f, -6f * sc);
            ScratchQuad[1] = loopC + new Vector2(5f * sc, 0f);
            ScratchQuad[2] = loopC + new Vector2(0f, 6f * sc);
            ScratchQuad[3] = loopC + new Vector2(-5f * sc, 0f);
            StrokeGlowPolygon(ScratchQuad, 4, WithAlpha(amberDim, 225), 1.2f * sc);

            Color32 morseCol = pose.MorseOn ? (titleGlow ? cyan : amberHi) : amber;
            StrokeGlowCircle(antTip, 2.6f * sc, WithAlpha(morseCol, 240), 1.5f * sc, titleGlow && pose.MorseOn);
            if (pose.MorseOn)
            {
                FillCircle(painter, antTip, 2.0f * sc, cream);
                Color32 ringCol = titleGlow ? cyan : amberHi;
                Color32 outerRingCol = titleGlow ? cyanDim : amber;
                StrokeGlowCircle(antTip, 7f * sc, WithAlpha(ringCol, (byte)(titleGlow ? 140 : 225)), 1.2f * sc, titleGlow);
                StrokeCircle(painter, antTip, 13f * sc, WithAlpha(outerRingCol, (byte)(titleGlow ? 72 : 215)), 1.0f * sc);
            }

            // 14. Ribbed rubber neck bellows (3 stacked collars)
            for (int r = 0; r < 3; r++)
            {
                float ny = (-0.985f - r * 0.036f) * s;
                float nrw = (10.5f - r * 0.8f) * sc;
                Vector2 neckC = B(0.15f * s, ny);
                FillEllipse(painter, neckC, nrw, 2.6f * sc, pose.Pitch, bgWarm, 16);
                if (titleGlow)
                {
                    StrokeEllipse(painter, neckC, nrw, 2.6f * sc, pose.Pitch, ScaleAlpha(amberEdge, 230, outerGlowAlpha), 1.3f * outerGlowMul * sc, 16);
                    StrokeEllipse(painter, neckC, nrw, 2.6f * sc, pose.Pitch, ScaleAlpha(amber, 230, innerGlowAlpha), 1.3f * innerGlowMul * sc, 16);
                }

                StrokeEllipse(painter, neckC, nrw, 2.6f * sc, pose.Pitch, WithAlpha(amberDim, 230), 1.3f * sc, 16);
            }

            // 15. Expressive CRT Monitor Head
            Vector2 headPivot = B(0.15f * s, -1.06f * s);
            float headAng = pose.Pitch + pose.HeadNod;
            float cosH = Mathf.Cos(headAng);
            float sinH = Mathf.Sin(headAng);
            Vector2 H(float hx, float hy)
            {
                return new Vector2(
                    headPivot.x + hx * cosH - hy * sinH,
                    headPivot.y + hx * sinH + hy * cosH);
            }

            float hw = 0.25f * s;
            float hh = 0.22f * s;
            float hr = 6f * sc;

            // Outer rounded CRT head housing
            BeginRoundedHeadPath(painter, headPivot, cosH, sinH, -hw * 0.85f, -hh, hw * 1.85f, hh, hr);
            painter.fillColor = bgWarm;
            painter.Fill();

            int headPasses = titleGlow ? 3 : 1;
            for (int pass = 0; pass < headPasses; pass++)
            {
                if (titleGlow)
                {
                    if (pass == 0)
                    {
                        painter.strokeColor = ScaleAlpha(amberEdge, 248, outerGlowAlpha);
                        painter.lineWidth = 2.3f * outerGlowMul * sc;
                    }
                    else if (pass == 1)
                    {
                        painter.strokeColor = ScaleAlpha(amber, 248, innerGlowAlpha);
                        painter.lineWidth = 2.3f * innerGlowMul * sc;
                    }
                    else
                    {
                        painter.strokeColor = CoreCol(WithAlpha(amber, 248));
                        painter.lineWidth = 2.3f * sc;
                    }
                }
                else
                {
                    painter.strokeColor = WithAlpha(amber, 248);
                    painter.lineWidth = 2.3f * sc;
                }

                BeginRoundedHeadPath(painter, headPivot, cosH, sinH, -hw * 0.85f, -hh, hw * 1.85f, hh, hr);
                painter.Stroke();
            }

            // Inner CRT screen bezel
            float inset = 3.5f * sc;
            ScratchQuad[0] = H(-hw * 0.50f, -hh + inset);
            ScratchQuad[1] = H(hw - inset, -hh + inset);
            ScratchQuad[2] = H(hw - inset, -inset);
            ScratchQuad[3] = H(-hw * 0.50f, -inset);
            StrokeGlowPolygon(ScratchQuad, 4, WithAlpha(amberDim, 225), 1.3f * sc);

            // Side brass lens pod on cheek
            Vector2 cheekC = H(-hw * 0.38f, -hh * 0.48f);
            FillCircle(painter, cheekC, 4.2f * sc, bgWarm);
            StrokeGlowCircle(cheekC, 4.2f * sc, WithAlpha(amber, 235), 1.5f * sc);
            StrokeCircle(painter, cheekC, 1.5f * sc, WithAlpha(amberHi, 235), 1.2f * sc);
            if (titleGlow)
            {
                FillCircle(painter, cheekC, 0.9f * sc, cyan);
            }

            // Forehead brow ridge / sun visor
            DrawGlowLine(H(-hw + 3.5f * sc, -hh - 2.5f * sc), H(hw + 2f * sc, -hh - 2.5f * sc),
                WithAlpha(amberHi, 240), 1.8f * sc);

            // Glowing eye inside CRT face
            float eyeX = hw * 0.14f;
            float eyeY = -hh * 0.50f;
            if (pose.EyeBlink)
            {
                DrawGlowLine(H(eyeX - 5f * sc, eyeY), H(eyeX + 5f * sc, eyeY), WithAlpha(amberHi, 240), 2.0f * sc);
            }
            else
            {
                Vector2 eyeCenter = H(eyeX, eyeY);
                StrokeGlowCircle(eyeCenter, 6.2f * sc, WithAlpha(amberHi, 225), 1.4f * sc);
                FillCircle(painter, eyeCenter, 3.6f * sc, amberHi);
                FillCircle(painter, H(eyeX + 1.1f * sc, eyeY - 1.0f * sc), 1.3f * sc, cream);
            }

            // Installed Cutter / Tether highlights on head
            if (pose.IsInstalled(RoverSocket.Cutter))
            {
                Vector2 cPt = H(hw * 0.12f, -hh - 4.5f * sc);
                DrawLine(painter, H(-hw * 0.15f, -hh - 4.5f * sc), H(hw * 0.45f, -hh - 4.5f * sc),
                    WithAlpha(amberHi, 245), 2.2f * sc);
                FillCircle(painter, cPt, 2.2f * sc, WithAlpha(cyan, 235));
            }

            if (pose.IsInstalled(RoverSocket.Tether))
            {
                Vector2 tPt = H(hw + 2.5f * sc, -hh * 0.45f);
                StrokeCircle(painter, tPt, 4.2f * sc, WithAlpha(amberHi, 240), 1.8f * sc);
                FillCircle(painter, tPt, 1.8f * sc, WithAlpha(cyan, 235));
            }

            // 16. Near-side wheels (2 wheels drawn in front of body with dark interior disc fill)
            DrawWheel(painter, new Vector2(wxRear, wyRear), wheelR, rimR,
                pose.WheelAngle, lugCount, spokeCount, 1.0f, false, sc, bgWarm, amber, amberHi, amberDim, amberEdge,
                titleGlow, outerGlowMul, outerGlowAlpha, innerGlowMul, innerGlowAlpha);
            DrawWheel(painter, new Vector2(wxFront, wyFront), wheelR, rimR,
                pose.WheelAngle + 0.7f, lugCount, spokeCount, 1.0f, false, sc, bgWarm, amber, amberHi, amberDim, amberEdge,
                titleGlow, outerGlowMul, outerGlowAlpha, innerGlowMul, innerGlowAlpha);
        }

        private static Color32 ScaleAlpha(Color32 color, byte baseAlpha, float factor)
        {
            byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(baseAlpha * factor), 0, 255);
            return new Color32(color.r, color.g, color.b, a);
        }

        private static Vector2 TransformHeadPoint(Vector2 headPivot, float cosH, float sinH, float hx, float hy)
        {
            return new Vector2(
                headPivot.x + hx * cosH - hy * sinH,
                headPivot.y + hx * sinH + hy * cosH);
        }

        private static void BeginRoundedHeadPath(Painter2D painter, Vector2 headPivot, float cosH, float sinH,
            float x0, float y0, float w, float h, float r)
        {
            float x1 = x0 + w;
            float y1 = y0 + h;
            painter.BeginPath();
            painter.MoveTo(TransformHeadPoint(headPivot, cosH, sinH, x0 + r, y0));
            painter.LineTo(TransformHeadPoint(headPivot, cosH, sinH, x1 - r, y0));
            AppendQuadraticTo(painter,
                TransformHeadPoint(headPivot, cosH, sinH, x1 - r, y0),
                TransformHeadPoint(headPivot, cosH, sinH, x1, y0),
                TransformHeadPoint(headPivot, cosH, sinH, x1, y0 + r), 5);
            painter.LineTo(TransformHeadPoint(headPivot, cosH, sinH, x1, y1 - r));
            AppendQuadraticTo(painter,
                TransformHeadPoint(headPivot, cosH, sinH, x1, y1 - r),
                TransformHeadPoint(headPivot, cosH, sinH, x1, y1),
                TransformHeadPoint(headPivot, cosH, sinH, x1 - r, y1), 5);
            painter.LineTo(TransformHeadPoint(headPivot, cosH, sinH, x0 + r, y1));
            AppendQuadraticTo(painter,
                TransformHeadPoint(headPivot, cosH, sinH, x0 + r, y1),
                TransformHeadPoint(headPivot, cosH, sinH, x0, y1),
                TransformHeadPoint(headPivot, cosH, sinH, x0, y1 - r), 5);
            painter.LineTo(TransformHeadPoint(headPivot, cosH, sinH, x0, y0 + r));
            AppendQuadraticTo(painter,
                TransformHeadPoint(headPivot, cosH, sinH, x0, y0 + r),
                TransformHeadPoint(headPivot, cosH, sinH, x0, y0),
                TransformHeadPoint(headPivot, cosH, sinH, x0 + r, y0), 5);
            painter.ClosePath();
        }

        /// <summary>
        /// Returns the 2D screen/canvas anchor point on Rover 07 for <paramref name="socket"/>'s leader line,
        /// matching the side-3/4 Rover 07 geometry.
        /// </summary>
        public static Vector2 GetSocketMountPoint(RoverSocket socket, Vector2 origin, float s, float pitch = 0f,
            float headNod = -0.06f, float pivotHeight = 0.58f, float wingTilt = -0.11f)
        {
            float sc = s / RefRoverScale;
            float pivotOffsetY = -pivotHeight * s;
            float cosP = Mathf.Cos(pitch);
            float sinP = Mathf.Sin(pitch);
            float pivotX = origin.x;
            float pivotY = origin.y + pivotOffsetY;

            Vector2 B(float bx, float by)
            {
                float dy = by - pivotOffsetY;
                return new Vector2(
                    pivotX + bx * cosP - dy * sinP,
                    pivotY + bx * sinP + dy * cosP);
            }

            Vector2 headPivot = B(0.15f * s, -1.06f * s);
            float headAng = pitch + headNod;
            float cosH = Mathf.Cos(headAng);
            float sinH = Mathf.Sin(headAng);
            Vector2 H(float hx, float hy)
            {
                return new Vector2(
                    headPivot.x + hx * cosH - hy * sinH,
                    headPivot.y + hx * sinH + hy * cosH);
            }

            Vector2 wingPivot = B(-0.08f * s, -0.96f * s);
            float wingAng = pitch + wingTilt;
            float cosW = Mathf.Cos(wingAng);
            float sinW = Mathf.Sin(wingAng);
            Vector2 W(float wx, float wy)
            {
                return new Vector2(
                    wingPivot.x + wx * cosW - wy * sinW,
                    wingPivot.y + wx * sinW + wy * cosW);
            }

            switch (socket)
            {
                case RoverSocket.Wing: return W(-0.04f * s, -9f * sc);
                case RoverSocket.Cutter: return H(0.03f * s, -0.31f * s);
                case RoverSocket.Tether: return H(0.08f * s, -0.15f * s);
                case RoverSocket.Lamp: return B(0.70f * s, -0.62f * s);
                case RoverSocket.Flanks: return B(0.35f * s, -0.69f * s);
                case RoverSocket.Belly: return B(0.06f * s, -0.36f * s);
                case RoverSocket.Panniers: return B(-0.06f * s, -0.46f * s);
                case RoverSocket.Hold: return B(-0.75f * s, -0.66f * s);
                case RoverSocket.Deck: return B(-0.47f * s, -0.96f * s);
                case RoverSocket.Back: return B(-0.24f * s, -0.96f * s);
                case RoverSocket.Wheels: return B(0.0f, -0.35f * s);
                case RoverSocket.Antenna: return B(-0.46f * s, -1.35f * s);
                default: return B(0f, -0.60f * s);
            }
        }

        private static void DrawWheel(Painter2D painter, Vector2 center, float rOuter, float rRim, float angle,
            int lugCount, int spokeCount, float alphaScale, bool isFarSide, float sc, Color32 bgWarm, Color32 amber,
            Color32 amberHi, Color32 amberDim, Color32 amberEdge, bool titleGlow, float outerGlowMul,
            float outerGlowAlpha, float innerGlowMul, float innerGlowAlpha)
        {
            byte aMain = (byte)Mathf.RoundToInt(242f * alphaScale);
            byte aDim = (byte)Mathf.RoundToInt(215f * alphaScale);
            byte aHi = (byte)Mathf.RoundToInt(248f * alphaScale);

            if (!isFarSide)
            {
                // Dark interior disc so background lines do not show through near-side wheels.
                FillCircle(painter, center, rOuter, bgWarm);
            }

            float outerW = (isFarSide ? 1.5f : 2.2f) * sc;
            if (titleGlow)
            {
                StrokeCircle(painter, center, rOuter, ScaleAlpha(amberEdge, aMain, outerGlowAlpha), outerW * outerGlowMul);
                StrokeCircle(painter, center, rOuter, ScaleAlpha(amber, aMain, innerGlowAlpha), outerW * innerGlowMul);
            }

            // Outer tire ring & inner tread ring
            StrokeCircle(painter, center, rOuter, WithAlpha(amber, aMain), outerW);
            if (titleGlow && !isFarSide)
            {
                StrokeCircle(painter, center, rOuter * 0.86f, ScaleAlpha(amber, aDim, innerGlowAlpha), 1.1f * innerGlowMul * sc);
            }

            StrokeCircle(painter, center, rOuter * 0.86f, WithAlpha(amberDim, aDim), 1.1f * sc);

            // Radial tread lugs
            float lugW = (isFarSide ? 1.1f : 1.5f) * sc;
            for (int i = 0; i < lugCount; i++)
            {
                float a = angle + (i * Mathf.PI * 2f) / lugCount;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 p0 = center + dir * (rOuter * 0.86f);
                Vector2 p1 = center + dir * (rOuter * 1.04f);
                if (titleGlow && !isFarSide)
                {
                    DrawLine(painter, p0, p1, ScaleAlpha(amber, aMain, innerGlowAlpha), lugW * innerGlowMul);
                }

                DrawLine(painter, p0, p1, WithAlpha(amber, aMain), lugW);
            }

            // Hub rim circle
            if (titleGlow)
            {
                StrokeCircle(painter, center, rRim, ScaleAlpha(amberEdge, aMain, outerGlowAlpha), 1.5f * outerGlowMul * sc);
                StrokeCircle(painter, center, rRim, ScaleAlpha(amber, aMain, innerGlowAlpha), 1.5f * innerGlowMul * sc);
            }

            StrokeCircle(painter, center, rRim, WithAlpha(amber, aMain), 1.5f * sc);

            // 6 spokes
            float capR = 4.5f * sc;
            for (int i = 0; i < spokeCount; i++)
            {
                float a = angle + (i * Mathf.PI * 2f) / spokeCount;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 s0 = center + dir * capR;
                Vector2 s1 = center + dir * rRim;
                if (titleGlow && !isFarSide)
                {
                    DrawLine(painter, s0, s1, ScaleAlpha(amber, aDim, innerGlowAlpha), 1.3f * innerGlowMul * sc);
                }

                DrawLine(painter, s0, s1, WithAlpha(amberDim, aDim), 1.3f * sc);
            }

            // Center axle cap
            if (!isFarSide)
            {
                FillCircle(painter, center, capR, bgWarm);
            }

            if (titleGlow)
            {
                StrokeCircle(painter, center, capR, ScaleAlpha(amber, aHi, innerGlowAlpha), 1.4f * innerGlowMul * sc);
            }

            StrokeCircle(painter, center, capR, WithAlpha(amberHi, aHi), 1.4f * sc);
        }

        private static void AppendQuadraticTo(Painter2D painter, Vector2 p0, Vector2 control, Vector2 p1, int steps)
        {
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                float mt = 1f - t;
                Vector2 pt = mt * mt * p0 + 2f * mt * t * control + t * t * p1;
                painter.LineTo(pt);
            }
        }

        public static void StrokeQuadratic(Painter2D painter, Vector2 p0, Vector2 control, Vector2 p1, Color32 color,
            float width, int steps = 16)
        {
            if (color.a == 0 || steps < 2)
            {
                return;
            }

            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            painter.MoveTo(p0);
            AppendQuadraticTo(painter, p0, control, p1, steps);
            painter.Stroke();
        }

        public static void StrokeDashedQuadratic(Painter2D painter, Vector2 p0, Vector2 control, Vector2 p1,
            Color32 color, float width, float dash = 6f, float gap = 4f, int steps = 48)
        {
            if (color.a == 0 || steps < 2)
            {
                return;
            }

            Vector2 prev = p0;
            float phase = 0f;
            float period = Mathf.Max(1f, dash + gap);
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                float mt = 1f - t;
                Vector2 cur = mt * mt * p0 + 2f * mt * t * control + t * t * p1;
                float segLen = Vector2.Distance(prev, cur);
                if (segLen > 0.01f)
                {
                    Vector2 dir = (cur - prev) / segLen;
                    float pos = 0f;
                    while (pos < segLen)
                    {
                        float cyclePos = phase % period;
                        if (cyclePos < dash)
                        {
                            float step = Mathf.Min(dash - cyclePos, segLen - pos);
                            DrawLine(painter, prev + dir * pos, prev + dir * (pos + step), color, width);
                            pos += step;
                            phase += step;
                        }
                        else
                        {
                            float step = Mathf.Min(period - cyclePos, segLen - pos);
                            pos += step;
                            phase += step;
                        }
                    }
                }

                prev = cur;
            }
        }

        private static void StrokeArcRad(Painter2D painter, Vector2 center, float radius, float a0, float a1,
            Color32 color, float width, int steps)
        {
            if (radius <= 0f || color.a == 0 || steps < 2)
            {
                return;
            }

            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            for (int i = 0; i <= steps; i++)
            {
                float a = a0 + (a1 - a0) * i / steps;
                Vector2 pt = new Vector2(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius);
                if (i == 0)
                {
                    painter.MoveTo(pt);
                }
                else
                {
                    painter.LineTo(pt);
                }
            }

            painter.Stroke();
        }

        private static void FillArcRad(Painter2D painter, Vector2 center, float radius, float a0, float a1,
            Color32 color, int steps)
        {
            if (radius <= 0f || color.a == 0 || steps < 2)
            {
                return;
            }

            painter.fillColor = color;
            painter.BeginPath();
            for (int i = 0; i <= steps; i++)
            {
                float a = a0 + (a1 - a0) * i / steps;
                Vector2 pt = new Vector2(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius);
                if (i == 0)
                {
                    painter.MoveTo(pt);
                }
                else
                {
                    painter.LineTo(pt);
                }
            }

            painter.ClosePath();
            painter.Fill();
        }

        /// <summary>Draws a tiny locked-cell padlock icon centred at <paramref name="center"/>.</summary>
        public static void DrawPadlock(Painter2D painter, Vector2 center, Color32 color)
        {
            StrokeRect(painter, center.x - 5.5f, center.y - 1f, 11f, 8.5f, color, 1.4f);
            StrokeArc(painter, new Vector2(center.x, center.y - 1f), 3.6f, 180f, 360f, color, 1.4f);
            FillCircle(painter, new Vector2(center.x, center.y + 3.2f), 1.2f, color);
        }

        /// <summary>Draws L-shaped corner bracket ticks inside rectangle <c>(x, y, w, h)</c>.</summary>
        public static void DrawCornerTicks(Painter2D painter, float x, float y, float w, float h, float length,
            Color32 color, float width)
        {
            DrawLine(painter, new Vector2(x, y + length), new Vector2(x, y), color, width);
            DrawLine(painter, new Vector2(x, y), new Vector2(x + length, y), color, width);

            DrawLine(painter, new Vector2(x + w - length, y), new Vector2(x + w, y), color, width);
            DrawLine(painter, new Vector2(x + w, y), new Vector2(x + w, y + length), color, width);

            DrawLine(painter, new Vector2(x, y + h - length), new Vector2(x, y + h), color, width);
            DrawLine(painter, new Vector2(x, y + h), new Vector2(x + length, y + h), color, width);

            DrawLine(painter, new Vector2(x + w - length, y + h), new Vector2(x + w, y + h), color, width);
            DrawLine(painter, new Vector2(x + w, y + h), new Vector2(x + w, y + h - length), color, width);
        }

        public static void FillRect(Painter2D painter, float x, float y, float w, float h, Color32 color)
        {
            if (w <= 0f || h <= 0f || color.a == 0)
            {
                return;
            }

            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(x, y));
            painter.LineTo(new Vector2(x + w, y));
            painter.LineTo(new Vector2(x + w, y + h));
            painter.LineTo(new Vector2(x, y + h));
            painter.ClosePath();
            painter.Fill();
        }

        public static void StrokeRect(Painter2D painter, float x, float y, float w, float h, Color32 color, float width)
        {
            if (w <= 0f || h <= 0f || color.a == 0)
            {
                return;
            }

            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.lineJoin = LineJoin.Miter;
            painter.BeginPath();
            painter.MoveTo(new Vector2(x, y));
            painter.LineTo(new Vector2(x + w, y));
            painter.LineTo(new Vector2(x + w, y + h));
            painter.LineTo(new Vector2(x, y + h));
            painter.ClosePath();
            painter.Stroke();
        }

        public static void StrokeDashedRect(Painter2D painter, float x, float y, float w, float h, Color32 color,
            float width, float dash = 5f, float gap = 4f)
        {
            StrokeDashedLine(painter, new Vector2(x, y), new Vector2(x + w, y), color, width, dash, gap);
            StrokeDashedLine(painter, new Vector2(x + w, y), new Vector2(x + w, y + h), color, width, dash, gap);
            StrokeDashedLine(painter, new Vector2(x + w, y + h), new Vector2(x, y + h), color, width, dash, gap);
            StrokeDashedLine(painter, new Vector2(x, y + h), new Vector2(x, y), color, width, dash, gap);
        }

        public static void StrokeDashedLine(Painter2D painter, Vector2 a, Vector2 b, Color32 color, float width,
            float dash = 5f, float gap = 4f)
        {
            float dist = Vector2.Distance(a, b);
            if (dist < 0.5f || color.a == 0)
            {
                return;
            }

            Vector2 dir = (b - a) / dist;
            float pos = 0f;
            while (pos < dist)
            {
                float end = Mathf.Min(pos + dash, dist);
                DrawLine(painter, a + dir * pos, a + dir * end, color, width);
                pos += dash + gap;
            }
        }

        public static void FillPolygon(Painter2D painter, Vector2[] pts, int count, Color32 color)
        {
            if (count < 3 || color.a == 0)
            {
                return;
            }

            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(pts[0]);
            for (int i = 1; i < count; i++)
            {
                painter.LineTo(pts[i]);
            }

            painter.ClosePath();
            painter.Fill();
        }

        public static void StrokePolygon(Painter2D painter, Vector2[] pts, int count, Color32 color, float width,
            bool closed = true)
        {
            if (count < 2 || color.a == 0)
            {
                return;
            }

            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            painter.MoveTo(pts[0]);
            for (int i = 1; i < count; i++)
            {
                painter.LineTo(pts[i]);
            }

            if (closed)
            {
                painter.ClosePath();
            }

            painter.Stroke();
        }

        public static void FillCircle(Painter2D painter, Vector2 center, float radius, Color32 color)
        {
            if (radius <= 0f || color.a == 0)
            {
                return;
            }

            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
        }

        public static void StrokeCircle(Painter2D painter, Vector2 center, float radius, Color32 color, float width)
        {
            if (radius <= 0f || color.a == 0)
            {
                return;
            }

            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f);
            painter.ClosePath();
            painter.Stroke();
        }

        public static void StrokeArc(Painter2D painter, Vector2 center, float radius, float startDeg, float endDeg,
            Color32 color, float width)
        {
            if (radius <= 0f || color.a == 0)
            {
                return;
            }

            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(startDeg), Angle.Degrees(endDeg));
            painter.Stroke();
        }

        public static void DrawLine(Painter2D painter, Vector2 a, Vector2 b, Color32 color, float width)
        {
            if (color.a == 0)
            {
                return;
            }

            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.MoveTo(a);
            painter.LineTo(b);
            painter.Stroke();
        }

        public static void FillEllipse(Painter2D painter, Vector2 center, float rx, float ry, float rotationRad,
            Color32 color, int steps = 28)
        {
            if (rx <= 0f || ry <= 0f || color.a == 0)
            {
                return;
            }

            float cr = Mathf.Cos(rotationRad);
            float sr = Mathf.Sin(rotationRad);
            painter.fillColor = color;
            painter.BeginPath();
            for (int i = 0; i <= steps; i++)
            {
                float a = i * Mathf.PI * 2f / steps;
                float ex = Mathf.Cos(a) * rx;
                float ey = Mathf.Sin(a) * ry;
                Vector2 pt = new Vector2(center.x + ex * cr - ey * sr, center.y + ex * sr + ey * cr);
                if (i == 0)
                {
                    painter.MoveTo(pt);
                }
                else
                {
                    painter.LineTo(pt);
                }
            }

            painter.ClosePath();
            painter.Fill();
        }

        public static void StrokeEllipse(Painter2D painter, Vector2 center, float rx, float ry, float rotationRad,
            Color32 color, float width, int steps = 28)
        {
            if (rx <= 0f || ry <= 0f || color.a == 0)
            {
                return;
            }

            float cr = Mathf.Cos(rotationRad);
            float sr = Mathf.Sin(rotationRad);
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            for (int i = 0; i <= steps; i++)
            {
                float a = i * Mathf.PI * 2f / steps;
                float ex = Mathf.Cos(a) * rx;
                float ey = Mathf.Sin(a) * ry;
                Vector2 pt = new Vector2(center.x + ex * cr - ey * sr, center.y + ex * sr + ey * cr);
                if (i == 0)
                {
                    painter.MoveTo(pt);
                }
                else
                {
                    painter.LineTo(pt);
                }
            }

            painter.ClosePath();
            painter.Stroke();
        }

        public static Color32 WithAlpha(Color32 color, byte alpha)
        {
            return new Color32(color.r, color.g, color.b, alpha);
        }
    }
}
