using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How a crew log cache is found: how close 07 must come, its glint and 07's glance before, its warm puff after.
    /// Created by the Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class LogCacheTuning : ScriptableObject
    {
        [Tooltip("07 opens a cache by coming within this many metres of it.")]
        [Range(1f, 15f)] [SerializeField] private float _reachRadius = 4f;

        [Tooltip("Height (m) above the cache's root where its glint and puff of light sit (inside the open lid).")]
        [Range(0f, 2f)] [SerializeField] private float _glowHeight = 0.3f;

        [Tooltip("07 glances at an unopened cache within this many metres.")]
        [Range(1f, 60f)] [SerializeField] private float _glanceRadius = 18f;

        [Tooltip("Seconds of the warm puff of light when a cache opens.")]
        [Range(0.05f, 4f)] [SerializeField] private float _flashDuration = 1.2f;

        [Tooltip("Radius (m) of that puff as it swells (from, to).")]
        [SerializeField] private Vector2 _flashRadius = new Vector2(0.2f, 1.1f);

        [Tooltip("Brightness of that puff.")]
        [Range(0f, 4f)] [SerializeField] private float _flashIntensity = 1f;

        public float ReachRadius => _reachRadius;
        public float GlowHeight => _glowHeight;
        public float GlanceRadius => _glanceRadius;
        public float FlashDuration => _flashDuration;
        public Vector2 FlashRadius => _flashRadius.x <= _flashRadius.y ? _flashRadius
            : new Vector2(_flashRadius.y, _flashRadius.x);
        public float FlashIntensity => _flashIntensity;
    }
}
