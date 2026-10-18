using UnityEngine;

namespace MoonProject.Autoplay
{
    /// <summary>The mouse movement a player makes to bring the view onto something, as a feedback step per frame.</summary>
    public static class LookSteering
    {
        /// <summary>Degrees off within which the view counts as on the target.</summary>
        public const float ToleranceDegrees = 2.5f;

        /// <summary>Mouse pixels asked per degree of error; the loop runs every frame so it settles.</summary>
        public const float PixelsPerDegree = 1.2f;

        public const float MaxPixels = 18f;

        /// <summary>Pixels to move this frame for a view that is <paramref name="errorDegrees"/> off (positive = target further right/up).</summary>
        public static float Pixels(float errorDegrees)
        {
            if (Mathf.Abs(errorDegrees) <= ToleranceDegrees)
            {
                return 0f;
            }

            return Mathf.Clamp(errorDegrees * PixelsPerDegree, -MaxPixels, MaxPixels);
        }

        /// <summary>Yaw error in degrees (positive: target to the right) from the view's forward to a world point.</summary>
        public static float YawError(Vector3 viewPosition, Vector3 viewForward, Vector3 target)
        {
            return DriveSteering.HeadingError(viewPosition, viewForward, target);
        }

        /// <summary>Pitch error in degrees (positive: target above the view's centre).</summary>
        public static float PitchError(Vector3 viewPosition, Vector3 viewForward, Vector3 target)
        {
            Vector3 toTarget = target - viewPosition;
            float wanted = Mathf.Asin(Mathf.Clamp(toTarget.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
            float have = Mathf.Asin(Mathf.Clamp(viewForward.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
            return wanted - have;
        }
    }
}
