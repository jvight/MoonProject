using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Stands in for the World: a flat drivable disc at height 0 with a matching Ground collider, a visible floor for
    /// captures, a soft key light and The Peak beyond the rim. Registers ITerrainQuery and IWorldLayout.
    /// </summary>
    public sealed class FlatWorldSystem : MonoBehaviour, IGameSystem, ITerrainQuery, IWorldLayout
    {
        public const float DrivableRadius = 300f;
        private const float FloorThickness = 1f;

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
