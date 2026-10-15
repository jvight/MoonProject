using System;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World
{
    /// <summary>
    /// Builds the billboard quad mesh used by <c>LofiEarth.shader</c> to ray-trace a smooth, stylized-real Earth
    /// sphere in the sky, and provides the linear palette swatch colours (<see cref="FaceColor"/>) faded toward the
    /// sky's pale haze.
    /// </summary>
    public static class EarthMeshBuilder
    {
        // Quad half-extent in disc radii (> 1 so sub-pixel edge anti-aliasing and the atmosphere rim are never clipped).
        private const float QuadExtent = 1.08f;

        // The mesh is positioned by the shader around the camera, so CPU culling must never reject it.
        private const float NeverCulledExtent = 1e7f;

        public static Mesh Build(SkySettings sky, int seed)
        {
            if (sky == null)
            {
                throw new ArgumentNullException(nameof(sky));
            }

            _ = seed;
            Vector3[] positions =
            {
                new Vector3(-QuadExtent, -QuadExtent, 0f),
                new Vector3(QuadExtent, -QuadExtent, 0f),
                new Vector3(-QuadExtent, QuadExtent, 0f),
                new Vector3(QuadExtent, QuadExtent, 0f),
            };
            Vector3[] normals =
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
            };
            int[] indices = { 0, 2, 1, 2, 3, 1 };

            var mesh = new Mesh { name = "Earth" };
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetTriangles(indices, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * NeverCulledExtent);
            return mesh;
        }

        /// <summary>
        /// Linear colour of an Earth surface element painted in <paramref name="swatch"/>: the swatch faded toward the
        /// sky's haze (mixed in sRGB, as the eye compares them).
        /// </summary>
        public static Color FaceColor(PaletteSwatch swatch, SkySettings sky)
        {
            if (sky == null)
            {
                throw new ArgumentNullException(nameof(sky));
            }

            return Color.Lerp(Palette.Get(swatch), sky.EarthHaze, sky.EarthPaleness).linear;
        }
    }
}

