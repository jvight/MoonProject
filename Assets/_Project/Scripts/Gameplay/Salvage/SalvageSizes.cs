using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Deals a site's size classes to its pieces (docs/features/M3-26 §4.2): the biggest classes go to the biggest
    /// pieces by mesh volume, so the hull plate is the hull-like piece. Pure, shared by the content builder and the
    /// economy tests.
    /// </summary>
    public static class SalvageSizes
    {
        private const string CutPointName = "CutPoint";

        /// <summary>
        /// One entry per Salvage_&lt;n&gt;_&lt;Material&gt; piece of <paramref name="sitePrefab"/>, in piece order,
        /// with <paramref name="classes"/> (one per piece, any order) dealt biggest to biggest.
        /// </summary>
        /// <exception cref="ArgumentException">The classes do not number the site's pieces.</exception>
        public static PieceSizeEntry[] Deal(GameObject sitePrefab, IReadOnlyList<PieceSize> classes)
        {
            if (sitePrefab == null || classes == null)
            {
                throw new ArgumentNullException(sitePrefab == null ? nameof(sitePrefab) : nameof(classes));
            }

            var pieces = new List<(int Number, float Volume)>();
            foreach (Transform node in sitePrefab.transform)
            {
                if (!SalvagePieceName.TryParsePiece(node.name, out int number, out _, out _) ||
                    node.Find(CutPointName) == null || !node.TryGetComponent(out MeshFilter filter) ||
                    filter.sharedMesh == null)
                {
                    continue;
                }

                Vector3 size = Vector3.Scale(filter.sharedMesh.bounds.size, node.localScale);
                pieces.Add((number, Mathf.Abs(size.x * size.y * size.z)));
            }

            if (pieces.Count != classes.Count)
            {
                throw new ArgumentException(
                    $"'{sitePrefab.name}' has {pieces.Count} pieces but {classes.Count} size classes.", nameof(classes));
            }

            var biggestFirst = new List<(int Number, float Volume)>(pieces);
            biggestFirst.Sort((a, b) => b.Volume.CompareTo(a.Volume));
            var sizes = new List<PieceSize>(classes);
            sizes.Sort((a, b) => b.CompareTo(a));
            var dealt = new Dictionary<int, PieceSize>();
            for (int i = 0; i < biggestFirst.Count; i++)
            {
                dealt[biggestFirst[i].Number] = sizes[i];
            }

            var entries = new PieceSizeEntry[pieces.Count];
            for (int i = 0; i < pieces.Count; i++)
            {
                entries[i] = new PieceSizeEntry(pieces[i].Number, dealt[pieces[i].Number]);
            }

            return entries;
        }
    }
}
