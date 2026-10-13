using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// The Tab "07" page's live portrait of 07 (<see cref="IRoverPortrait"/>, 0.5.5 §3.5): a camera riding 07's
    /// Visual root at <see cref="PortraitFraming"/>'s pose, seeing only the Rover layer (07's model and its fitted
    /// parts) against transparency, into one render texture. It is disabled unless the page is open, so it costs
    /// nothing otherwise. Socket positions come from art's <c>&lt;Socket&gt;Socket</c> nodes on RoverModel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverPortrait : MonoBehaviour, IRoverPortrait
    {
        private const int DepthBits = 16;

        [Tooltip("The rig tuning holding the portrait's framing.")]
        [SerializeField] private RoverRigTuning _tuning;

        [Tooltip("The portrait camera, a child of this Visual root.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Art's socket nodes, one per RoverSocket in order; empty where art has none.")]
        [SerializeField] private Transform[] _sockets = new Transform[0];

        private RenderTexture _texture;

        public RenderTexture Texture => _texture;

        /// <summary>Makes the render texture and points the camera; false (with an error) on broken wiring.</summary>
        public bool Initialize()
        {
            if (_tuning == null || _camera == null || _camera.transform.parent != transform)
            {
                Debug.LogError("RoverPortrait needs its tuning and its camera as a child of the Visual root.", this);
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

        private void OnDestroy()
        {
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }
    }
}
