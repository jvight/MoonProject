using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Shows the player what E would scan: a faint wire box in the scan's teal rests on the bounds of the target the
    /// prompt is for, breathing slowly, fading in when a target comes and out when it goes or the scan starts (the
    /// scan's own light takes over). A prompt with no visible target never happens. Tuned in
    /// <see cref="ScanTuning"/>; allocation-free per frame.
    /// </summary>
    public sealed class ScanTargetMark
    {
        private const int PathPoints = 16;
        private const string TintProperty = "_Color";
        private const string IntensityProperty = "_Intensity";
        private static readonly int IntensityId = Shader.PropertyToID(IntensityProperty);

        private readonly ScanTuning _tuning;
        private readonly LineRenderer _line;
        private readonly Material _material;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly Vector3[] _path = new Vector3[PathPoints];
        private ScanFragment _shown;
        private float _visible;
        private float _clock;

        /// <param name="parent">Where the wire box's line object hangs.</param>
        /// <param name="glow">A soft-glow line material; the mark uses a tinted copy of it.</param>
        public ScanTargetMark(Transform parent, Material glow, ScanTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _material = new Material(glow != null ? glow : throw new ArgumentNullException(nameof(glow)))
            {
                name = glow.name + "_ScanTargetMark",
            };
            _material.SetColor(TintProperty, tuning.ScanLineColor);
            _material.SetFloat(IntensityProperty, 0f);
            var host = new GameObject("ScanTargetMark");
            host.transform.SetParent(parent, false);
            _line = host.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = PathPoints;
            _line.widthMultiplier = tuning.TargetMarkWidth;
            _line.textureMode = LineTextureMode.Stretch;
            _line.alignment = LineAlignment.View;
            _line.numCornerVertices = 2;
            _line.numCapVertices = 2;
            _line.sharedMaterial = _material;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.lightProbeUsage = LightProbeUsage.Off;
            _line.enabled = false;
        }

        /// <summary>The target the wire box rests on now, or null while it is hidden.</summary>
        public ScanFragment Shown => _shown;

        /// <summary>0 (hidden) .. 1 (fully shown): how visible the mark is before its breathing.</summary>
        public float Visible => _visible;

        /// <summary>
        /// Advances the mark: it rests on <paramref name="target"/> (null: none) once any earlier target has faded
        /// out.
        /// </summary>
        public void Step(ScanFragment target, float deltaTime)
        {
            _clock += deltaTime;
            float fade = deltaTime / Mathf.Max(_tuning.TargetMarkFadeSeconds, 0.05f);
            if (_shown != null && target != _shown)
            {
                _visible = Mathf.MoveTowards(_visible, 0f, fade);
                if (_visible <= 0f)
                {
                    _shown = null;
                }
            }

            if (_shown == null && target != null)
            {
                _shown = target;
                _visible = 0f;
                _clock = 0f;
            }

            if (_shown != null && target == _shown)
            {
                _visible = Mathf.MoveTowards(_visible, 1f, fade);
            }

            Apply();
        }

        /// <summary>Destroys the tinted material copy.</summary>
        public void Dispose()
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_material);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_material);
            }
        }

        /// <summary>The breathing level 0..1 at <paramref name="seconds"/>: slow, eased, never fully dark.</summary>
        public float Breath(float seconds)
        {
            float period = Mathf.Max(_tuning.TargetMarkPeriodSeconds, 0.2f);
            float wave = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * seconds / period);
            return Mathf.Lerp(_tuning.TargetMarkFloorShare, 1f, Ease.InOutSine(wave));
        }

        private void Apply()
        {
            Renderer solid = _shown != null ? _shown.Solid : null;
            float intensity = solid != null
                ? Breath(_clock) * Ease.InOutSine(_visible) * _tuning.TargetMarkIntensity
                : 0f;
            bool visible = intensity > 0.0005f;
            _line.enabled = visible;
            if (!visible)
            {
                return;
            }

            Bounds box = solid.bounds;
            box.Expand(_tuning.TargetMarkMargin * 2f);
            Trace(box);
            _line.SetPositions(_path);
            _block.SetFloat(IntensityId, intensity);
            _line.SetPropertyBlock(_block);
        }

        /// <summary>One unbroken path over all twelve edges of the box (some are walked twice).</summary>
        private void Trace(Bounds box)
        {
            Vector3 lo = box.min;
            Vector3 hi = box.max;
            var b0 = new Vector3(lo.x, lo.y, lo.z);
            var b1 = new Vector3(hi.x, lo.y, lo.z);
            var b2 = new Vector3(hi.x, lo.y, hi.z);
            var b3 = new Vector3(lo.x, lo.y, hi.z);
            var t0 = new Vector3(lo.x, hi.y, lo.z);
            var t1 = new Vector3(hi.x, hi.y, lo.z);
            var t2 = new Vector3(hi.x, hi.y, hi.z);
            var t3 = new Vector3(lo.x, hi.y, hi.z);
            _path[0] = b0;
            _path[1] = b1;
            _path[2] = b2;
            _path[3] = b3;
            _path[4] = b0;
            _path[5] = t0;
            _path[6] = t1;
            _path[7] = b1;
            _path[8] = t1;
            _path[9] = t2;
            _path[10] = b2;
            _path[11] = t2;
            _path[12] = t3;
            _path[13] = b3;
            _path[14] = t3;
            _path[15] = t0;
        }
    }
}
