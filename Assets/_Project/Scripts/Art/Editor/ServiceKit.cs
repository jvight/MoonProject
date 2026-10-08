using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Rover-height service parts the crew built so 07 could tend the base without hands (VISION ruling 14): material
    /// hoppers 07's beam feeds and service hatches it can reach. Shared by the Rover Bay and the radio tower stages.
    /// </summary>
    internal static class ServiceKit
    {
        /// <summary>How far a hopper's mouth tips up from level, so 07's beam drops things in from above.</summary>
        public const float MouthTilt = 40f;

        private const float LegThickness = 0.06f;

        // The funnel's dark inside stands a hair proud of its outer shell's top, so the mouth reads open and dark.
        private const float InnerDrop = 0.47f;

        /// <summary>
        /// A hopper standing on four legs: a box body and a square funnel whose mouth is centred on
        /// <paramref name="mouth"/>, opening along the +Z of <paramref name="facing"/> (Euler degrees), its inside
        /// dark, a worn orange lip. <paramref name="size"/> is the mouth's half width.
        /// </summary>
        public static void Hopper(LowPolyMeshBuilder b, Vector3 mouth, Vector3 facing, float size)
        {
            Matrix4x4 funnel = Funnel(mouth, facing);
            Vector3 bowl = funnel.MultiplyPoint3x4(new Vector3(0f, -size, 0f));
            float leg = size * 0.6f;
            var foot = new Vector3(bowl.x, 0f, bowl.z);
            float bodyTop = bowl.y - 0.05f;
            for (int i = 0; i < 4; i++)
            {
                var corner = new Vector3(i % 2 == 0 ? -leg : leg, 0f, i < 2 ? -leg : leg);
                SiteKit.Bar(b, foot + corner + Vector3.up * LegThickness,
                    foot + corner * 0.7f + Vector3.up * (bodyTop - 0.1f), LegThickness, PaletteSwatch.Metal);
            }

            b.Box(At(foot + Vector3.up * (bodyTop - 0.1f)), new Vector3(leg * 1.8f, 0.26f, leg * 1.8f),
                PaletteSwatch.FadedPaint, 0.03f);
            b.Frustum(funnel * At(0f, -size * 0.5f, 0f), size * 0.35f, size, size, 4, PaletteSwatch.FadedPaint);
            b.Frustum(funnel * At(0f, -size * InnerDrop, 0f), size * 0.31f, size * 0.94f, size * 0.95f, 4,
                PaletteSwatch.Charcoal);
            b.Torus(funnel, size, size * 0.1f, 4, 3, PaletteSwatch.FadedAccent);
        }

        /// <summary>The rust a hopper gathers: collars round the feet of its legs.</summary>
        public static void HopperRust(LowPolyMeshBuilder b, Vector3 mouth, Vector3 facing, float size)
        {
            Vector3 bowl = Funnel(mouth, facing).MultiplyPoint3x4(new Vector3(0f, -size, 0f));
            float leg = size * 0.6f;
            for (int i = 0; i < 4; i++)
            {
                var corner = new Vector3(i % 2 == 0 ? -leg : leg, 0.12f, i < 2 ? -leg : leg);
                Weathering.Collar(b, At(new Vector3(bowl.x, 0f, bowl.z) + corner), 0.06f, 0.12f);
            }
        }

        /// <summary>The funnel's frame: origin at the mouth, local -Y running down its throat to the bowl.</summary>
        private static Matrix4x4 Funnel(Vector3 mouth, Vector3 facing)
        {
            return At(mouth, facing) * Matrix4x4.Rotate(Rotation(AlongZ));
        }

        /// <summary>
        /// A service hatch door in its own space: hinged on its left edge at the origin, the door running +X
        /// <paramref name="width"/> and centred on the origin's height, facing +Z: a faded panel in a dark frame with
        /// a big ring pull 07's beam can hook.
        /// </summary>
        public static LowPolyMeshBuilder Hatch(float width, float height)
        {
            var b = new LowPolyMeshBuilder(160);
            b.Box(At(width * 0.5f, 0f, 0.015f), new Vector3(width, height, 0.03f), PaletteSwatch.FadedPaint, 0.008f);
            b.Box(At(width * 0.5f, 0f, 0.034f), new Vector3(width - 0.08f, height - 0.08f, 0.008f),
                PaletteSwatch.Metal);
            b.Torus(At(new Vector3(width - 0.1f, 0f, 0.05f), AlongZ), 0.05f, 0.012f, 8, 3, PaletteSwatch.Charcoal);
            for (int i = 0; i < 2; i++)
            {
                b.Prism(At(0.01f, (i == 0 ? -1f : 1f) * height * 0.3f, 0.015f), 0.018f, 0.12f, 6,
                    PaletteSwatch.Charcoal);
            }

            return b;
        }
    }
}
