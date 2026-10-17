using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Stands in for the World: a flat drivable disc at height 0 with a matching Ground collider, a visible floor for
    /// captures, a soft key light, The Peak beyond the rim, the Kestrel's salvage site (approached heading +Z) and the
    /// opening's places (opening-v2 §4: the wake, the lander 165 m out, the build apron short of it, the ruins east
    /// of it, the midway crate). A steady sky stands in for World's: <see cref="Phase"/> is set by the tests.
    /// Registers ITerrainQuery, IWorldLayout, IWorldAnchors and ISunlight.
    /// </summary>
    public sealed class FlatWorldSystem : MonoBehaviour, IGameSystem, ITerrainQuery, IWorldLayout, IWorldAnchors,
        ISunlight
    {
        public const float DrivableRadius = 300f;
        private const float FloorThickness = 1f;
        private const float AnchorRadius = 8f;
        private const float SiteRadius = 9f;

        private readonly WorldAnchor[] _anchors =
        {
            Site("kestrel", -20f, 170f),
            new WorldAnchor("place.wake", new Vector3(0f, 0f, -4.5f), Vector3.forward, AnchorRadius),
            new WorldAnchor("place.lander", new Vector3(35f, 0f, 160f), Vector3.back, AnchorRadius),
            new WorldAnchor("place.apron", new Vector3(15f, 0f, 140f), Vector3.forward, 14f),
            new WorldAnchor("place.ruins", new Vector3(95f, 0f, 185f), Vector3.back, AnchorRadius),
            new WorldAnchor("place.midway", new Vector3(-40f, 0f, 90f), Vector3.forward, AnchorRadius),
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

        /// <summary>The sky's phase the tests set (sunlit by default: solar arrays feed).</summary>
        public SkyPhase Phase { get; set; } = SkyPhase.Sunlit;

        public float TimeSeconds => 0f;

        public bool IsHoldingEclipse => false;

        public Vector3 SunDirection => Vector3.up;

        public float EclipseFactor => 0f;

        public float EarthPhase => 0f;

        public float SunlightAt(Vector3 position)
        {
            return Phase == SkyPhase.Sunlit ? 1f : 0f;
        }

        public void ReleaseOpeningEclipse()
        {
            Phase = SkyPhase.Sunlit;
        }

        public void Initialize(GameContext context)
        {
            context.Register<ITerrainQuery>(this);
            context.Register<IWorldLayout>(this);
            context.Register<IWorldAnchors>(this);
            context.Register<ISunlight>(this);
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

        /// <summary>A salvage site <paramref name="name"/> at (x, z), approached heading +Z.</summary>
        private static WorldAnchor Site(string name, float x, float z)
        {
            return new WorldAnchor(WorldAnchorIds.SitePrefix + name, new Vector3(x, 0f, z), Vector3.forward,
                SiteRadius);
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
