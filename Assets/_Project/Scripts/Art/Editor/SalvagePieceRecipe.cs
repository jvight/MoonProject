using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// One salvage point of a site, modelled in the site's space: its material, the attach point it is cut from
    /// (its pivot), where 07's beam aims and which way the cut face looks, and whether it must be dragged clear first.
    /// </summary>
    internal sealed class SalvagePieceRecipe
    {
        public SalvagePieceRecipe(SalvageMaterial material, Vector3 pivot, Vector3 cutPoint, Vector3 cutNormal,
            bool drag)
        {
            if (cutNormal.sqrMagnitude < 1e-6f)
            {
                throw new System.ArgumentException("A cut face needs a direction.", nameof(cutNormal));
            }

            Material = material;
            Pivot = pivot;
            CutPoint = cutPoint;
            CutNormal = cutNormal.normalized;
            Drag = drag;
        }

        public SalvageMaterial Material { get; }

        /// <summary>The attach point in site space; the piece's node pivots here.</summary>
        public Vector3 Pivot { get; }

        /// <summary>Where the beam aims and sparks, in site space.</summary>
        public Vector3 CutPoint { get; }

        /// <summary>Out of the cut face, in site space (the CutPoint's +Z).</summary>
        public Vector3 CutNormal { get; }

        /// <summary>A big piece that must be tethered clear before it can be cut.</summary>
        public bool Drag { get; }

        /// <summary>The piece's mesh, built in site space (the builder moves it onto the pivot).</summary>
        public LowPolyMeshBuilder Geometry { get; } = new LowPolyMeshBuilder(400);
    }
}
