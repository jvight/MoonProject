using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Keeps a friend out of the player's view of 07: a point between the camera and 07 (inside a cone of
    /// <c>minAngle</c> around the camera's line to 07) is pushed sideways to the cone's edge. Pure.
    /// </summary>
    public static class CameraClearance
    {
        /// <summary>Points this far (m) beyond 07 along the camera's line cannot hide it.</summary>
        private const float BeyondRover = 1f;

        public static Vector3 Apply(Vector3 point, Vector3 rover, Vector3 camera, float minAngleDegrees)
        {
            Vector3 toRover = rover - camera;
            float roverDistance = toRover.magnitude;
            if (roverDistance < 1e-3f)
            {
                return point;
            }

            Vector3 axis = toRover / roverDistance;
            Vector3 toPoint = point - camera;
            float along = Vector3.Dot(toPoint, axis);
            if (along <= 0f || along > roverDistance + BeyondRover)
            {
                return point;
            }

            Vector3 lateral = toPoint - axis * along;
            float offset = lateral.magnitude;
            float needed = along * Mathf.Tan(minAngleDegrees * Mathf.Deg2Rad);
            if (offset >= needed)
            {
                return point;
            }

            Vector3 side = offset > 1e-3f ? lateral / offset : Vector3.Cross(Vector3.up, axis).normalized;
            if (side.sqrMagnitude < 1e-6f)
            {
                side = Vector3.right;
            }

            return camera + axis * along + side * needed;
        }

        /// <summary>Angle (degrees) at the camera between <paramref name="point"/> and 07.</summary>
        public static float AngleOff(Vector3 point, Vector3 rover, Vector3 camera)
        {
            return Vector3.Angle(point - camera, rover - camera);
        }
    }
}
