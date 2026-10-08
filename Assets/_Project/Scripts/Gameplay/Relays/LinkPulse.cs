using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A thin line of warm light that runs once along the ground from a newly lit relay mast toward the node it links
    /// to (home or another mast), easing in as it leaves and fading to a whisper as it arrives. One line renderer of
    /// LinkPulse light; its few points follow the ground under the travelling pulse. Allocation-free per frame.
    /// </summary>
    public sealed class LinkPulse
    {
        /// <summary>Points along the visible pulse.</summary>
        private const int Points = 12;

        /// <summary>Share of the way over which the pulse eases in as it leaves the mast.</summary>
        private const float LeaveShare = 0.05f;

        private readonly LineRenderer _line;
        private readonly GlowRenderer _glow;
        private readonly ITerrainQuery _terrain;
        private readonly RelayTuning _tuning;
        private readonly Vector3[] _points = new Vector3[Points];
        private Vector3 _from;
        private Vector3 _direction;
        private float _length;
        private float _start = float.NegativeInfinity;

        public LinkPulse(string name, Transform parent, Material material, ITerrainQuery terrain, RelayTuning tuning)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            _line = host.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = Points;
            _line.textureMode = LineTextureMode.Stretch;
            _line.alignment = LineAlignment.View;
            _line.widthMultiplier = tuning.PulseWidth;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            GlowObject.Configure(_line, material);
            _glow = new GlowRenderer(_line);
        }

        /// <summary>True while the pulse is on its way.</summary>
        public bool Running { get; private set; }

        /// <summary>Brightness right now.</summary>
        public float Level => _glow.Intensity;

        /// <summary>
        /// Sends a pulse from <paramref name="from"/> toward <paramref name="to"/>; returns the seconds until its head
        /// arrives there.
        /// </summary>
        public float Begin(Vector3 from, Vector3 to, float now)
        {
            Vector3 way = to - from;
            way.y = 0f;
            _length = way.magnitude;
            if (_length < Mathf.Epsilon)
            {
                return 0f;
            }

            _from = from;
            _direction = way / _length;
            _start = now;
            Running = true;
            return _length / _tuning.PulseSpeed;
        }

        public void Step(float now)
        {
            if (!Running)
            {
                return;
            }

            float head = (now - _start) * _tuning.PulseSpeed;
            float tail = head - _tuning.PulseLength;
            if (tail >= _length)
            {
                Running = false;
                _glow.Apply(0f);
                return;
            }

            float progress = Mathf.Clamp01(head / _length);
            float leave = Ease.Step(0f, LeaveShare, progress);
            float arrive = 1f - Ease.Step(1f - _tuning.PulseFade, 1f, tail / _length);
            _glow.Apply(_tuning.PulseGlow * leave * arrive);
            if (!_line.enabled)
            {
                return;
            }

            float start = Mathf.Clamp(tail, 0f, _length);
            float end = Mathf.Clamp(head, 0f, _length);
            for (int i = 0; i < Points; i++)
            {
                float along = Mathf.Lerp(start, end, (float)i / (Points - 1));
                Vector3 point = _from + _direction * along;
                point.y = _terrain.SampleHeight(point.x, point.z) + _tuning.PulseLift;
                _points[i] = point;
            }

            _line.SetPositions(_points);
        }
    }
}
