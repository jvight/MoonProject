using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How the socket rack looks from moment to moment (pure, EditMode-tested; <see cref="SocketRackView"/> applies it).
    /// Revealed live, it rises out of the ground, eased, and once it stands its bays light left to right, each a beat
    /// after the last: a seen part's outline breathes slowly at its dim strength, and a fitted part's outline brightens
    /// while its fill and lamp warm and its jaws close. A rack found already out (a loaded game) stands lit at once.
    /// </summary>
    public sealed class RackLook
    {
        private readonly RackViewSettings _settings;
        private readonly float[] _presence;
        private readonly float[] _filled;
        private readonly float[] _jaw;
        private float _rise;
        private float _sinceStanding;
        private float _clock;
        private bool _revealed;

        public RackLook(int bays, RackViewSettings settings)
        {
            if (bays < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bays));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _presence = new float[bays];
            _filled = new float[bays];
            _jaw = new float[bays];
        }

        /// <summary>How far the rack has risen, eased: 0 sunk below the ground, 1 standing.</summary>
        public float Rise01 => Smooth(_rise);

        /// <summary>The rack stands (risen fully).</summary>
        public bool Standing => _revealed && _rise >= 1f;

        /// <summary>The release reveals the rack: it rises (live) or simply stands, lit (a loaded game).</summary>
        public void Reveal(bool live)
        {
            _revealed = true;
            _rise = live ? 0f : 1f;
            _sinceStanding = live ? 0f : float.MaxValue;
        }

        /// <param name="deltaTime">Seconds since the last step.</param>
        /// <param name="rack">What each bay shows.</param>
        /// <param name="snap">Bays take their look at once (a loaded game), with no easing.</param>
        public void Step(float deltaTime, IRackStatus rack, bool snap)
        {
            if (rack == null)
            {
                throw new ArgumentNullException(nameof(rack));
            }

            if (!_revealed)
            {
                return;
            }

            _clock += deltaTime;
            if (_rise < 1f)
            {
                _rise = Mathf.Min(1f, _rise + deltaTime / _settings.RiseSeconds);
            }
            else if (_sinceStanding < float.MaxValue)
            {
                _sinceStanding += deltaTime;
            }

            float glow = snap ? 1f : deltaTime / _settings.GlowSeconds;
            float close = snap ? 1f : deltaTime / _settings.JawSeconds;
            int count = Mathf.Min(rack.Count, _presence.Length);
            for (int i = 0; i < count; i++)
            {
                RackBayState state = rack.StateOf(i);
                bool lit = Standing && _sinceStanding >= i * _settings.BayStaggerSeconds;
                bool fitted = lit && state == RackBayState.Filled;
                _presence[i] = Mathf.MoveTowards(_presence[i], lit && state != RackBayState.Hidden ? 1f : 0f, glow);
                _filled[i] = Mathf.MoveTowards(_filled[i], fitted ? 1f : 0f, glow);
                _jaw[i] = Mathf.MoveTowards(_jaw[i], fitted ? 1f : 0f, close);
            }
        }

        /// <summary>The outline's glow strength now (times the outline colour).</summary>
        public float Outline(int bay)
        {
            float breath = 1f - _settings.BreathDepth *
                (0.5f - 0.5f * Mathf.Cos(_clock * 2f * Mathf.PI / _settings.BreathSeconds));
            float level = Mathf.Lerp(_settings.OutlineDim * breath, _settings.OutlineBright, Smooth(_filled[bay]));
            return Smooth(_presence[bay]) * level;
        }

        /// <summary>The fill's glow strength now, 0..1 (times the fill colour).</summary>
        public float Fill(int bay)
        {
            return Smooth(_filled[bay]);
        }

        /// <summary>The lamp's glow strength now, 0..1 (times the lamp colour).</summary>
        public float Lamp(int bay)
        {
            return Smooth(_filled[bay]);
        }

        /// <summary>How closed the bay's jaws are, eased: 0 open (as authored), 1 closed.</summary>
        public float Jaw(int bay)
        {
            return Smooth(_jaw[bay]);
        }

        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
