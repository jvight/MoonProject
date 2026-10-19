using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The first pad's home, made plain: while build mode is open and no charging pad stands yet, a calm cool-white
    /// frame lies round the marked apron's bay with a soft ring segment at its centre, both breathing slowly; once a pad
    /// is placed it fades out for good. Thicker than Art's worn outline and unlit by the Sun, so it reads from the chase
    /// camera at the distance 07 approaches from. Drawn with soft-glow lines, never a field of dots; tuned in
    /// <see cref="BuildingTuning"/>.
    /// </summary>
    public sealed class ApronGuide
    {
        private const int FrameCorners = 4;
        private const int MarkerSegments = 24;
        private const float GroundLift = 0.04f;
        private const string TintProperty = "_Color";
        private const string IntensityProperty = "_Intensity";
        private static readonly int IntensityId = Shader.PropertyToID(IntensityProperty);

        private readonly BuildingTuning _tuning;
        private readonly LineRenderer _frame;
        private readonly LineRenderer _marker;
        private readonly Material _material;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private float _visible;
        private float _clock;
        private float _shownIntensity = -1f;

        /// <param name="parent">Where the guide's line objects hang.</param>
        /// <param name="bay">The apron's bay outline renderer: its oriented bounds give the frame and its centre.</param>
        /// <param name="glow">A soft-glow line material; the guide uses a tinted copy of it.</param>
        public ApronGuide(Transform parent, Renderer bay, Material glow, BuildingTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            if (bay == null)
            {
                throw new ArgumentNullException(nameof(bay));
            }

            _material = new Material(glow != null ? glow : throw new ArgumentNullException(nameof(glow)))
            {
                name = glow.name + "_ApronGuide",
            };
            _material.SetColor(TintProperty, tuning.ApronGuideColor);
            _material.SetFloat(IntensityProperty, 0f);
            Bounds local = bay.localBounds;
            Transform bayTransform = bay.transform;
            var corners = new Vector3[FrameCorners];
            Vector3 centre = bayTransform.TransformPoint(local.center);
            float ground = bay.bounds.min.y + GroundLift;
            Vector3 extents = local.extents;
            Vector3[] signs =
            {
                new Vector3(-1f, 0f, -1f), new Vector3(1f, 0f, -1f), new Vector3(1f, 0f, 1f), new Vector3(-1f, 0f, 1f),
            };
            for (int i = 0; i < FrameCorners; i++)
            {
                Vector3 world = bayTransform.TransformPoint(local.center +
                                                            Vector3.Scale(extents, signs[i]));
                corners[i] = new Vector3(world.x, ground, world.z);
            }

            _frame = Line("ApronGuideFrame", parent, FrameCorners, true);
            _frame.SetPositions(corners);
            _marker = Line("ApronGuideMarker", parent, MarkerSegments + 1, false);
            float radius = tuning.ApronGuideMarkerRadius;
            float arc = tuning.ApronGuideMarkerArcDegrees * Mathf.Deg2Rad;
            float start = (Mathf.PI - arc) * 0.5f;
            for (int i = 0; i <= MarkerSegments; i++)
            {
                float angle = start + arc * i / MarkerSegments;
                _marker.SetPosition(i, new Vector3(centre.x + Mathf.Cos(angle) * radius, ground,
                    centre.z + Mathf.Sin(angle) * radius));
            }

            Apply(0f);
        }

        /// <summary>0 (hidden) .. 1 (fully shown): how visible the guide is before its breathing.</summary>
        public float Visible => _visible;

        /// <summary>True while the guide is drawn at all.</summary>
        public bool IsShown => _visible > 0f;

        /// <summary>
        /// Advances the guide: it fades in while <paramref name="wanted"/> (build mode open, no pad placed yet) and out
        /// otherwise, breathing slowly while it shows.
        /// </summary>
        public void Step(bool wanted, float deltaTime)
        {
            _clock += deltaTime;
            _visible = Mathf.MoveTowards(_visible, wanted ? 1f : 0f,
                deltaTime / Mathf.Max(_tuning.ApronGuideFadeSeconds, 0.05f));
            Apply(_visible > 0f ? Breath(_clock) * Ease.InOutSine(_visible) : 0f);
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
            float period = Mathf.Max(_tuning.ApronGuidePeriodSeconds, 0.2f);
            float wave = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * seconds / period);
            return Mathf.Lerp(_tuning.ApronGuideFloorShare, 1f, Ease.InOutSine(wave));
        }

        private void Apply(float level)
        {
            float intensity = level * _tuning.ApronGuideIntensity;
            bool visible = intensity > 0.0005f;
            _frame.enabled = visible;
            _marker.enabled = visible;
            if (!visible || Mathf.Approximately(intensity, _shownIntensity))
            {
                return;
            }

            _shownIntensity = intensity;
            _block.SetFloat(IntensityId, intensity);
            _frame.SetPropertyBlock(_block);
            _marker.SetPropertyBlock(_block);
        }

        private LineRenderer Line(string name, Transform parent, int points, bool loop)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            var line = host.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = loop;
            line.positionCount = points;
            line.widthMultiplier = _tuning.ApronGuideWidth;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.numCornerVertices = 3;
            line.numCapVertices = 3;
            line.sharedMaterial = _material;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.enabled = false;
            return line;
        }
    }
}
