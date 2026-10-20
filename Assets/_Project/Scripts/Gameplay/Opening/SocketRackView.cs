using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The socket rack in the world (docs/design/opening-v3-plan.md 4.3): Art's prefab, stood beside the pad's dock at
    /// <see cref="IRackStatus.Position"/> and <see cref="IRackStatus.Rotation"/> once the release reveals it, on the
    /// ground there. <see cref="RackLook"/> says how it rises and how each bay glows; this applies it, writing each
    /// bay's outline, fill and lamp through <c>_EmissionColor</c> property blocks only when their glow changes, and
    /// turning its jaws from their authored open pose to closed. It also says which outline 07 reads
    /// (<see cref="RackAim"/>). Allocation-free per frame once stood.
    /// </summary>
    public sealed class SocketRackView : IDisposable
    {
        private const string BayPrefix = "Bay_";
        private const float GlowEpsilon = 0.002f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly IRackStatus _rack;
        private readonly RackViewSettings _settings;
        private readonly ITerrainQuery _terrain;
        private readonly IViewCamera _view;
        private readonly IRoverState _rover;
        private readonly Transform _parent;
        private readonly RackLook _look;
        private readonly IDisposable _revealed;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private Bay[] _bays = Array.Empty<Bay>();
        private Vector3[] _bayPoints = Array.Empty<Vector3>();
        private Vector3[] _viewPoints = Array.Empty<Vector3>();
        private GameObject _instance;
        private Vector3 _standing;
        private bool _live;
        private bool _snap;
        private bool _failed;

        public SocketRackView(EventBus events, IRackStatus rack, RackViewSettings settings, ITerrainQuery terrain,
            IViewCamera view, IRoverState rover, Transform parent)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _rack = rack ?? throw new ArgumentNullException(nameof(rack));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            _look = new RackLook(rack.Count, settings);
            _revealed = events.Subscribe<SocketRackRevealed>(_ => _live = true);
        }

        /// <summary>The rack stands in the world (tests and captures).</summary>
        public GameObject Instance => _instance;

        /// <summary>The rack's look and place (tests and captures).</summary>
        public RackViewSettings Settings => _settings;

        /// <summary>How the rack looks now (tests).</summary>
        public RackLook Look => _look;

        public void Step(float deltaTime)
        {
            if (_instance == null)
            {
                if (!_rack.Shown || _failed || !Stand())
                {
                    return;
                }
            }

            _look.Step(deltaTime, _rack, _snap);
            _snap = false;
            _instance.transform.position = _standing + Vector3.down * (_settings.RiseDepth * (1f - _look.Rise01));
            for (int i = 0; i < _bays.Length; i++)
            {
                Bay bay = _bays[i];
                bay.OutlineGlow = Glow(bay.OutlineRenderer, bay.OutlineGlow, _look.Outline(i),
                    _settings.OutlineColor, true);
                bay.FillGlow = Glow(bay.FillRenderer, bay.FillGlow, _look.Fill(i), _settings.FillColor, true);
                bay.LampGlow = Glow(bay.LampRenderer, bay.LampGlow, _look.Lamp(i), _settings.LampColor, false);
                float jaw = _look.Jaw(i);
                bay.ClampLeft.localRotation = Quaternion.Slerp(bay.LeftOpen, Quaternion.identity, jaw);
                bay.ClampRight.localRotation = Quaternion.Slerp(bay.RightOpen, Quaternion.identity, jaw);
            }
        }

        /// <summary>
        /// The outline 07 reads now: a seen part's bay, within reach and in view, nearest the centre of the view.
        /// </summary>
        public bool TryGetRead(out Vector3 point, out string bayId)
        {
            point = Vector3.zero;
            bayId = null;
            if (_instance == null || !_look.Standing)
            {
                return false;
            }

            Camera camera = _view.Camera;
            for (int i = 0; i < _bays.Length; i++)
            {
                _bayPoints[i] = _bays[i].OutlineRenderer.bounds.center;
                _viewPoints[i] = camera.WorldToViewportPoint(_bayPoints[i]);
            }

            int bay = RackAim.Pick(_rack, _bayPoints, _viewPoints, _rover.Position, _settings.ReadReach,
                _settings.ViewMargin);
            if (bay < 0)
            {
                return false;
            }

            point = _bayPoints[bay];
            bayId = _rack.IdOf(bay);
            return true;
        }

        public void Dispose()
        {
            _revealed.Dispose();
            if (_instance != null)
            {
                Object.Destroy(_instance);
            }
        }

        /// <summary>Stands Art's rack beside the dock, on the ground; false (logged once) when its nodes are wrong.</summary>
        private bool Stand()
        {
            Quaternion dock = _rack.Rotation;
            Vector3 foot = _rack.Position + dock * _settings.Offset;
            _standing = SurfaceRules.OnSurface(_terrain, foot.x, foot.z);
            // Art's root keeps its import rotation (Blender's Z up turned to Unity's Y up): the yaw goes on top of it.
            Quaternion facing = dock * Quaternion.Euler(0f, _settings.YawFromDock, 0f) *
                                _settings.Prefab.transform.localRotation;
            _instance = Object.Instantiate(_settings.Prefab, _standing, facing, _parent);
            _instance.name = _settings.Prefab.name;
            string problem = Bind(_instance.transform);
            if (problem != null)
            {
                Debug.LogError($"{nameof(SocketRackView)}: {problem}", _instance);
                Object.Destroy(_instance);
                _instance = null;
                _failed = true;
                return false;
            }

            _look.Reveal(_live);
            _snap = !_live;
            return true;
        }

        private string Bind(Transform root)
        {
            if (root.Find(BayPrefix + _rack.Count) != null)
            {
                return $"the prefab has more bays than the rack's {_rack.Count}";
            }

            _bays = new Bay[_rack.Count];
            _bayPoints = new Vector3[_rack.Count];
            _viewPoints = new Vector3[_rack.Count];
            for (int i = 0; i < _bays.Length; i++)
            {
                Transform bay = root.Find(BayPrefix + i);
                if (bay == null)
                {
                    return $"the prefab has no {BayPrefix}{i} for the rack's bay '{_rack.IdOf(i)}'";
                }

                var bound = new Bay
                {
                    ClampLeft = bay.Find("Clamp_L"),
                    ClampRight = bay.Find("Clamp_R"),
                    OutlineRenderer = Renderer(bay, "Outline"),
                    FillRenderer = Renderer(bay, "Fill"),
                    LampRenderer = Renderer(bay, "Lamp"),
                    OutlineGlow = -1f,
                    FillGlow = -1f,
                    LampGlow = -1f,
                };
                if (bound.ClampLeft == null || bound.ClampRight == null ||
                    bound.OutlineRenderer == null || bound.FillRenderer == null || bound.LampRenderer == null)
                {
                    return $"{bay.name} lacks one of Outline, Fill, Lamp (each with a renderer), Clamp_L, Clamp_R";
                }

                bound.LeftOpen = bound.ClampLeft.localRotation;
                bound.RightOpen = bound.ClampRight.localRotation;
                _bays[i] = bound;
            }

            return null;
        }

        private static Renderer Renderer(Transform bay, string node)
        {
            Transform found = bay.Find(node);
            return found != null ? found.GetComponent<Renderer>() : null;
        }

        /// <summary>
        /// Writes a glow only when it moved: the colour times <paramref name="strength"/>. With
        /// <paramref name="hideWhenDark"/> (the outline and the fill) a dark one is not drawn at all, so a part not yet
        /// seen leaves no shape on its bay; the lamp's housing stays, dark.
        /// </summary>
        private float Glow(Renderer renderer, float shown, float strength, Color color, bool hideWhenDark)
        {
            if (Mathf.Abs(strength - shown) < GlowEpsilon)
            {
                return shown;
            }

            renderer.enabled = !hideWhenDark || strength > 0f;
            renderer.GetPropertyBlock(_block);
            _block.SetColor(EmissionColorId, color * strength);
            renderer.SetPropertyBlock(_block);
            return strength;
        }

        private sealed class Bay
        {
            public Transform ClampLeft;
            public Transform ClampRight;
            public Renderer OutlineRenderer;
            public Renderer FillRenderer;
            public Renderer LampRenderer;
            public Quaternion LeftOpen;
            public Quaternion RightOpen;
            public float OutlineGlow;
            public float FillGlow;
            public float LampGlow;
        }
    }
}
