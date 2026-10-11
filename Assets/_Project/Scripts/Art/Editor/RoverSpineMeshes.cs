using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Cell Pack II's battery spine (docs/features/M3-20, GDD v3 §4.4's back slot): two fat cream-enamel cells lying
    /// end to end on a metal rail along the lid's right edge, ringed with brass bands, a brass terminal on each, so 07's
    /// back reads as carrying more power. Built in SpineSocket space: +Y up, +Z forward, the rail on the socket. Low
    /// and rounded (the head and the antenna stay 07's tallest parts), never spiky.
    /// </summary>
    internal static class RoverSpineMeshes
    {
        private const float RailWidth = 0.1f;
        private const float RailHeight = 0.012f;
        private const float RailLength = 0.44f;
        private const float CellRadius = 0.048f;
        private const float CellLength = 0.19f;
        private const float CellOffset = 0.1f;
        private const float BandRadius = 0.052f;
        private const float BandWidth = 0.014f;
        private const float TerminalRadius = 0.013f;
        private const int Sides = 12;

        /// <summary>Lays a prism's local Y along +Z.</summary>
        private static readonly Vector3 AlongZ = new Vector3(90f, 0f, 0f);

        public static LowPolyMeshBuilder Spine()
        {
            var b = new LowPolyMeshBuilder(480);
            b.Box(At(0f, RailHeight * 0.5f, 0f), new Vector3(RailWidth, RailHeight, RailLength), PaletteSwatch.Metal,
                0.003f);
            float cellY = RailHeight + CellRadius;
            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * CellOffset;
                b.Prism(At(new Vector3(0f, cellY, z), AlongZ), CellRadius, CellLength, Sides, PaletteSwatch.Cream);
                for (int end = -1; end <= 1; end += 2)
                {
                    float band = z + end * (CellLength * 0.5f - BandWidth);
                    b.Prism(At(new Vector3(0f, cellY, band), AlongZ), BandRadius, BandWidth, Sides,
                        PaletteSwatch.Honey, false);
                }

                b.Icosphere(At(0f, cellY + CellRadius, z), TerminalRadius, 0, PaletteSwatch.Honey);
            }

            return b;
        }
    }
}
