using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Stands in for the World: a flat drivable disc at height 0 with a matching Ground collider, a visible floor for
    /// captures, a soft key light, The Peak beyond the rim, and the canyon's anchors laid out flat to the west (no
    /// chasm: the tests drive straight to them). Registers ITerrainQuery, IWorldLayout and IWorldAnchors.
    /// </summary>
    public sealed class FlatWorldSystem : MonoBehaviour, IGameSystem, ITerrainQuery, IWorldLayout, IWorldAnchors
    {
        public const float DrivableRadius = 300f;
        private const float FloorThickness = 1f;
        private const float AnchorRadius = 8f;

        private static readonly Vector3 West = new Vector3(-1f, 0f, 0f);

        private readonly WorldAnchor[] _anchors =
        {
            new WorldAnchor(WorldAnchorIds.CanyonMouth, new Vector3(-170f, 0f, 60f), West, AnchorRadius),
            new WorldAnchor(WorldAnchorIds.CanyonLip, new Vector3(-180f, 0f, 60f), West, AnchorRadius),
            new WorldAnchor(WorldAnchorIds.CanyonLanding, new Vector3(-205f, 0f, 60f), West, AnchorRadius),
            new WorldAnchor(WorldAnchorIds.CanyonLedge, new Vector3(-200f, 0f, 95f), West, AnchorRadius),
            new WorldAnchor(WorldAnchorIds.CanyonAlcovePrefix + 0, new Vector3(-225f, 0f, 75f), West, AnchorRadius),
            new WorldAnchor(WorldAnchorIds.CanyonAlcovePrefix + 1, new Vector3(-245f, 0f, 45f), West, AnchorRadius),
            new WorldAnchor(WorldAnchorIds.CanyonAlcovePrefix + 2, new Vector3(-262f, 0f, 78f), West, AnchorRadius),
            new WorldAnchor(WorldAnchorIds.CanyonTerminus, new Vector3(-275f, 0f, 60f), West, AnchorRadius),
            new WorldAnchor(WorldAnchorIds.CanyonExit, new Vector3(-200f, 0f, 30f), Vector3.right, AnchorRadius),
        };

        public Rect PlayableArea
        {
            get
            {
                float half = DrivableRadius * 0.70710678f;
                return new Rect(-half, -half, half * 2f, half * 2f);
            }
        }

        public Vector3 BasePosition => Vector3.zero;

        public Vector3 PeakPosition => new Vector3(0f, 150f, 440f);

        public Vector3 EarthDirection => new Vector3(0f, 0.5f, 0.866f);

        public int Count => _anchors.Length;

        public static FlatWorldSystem Create()
        {
            var host = new GameObject("FlatWorld");
            var world = host.AddComponent<FlatWorldSystem>();
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.layer = Layers.Ground;
            floor.transform.SetParent(host.transform, false);
            floor.transform.localPosition = new Vector3(0f, -FloorThickness * 0.5f, 0f);
            floor.transform.localScale = new Vector3(DrivableRadius * 2f, FloorThickness, DrivableRadius * 2f);

            var light = new GameObject("KeyLight").AddComponent<Light>();
            light.transform.SetParent(host.transform, false);
            light.type = LightType.Directional;
            light.intensity = 0.6f;
            light.color = new Color(0.75f, 0.78f, 1f);
            light.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
            return world;
        }

        public void Initialize(GameContext context)
        {
            context.Register<ITerrainQuery>(this);
            context.Register<IWorldLayout>(this);
            context.Register<IWorldAnchors>(this);
        }

        public WorldAnchor Get(int index)
        {
            return _anchors[index];
        }

        public bool TryGet(string id, out WorldAnchor anchor)
        {
            foreach (WorldAnchor candidate in _anchors)
            {
                if (string.Equals(candidate.Id, id, StringComparison.Ordinal))
                {
                    anchor = candidate;
                    return true;
                }
            }

            anchor = default;
            return false;
        }

        /// <summary>The anchor <paramref name="id"/> (tests drive to it); throws when there is none.</summary>
        public WorldAnchor Anchor(string id)
        {
            if (!TryGet(id, out WorldAnchor anchor))
            {
                throw new ArgumentException($"The flat world has no anchor '{id}'.", nameof(id));
            }

            return anchor;
        }

        public bool IsDrivable(float x, float z)
        {
            return x * x + z * z <= DrivableRadius * DrivableRadius;
        }

        public float SampleHeight(float x, float z)
        {
            return 0f;
        }

        public Vector3 SampleNormal(float x, float z)
        {
            return Vector3.up;
        }
    }
}
