using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Pure, allocation-free framing math and occlusion solver for 07's CRT face-inspect camera mode (FACE-INV).
    /// Frames the <c>Screen</c> mesh (<c>0.36 m</c> wide x <c>0.24 m</c> tall) at ~75% of viewport height from slightly
    /// above (07 pitches its head up <c>+10 deg</c> to meet the camera), and probes terrain/rock occlusion in strict
    /// order: Default (<c>0 deg</c>), Left (<c>-sideYaw</c>), Right (<c>+sideYaw</c>), and Closer (pulling the camera
    /// in along the clearest bearing while widening vertical FOV proportionally so the screen still fills ~75% of view
    /// height).
    /// </summary>
    public static class FaceInspectComposer
    {
        private const float ClearSlack = 1e-3f;
        private const float MinFill = 0.05f;
        private const float MinDistanceFloor = 0.15f;

        /// <summary>
        /// Vertical field of view (deg) required for a screen of height <paramref name="screenHeightM"/> at distance
        /// <paramref name="distanceM"/> to occupy <paramref name="targetHeightFill"/> (e.g. 0.75) of the viewport height.
        /// </summary>
        public static float VerticalFovForFill(float screenHeightM, float targetHeightFill, float distanceM)
        {
            float fill = Mathf.Max(MinFill, targetHeightFill);
            float distance = Mathf.Max(MinDistanceFloor, distanceM);
            float visibleHeight = screenHeightM / fill;
            return 2f * Mathf.Atan(visibleHeight / (2f * distance)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Fraction of viewport height (0..1+) occupied by a screen of height <paramref name="screenHeightM"/> at
        /// <paramref name="distanceM"/> under <paramref name="verticalFovDeg"/>.
        /// </summary>
        public static float VerticalFill(float screenHeightM, float distanceM, float verticalFovDeg)
        {
            float distance = Mathf.Max(MinDistanceFloor, distanceM);
            float halfRad = 0.5f * Mathf.Max(1f, verticalFovDeg) * Mathf.Deg2Rad;
            float visibleHeight = 2f * distance * Mathf.Tan(halfRad);
            return visibleHeight > 1e-5f ? screenHeightM / visibleHeight : 0f;
        }

        /// <summary>
        /// Computes the world-space look focus and camera eye position for a given screen center, screen rotation,
        /// vertical look offset, and camera distance along <c>screenRotation * Vector3.forward</c>.
        /// </summary>
        public static void ComposePose(
            Vector3 screenPosition,
            Quaternion screenRotation,
            float lookOffsetY,
            float distance,
            out Vector3 focus,
            out Vector3 eye)
        {
            Vector3 up = screenRotation * Vector3.up;
            Vector3 forward = screenRotation * Vector3.forward;
            focus = screenPosition + up * lookOffsetY;
            eye = focus + forward * distance;
        }

        /// <summary>
        /// Computes the world-space screen position and rotation when 07's neck turns by <paramref name="neckYawOffsetDeg"/>
        /// around the neck's up axis and the head pitches up by <paramref name="headPitchDeg"/> around the neck's right
        /// axis relative to the zero-yaw lifted screen pose.
        /// </summary>
        public static void PoseForNeckYaw(
            Vector3 neckPivotWorld,
            Quaternion roverRotation,
            Vector3 baseScreenPosition,
            Quaternion baseScreenRotation,
            float neckYawOffsetDeg,
            out Vector3 screenPosition,
            out Quaternion screenRotation)
        {
            if (Mathf.Abs(neckYawOffsetDeg) <= 1e-4f)
            {
                screenPosition = baseScreenPosition;
                screenRotation = baseScreenRotation;
                return;
            }

            Vector3 neckUp = roverRotation * Vector3.up;
            Quaternion yawRot = Quaternion.AngleAxis(neckYawOffsetDeg, neckUp);
            screenPosition = neckPivotWorld + yawRot * (baseScreenPosition - neckPivotWorld);
            screenRotation = yawRot * baseScreenRotation;
        }

        /// <summary>
        /// Solves the face-inspect shot by probing terrain/rock occlusion in strict order:
        /// 1. Default (<c>0 deg</c> yaw, <paramref name="defaultDistance"/>)
        /// 2. Left (<c>-sideYawDeg</c> yaw, <paramref name="defaultDistance"/>)
        /// 3. Right (<c>+sideYawDeg</c> yaw, <paramref name="defaultDistance"/>)
        /// 4. Closer (pulls in along the clearest bearing to <c>[minDistance..defaultDistance]</c> and widens vertical
        ///    FOV so the screen still fills <paramref name="targetHeightFill"/> of the viewport height).
        /// </summary>
        public static FaceInspectShot Solve(
            Vector3 neckPivotWorld,
            Quaternion roverRotation,
            Vector3 baseScreenPosition,
            Quaternion baseScreenRotation,
            float screenHeightM,
            float targetHeightFill,
            float defaultDistance,
            float minDistance,
            float lookOffsetY,
            float sideYawDeg,
            float probeRadius,
            ISightLine sight)
        {
            float absSideYaw = Mathf.Abs(sideYawDeg);
            float baseFov = VerticalFovForFill(screenHeightM, targetHeightFill, defaultDistance);

            // 1. Default (0 deg)
            PoseForNeckYaw(neckPivotWorld, roverRotation, baseScreenPosition, baseScreenRotation, 0f,
                out Vector3 defScreenPos, out Quaternion defScreenRot);
            ComposePose(defScreenPos, defScreenRot, lookOffsetY, defaultDistance, out Vector3 defFocus, out Vector3 defEye);
            if (sight == null || IsCandidateClear(sight, defFocus, defEye, defaultDistance, probeRadius, out float defClear))
            {
                return new FaceInspectShot(defFocus, defEye, defaultDistance, baseFov, 0f,
                    FaceInspectProbeChoice.Default, defScreenRot * Vector3.up);
            }

            // 2. Left (-sideYawDeg)
            float leftYaw = -absSideYaw;
            PoseForNeckYaw(neckPivotWorld, roverRotation, baseScreenPosition, baseScreenRotation, leftYaw,
                out Vector3 leftScreenPos, out Quaternion leftScreenRot);
            ComposePose(leftScreenPos, leftScreenRot, lookOffsetY, defaultDistance, out Vector3 leftFocus, out Vector3 leftEye);
            if (IsCandidateClear(sight, leftFocus, leftEye, defaultDistance, probeRadius, out float leftClear))
            {
                return new FaceInspectShot(leftFocus, leftEye, defaultDistance, baseFov, leftYaw,
                    FaceInspectProbeChoice.Left, leftScreenRot * Vector3.up);
            }

            // 3. Right (+sideYawDeg)
            float rightYaw = absSideYaw;
            PoseForNeckYaw(neckPivotWorld, roverRotation, baseScreenPosition, baseScreenRotation, rightYaw,
                out Vector3 rightScreenPos, out Quaternion rightScreenRot);
            ComposePose(rightScreenPos, rightScreenRot, lookOffsetY, defaultDistance, out Vector3 rightFocus, out Vector3 rightEye);
            if (IsCandidateClear(sight, rightFocus, rightEye, defaultDistance, probeRadius, out float rightClear))
            {
                return new FaceInspectShot(rightFocus, rightEye, defaultDistance, baseFov, rightYaw,
                    FaceInspectProbeChoice.Right, rightScreenRot * Vector3.up);
            }

            // 4. Closer: choose the bearing with the greatest clear reach (preferring Default on ties) and pull in.
            float bestReach = defClear;
            float bestYaw = 0f;
            Vector3 bestScreenPos = defScreenPos;
            Quaternion bestScreenRot = defScreenRot;

            if (leftClear > bestReach + ClearSlack)
            {
                bestReach = leftClear;
                bestYaw = leftYaw;
                bestScreenPos = leftScreenPos;
                bestScreenRot = leftScreenRot;
            }

            if (rightClear > bestReach + ClearSlack)
            {
                bestReach = rightClear;
                bestYaw = rightYaw;
                bestScreenPos = rightScreenPos;
                bestScreenRot = rightScreenRot;
            }

            float safeDistance = Mathf.Clamp(bestReach - probeRadius, Mathf.Min(minDistance, defaultDistance), defaultDistance);
            ComposePose(bestScreenPos, bestScreenRot, lookOffsetY, safeDistance, out Vector3 closerFocus, out Vector3 closerEye);
            float closerFov = VerticalFovForFill(screenHeightM, targetHeightFill, safeDistance);
            return new FaceInspectShot(closerFocus, closerEye, safeDistance, closerFov, bestYaw,
                FaceInspectProbeChoice.Closer, bestScreenRot * Vector3.up);
        }

        private static bool IsCandidateClear(
            ISightLine sight,
            Vector3 focus,
            Vector3 eye,
            float distance,
            float probeRadius,
            out float clearDistance)
        {
            clearDistance = sight.Clear(focus, eye, probeRadius);
            return clearDistance >= distance - ClearSlack && sight.IsFree(eye, probeRadius);
        }
    }
}
