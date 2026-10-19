using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Where the placing ghost stands: where the view's centre meets the ground (<see cref="PointFromView"/>), or
    /// <see cref="BuildingTuning.GhostAheadMetres"/> in front of 07 when there is no view, settling onto the
    /// apron's marked bay for the charging pad once it rides within <see cref="BuildingTuning.ApronSnapRadius"/> of it
    /// and 07 stands close enough that the snapped frame is still inside weld range (see
    /// <see cref="BuildingTuning.ApronSnapReachMargin"/>).
    /// </summary>
    public static class GhostAim
    {
        private const float RayStepMetres = 0.5f;
        private const int RayRefineSteps = 8;

        /// <summary>The farthest the bay may stand from 07 for the ghost to settle onto it.</summary>
        public static float SnapReach(BuildingTuning tuning)
        {
            return Mathf.Max(0f, tuning.WeldRange - tuning.ApronSnapReachMargin);
        }

        /// <summary>
        /// Where the ghost stands when it follows the camera's aim: the ground under the view's centre, held between
        /// <see cref="BuildingTuning.MinPlacementDistance"/> and <see cref="BuildingTuning.MaxPlacementDistance"/> of
        /// 07 along the way it lies (a view that never meets the ground gives the farthest spot straight ahead), and
        /// settling onto the apron's bay when the view points along it (see <see cref="Point"/>).
        /// </summary>
        public static Vector3 PointFromView(ITerrainQuery terrain, Vector3 eye, Vector3 forward, Vector3 roverPosition,
            Vector3? apronCentre, BuildingTuning tuning)
        {
            Vector3 flatForward = forward;
            flatForward.y = 0f;
            flatForward = flatForward.sqrMagnitude > 1e-6f ? flatForward.normalized : Vector3.forward;
            Vector3 hit = TryGround(terrain, eye, forward, tuning.ViewRayMetres, out Vector3 ground)
                ? ground
                : roverPosition + flatForward * tuning.MaxPlacementDistance;
            Vector3 away = hit - roverPosition;
            away.y = 0f;
            float distance = away.magnitude;
            Vector3 direction = distance > 1e-4f ? away / distance : flatForward;
            Vector3 point = roverPosition + direction * Mathf.Clamp(distance, tuning.MinPlacementDistance,
                tuning.MaxPlacementDistance);
            return LooksAt(eye, flatForward, apronCentre, roverPosition, tuning)
                ? apronCentre.Value
                : Settle(point, roverPosition, apronCentre, tuning);
        }

        /// <summary>
        /// True when the view points along the apron's bay (the line of sight passes within the snap radius of its
        /// centre, ahead of the eye) and the bay is within snap reach of 07: looking toward it is enough for the pad's
        /// ghost to settle there, whatever the pitch.
        /// </summary>
        private static bool LooksAt(Vector3 eye, Vector3 flatForward, Vector3? apronCentre, Vector3 roverPosition,
            BuildingTuning tuning)
        {
            if (!apronCentre.HasValue ||
                SurfaceRules.HorizontalDistanceSquared(roverPosition, apronCentre.Value) >
                SnapReach(tuning) * SnapReach(tuning))
            {
                return false;
            }

            Vector3 toBay = apronCentre.Value - eye;
            toBay.y = 0f;
            float along = Vector3.Dot(toBay, flatForward);
            if (along <= 0f)
            {
                return false;
            }

            Vector3 off = toBay - flatForward * along;
            return off.sqrMagnitude <= tuning.ApronSnapRadius * tuning.ApronSnapRadius;
        }

        private static bool TryGround(ITerrainQuery terrain, Vector3 eye, Vector3 forward, float reach,
            out Vector3 ground)
        {
            ground = eye;
            if (forward.y >= 0f)
            {
                return false;
            }

            Vector3 direction = forward.normalized;
            float previous = 0f;
            for (float along = RayStepMetres; along <= reach; along += RayStepMetres)
            {
                Vector3 at = eye + direction * along;
                float gap = at.y - terrain.SampleHeight(at.x, at.z);
                if (gap <= 0f)
                {
                    float low = previous;
                    float high = along;
                    for (int i = 0; i < RayRefineSteps; i++)
                    {
                        float mid = (low + high) * 0.5f;
                        Vector3 probe = eye + direction * mid;
                        if (probe.y - terrain.SampleHeight(probe.x, probe.z) <= 0f)
                        {
                            high = mid;
                        }
                        else
                        {
                            low = mid;
                        }
                    }

                    ground = eye + direction * high;
                    return true;
                }

                previous = along;
            }

            return false;
        }

        public static Vector3 Point(Vector3 roverPosition, Vector3 heading, Vector3? apronCentre, BuildingTuning tuning)
        {
            Vector3 forward = heading;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            return Settle(roverPosition + forward * tuning.GhostAheadMetres, roverPosition, apronCentre, tuning);
        }

        private static Vector3 Settle(Vector3 point, Vector3 roverPosition, Vector3? apronCentre,
            BuildingTuning tuning)
        {
            if (apronCentre.HasValue &&
                SurfaceRules.HorizontalDistanceSquared(point, apronCentre.Value) <=
                tuning.ApronSnapRadius * tuning.ApronSnapRadius &&
                SurfaceRules.HorizontalDistanceSquared(roverPosition, apronCentre.Value) <=
                SnapReach(tuning) * SnapReach(tuning))
            {
                return apronCentre.Value;
            }

            return point;
        }
    }
}
