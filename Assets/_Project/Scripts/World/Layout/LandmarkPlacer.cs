using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Places Art's landmark kit on the ground at boot (M3-18): the route posts along the road to the canyon with
    /// faintly glinting reflectors (L2), the broken cable bridge at the chasm with its far anchor's lamp lit warm (L4)
    /// and the antenna farm's toppled dish frames (L7). Meshes only, no colliders: they never block 07's lanes.
    /// </summary>
    public static class LandmarkPlacer
    {
        private const string ReflectorName = "Reflector";
        private const string LampName = "Lamp";
        private const uint DishSalt = 0x68E31DA4u;

        // The dish frames lie around the farm's centre a little off even spacing, between half and all of the spread.
        private const float DishAngleJitter = 0.4f;
        private const float DishNearestShare = 0.5f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        /// <summary>The kit's prefabs, from Generated/Art/Landmarks.</summary>
        public static void Build(Transform parent, MoonSurface surface, IWorldAnchors anchors, LandmarkSettings settings,
            int seed, LandmarkPrefabs prefabs, HideFlags hideFlags)
        {
            if (parent == null || surface == null || anchors == null || settings == null || prefabs == null)
            {
                throw new ArgumentNullException(nameof(parent), "Landmark build inputs must all be provided.");
            }

            var root = new GameObject("Landmarks") { hideFlags = hideFlags };
            root.transform.SetParent(parent, false);
            var block = new MaterialPropertyBlock();
            PlaceRoutePosts(root.transform, surface, settings, prefabs.RoutePost, block, hideFlags);
            PlaceBridge(root.transform, surface, anchors, settings, prefabs, block, hideFlags);
            PlaceAntennaFarm(root.transform, surface, anchors, settings, seed, prefabs.DishFrames, hideFlags);
        }

        private static void PlaceRoutePosts(Transform root, MoonSurface surface, LandmarkSettings settings,
            GameObject prefab, MaterialPropertyBlock block, HideFlags hideFlags)
        {
            Vector2 along = MoonSurface.BearingToDirection(settings.RouteBearing);
            var right = new Vector2(along.y, -along.x);
            foreach (float distance in settings.RoutePostDistances)
            {
                Vector2 spot = along * distance + right * settings.RoutePostSide;
                GameObject post = Place(root, prefab, Ground(surface, spot), new Vector3(along.x, 0f, along.y),
                    hideFlags);
                Glow(post.transform, ReflectorName, settings.ReflectorGlow, block);
            }
        }

        private static void PlaceBridge(Transform root, MoonSurface surface, IWorldAnchors anchors,
            LandmarkSettings settings, LandmarkPrefabs prefabs, MaterialPropertyBlock block, HideFlags hideFlags)
        {
            WorldAnchor lip = Require(anchors, WorldAnchorIds.CanyonLip);
            WorldAnchor landing = Require(anchors, WorldAnchorIds.CanyonLanding);
            Vector3 across = landing.Position - lip.Position;
            across.y = 0f;
            across.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, across) * settings.BridgeSide;
            Vector3 near = lip.Position + side;
            Place(root, prefabs.BridgeNear, Ground(surface, new Vector2(near.x, near.z)), across, hideFlags);
            Vector3 far = landing.Position + side;
            GameObject anchor = Place(root, prefabs.BridgeFar, Ground(surface, new Vector2(far.x, far.z)), across,
                hideFlags);
            Glow(anchor.transform, LampName, settings.BridgeLampGlow, block);
            Vector3 middle = (lip.Position + landing.Position) * 0.5f + side;
            Place(root, prefabs.BridgeCable, Ground(surface, new Vector2(middle.x, middle.z)), across, hideFlags);
        }

        private static void PlaceAntennaFarm(Transform root, MoonSurface surface, IWorldAnchors anchors,
            LandmarkSettings settings, int seed, GameObject[] frames, HideFlags hideFlags)
        {
            WorldAnchor farm = Require(anchors, WorldAnchors.AntennaFarm);
            uint dishSeed = Hashing.Mix((uint)seed ^ DishSalt);
            for (int i = 0; i < frames.Length; i++)
            {
                float angle = (i + DishAngleJitter * Hashing.ToSigned(Hashing.Hash(i, 0, dishSeed))) * Mathf.PI * 2f
                    / frames.Length;
                float reach = settings.DishSpread
                    * Mathf.Lerp(DishNearestShare, 1f, Hashing.ToUnit(Hashing.Hash(i, 1, dishSeed)));
                var spot = new Vector2(farm.Position.x + Mathf.Sin(angle) * reach,
                    farm.Position.z + Mathf.Cos(angle) * reach);
                float yaw = Hashing.ToUnit(Hashing.Hash(i, 2, dishSeed)) * Mathf.PI * 2f;
                Place(root, frames[i], Ground(surface, spot), new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)),
                    hideFlags);
            }
        }

        private static void Glow(Transform model, string node, float glow, MaterialPropertyBlock block)
        {
            Transform target = model.Find(node);
            Renderer glowing = target != null ? target.GetComponent<Renderer>() : null;
            if (glowing == null)
            {
                throw new InvalidOperationException($"{model.name} has no '{node}' renderer to light.");
            }

            block.Clear();
            block.SetVector(EmissionColorId, new Vector4(glow, glow, glow, 1f));
            glowing.SetPropertyBlock(block);
        }

        private static GameObject Place(Transform root, GameObject prefab, Vector3 position, Vector3 forward,
            HideFlags hideFlags)
        {
            GameObject model = UnityEngine.Object.Instantiate(prefab, position, Quaternion.LookRotation(forward), root);
            model.name = prefab.name;
            foreach (Transform node in model.GetComponentsInChildren<Transform>(true))
            {
                node.gameObject.hideFlags = hideFlags;
            }

            return model;
        }

        private static Vector3 Ground(MoonSurface surface, Vector2 spot)
        {
            return new Vector3(spot.x, surface.SampleHeight(spot.x, spot.y), spot.y);
        }

        private static WorldAnchor Require(IWorldAnchors anchors, string id)
        {
            if (!anchors.TryGet(id, out WorldAnchor anchor))
            {
                throw new InvalidOperationException($"The landmarks need the world anchor '{id}'.");
            }

            return anchor;
        }
    }
}
