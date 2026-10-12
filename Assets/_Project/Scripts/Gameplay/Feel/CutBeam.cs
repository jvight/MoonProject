using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's cutting beam (docs/design/feel-pass.md §3.1): a hot white core inside a cyan sheath that shimmers. At
    /// ignition it reaches the cut point on a fast ease-out (never instantly); on a release it retracts into the
    /// emitter. Two line renderers, built once.
    /// </summary>
    public sealed class CutBeam
    {
        private const float TwoPi = Mathf.PI * 2f;

        private readonly FeelVfxTuning _tuning;
        private readonly LineRenderer _core;
        private readonly LineRenderer _sheath;
        private float _reach;
        private bool _out;
        private float _clock;

        public CutBeam(Transform parent, FeelVfxTuning tuning, Material core, Material sheath)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _sheath = Line("BeamSheath", parent, sheath, tuning.SheathWidth);
            _core = Line("BeamCore", parent, core, tuning.CoreWidth);
        }

        /// <summary>How far the beam reaches from the emitter to the cut point (0..1).</summary>
        public float Reach => _reach;

        /// <summary>The beam fires: it extends to the cut point.</summary>
        public void Extend()
        {
            _out = true;
        }

        /// <summary>The beam lets go: it retracts into the emitter.</summary>
        public void Retract()
        {
            _out = false;
        }

        public void Step(float deltaTime, Vector3 emitter, Vector3 target)
        {
            _clock += deltaTime;
            float seconds = _out ? _tuning.ExtendSeconds : _tuning.RetractSeconds;
            _reach = Mathf.MoveTowards(_reach, _out ? 1f : 0f, deltaTime / Mathf.Max(seconds, 1e-3f));
            bool shown = _reach > 0f;
            _core.enabled = shown;
            _sheath.enabled = shown;
            if (!shown)
            {
                return;
            }

            float eased = _out ? FeelCurves.Extend(_reach) : _reach * _reach;
            Vector3 tip = Vector3.Lerp(emitter, target, eased);
            _core.SetPosition(0, emitter);
            _core.SetPosition(1, tip);
            _sheath.SetPosition(0, emitter);
            _sheath.SetPosition(1, tip);
            float wobble = Mathf.Sin(TwoPi * _tuning.ShimmerRate * _clock);
            _sheath.widthMultiplier = _tuning.SheathWidth * (1f + _tuning.Shimmer * wobble);
        }

        private static LineRenderer Line(string name, Transform parent, Material material, float width)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material), $"{name}: the beam material is missing.");
            }

            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            var line = host.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = width;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.sharedMaterial = material;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.enabled = false;
            return line;
        }
    }
}
