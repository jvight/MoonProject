using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Stands one of Art's wreck prefabs on its World anchor and reads the site contract (docs/features/M3-13): the
    /// "Skeleton" and the pieces still on it become solid on the Prop layer; every
    /// "Salvage_&lt;n&gt;_&lt;Material&gt;[_Drag]" node becomes a <see cref="SalvagePiece"/> with its cut point, a halo
    /// shell and the hidden bundle it folds into (a drag piece is one like any other: there is no tether in the
    /// opening). Runs at initialisation (allocates).
    /// </summary>
    internal static class SalvageSiteBuilder
    {
        private const string SkeletonName = "Skeleton";
        private const string CutPointName = "CutPoint";
        private const string HaloName = "Halo";

        /// <summary>
        /// Builds the site under <paramref name="parent"/> and adds its pieces to <paramref name="pieces"/>. Null when
        /// it stands, else what breaks the contract.
        /// </summary>
        public static string Build(int index, SalvageSiteEntry entry, WorldAnchor anchor, Transform parent,
            SalvageTuning tuning, SalvageCatalog catalog, Material halo, List<SalvagePiece> pieces,
            out SalvageSite site)
        {
            site = null;
            Vector3 forward = anchor.Forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            GameObject instance = Object.Instantiate(entry.Prefab, anchor.Position, Quaternion.LookRotation(forward),
                parent);
            instance.name = entry.Prefab.name;
            Transform root = instance.transform;
            Transform skeleton = Child(root, SkeletonName);
            if (skeleton == null)
            {
                return $"the prefab has no '{SkeletonName}' node";
            }

            if (MakeSolid(skeleton) == null)
            {
                return $"'{SkeletonName}' has no mesh";
            }

            var built = new SalvageSite(index, entry.Id, root, anchor.Radius, new ComboCounter(tuning.ChainWindow));
            for (int i = 0; i < root.childCount; i++)
            {
                Transform node = root.GetChild(i);
                if (!SalvagePieceName.TryParsePiece(node.name, out int number, out SalvageMaterial material, out _))
                {
                    continue;
                }

                string problem = AddPiece(built, entry, node, number, material, parent, tuning, catalog, halo);
                if (problem != null)
                {
                    return problem;
                }
            }

            if (built.Pieces.Count == 0)
            {
                return "the prefab has no Salvage_<n>_<Material> pieces";
            }

            pieces.AddRange(built.Pieces);
            site = built;
            return null;
        }

        private static string AddPiece(SalvageSite site, SalvageSiteEntry entry, Transform node, int number,
            SalvageMaterial material, Transform parent, SalvageTuning tuning, SalvageCatalog catalog, Material halo)
        {
            if (site.Find(number) != null)
            {
                return $"two pieces are numbered {number}";
            }

            if (!node.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
            {
                return $"'{node.name}' has no mesh";
            }

            Transform cutPoint = Child(node, CutPointName);
            if (cutPoint == null)
            {
                return $"'{node.name}' has no '{CutPointName}' node";
            }

            if (!entry.TryGetCutLine(number, out Vector3[] cutLine))
            {
                return $"'{node.name}' has no cut line of {PieceCutLine.MinPoints}-{PieceCutLine.MaxPoints} points " +
                       "in the catalog (run Gameplay/Content)";
            }

            if (!entry.TryGetSize(number, out PieceSize size))
            {
                return $"'{node.name}' has no size class in the catalog (run Gameplay/Content)";
            }

            Mesh mesh = filter.sharedMesh;
            Bounds bounds = mesh.bounds;
            int units = SalvageYield.Units(size, entry.YieldScale, tuning);
            Collider solid = MakeSolid(node);
            MeshRenderer haloRenderer = GlowObject.Create(HaloName, node, mesh, halo);
            float scale = tuning.HaloScale;
            haloRenderer.transform.localPosition = bounds.center * (1f - scale);
            haloRenderer.transform.localScale = Vector3.one * scale;
            haloRenderer.gameObject.layer = node.gameObject.layer;

            GameObject bundle = Object.Instantiate(catalog.Bundle(material), parent);
            bundle.name = "Bundle_" + site.Id + "_" + number;
            SetLayer(bundle.transform, Layers.Pickup);
            bundle.SetActive(false);

            site.Add(new SalvagePiece(site, number, material, size, units, tuning.Size(size).Segments,
                SalvageYield.CutSeconds(size, 1f, tuning), node, bounds.center, cutPoint, cutLine, solid,
                new GlowRenderer(haloRenderer), bundle.transform));
            return null;
        }

        /// <summary>A static collider of the node's mesh on the Prop layer (wrecks are solid), or null.</summary>
        internal static Collider MakeSolid(Transform node)
        {
            if (!node.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
            {
                return null;
            }

            node.gameObject.layer = Layers.Prop;
            var solid = node.gameObject.AddComponent<MeshCollider>();
            solid.sharedMesh = filter.sharedMesh;
            return solid;
        }

        private static Transform Child(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>Puts <paramref name="root"/> and everything under it on <paramref name="layer"/>.</summary>
        public static void SetLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayer(root.GetChild(i), layer);
            }
        }
    }
}
