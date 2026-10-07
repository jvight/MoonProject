using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Bell's four camera-tripod legs: a chrome thigh with a charcoal foam grip from a hip knuckle under the cabinet,
    /// a hinged knee with Ro's honey flip-lock, a thinner chrome shin and a rubber foot. Corners are 0 FL, 1 FR,
    /// 2 RL, 3 RR (left = -X, front = +Z). Each leg splays out a little more below the knee, like a tripod. A thigh
    /// is built from its hip pivot, a shin from its knee pivot; in the rest pose every foot stands on y = 0.
    /// </summary>
    internal static class BellLegMeshes
    {
        public const int LegCount = 4;

        /// <summary>Height of every hip pivot, a centimetre under the cabinet's underside.</summary>
        public const float HipHeight = 0.74f;

        /// <summary>Height of the cabinet's underside (the Body pivot) in the rest pose.</summary>
        public const float BodyBase = 0.75f;

        private const float HipX = 0.33f;
        private const float HipZ = 0.15f;
        private const float FootHalfHeight = 0.015f;

        private static readonly string[] Corners = { "FL", "FR", "RL", "RR" };

        public static string Corner(int corner)
        {
            return Corners[Require(corner)];
        }

        /// <summary>Hip pivot of leg <paramref name="corner"/> in Bell's space (rest pose).</summary>
        public static Vector3 Hip(int corner)
        {
            return Scale(corner, new Vector3(HipX, 0f, HipZ)) + Vector3.up * HipHeight;
        }

        /// <summary>Knee pivot of leg <paramref name="corner"/> in its leg's space.</summary>
        public static Vector3 Knee(int corner)
        {
            return Scale(corner, new Vector3(0.13f, -0.35f, 0.08f));
        }

        /// <summary>Centre of the foot of leg <paramref name="corner"/> in its shin's space.</summary>
        public static Vector3 Foot(int corner)
        {
            return Scale(corner, new Vector3(0.09f, -(HipHeight - 0.35f - FootHalfHeight), 0.05f));
        }

        /// <summary>The upper leg from the hip knuckle (origin) to the knee.</summary>
        public static LowPolyMeshBuilder Thigh(int corner)
        {
            var b = new LowPolyMeshBuilder(200);
            Vector3 knee = Knee(corner);
            b.Icosphere(Matrix4x4.identity, 0.032f, 1, PaletteSwatch.Charcoal);
            b.Box(At(0f, 0.022f, 0f), new Vector3(0.06f, 0.024f, 0.06f), PaletteSwatch.Metal, 0.006f);
            RecipeKit.Rod(b, Vector3.zero, knee, 0.021f, 6, PaletteSwatch.Metal);
            RecipeKit.Rod(b, knee * 0.12f, knee * 0.5f, 0.027f, 6, PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>The lower leg from the knee hinge (origin) to the rubber foot.</summary>
        public static LowPolyMeshBuilder Shin(int corner)
        {
            var b = new LowPolyMeshBuilder(200);
            Vector3 foot = Foot(corner);
            float outward = Mathf.Sign(Scale(corner, Vector3.right).x);
            b.Prism(At(Vector3.zero, AlongX), 0.029f, 0.056f, 8, PaletteSwatch.Charcoal);
            b.Box(At(new Vector3(outward * 0.034f, -0.025f, 0f), new Vector3(0f, 0f, outward * 8f)),
                new Vector3(0.012f, 0.065f, 0.026f), PaletteSwatch.Honey, 0.004f);
            RecipeKit.Rod(b, Vector3.zero, foot, 0.015f, 6, PaletteSwatch.Metal);
            RecipeKit.Rod(b, foot * 0.82f, foot * 0.9f, 0.019f, 6, PaletteSwatch.Charcoal);
            b.Frustum(At(foot), 0.034f, 0.022f, FootHalfHeight * 2f, 8, PaletteSwatch.Charcoal);
            return b;
        }

        private static Vector3 Scale(int corner, Vector3 frontRight)
        {
            Require(corner);
            float x = corner % 2 == 0 ? -1f : 1f;
            float z = corner < 2 ? 1f : -1f;
            return new Vector3(frontRight.x * x, frontRight.y, frontRight.z * z);
        }

        private static int Require(int corner)
        {
            if (corner < 0 || corner >= LegCount)
            {
                throw new System.ArgumentOutOfRangeException(nameof(corner), corner, "Bell has legs 0..3.");
            }

            return corner;
        }
    }
}
