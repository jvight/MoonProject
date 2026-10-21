using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The range ring drawn on the ground (<see cref="RangeRingField"/>): one closed, quiet line along the polygon,
    /// lifted a little above the ground. It draws outward from 07 to its edge, holds, then fades slowly away. It is a
    /// few pixels wide on screen at most, thinner with distance (<see cref="RangeRingWidth"/>), so it reads as a line
    /// drawn on the ground, not a band on the horizon. Allocation-free while it is not shown; while it is, the width
    /// curve is refreshed only when the view has moved.
    /// </summary>
    public sealed class RangeRingLine
    {
        private const float WidthRefreshMetres = 0.5f;

        private readonly RangeRingField _ring;
        private readonly RangeRingSettings _settings;
        private readonly ITerrainQuery _terrain;
        private readonly IViewCamera _view;
        private readonly LineRenderer _line;
        private readonly Keyframe[] _widthKeys;
        private readonly AnimationCurve _widthCurve = new AnimationCurve();
        private readonly int _vertices;
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
            _vertices = ring.Count * Mathf.Max(1, settings.Subdivisions);
            _line.positionCount = _vertices;
            _widthKeys = new Keyframe[_vertices];
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
                _widthReveal = -1f;
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

            RefreshWidth(reveal);
            _line.enabled = true;
        }

        /// <summary>The ring drawn out to <paramref name="reveal"/> of the way from 07 to its edge, on the ground.</summary>
        private void Place(float reveal)
        {
            Vector3 centre = _ring.Centre;
            int sub = Mathf.Max(1, _settings.Subdivisions);
            for (int j = 0; j < _vertices; j++)
            {
                int i = j / sub;
                Vector3 edge = Vector3.Lerp(_ring.Point(i), _ring.Point((i + 1) % _ring.Count), (j % sub) / (float)sub);
                float x = Mathf.Lerp(centre.x, edge.x, reveal);
                float z = Mathf.Lerp(centre.z, edge.z, reveal);
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
