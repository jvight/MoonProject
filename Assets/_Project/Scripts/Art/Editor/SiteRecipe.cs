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
