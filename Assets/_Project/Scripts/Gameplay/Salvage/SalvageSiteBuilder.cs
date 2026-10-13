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
    /// "Heart" is where the site's relics rest. A derelict (0.5.5 §1.7) has no heart and no crew crate, and may have
    /// no pieces. Runs at initialisation (allocates).
    /// </summary>
    internal static class SalvageSiteBuilder
    {
        private const string SkeletonName = "Skeleton";
        private const string HeartName = "Heart";
        private const string CutPointName = "CutPoint";
        private const string HaloName = "Halo";
        private const string LandmarkName = "Landmark";
        private const string LandmarkPilotName = "LandmarkPilot";
        private const string CrateName = "CrewCrate";
        private const string LidName = "Lid";
        private const string SealName = "Seal";

        /// <summary>
        /// Builds the site under <paramref name="parent"/> and adds its pieces and drag bodies to the lists. Null when
        /// it stands, else what breaks the contract.
        /// </summary>
        /// <param name="derelict">A derelict: no heart, no crew crate, any number of pieces.</param>
        public static string Build(int index, SalvageSiteEntry entry, bool derelict, WorldAnchor anchor,
            Transform parent, ITerrainQuery terrain, SalvageTuning tuning, SalvageCatalog catalog, Material halo,
            PhysicsMaterial dragMaterial, List<SalvagePiece> pieces, List<SalvageDrag> drags, out SalvageSite site)
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
            Transform heart = derelict ? root : Child(root, HeartName);
            if (skeleton == null || heart == null)
            {
                return $"the prefab has no '{(skeleton == null ? SkeletonName : HeartName)}' node";
            }

            Collider skeletonCollider = MakeSolid(skeleton);
            if (skeletonCollider == null)
            {
                return $"'{SkeletonName}' has no mesh";
            }

            return Stand(index, entry, derelict, root, heart.position, anchor.Radius,
                new List<Collider> { skeletonCollider }, parent, terrain, tuning, catalog, halo, dragMaterial, pieces,
                drags, out site);
        }

        /// <summary>
        /// Reads a scrap field (0.5.5 §2) the caller has laid out under <paramref name="root"/>: pieces only (no
        /// skeleton, heart or crew crate), each a Salvage_&lt;n&gt;_&lt;Material&gt; node.
        /// </summary>
        public static string BuildScrap(int index, SalvageSiteEntry entry, Transform root, float radius,
            Transform parent, ITerrainQuery terrain, SalvageTuning tuning, SalvageCatalog catalog, Material halo,
            PhysicsMaterial dragMaterial, List<SalvagePiece> pieces, List<SalvageDrag> drags, out SalvageSite site)
        {
            string problem = Stand(index, entry, true, root, root.position, radius, new List<Collider>(), parent,
                terrain, tuning, catalog, halo, dragMaterial, pieces, drags, out site);
            return problem ?? (site.Pieces.Count == 0 ? "the scrap field has no pieces" : null);
        }

        private static string Stand(int index, SalvageSiteEntry entry, bool derelict, Transform root, Vector3 heart,
            float radius, List<Collider> wreck, Transform parent, ITerrainQuery terrain, SalvageTuning tuning,
            SalvageCatalog catalog, Material halo, PhysicsMaterial dragMaterial, List<SalvagePiece> pieces,
            List<SalvageDrag> drags, out SalvageSite site)
        {
            site = null;
            var built = new SalvageSite(index, entry.Id, root, radius, heart, entry.Gate,
                new ComboCounter(tuning.ChainWindow), entry.HeartUnderPieces);
            string problem = (derelict ? null : AddCrate(built, entry, root)) ?? AddProps(entry, root);
            if (problem != null)
            {
                return problem;
            }

            Transform landmark = Child(root, LandmarkName);
            if (landmark != null)
            {
                Transform pilot = Child(landmark, LandmarkPilotName);
                Renderer lamp = pilot != null ? pilot.GetComponent<Renderer>() : null;
                if (lamp == null)
                {
                    return $"'{LandmarkName}' has no '{LandmarkPilotName}' node with a renderer";
                }

                built.AttachPilot(lamp, tuning);
            }

            int firstDrag = drags.Count;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform node = root.GetChild(i);
                if (!SalvagePieceName.TryParsePiece(node.name, out int number, out SalvageMaterial material,
                        out bool drag))
                {
                    continue;
                }

                problem = AddPiece(built, entry, node, number, material, drag, entry.YieldScale, parent,
                    terrain, tuning, catalog, halo, dragMaterial, wreck, drags);
                if (problem != null)
                {
                    return problem;
                }
            }

            if (built.Pieces.Count == 0 && !derelict)
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

        /// <summary>Art's props on their sockets with identity, solid to 07; null when they all stand.</summary>
        private static string AddProps(SalvageSiteEntry entry, Transform root)
        {
            for (int i = 0; i < entry.Props.Count; i++)
            {
                SocketProp prop = entry.Props[i];
                Transform socket = Child(root, prop.Socket);
                if (socket == null)
                {
                    return $"the prefab has no '{prop.Socket}' node for its prop '{prop.Prefab.name}'";
                }

                GameObject instance = Object.Instantiate(prop.Prefab, socket);
                instance.name = prop.Prefab.name;
                instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (MeshFilter mesh in instance.GetComponentsInChildren<MeshFilter>())
                {
                    MakeSolid(mesh.transform);
                }
            }

            return null;
        }

        /// <summary>The crew's supply crate beside a wreck's heart (M3-25 T4); null when it stands.</summary>
        private static string AddCrate(SalvageSite site, SalvageSiteEntry entry, Transform root)
        {
            Transform crate = Child(root, CrateName);
            Transform lid = crate != null ? Child(crate, LidName) : null;
            Transform cut = lid != null ? Child(lid, CutPointName) : null;
            Transform seal = lid != null ? Child(lid, SealName) : null;
            Renderer sealGlow = seal != null ? seal.GetComponent<Renderer>() : null;
            if (cut == null || sealGlow == null)
            {
                return $"the prefab has no '{CrateName}/{LidName}' with its '{CutPointName}' and '{SealName}' glow " +
                       "(the crew's supply crate, M3-25 T4)";
            }

            site.Crate = new CrewCrate(site, entry.Crate, crate, lid, cut, sealGlow);
            return null;
        }

        private static string AddPiece(SalvageSite site, SalvageSiteEntry entry, Transform node, int number,
            SalvageMaterial material, bool drag, int yieldScale, Transform parent, ITerrainQuery terrain,
            SalvageTuning tuning, SalvageCatalog catalog, Material halo, PhysicsMaterial dragMaterial,
            List<Collider> wreck, List<SalvageDrag> drags)
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
            int units = SalvageYield.Units(size, yieldScale, tuning);
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

            var piece = new SalvagePiece(site, number, material, drag, size, units, tuning.Size(size).Segments,
                SalvageYield.CutSeconds(size, 1f, tuning),
                node, bounds.center, cutPoint, cutLine, solid, new GlowRenderer(haloRenderer), bundle.transform);
            if (drag)
            {
                SetLayer(node, Layers.Relic);
                var body = node.gameObject.AddComponent<SalvageDrag>();
                body.Setup(piece, bounds, tuning, dragMaterial, terrain);
                piece.Drag = body;
                drags.Add(body);
            }

            site.Add(piece);
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
