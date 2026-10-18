using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's cutting and welding beam (docs/design/feel-pass.md §3.1): a hot white core inside a cyan sheath and outer
    /// halo that shimmer. At ignition it reaches the target point on a fast ease-out (never instantly); on release it
    /// thins out where it last met its target, its end held there (the owner saw a finished weld's beam shoot off
    /// into the distance while its target point went away). Three line renderers, built once.
    /// </summary>
    public sealed class CutBeam
    {
        private const float TwoPi = Mathf.PI * 2f;
        private const float HaloWidthFactor = 2.2f;

        private readonly FeelVfxTuning _tuning;
        private readonly LineRenderer _core;
        private readonly LineRenderer _sheath;
        private readonly LineRenderer _halo;
        private float _reach;
        private bool _out;
        private Vector3 _held;
        private float _clock;

        public CutBeam(Transform parent, FeelVfxTuning tuning, Material core, Material sheath)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _halo = Line("BeamHalo", parent, sheath, tuning.SheathWidth * HaloWidthFactor);
            _sheath = Line("BeamSheath", parent, sheath, tuning.SheathWidth);
            _core = Line("BeamCore", parent, core, tuning.CoreWidth);
        }

        /// <summary>How far the beam reaches from the emitter to the target point (0..1).</summary>
        public float Reach => _reach;

        /// <summary>Number of rendered beam layers (core + sheath + halo).</summary>
        public int LayerCount => 3;

        /// <summary>World-space start position of the beam when active, or Vector3.zero when retracted.</summary>
        public Vector3 Start { get; private set; }

        /// <summary>World-space tip position of the beam when active, or Vector3.zero when retracted.</summary>
        public Vector3 End { get; private set; }

        /// <summary>The beam fires: it extends to the target point.</summary>
        public void Extend()
        {
            _out = true;
        }

        /// <summary>The beam lets go: it thins out with its end held where it last met its target.</summary>
        public void Retract()
        {
            _out = false;
        }

        /// <summary>
        /// Advances the beam from <paramref name="emitter"/>: while it fires its end follows <paramref name="target"/>;
        /// once it lets go the target is ignored, its end staying where it last was.
        /// </summary>
        public void Step(float deltaTime, Vector3 emitter, Vector3 target)
        {
            _clock += deltaTime;
            float seconds = _out ? _tuning.ExtendSeconds : _tuning.RetractSeconds;
            _reach = Mathf.MoveTowards(_reach, _out ? 1f : 0f, deltaTime / Mathf.Max(seconds, 1e-3f));
            bool shown = _reach > 0f;
            _core.enabled = shown;
            _sheath.enabled = shown;
            _halo.enabled = shown;
            if (!shown)
            {
                Start = Vector3.zero;
                End = Vector3.zero;
                return;
            }

            if (_out)
            {
                _held = target;
            }

            float eased = _out ? FeelCurves.Extend(_reach) : 1f;
            float thickness = _out ? 1f : _reach * _reach;
            Vector3 tip = Vector3.Lerp(emitter, _held, eased);
            Start = emitter;
            End = tip;
            _core.SetPosition(0, emitter);
            _core.SetPosition(1, tip);
            _sheath.SetPosition(0, emitter);
            _sheath.SetPosition(1, tip);
            _halo.SetPosition(0, emitter);
            _halo.SetPosition(1, tip);
            float wobble = Mathf.Sin(TwoPi * _tuning.ShimmerRate * _clock);
            _core.widthMultiplier = _tuning.CoreWidth * thickness;
            _sheath.widthMultiplier = _tuning.SheathWidth * thickness * (1f + _tuning.Shimmer * wobble);
            _halo.widthMultiplier = _tuning.SheathWidth * HaloWidthFactor * thickness *
                                    (1f + _tuning.Shimmer * 0.5f * wobble);
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
