using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Cheek-lens aim math, head-reach vs whole-body turn evaluation, and self-occlusion line-of-sight checks for 07's
    /// scan lens (<c>IRoverRig.ScanOrigin</c>). Allocation-free per frame.
    /// </summary>
    public static class ScanAim
    {
        private const float MinDistance = 1e-4f;
        private const float MaxParallaxSin = 0.95f;
        private const float HeadClearance = 0.03f;

        /// <summary>
        /// Unclamped neck yaw (x, deg, + = right) and head pitch (y, deg, + = up) so that the cheek scan lens at
        /// <paramref name="lensOffsetInHead"/> (in <c>Head</c> local space) aims its +Z forward axis directly at
        /// <paramref name="targetFromHeadInNeckFrame"/> (target minus head pivot, in neck parent space).
        /// </summary>
        public static Vector2 UnclampedLensAngles(Vector3 targetFromHeadInNeckFrame, Vector3 lensOffsetInHead)
        {
            float tx = targetFromHeadInNeckFrame.x;
            float ty = targetFromHeadInNeckFrame.y;
            float tz = targetFromHeadInNeckFrame.z;
            float horizSq = tx * tx + tz * tz;
            float horiz = Mathf.Sqrt(horizSq);
            if (horiz < MinDistance && Mathf.Abs(ty) < MinDistance)
            {
                return Vector2.zero;
            }

            float baseYawRad = horiz < MinDistance ? 0f : Mathf.Atan2(tx, tz);
            float parallaxSin = horiz > MinDistance
                ? Mathf.Clamp(lensOffsetInHead.x / Mathf.Max(horiz, Mathf.Abs(lensOffsetInHead.x) + 0.05f),
                    -MaxParallaxSin, MaxParallaxSin)
                : 0f;
            float yawDeg = (baseYawRad - Mathf.Asin(parallaxSin)) * Mathf.Rad2Deg;

            float forwardHoriz = Mathf.Sqrt(Mathf.Max(0.01f, horizSq - lensOffsetInHead.x * lensOffsetInHead.x));
            float vertDist = Mathf.Sqrt(ty * ty + forwardHoriz * forwardHoriz);
            float basePitchRad = Mathf.Atan2(ty, forwardHoriz);
            float pitchParallaxSin = vertDist > MinDistance
                ? Mathf.Clamp(lensOffsetInHead.y / Mathf.Max(vertDist, Mathf.Abs(lensOffsetInHead.y) + 0.05f),
                    -MaxParallaxSin, MaxParallaxSin)
                : 0f;
            float pitchDeg = (basePitchRad - Mathf.Asin(pitchParallaxSin)) * Mathf.Rad2Deg;
            return new Vector2(yawDeg, pitchDeg);
        }

        /// <summary>
        /// Neck yaw (x, deg) and head pitch (y, deg) clamped to 07's mechanical neck and head limits.
        /// </summary>
        public static Vector2 LensAngles(
            Vector3 targetFromHeadInNeckFrame,
            Vector3 lensOffsetInHead,
            float yawLimit,
            float pitchUpLimit,
            float pitchDownLimit)
        {
            Vector2 raw = UnclampedLensAngles(targetFromHeadInNeckFrame, lensOffsetInHead);
            return new Vector2(
                Mathf.Clamp(raw.x, -yawLimit, yawLimit),
                Mathf.Clamp(raw.y, -pitchDownLimit, pitchUpLimit));
        }

        /// <summary>
        /// True when the scan target is outside 07's comfortable head scan reach (or within the hysteresis window while
        /// a body turn is already under way), requiring 07 to turn its whole body in place before the head finishes the aim.
        /// </summary>
        public static bool NeedsBodyTurn(
            Vector3 targetFromHeadInNeckFrame,
            Vector3 lensOffsetInHead,
            float headYawReach,
            float bodyStopYaw,
            float pitchUpLimit,
            float pitchDownLimit,
            bool currentlyTurningBody,
            out float bodyYawDeltaDeg)
        {
            Vector2 raw = UnclampedLensAngles(targetFromHeadInNeckFrame, lensOffsetInHead);
            bodyYawDeltaDeg = raw.x;
            float absYaw = Mathf.Abs(raw.x);
            bool pitchOut = raw.y > pitchUpLimit || raw.y < -pitchDownLimit;
            if (absYaw > headYawReach || (pitchOut && absYaw > bodyStopYaw))
            {
                return true;
            }

            return currentlyTurningBody && absYaw > bodyStopYaw;
        }

        /// <summary>
        /// True when <paramref name="scanOrigin"/>'s forward axis is aimed within <paramref name="maxAimAngleDeg"/> of
        /// <paramref name="targetPoint"/> and the ray from the lens to the target does not pass through 07's own
        /// colliders or meshes.
        /// </summary>
        public static bool HasLineOfSight(
            Transform scanOrigin,
            Vector3 targetPoint,
            float maxAimAngleDeg,
            Transform head,
            Collider[] selfColliders,
            MeshFilter[] selfMeshes)
        {
            if (scanOrigin == null)
            {
                return false;
            }

            Vector3 origin = scanOrigin.position;
            Vector3 toTarget = targetPoint - origin;
            float distance = toTarget.magnitude;
            if (distance <= MinDistance)
            {
                return true;
            }

            Vector3 dir = toTarget / distance;
            if (Vector3.Angle(scanOrigin.forward, dir) > maxAimAngleDeg)
            {
                return false;
            }

            Vector3 lensInHead = head != null ? head.InverseTransformPoint(origin) : Vector3.zero;
            return !IsRaySelfOccluded(origin, dir, distance, head, lensInHead, selfColliders, selfMeshes);
        }

        /// <summary>
        /// True when the ray from <paramref name="rayOrigin"/> along <paramref name="rayDir"/> over
        /// <paramref name="distance"/> intersects 07's head housing, colliders, or structural meshes.
        /// </summary>
        public static bool IsRaySelfOccluded(
            Vector3 rayOrigin,
            Vector3 rayDir,
            float distance,
            Transform head,
            Vector3 lensOffsetInHead,
            Collider[] selfColliders,
            MeshFilter[] selfMeshes)
        {
            if (head != null && HitsHeadHousing(rayOrigin, rayDir, distance, head, lensOffsetInHead))
            {
                return true;
            }

            var ray = new Ray(rayOrigin, rayDir);
            if (selfColliders != null)
            {
                for (int i = 0; i < selfColliders.Length; i++)
                {
                    Collider col = selfColliders[i];
                    if (col == null || !col.enabled || col.isTrigger ||
                        (head != null && (col.transform == head || col.transform.IsChildOf(head))))
                    {
                        continue;
                    }

                    if (col.Raycast(ray, out RaycastHit hit, distance)
                        && hit.distance > 0.02f && hit.distance < distance - 0.02f)
                    {
                        return true;
                    }
                }
            }

            if (selfMeshes != null)
            {
                for (int i = 0; i < selfMeshes.Length; i++)
                {
                    if (HitsStructuralMesh(rayOrigin, rayDir, distance, head, selfMeshes[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HitsHeadHousing(
            Vector3 rayOrigin,
            Vector3 rayDir,
            float distance,
            Transform head,
            Vector3 lensOffsetInHead)
        {
            Vector3 localOrigin = head.InverseTransformPoint(rayOrigin);
            Vector3 localEnd = head.InverseTransformPoint(rayOrigin + rayDir * distance);
            Vector3 localVec = localEnd - localOrigin;
            float localDist = localVec.magnitude;
            if (localDist <= MinDistance)
            {
                return false;
            }

            float halfWidth = Mathf.Max(0.08f, Mathf.Abs(lensOffsetInHead.x) - HeadClearance);
            var headCore = new Bounds(new Vector3(0f, 0.04f, -0.02f), new Vector3(halfWidth * 2f, 0.44f, 0.36f));
            if (headCore.Contains(localOrigin))
            {
                return false;
            }

            var localRay = new Ray(localOrigin, localVec / localDist);
            return headCore.IntersectRay(localRay, out float hit) && hit >= 0f && hit <= localDist;
        }

        private static bool HitsStructuralMesh(
            Vector3 rayOrigin,
            Vector3 rayDir,
            float distance,
            Transform head,
            MeshFilter filter)
        {
            if (filter == null || filter.sharedMesh == null || !filter.gameObject.activeInHierarchy)
            {
                return false;
            }

            Transform tr = filter.transform;
            if (!string.Equals(tr.name, "Chassis", StringComparison.Ordinal))
            {
                return false;
            }

            Bounds bounds = filter.sharedMesh.bounds;
            Vector3 localOrigin = tr.InverseTransformPoint(rayOrigin);
            if (bounds.Contains(localOrigin))
            {
                return false;
            }

            Vector3 localEnd = tr.InverseTransformPoint(rayOrigin + rayDir * distance);
            Vector3 localVec = localEnd - localOrigin;
            float localDist = localVec.magnitude;
            if (localDist <= MinDistance)
            {
                return false;
            }

            var localRay = new Ray(localOrigin, localVec / localDist);
            return bounds.IntersectRay(localRay, out float hit) && hit >= 0.01f && hit <= localDist - 0.01f;
        }
    }
}
