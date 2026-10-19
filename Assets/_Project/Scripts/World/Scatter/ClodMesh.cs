using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The regolith's small clods and stones (owner's Apollo 11 reference, 2026-10-19): tiny lumpy rocks built from a
    /// squat square bipyramid (6 vertices, 8 triangles), each variant with its own uneven corners, so the rubble layer
    /// can lie thick near the camera for a few triangles a stone. Origin at the ground contact, one unit across.
    /// </summary>
    public static class ClodMesh
    {
        private const uint ShapeSalt = 0x6A09E667u;

        /// <summary>
        /// A readable clod mesh for <paramref name="variant"/>, vertex-coloured <paramref name="color"/>.
        /// </summary>
        public static Mesh Create(int variant, Color32 color)
        {
            uint seed = Hashing.Mix((uint)variant ^ ShapeSalt);
            var positions = new Vector3[6];
            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 90f + Lerp(-20f, 20f, Unit(seed, i))) * Mathf.Deg2Rad;
                float reach = 0.5f * Lerp(0.7f, 1f, Unit(seed, i + 4));
                positions[i] = new Vector3(Mathf.Sin(angle) * reach, Lerp(0.08f, 0.22f, Unit(seed, i + 8)),
                    Mathf.Cos(angle) * reach);
            }

            positions[4] = new Vector3(Lerp(-0.1f, 0.1f, Unit(seed, 12)), Lerp(0.35f, 0.55f, Unit(seed, 13)),
                Lerp(-0.1f, 0.1f, Unit(seed, 14)));
            positions[5] = new Vector3(0f, -0.08f, 0f);
            int[] triangles =
            {
                4, 0, 1, 4, 1, 2, 4, 2, 3, 4, 3, 0,
                5, 1, 0, 5, 2, 1, 5, 3, 2, 5, 0, 3,
            };
            var colors = new Color32[6];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }

            var mesh = new Mesh { name = "Clod_" + variant };
            mesh.vertices = positions;
            mesh.triangles = triangles;
            mesh.colors32 = colors;
            mesh.uv = new Vector2[6];
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float Unit(uint seed, int index)
        {
            return Hashing.ToUnit(Hashing.Hash(index, 0, seed));
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
