using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Builds Kestrel-3's scrap field at boot (0.5.5 §2): Art's hull fragments along the furrow, dust-tinted and with
    /// their cut point removed, each mesh with a box collider on <see cref="Layers.Ground"/> so 07 bumps them like
    /// rock. Scenery only: no glow, nothing to cut or pick up.
    /// </summary>
    public static class ScrapFieldBuilder
    {
        // Only Gameplay registers cut points, on the 4 cuttable fragments it places; the scenery copies lose theirs.
        private const string CutPointName = "CutPoint";
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

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
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < pieces.Count; i++)
            {
                ScrapPlate piece = pieces[i];
                GameObject model = UnityEngine.Object.Instantiate(fragments[piece.Variant], piece.Position,
                    piece.Rotation, root.transform);
                model.name = fragments[piece.Variant].name;
                model.transform.localScale = Vector3.one * piece.Scale;
                Transform cutPoint = model.transform.Find(CutPointName);
                if (cutPoint != null)
                {
                    Release(cutPoint.gameObject);
                }

                foreach (Transform node in model.GetComponentsInChildren<Transform>(true))
                {
                    node.gameObject.hideFlags = hideFlags;
                }

                block.Clear();
                block.SetColor(BaseColorId, settings.Dust);
                foreach (Renderer part in model.GetComponentsInChildren<Renderer>(true))
                {
                    part.SetPropertyBlock(block);
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

        private static void Release(GameObject node)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(node);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(node);
            }
        }
    }
}
