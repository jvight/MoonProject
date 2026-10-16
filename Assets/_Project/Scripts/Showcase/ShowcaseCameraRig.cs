using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Showcase
{
    /// <summary>
    /// Camera controller for the Showcase scene. Supports smooth mouse-orbit around the focused exhibit (selected via
    /// Tab or 1-6) with eased transitions that never snap, and seamless WASD + Q/E + Shift free-fly movement.
    /// </summary>
    public sealed class ShowcaseCameraRig : MonoBehaviour
    {
        private const float GroundClearanceMetres = 0.45f;

        [Tooltip("The rendering Camera driven by this rig.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Shared Showcase tuning asset.")]
        [SerializeField] private ShowcaseTuning _tuning;

        private ShowcaseExhibitRig[] _exhibits;
        private ITerrainQuery _terrain;
        private int _focusedIndex;
        private bool _isFreeFly;

        private Vector3 _currentOrbitPivot;
        private Vector3 _targetOrbitPivot;
        private float _currentYaw;
        private float _targetYaw;
        private float _currentPitch;
        private float _targetPitch;
        private float _currentDistance;
        private float _targetDistance;

        public Camera Camera => _camera;
        public int FocusedExhibitIndex => _focusedIndex;
        public bool IsFreeFly => _isFreeFly;
        public ShowcaseExhibitRig FocusedExhibit =>
            (_exhibits != null && _focusedIndex >= 0 && _focusedIndex < _exhibits.Length)
                ? _exhibits[_focusedIndex]
                : null;

        public void Initialize(ShowcaseTuning tuning, ShowcaseExhibitRig[] exhibits, ITerrainQuery terrain)
        {
            if (tuning != null)
            {
                _tuning = tuning;
            }

            _exhibits = exhibits;
            _terrain = terrain;
            _isFreeFly = false;
            SelectExhibitImmediate(0);
        }

        /// <summary>
        /// Smoothly transitions the camera into orbit around the exhibit at <paramref name="index"/> without snapping.
        /// </summary>
        public void FocusExhibit(int index)
        {
            if (_exhibits == null || _exhibits.Length == 0)
            {
                return;
            }

            int wrapped = ((index % _exhibits.Length) + _exhibits.Length) % _exhibits.Length;
            ShowcaseExhibitRig targetRig = _exhibits[wrapped];
            if (targetRig == null)
            {
                return;
            }

            if (_isFreeFly)
            {
                SyncOrbitStateFromCurrentTransform(targetRig.OrbitPivotWorld);
                _isFreeFly = false;
            }

            _focusedIndex = wrapped;
            ShowcaseExhibitDefinition def = targetRig.Definition;
            _targetOrbitPivot = targetRig.OrbitPivotWorld;
            _targetYaw = def != null ? def.DefaultOrbitYaw : 155f;
            _targetPitch = def != null ? def.DefaultOrbitPitch : 12f;
            _targetDistance = def != null ? def.DefaultOrbitDistance : 4.2f;
        }

        public void FocusNextExhibit()
        {
            FocusExhibit(_focusedIndex + 1);
        }

        /// <summary>
        /// Immediately positions the camera on an exhibit orbit (used on initial scene boot and deterministic capture
        /// framing).
        /// </summary>
        public void SelectExhibitImmediate(
            int index,
            float? overrideYaw = null,
            float? overridePitch = null,
            float? overrideDistance = null,
            Vector3? overridePivotOffset = null)
        {
            if (_exhibits == null || _exhibits.Length == 0)
            {
                return;
            }

            _focusedIndex = ((index % _exhibits.Length) + _exhibits.Length) % _exhibits.Length;
            _isFreeFly = false;
            ShowcaseExhibitRig rig = _exhibits[_focusedIndex];
            if (rig == null)
            {
                return;
            }

            ShowcaseExhibitDefinition def = rig.Definition;
            Vector3 pivot = overridePivotOffset.HasValue
                ? rig.transform.position + overridePivotOffset.Value
                : rig.OrbitPivotWorld;
            float yaw = overrideYaw ?? (def != null ? def.DefaultOrbitYaw : 155f);
            float pitch = overridePitch ?? (def != null ? def.DefaultOrbitPitch : 12f);
            float distance = overrideDistance ?? (def != null ? def.DefaultOrbitDistance : 4.2f);

            _currentOrbitPivot = pivot;
            _targetOrbitPivot = pivot;
            _currentYaw = yaw;
            _targetYaw = yaw;
            _currentPitch = pitch;
            _targetPitch = pitch;
            _currentDistance = distance;
            _targetDistance = distance;

            ApplyOrbitTransform();
        }

        public void StepCamera(
            float deltaTime,
            Vector2 moveInput,
            float verticalInput,
            bool fastHeld,
            Vector2 mouseDelta,
            Vector2 gamepadLook,
            float scrollDelta)
        {
            if (_tuning == null || deltaTime <= 0f)
            {
                return;
            }

            float yawDelta = mouseDelta.x * _tuning.LookSensitivity
                + gamepadLook.x * _tuning.GamepadLookRate * deltaTime;
            float pitchDelta = -mouseDelta.y * _tuning.LookSensitivity
                - gamepadLook.y * _tuning.GamepadLookRate * deltaTime;

            bool hasFlyMove = moveInput.sqrMagnitude > 1e-4f || Mathf.Abs(verticalInput) > 1e-4f;
            if (hasFlyMove && !_isFreeFly)
            {
                _isFreeFly = true;
                _targetYaw = _currentYaw;
                _targetPitch = _currentPitch;
            }

            if (_isFreeFly)
            {
                StepFreeFly(deltaTime, moveInput, verticalInput, fastHeld, yawDelta, pitchDelta);
                return;
            }

            StepOrbit(deltaTime, yawDelta, pitchDelta, scrollDelta);
        }

        private void StepOrbit(float deltaTime, float yawDelta, float pitchDelta, float scrollDelta)
        {
            ShowcaseExhibitRig rig = FocusedExhibit;

            _targetYaw += yawDelta;
            _targetPitch = Mathf.Clamp(_targetPitch + pitchDelta, _tuning.MinPitchDegrees, _tuning.MaxPitchDegrees);

            if (Mathf.Abs(scrollDelta) > 1e-4f)
            {
                ShowcaseExhibitDefinition def = rig != null ? rig.Definition : null;
                float minDist = def != null ? def.MinOrbitDistance : 1.5f;
                float maxDist = def != null ? def.MaxOrbitDistance : 14f;
                float scrollNotches = Mathf.Clamp(scrollDelta, -3f, 3f);
                _targetDistance = Mathf.Clamp(_targetDistance - scrollNotches * _tuning.OrbitZoomStep, minDist, maxDist);
            }

            float easeTime = _tuning.OrbitTransitionSeconds;
            _currentOrbitPivot = new Vector3(
                ShowcaseEasing.SmoothScalar(_currentOrbitPivot.x, _targetOrbitPivot.x, easeTime, deltaTime),
                ShowcaseEasing.SmoothScalar(_currentOrbitPivot.y, _targetOrbitPivot.y, easeTime, deltaTime),
                ShowcaseEasing.SmoothScalar(_currentOrbitPivot.z, _targetOrbitPivot.z, easeTime, deltaTime));

            float angleEase = Mathf.Min(0.32f, easeTime * 0.5f);
            _currentYaw = ShowcaseEasing.SmoothAngle(_currentYaw, _targetYaw, angleEase, deltaTime);
            _currentPitch = ShowcaseEasing.SmoothScalar(_currentPitch, _targetPitch, angleEase, deltaTime);
            _currentDistance = ShowcaseEasing.SmoothScalar(_currentDistance, _targetDistance, easeTime * 0.65f, deltaTime);

            ApplyOrbitTransform();
        }

        private void StepFreeFly(
            float deltaTime,
            Vector2 moveInput,
            float verticalInput,
            bool fastHeld,
            float yawDelta,
            float pitchDelta)
        {
            _targetYaw += yawDelta;
            _targetPitch = Mathf.Clamp(_targetPitch + pitchDelta, _tuning.MinPitchDegrees, _tuning.MaxPitchDegrees);

            _currentYaw = ShowcaseEasing.SmoothAngle(_currentYaw, _targetYaw, 0.12f, deltaTime);
            _currentPitch = ShowcaseEasing.SmoothScalar(_currentPitch, _targetPitch, 0.12f, deltaTime);

            Quaternion lookRot = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            transform.rotation = lookRot;

            float speedMultiplier = fastHeld ? _tuning.FreeFlyFastMultiplier : 1f;
            float moveSpeed = _tuning.FreeFlySpeed * speedMultiplier;
            float verticalSpeed = _tuning.FreeFlyVerticalSpeed * speedMultiplier;

            Vector3 forward = lookRot * Vector3.forward;
            Vector3 right = lookRot * Vector3.right;
            Vector3 velocity = (forward * moveInput.y + right * moveInput.x) * moveSpeed
                + Vector3.up * (verticalInput * verticalSpeed);

            Vector3 nextPos = transform.position + velocity * deltaTime;
            nextPos.y = ClampAboveGround(nextPos.x, nextPos.y, nextPos.z);
            transform.position = nextPos;
        }

        private void ApplyOrbitTransform()
        {
            Quaternion rot = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            Vector3 pos = _currentOrbitPivot - (rot * Vector3.forward) * _currentDistance;
            pos.y = ClampAboveGround(pos.x, pos.y, pos.z);
            transform.SetPositionAndRotation(pos, rot);
        }

        private void SyncOrbitStateFromCurrentTransform(Vector3 nextPivot)
        {
            Vector3 toCam = transform.position - nextPivot;
            float dist = toCam.magnitude;
            _currentOrbitPivot = transform.position + transform.forward * Mathf.Clamp(dist, 2f, 8f);
            _currentDistance = Mathf.Clamp(dist, 2f, 12f);
            Vector3 euler = transform.eulerAngles;
            _currentYaw = euler.y;
            _currentPitch = ShowcaseEasing.NormalizeSignedAngle(euler.x);
        }

        private float ClampAboveGround(float x, float y, float z)
        {
            if (_terrain == null)
            {
                return Mathf.Max(GroundClearanceMetres, y);
            }

            float minGround = _terrain.SampleHeight(x, z) + GroundClearanceMetres;
            return Mathf.Max(minGround, y);
        }
    }
}
