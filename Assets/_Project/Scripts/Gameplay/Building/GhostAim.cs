using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Where the placing ghost stands: <see cref="BuildingTuning.GhostAheadMetres"/> in front of 07, settling onto the
    /// apron's marked bay for the charging pad once it rides within <see cref="BuildingTuning.ApronSnapRadius"/> of it
    /// and 07 stands close enough that the snapped frame is still inside weld range (see
    /// <see cref="BuildingTuning.ApronSnapReachMargin"/>).
    /// </summary>
    public static class GhostAim
    {
        /// <summary>The farthest the bay may stand from 07 for the ghost to settle onto it.</summary>
        public static float SnapReach(BuildingTuning tuning)
        {
            return Mathf.Max(0f, tuning.WeldRange - tuning.ApronSnapReachMargin);
        }

        public static Vector3 Point(Vector3 roverPosition, Vector3 heading, Vector3? apronCentre, BuildingTuning tuning)
        {
            Vector3 forward = heading;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            Vector3 point = roverPosition + forward * tuning.GhostAheadMetres;
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
