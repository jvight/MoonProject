using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Stands one of Art's wreck prefabs on its World anchor and reads the site contract (docs/features/M3-13): the
    /// "Skeleton" and the pieces still on it become solid on the Prop layer; every
    /// "Salvage_&lt;n&gt;_&lt;Material&gt;[_Drag]" node becomes a <see cref="SalvagePiece"/> with its cut point, a halo
    /// shell and the hidden bundle it folds into;
    /// a drag piece gets its tetherable <see cref="SalvageDrag"/> body, which never collides with its own wreck; the
    /// "Heart" is where the site's relics rest. Runs at initialisation (allocates).
    /// </summary>
    internal static class SalvageSiteBuilder
    {
        private const string SkeletonName = "Skeleton";
        private const string HeartName = "Heart";
        private const string CutPointName = "CutPoint";
        private const string HaloName = "Halo";

        /// <summary>
        /// Builds the site under <paramref name="parent"/> and adds its pieces and drag bodies to the lists. Null when
        /// it stands, else what breaks the contract.
        /// </summary>
        public static string Build(int index, SalvageSiteEntry entry, WorldAnchor anchor, Transform parent,
            SalvageTuning tuning, SalvageCatalog catalog, Material halo, PhysicsMaterial dragMaterial,
            List<SalvagePiece> pieces, List<SalvageDrag> drags, out SalvageSite site)
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
            Transform heart = Child(root, HeartName);
            if (skeleton == null || heart == null)
            {
                return $"the prefab has no '{(skeleton == null ? SkeletonName : HeartName)}' node";
            }

            Collider skeletonCollider = MakeSolid(skeleton);
            if (skeletonCollider == null)
            {
                return $"'{SkeletonName}' has no mesh";
            }

            var built = new SalvageSite(index, entry.AnchorId, root, heart.position, entry.Gate,
                new ComboCounter(tuning.ChainWindow));
            var wreck = new List<Collider> { skeletonCollider };
            int firstDrag = drags.Count;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform node = root.GetChild(i);
                if (!SalvagePieceName.TryParsePiece(node.name, out int number, out SalvageMaterial material,
                        out bool drag))
                {
                    continue;
                }

                string problem = AddPiece(built, node, number, material, drag, parent, tuning, catalog, halo,
                    dragMaterial, wreck, drags);
                if (problem != null)
                {
                    return problem;
                }
            }

            if (built.Pieces.Count == 0)
            {
                return "the prefab has no Salvage_<n>_<Material> pieces";
            }

            // A drag piece hangs into its wreck: it must never be shoved out of it when the tether frees it.
            for (int d = firstDrag; d < drags.Count; d++)
            {
                foreach (Collider part in wreck)
                {
                    Physics.IgnoreCollision(drags[d].Collider, part, true);
                }
            }

            pieces.AddRange(built.Pieces);
            site = built;
            return null;
        }

        private static string AddPiece(SalvageSite site, Transform node, int number, SalvageMaterial material,
            bool drag, Transform parent, SalvageTuning tuning, SalvageCatalog catalog, Material halo,
            PhysicsMaterial dragMaterial, List<Collider> wreck, List<SalvageDrag> drags)
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

            Mesh mesh = filter.sharedMesh;
            Bounds bounds = mesh.bounds;
            int units = SalvageYield.Units(Vector3.Scale(bounds.size, node.localScale), drag, tuning);
            Collider solid = drag ? null : MakeSolid(node);
            if (solid != null)
            {
                wreck.Add(solid);
            }

            MeshRenderer haloRenderer = GlowObject.Create(HaloName, node, mesh, halo);
            float scale = tuning.HaloScale;
            haloRenderer.transform.localPosition = bounds.center * (1f - scale);
            haloRenderer.transform.localScale = Vector3.one * scale;
            haloRenderer.gameObject.layer = node.gameObject.layer;

            GameObject bundle = Object.Instantiate(catalog.Bundle(material), parent);
            bundle.name = "Bundle_" + site.Id + "_" + number;
            SetLayer(bundle.transform, Layers.Pickup);
            bundle.SetActive(false);

            var piece = new SalvagePiece(site, number, material, drag, units, SalvageYield.CutSeconds(units, tuning),
                node, bounds.center, cutPoint, solid, new GlowRenderer(haloRenderer), bundle.transform);
            if (drag)
            {
                SetLayer(node, Layers.Relic);
                var body = node.gameObject.AddComponent<SalvageDrag>();
                body.Setup(piece, bounds, tuning, dragMaterial);
                piece.Drag = body;
                drags.Add(body);
            }

            site.Add(piece);
            return null;
        }

        /// <summary>A static collider of the node's mesh on the Prop layer (wrecks are solid), or null.</summary>
        private static Collider MakeSolid(Transform node)
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
