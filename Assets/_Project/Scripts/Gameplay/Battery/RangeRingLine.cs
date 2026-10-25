using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The range ring drawn on the ground (<see cref="RangeRingField"/>): one closed, continuous moonlit-amber glow line
    /// along the polygon, lifted a little above the ground, with a soft gradient falloff across its width (never dots or
    /// dashes, never a hard-edged wire). It draws outward from 07 over ~2 s, holds ~1.5 s, then eases out over ~3 s and
    /// is completely gone (zero vertices, zero width, zero alpha, renderer disabled); a ring that grows reaches out from
    /// the ring it was instead. With it lies the network's link, the mast to its node: dashed while the node needs its
    /// part, solid once it charges, drawn out from the hub as the ring draws. Allocation-free while hidden; while shown,
    /// the width curves are refreshed only when the view has moved.
    /// </summary>
    public sealed class RangeRingLine
    {
        private const float WidthRefreshMetres = 0.5f;

        // The most points the network link lays on the ground (a link longer than this many steps spaces them out).
        private const int LinkMaxPoints = 128;

        private readonly RangeRingField _ring;
        private readonly RangeRingSettings _settings;
        private readonly ITerrainQuery _terrain;
        private readonly IViewCamera _view;
        private readonly LineRenderer _line;
        private readonly LineRenderer _linkLine;
        private readonly MaterialPropertyBlock _linkBlock = new MaterialPropertyBlock();
        private readonly Keyframe[] _linkKeys = new Keyframe[2];
        private readonly AnimationCurve _linkCurve = new AnimationCurve();
        private readonly MaterialPropertyBlock _block;
        private readonly Keyframe[] _widthKeys;
        private readonly AnimationCurve _widthCurve = new AnimationCurve();
        private readonly int _vertices;
        private readonly int _colorId;
        private readonly int _intensityId;
        private readonly int _acrossInId;
        private readonly int _acrossOutId;
        private readonly int _alongInId;
        private readonly int _alongOutId;
        private readonly int _bandCountId;
        private readonly int _bandStrengthId;
        private readonly int _corePowerId;
        private readonly int _coreMixId;
        private readonly int _rimMixId;
        private readonly int _roundRadialId;
        private bool _placed;
        private Color _lastColor;
        private Vector3 _widthFrom = new Vector3(float.MaxValue, 0f, 0f);
        private float _widthReveal = -1f;

        public RangeRingLine(Transform parent, Material material, RangeRingField ring, RangeRingSettings settings,
            ITerrainQuery terrain, IViewCamera view)
        {
            _ring = ring;
            _settings = settings;
            _terrain = terrain ?? throw new System.ArgumentNullException(nameof(terrain));
            _view = view ?? throw new System.ArgumentNullException(nameof(view));
            _colorId = Shader.PropertyToID("_Color");
            _intensityId = Shader.PropertyToID("_Intensity");
            _acrossInId = Shader.PropertyToID("_AcrossIn");
            _acrossOutId = Shader.PropertyToID("_AcrossOut");
            _alongInId = Shader.PropertyToID("_AlongIn");
            _alongOutId = Shader.PropertyToID("_AlongOut");
            _bandCountId = Shader.PropertyToID("_BandCount");
            _bandStrengthId = Shader.PropertyToID("_BandStrength");
            _corePowerId = Shader.PropertyToID("_CorePower");
            _coreMixId = Shader.PropertyToID("_CoreMix");
            _rimMixId = Shader.PropertyToID("_RimMix");
            _roundRadialId = Shader.PropertyToID("_RoundRadial");
            var host = new GameObject("RangeRing");
            host.transform.SetParent(parent, false);
            _line = host.AddComponent<LineRenderer>();
            _line.sharedMaterial = material;
            _line.useWorldSpace = true;
            _line.loop = true;
            _line.textureMode = LineTextureMode.Stretch;
            _line.numCornerVertices = 4;
            _line.numCapVertices = 0;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.lightProbeUsage = LightProbeUsage.Off;
            _line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _vertices = ring.Count * Mathf.Max(1, settings.Subdivisions);
            _widthKeys = new Keyframe[_vertices];
            _block = new MaterialPropertyBlock();
            ApplySoftGlowProfile(_line, _block);
            _linkLine = NewLine(parent, material, "RangeRingLink");
            _linkLine.loop = false;
            ApplySoftGlowProfile(_linkLine, _linkBlock);
            Hide();
        }

        private static LineRenderer NewLine(Transform parent, Material material, string name)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            LineRenderer line = host.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.textureMode = LineTextureMode.Stretch;
            line.numCornerVertices = 4;
            line.numCapVertices = 0;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return line;
        }

        /// <summary>The line is on show.</summary>
        public bool IsDrawn => _line.enabled;

        public void Step()
        {
            float strength = _ring.Strength;
            if (!_ring.IsShown || strength <= 0.001f)
            {
                if (_line.enabled || _line.positionCount > 0)
                {
                    Hide();
                }

                return;
            }

            float reveal = _ring.Reveal01;
            if (reveal < 1f || !_placed)
            {
                Place(reveal);
                _placed = reveal >= 1f;
            }

            Color color = _settings.Color;
            color.a = strength;
            if (color != _lastColor)
            {
                _line.startColor = color;
                _line.endColor = color;
                _lastColor = color;
            }

            _line.widthMultiplier = 1f;
            RefreshWidth(reveal);
            _line.enabled = true;
            StepLink(reveal, color);
        }

        /// <summary>The network's link with the ring: drawn out from the hub as far as the ring has drawn.</summary>
        private void StepLink(float reveal, Color color)
        {
            if (!_ring.TryGetLink(out Vector3 hub, out Vector3 node, out bool needsPart))
            {
                HideLink();
                return;
            }

            float length = SurfaceRules.HorizontalDistance(hub, node);
            int count = Mathf.Clamp(Mathf.CeilToInt(length * reveal / _settings.LinkStepMetres) + 1, 2,
                LinkMaxPoints);
            if (_linkLine.positionCount != count)
            {
                _linkLine.positionCount = count;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 at = Vector3.Lerp(hub, node, reveal * i / (count - 1));
                _linkLine.SetPosition(i, new Vector3(at.x, _terrain.SampleHeight(at.x, at.z) + _settings.LineLift,
                    at.z));
            }

            _linkLine.GetPropertyBlock(_linkBlock);
            _linkBlock.SetFloat(_bandCountId, needsPart ? length * reveal / (_settings.LinkDashMetres * 2f) : 0f);
            _linkBlock.SetFloat(_bandStrengthId, needsPart ? 1f : 0f);
            _linkLine.SetPropertyBlock(_linkBlock);
            _linkLine.startColor = color;
            _linkLine.endColor = color;
            Camera camera = _view.Camera;
            if (camera != null)
            {
                float focal = RangeRingWidth.FocalPixels(camera.pixelHeight, camera.fieldOfView);
                Vector3 eye = camera.transform.position;
                _linkKeys[0] = new Keyframe(0f, RangeRingWidth.World(Vector3.Distance(eye, hub), focal,
                    _settings.MaxPixels, _settings.MinPixels, _settings.FullPixelDistance));
                _linkKeys[1] = new Keyframe(1f, RangeRingWidth.World(Vector3.Distance(eye, node), focal,
                    _settings.MaxPixels, _settings.MinPixels, _settings.FullPixelDistance));
                _linkCurve.keys = _linkKeys;
                _linkLine.widthCurve = _linkCurve;
            }

            _linkLine.enabled = true;
        }

        private void HideLink()
        {
            if (_linkLine.enabled || _linkLine.positionCount > 0)
            {
                _linkLine.enabled = false;
                _linkLine.positionCount = 0;
            }
        }

        /// <summary>
        /// Configures the <c>SoftGlow</c> shader block for a continuous, soft-edged moonlit-amber glow across the line's
        /// width, with bands zeroed so it is never dotted or dashed, and intensity kept below bloom and relay.1's lamp.
        /// </summary>
        private void ApplySoftGlowProfile(LineRenderer line, MaterialPropertyBlock block)
        {
            line.GetPropertyBlock(block);
            block.SetColor(_colorId, Color.white);
            block.SetFloat(_intensityId, _settings.ShaderIntensity);
            block.SetFloat(_acrossInId, _settings.EdgeFalloff);
            block.SetFloat(_acrossOutId, _settings.EdgeFalloff);
            block.SetFloat(_alongInId, 0f);
            block.SetFloat(_alongOutId, 0f);
            block.SetFloat(_bandCountId, 0f);
            block.SetFloat(_bandStrengthId, 0f);
            block.SetFloat(_corePowerId, _settings.CoreFalloffPower);
            block.SetFloat(_coreMixId, _settings.CoreFalloffMix);
            block.SetFloat(_rimMixId, 0f);
            block.SetFloat(_roundRadialId, 0f);
            line.SetPropertyBlock(block);
        }

        /// <summary>Clears every vertex, width and alpha and disables the renderer so nothing lingers after the fade.</summary>
        private void Hide()
        {
            _line.enabled = false;
            _line.positionCount = 0;
            _line.widthMultiplier = 0f;
            Color clear = _settings.Color;
            clear.a = 0f;
            _line.startColor = clear;
            _line.endColor = clear;
            _lastColor = clear;
            _placed = false;
            _widthReveal = -1f;
            HideLink();
        }

        /// <summary>The ring drawn out to <paramref name="reveal"/> of the way from 07 to its edge, on the ground.</summary>
        private void Place(float reveal)
        {
            if (_line.positionCount != _vertices)
            {
                _line.positionCount = _vertices;
            }

            Vector3 centre = _ring.Centre;
            int sub = Mathf.Max(1, _settings.Subdivisions);
            for (int j = 0; j < _vertices; j++)
            {
                int i = j / sub;
                float along = (j % sub) / (float)sub;
                int next = (i + 1) % _ring.Count;
                Vector3 edge = Vector3.Lerp(_ring.Point(i), _ring.Point(next), along);
                Vector3 start = _ring.Growing ? Vector3.Lerp(_ring.From(i), _ring.From(next), along) : centre;
                float x = Mathf.Lerp(start.x, edge.x, reveal);
                float z = Mathf.Lerp(start.z, edge.z, reveal);
                float lift = _settings.LineLift + _settings.LiftPerMetre *
                    Mathf.Sqrt((x - centre.x) * (x - centre.x) + (z - centre.z) * (z - centre.z));
                _line.SetPosition(j, new Vector3(x, _terrain.SampleHeight(x, z) + lift, z));
            }
        }

        /// <summary>
        /// Sets each vertex's width so it shows as a few pixels from the view: refreshed while the ring is drawing out
        /// and whenever the view has moved enough to matter.
        /// </summary>
        private void RefreshWidth(float reveal)
        {
            Camera camera = _view.Camera;
            if (camera == null)
            {
                return;
            }

            Vector3 eye = camera.transform.position;
            bool moved = (eye - _widthFrom).sqrMagnitude > WidthRefreshMetres * WidthRefreshMetres;
            if (!moved && reveal >= 1f && _widthReveal >= 1f)
            {
                return;
            }

            _widthFrom = eye;
            _widthReveal = reveal;
            float focal = RangeRingWidth.FocalPixels(camera.pixelHeight, camera.fieldOfView);
            int count = _vertices;
            for (int i = 0; i < count; i++)
            {
                float width = RangeRingWidth.World(Vector3.Distance(eye, _line.GetPosition(i)), focal,
                    _settings.MaxPixels, _settings.MinPixels, _settings.FullPixelDistance);
                _widthKeys[i] = new Keyframe(i / (float)(count - 1), width);
            }

            _widthCurve.keys = _widthKeys;
            _line.widthCurve = _widthCurve;
        }
    }
}
