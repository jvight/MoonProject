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
        public float ScoopNod;
        public float SolarTilt;
        public float AntennaSway;
        public float HeadNod;
        public bool MorseOn;
        public bool EyeBlink;
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
        private static readonly float[] WheelOffsets = { -1f, 0f, 1f };

        private static readonly Vector2[] ScratchQuad = new Vector2[4];
        private static readonly Vector2[] ScratchHex = new Vector2[6];
        private static readonly Vector2[] ScratchOct = new Vector2[8];
        private static readonly Vector2[] ScratchBeam = new Vector2[4];
        private static readonly Vector2[] ScratchScoop = new Vector2[4];
        private static readonly Vector2[] ScratchWing = new Vector2[4];
        private static readonly Vector2[] ScratchHead = new Vector2[4];
        private static readonly Vector2[] ScratchVisor = new Vector2[4];

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
        /// When <paramref name="pose"/> has <see cref="RoverPose.InstalledSocketsMask"/> bits set, the corresponding
        /// kit hardware on 07 (belly coils, rear rack, spine pack, side bins, flank drum, lamp beam, solar wing, tape
        /// deck, tether collar, focus lens) is drawn and lit.
        /// </summary>
        public static void DrawRover07(Painter2D painter, Vector2 origin, float s, in RoverPose pose,
            TitleSettings settings = null)
        {
            float rWheel = (settings != null ? settings.WheelRadius : 0.35f) * s;
            float rRim = (settings != null ? settings.WheelRimRadius : 0.185f) * s;
            float wb = (settings != null ? settings.WheelbaseHalf : 0.53f) * s;
            float farScale = settings != null ? settings.FarWheelScale : 0.94f;
            float farAlpha = settings != null ? settings.FarWheelAlpha : 0.42f;
            int lugCount = settings != null ? settings.WheelLugCount : 12;
            int spokeCount = settings != null ? settings.WheelSpokeCount : 6;
            float pivotHeight = settings != null ? settings.BodyPivotHeight : 0.58f;

            Color32 bgWarm = settings != null ? settings.BgWarm : DefaultBgWarm;
            Color32 amber = settings != null ? settings.Amber : DefaultAmber;
            Color32 amberHi = settings != null ? settings.AmberHi : DefaultAmberHi;
            Color32 cream = settings != null ? settings.Cream : DefaultCream;
            Color32 amberDim = settings != null ? settings.AmberDim : DefaultAmberDim;
            Color32 amberFaint = settings != null ? settings.AmberFaint : DefaultAmberFaint;
            Color32 cyan = settings != null ? settings.Cyan : DefaultCyan;
            Color32 cyanDim = settings != null ? settings.CyanDim : DefaultCyanDim;

            float ox = origin.x;
            float oy = origin.y + pose.Bob * s;
            float cp = Mathf.Cos(pose.Pitch);
            float sp = Mathf.Sin(pose.Pitch);
            float pivX = ox;
            float pivY = oy - pivotHeight * s;

            Vector2 R(float lx, float ly)
            {
                float dx = lx * s;
                float dy = ly * s + pivotHeight * s;
                return new Vector2(pivX + dx * cp - dy * sp, pivY + dx * sp + dy * cp);
            }

            // 0. Ground contact shadow
            FillEllipse(painter, new Vector2(ox, oy + 0.02f * s), wb * 1.38f, 0.10f * s, 0f,
                new Color32(8, 5, 2, 165));

            // 1. Far-side 3 wheels
            float yFar = -0.05f * s;
            float xFar = 0.06f * s;
            for (int wi = 0; wi < 3; wi++)
            {
                float wx = ox + WheelOffsets[wi] * wb + xFar;
                float wy = oy - rWheel + yFar;
                DrawWheel(painter, new Vector2(wx, wy), rWheel * farScale, rRim * farScale,
                    pose.WheelAngle + wi * 0.4f, lugCount, spokeCount, farAlpha, bgWarm, amber, amberDim, amberFaint);
            }

            // 2. Rocker-bogie suspension struts
            Vector2 w0 = R(-0.53f, -0.35f);
            Vector2 w1 = R(0f, -0.35f);
            Vector2 w2 = R(0.53f, -0.35f);
            Vector2 hubPiv = R(-0.08f, -0.52f);
            Vector2 frontJoin = R(0.24f, -0.46f);
            Vector2 rearJoin = R(-0.32f, -0.46f);

            DrawLine(painter, w0, rearJoin, WithAlpha(amberDim, 210), 2.8f);
            DrawLine(painter, rearJoin, hubPiv, WithAlpha(amberDim, 210), 2.8f);
            DrawLine(painter, w1, rearJoin, WithAlpha(amberDim, 190), 2.3f);
            DrawLine(painter, hubPiv, frontJoin, WithAlpha(amberDim, 210), 2.8f);
            DrawLine(painter, frontJoin, w2, WithAlpha(amberDim, 210), 2.8f);
            FillCircle(painter, hubPiv, 3.6f, bgWarm);
            StrokeCircle(painter, hubPiv, 3.6f, WithAlpha(amber, 230), 1.6f);

            // 2b. Underbody belly hover coils (when Belly is installed)
            if (pose.IsInstalled(RoverSocket.Belly))
            {
                for (int ci = 0; ci < 2; ci++)
                {
                    float cxOff = ci == 0 ? -0.22f : 0.20f;
                    Vector2 cTop = R(cxOff - 0.09f, -0.36f);
                    Vector2 cBot = R(cxOff + 0.09f, -0.22f);
                    float rx = Mathf.Min(cTop.x, cBot.x);
                    float ry = Mathf.Min(cTop.y, cBot.y);
                    float rw = Mathf.Abs(cBot.x - cTop.x);
                    float rh = Mathf.Abs(cBot.y - cTop.y);
                    FillRect(painter, rx, ry, rw, rh, bgWarm);
                    StrokeRect(painter, rx, ry, rw, rh, WithAlpha(amberHi, 235), 1.6f);
                    DrawLine(painter, new Vector2(rx + 2f, ry + rh * 0.45f),
                        new Vector2(rx + rw - 2f, ry + rh * 0.45f), WithAlpha(cyan, 210), 1.3f);
                    Vector2 padC = R(cxOff, -0.17f);
                    DrawLine(painter, new Vector2(padC.x - 6f, padC.y - 2f), new Vector2(padC.x, padC.y + 3f),
                        WithAlpha(cyan, 185), 1.3f);
                    DrawLine(painter, new Vector2(padC.x, padC.y + 3f), new Vector2(padC.x + 6f, padC.y - 2f),
                        WithAlpha(cyan, 185), 1.3f);
                }
            }

            // 3. Rear cargo rack (expanded when Hold is installed)
            ScratchQuad[0] = R(-0.68f, -0.52f);
            ScratchQuad[1] = R(-0.46f, -0.52f);
            ScratchQuad[2] = R(-0.46f, -0.74f);
            ScratchQuad[3] = R(-0.66f, -0.72f);
            FillPolygon(painter, ScratchQuad, 4, bgWarm);
            StrokePolygon(painter, ScratchQuad, 4,
                WithAlpha(pose.IsInstalled(RoverSocket.Hold) ? amberHi : amberDim, 210), 1.8f);
            for (int ri = 0; ri < 3; ri++)
            {
                float ry = -0.56f - ri * 0.055f;
                DrawLine(painter, R(-0.66f, ry), R(-0.47f, ry), WithAlpha(amberFaint, 175), 1.2f);
            }

            if (pose.IsInstalled(RoverSocket.Hold))
            {
                ScratchQuad[0] = R(-0.78f, -0.49f);
                ScratchQuad[1] = R(-0.48f, -0.49f);
                ScratchQuad[2] = R(-0.48f, -0.78f);
                ScratchQuad[3] = R(-0.75f, -0.75f);
                StrokePolygon(painter, ScratchQuad, 4, WithAlpha(amberHi, 240), 2.0f);
                DrawLine(painter, R(-0.62f, -0.49f), R(-0.62f, -0.76f), WithAlpha(cyan, 200), 1.4f);
            }

            // 4. Main chassis tub
            ScratchOct[0] = R(-0.50f, -0.40f);
            ScratchOct[1] = R(0.46f, -0.40f);
            ScratchOct[2] = R(0.56f, -0.50f);
            ScratchOct[3] = R(0.50f, -0.74f);
            ScratchOct[4] = R(0.30f, -0.78f);
            ScratchOct[5] = R(-0.46f, -0.78f);
            ScratchOct[6] = R(-0.54f, -0.68f);
            ScratchOct[7] = R(-0.54f, -0.46f);
            FillPolygon(painter, ScratchOct, 8, bgWarm);
            StrokePolygon(painter, ScratchOct, 8, WithAlpha(amber, 248), 2.3f);

            DrawLine(painter, R(-0.52f, -0.57f), R(0.53f, -0.57f), WithAlpha(amberDim, 175), 1.3f);
            for (int vi = 0; vi < 4; vi++)
            {
                float vx = -0.38f + vi * 0.065f;
                DrawLine(painter, R(vx, -0.63f), R(vx, -0.72f), WithAlpha(amberFaint, 185), 1.3f);
            }

            DrawZeroSeven(painter, R(0.18f, -0.61f), s * 0.075f, WithAlpha(amberHi, 240));

            // 4b. Spine battery pack on back deck (when Back is installed)
            if (pose.IsInstalled(RoverSocket.Back))
            {
                ScratchQuad[0] = R(-0.46f, -0.78f);
                ScratchQuad[1] = R(0.02f, -0.78f);
                ScratchQuad[2] = R(-0.02f, -0.91f);
                ScratchQuad[3] = R(-0.44f, -0.91f);
                FillPolygon(painter, ScratchQuad, 4, bgWarm);
                StrokePolygon(painter, ScratchQuad, 4, WithAlpha(amberHi, 240), 1.8f);
                for (int bi = 0; bi < 3; bi++)
                {
                    float bx = -0.35f + bi * 0.12f;
                    DrawLine(painter, R(bx, -0.78f), R(bx, -0.90f), WithAlpha(amberDim, 200), 1.2f);
                    FillCircle(painter, R(bx - 0.04f, -0.84f), 1.8f, WithAlpha(cyan, 215));
                }
            }

            // 4c. Side panniers bin low between wheels (when Panniers is installed)
            if (pose.IsInstalled(RoverSocket.Panniers))
            {
                ScratchQuad[0] = R(-0.22f, -0.44f);
                ScratchQuad[1] = R(0.18f, -0.44f);
                ScratchQuad[2] = R(0.20f, -0.27f);
                ScratchQuad[3] = R(-0.24f, -0.27f);
                FillPolygon(painter, ScratchQuad, 4, bgWarm);
                StrokePolygon(painter, ScratchQuad, 4, WithAlpha(amberHi, 235), 1.8f);
                DrawLine(painter, R(-0.10f, -0.44f), R(-0.10f, -0.27f), WithAlpha(amberDim, 210), 1.3f);
                DrawLine(painter, R(0.06f, -0.44f), R(0.06f, -0.27f), WithAlpha(amberDim, 210), 1.3f);
                FillCircle(painter, R(-0.02f, -0.35f), 2.0f, WithAlpha(cyan, 225));
            }

            // 5. Flank capacitor drum
            Vector2 drumA = R(-0.34f, -0.58f);
            Vector2 drumB = R(0.06f, -0.43f);
            float dLeft = Mathf.Min(drumA.x, drumB.x);
            float dTop = Mathf.Min(drumA.y, drumB.y);
            float dWidth = Mathf.Abs(drumB.x - drumA.x);
            float dHeight = Mathf.Abs(drumB.y - drumA.y);
            bool hasFlanks = pose.IsInstalled(RoverSocket.Flanks);
            FillRect(painter, dLeft, dTop, dWidth, dHeight, bgWarm);
            StrokeRect(painter, dLeft, dTop, dWidth, dHeight,
                WithAlpha(hasFlanks ? amberHi : amber, 235), hasFlanks ? 2.1f : 1.7f);
            DrawLine(painter, R(-0.24f, -0.58f), R(-0.24f, -0.43f), WithAlpha(amberDim, 200), 1.3f);
            DrawLine(painter, R(-0.04f, -0.58f), R(-0.04f, -0.43f), WithAlpha(amberDim, 200), 1.3f);
            if (hasFlanks)
            {
                DrawLine(painter, R(-0.14f, -0.57f), R(-0.14f, -0.44f), WithAlpha(cyan, 230), 2.0f);
            }

            // 6. Front lamp bar + headlamp beam cone
            bool hasLamp = pose.IsInstalled(RoverSocket.Lamp);
            Vector2 lampPos = R(0.56f, -0.52f);
            ScratchBeam[0] = lampPos;
            ScratchBeam[1] = R(1.85f, -0.82f);
            ScratchBeam[2] = R(1.85f, -0.10f);
            ScratchBeam[3] = lampPos;
            FillPolygon(painter, ScratchBeam, 3, new Color32(amber.r, amber.g, amber.b, (byte)(hasLamp ? 34 : 16)));
            DrawLine(painter, lampPos, R(1.55f, -0.75f), WithAlpha(amberDim, (byte)(hasLamp ? 115 : 55)), 1.1f);
            DrawLine(painter, lampPos, R(1.55f, -0.22f), WithAlpha(amberDim, (byte)(hasLamp ? 115 : 55)), 1.1f);

            Vector2 lA = R(0.50f, -0.60f);
            Vector2 lB = R(0.58f, -0.45f);
            FillRect(painter, Mathf.Min(lA.x, lB.x), Mathf.Min(lA.y, lB.y),
                Mathf.Abs(lB.x - lA.x), Mathf.Abs(lB.y - lA.y), bgWarm);
            StrokeRect(painter, Mathf.Min(lA.x, lB.x), Mathf.Min(lA.y, lB.y),
                Mathf.Abs(lB.x - lA.x), Mathf.Abs(lB.y - lA.y), WithAlpha(amberHi, 245), 1.8f);
            FillCircle(painter, lampPos, hasLamp ? 4.4f : 3.2f, WithAlpha(cream, 250));

            // 7. Front scoop / excavator blade
            float scoopY = pose.ScoopNod;
            ScratchScoop[0] = R(0.50f, -0.35f);
            ScratchScoop[1] = R(0.74f, -0.21f + scoopY);
            ScratchScoop[2] = R(0.79f, -0.32f + scoopY);
            ScratchScoop[3] = R(0.54f, -0.43f);
            FillPolygon(painter, ScratchScoop, 4, bgWarm);
            StrokePolygon(painter, ScratchScoop, 4, WithAlpha(amber, 235), 1.9f);
            DrawLine(painter, R(0.46f, -0.42f), R(0.66f, -0.29f + scoopY), WithAlpha(amberDim, 200), 1.7f);

            // 8. Near-side 3 wheels
            for (int wi = 0; wi < 3; wi++)
            {
                float wx = ox + WheelOffsets[wi] * wb;
                float wy = oy - rWheel;
                DrawWheel(painter, new Vector2(wx, wy), rWheel, rRim,
                    pose.WheelAngle + wi * 0.4f, lugCount, spokeCount, 1.0f, bgWarm, amber, amberDim, amberFaint);
            }

            // 9. Tilted solar wing on top deck
            bool hasWing = pose.IsInstalled(RoverSocket.Wing);
            float wTilt = pose.SolarTilt;
            ScratchWing[0] = R(-0.50f, -0.80f);
            ScratchWing[1] = R(0.14f, -0.80f + wTilt);
            ScratchWing[2] = R(0.19f, -0.87f + wTilt);
            ScratchWing[3] = R(-0.45f, -0.87f);
            FillPolygon(painter, ScratchWing, 4, bgWarm);
            StrokePolygon(painter, ScratchWing, 4, WithAlpha(hasWing ? amberHi : amber, 240), 1.9f);
            for (int si = 1; si < 4; si++)
            {
                float frac = si / 4f;
                Vector2 topP = Vector2.Lerp(ScratchWing[0], ScratchWing[1], frac);
                Vector2 botP = Vector2.Lerp(ScratchWing[3], ScratchWing[2], frac);
                DrawLine(painter, topP, botP, WithAlpha(amberFaint, 195), 1.1f);
            }

            if (hasWing)
            {
                FillCircle(painter, Vector2.Lerp(ScratchWing[1], ScratchWing[2], 0.5f) + new Vector2(-5f, 0f),
                    2.2f, WithAlpha(cyan, 230));
            }

            // 10. Rear antenna mast + sonar loop + blinking Morse beacon
            Vector2 antBase = R(-0.42f, -0.78f);
            Vector2 antTip = R(-0.46f + pose.AntennaSway, -1.36f);
            DrawLine(painter, antBase, antTip, WithAlpha(amber, 235), 1.9f);
            Vector2 loopC = R(-0.44f + pose.AntennaSway * 0.6f, -1.08f);
            StrokeEllipse(painter, loopC, 0.065f * s, 0.11f * s, -0.15f, WithAlpha(amberDim, 215), 1.5f);

            if (pose.MorseOn)
            {
                FillCircle(painter, antTip, 4.2f, WithAlpha(cyan, 245));
                StrokeCircle(painter, antTip, 8.5f, WithAlpha(cyanDim, 145), 1.3f);
                StrokeCircle(painter, antTip, 14.0f, WithAlpha(cyanDim, 68), 1.0f);
            }
            else
            {
                FillCircle(painter, antTip, 2.8f, WithAlpha(amberHi, 205));
            }

            // 10b. Tape deck on rear-left flank (shown when Deck is installed, or faint outline otherwise)
            bool hasDeck = pose.IsInstalled(RoverSocket.Deck);
            Vector2 tdA = R(-0.50f, -0.68f);
            Vector2 tdB = R(-0.26f, -0.52f);
            float tdX = Mathf.Min(tdA.x, tdB.x);
            float tdY = Mathf.Min(tdA.y, tdB.y);
            float tdW = Mathf.Abs(tdB.x - tdA.x);
            float tdH = Mathf.Abs(tdB.y - tdA.y);
            FillRect(painter, tdX, tdY, tdW, tdH, bgWarm);
            StrokeRect(painter, tdX, tdY, tdW, tdH,
                WithAlpha(hasDeck ? amberHi : amberFaint, (byte)(hasDeck ? 240 : 120)), hasDeck ? 1.8f : 1.1f);
            StrokeCircle(painter, R(-0.43f, -0.60f), 0.038f * s,
                WithAlpha(hasDeck ? amberHi : amberFaint, (byte)(hasDeck ? 235 : 110)), 1.2f);
            StrokeCircle(painter, R(-0.33f, -0.60f), 0.038f * s,
                WithAlpha(hasDeck ? amberHi : amberFaint, (byte)(hasDeck ? 235 : 110)), 1.2f);
            if (hasDeck)
            {
                FillCircle(painter, R(-0.38f, -0.55f), 1.8f, WithAlpha(cyan, 230));
            }

            // 11. Articulated camera mast & head with lens + visor + cyan eye
            Vector2 mastBase = R(0.24f, -0.78f);
            Vector2 mastKnee = R(0.30f, -0.98f);
            Vector2 headPiv = R(0.28f, -1.08f);
            DrawLine(painter, mastBase, mastKnee, WithAlpha(amber, 245), 3.0f);
            DrawLine(painter, mastKnee, headPiv, WithAlpha(amber, 245), 3.0f);
            FillCircle(painter, mastKnee, 3.0f, bgWarm);
            StrokeCircle(painter, mastKnee, 3.0f, WithAlpha(amberHi, 230), 1.4f);

            float ch = Mathf.Cos(pose.HeadNod);
            float sh = Mathf.Sin(pose.HeadNod);

            Vector2 H(float hx, float hy)
            {
                float dx = hx * s;
                float dy = hy * s;
                return new Vector2(headPiv.x + dx * ch - dy * sh, headPiv.y + dx * sh + dy * ch);
            }

            ScratchHead[0] = H(-0.14f, -0.11f);
            ScratchHead[1] = H(0.18f, -0.11f);
            ScratchHead[2] = H(0.21f, 0.09f);
            ScratchHead[3] = H(-0.14f, 0.09f);
            FillPolygon(painter, ScratchHead, 4, bgWarm);
            StrokePolygon(painter, ScratchHead, 4, WithAlpha(amber, 250), 2.1f);

            ScratchVisor[0] = H(0.14f, -0.11f);
            ScratchVisor[1] = H(0.27f, -0.07f);
            ScratchVisor[2] = H(0.24f, 0.03f);
            ScratchVisor[3] = H(0.18f, 0.03f);
            StrokePolygon(painter, ScratchVisor, 4, WithAlpha(amberHi, 235), 1.7f);

            Vector2 eyeC = H(0.17f, -0.01f);
            float eyeR = 0.068f * s;
            FillCircle(painter, eyeC, eyeR, bgWarm);
            StrokeCircle(painter, eyeC, eyeR, WithAlpha(amberHi, 250), 1.9f);

            if (pose.IsInstalled(RoverSocket.Tether))
            {
                StrokeCircle(painter, eyeC, eyeR + 3.2f, WithAlpha(cyan, 210), 1.5f);
            }

            if (pose.IsInstalled(RoverSocket.Cutter))
            {
                Vector2 cutterTip = H(0.27f, -0.01f);
                DrawLine(painter, eyeC + new Vector2(eyeR, 0f), cutterTip, WithAlpha(amberHi, 240), 1.8f);
                FillCircle(painter, cutterTip, 2.2f, WithAlpha(cyan, 230));
            }

            if (!pose.EyeBlink)
            {
                FillCircle(painter, eyeC + new Vector2(1.4f, 0f), eyeR * 0.46f, WithAlpha(cyan, 245));
                FillCircle(painter, eyeC + new Vector2(2.2f, -1.2f), eyeR * 0.18f, WithAlpha(cream, 255));
            }
            else
            {
                DrawLine(painter, eyeC + new Vector2(-eyeR * 0.6f, 0f), eyeC + new Vector2(eyeR * 0.6f, 0f),
                    WithAlpha(amberHi, 235), 1.8f);
            }
        }

        /// <summary>
        /// Returns the 2D screen/canvas anchor point on Rover 07 for <paramref name="socket"/>'s leader line.
        /// </summary>
        public static Vector2 GetSocketMountPoint(RoverSocket socket, Vector2 origin, float s, float pitch = 0f,
            float headNod = -0.05f, float pivotHeight = 0.58f)
        {
            float cp = Mathf.Cos(pitch);
            float sp = Mathf.Sin(pitch);
            float pivX = origin.x;
            float pivY = origin.y - pivotHeight * s;

            Vector2 R(float lx, float ly)
            {
                float dx = lx * s;
                float dy = ly * s + pivotHeight * s;
                return new Vector2(pivX + dx * cp - dy * sp, pivY + dx * sp + dy * cp);
            }

            Vector2 headPiv = R(0.28f, -1.08f);
            float ch = Mathf.Cos(headNod);
            float sh = Mathf.Sin(headNod);

            Vector2 H(float hx, float hy)
            {
                float dx = hx * s;
                float dy = hy * s;
                return new Vector2(headPiv.x + dx * ch - dy * sh, headPiv.y + dx * sh + dy * ch);
            }

            switch (socket)
            {
                case RoverSocket.Wing: return R(-0.18f, -0.84f);
                case RoverSocket.Cutter: return H(0.27f, -0.02f);
                case RoverSocket.Tether: return H(0.20f, 0.04f);
                case RoverSocket.Lamp: return R(0.56f, -0.52f);
                case RoverSocket.Flanks: return R(-0.14f, -0.50f);
                case RoverSocket.Belly: return R(0.0f, -0.27f);
                case RoverSocket.Panniers: return R(-0.02f, -0.35f);
                case RoverSocket.Hold: return R(-0.62f, -0.60f);
                case RoverSocket.Deck: return R(-0.38f, -0.60f);
                case RoverSocket.Back: return R(-0.20f, -0.84f);
                case RoverSocket.Wheels: return R(0.0f, -0.18f);
                case RoverSocket.Antenna: return R(-0.46f, -1.20f);
                default: return R(0f, -0.55f);
            }
        }

        private static void DrawWheel(Painter2D painter, Vector2 center, float rOuter, float rRim, float angle,
            int lugCount, int spokeCount, float alphaScale, Color32 bgWarm, Color32 amber, Color32 amberDim,
            Color32 amberFaint)
        {
            byte aMain = (byte)Mathf.RoundToInt(245f * alphaScale);
            byte aDim = (byte)Mathf.RoundToInt(175f * alphaScale);
            byte aHub = (byte)Mathf.RoundToInt(215f * alphaScale);

            FillCircle(painter, center, rOuter, bgWarm);
            StrokeCircle(painter, center, rOuter, WithAlpha(amber, aMain), 2.1f);

            for (int i = 0; i < lugCount; i++)
            {
                float a = angle + i * (Mathf.PI * 2f / lugCount);
                Vector2 p0 = new Vector2(center.x + Mathf.Cos(a) * (rOuter - 1.5f),
                    center.y + Mathf.Sin(a) * (rOuter - 1.5f));
                Vector2 p1 = new Vector2(center.x + Mathf.Cos(a) * (rOuter + 3.6f),
                    center.y + Mathf.Sin(a) * (rOuter + 3.6f));
                DrawLine(painter, p0, p1, WithAlpha(amber, aMain), 2.3f);
            }

            StrokeCircle(painter, center, rRim, WithAlpha(amberDim, aDim), 1.4f);

            for (int i = 0; i < spokeCount; i++)
            {
                float a = angle + i * (Mathf.PI * 2f / spokeCount);
                Vector2 p0 = new Vector2(center.x + Mathf.Cos(a) * (rRim * 0.36f),
                    center.y + Mathf.Sin(a) * (rRim * 0.36f));
                Vector2 p1 = new Vector2(center.x + Mathf.Cos(a) * rRim, center.y + Mathf.Sin(a) * rRim);
                DrawLine(painter, p0, p1, WithAlpha(amberFaint, aDim), 1.3f);
            }

            FillCircle(painter, center, rRim * 0.36f, bgWarm);
            StrokeCircle(painter, center, rRim * 0.36f, WithAlpha(amber, aHub), 1.5f);
        }

        private static void DrawZeroSeven(Painter2D painter, Vector2 center, float size, Color32 color)
        {
            float w = size * 0.72f;
            float h = size * 1.15f;
            float gap = size * 0.35f;

            float x0 = center.x - w - gap * 0.5f;
            float y0 = center.y - h * 0.5f;
            StrokeRect(painter, x0, y0, w, h, color, 1.5f);

            float x7 = center.x + gap * 0.5f;
            painter.strokeColor = color;
            painter.lineWidth = 1.5f;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            painter.MoveTo(new Vector2(x7, y0));
            painter.LineTo(new Vector2(x7 + w, y0));
            painter.LineTo(new Vector2(x7 + w * 0.35f, y0 + h));
            painter.Stroke();
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
