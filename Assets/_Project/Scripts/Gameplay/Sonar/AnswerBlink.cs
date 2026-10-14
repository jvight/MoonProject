using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The warm lamp blink on an answering place or relic itself when 07 listens: two or three slow blinks of its own
    /// lamp, then it fades out. A small camera-facing warm halo sprite sits at the lamp's position with a minimum
    /// apparent angle so an answer at 140 m still reads as a few warm pixels on screen (under the bloom threshold
    /// except at its core, with nothing on bare ground and nothing in the sky). Allocation-free after construction.
    /// </summary>
    internal sealed class AnswerBlink : IDisposable
    {
        /// <summary>The halo never comes closer to the camera than this share of its distance.</summary>
        private const float MaxPull = 0.5f;

        private readonly SonarTuning _tuning;
        private readonly Transform _sprite;
        private readonly GlowRenderer _glow;
        private readonly Action<ScriptableRenderContext, Camera> _beforeCamera;
        private float _startTime = float.NegativeInfinity;
        private float _peakBrightness;
        private float _linger;
        private bool _disposed;

        public AnswerBlink(string name, Transform parent, SonarTuning tuning, Material haloMaterial, Mesh quadMesh)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            if (haloMaterial == null || quadMesh == null)
            {
                throw new ArgumentNullException(haloMaterial == null ? nameof(haloMaterial) : nameof(quadMesh));
            }

            MeshRenderer sprite = GlowObject.Create(name, parent, quadMesh, haloMaterial);
            _sprite = sprite.transform;
            _glow = new GlowRenderer(sprite);
            _beforeCamera = BeforeCamera;
            RenderPipelineManager.beginCameraRendering += _beforeCamera;
        }

        /// <summary>World position of the answering lamp.</summary>
        public Vector3 Position { get; private set; }

        /// <summary>Current blink intensity (0 when silent).</summary>
        public float Intensity => _glow.Intensity;

        /// <summary>World-space halo radius (m) as last placed for a camera.</summary>
        public float Radius { get; private set; }

        /// <summary>Whether this answer is currently in its blink window at <paramref name="now"/>.</summary>
        public bool Blinking(float now)
        {
            return now >= _startTime && now < _startTime + _tuning.BlinkDuration + _linger;
        }

        /// <summary>Starts a new blink at <paramref name="position"/> with peak <paramref name="brightness"/>.</summary>
        public void Answer(Vector3 position, float brightness, float now)
        {
            Position = position;
            _sprite.position = position;
            _startTime = now;
            _peakBrightness = brightness;
            _linger = 0f;
        }

        /// <summary>
        /// The Warm Headlamp's gift to the sonar: an answer lit by the headlamp ages slower so it keeps blinking a
        /// little longer, up to <paramref name="maxLinger"/> extra seconds.
        /// </summary>
        public void Linger(float seconds, float maxLinger, float now)
        {
            if (seconds > 0f && Blinking(now))
            {
                _linger = Mathf.Min(_linger + seconds, maxLinger);
            }
        }

        public void Tick(Vector3 position, bool stillThere, float now)
        {
            if (!stillThere)
            {
                _startTime = float.NegativeInfinity;
                _linger = 0f;
                _glow.Apply(0f);
                return;
            }

            Position = position;
            float duration = _tuning.BlinkDuration + _linger;
            float age = now - _startTime;
            float envelope = MarkerEnvelope.Blink(age, duration, _tuning.BlinkPeriod);
            float intensity = _peakBrightness * envelope;
            if (intensity <= GlowRenderer.VisibleThreshold)
            {
                intensity = 0f;
            }

            _glow.Apply(intensity);
            if (intensity > 0f)
            {
                _sprite.position = position;
            }
        }

        /// <summary>Places and scales the camera-facing halo for an eye at <paramref name="eye"/>.</summary>
        public void ShowFrom(Vector3 eye)
        {
            if (_glow.Intensity <= GlowRenderer.VisibleThreshold)
            {
                return;
            }

            Vector3 toEye = eye - Position;
            float distance = toEye.magnitude;
            if (distance <= 1e-4f)
            {
                return;
            }

            Vector3 facing = toEye / distance;
            Radius = _tuning.BlinkRadiusAt(distance);
            float pull = Mathf.Min(Mathf.Max(Radius, 0.8f), distance * MaxPull);
            _sprite.SetPositionAndRotation(Position + facing * pull, Quaternion.LookRotation(facing));
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
            if (_sprite != null)
            {
                UnityEngine.Object.Destroy(_sprite.gameObject);
            }
        }

        private void BeforeCamera(ScriptableRenderContext context, Camera camera)
        {
            if (_glow.Intensity <= GlowRenderer.VisibleThreshold)
            {
                return;
            }

            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
            {
                return;
            }

            ShowFrom(camera.transform.position);
        }
    }
}
