using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's scan beam (Task SCAN-BEAM): a thin, steady tech-cyan instrument beam connecting 07's lens
    /// (<c>TetherOrigin</c>) to the point being scanned on the fragment, sweeping up the object in lockstep with
    /// <c>_ScanProgress</c>. Draws a small aperture glow at the lens (the only point allowed just above the bloom
    /// threshold) and a soft contact point on the object where the beam meets the scan line, easing in when a scan
    /// starts and easing out when let go or completed. Zero allocations per frame.
    /// </summary>
    public sealed class ScanBeam
    {
        private const int PointSlots = 4;
        private const float VisibleThreshold = 1e-4f;
        private const float MinFadeSeconds = 1e-3f;
        private const float MinDistance = 1e-4f;
        private const float MaxContactShare = 0.42f;
        private const float ApertureStartMultiplier = 1.48f;
        private const float ApertureEndMultiplier = 1.02f;
        private const float MinWidthShare = 0.6f;
        private const float ContactEndShare = 0.75f;
        private const float LineIntensity = 1f;

        private readonly FeelVfxTuning _tuning;
        private readonly Material _lineMaterial;
        private readonly LineRenderer _core;
        private readonly LineRenderer _sheath;
        private readonly FeelSprites _points;
        private Vector3 _target;
        private Vector3 _span;
        private float _progress;
        private float _rate;
        private float _level;
        private bool _active;

        public ScanBeam(Transform parent, FeelVfxTuning tuning, Material beamMaterial, Material spriteMaterial)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            if (beamMaterial == null)
            {
                throw new ArgumentNullException(nameof(beamMaterial), "ScanBeam: the scan beam material is missing.");
            }

            _lineMaterial = new Material(beamMaterial)
            {
                name = "M_ScanBeamLine",
                renderQueue = (int)RenderQueue.Transparent - 1,
            };
            _lineMaterial.SetColor("_Color", Color.white);
            _lineMaterial.SetFloat("_Intensity", LineIntensity);
            _lineMaterial.SetFloat("_SrcBlend", (float)BlendMode.One);
            _lineMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            _sheath = Line("ScanBeamSheath", parent, _lineMaterial, tuning.ScanSheathWidth);
            _core = Line("ScanBeamCore", parent, _lineMaterial, tuning.ScanCoreWidth);
            _points = new FeelSprites("ScanBeamPoints", parent, spriteMaterial, PointSlots, false, 0f);
        }

        /// <summary>True while the beam is active or easing out.</summary>
        public bool Shown => _level > VisibleThreshold;

        /// <summary>Current eased visibility level (0..1).</summary>
        public float Level => Ease.InOutSine(_level);

        /// <summary>Starts or resumes the beam at <paramref name="progress"/> (0..1) over <paramref name="remainingSeconds"/>.</summary>
        public void Begin(Vector3 target, Vector3 span, float progress, float remainingSeconds)
        {
            _target = target;
            _span = span.sqrMagnitude > MinDistance ? span : Vector3.up * _tuning.ScanDefaultHeight;
            _progress = Mathf.Clamp01(progress);
            _rate = remainingSeconds > MinFadeSeconds ? (1f - _progress) / remainingSeconds : 0f;
            _active = true;
        }

        /// <summary>Keeps progress in step with quarter beats.</summary>
        public void Sync(float progress)
        {
            _progress = Mathf.Clamp01(progress);
        }

        /// <summary>Eases the beam out on let-go or scan completion.</summary>
        public void End()
        {
            _active = false;
        }

        public void Step(float deltaTime, Vector3 emitter)
        {
            float fadeSeconds = _active ? _tuning.ScanEaseInSeconds : _tuning.ScanEaseOutSeconds;
            _level = Mathf.MoveTowards(_level, _active ? 1f : 0f, deltaTime / Mathf.Max(fadeSeconds, MinFadeSeconds));
            if (_active)
            {
                _progress = Mathf.Clamp01(_progress + _rate * deltaTime);
            }

            _points.Begin();
            bool shown = _level > VisibleThreshold;
            _core.enabled = shown;
            _sheath.enabled = shown;
            if (shown)
            {
                float eased = Ease.InOutSine(_level);
                Vector3 lineCentre = _target + _span * (_progress - 0.5f);
                Vector3 toEmitter = emitter - lineCentre;
                float distance = toEmitter.magnitude;
                Vector3 towardLens = distance > MinDistance ? toEmitter / distance : Vector3.back;
                float standOff = Mathf.Min(_tuning.ScanContactOffset, distance * MaxContactShare);
                float apertureScale = Mathf.Lerp(ApertureStartMultiplier, ApertureEndMultiplier, _progress);
                float apertureForward = Mathf.Min(_tuning.ScanApertureOffset * apertureScale, distance * MaxContactShare);
                Vector3 aperture = emitter - towardLens * apertureForward;
                Vector3 contact = lineCentre + towardLens * standOff;

                _core.SetPosition(0, emitter);
                _core.SetPosition(1, contact);
                _sheath.SetPosition(0, emitter);
                _sheath.SetPosition(1, contact);

                float widthScale = Mathf.Lerp(MinWidthShare, 1f, eased);
                _core.widthMultiplier = _tuning.ScanCoreWidth * widthScale;
                _sheath.widthMultiplier = _tuning.ScanSheathWidth * widthScale;

                Color coreStart = _tuning.ScanCoreColor;
                coreStart.a *= eased;
                Color coreEnd = coreStart;
                coreEnd.a *= ContactEndShare;
                _core.startColor = coreStart;
                _core.endColor = coreEnd;

                Color sheathStart = _tuning.ScanBeamColor;
                sheathStart.a *= eased;
                Color sheathEnd = sheathStart;
                sheathEnd.a *= ContactEndShare;
                _sheath.startColor = sheathStart;
                _sheath.endColor = sheathEnd;

                Color lensCore = _tuning.ScanCoreColor;
                lensCore.a = _tuning.ScanLensCoreAlpha * eased;
                _points.Add(emitter, _tuning.ScanLensCoreSize * widthScale, lensCore, Vector3.zero);
                _points.Add(aperture, _tuning.ScanLensCoreSize * widthScale, lensCore, Vector3.zero);

                Color lensHalo = _tuning.ScanBeamColor;
                lensHalo.a = _tuning.ScanLensHaloAlpha * eased;
                _points.Add(aperture, _tuning.ScanLensHaloSize * widthScale, lensHalo, Vector3.zero);

                Color contactDot = _tuning.ScanBeamColor;
                contactDot.a = _tuning.ScanContactAlpha * eased;
                _points.Add(contact, _tuning.ScanContactSize * widthScale, contactDot, Vector3.zero);
            }

            _points.End();
        }

        private static LineRenderer Line(string name, Transform parent, Material material, float width)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            var line = host.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
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
