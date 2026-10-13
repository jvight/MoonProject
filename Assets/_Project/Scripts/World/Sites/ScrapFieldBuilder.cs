using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Builds Kestrel-3's scrap field at boot (0.5.5 §2): Art's half-buried hull fragments along the furrow, each mesh
    /// with a box collider on <see cref="Layers.Ground"/> so 07 bumps them like rock. Scenery only: no glow, nothing
    /// to pick up.
    /// </summary>
    public static class ScrapFieldBuilder
    {
        public static void Build(Transform parent, MoonSurface surface, ScrapFieldSettings settings, int seed,
            GameObject[] fragments, HideFlags hideFlags)
        {
            if (parent == null || surface == null || settings == null || fragments == null)
            {
                throw new ArgumentNullException(nameof(parent), "Scrap field build inputs must all be provided.");
            }

            var root = new GameObject("KestrelScrapField") { hideFlags = hideFlags };
            root.transform.SetParent(parent, false);
            List<ScrapPlate> pieces = ScrapFieldPlanner.Plan(surface, settings, fragments.Length, seed);
            for (int i = 0; i < pieces.Count; i++)
            {
                ScrapPlate piece = pieces[i];
                GameObject model = UnityEngine.Object.Instantiate(fragments[piece.Variant], piece.Position,
                    piece.Rotation, root.transform);
                model.name = fragments[piece.Variant].name;
                foreach (Transform node in model.GetComponentsInChildren<Transform>(true))
                {
                    node.gameObject.hideFlags = hideFlags;
                }

                foreach (MeshFilter part in model.GetComponentsInChildren<MeshFilter>(true))
                {
                    part.gameObject.layer = Layers.Ground;
                    Bounds bounds = part.sharedMesh.bounds;
                    var box = part.gameObject.AddComponent<BoxCollider>();
                    box.center = bounds.center;
                    box.size = bounds.size;
                }
            }
        }
    }
}
