using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// The Tab "07" page's live portrait of 07 (<see cref="IRoverPortrait"/>, 0.5.5 §3.5): a camera riding 07's
    /// Visual root at <see cref="PortraitFraming"/>'s pose, seeing only the Rover layer (07's model and its fitted
    /// parts) against transparency, into one render texture. It is disabled unless the page is open, so it costs
    /// nothing otherwise. Its own warm key and cool rim light (Rover layer only) are on only while its camera renders,
    /// so the portrait reads the same day or night and 07 in the world never changes. Socket positions come from
    /// art's <c>&lt;Socket&gt;Socket</c> nodes on RoverModel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverPortrait : MonoBehaviour, IRoverPortrait
    {
        private const int DepthBits = 16;

        [Tooltip("The rig tuning holding the portrait's framing.")]
        [SerializeField] private RoverRigTuning _tuning;

        [Tooltip("The portrait camera, a child of this Visual root.")]
        [SerializeField] private Camera _camera;

        [Tooltip("The portrait's warm key light, a child of this Visual root.")]
        [SerializeField] private Light _key;

        [Tooltip("The portrait's cool rim light, a child of this Visual root.")]
        [SerializeField] private Light _rim;

        [Tooltip("Art's socket nodes, one per RoverSocket in order; empty where art has none.")]
        [SerializeField] private Transform[] _sockets = new Transform[0];

        private RenderTexture _texture;

        public RenderTexture Texture => _texture;

        /// <summary>Makes the render texture and points the camera; false (with an error) on broken wiring.</summary>
        public bool Initialize()
        {
            if (_tuning == null || _camera == null || _camera.transform.parent != transform || _key == null
                || _rim == null)
            {
                Debug.LogError("RoverPortrait needs its tuning, its camera and its key and rim lights.", this);
                enabled = false;
                return false;
            }

            PortraitSettings settings = _tuning.Portrait;
            _texture = new RenderTexture(settings.Width, settings.Height, DepthBits, RenderTextureFormat.ARGB32)
            {
                name = "RoverPortrait",
            };
            PortraitFraming.Pose(settings, out Vector3 position, out Quaternion rotation);
            _camera.transform.SetLocalPositionAndRotation(position, rotation);
            _camera.fieldOfView = settings.FieldOfView;
            _camera.nearClipPlane = settings.Near;
            _camera.farClipPlane = settings.Far;
            _camera.aspect = settings.Aspect;
            _camera.cullingMask = Layers.RoverMask;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.targetTexture = _texture;
            _camera.enabled = false;
            Configure(_key, settings.KeyColour, settings.KeyIntensity, settings.KeyRotation);
            Configure(_rim, settings.RimColour, settings.RimIntensity, settings.RimRotation);
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            RenderPipelineManager.endCameraRendering += OnEndCamera;
            return true;
        }

        public void SetActive(bool active)
        {
            _camera.enabled = active;
        }

        public bool TryGetSocketViewport(RoverSocket socket, out Vector2 uv)
        {
            int index = (int)socket;
            if (index < 0 || index >= _sockets.Length || _sockets[index] == null)
            {
                uv = default;
                return false;
            }

            Vector3 local = transform.InverseTransformPoint(_sockets[index].position);
            return PortraitFraming.TryViewport(_tuning.Portrait, local, out uv);
        }

        private static void Configure(Light light, Color colour, float intensity, Quaternion rotation)
        {
            light.type = LightType.Directional;
            light.color = colour;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            light.cullingMask = Layers.RoverMask;
            light.transform.localRotation = rotation;
            light.enabled = false;
        }

        private void OnBeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera)
            {
                _key.enabled = true;
                _rim.enabled = true;
            }
        }

        private void OnEndCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera)
            {
                _key.enabled = false;
                _rim.enabled = false;
            }
        }

        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            RenderPipelineManager.endCameraRendering -= OnEndCamera;
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }
    }
}
