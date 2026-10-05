using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Tuning root of the World domain: the world seed plus every number for the surface shape. Read-only at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/World Settings", fileName = "WorldSettings")]
    public sealed class WorldSettings : ScriptableObject
    {
        /// <summary>Seed of the shipped world; tests pin their expectations to it.</summary>
        public const int DefaultSeed = 1337;

        [Tooltip("Master seed: craters, noise, jitter and scatter all derive from it. Same seed = same world.")]
        [SerializeField] private int _seed = DefaultSeed;

        [Tooltip("Shape of the moon surface: basin, rim, The Peak, dunes, craters and play features.")]
        [SerializeField] private SurfaceSettings _surface = new SurfaceSettings();

        public int Seed => _seed;

        public SurfaceSettings Surface => _surface;

        /// <summary>Builds the analytic surface for these settings (throws on invalid settings).</summary>
        public MoonSurface CreateSurface()
        {
            return new MoonSurface(_surface, _seed);
        }

        private void OnValidate()
        {
            string error = _surface.Validate();
            if (error != null)
            {
                Debug.LogError($"{nameof(WorldSettings)} '{name}': {error}", this);
            }
        }
    }
}
