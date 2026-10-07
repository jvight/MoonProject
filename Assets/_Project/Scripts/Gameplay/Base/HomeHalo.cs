using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The soft amber glow over home seen from across the basin (VISION pillar 6: from far away the base is a small
    /// amber cluster). A camera-facing SoftGlow sprite over the lander, placed for each camera just before it renders,
    /// so every view (the game's, a review camera's) sees it right: invisible up close, it fades in with distance and
    /// grows slower than home shrinks on screen. It is pulled toward the camera by its own radius so the lander never
    /// cuts it. Allocation-free per frame.
    /// </summary>
    internal sealed class HomeHalo : IDisposable
    {
        /// <summary>The halo never comes closer to the camera than this share of its distance.</summary>
        private const float MaxPull = 0.5f;

        private readonly BaseTuning _tuning;
        private readonly Transform _lander;
        private readonly Transform _sprite;
        private readonly GlowRenderer _glow;
        private readonly Action<ScriptableRenderContext, Camera> _beforeCamera;
        private bool _disposed;

        public HomeHalo(Transform parent, Mesh quad, Material material, BaseTuning tuning, Transform lander)
        {
            if (quad == null || material == null)
            {
                throw new ArgumentNullException(quad == null ? nameof(quad) : nameof(material));
            }

            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _lander = lander != null ? lander : throw new ArgumentNullException(nameof(lander));
            MeshRenderer sprite = GlowObject.Create("HomeHalo", parent, quad, material);
            _sprite = sprite.transform;
            _glow = new GlowRenderer(sprite);
            _beforeCamera = BeforeCamera;
            RenderPipelineManager.beginCameraRendering += _beforeCamera;
        }

        /// <summary>Multiplies the glow (the tower's light boost).</summary>
        public float Boost { get; set; } = 1f;

        /// <summary>Glow as last placed (for the camera that rendered last).</summary>
        public float Level => _glow.Intensity;

        /// <summary>Radius (m) as last placed.</summary>
        public float Radius { get; private set; }

        public Vector3 Centre => _lander.TransformPoint(_tuning.HaloCentre);

        /// <summary>Places and lights the halo for an eye at <paramref name="eye"/>.</summary>
        public void ShowFrom(Vector3 eye)
        {
            Vector3 centre = Centre;
            Vector3 toEye = eye - centre;
            float distance = toEye.magnitude;
            _glow.Apply(_tuning.HaloGlowAt(distance) * Boost);
            if (_glow.Intensity <= GlowRenderer.VisibleThreshold)
            {
                return;
            }

            Vector3 facing = toEye / distance;
            Radius = _tuning.HaloRadiusAt(distance);
            _sprite.SetPositionAndRotation(centre + facing * Mathf.Min(Radius, distance * MaxPull),
                Quaternion.LookRotation(facing));
            _sprite.localScale = new Vector3(Radius, Radius, Radius);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            RenderPipelineManager.beginCameraRendering -= _beforeCamera;
        }

        private void BeforeCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
            {
                _glow.Apply(0f);
                return;
            }

            ShowFrom(camera.transform.position);
        }
    }
}
