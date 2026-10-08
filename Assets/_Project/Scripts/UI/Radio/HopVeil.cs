using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The radio-hop's soft dark as numbers (pure logic, EditMode-tested). It follows gameplay's eased fade exactly
    /// while that changes no faster than <see cref="RelaySettings.VeilMaxChangePerSecond"/>, so a value that jumps (a
    /// hop cut short) still eases instead of cutting; it never goes past <see cref="RelaySettings.VeilMaxDarkness"/>,
    /// so the dark stays soft. A faint static grain rises
    /// and resolves with it and crawls to a new place a few times a second (a seeded sequence, so captures repeat).
    /// </summary>
    internal sealed class HopVeil
    {
        private const uint GrainSeed = 0x9E3779B9u;

        private readonly RelaySettings _settings;
        private uint _state = GrainSeed;
        private float _level;
        private float _grainClock;

        public HopVeil(RelaySettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>0..1 how dark the hop is, as the veil shows it (following gameplay's fade).</summary>
        public float Level => _level;

        /// <summary>Opacity of the indigo veil.</summary>
        public float Darkness => _level * _settings.VeilMaxDarkness;

        /// <summary>Opacity of the static grain.</summary>
        public float Grain => _level * _settings.GrainOpacity;

        /// <summary>True while the screen is fully clear (the veil can leave the layout).</summary>
        public bool IsClear => _level <= 0f;

        /// <summary>Where the grain tile sits now, in pixels, each axis within one tile.</summary>
        public Vector2 GrainOffset { get; private set; }

        /// <summary>Grows each time the grain moves, so the view writes it only then.</summary>
        public int GrainRevision { get; private set; }

        /// <param name="fade">Gameplay's hop fade, 0 clear .. 1 dark.</param>
        /// <param name="deltaTime">Unscaled seconds since the last step (0 while paused: the veil holds).</param>
        public void Step(float fade, float deltaTime)
        {
            _level = Mathf.MoveTowards(_level, Mathf.Clamp01(fade), _settings.VeilMaxChangePerSecond * deltaTime);

            if (IsClear)
            {
                _grainClock = 0f;
                return;
            }

            _grainClock += deltaTime;
            float step = 1f / _settings.GrainStepsPerSecond;
            if (_grainClock >= step || GrainRevision == 0)
            {
                _grainClock = GrainRevision == 0 ? 0f : _grainClock % step;
                float tile = _settings.GrainTilePixels;
                GrainOffset = new Vector2(-Next01() * tile, -Next01() * tile);
                GrainRevision++;
            }
        }

        /// <summary>The next value of a small xorshift sequence, 0 (inclusive) .. 1 (exclusive).</summary>
        private float Next01()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return (_state & 0xFFFFFF) / (float)0x1000000;
        }
    }
}
