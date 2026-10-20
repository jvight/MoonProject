using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The range ring drawn on the ground (<see cref="RangeRingField"/>): one closed, soft, cool line along the
    /// polygon, lifted a little above the ground, fading with the ring. Allocation-free per frame.
    /// </summary>
    public sealed class RangeRingLine
    {
        private readonly RangeRingField _ring;
        private readonly RangeRingSettings _settings;
        private readonly LineRenderer _line;
        private bool _placed;
        private Color _lastColor;

        public RangeRingLine(Transform parent, Material material, RangeRingField ring, RangeRingSettings settings)
        {
            _ring = ring;
            _settings = settings;
            var host = new GameObject("RangeRing");
            host.transform.SetParent(parent, false);
            _line = host.AddComponent<LineRenderer>();
            _line.sharedMaterial = material;
            _line.useWorldSpace = true;
            _line.loop = true;
            _line.numCornerVertices = 2;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.lightProbeUsage = LightProbeUsage.Off;
            _line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _line.startWidth = settings.LineWidth;
            _line.endWidth = settings.LineWidth;
            _line.positionCount = ring.Count;
            _line.enabled = false;
        }

        /// <summary>The line is on show.</summary>
        public bool IsDrawn => _line.enabled;

        public void Step()
        {
            float strength = _ring.Strength;
            if (!_ring.IsShown || strength <= 0.001f)
            {
                _line.enabled = false;
                _placed = false;
                return;
            }

            if (!_placed)
            {
                for (int i = 0; i < _ring.Count; i++)
                {
                    _line.SetPosition(i, _ring.Point(i) + Vector3.up * _settings.LineLift);
                }

                _placed = true;
            }

            Color color = _settings.Color;
            color.a = strength;
            if (color != _lastColor)
            {
                _line.startColor = color;
                _line.endColor = color;
                _lastColor = color;
            }

            _line.enabled = true;
        }
    }
}
