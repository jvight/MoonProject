using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Where a beam from 07 meets a target's real surface, never the air beside or past it (the owner saw the scan and
    /// the weld land in empty space): the nearest hit of a ray from the emitter through an aim point on the target's
    /// colliders, else the nearest point on its convex colliders, else on the oriented box of its mesh's own bounds.
    /// Allocation-free.
    /// </summary>
    public static class SurfaceAim
    {
        /// <summary>Metres a beam's ray runs on past its aim point, so an aim point inside the target still meets the
        /// far side of whatever stands between.</summary>
        public const float ReachPast = 1.5f;

        /// <summary>
        /// Where across a collider's bounds the rays toward its parts go (a 3x3x3 grid, as shares of its extents): a
        /// part with air in its middle (rails either side of a gap) still meets one.
        /// </summary>
        private static readonly float[] PartGrid = { -0.8f, 0f, 0.8f };

        /// <summary>
        /// Degrees between the level rays fanned across a part's footprint: at a few metres they fall a few
        /// centimetres apart, so a thin strut standing alone at that height still meets one.
        /// </summary>
        public const float FanStepDegrees = 0.5f;

        /// <summary>
        /// The nearest point where the ray from <paramref name="origin"/> through <paramref name="through"/> meets any
        /// enabled collider of <paramref name="colliders"/>.
        /// </summary>
        public static bool TryHit(IReadOnlyList<Collider> colliders, Vector3 origin, Vector3 through,
            out Vector3 point, out Vector3 normal)
        {
            point = through;
            normal = Vector3.up;
            Vector3 toward = through - origin;
            float length = toward.magnitude;
            if (colliders == null || length < 1e-4f)
            {
                return false;
            }

            var ray = new Ray(origin, toward / length);
            float best = length + ReachPast;
            bool found = false;
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider collider = colliders[i];
                if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy &&
                    collider.Raycast(ray, out RaycastHit hit, best))
                {
                    best = hit.distance;
                    point = hit.point;
                    normal = hit.normal;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// Where rays from <paramref name="origin"/> across each collider's bounds meet it, the hit nearest
        /// <paramref name="near"/>: a real part of a target whose middle is air (rails either side of a gap), even
        /// where its colliders are meshes with no nearest point to ask for.
        /// </summary>
        public static bool TryNearestPart(IReadOnlyList<Collider> colliders, Vector3 origin, Vector3 near,
            out Vector3 point, out Vector3 normal)
        {
            point = near;
            normal = Vector3.up;
            if (colliders == null)
            {
                return false;
            }

            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Bounds bounds = collider.bounds;
                for (int x = 0; x < PartGrid.Length; x++)
                {
                    for (int y = 0; y < PartGrid.Length; y++)
                    {
                        for (int z = 0; z < PartGrid.Length; z++)
                        {
                            Vector3 aim = bounds.center + Vector3.Scale(bounds.extents,
                                new Vector3(PartGrid[x], PartGrid[y], PartGrid[z]));
                            Vector3 toward = aim - origin;
                            float length = toward.magnitude;
                            if (length < 1e-4f || !collider.Raycast(new Ray(origin, toward / length),
                                    out RaycastHit hit, length + ReachPast))
                            {
                                continue;
                            }

                            float distance = (hit.point - near).sqrMagnitude;
                            if (distance < best)
                            {
                                best = distance;
                                point = hit.point;
                                normal = hit.normal;
                                found = true;
                            }
                        }
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Where a fan of level rays from <paramref name="origin"/>, at its height, swept across each collider's
        /// footprint in <see cref="FanStepDegrees"/> steps meets it, the hit nearest <paramref name="near"/>: a point
        /// on the colliders' surface at exactly that height (a layer line drawn round a structure), thin struts
        /// included, or false where nothing stands at it.
        /// </summary>
        public static bool TryAcrossLevel(IReadOnlyList<Collider> colliders, Vector3 origin, Vector3 near,
            out Vector3 point, out Vector3 normal)
        {
            point = near;
            normal = Vector3.up;
            if (colliders == null)
            {
                return false;
            }

            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Bounds bounds = collider.bounds;
                if (origin.y < bounds.min.y || origin.y > bounds.max.y)
                {
                    continue;
                }

                FootprintAngles(bounds, origin, out float from, out float to, out float reach);
                for (float angle = from; angle <= to; angle += FanStepDegrees)
                {
                    float radians = angle * Mathf.Deg2Rad;
                    var ray = new Ray(origin, new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)));
                    if (!collider.Raycast(ray, out RaycastHit hit, reach))
                    {
                        continue;
                    }

                    float distance = (hit.point - near).sqrMagnitude;
                    if (distance < best)
                    {
                        best = distance;
                        point = hit.point;
                        normal = hit.normal;
                        found = true;
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// The headings (degrees, about world up) between which <paramref name="bounds"/>' footprint lies as seen
        /// from <paramref name="origin"/>, and how far its far side is.
        /// </summary>
        private static void FootprintAngles(Bounds bounds, Vector3 origin, out float from, out float to,
            out float reach)
        {
            Vector3 toCentre = bounds.center - origin;
            float centre = Mathf.Atan2(toCentre.x, toCentre.z) * Mathf.Rad2Deg;
            from = 0f;
            to = 0f;
            reach = 0f;
            for (int corner = 0; corner < 4; corner++)
            {
                float x = (corner & 1) == 0 ? bounds.min.x : bounds.max.x;
                float z = (corner & 2) == 0 ? bounds.min.z : bounds.max.z;
                float dx = x - origin.x;
                float dz = z - origin.z;
                float offset = Mathf.DeltaAngle(centre, Mathf.Atan2(dx, dz) * Mathf.Rad2Deg);
                from = Mathf.Min(from, offset);
                to = Mathf.Max(to, offset);
                reach = Mathf.Max(reach, Mathf.Sqrt(dx * dx + dz * dz));
            }

            from += centre;
            to += centre;
            reach += ReachPast;
        }

        /// <summary>
        /// The point on a convex collider of <paramref name="colliders"/> nearest <paramref name="target"/> (a
        /// non-convex mesh has no nearest point to ask for, and is skipped).
        /// </summary>
        public static bool TryClosest(IReadOnlyList<Collider> colliders, Vector3 target, out Vector3 point,
            out Vector3 normal)
        {
            point = target;
            normal = Vector3.up;
            if (colliders == null)
            {
                return false;
            }

            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy ||
                    (collider is MeshCollider mesh && !mesh.convex))
                {
                    continue;
                }

                Vector3 closest = collider.ClosestPoint(target);
                float distance = (closest - target).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    point = closest;
                    normal = distance > 1e-8f ? (target - closest).normalized : Vector3.up;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// Where the ray from <paramref name="origin"/> through <paramref name="through"/> enters the box
        /// <paramref name="local"/> carried by <paramref name="localToWorld"/> (a mesh's own bounds on its renderer).
        /// </summary>
        public static bool TryHitBox(Matrix4x4 localToWorld, Bounds local, Vector3 origin, Vector3 through,
            out Vector3 point, out Vector3 normal)
        {
            Matrix4x4 worldToLocal = localToWorld.inverse;
            Vector3 o = worldToLocal.MultiplyPoint3x4(origin);
            Vector3 d = worldToLocal.MultiplyVector(through - origin);
            point = through;
            normal = Vector3.up;
            float enter = 0f;
            float exit = 1f + ReachPast / Mathf.Max((through - origin).magnitude, 1e-4f);
            int enterAxis = -1;
            float enterSign = 0f;
            for (int axis = 0; axis < 3; axis++)
            {
                float min = local.min[axis];
                float max = local.max[axis];
                if (Mathf.Abs(d[axis]) < 1e-8f)
                {
                    if (o[axis] < min || o[axis] > max)
                    {
                        return false;
                    }

                    continue;
                }

                float t0 = (min - o[axis]) / d[axis];
                float t1 = (max - o[axis]) / d[axis];
                float sign = -1f;
                if (t0 > t1)
                {
                    (t0, t1) = (t1, t0);
                    sign = 1f;
                }

                if (t0 > enter)
                {
                    enter = t0;
                    enterAxis = axis;
                    enterSign = sign;
                }

                exit = Mathf.Min(exit, t1);
                if (enter > exit)
                {
                    return false;
                }
            }

            if (enterAxis < 0)
            {
                // The origin lies inside the box: the beam meets its nearest face.
                ClosestOnBox(localToWorld, local, origin, out point, out normal);
                return true;
            }

            Vector3 localNormal = Vector3.zero;
            localNormal[enterAxis] = enterSign;
            point = localToWorld.MultiplyPoint3x4(o + d * enter);
            normal = worldToLocal.transpose.MultiplyVector(localNormal).normalized;
            return true;
        }

        /// <summary>
        /// The point on the surface of the box <paramref name="local"/> carried by <paramref name="localToWorld"/>
        /// nearest <paramref name="target"/>: clamped into it from outside, pushed to its nearest face from inside.
        /// </summary>
        public static void ClosestOnBox(Matrix4x4 localToWorld, Bounds local, Vector3 target, out Vector3 point,
            out Vector3 normal)
        {
            Vector3 p = localToWorld.inverse.MultiplyPoint3x4(target);
            Vector3 min = local.min;
            Vector3 max = local.max;
            Vector3 clamped = Vector3.Max(min, Vector3.Min(max, p));
            Vector3 localNormal = Vector3.zero;
            if (clamped != p)
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    localNormal[axis] = p[axis] > max[axis] ? 1f : p[axis] < min[axis] ? -1f : 0f;
                }
            }
            else
            {
                float nearest = float.MaxValue;
                int face = 0;
                float sign = 1f;
                for (int axis = 0; axis < 3; axis++)
                {
                    float toMin = p[axis] - min[axis];
                    float toMax = max[axis] - p[axis];
                    if (toMin < nearest)
                    {
                        nearest = toMin;
                        face = axis;
                        sign = -1f;
                    }

                    if (toMax < nearest)
                    {
                        nearest = toMax;
                        face = axis;
                        sign = 1f;
                    }
                }

                clamped[face] = sign > 0f ? max[face] : min[face];
                localNormal[face] = sign;
            }

            point = localToWorld.MultiplyPoint3x4(clamped);
            normal = localToWorld.inverse.transpose.MultiplyVector(localNormal).normalized;
        }
    }
}
