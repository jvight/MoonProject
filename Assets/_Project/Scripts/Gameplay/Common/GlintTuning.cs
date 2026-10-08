using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How a pickup's glint reads from far away (<see cref="PickupGlints"/>): friends' parts, relay parts, tapes, crew
    /// log caches and the loose salvage along a debris trail. Created by the Gameplay/Tuning builder; runtime code only
    /// reads it.
    /// </summary>
    public sealed class GlintTuning : ScriptableObject
    {
        [Header("Glint (reads on the horizon)")]
        [Tooltip("Half-size (m) of a piece's glint when seen up close.")]
        [Range(0.05f, 2f)] [SerializeField] private float _glintSize = 0.45f;

        [Tooltip("A glint never looks smaller than this half-angle (degrees), so a far pickup still sparkles.")]
        [Range(0.05f, 2f)] [SerializeField] private float _glintMinAngle = 0.45f;

        [Tooltip("Glint brightness at full strength (a touch above 1 so bloom gives a far pickup a soft sparkle).")]
        [Range(0f, 4f)] [SerializeField] private float _glintIntensity = 1.2f;

        [Tooltip("Height (m) of the glint above the piece's centre.")]
        [Range(0f, 1f)] [SerializeField] private float _glintLift = 0.12f;

        [Tooltip("Metres the glint is drawn toward the camera, so its own piece never hides it.")]
        [Range(0f, 2f)] [SerializeField] private float _glintPull = 0.6f;

        [Tooltip("Closer than this (m) the glint is gone and the model itself takes over...")]
        [Range(0f, 30f)] [SerializeField] private float _glintFadeNear = 4f;

        [Tooltip("...and from this distance (m) on it shines fully.")]
        [Range(1f, 60f)] [SerializeField] private float _glintFadeFar = 12f;

        [Tooltip("Glints farther than this (m) from the camera fade out over the last fifth.")]
        [Range(20f, 600f)] [SerializeField] private float _glintMaxDistance = 170f;

        [Tooltip("Twinkles per second of a glint (each piece has its own phase).")]
        [Range(0.05f, 3f)] [SerializeField] private float _glintTwinkleRate = 0.35f;

        [Tooltip("How much a glint dims between twinkles (0 = steady).")]
        [Range(0f, 1f)] [SerializeField] private float _glintTwinkleDepth = 0.55f;

        public float GlintSize => _glintSize;
        public float GlintMinAngle => _glintMinAngle;
        public float GlintIntensity => _glintIntensity;
        public float GlintLift => _glintLift;
        public float GlintPull => _glintPull;
        public float GlintFadeNear => _glintFadeNear;
        public float GlintFadeFar => Mathf.Max(_glintFadeFar, _glintFadeNear + 0.01f);
        public float GlintMaxDistance => _glintMaxDistance;
        public float GlintTwinkleRate => _glintTwinkleRate;
        public float GlintTwinkleDepth => _glintTwinkleDepth;
    }
}
