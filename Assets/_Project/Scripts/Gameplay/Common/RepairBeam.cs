using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's stitching beam (a friend's repair, a relay mast's restoration, a tower section rising): a line of light
    /// bowed up from 07's eye to what it mends, its far end sweeping sideways and, at twice the rate, up and down so it
    /// reads as stitching; with no sweep it is a steady beam (feeding a hopper, tapping Bell's dial). It eases in and
    /// out; dark, it costs no draw call. Allocation-free per frame.
    /// </summary>
    public sealed class RepairBeam
    {
        /// <summary>Points along the beam.</summary>
        private const int Points = 16;

        /// <summary>Seconds (time constant) for the beam to brighten and fade.</summary>
        private const float FadeTime = 0.15f;

        private const float Width = 0.06f;

        /// <summary>Upward bow (m) of the beam.</summary>
        private const float Arc = 0.2f;

        /// <summary>The stitch's up-and-down sweep is this share of its sideways sweep, at twice the rate.</summary>
        private const float StitchLift = 0.5f;

        private readonly LineRenderer _line;
        private readonly GlowRenderer _glow;
        private readonly Vector3[] _points = new Vector3[Points];
        private readonly float _stitchRate;
        private readonly float _stitchSpread;
        private float _level;

        /// <param name="stitchRate">Sweeps per second of the beam's end.</param>
        /// <param name="stitchSpread">Sideways reach (m) of the sweep.</param>
        public RepairBeam(string name, Transform parent, Material material, float stitchRate, float stitchSpread)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            _line = host.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = Points;
            _line.textureMode = LineTextureMode.Stretch;
            _line.alignment = LineAlignment.View;
            _line.widthMultiplier = Width;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            GlowObject.Configure(_line, material);
            _glow = new GlowRenderer(_line);
            _stitchRate = stitchRate;
            _stitchSpread = stitchSpread;
        }

        /// <summary>Current brightness (0 dark .. 1 full).</summary>
        public float Level => _level;

        /// <summary>
        /// Eases toward on while <paramref name="beaming"/>, and while there is a target draws the beam from
        /// <paramref name="start"/> to a stitching point around <paramref name="target"/>.
        /// </summary>
        public void Step(bool beaming, bool hasTarget, Vector3 start, Vector3 target, float now, float deltaTime)
        {
            _level = Damp.Toward(_level, beaming ? 1f : 0f, FadeTime, deltaTime);
            _glow.Apply(_level);
            if (!_line.enabled || !hasTarget)
            {
                return;
            }

            float phase = now * _stitchRate * 2f * Mathf.PI;
            Vector3 across = Vector3.Cross(Vector3.up, target - start).normalized;
            Vector3 end = target + across * (Mathf.Sin(phase) * _stitchSpread) +
                          Vector3.up * (Mathf.Sin(phase * 2f) * _stitchSpread * StitchLift);
            for (int i = 0; i < Points; i++)
            {
                float t = (float)i / (Points - 1);
                _points[i] = Vector3.Lerp(start, end, t) + Vector3.up * (Ease.Hump(t) * Arc);
            }

            _line.SetPositions(_points);
        }
    }
}
