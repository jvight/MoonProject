using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// A salvage site as a site recipe draws it, in the site's space (origin on the ground at the anchor, +Z = the
    /// anchor's Forward, away from home; the approach comes from -Z): the skeleton that stays forever, its salvage
    /// pieces and the sheltered spot that holds the site's relic.
    /// </summary>
    internal sealed class SiteRecipe
    {
        private readonly List<SalvagePieceRecipe> _pieces = new List<SalvagePieceRecipe>();

        public LowPolyMeshBuilder Skeleton { get; } = new LowPolyMeshBuilder(6000);

        public IReadOnlyList<SalvagePieceRecipe> Pieces => _pieces;

        /// <summary>Where the site's crew relic rests, in site space.</summary>
        public Vector3 Heart { get; set; }

        /// <summary>
        /// Turns a drawn piece into part of the skeleton (it stays as scenery and is no longer a cut), so a site keeps
        /// its look while giving only the yield it should (M3-16 P9).
        /// </summary>
        public void Settle(SalvagePieceRecipe piece)
        {
            _pieces.Remove(piece);
            int first = Skeleton.TriangleCount;
            Skeleton.Append(piece.Geometry, Matrix4x4.identity);
            for (int t = first; t < Skeleton.TriangleCount; t++)
            {
                PaletteSwatch dead = Dead(Skeleton.SwatchOf(t));
                if (dead != Skeleton.SwatchOf(t))
                {
                    Skeleton.Repaint(new MeshRange(t, 1), dead);
                }
            }
        }

        /// <summary>
        /// The colours that mark a piece worth taking (copper for wiring, violet glass for optics) gone dead on
        /// scenery: faded copper and dark glass, so nothing settled reads as a cut.
        /// </summary>
        private static PaletteSwatch Dead(PaletteSwatch swatch)
        {
            switch (swatch)
            {
                case PaletteSwatch.WarmAccent:
                    return PaletteSwatch.FadedAccent;
                case PaletteSwatch.SkyHorizon:
                    return PaletteSwatch.Charcoal;
                default:
                    return swatch;
            }
        }

        /// <summary>Adds a salvage piece; draw its geometry into the returned recipe in site space.</summary>
        public SalvagePieceRecipe Piece(SalvageMaterial material, Vector3 pivot, Vector3 cutPoint, Vector3 cutNormal,
            bool drag = false)
        {
            var piece = new SalvagePieceRecipe(material, pivot, cutPoint, cutNormal, drag);
            _pieces.Add(piece);
            return piece;
        }
    }
}
