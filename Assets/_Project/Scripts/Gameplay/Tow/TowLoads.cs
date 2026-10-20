using System;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Stands the opening's towable loads in the world (docs/design/opening-v3-purpose.md R6): each spot's Art prefab
    /// under the gameplay root, set down level in its frame with its collider's underside on the ground, given a body
    /// and made a <see cref="TowableBody"/> on 07's winch cable.
    /// </summary>
    public static class TowLoads
    {
        /// <summary>The prefab node where the cable meets the load.</summary>
        public const string HitchNode = "TowHitch";

        /// <summary>Stands <paramref name="spot"/>'s load by <paramref name="frame"/>; throws when Art's prefab is wrong.</summary>
        public static TowableBody Stand(TowLoadSpot spot, Transform frame, Transform parent, ITerrainQuery terrain,
            TowSettings settings)
        {
            if (frame == null)
            {
                throw new InvalidOperationException(
                    $"Towable load '{spot.Id}' stands by '{spot.AnchorId}', which is no world anchor or ruin piece.");
            }

            GameObject prefab = spot.Prefab != null ? spot.Prefab : throw new InvalidOperationException(
                $"Towable load '{spot.Id}' has no prefab (OpeningTuning's tow loads).");
            Quaternion heading = SupplyCaches.Heading(frame);
            Vector3 spotAt = frame.position + heading * new Vector3(spot.Offset.x, 0f, spot.Offset.z);
            GameObject load = Object.Instantiate(prefab, parent);
            load.name = spot.Id;
            load.transform.SetPositionAndRotation(SurfaceRules.OnSurface(terrain, spotAt.x, spotAt.z),
                heading * Quaternion.Euler(0f, spot.Yaw, 0f) * prefab.transform.localRotation);
            SetLayer(load.transform, Layers.Prop);
            Transform hitch = Find(load.transform, HitchNode) ?? throw new InvalidOperationException(
                $"Towable load '{spot.Id}': Art's prefab has no '{HitchNode}' node.");
            Collider[] colliders = load.GetComponentsInChildren<Collider>();
            if (colliders.Length == 0)
            {
                throw new InvalidOperationException($"Towable load '{spot.Id}': Art's prefab has no collider.");
            }

            // Rest its underside on the ground, not its pivot.
            Physics.SyncTransforms();
            float underside = float.PositiveInfinity;
            foreach (Collider collider in colliders)
            {
                underside = Mathf.Min(underside, collider.bounds.min.y);
            }

            load.transform.position += Vector3.up * (load.transform.position.y - underside);
            var body = load.AddComponent<Rigidbody>();
            return new TowableBody(spot.Id, body, hitch, spot.Mass, settings);
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            return null;
        }

        private static void SetLayer(Transform node, int layer)
        {
            node.gameObject.layer = layer;
            for (int i = 0; i < node.childCount; i++)
            {
                SetLayer(node.GetChild(i), layer);
            }
        }
    }
}
